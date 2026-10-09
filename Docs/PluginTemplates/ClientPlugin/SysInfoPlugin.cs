using System;
using System.Text;
using Orion.Common.Plugins;

namespace MyClientPlugin.ClientModule
{
    /// <summary>
    /// 客户端插件模板：在被控机上收集系统信息并回传。
    ///
    /// 服务端下发时要填的 TypeName 是【完整类型名】：
    ///     "MyClientPlugin.ClientModule.SysInfoPlugin"
    ///
    /// 三条铁律：
    ///   1. public + 非抽象 + 公共无参构造函数
    ///   2. PluginResult.Data 只能是 byte[]（客户端会强转）
    ///   3. ExecuteCommand 的 parameters 运行时是 byte[] 或 null
    /// </summary>
    public sealed class SysInfoPlugin : IUniversalPlugin
    {
        private string _initPayload;

        // ------------------------------------------------------------------
        // 以下三个属性会被【服务端 net9 进程】反射读取以完成登记。
        // 必须是零依赖的常量 —— 不能引用任何 net9 里不存在的类型，
        // 否则服务端 Assembly.Load 时抛 ReflectionTypeLoadException，插件直接被跳过。
        // ------------------------------------------------------------------

        public string PluginId => "MyClientPlugin_SysInfo";
        public string Version => "1.0.0";
        public string[] SupportedCommands => new[] { "sysinfo", "drives", "env", "echo" };

        /// <summary>
        /// 返回 true 会在本次命令执行后被卸载（自动调 Cleanup）。
        /// 常驻插件保持 false。
        /// </summary>
        public bool IsComplete => false;

        // ------------------------------------------------------------------
        // 生命周期
        // ------------------------------------------------------------------

        public void Initialize(object initData)
        {
            // initData 来自两处之一：
            //   1. 服务端 PushSender.LoadUniversalPlugin(..., initData, ...) 传入的字节
            //   2. 插件目录下与 DLL 同名的 .init 文件（如 SysInfoPlugin.Client.init）
            // 运行时类型是 byte[] 或 null。
            if (initData is byte[] bytes && bytes.Length > 0)
            {
                try { _initPayload = Encoding.UTF8.GetString(bytes); }
                catch { _initPayload = null; }
            }
        }

        public PluginResult ExecuteCommand(string command, object parameters)
        {
            try
            {
                // parameters 运行时是 byte[] 或 null
                var args = parameters as byte[];
                var argText = (args != null && args.Length > 0)
                    ? Encoding.UTF8.GetString(args)
                    : string.Empty;

                switch (command)
                {
                    case "sysinfo": return Ok(CollectSysInfo());
                    case "drives":  return Ok(CollectDrives());
                    case "env":     return Ok(CollectEnv());
                    case "echo":    return Ok("echo: " + argText);
                    default:        return Fail("不支持的命令: " + command);
                }
            }
            catch (Exception ex)
            {
                // ⚠️ 抛出未处理异常会导致插件被立刻卸载。
                //    所以这里主动兜住，把错误作为失败结果回传。
                return Fail(ex.GetType().Name + ": " + ex.Message);
            }
        }

        public void Cleanup()
        {
            _initPayload = null;
        }

        // ------------------------------------------------------------------
        // 业务实现（只用到 mscorlib，无任何外部依赖）
        // ------------------------------------------------------------------

        private static string CollectSysInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MachineName : " + Environment.MachineName);
            sb.AppendLine("UserName    : " + Environment.UserDomainName + "\\" + Environment.UserName);
            sb.AppendLine("OSVersion   : " + Environment.OSVersion);
            sb.AppendLine("Is64BitOS   : " + Environment.Is64BitOperatingSystem);
            sb.AppendLine("Processors  : " + Environment.ProcessorCount);
            sb.AppendLine("SystemDir   : " + Environment.SystemDirectory);
            sb.AppendLine("CurrentDir  : " + Environment.CurrentDirectory);
            sb.AppendLine("CLR Version : " + Environment.Version);
            sb.AppendLine("TickCount   : " + Environment.TickCount);
            return sb.ToString();
        }

        private static string CollectDrives()
        {
            var sb = new StringBuilder();
            foreach (var d in Environment.GetLogicalDrives())
            {
                sb.AppendLine("Drive: " + d);
            }
            return sb.ToString();
        }

        private static string CollectEnv()
        {
            var sb = new StringBuilder();
            foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                sb.AppendLine(e.Key + " = " + e.Value);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // 结果构造（Data 必须是 byte[]）
        // ------------------------------------------------------------------

        private static PluginResult Ok(string text)
        {
            return new PluginResult
            {
                Success = true,
                Message = "OK",
                Data = Encoding.UTF8.GetBytes(text ?? string.Empty),
                ShouldUnload = false,
                NextCommand = null
            };
        }

        private static PluginResult Fail(string message)
        {
            return new PluginResult
            {
                Success = false,
                Message = message ?? "unknown error",
                Data = Encoding.UTF8.GetBytes(string.Empty),
                ShouldUnload = false,
                NextCommand = null
            };
        }
    }
}
