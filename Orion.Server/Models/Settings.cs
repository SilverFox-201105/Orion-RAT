using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Orion.Server.Models
{
    public static class Settings
    {
        private static readonly string OrionStuffDir = Path.Combine(Application.StartupPath, "OrionStuff");
        private static readonly string SettingsPath = Path.Combine(OrionStuffDir, "settings.json");
        private static SettingsModel _settings;
        private static readonly object _lockObject = new object();

        public static readonly string CertificatePath = Path.Combine(OrionStuffDir, "Orion.p12");

        private static SettingsModel LoadSettings()
        {
            if (_settings != null)
                return _settings;

            lock (_lockObject)
            {
                if (_settings != null)
                    return _settings;

                try
                {
                    if (File.Exists(SettingsPath))
                    {
                        string json = File.ReadAllText(SettingsPath);
                        _settings = JsonConvert.DeserializeObject<SettingsModel>(json) ?? new SettingsModel();
                    }
                    else
                    {
                        _settings = new SettingsModel();
                        SaveSettings();
                    }
                }
                catch
                {
                    _settings = new SettingsModel();
                }

                return _settings;
            }
        }

        private static void SaveSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    if (!Directory.Exists(OrionStuffDir))
                    {
                        Directory.CreateDirectory(OrionStuffDir);
                    }
                    string json = JsonConvert.SerializeObject(_settings, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(SettingsPath, json);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 在一个锁内批量修改设置并【只落盘一次】。
        /// 逐属性赋值（Settings.X = v）每个 setter 都会立刻写一次 settings.json，
        /// 一旦中途出错或被半空的表单值覆盖，磁盘上就会留下半截配置。
        /// 表单保存应统一走这里。
        /// </summary>
        public static void Update(Action<SettingsModel> mutate)
        {
            if (mutate == null) return;

            lock (_lockObject)
            {
                mutate(LoadSettings());
                SaveSettings();
            }
        }

        public static ushort ListenPort
        {
            get { return LoadSettings().ListenPort; }
            set { LoadSettings().ListenPort = value; SaveSettings(); }
        }

        public static ushort[] ListenPorts
        {
            get { return LoadSettings().ListenPorts ?? new ushort[0]; }
            set { LoadSettings().ListenPorts = value; SaveSettings(); }
        }

        public static bool IPv6Support
        {
            get { return LoadSettings().IPv6Support; }
            set { LoadSettings().IPv6Support = value; SaveSettings(); }
        }

        public static bool AutoListen
        {
            get { return LoadSettings().AutoListen; }
            set { LoadSettings().AutoListen = value; SaveSettings(); }
        }

        public static bool EventLog
        {
            get { return LoadSettings().EventLog; }
            set { LoadSettings().EventLog = value; SaveSettings(); }
        }

        public static bool TelegramNotifications
        {
            get { return LoadSettings().TelegramNotifications; }
            set { LoadSettings().TelegramNotifications = value; SaveSettings(); }
        }

        public static bool ShowPopup
        {
            get { return LoadSettings().ShowPopup; }
            set { LoadSettings().ShowPopup = value; SaveSettings(); }
        }

        public static string TelegramChatID
        {
            get { return LoadSettings().TelegramChatID; }
            set { LoadSettings().TelegramChatID = value; SaveSettings(); }
        }

        public static string TelegramBotToken
        {
            get { return LoadSettings().TelegramBotToken; }
            set { LoadSettings().TelegramBotToken = value; SaveSettings(); }
        }

        public static string SaveFormat
        {
            get { return LoadSettings().SaveFormat; }
            set { LoadSettings().SaveFormat = value; SaveSettings(); }
        }

        public static ushort ReverseProxyPort
        {
            get { return LoadSettings().ReverseProxyPort; }
            set { LoadSettings().ReverseProxyPort = value; SaveSettings(); }
        }

        public static bool ShowCountryGroups
        {
            get { return LoadSettings().ShowCountryGroups; }
            set { LoadSettings().ShowCountryGroups = value; SaveSettings(); }
        }

        public static bool ShowTagGroups
        {
            get { return LoadSettings().ShowTagGroups; }
            set { LoadSettings().ShowTagGroups = value; SaveSettings(); }
        }

        public static bool FrpEnabled
        {
            get { return LoadSettings().FrpEnabled; }
            set { LoadSettings().FrpEnabled = value; SaveSettings(); }
        }

        public static string FrpRawConfig
        {
            get { return LoadSettings().FrpRawConfig; }
            set { LoadSettings().FrpRawConfig = value; SaveSettings(); }
        }

        public static string FrpConfigFormat
        {
            get { return LoadSettings().FrpConfigFormat; }
            set { LoadSettings().FrpConfigFormat = value; SaveSettings(); }
        }
    }
}
