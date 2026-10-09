using Orion.Client.Config;
using Orion.Client.Extensions;
using Orion.Common.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Orion.Client.Setup
{
    public class ClientInstaller : ClientSetupBase
    {
        public void ApplySettings()
        {
            string clientPath;
            try
            {
                clientPath = Application.ExecutablePath;
            }
            catch
            {
                clientPath = null;
            }
            

            if (Settings.STARTUP && clientPath != null)
            {
                var clientStartup = new ClientStartup();
                if (Settings.INSTALL)
                {
                    clientStartup.AddToStartup(Settings.INSTALLPATH, Settings.STARTUPKEY);
                }
                else
                {
                    clientStartup.AddToStartup(Application.ExecutablePath, Settings.STARTUPKEY);
                }

            }
        }

        public void Install()
        {
            // create target dir
            if (!Directory.Exists(Path.GetDirectoryName(Settings.INSTALLPATH)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Settings.INSTALLPATH));
            }

            // delete existing file
            if (File.Exists(Settings.INSTALLPATH))
            {
                try
                {
                    File.Delete(Settings.INSTALLPATH);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        // kill old process running at destination path
                        Process[] foundProcesses =
                            Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Settings.INSTALLPATH));
                        int myPid = Process.GetCurrentProcess().Id;
                        foreach (var prc in foundProcesses)
                        {
                            // dont kill own process
                            if (prc.Id == myPid) continue;
                            // only kill the process at the destination path
                            if (prc.GetMainModuleFileName() != Settings.INSTALLPATH) continue;
                            prc.Kill();
                            Thread.Sleep(2000);
                            break;
                        }
                    }
                }
            }

            File.Copy(Application.ExecutablePath, Settings.INSTALLPATH, true);

            ApplySettings();

            FileHelper.DeleteZoneIdentifier(Settings.INSTALLPATH);

            //start file
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = Settings.INSTALLPATH
            };
            Process.Start(startInfo);
        }
    }
}
