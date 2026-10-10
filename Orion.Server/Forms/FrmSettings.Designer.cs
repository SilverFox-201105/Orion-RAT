namespace Orion.Server.Forms
{
    partial class FrmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            btnSave = new System.Windows.Forms.Button();
            lblPort = new System.Windows.Forms.Label();
            ncPort = new System.Windows.Forms.NumericUpDown();
            chkAutoListen = new System.Windows.Forms.CheckBox();
            chkPopup = new System.Windows.Forms.CheckBox();
            btnListen = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            chkIPv6Support = new System.Windows.Forms.CheckBox();
            chkEventLog = new System.Windows.Forms.CheckBox();
            chkTelegramNotis = new System.Windows.Forms.CheckBox();
            txtTelegramToken = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            txtTelegramChatID = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            button1 = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            BlockedRichTB = new System.Windows.Forms.RichTextBox();
            lblMultiPorts = new System.Windows.Forms.Label();
            txtMultiPorts = new System.Windows.Forms.TextBox();
            chkShowCountryGroups = new System.Windows.Forms.CheckBox();
            chkShowTagGroups = new System.Windows.Forms.CheckBox();
            grpFrp = new System.Windows.Forms.GroupBox();
            rtbFrpcConsole = new System.Windows.Forms.RichTextBox();
            lblFrpConsole = new System.Windows.Forms.Label();
            btnFrpToggle = new System.Windows.Forms.Button();
            lblFrpStatus = new System.Windows.Forms.Label();
            cboFrpFormat = new System.Windows.Forms.ComboBox();
            lblFrpFormat = new System.Windows.Forms.Label();
            btnFrpInstall = new System.Windows.Forms.Button();
            lblFrpcBinary = new System.Windows.Forms.Label();
            txtFrpConfig = new System.Windows.Forms.TextBox();
            lblFrpConfig = new System.Windows.Forms.Label();
            chkFrpEnabled = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)ncPort).BeginInit();
            groupBox1.SuspendLayout();
            grpFrp.SuspendLayout();
            SuspendLayout();
            // 
            // btnSave
            // 
            btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnSave.Location = new System.Drawing.Point(227, 545);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(90, 23);
            btnSave.TabIndex = 19;
            btnSave.Text = "保存(&S)";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // lblPort
            // 
            lblPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(12, 11);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(78, 13);
            lblPort.TabIndex = 0;
            lblPort.Text = "要添加的端口:";
            lblPort.Visible = false;
            // 
            // ncPort
            // 
            ncPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ncPort.Enabled = false;
            ncPort.Location = new System.Drawing.Point(111, 7);
            ncPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            ncPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            ncPort.Name = "ncPort";
            ncPort.Size = new System.Drawing.Size(75, 23);
            ncPort.TabIndex = 1;
            ncPort.Value = new decimal(new int[] { 1, 0, 0, 0 });
            ncPort.Visible = false;
            // 
            // chkAutoListen
            // 
            chkAutoListen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkAutoListen.AutoSize = true;
            chkAutoListen.Location = new System.Drawing.Point(11, 70);
            chkAutoListen.Name = "chkAutoListen";
            chkAutoListen.Size = new System.Drawing.Size(130, 17);
            chkAutoListen.TabIndex = 6;
            chkAutoListen.Text = "启动时监听新连接";
            chkAutoListen.UseVisualStyleBackColor = true;
            // 
            // chkPopup
            // 
            chkPopup.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkPopup.AutoSize = true;
            chkPopup.Location = new System.Drawing.Point(11, 116);
            chkPopup.Name = "chkPopup";
            chkPopup.Size = new System.Drawing.Size(156, 17);
            chkPopup.TabIndex = 7;
            chkPopup.Text = "新连接时显示弹出通知";
            chkPopup.UseVisualStyleBackColor = true;
            // 
            // btnListen
            // 
            btnListen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnListen.Location = new System.Drawing.Point(11, 232);
            btnListen.Name = "btnListen";
            btnListen.Size = new System.Drawing.Size(305, 23);
            btnListen.TabIndex = 2;
            btnListen.Text = "开始监听";
            btnListen.UseVisualStyleBackColor = true;
            btnListen.Click += btnListen_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnCancel.Location = new System.Drawing.Point(12, 545);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(90, 23);
            btnCancel.TabIndex = 18;
            btnCancel.Text = "取消(&C)";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // chkIPv6Support
            // 
            chkIPv6Support.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkIPv6Support.AutoSize = true;
            chkIPv6Support.Location = new System.Drawing.Point(11, 93);
            chkIPv6Support.Name = "chkIPv6Support";
            chkIPv6Support.Size = new System.Drawing.Size(104, 17);
            chkIPv6Support.TabIndex = 5;
            chkIPv6Support.Text = "启用 IPv6 支持";
            chkIPv6Support.UseVisualStyleBackColor = true;
            // 
            // chkEventLog
            // 
            chkEventLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkEventLog.AutoSize = true;
            chkEventLog.Location = new System.Drawing.Point(11, 139);
            chkEventLog.Name = "chkEventLog";
            chkEventLog.Size = new System.Drawing.Size(169, 17);
            chkEventLog.TabIndex = 21;
            chkEventLog.Text = "显示事件日志和调试日志";
            chkEventLog.UseVisualStyleBackColor = true;
            // 
            // chkTelegramNotis
            // 
            chkTelegramNotis.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkTelegramNotis.AutoSize = true;
            chkTelegramNotis.Location = new System.Drawing.Point(11, 260);
            chkTelegramNotis.Name = "chkTelegramNotis";
            chkTelegramNotis.Size = new System.Drawing.Size(129, 17);
            chkTelegramNotis.TabIndex = 23;
            chkTelegramNotis.Text = "启用 Telegram 通知";
            chkTelegramNotis.UseVisualStyleBackColor = true;
            chkTelegramNotis.CheckedChanged += chkTelegramNotis_CheckedChanged;
            // 
            // txtTelegramToken
            // 
            txtTelegramToken.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtTelegramToken.Enabled = false;
            txtTelegramToken.Location = new System.Drawing.Point(70, 296);
            txtTelegramToken.Name = "txtTelegramToken";
            txtTelegramToken.Size = new System.Drawing.Size(247, 22);
            txtTelegramToken.TabIndex = 24;
            txtTelegramToken.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Enabled = false;
            label1.Location = new System.Drawing.Point(28, 299);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(39, 13);
            label1.TabIndex = 25;
            label1.Text = "令牌: ";
            // 
            // txtTelegramChatID
            // 
            txtTelegramChatID.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtTelegramChatID.Enabled = false;
            txtTelegramChatID.Location = new System.Drawing.Point(70, 328);
            txtTelegramChatID.Name = "txtTelegramChatID";
            txtTelegramChatID.Size = new System.Drawing.Size(247, 22);
            txtTelegramChatID.TabIndex = 26;
            // 
            // label2
            // 
            label2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Enabled = false;
            label2.Location = new System.Drawing.Point(20, 331);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(47, 13);
            label2.TabIndex = 27;
            label2.Text = "聊天ID:";
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            button1.Location = new System.Drawing.Point(191, 356);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(125, 23);
            button1.TabIndex = 28;
            button1.Text = "测试";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(BlockedRichTB);
            groupBox1.Location = new System.Drawing.Point(12, 385);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(8, 6, 8, 8);
            groupBox1.Size = new System.Drawing.Size(305, 143);
            groupBox1.TabIndex = 30;
            groupBox1.TabStop = false;
            groupBox1.Text = "已屏蔽的 IP（每行一个）";
            // 
            // BlockedRichTB
            // 
            BlockedRichTB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            BlockedRichTB.Dock = System.Windows.Forms.DockStyle.Fill;
            BlockedRichTB.Location = new System.Drawing.Point(8, 21);
            BlockedRichTB.Name = "BlockedRichTB";
            BlockedRichTB.Size = new System.Drawing.Size(289, 114);
            BlockedRichTB.TabIndex = 0;
            BlockedRichTB.Text = "";
            // 
            // lblMultiPorts
            // 
            lblMultiPorts.AutoSize = true;
            lblMultiPorts.Location = new System.Drawing.Point(11, 39);
            lblMultiPorts.Name = "lblMultiPorts";
            lblMultiPorts.Size = new System.Drawing.Size(88, 13);
            lblMultiPorts.TabIndex = 20;
            lblMultiPorts.Text = "要监听的端口:";
            // 
            // txtMultiPorts
            // 
            txtMultiPorts.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            txtMultiPorts.Location = new System.Drawing.Point(110, 36);
            txtMultiPorts.Name = "txtMultiPorts";
            txtMultiPorts.Size = new System.Drawing.Size(206, 22);
            txtMultiPorts.TabIndex = 21;
            txtMultiPorts.Text = "端口1 端口2 等..";
            txtMultiPorts.Enter += txtMultiPorts_Enter;
            txtMultiPorts.KeyPress += txtMultiPorts_KeyPress;
            txtMultiPorts.Leave += txtMultiPorts_Leave;
            // 
            // chkShowCountryGroups
            // 
            chkShowCountryGroups.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkShowCountryGroups.AutoSize = true;
            chkShowCountryGroups.Checked = true;
            chkShowCountryGroups.CheckState = System.Windows.Forms.CheckState.Checked;
            chkShowCountryGroups.Location = new System.Drawing.Point(11, 175);
            chkShowCountryGroups.Name = "chkShowCountryGroups";
            chkShowCountryGroups.Size = new System.Drawing.Size(143, 17);
            chkShowCountryGroups.TabIndex = 10;
            chkShowCountryGroups.Text = "按国家对客户端分组";
            chkShowCountryGroups.UseVisualStyleBackColor = true;
            chkShowCountryGroups.CheckedChanged += chkShowCountryGroups_CheckedChanged;
            // 
            // chkShowTagGroups
            // 
            chkShowTagGroups.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkShowTagGroups.AutoSize = true;
            chkShowTagGroups.Location = new System.Drawing.Point(11, 198);
            chkShowTagGroups.Name = "chkShowTagGroups";
            chkShowTagGroups.Size = new System.Drawing.Size(143, 17);
            chkShowTagGroups.TabIndex = 11;
            chkShowTagGroups.Text = "按标签对客户端分组";
            chkShowTagGroups.UseVisualStyleBackColor = true;
            chkShowTagGroups.CheckedChanged += chkShowTagGroups_CheckedChanged;
            // 
            // grpFrp
            // 
            grpFrp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            grpFrp.Controls.Add(rtbFrpcConsole);
            grpFrp.Controls.Add(lblFrpConsole);
            grpFrp.Controls.Add(btnFrpToggle);
            grpFrp.Controls.Add(lblFrpStatus);
            grpFrp.Controls.Add(cboFrpFormat);
            grpFrp.Controls.Add(lblFrpFormat);
            grpFrp.Controls.Add(btnFrpInstall);
            grpFrp.Controls.Add(lblFrpcBinary);
            grpFrp.Controls.Add(txtFrpConfig);
            grpFrp.Controls.Add(lblFrpConfig);
            grpFrp.Controls.Add(chkFrpEnabled);
            grpFrp.Location = new System.Drawing.Point(340, 12);
            grpFrp.Name = "grpFrp";
            grpFrp.Size = new System.Drawing.Size(308, 560);
            grpFrp.TabIndex = 40;
            grpFrp.TabStop = false;
            grpFrp.Text = "C2 隧道 (FRP)";
            // 
            // rtbFrpcConsole
            // 
            rtbFrpcConsole.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtbFrpcConsole.BackColor = System.Drawing.Color.Black;
            rtbFrpcConsole.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            rtbFrpcConsole.ForeColor = System.Drawing.Color.LimeGreen;
            rtbFrpcConsole.Location = new System.Drawing.Point(12, 460);
            rtbFrpcConsole.Name = "rtbFrpcConsole";
            rtbFrpcConsole.ReadOnly = true;
            rtbFrpcConsole.Size = new System.Drawing.Size(284, 68);
            rtbFrpcConsole.TabIndex = 14;
            rtbFrpcConsole.Text = "";
            rtbFrpcConsole.WordWrap = false;
            // 
            // lblFrpConsole
            // 
            lblFrpConsole.AutoSize = true;
            lblFrpConsole.Location = new System.Drawing.Point(12, 442);
            lblFrpConsole.Name = "lblFrpConsole";
            lblFrpConsole.Size = new System.Drawing.Size(104, 13);
            lblFrpConsole.TabIndex = 13;
            lblFrpConsole.Text = "FRPC 控制台输出:";
            // 
            // btnFrpToggle
            // 
            btnFrpToggle.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnFrpToggle.Location = new System.Drawing.Point(12, 408);
            btnFrpToggle.Name = "btnFrpToggle";
            btnFrpToggle.Size = new System.Drawing.Size(284, 23);
            btnFrpToggle.TabIndex = 12;
            btnFrpToggle.Text = "连接";
            btnFrpToggle.UseVisualStyleBackColor = true;
            btnFrpToggle.Click += btnFrpToggle_Click;
            // 
            // lblFrpStatus
            // 
            lblFrpStatus.AutoSize = true;
            lblFrpStatus.Location = new System.Drawing.Point(12, 386);
            lblFrpStatus.Name = "lblFrpStatus";
            lblFrpStatus.Size = new System.Drawing.Size(78, 13);
            lblFrpStatus.TabIndex = 11;
            lblFrpStatus.Text = "状态: 未连接";
            // 
            // cboFrpFormat
            // 
            cboFrpFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboFrpFormat.FormattingEnabled = true;
            cboFrpFormat.Items.AddRange(new object[] { "自动", "TOML", "INI" });
            cboFrpFormat.Location = new System.Drawing.Point(78, 94);
            cboFrpFormat.Name = "cboFrpFormat";
            cboFrpFormat.Size = new System.Drawing.Size(90, 21);
            cboFrpFormat.TabIndex = 44;
            cboFrpFormat.SelectedIndexChanged += cboFrpFormat_SelectedIndexChanged;
            // 
            // lblFrpFormat
            // 
            lblFrpFormat.AutoSize = true;
            lblFrpFormat.Location = new System.Drawing.Point(12, 98);
            lblFrpFormat.Name = "lblFrpFormat";
            lblFrpFormat.Size = new System.Drawing.Size(62, 13);
            lblFrpFormat.TabIndex = 43;
            lblFrpFormat.Text = "配置格式:";
            // 
            // btnFrpInstall
            // 
            btnFrpInstall.Location = new System.Drawing.Point(12, 66);
            btnFrpInstall.Name = "btnFrpInstall";
            btnFrpInstall.Size = new System.Drawing.Size(150, 23);
            btnFrpInstall.TabIndex = 42;
            btnFrpInstall.Text = "安装/更新 frpc.exe";
            btnFrpInstall.UseVisualStyleBackColor = true;
            btnFrpInstall.Click += btnFrpInstall_Click;
            // 
            // lblFrpcBinary
            // 
            lblFrpcBinary.AutoSize = true;
            lblFrpcBinary.Location = new System.Drawing.Point(12, 48);
            lblFrpcBinary.Name = "lblFrpcBinary";
            lblFrpcBinary.Size = new System.Drawing.Size(114, 13);
            lblFrpcBinary.TabIndex = 41;
            lblFrpcBinary.Text = "frpc 客户端: 未安装";
            // 
            // txtFrpConfig
            // 
            txtFrpConfig.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtFrpConfig.Location = new System.Drawing.Point(12, 144);
            txtFrpConfig.Multiline = true;
            txtFrpConfig.Name = "txtFrpConfig";
            txtFrpConfig.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtFrpConfig.Size = new System.Drawing.Size(284, 200);
            txtFrpConfig.TabIndex = 2;
            txtFrpConfig.TextChanged += txtFrpConfig_TextChanged;
            // 
            // lblFrpConfig
            // 
            lblFrpConfig.AutoSize = true;
            lblFrpConfig.Location = new System.Drawing.Point(12, 126);
            lblFrpConfig.Name = "lblFrpConfig";
            lblFrpConfig.Size = new System.Drawing.Size(182, 13);
            lblFrpConfig.TabIndex = 1;
            lblFrpConfig.Text = "frpc 配置文件(粘贴服务商原文):";
            // 
            // chkFrpEnabled
            // 
            chkFrpEnabled.AutoSize = true;
            chkFrpEnabled.Location = new System.Drawing.Point(12, 22);
            chkFrpEnabled.Name = "chkFrpEnabled";
            chkFrpEnabled.Size = new System.Drawing.Size(123, 17);
            chkFrpEnabled.TabIndex = 0;
            chkFrpEnabled.Text = "启用远程FRP隧道";
            chkFrpEnabled.UseVisualStyleBackColor = true;
            chkFrpEnabled.CheckedChanged += chkFrpEnabled_CheckedChanged;
            // 
            // FrmSettings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(660, 605);
            Controls.Add(grpFrp);
            Controls.Add(txtMultiPorts);
            Controls.Add(lblMultiPorts);
            Controls.Add(chkShowCountryGroups);
            Controls.Add(chkShowTagGroups);
            Controls.Add(groupBox1);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(txtTelegramChatID);
            Controls.Add(label1);
            Controls.Add(txtTelegramToken);
            Controls.Add(chkTelegramNotis);
            Controls.Add(chkEventLog);
            Controls.Add(chkIPv6Support);
            Controls.Add(btnCancel);
            Controls.Add(btnListen);
            Controls.Add(chkPopup);
            Controls.Add(chkAutoListen);
            Controls.Add(btnSave);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmSettings";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "设置";
            Load += FrmSettings_Load;
            ((System.ComponentModel.ISupportInitialize)ncPort).EndInit();
            groupBox1.ResumeLayout(false);
            grpFrp.ResumeLayout(false);
            grpFrp.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown ncPort;
        private System.Windows.Forms.CheckBox chkAutoListen;
        private System.Windows.Forms.CheckBox chkPopup;
        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkIPv6Support;
        private System.Windows.Forms.CheckBox chkEventLog;
        private System.Windows.Forms.CheckBox chkTelegramNotis;
        private System.Windows.Forms.TextBox txtTelegramToken;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTelegramChatID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox BlockedRichTB;
        private System.Windows.Forms.Label lblMultiPorts;
        private System.Windows.Forms.TextBox txtMultiPorts;
        private System.Windows.Forms.CheckBox chkShowCountryGroups;
        private System.Windows.Forms.CheckBox chkShowTagGroups;
        private System.Windows.Forms.GroupBox grpFrp;
        private System.Windows.Forms.CheckBox chkFrpEnabled;
        private System.Windows.Forms.Label lblFrpConfig;
        private System.Windows.Forms.TextBox txtFrpConfig;
        private System.Windows.Forms.Label lblFrpStatus;
        private System.Windows.Forms.Button btnFrpToggle;
        private System.Windows.Forms.Label lblFrpConsole;
        private System.Windows.Forms.RichTextBox rtbFrpcConsole;
        private System.Windows.Forms.Label lblFrpcBinary;
        private System.Windows.Forms.Button btnFrpInstall;
        private System.Windows.Forms.Label lblFrpFormat;
        private System.Windows.Forms.ComboBox cboFrpFormat;
    }
}