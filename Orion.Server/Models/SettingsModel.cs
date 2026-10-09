using Newtonsoft.Json;

namespace Orion.Server.Models
{
    public class SettingsModel
    {
        [JsonProperty("listenPort")]
        public ushort ListenPort { get; set; } = 4782;

        [JsonProperty("ipv6Support")]
        public bool IPv6Support { get; set; } = false;

        [JsonProperty("autoListen")]
        public bool AutoListen { get; set; } = false;

        [JsonProperty("eventLog")]
        public bool EventLog { get; set; } = false;

        [JsonProperty("telegramNotifications")]
        public bool TelegramNotifications { get; set; } = false;

        [JsonProperty("showPopup")]
        public bool ShowPopup { get; set; } = false;

        [JsonProperty("enableNoIPUpdater")]
        public bool EnableNoIPUpdater { get; set; } = false;

        [JsonProperty("telegramChatID")]
        public string TelegramChatID { get; set; } = "";

        [JsonProperty("telegramBotToken")]
        public string TelegramBotToken { get; set; } = "";


        [JsonProperty("saveFormat")]
        public string SaveFormat { get; set; } = "APP - URL - USER:PASS";

        [JsonProperty("reverseProxyPort")]
        public ushort ReverseProxyPort { get; set; } = 3128;

        [JsonProperty("ListenPorts")]
        public ushort[] ListenPorts { get; set; } = [4782];

        [JsonProperty("showCountryGroups")]
        public bool ShowCountryGroups { get; set; } = true;

        [JsonProperty("showTagGroups")]
        public bool ShowTagGroups { get; set; } = false;

        // ---- FRP / C2 隧道 ----
        // 是否启用 FRP 隧道（纯开关；真正的建链动作由设置页「连接」按钮触发）。
        [JsonProperty("frpEnabled")]
        public bool FrpEnabled { get; set; } = false;

        // frpc 配置文件的完整内容。第三方托管 frp(如 ChmlFrp)会把本地配置与其云端记录严格比对，
        // 因此直接让用户粘贴由服务商提供的原始配置文件，Orion 原样写盘并启动 frpc，避免字段映射错位。
        [JsonProperty("frpRawConfig")]
        public string FrpRawConfig { get; set; } = "";

        // 配置文件格式：Auto / Toml / Ini。Auto 时按 frpc 版本自动选（>=0.33 用 TOML，否则 INI），
        // 决定写盘用的扩展名(.toml/.ini)——frpc 按扩展名选择解析器。
        [JsonProperty("frpConfigFormat")]
        public string FrpConfigFormat { get; set; } = "Auto";
    }
}
