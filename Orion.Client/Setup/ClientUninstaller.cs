using Orion.Client.Config;
using Orion.Client.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Orion.Client.Setup
{
    public class ClientUninstaller : ClientSetupBase
    {
        public void Uninstall()
        {
            if (Settings.STARTUP)
            {
                var clientStartup = new ClientStartup();
                clientStartup.RemoveFromStartup(Settings.STARTUPKEY);
            }

            string batchFile = BatchFile.CreateUninstallBatch(Application.ExecutablePath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                FileName = batchFile
            };
            Process.Start(startInfo);
        }
    }
}
