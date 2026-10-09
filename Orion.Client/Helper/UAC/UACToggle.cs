using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orion.Client.Helper.UAC
{
    public class UACToggle
    {
        public static void EnableUAC()
        {
            try
            {
                Microsoft.Win32.RegistryKey uacKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (uacKey != null)
                {
                    uacKey.SetValue("EnableLUA", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    uacKey.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error enabling UAC: " + ex.Message);
            }
        }

        public static void DisableUAC()
        {
            try
            {
                Microsoft.Win32.RegistryKey uacKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (uacKey != null)
                {
                    uacKey.SetValue("EnableLUA", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    uacKey.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error disabling UAC: " + ex.Message);
            }
        }
    }
}
