using Orion.Client.Config;
using Orion.Client.IO;
using Orion.Common.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Orion.Client.Setup
{
    public class ClientUpdater : ClientSetupBase
    {
        public void Update(string newFilePath)
        {
            FileHelper.DeleteZoneIdentifier(newFilePath);

            var bytes = File.ReadAllBytes(newFilePath);
            if (!FileHelper.HasExecutableIdentifier(bytes))
                throw new Exception("No executable file.");

            string batchFile = BatchFile.CreateUpdateBatch(Application.ExecutablePath, newFilePath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                FileName = batchFile
            };
            Process.Start(startInfo);

            if (Settings.STARTUP)
            {
                var clientStartup = new ClientStartup();
                clientStartup.RemoveFromStartup(Settings.STARTUPKEY);
            }
        }
    }
}
