namespace Orion.Server.Forms
{
    partial class FrmRemoteChat
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteChat));
            noButtonTabControl1 = new Orion.Server.Controls.NoButtonTabControl();
            Settings = new System.Windows.Forms.TabPage();
            groupBox3 = new System.Windows.Forms.GroupBox();
            WelcomeMsgRB = new System.Windows.Forms.RichTextBox();
            groupBox4 = new System.Windows.Forms.GroupBox();
            DisableTypeChk = new System.Windows.Forms.CheckBox();
            DisableCloseChk = new System.Windows.Forms.CheckBox();
            TopMostChk = new System.Windows.Forms.CheckBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            ChatTitleTB = new System.Windows.Forms.TextBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            textBox1 = new System.Windows.Forms.TextBox();
            button1 = new System.Windows.Forms.Button();
            Chat = new System.Windows.Forms.TabPage();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            button2 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            NameTB = new System.Windows.Forms.TextBox();
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            SendBTN = new System.Windows.Forms.Button();
            message = new System.Windows.Forms.TextBox();
            Chatlog = new System.Windows.Forms.RichTextBox();
            noButtonTabControl1.SuspendLayout();
            Settings.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            Chat.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // noButtonTabControl1
            // 
            noButtonTabControl1.Controls.Add(Settings);
            noButtonTabControl1.Controls.Add(Chat);
            noButtonTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            noButtonTabControl1.Location = new System.Drawing.Point(0, 0);
            noButtonTabControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            noButtonTabControl1.Name = "noButtonTabControl1";
            noButtonTabControl1.SelectedIndex = 0;
            noButtonTabControl1.Size = new System.Drawing.Size(425, 564);
            noButtonTabControl1.TabIndex = 1;
            // 
            // Settings
            // 
            Settings.Controls.Add(groupBox3);
            Settings.Controls.Add(groupBox4);
            Settings.Controls.Add(groupBox2);
            Settings.Controls.Add(groupBox1);
            Settings.Controls.Add(button1);
            Settings.Location = new System.Drawing.Point(4, 26);
            Settings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Settings.Name = "Settings";
            Settings.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Settings.Size = new System.Drawing.Size(417, 534);
            Settings.TabIndex = 0;
            Settings.Text = "设置";
            Settings.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(WelcomeMsgRB);
            groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            groupBox3.Location = new System.Drawing.Point(4, 114);
            groupBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox3.Size = new System.Drawing.Size(409, 272);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "欢迎消息";
            // 
            // WelcomeMsgRB
            // 
            WelcomeMsgRB.Dock = System.Windows.Forms.DockStyle.Fill;
            WelcomeMsgRB.Location = new System.Drawing.Point(4, 20);
            WelcomeMsgRB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            WelcomeMsgRB.Name = "WelcomeMsgRB";
            WelcomeMsgRB.Size = new System.Drawing.Size(401, 248);
            WelcomeMsgRB.TabIndex = 0;
            WelcomeMsgRB.Text = "你好！\n你当前正在与管理员对话。\n请随时提出任何问题。\n\n";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(DisableTypeChk);
            groupBox4.Controls.Add(DisableCloseChk);
            groupBox4.Controls.Add(TopMostChk);
            groupBox4.Dock = System.Windows.Forms.DockStyle.Bottom;
            groupBox4.Location = new System.Drawing.Point(4, 386);
            groupBox4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox4.Size = new System.Drawing.Size(409, 114);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "附加选项";
            // 
            // DisableTypeChk
            // 
            DisableTypeChk.AutoSize = true;
            DisableTypeChk.Checked = true;
            DisableTypeChk.CheckState = System.Windows.Forms.CheckState.Checked;
            DisableTypeChk.Location = new System.Drawing.Point(7, 55);
            DisableTypeChk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            DisableTypeChk.Name = "DisableTypeChk";
            DisableTypeChk.Size = new System.Drawing.Size(75, 21);
            DisableTypeChk.TabIndex = 2;
            DisableTypeChk.Text = "允许输入";
            DisableTypeChk.UseVisualStyleBackColor = true;
            // 
            // DisableCloseChk
            // 
            DisableCloseChk.AutoSize = true;
            DisableCloseChk.Location = new System.Drawing.Point(7, 85);
            DisableCloseChk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            DisableCloseChk.Name = "DisableCloseChk";
            DisableCloseChk.Size = new System.Drawing.Size(99, 21);
            DisableCloseChk.TabIndex = 1;
            DisableCloseChk.Text = "禁止关闭聊天";
            DisableCloseChk.UseVisualStyleBackColor = true;
            // 
            // TopMostChk
            // 
            TopMostChk.AutoSize = true;
            TopMostChk.Checked = true;
            TopMostChk.CheckState = System.Windows.Forms.CheckState.Checked;
            TopMostChk.Location = new System.Drawing.Point(7, 25);
            TopMostChk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            TopMostChk.Name = "TopMostChk";
            TopMostChk.Size = new System.Drawing.Size(75, 21);
            TopMostChk.TabIndex = 0;
            TopMostChk.Text = "置顶显示";
            TopMostChk.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(ChatTitleTB);
            groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            groupBox2.Location = new System.Drawing.Point(4, 59);
            groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox2.Size = new System.Drawing.Size(409, 55);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "客户端窗口标题";
            // 
            // ChatTitleTB
            // 
            ChatTitleTB.Dock = System.Windows.Forms.DockStyle.Top;
            ChatTitleTB.Location = new System.Drawing.Point(4, 20);
            ChatTitleTB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            ChatTitleTB.Name = "ChatTitleTB";
            ChatTitleTB.Size = new System.Drawing.Size(401, 23);
            ChatTitleTB.TabIndex = 0;
            ChatTitleTB.Text = "正在与管理员聊天";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox1);
            groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            groupBox1.Location = new System.Drawing.Point(4, 4);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            groupBox1.Size = new System.Drawing.Size(409, 55);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "你的名称";
            // 
            // textBox1
            // 
            textBox1.Dock = System.Windows.Forms.DockStyle.Top;
            textBox1.Location = new System.Drawing.Point(4, 20);
            textBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(401, 23);
            textBox1.TabIndex = 0;
            textBox1.Text = "管理员";
            // 
            // button1
            // 
            button1.Dock = System.Windows.Forms.DockStyle.Bottom;
            button1.Location = new System.Drawing.Point(4, 500);
            button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(409, 30);
            button1.TabIndex = 4;
            button1.Text = "开始聊天";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Chat
            // 
            Chat.Controls.Add(tableLayoutPanel1);
            Chat.Location = new System.Drawing.Point(4, 26);
            Chat.Margin = new System.Windows.Forms.Padding(0);
            Chat.Name = "Chat";
            Chat.Size = new System.Drawing.Size(417, 534);
            Chat.TabIndex = 1;
            Chat.Text = "聊天";
            Chat.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 2);
            tableLayoutPanel1.Controls.Add(Chatlog, 0, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            tableLayoutPanel1.Size = new System.Drawing.Size(417, 534);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(button2);
            flowLayoutPanel1.Controls.Add(button3);
            flowLayoutPanel1.Controls.Add(button4);
            flowLayoutPanel1.Controls.Add(NameTB);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(0, 457);
            flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(417, 39);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // button2
            // 
            button2.Image = Properties.Resources.broom;
            button2.Location = new System.Drawing.Point(4, 4);
            button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(27, 30);
            button2.TabIndex = 0;
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Image = Properties.Resources.save;
            button3.Location = new System.Drawing.Point(39, 4);
            button3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(27, 30);
            button3.TabIndex = 1;
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Image = Properties.Resources.shutdown;
            button4.Location = new System.Drawing.Point(74, 4);
            button4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(27, 30);
            button4.TabIndex = 2;
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // NameTB
            // 
            NameTB.Location = new System.Drawing.Point(4, 42);
            NameTB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            NameTB.Name = "NameTB";
            NameTB.Size = new System.Drawing.Size(306, 23);
            NameTB.TabIndex = 4;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 89F));
            tableLayoutPanel2.Controls.Add(SendBTN, 1, 0);
            tableLayoutPanel2.Controls.Add(message, 0, 0);
            tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(0, 496);
            tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            tableLayoutPanel2.Size = new System.Drawing.Size(417, 38);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // SendBTN
            // 
            SendBTN.Dock = System.Windows.Forms.DockStyle.Fill;
            SendBTN.Location = new System.Drawing.Point(332, 4);
            SendBTN.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            SendBTN.Name = "SendBTN";
            SendBTN.Size = new System.Drawing.Size(81, 30);
            SendBTN.TabIndex = 0;
            SendBTN.Text = "发送";
            SendBTN.UseVisualStyleBackColor = true;
            SendBTN.Click += SendBTN_Click;
            // 
            // message
            // 
            message.Dock = System.Windows.Forms.DockStyle.Fill;
            message.Location = new System.Drawing.Point(4, 4);
            message.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            message.Name = "message";
            message.Size = new System.Drawing.Size(320, 23);
            message.TabIndex = 1;
            message.KeyDown += message_KeyDown;
            // 
            // Chatlog
            // 
            Chatlog.Dock = System.Windows.Forms.DockStyle.Fill;
            Chatlog.Location = new System.Drawing.Point(4, 4);
            Chatlog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Chatlog.Name = "Chatlog";
            Chatlog.ReadOnly = true;
            Chatlog.Size = new System.Drawing.Size(409, 449);
            Chatlog.TabIndex = 3;
            Chatlog.Text = "";
            // 
            // FrmRemoteChat
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(425, 564);
            Controls.Add(noButtonTabControl1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            MinimumSize = new System.Drawing.Size(441, 511);
            Name = "FrmRemoteChat";
            Text = "远程聊天 []";
            FormClosing += FrmRemoteChat_FormClosing;
            Load += FrmRemoteChat_Load;
            noButtonTabControl1.ResumeLayout(false);
            Settings.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            Chat.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private Controls.NoButtonTabControl noButtonTabControl1;
        private System.Windows.Forms.TabPage Settings;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RichTextBox WelcomeMsgRB;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox DisableTypeChk;
        private System.Windows.Forms.CheckBox DisableCloseChk;
        private System.Windows.Forms.CheckBox TopMostChk;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox ChatTitleTB;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabPage Chat;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox NameTB;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button SendBTN;
        private System.Windows.Forms.TextBox message;
        private System.Windows.Forms.RichTextBox Chatlog;
    }
}