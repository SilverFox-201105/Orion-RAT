using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Orion.Server.Utilities
{
    /// <summary>一个正在占用文件的进程。</summary>
    public sealed class FileLocker
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(ProcessName)
                ? "PID " + ProcessId
                : ProcessName + " (PID " + ProcessId + ")";
        }
    }

    /// <summary>
    /// 文件占用检测与释放。
    ///
    /// 构建客户端时若输出路径上的旧客户端仍在运行（或被编辑器/杀软扫描句柄持有），
    /// 直接写盘会抛 IOException("文件正由另一进程使用")。本类负责：
    ///   1) 判断文件是否真的被独占锁住；
    ///   2) 定位到底是哪个进程占着（优先用 Windows Restart Manager，精确定位到句柄持有者）；
    ///   3) 结束它们，让构建可以重试覆盖。
    /// </summary>
    public static class FileLockHelper
    {
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;
        private const int ERROR_MORE_DATA = 234;

        private const int ERROR_SHARING_VIOLATION = 32;
        private const int ERROR_LOCK_VIOLATION = 33;

        /// <summary>
        /// 文件是否被别的进程独占打开。用 FileShare.None 试探性打开：打不开即被占用。
        /// 文件不存在视为“未被占用”（构建时会新建）。
        /// </summary>
        public static bool IsLocked(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                // FileShare.None = 拒绝任何其他句柄；只要有人在用它就会失败
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // 权限不足也算“写不进去”，交给上层当作占用处理提示用户
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>异常是否为“文件被占用”类错误（含 InnerException 链）。</summary>
        public static bool IsFileLockError(Exception error)
        {
            for (Exception e = error; e != null; e = e.InnerException)
            {
                if (e is IOException || e is UnauthorizedAccessException)
                {
                    // 写保护/目录不可写等同属 IOException，但不一定是被进程占用；
                    // 是否为真的占用由调用方再用 IsLocked 二次确认。
                    return true;
                }

                int hr = Marshal.GetHRForException(e) & 0xFFFF;
                if (hr == ERROR_SHARING_VIOLATION || hr == ERROR_LOCK_VIOLATION)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 找出占用该文件的进程。
        /// 主路径：Restart Manager（能给出所有持有该文件句柄的进程，含非“主模块”场景）。
        /// 回退：遍历进程比较主模块路径（Restart Manager 不可用时，如极度精简的系统）。
        /// </summary>
        public static List<FileLocker> FindLockingProcesses(string path)
        {
            var result = new List<FileLocker>();
            if (string.IsNullOrWhiteSpace(path))
                return result;

            var seen = new HashSet<int>();
            int self = Process.GetCurrentProcess().Id;

            foreach (var l in QueryRestartManager(path))
            {
                if (l.ProcessId <= 0 || l.ProcessId == self || l.ProcessId == 4) continue;   // 0/4 = System
                if (seen.Add(l.ProcessId))
                    result.Add(l);
            }

            if (result.Count == 0)
            {
                foreach (var l in MatchByMainModule(path))
                {
                    if (l.ProcessId <= 0 || l.ProcessId == self) continue;
                    if (seen.Add(l.ProcessId))
                        result.Add(l);
                }
            }

            return result;
        }

        /// <summary>
        /// 结束所有占用进程并等待其真正退出。
        /// </summary>
        /// <param name="lockers">占用进程</param>
        /// <param name="failed">结束失败的描述（进程名 + 原因），全部成功时为 null</param>
        /// <returns>是否全部结束成功</returns>
        public static bool TryKillAll(IEnumerable<FileLocker> lockers, out string failed)
        {
            failed = null;
            bool allOk = true;

            foreach (var l in lockers)
            {
                try
                {
                    var p = Process.GetProcessById(l.ProcessId);
                    string name = SafeProcessName(p, l);

                    p.Kill();
                    // 等句柄真正释放，避免刚 Kill 完就去写盘仍然失败
                    p.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    allOk = false;
                    failed = (l.ProcessName ?? ("PID " + l.ProcessId)) + "：" + ex.Message;
                }
            }

            return allOk;
        }

        /// <summary>等待文件不再被占用，最多等待 <paramref name="timeoutMs"/> 毫秒。</summary>
        public static void WaitUntilUnlocked(string path, int timeoutMs)
        {
            int waited = 0;
            while (waited < timeoutMs && IsLocked(path))
            {
                Thread.Sleep(200);
                waited += 200;
            }
        }

        private static string SafeProcessName(Process p, FileLocker fallback)
        {
            try { return p.ProcessName; }
            catch { return fallback.ProcessName ?? ("PID " + fallback.ProcessId); }
        }

        // ---------------- Restart Manager ----------------

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public long ProcessStartTime;   // FILETIME（64 位）
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(uint pSessionHandle,
            uint nFiles, string[] rgsFilenames,
            uint nApplications, RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded, ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        private static List<FileLocker> QueryRestartManager(string path)
        {
            var list = new List<FileLocker>();
            uint session;
            string key = Guid.NewGuid().ToString();

            if (RmStartSession(out session, 0, key) != 0)
                return list;

            try
            {
                string fullPath;
                try { fullPath = Path.GetFullPath(path); }
                catch { return list; }

                int reg = RmRegisterResources(session, 1, new[] { fullPath }, 0, null, 0, null);
                if (reg != 0)
                    return list;

                uint needed = 0;
                uint count = 0;
                uint reboot = 0;

                // 先问“需要多少个”，ERROR_MORE_DATA 是正常返回
                int res = RmGetList(session, out needed, ref count, null, ref reboot);
                if (res != 0 && res != ERROR_MORE_DATA)
                    return list;
                if (needed == 0)
                    return list;

                count = needed;
                var infos = new RM_PROCESS_INFO[count];
                res = RmGetList(session, out needed, ref count, infos, ref reboot);
                if (res != 0)
                    return list;

                for (int i = 0; i < count && i < infos.Length; i++)
                {
                    int pid = infos[i].Process.dwProcessId;
                    if (pid <= 0) continue;

                    string name = infos[i].strAppName;
                    if (string.IsNullOrEmpty(name))
                        name = SafeNameOfPid(pid);

                    list.Add(new FileLocker { ProcessId = pid, ProcessName = name });
                }
            }
            finally
            {
                RmEndSession(session);
            }

            return list;
        }

        // ---------------- 回退方案：按主模块路径匹配 ----------------

        private static List<FileLocker> MatchByMainModule(string path)
        {
            var list = new List<FileLocker>();
            string target;
            try { target = Path.GetFullPath(path).ToLowerInvariant(); }
            catch { return list; }

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    string main = p.MainModule?.FileName;
                    if (string.IsNullOrEmpty(main)) continue;

                    if (string.Equals(main.ToLowerInvariant(), target, StringComparison.Ordinal))
                        list.Add(new FileLocker { ProcessId = p.Id, ProcessName = p.ProcessName });
                }
                catch (Win32Exception)
                {
                    // 系统进程/权限不足，跳过
                }
                catch (InvalidOperationException)
                {
                    // 进程已退出
                }
                catch (Exception)
                {
                }
            }

            return list;
        }

        private static string SafeNameOfPid(int pid)
        {
            try { return Process.GetProcessById(pid).ProcessName; }
            catch { return "PID " + pid; }
        }
    }
}
