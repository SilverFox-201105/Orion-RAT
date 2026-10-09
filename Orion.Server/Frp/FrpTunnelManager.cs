using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Orion.Server.Frp
{
    /// <summary>
    /// 把 FRP 客户端(frpc.exe)作为子进程内嵌到控端(Orion.Server)。
    ///
    /// 拓扑：被控端 → FRPS 公网:remote_port →(frp tcp 隧道)→ 控端本地:local_port(Orion 监听)。
    /// 控端主动出网连 FRPS，因此控端本身不需要公网 IP。
    ///
    /// 不再在 C# 里复刻 frp 协议，也不再由 Orion 拼装 frpc 配置字段——第三方托管 frp(如 ChmlFrp)
    /// 会在云端记录隧道参数并严格校验本地配置，字段映射极易错位。改为：用户在设置页直接粘贴
    /// 由服务商提供的原始 frpc 配置文件，Orion 原样写盘并启动 frpc.exe。Orion 只做编排：
    /// 解压安装、写配置、启停进程、读日志。frpc.exe 由用户在设置里选择官方/服务商压缩包提供，
    /// 不进仓库（见 .gitignore 的 FRPC/）。
    /// </summary>
    public class FrpTunnelManager
    {
        private static readonly FrpTunnelManager _instance = new FrpTunnelManager();
        public static FrpTunnelManager Instance => _instance;

        private Process _process;
        private readonly object _lock = new object();

        public event Action<string> StatusChanged;
        public event Action<string> LogReceived;

        // 累积所有 frpc 终端输出与状态变更，供设置页关闭重开时回放完整历史
        private readonly List<string> _history = new List<string>();
        private readonly object _historyLock = new object();
        private const int MaxHistoryLines = 2000;

        /// <summary>返回迄今所有 frpc 终端输出与状态行的快照（线程安全）。</summary>
        public List<string> GetHistory()
        {
            lock (_historyLock)
                return new List<string>(_history);
        }

        /// <summary>清空历史缓冲（用于取消勾选 FRP 时清理控制台）。</summary>
        public void ClearHistory()
        {
            lock (_historyLock)
                _history.Clear();
        }

        private void RecordLine(string line)
        {
            if (line == null) return;
            lock (_historyLock)
            {
                _history.Add(line);
                if (_history.Count > MaxHistoryLines)
                    _history.RemoveRange(0, _history.Count - MaxHistoryLines);
            }
            LogReceived?.Invoke(line);
        }

        private void RaiseStatus(string status)
        {
            if (status == null) return;
            RecordLine("[状态] " + status);
            StatusChanged?.Invoke(status);
        }

        /// <summary>当前正在运行的隧道所使用的配置签名；未运行时为 null。</summary>
        private string _currentSignature;

        /// <summary>
        /// 配置签名：用于在「设置保存后」判断是否需要重启隧道——只改了分组/Telegram/屏蔽 IP 时
        /// 不再把隧道断一次重连。现在配置即用户粘贴的原文，签名直接由其内容 + 格式决定。
        /// </summary>
        public static string CurrentSettingsSignature
        {
            get
            {
                if (!Models.Settings.FrpEnabled) return "disabled";
                return "raw|" + (Models.Settings.FrpConfigFormat ?? "Auto") + "|" + (Models.Settings.FrpRawConfig ?? "");
            }
        }

        /// <summary>隧道正在运行，且运行的正是这份配置（签名一致）。</summary>
        public bool IsRunningWith(string signature)
        {
            lock (_lock)
            {
                return _process != null && !_process.HasExited
                       && _currentSignature != null && _currentSignature == signature;
            }
        }

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _process != null && !_process.HasExited;
                }
            }
        }

        // ---- 路径 ----
        private string FrpcDir => Path.Combine(Application.StartupPath, "FRPC");
        private string FrpcPath => Path.Combine(FrpcDir, "frpc.exe");
        private string OrionStuffDir => Path.Combine(Application.StartupPath, "OrionStuff");

        /// <summary>配置文件路径：依据当前格式设置返回 .toml 或 .ini。</summary>
        public string ConfigPath
        {
            get
            {
                string fmt = ResolveFormat();
                return Path.Combine(OrionStuffDir, fmt == "ini" ? "frpc.ini" : "frpc.toml");
            }
        }

        /// <summary>frpc.exe 是否就位（由用户选择官方压缩包解压而来）。</summary>
        public bool IsBinaryAvailable => File.Exists(FrpcPath);

        /// <summary>frpc.exe 所在的 FRPC 目录名（用于 UI 提示）。</summary>
        public string FrpcDirectory => FrpcDir;

        // ====== 安装 / 版本探测 ======

        /// <summary>
        /// 从官方 frp 压缩包(.zip)中安全提取 frpc.exe 到 FRPC/frpc.exe。
        /// 只抽取 Name 精确等于 "frpc.exe" 的条目，并固定写入 FrpcPath，杜绝 zip-slip
        /// （绝不按压缩包内条目的相对路径写盘）。解压后跑一次 --version 校验真是 frpc。
        /// 失败抛异常并清理半成品。
        /// </summary>
        public void InstallFrpcFromZip(string zipPath)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("找不到压缩包: " + zipPath);

            if (!Directory.Exists(FrpcDir))
                Directory.CreateDirectory(FrpcDir);

            string tempPath = FrpcPath + ".tmp";
            bool extracted = false;
            try
            {
                using (var zip = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in zip.Entries)
                    {
                        // 只接受条目名恰好是 frpc.exe 的项（官方 Windows 包为
                        // frp_x.y.z_windows_amd64/frpc.exe）。忽略 frps.exe、LICENSE、其它一切。
                        if (!string.Equals(entry.Name, "frpc.exe", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                            continue; // 目录条目跳过
                        // 固定写入 FrpcPath——不碰条目里的任何路径，天然免疫 ../ 穿越。
                        using (var src = entry.Open())
                        using (var dst = File.Create(tempPath))
                        {
                            src.CopyTo(dst);
                        }
                        extracted = true;
                        break;
                    }
                }

                if (!extracted)
                    throw new InvalidDataException("压缩包里没有找到 frpc.exe（请确认下载的是官方 frp Windows 发行包）。");

                // 校验真是 frpc：跑 --version，输出必须含版本号
                string ver = RunFrpcVersion(tempPath);
                if (string.IsNullOrEmpty(ver))
                    throw new InvalidDataException("解压出的文件不是有效的 frpc.exe（--version 未返回版本号）。");

                // 原子替换：先删旧、再改名，避免中途异常留下半截文件
                if (File.Exists(FrpcPath))
                    File.Delete(FrpcPath);
                File.Move(tempPath, FrpcPath);
            }
            catch
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
                throw;
            }
        }

        /// <summary>运行 frpc --version，返回解析到的版本号字符串（如 "0.62.0"），失败返回 null。</summary>
        private string RunFrpcVersion(string exePath)
        {
            try
            {
                var psi = new ProcessStartInfo(exePath, "--version")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    // frpc(Go) 在被管道重定向时输出 UTF-8；显式指定，避免中文被按 GBK 误解码成乱码
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = FrpcDir
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(10000);
                string combined = stdout + "\n" + stderr;
                var m = Regex.Match(combined, @"(\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?)");
                return m.Success ? m.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        private string _cachedVersion;
        private DateTime _cachedVersionTime;

        /// <summary>已安装 frpc 的版本号；未安装或探测失败返回 null。带 5 分钟缓存，避免每次启连都 spawn 进程。</summary>
        public string GetInstalledVersion()
        {
            if (!IsBinaryAvailable) { _cachedVersion = null; return null; }
            var now = DateTime.UtcNow;
            if (_cachedVersion != null && (now - _cachedVersionTime).TotalMinutes < 5)
                return _cachedVersion;
            _cachedVersion = RunFrpcVersion(FrpcPath);
            _cachedVersionTime = now;
            return _cachedVersion;
        }

        // ====== 配置生成 ======

        /// <summary>把 Settings 里的字符串格式解析为内部枚举；非法/空 → Auto。</summary>
        private string ParseFormatSetting()
        {
            string s = (Models.Settings.FrpConfigFormat ?? "Auto").Trim();
            if (s.Equals("Toml", StringComparison.OrdinalIgnoreCase)) return "toml";
            if (s.Equals("Ini", StringComparison.OrdinalIgnoreCase)) return "ini";
            return "auto";
        }

        /// <summary>
        /// 解析最终配置格式：Auto 时按已安装 frpc 版本自动选——frp v0.33+ 用 TOML（INI 在 v0.51+ 被移除），
        /// 更早版本回退 INI。手动 Toml/Ini 则原样返回。
        /// </summary>
        public string ResolveFormat()
        {
            string fmt = ParseFormatSetting();
            if (fmt != "auto") return fmt;

            string ver = GetInstalledVersion();
            if (string.IsNullOrEmpty(ver))
                return "toml"; // 无版本信息时默认 TOML（官方当前默认格式）
            return VersionAtLeast(ver, 0, 33, 0) ? "toml" : "ini";
        }

        /// <summary>比较版本号 v >= major.minor.patch。</summary>
        private static bool VersionAtLeast(string version, int major, int minor, int patch)
        {
            var m = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)");
            if (!m.Success) return false;
            int vmaj = int.Parse(m.Groups[1].Value);
            int vmin = int.Parse(m.Groups[2].Value);
            int vpat = int.Parse(m.Groups[3].Value);
            if (vmaj != major) return vmaj > major;
            if (vmin != minor) return vmin > minor;
            return vpat >= patch;
        }

        /// <summary>
        /// 把用户粘贴的原始 frpc 配置原文原样写盘到配置文件(frpc.toml / frpc.ini)。
        /// 不改动任何内容——避免与第三方托管 frp(如 ChmlFrp)的云端记录校验冲突。
        /// 扩展名由 ResolveFormat() 依据格式设置决定(frpc 按扩展名选解析器)。
        /// </summary>
        public void WriteConfigFromText(string text)
        {
            if (!Directory.Exists(OrionStuffDir))
                Directory.CreateDirectory(OrionStuffDir);
            File.WriteAllText(ConfigPath, text ?? "");
        }

        /// <summary>
        /// 从当前保存的 frpc 配置原文中解析出"被控端应当连接的公开端点"——
        /// server_addr/serverAddr 与代理的 remote_port/remotePort。
        /// 供「客户端构建器」预填连接目标使用（连接目标 = frps:remote_port）。
        /// 解析失败(配置为空/缺字段)返回 false。
        /// </summary>
        public bool TryGetConnectionTarget(out string addr, out int remotePort)
        {
            addr = null;
            remotePort = 0;
            string cfg = Models.Settings.FrpRawConfig ?? "";
            if (string.IsNullOrWhiteSpace(cfg)) return false;

            // server 地址：INI/TOML 旧式 server_addr 或现代 TOML serverAddr；值可能带或不带引号
            var mAddr = Regex.Match(cfg, @"(?:server_addr|serverAddr)\s*=\s*""?\s*([^\s""#]+)");
            if (!mAddr.Success) return false;
            addr = mAddr.Groups[1].Value.Trim();

            // 代理公网端口：remote_port(旧/TOML) 或 remotePort(现代 TOML)
            var mPort = Regex.Match(cfg, @"remote_port\s*=\s*(\d+)|remotePort\s*=\s*(\d+)");
            if (mPort.Success)
                remotePort = int.Parse(mPort.Groups[1].Success ? mPort.Groups[1].Value : mPort.Groups[2].Value);
            return true;
        }

        /// <summary>
        /// 从当前保存的 frpc 配置原文里解析本地转发端口(local_port/localPort)——
        /// 隧道会把远端流量送到该端口，Orion 必须在此端口监听才能收到被控端连接。
        /// 解析失败(配置为空/缺字段)返回 false。
        /// </summary>
        public bool TryGetLocalPort(out int localPort)
        {
            localPort = 0;
            string cfg = Models.Settings.FrpRawConfig ?? "";
            if (string.IsNullOrWhiteSpace(cfg)) return false;
            var m = Regex.Match(cfg, @"local_port\s*=\s*(\d+)|localPort\s*=\s*(\d+)");
            if (m.Success)
                localPort = int.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
            return localPort > 0;
        }

        public void StartFromRaw(string rawConfigText)
        {
            lock (_lock)
            {
                // 内部重启不广播「已断开」——否则每次 Start 都会多出一条误导性的断开记录
                StopCore(false);

                if (string.IsNullOrWhiteSpace(rawConfigText))
                {
                    RaiseStatus("FRP 未配置（配置文件为空）");
                    RecordLine("frpc 配置文件为空。请在「C2 隧道」设置里粘贴 frpc 配置文件内容。");
                    return;
                }

                if (!IsBinaryAvailable)
                {
                    RaiseStatus("frpc.exe 缺失，无法启动隧道");
                    RecordLine("找不到 FRPC\\frpc.exe。请在「C2 隧道」设置里点击「安装/更新 frpc.exe」选择官方 frp 压缩包。");
                    return;
                }

                try
                {
                    WriteConfigFromText(rawConfigText);
                }
                catch (Exception ex)
                {
                    RaiseStatus("写 frpc 配置失败");
                    RecordLine($"写配置失败: {ex.Message}");
                    return;
                }

                var psi = new ProcessStartInfo(FrpcPath, $"-c \"{ConfigPath}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    // frpc(Go) 在被管道重定向时输出 UTF-8；显式指定，避免中文被按 GBK 误解码成乱码
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Application.StartupPath
                };

                _process = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                _process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    RecordLine(e.Data);
                    if (e.Data.Contains("login to server success", StringComparison.OrdinalIgnoreCase))
                        RaiseStatus("已连接 (C2 隧道在线)");
                    else if (e.Data.Contains("reconnect", StringComparison.OrdinalIgnoreCase) ||
                             e.Data.Contains("dial tcp", StringComparison.OrdinalIgnoreCase))
                        RaiseStatus("重连中...");
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    RecordLine(e.Data);
                    if (e.Data.Contains("login to server success", StringComparison.OrdinalIgnoreCase))
                        RaiseStatus("已连接 (C2 隧道在线)");
                };
                _process.Exited += (s, e) =>
                {
                    var dead = (Process)s;
                    bool mine;
                    lock (_lock)
                    {
                        // 仅当退出的正是当前记录的进程时才清理。否则旧进程异步退出时会把 _process 置空、
                        // 误杀/遗弃新启动的进程，导致新进程沦为孤儿，进而两个 frpc 同时注册同一个
                        // 代理名，在服务端触发冲突。
                        mine = _process == dead;
                        if (mine)
                        {
                            _process?.Dispose();
                            _process = null;
                            _currentSignature = null;
                        }
                    }
                    // 只针对「当前进程」的退出广播状态；迟到的旧进程退出不再翻转 UI 状态。
                    // （Stop() 已同步广播过「已断开」，这里不会重复。）
                    if (mine)
                        RaiseStatus("已断开");
                };

                try
                {
                    _process.Start();
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();
                    _currentSignature = CurrentSettingsSignature;
                    RaiseStatus("正在连接 FRPS...");
                }
                catch (Exception ex)
                {
                    _currentSignature = null;
                    RecordLine($"启动 frpc 失败: {ex.Message}");
                    RaiseStatus("启动失败");
                }
            }
        }

        /// <summary>停止隧道。若确实有进程在跑，同步广播一次「已断开」（由 Stop 而非异步 Exited 负责）。</summary>
        public void Stop()
        {
            StopCore(true);
        }

        private void StopCore(bool announceStatus)
        {
            bool wasRunning = false;

            lock (_lock)
            {
                if (_process != null && !_process.HasExited)
                {
                    wasRunning = true;
                    try { _process.Kill(); }
                    catch { /* 已退出/无权限：忽略 */ }
                    try { _process.Dispose(); }
                    catch { /* 忽略 */ }
                }
                _process = null;
                _currentSignature = null;
            }

            // 在锁外同步广播，保证「调用 Stop 返回后」历史里已经有 [状态] 已断开，
            // 调用方紧接着 ClearHistory() 才不会被迟到的异步 Exited 回写弄脏。
            if (wasRunning && announceStatus)
                RaiseStatus("已断开");
        }

        /// <summary>用当前全局 Settings 启动隧道（配置文件取粘贴原文）。</summary>
        public void StartFromSettings()
        {
            StartFromRaw(Models.Settings.FrpRawConfig);
        }
    }
}
