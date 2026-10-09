using Orion.Common.Messages;
using Orion.Common.Messages.Other;
using Orion.Common.Messages.UserSupport.MessageBox;
using Orion.Common.Messages.UserSupport.RemoteChat;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using Orion.Server.Messages;
using Orion.Server.Networking;
using Orion.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRemoteChat : Form
    {
        private readonly Client _connectClient;

        private readonly RemoteChatHandler chatHandler;
        private static readonly Dictionary<Client, FrmRemoteChat> OpenedForms = new Dictionary<Client, FrmRemoteChat>();

        public static FrmRemoteChat CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmRemoteChat r = new FrmRemoteChat(client);
            r.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, r);
            return r;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }
        public FrmRemoteChat(Client client)
        {
            _connectClient = client;
            chatHandler = new RemoteChatHandler(client);
            RegisterMessageHandler();
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ApplyHighDpiImages();
            message.KeyDown += new KeyEventHandler(message_KeyDown); // Subscribe to the KeyDown event
        }

        private void ApplyHighDpiImages()
        {
            float scale = DpiImageScaling.GetScaleFactor(this);
            DpiImageScaling.ApplyToIconButton(button2, scale);
            DpiImageScaling.ApplyToIconButton(button3, scale);
            DpiImageScaling.ApplyToIconButton(button4, scale);
        }


        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            chatHandler.PacketsRetrieved += AddMessageClient;
            MessageHandler.Register(chatHandler);
        }
        
        private void UnregisterMessageHandler()
        {
            try
            {
                MessageHandler.Unregister(chatHandler);
                chatHandler.PacketsRetrieved -= AddMessageClient;
                _connectClient.ClientState -= ClientDisconnected;
            }
            catch (Exception)
            {
                // Ignore exceptions during cleanup
            }
        }

        private void FrmRemoteChat_Load(object sender, EventArgs e)
        {

            this.Text = WindowHelper.GetWindowTitle("远程聊天 | ", _connectClient);
        }
        
        private void FrmRemoteChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            chatHandler.KillForm();
            UnregisterMessageHandler();
        }

        public void AddMessageClient(object sender, string message)
        {
            if (this.IsHandleCreated && !this.Disposing && !this.IsDisposed)
            {
                Chatlog.Invoke((MethodInvoker)delegate
                {
                    Chatlog.AppendText(string.Format("{0} {1}: {2}{3}", DateTime.Now.ToString("HH:mm:ss"), "客户端", message, Environment.NewLine));
                });
            }
        }

        public void AddMessageServer(object sender, string message)
        {
            Chatlog.Invoke((MethodInvoker)delegate
            {
                Chatlog.AppendText(string.Format("{0} {1}: {2}{3}", DateTime.Now.ToString("HH:mm:ss"), sender, message, Environment.NewLine));
            });
        }

        private void Sendpacket_Click(object sender, EventArgs e)
        {
            if (message.Text.Trim() != "")
            {

                chatHandler.SendMessageClient(NameTB.Text, message.Text.Trim());

                AddMessageServer(NameTB.Text, message.Text.Trim());
                message.Text = "";
            }
        }

        private void SendBTN_Click(object sender, EventArgs e)
        {
            if (message.Text.Trim() != "")
            {

                chatHandler.SendMessageClient(NameTB.Text, message.Text.Trim());

                AddMessageServer(NameTB.Text, message.Text.Trim());
                message.Text = "";
            }
        }

        private void message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; 
                Sendpacket_Click(this, new EventArgs()); 
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chatHandler.StartForm(ChatTitleTB.Text, WelcomeMsgRB.Text, TopMostChk.Checked, DisableCloseChk.Checked, DisableTypeChk.Checked);
            noButtonTabControl1.SelectedIndex = 1;
            NameTB.Text = textBox1.Text;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            chatHandler.KillForm();
            noButtonTabControl1.SelectedIndex = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new DoChatAction());
            Chatlog.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.Filter = "文本文件 (*.txt)|*.txt";
            saveFileDialog.FileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                System.IO.File.WriteAllText(filePath, Chatlog.Text);
            }
        }

    }
}
