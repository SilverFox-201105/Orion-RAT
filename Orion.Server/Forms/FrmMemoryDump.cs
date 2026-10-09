using Orion.Common.Messages;
using Orion.Common.Messages.Administration.TaskManager;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using Orion.Server.Messages;
using Orion.Server.Networking;
using Orion.Server.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmMemoryDump: Form
    {
        /// <summary>
        /// The client which can be used for the memory dump.
        /// </summary>
        private readonly Client _connectClient;

        private readonly DoProcessDumpResponse _dumpedProcess;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly MemoryDumpHandler _dumpHandler;

        /// <summary>
        /// Holds the opened memory dump form for each dump.
        /// </summary>
        private static readonly Dictionary<DoProcessDumpResponse, KeyValuePair<Client, FrmMemoryDump>> OpenedForms = new Dictionary<DoProcessDumpResponse, KeyValuePair<Client, FrmMemoryDump>>();

        /// <summary>
        /// Creates a new memory dump form for the dump or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the memory dump form.</param>
        /// <param name="dump">The dump associated with this form</param>
        /// <returns>
        /// Returns a new memory dump form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmMemoryDump CreateNewOrGetExisting(Client client, DoProcessDumpResponse dump) // Check this
        {
            if (OpenedForms.ContainsKey(dump))
            {
                return OpenedForms[dump].Value;
            }
            FrmMemoryDump f = new FrmMemoryDump(client, dump);
            f.Disposed += (sender, args) => OpenedForms.Remove(dump);
            OpenedForms.Add(dump, new KeyValuePair<Client, FrmMemoryDump>(client, f));
            return f;
        }
        public FrmMemoryDump(Client client, DoProcessDumpResponse dump)
        {
            _connectClient = client;
            _dumpHandler = new MemoryDumpHandler(client, dump);
            _dumpedProcess = dump;

            InitializeComponent();
            RegisterMessageHandler();

            progressDownload.Maximum = (int)dump.Length;
            progressDownload.Minimum = 0;

            DarkModeManager.ApplyDarkMode(this);
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _dumpHandler.FileTransferUpdated += FileTransferUpdated;
            MessageHandler.Register(_dumpHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_dumpHandler);
            _dumpHandler.FileTransferUpdated -= FileTransferUpdated;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        private void FileTransferUpdated(object sender, FileTransfer transfer)
        {
            if (transfer.RemotePath == _dumpedProcess.DumpPath)
            {
                if (transfer.Status == "Completed")
                {
                    _dumpHandler.Cleanup(transfer);
                    this.Close();
                }
                if (progressDownload.InvokeRequired)
                {
                    progressDownload.BeginInvoke((MethodInvoker)delegate
                    {
                        progressDownload.Value = (int)transfer.TransferredSize;
                    });
                }
                else
                {
                    progressDownload.Value = (int)transfer.TransferredSize;
                }
                if (labelProgress.InvokeRequired)
                {
                    labelProgress.BeginInvoke((MethodInvoker)delegate
                    {
                        labelProgress.Text = $"进度：{Math.Round((double)(transfer.TransferredSize / this._dumpedProcess.Length), 2)}%";
                    });
                }
                else
                {
                    labelProgress.Text = $"进度：{Math.Round((double)(transfer.TransferredSize / this._dumpedProcess.Length), 2)}%";
                }
                string localizedStatus = LocalizeStatus(transfer.Status);
                if (labelValue.InvokeRequired)
                {
                    labelValue.BeginInvoke((MethodInvoker)delegate
                    {
                        labelValue.Text = localizedStatus;
                    });
                }
                else
                {
                    labelValue.Text = localizedStatus;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Fix this later
            MessageBox.Show("功能未实现！");
            //_connectClient.Send(new FileTransferCancel { Id = 0, Reason = "User Requested" });
            this.Close();
        }

        private void FrmMemoryDump_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("内存转储", _connectClient) + $" => {_dumpedProcess.Pid} : {_dumpedProcess.ProcessName}";
        }

        private void FrmMemoryDump_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            _dumpHandler.Dispose();
        }

        private void FrmMemoryDump_Shown(object sender, EventArgs e)
        {
            _dumpHandler.BeginDumpDownload(_dumpedProcess);
        }

        private static string LocalizeStatus(string status)
        {
            switch (status)
            {
                case "Completed": return "已完成";
                case "Downloading": return "下载中";
                case "Uploading": return "上传中";
                case "Pending": return "等待中";
                case "Canceled": return "已取消";
                case "Failed": return "失败";
                default: return status;
            }
        }
    }
}
