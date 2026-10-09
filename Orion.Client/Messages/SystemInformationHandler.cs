using Orion.Client.Helper;
using Orion.Client.User;
using Orion.Common.Messages;
using Orion.Common.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using Orion.Client.IO;
using Orion.Common.Messages.Administration.SystemInfo;
using Orion.Common.Messages.Other;

namespace Orion.Client.Messages
{
    public class SystemInformationHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is GetSystemInfo;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetSystemInfo msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, GetSystemInfo message)
        {
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

                var domainName = (!string.IsNullOrEmpty(properties.DomainName)) ? properties.DomainName : "-";
                var hostName = (!string.IsNullOrEmpty(properties.HostName)) ? properties.HostName : "-";

                var userAccount = new UserAccount();
                string defaultBrowser = SystemHelper.GetDefaultBrowser();

                // The agent no longer performs an external IP geolocation lookup; the operator already
                // sees the country in the client list (resolved server-side from the connection IP).
                List<Tuple<string, string>> lstInfos = new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("处理器 (CPU)", HardwareDevices.CpuName),
                    new Tuple<string, string>("内存 (RAM)", $"{HardwareDevices.TotalPhysicalMemory} MB"),
                    new Tuple<string, string>("显卡 (GPU)", HardwareDevices.GpuNames),
                    new Tuple<string, string>("用户名", userAccount.UserName),
                    new Tuple<string, string>("计算机名", SystemHelper.GetPcName()),
                    new Tuple<string, string>("域名", domainName),
                    new Tuple<string, string>("主机名", hostName),
                    new Tuple<string, string>("系统盘", Path.GetPathRoot(Environment.SystemDirectory)),
                    new Tuple<string, string>("系统目录", Environment.SystemDirectory),
                    new Tuple<string, string>("运行时间", SystemHelper.GetUptime()),
                    new Tuple<string, string>("MAC 地址", HardwareDevices.MacAddress),
                    new Tuple<string, string>("局域网 IP", HardwareDevices.LanIpAddress),
                    new Tuple<string, string>("时区", TimeZoneInfo.Local.DisplayName),
                    new Tuple<string, string>("杀毒软件", SystemHelper.GetAntivirus()),
                    new Tuple<string, string>("防火墙", SystemHelper.GetFirewall()),
                    new Tuple<string, string>("默认浏览器", defaultBrowser)
                };

                client.Send(new GetSystemInfoResponse { SystemInfos = lstInfos });
            }
            catch
            {
            }
        }
    }
}
