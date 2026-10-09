using Orion.Common.Enums;
using System;
using System.Security.Principal;

namespace Orion.Client.User
{
    public class UserAccount
    {
        public string UserName { get; }

        public AccountType Type { get; }

        public UserAccount()
        {
            try
            {
                UserName = Environment.UserName;
            }
            catch
            {
                UserName = "Unknown";
            }

            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);

                    // SYSTEM 账户（NT AUTHORITY\SYSTEM, SID S-1-5-18）优先于管理员判定：
                    // SYSTEM 同时属于 Administrators 组，必须最先识别，否则会被误判为 Admin。
                    if (identity.User != null && identity.User.Value == "S-1-5-18")
                    {
                        Type = AccountType.System;
                    }
                    else if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        Type = AccountType.Admin;
                    }
                    else if (principal.IsInRole(WindowsBuiltInRole.User))
                    {
                        Type = AccountType.User;
                    }
                    else if (principal.IsInRole(WindowsBuiltInRole.Guest))
                    {
                        Type = AccountType.Guest;
                    }
                    else
                    {
                        Type = AccountType.Unknown;
                    }
                }
            }
            catch
            {
                Type = AccountType.Unknown;
            }
        }
    }
}
