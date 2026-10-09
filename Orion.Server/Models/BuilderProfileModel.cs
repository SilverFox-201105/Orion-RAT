using Newtonsoft.Json;
using System;

namespace Orion.Server.Models
{
    public class BuilderProfileModel
    {
        [JsonProperty("hosts")]
        public string Hosts { get; set; } = "";

        [JsonProperty("pastebin")]
        public string Pastebin { get; set; } = "";

        [JsonProperty("tag")]
        public string Tag { get; set; } = "Office04";

        [JsonProperty("delay")]
        public int Delay { get; set; } = 3000;

        [JsonProperty("mutex")]
        public string Mutex { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("installClient")]
        public bool InstallClient { get; set; } = false;

        [JsonProperty("installName")]
        public string InstallName { get; set; } = "Client";

        [JsonProperty("installPath")]
        public short InstallPath { get; set; } = 1;

        [JsonProperty("installSub")]
        public string InstallSub { get; set; } = "SubDir";

        [JsonProperty("addStartup")]
        public bool AddStartup { get; set; } = false;

        [JsonProperty("registryName")]
        public string RegistryName { get; set; } = "Orion Client Startup";

        [JsonProperty("changeIcon")]
        public bool ChangeIcon { get; set; } = false;

        [JsonProperty("iconPath")]
        public string IconPath { get; set; } = "";

        [JsonProperty("changeAsmInfo")]
        public bool ChangeAsmInfo { get; set; } = false;

        [JsonProperty("enablePastebin")]
        public bool EnablePastebin { get; set; } = false;

        [JsonProperty("productName")]
        public string ProductName { get; set; } = "";

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = "";

        [JsonProperty("copyright")]
        public string Copyright { get; set; } = "";

        [JsonProperty("trademarks")]
        public string Trademarks { get; set; } = "";

        [JsonProperty("originalFilename")]
        public string OriginalFilename { get; set; } = "";

        [JsonProperty("productVersion")]
        public string ProductVersion { get; set; } = "";

        [JsonProperty("fileVersion")]
        public string FileVersion { get; set; } = "";

        // 默认 64 位：绝大多数目标已是 x64，且 64 位客户端可直接使用完整的 API 集合。
        [JsonProperty("targetArchitecture")]
        public byte TargetArchitecture { get; set; } = 1;

        // 用户是否【主动】选过目标架构。
        // 该字段存在的意义是迁移：改成默认 64 位之前保存的 profile 里 targetArchitecture=0(x86)
        // 只是"当年没得选"的默认值，而不是用户真实意愿。有了这个标记，老配置也能一次性
        // 迁移到新的 64 位默认；用户一旦在界面上选过并保存，之后就始终尊重其选择。
        [JsonProperty("archExplicitlySet")]
        public bool ArchExplicitlySet { get; set; } = false;
    }
}
