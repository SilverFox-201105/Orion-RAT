using Orion.Server.Controls;

namespace Orion.Server.Forms
{
    partial class FrmBuilder
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

                if (portNotificationTimer != null)
                {
                    portNotificationTimer.Stop();
                    portNotificationTimer.Dispose();
                }

                if (portSetDelayTimer != null)
                {
                    portSetDelayTimer.Stop();
                    portSetDelayTimer.Dispose();
                }

                if (iconPreview?.Image != null)
                {
                    iconPreview.Image.Dispose();
                    iconPreview.Image = null;
                }
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmBuilder));
            btnBuild = new System.Windows.Forms.Button();
            tooltip = new System.Windows.Forms.ToolTip(components);
            picUAC2 = new System.Windows.Forms.PictureBox();
            picUAC1 = new System.Windows.Forms.PictureBox();
            rbSystem = new System.Windows.Forms.RadioButton();
            rbProgramFiles = new System.Windows.Forms.RadioButton();
            rbX86 = new System.Windows.Forms.RadioButton();
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            removeHostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            builderTabs = new DotNetBarTabControl();
            generalPage = new System.Windows.Forms.TabPage();
            groupBox2 = new System.Windows.Forms.GroupBox();
            rbX64 = new System.Windows.Forms.RadioButton();
            lblTargetArch = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            line6 = new Line();
            label8 = new System.Windows.Forms.Label();
            txtTag = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            lblTag = new System.Windows.Forms.Label();
            txtMutex = new System.Windows.Forms.TextBox();
            btnMutex = new System.Windows.Forms.Button();
            line5 = new Line();
            lblMutex = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            connectionPage = new System.Windows.Forms.TabPage();
            line11 = new Line();
            checkBox1 = new System.Windows.Forms.CheckBox();
            label13 = new System.Windows.Forms.Label();
            txtPastebin = new System.Windows.Forms.TextBox();
            numericUpDownPort = new System.Windows.Forms.NumericUpDown();
            numericUpDownDelay = new System.Windows.Forms.NumericUpDown();
            line3 = new Line();
            label4 = new System.Windows.Forms.Label();
            line1 = new Line();
            label1 = new System.Windows.Forms.Label();
            lstHosts = new System.Windows.Forms.ListBox();
            btnAddHost = new System.Windows.Forms.Button();
            lblMS = new System.Windows.Forms.Label();
            lblHost = new System.Windows.Forms.Label();
            txtHost = new System.Windows.Forms.TextBox();
            lblDelay = new System.Windows.Forms.Label();
            lblPort = new System.Windows.Forms.Label();
            installationPage = new System.Windows.Forms.TabPage();
            line7 = new Line();
            label10 = new System.Windows.Forms.Label();
            line4 = new Line();
            label5 = new System.Windows.Forms.Label();
            chkInstall = new System.Windows.Forms.CheckBox();
            lblInstallName = new System.Windows.Forms.Label();
            txtInstallName = new System.Windows.Forms.TextBox();
            txtRegistryKeyName = new System.Windows.Forms.TextBox();
            lblExtension = new System.Windows.Forms.Label();
            lblRegistryKeyName = new System.Windows.Forms.Label();
            chkStartup = new System.Windows.Forms.CheckBox();
            rbAppdata = new System.Windows.Forms.RadioButton();
            lblInstallDirectory = new System.Windows.Forms.Label();
            lblInstallSubDirectory = new System.Windows.Forms.Label();
            lblPreviewPath = new System.Windows.Forms.Label();
            txtInstallSubDirectory = new System.Windows.Forms.TextBox();
            txtPreviewPath = new System.Windows.Forms.TextBox();
            assemblyPage = new System.Windows.Forms.TabPage();
            btnClone = new System.Windows.Forms.Button();
            iconPreview = new System.Windows.Forms.PictureBox();
            btnBrowseIcon = new System.Windows.Forms.Button();
            txtIconPath = new System.Windows.Forms.TextBox();
            line8 = new Line();
            label11 = new System.Windows.Forms.Label();
            chkChangeAsmInfo = new System.Windows.Forms.CheckBox();
            txtFileVersion = new System.Windows.Forms.TextBox();
            line9 = new Line();
            lblProductName = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            chkChangeIcon = new System.Windows.Forms.CheckBox();
            lblFileVersion = new System.Windows.Forms.Label();
            txtProductName = new System.Windows.Forms.TextBox();
            txtProductVersion = new System.Windows.Forms.TextBox();
            lblDescription = new System.Windows.Forms.Label();
            lblProductVersion = new System.Windows.Forms.Label();
            txtDescription = new System.Windows.Forms.TextBox();
            txtOriginalFilename = new System.Windows.Forms.TextBox();
            lblCompanyName = new System.Windows.Forms.Label();
            lblOriginalFilename = new System.Windows.Forms.Label();
            txtCompanyName = new System.Windows.Forms.TextBox();
            txtTrademarks = new System.Windows.Forms.TextBox();
            lblCopyright = new System.Windows.Forms.Label();
            lblTrademarks = new System.Windows.Forms.Label();
            txtCopyright = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)picUAC2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picUAC1).BeginInit();
            contextMenuStrip.SuspendLayout();
            builderTabs.SuspendLayout();
            generalPage.SuspendLayout();
            groupBox2.SuspendLayout();
            connectionPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownDelay).BeginInit();
            installationPage.SuspendLayout();
            assemblyPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)iconPreview).BeginInit();
            SuspendLayout();
            // 
            // btnBuild
            // 
            btnBuild.Location = new System.Drawing.Point(402, 390);
            btnBuild.Name = "btnBuild";
            btnBuild.Size = new System.Drawing.Size(121, 23);
            btnBuild.TabIndex = 1;
            btnBuild.Text = "生成客户端";
            btnBuild.UseVisualStyleBackColor = true;
            btnBuild.Click += btnBuild_Click;
            // 
            // picUAC2
            // 
            picUAC2.Image = Properties.Resources.uac_shield;
            picUAC2.Location = new System.Drawing.Point(363, 88);
            picUAC2.Name = "picUAC2";
            picUAC2.Size = new System.Drawing.Size(16, 20);
            picUAC2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picUAC2.TabIndex = 32;
            picUAC2.TabStop = false;
            tooltip.SetToolTip(picUAC2, "在 System 目录安装客户端需要管理员权限。");
            // 
            // picUAC1
            // 
            picUAC1.Image = Properties.Resources.uac_shield;
            picUAC1.Location = new System.Drawing.Point(363, 68);
            picUAC1.Name = "picUAC1";
            picUAC1.Size = new System.Drawing.Size(16, 20);
            picUAC1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picUAC1.TabIndex = 31;
            picUAC1.TabStop = false;
            tooltip.SetToolTip(picUAC1, "在 Program Files 目录安装客户端需要管理员权限。");
            // 
            // rbSystem
            // 
            rbSystem.AutoSize = true;
            rbSystem.Location = new System.Drawing.Point(241, 91);
            rbSystem.Name = "rbSystem";
            rbSystem.Size = new System.Drawing.Size(89, 17);
            rbSystem.TabIndex = 5;
            rbSystem.TabStop = true;
            rbSystem.Text = "System 目录";
            tooltip.SetToolTip(rbSystem, "在 System 目录安装客户端需要管理员权限。");
            rbSystem.UseVisualStyleBackColor = true;
            rbSystem.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // rbProgramFiles
            // 
            rbProgramFiles.AutoSize = true;
            rbProgramFiles.Location = new System.Drawing.Point(241, 68);
            rbProgramFiles.Name = "rbProgramFiles";
            rbProgramFiles.Size = new System.Drawing.Size(123, 17);
            rbProgramFiles.TabIndex = 4;
            rbProgramFiles.TabStop = true;
            rbProgramFiles.Text = "Program Files 目录";
            tooltip.SetToolTip(rbProgramFiles, "在 Program Files 目录安装客户端需要管理员权限。");
            rbProgramFiles.UseVisualStyleBackColor = true;
            rbProgramFiles.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // rbX86
            // 
            rbX86.AutoSize = true;
            rbX86.Checked = false;
            rbX86.Location = new System.Drawing.Point(124, 20);
            rbX86.Name = "rbX86";
            rbX86.Size = new System.Drawing.Size(79, 17);
            rbX86.TabIndex = 1;
            rbX86.TabStop = true;
            rbX86.Text = "32 位 (x86)";
            tooltip.SetToolTip(rbX86, "32 位客户端兼容性最好（可同时在 32/64 位系统运行），但默认已改为 64 位");
            rbX86.UseVisualStyleBackColor = true;
            rbX86.CheckedChanged += HasChangedSetting;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { removeHostToolStripMenuItem, clearToolStripMenuItem });
            contextMenuStrip.Name = "ctxtMenuHosts";
            contextMenuStrip.Size = new System.Drawing.Size(129, 56);
            // 
            // removeHostToolStripMenuItem
            // 
            removeHostToolStripMenuItem.Image = Properties.Resources.delete;
            removeHostToolStripMenuItem.Name = "removeHostToolStripMenuItem";
            removeHostToolStripMenuItem.Size = new System.Drawing.Size(128, 26);
            removeHostToolStripMenuItem.Text = "移除主机";
            removeHostToolStripMenuItem.Click += removeHostToolStripMenuItem_Click;
            // 
            // clearToolStripMenuItem
            // 
            clearToolStripMenuItem.Image = Properties.Resources.broom;
            clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            clearToolStripMenuItem.Size = new System.Drawing.Size(128, 26);
            clearToolStripMenuItem.Text = "清除所有";
            clearToolStripMenuItem.Click += clearToolStripMenuItem_Click;
            // 
            // builderTabs
            // 
            builderTabs.Alignment = System.Windows.Forms.TabAlignment.Left;
            builderTabs.Controls.Add(generalPage);
            builderTabs.Controls.Add(connectionPage);
            builderTabs.Controls.Add(installationPage);
            builderTabs.Controls.Add(assemblyPage);
            builderTabs.Dock = System.Windows.Forms.DockStyle.Top;
            builderTabs.ItemSize = new System.Drawing.Size(44, 136);
            builderTabs.Location = new System.Drawing.Point(0, 0);
            builderTabs.Multiline = true;
            builderTabs.Name = "builderTabs";
            builderTabs.SelectedIndex = 0;
            builderTabs.Size = new System.Drawing.Size(535, 384);
            builderTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            builderTabs.TabIndex = 0;
            builderTabs.SelectedIndexChanged += builderTabs_SelectedIndexChanged;
            // 
            // generalPage
            // 
            generalPage.BackColor = System.Drawing.SystemColors.Control;
            generalPage.Controls.Add(groupBox2);
            generalPage.Controls.Add(label9);
            generalPage.Controls.Add(line6);
            generalPage.Controls.Add(label8);
            generalPage.Controls.Add(txtTag);
            generalPage.Controls.Add(label7);
            generalPage.Controls.Add(lblTag);
            generalPage.Controls.Add(txtMutex);
            generalPage.Controls.Add(btnMutex);
            generalPage.Controls.Add(line5);
            generalPage.Controls.Add(lblMutex);
            generalPage.Controls.Add(label6);
            generalPage.Location = new System.Drawing.Point(140, 4);
            generalPage.Name = "generalPage";
            generalPage.Padding = new System.Windows.Forms.Padding(3);
            generalPage.Size = new System.Drawing.Size(391, 376);
            generalPage.TabIndex = 4;
            generalPage.Text = "基本设置";
            generalPage.Click += generalPage_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(rbX64);
            groupBox2.Controls.Add(rbX86);
            groupBox2.Controls.Add(lblTargetArch);
            groupBox2.Location = new System.Drawing.Point(6, 187);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(379, 55);
            groupBox2.TabIndex = 32;
            groupBox2.TabStop = false;
            groupBox2.Text = "目标平台架构";
            // 
            // rbX64
            // 
            rbX64.AutoSize = true;
            rbX64.Checked = true;
            rbX64.Location = new System.Drawing.Point(259, 20);
            rbX64.Name = "rbX64";
            rbX64.Size = new System.Drawing.Size(79, 17);
            rbX64.TabIndex = 2;
            rbX64.Text = "64 位 (x64)";
            tooltip.SetToolTip(rbX64, "默认选择 64 位：绝大多数目标已是 x64 系统");
            rbX64.UseVisualStyleBackColor = true;
            rbX64.CheckedChanged += HasChangedSetting;
            // 
            // lblTargetArch
            // 
            lblTargetArch.AutoSize = true;
            lblTargetArch.Location = new System.Drawing.Point(10, 22);
            lblTargetArch.Name = "lblTargetArch";
            lblTargetArch.Size = new System.Drawing.Size(75, 13);
            lblTargetArch.TabIndex = 0;
            lblTargetArch.Text = "客户端架构:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.BackColor = System.Drawing.Color.Transparent;
            label9.Location = new System.Drawing.Point(17, 94);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(332, 13);
            label9.TabIndex = 5;
            label9.Text = "唯一的互斥体可确保同一系统上只运行一个客户端实例。";
            // 
            // line6
            // 
            line6.BackColor = System.Drawing.SystemColors.Control;
            line6.Location = new System.Drawing.Point(85, 78);
            line6.Name = "line6";
            line6.Size = new System.Drawing.Size(300, 13);
            line6.TabIndex = 20;
            line6.TabStop = false;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = System.Drawing.Color.Transparent;
            label8.Location = new System.Drawing.Point(6, 78);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(72, 13);
            label8.TabIndex = 4;
            label8.Text = "进程互斥体";
            // 
            // txtTag
            // 
            txtTag.Location = new System.Drawing.Point(130, 40);
            txtTag.Name = "txtTag";
            txtTag.Size = new System.Drawing.Size(255, 22);
            txtTag.TabIndex = 3;
            txtTag.TextChanged += HasChangedSetting;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(17, 20);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(241, 13);
            label7.TabIndex = 1;
            label7.Text = "您可以选择一个标签来标识您的客户端。";
            // 
            // lblTag
            // 
            lblTag.AutoSize = true;
            lblTag.Location = new System.Drawing.Point(17, 43);
            lblTag.Name = "lblTag";
            lblTag.Size = new System.Drawing.Size(75, 13);
            lblTag.TabIndex = 2;
            lblTag.Text = "客户端标签:";
            // 
            // txtMutex
            // 
            txtMutex.Location = new System.Drawing.Point(130, 130);
            txtMutex.MaxLength = 64;
            txtMutex.Name = "txtMutex";
            txtMutex.Size = new System.Drawing.Size(253, 22);
            txtMutex.TabIndex = 7;
            txtMutex.TextChanged += HasChangedSetting;
            // 
            // btnMutex
            // 
            btnMutex.BackColor = System.Drawing.Color.Transparent;
            btnMutex.Location = new System.Drawing.Point(262, 158);
            btnMutex.Name = "btnMutex";
            btnMutex.Size = new System.Drawing.Size(121, 23);
            btnMutex.TabIndex = 8;
            btnMutex.Text = "随机互斥体";
            btnMutex.UseVisualStyleBackColor = false;
            btnMutex.Click += btnMutex_Click;
            // 
            // line5
            // 
            line5.BackColor = System.Drawing.SystemColors.Control;
            line5.Location = new System.Drawing.Point(85, 5);
            line5.Name = "line5";
            line5.Size = new System.Drawing.Size(298, 13);
            line5.TabIndex = 15;
            line5.TabStop = false;
            // 
            // lblMutex
            // 
            lblMutex.AutoSize = true;
            lblMutex.BackColor = System.Drawing.Color.Transparent;
            lblMutex.Location = new System.Drawing.Point(17, 133);
            lblMutex.Name = "lblMutex";
            lblMutex.Size = new System.Drawing.Size(49, 13);
            lblMutex.TabIndex = 6;
            lblMutex.Text = "互斥体:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(6, 5);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(72, 13);
            label6.TabIndex = 0;
            label6.Text = "客户端标识";
            // 
            // connectionPage
            // 
            connectionPage.BackColor = System.Drawing.SystemColors.Control;
            connectionPage.Controls.Add(line11);
            connectionPage.Controls.Add(checkBox1);
            connectionPage.Controls.Add(label13);
            connectionPage.Controls.Add(txtPastebin);
            connectionPage.Controls.Add(numericUpDownPort);
            connectionPage.Controls.Add(numericUpDownDelay);
            connectionPage.Controls.Add(line3);
            connectionPage.Controls.Add(label4);
            connectionPage.Controls.Add(line1);
            connectionPage.Controls.Add(label1);
            connectionPage.Controls.Add(lstHosts);
            connectionPage.Controls.Add(btnAddHost);
            connectionPage.Controls.Add(lblMS);
            connectionPage.Controls.Add(lblHost);
            connectionPage.Controls.Add(txtHost);
            connectionPage.Controls.Add(lblDelay);
            connectionPage.Controls.Add(lblPort);
            connectionPage.Location = new System.Drawing.Point(140, 4);
            connectionPage.Name = "connectionPage";
            connectionPage.Padding = new System.Windows.Forms.Padding(3);
            connectionPage.Size = new System.Drawing.Size(391, 376);
            connectionPage.TabIndex = 0;
            connectionPage.Text = "连接设置";
            connectionPage.Click += connectionPage_Click;
            // 
            // line11
            // 
            line11.BackColor = System.Drawing.SystemColors.Control;
            line11.Location = new System.Drawing.Point(175, 107);
            line11.Name = "line11";
            line11.Size = new System.Drawing.Size(216, 19);
            line11.TabIndex = 22;
            line11.TabStop = false;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new System.Drawing.Point(370, 132);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(15, 14);
            checkBox1.TabIndex = 21;
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(175, 130);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(68, 13);
            label13.TabIndex = 20;
            label13.Text = "动态地址 : ";
            // 
            // txtPastebin
            // 
            txtPastebin.Location = new System.Drawing.Point(254, 126);
            txtPastebin.Name = "txtPastebin";
            txtPastebin.Size = new System.Drawing.Size(110, 22);
            txtPastebin.TabIndex = 19;
            txtPastebin.TextChanged += txtPastebin_TextChanged;
            // 
            // numericUpDownPort
            // 
            numericUpDownPort.Location = new System.Drawing.Point(254, 51);
            numericUpDownPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numericUpDownPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownPort.Name = "numericUpDownPort";
            numericUpDownPort.Size = new System.Drawing.Size(129, 22);
            numericUpDownPort.TabIndex = 3;
            numericUpDownPort.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numericUpDownDelay
            // 
            numericUpDownDelay.Location = new System.Drawing.Point(274, 234);
            numericUpDownDelay.Maximum = new decimal(new int[] { 600000, 0, 0, 0 });
            numericUpDownDelay.Name = "numericUpDownDelay";
            numericUpDownDelay.Size = new System.Drawing.Size(80, 22);
            numericUpDownDelay.TabIndex = 10;
            numericUpDownDelay.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            numericUpDownDelay.ValueChanged += HasChangedSetting;
            // 
            // line3
            // 
            line3.BackColor = System.Drawing.SystemColors.Control;
            line3.Location = new System.Drawing.Point(71, 199);
            line3.Name = "line3";
            line3.Size = new System.Drawing.Size(323, 13);
            line3.TabIndex = 18;
            line3.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(6, 199);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(59, 13);
            label4.TabIndex = 17;
            label4.Text = "重连延迟";
            // 
            // line1
            // 
            line1.BackColor = System.Drawing.SystemColors.Control;
            line1.Location = new System.Drawing.Point(71, 5);
            line1.Name = "line1";
            line1.Size = new System.Drawing.Size(314, 11);
            line1.TabIndex = 13;
            line1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 5);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(59, 13);
            label1.TabIndex = 14;
            label1.Text = "连接主机";
            // 
            // lstHosts
            // 
            lstHosts.ContextMenuStrip = contextMenuStrip;
            lstHosts.FormattingEnabled = true;
            lstHosts.HorizontalScrollbar = true;
            lstHosts.Location = new System.Drawing.Point(20, 21);
            lstHosts.Name = "lstHosts";
            lstHosts.Size = new System.Drawing.Size(149, 108);
            lstHosts.TabIndex = 5;
            lstHosts.TabStop = false;
            // 
            // btnAddHost
            // 
            btnAddHost.Location = new System.Drawing.Point(254, 79);
            btnAddHost.Name = "btnAddHost";
            btnAddHost.Size = new System.Drawing.Size(129, 22);
            btnAddHost.TabIndex = 4;
            btnAddHost.Text = "添加主机";
            btnAddHost.UseVisualStyleBackColor = true;
            btnAddHost.Click += btnAddHost_Click;
            // 
            // lblMS
            // 
            lblMS.AutoSize = true;
            lblMS.Location = new System.Drawing.Point(360, 236);
            lblMS.Name = "lblMS";
            lblMS.Size = new System.Drawing.Size(21, 13);
            lblMS.TabIndex = 11;
            lblMS.Text = "ms";
            // 
            // lblHost
            // 
            lblHost.AutoSize = true;
            lblHost.Location = new System.Drawing.Point(175, 25);
            lblHost.Name = "lblHost";
            lblHost.Size = new System.Drawing.Size(62, 13);
            lblHost.TabIndex = 0;
            lblHost.Text = "IP/主机名:";
            // 
            // txtHost
            // 
            txtHost.Location = new System.Drawing.Point(254, 22);
            txtHost.Name = "txtHost";
            txtHost.Size = new System.Drawing.Size(129, 22);
            txtHost.TabIndex = 1;
            txtHost.TextChanged += txtHost_TextChanged_1;
            // 
            // lblDelay
            // 
            lblDelay.AutoSize = true;
            lblDelay.Location = new System.Drawing.Point(20, 236);
            lblDelay.Name = "lblDelay";
            lblDelay.Size = new System.Drawing.Size(88, 13);
            lblDelay.TabIndex = 9;
            lblDelay.Text = "重连等待时间:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(175, 53);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(36, 13);
            lblPort.TabIndex = 2;
            lblPort.Text = "端口:";
            // 
            // installationPage
            // 
            installationPage.BackColor = System.Drawing.SystemColors.Control;
            installationPage.Controls.Add(line7);
            installationPage.Controls.Add(label10);
            installationPage.Controls.Add(line4);
            installationPage.Controls.Add(label5);
            installationPage.Controls.Add(picUAC2);
            installationPage.Controls.Add(picUAC1);
            installationPage.Controls.Add(chkInstall);
            installationPage.Controls.Add(rbSystem);
            installationPage.Controls.Add(lblInstallName);
            installationPage.Controls.Add(rbProgramFiles);
            installationPage.Controls.Add(txtInstallName);
            installationPage.Controls.Add(txtRegistryKeyName);
            installationPage.Controls.Add(lblExtension);
            installationPage.Controls.Add(lblRegistryKeyName);
            installationPage.Controls.Add(chkStartup);
            installationPage.Controls.Add(rbAppdata);
            installationPage.Controls.Add(lblInstallDirectory);
            installationPage.Controls.Add(lblInstallSubDirectory);
            installationPage.Controls.Add(lblPreviewPath);
            installationPage.Controls.Add(txtInstallSubDirectory);
            installationPage.Controls.Add(txtPreviewPath);
            installationPage.Location = new System.Drawing.Point(140, 4);
            installationPage.Name = "installationPage";
            installationPage.Padding = new System.Windows.Forms.Padding(3);
            installationPage.Size = new System.Drawing.Size(391, 376);
            installationPage.TabIndex = 1;
            installationPage.Text = "安装设置";
            // 
            // line7
            // 
            line7.BackColor = System.Drawing.SystemColors.Control;
            line7.Location = new System.Drawing.Point(72, 274);
            line7.Name = "line7";
            line7.Size = new System.Drawing.Size(311, 13);
            line7.TabIndex = 36;
            line7.TabStop = false;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(7, 274);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(59, 13);
            label10.TabIndex = 14;
            label10.Text = "自动启动";
            // 
            // line4
            // 
            line4.BackColor = System.Drawing.SystemColors.Control;
            line4.Location = new System.Drawing.Point(72, 5);
            line4.Name = "line4";
            line4.Size = new System.Drawing.Size(311, 13);
            line4.TabIndex = 34;
            line4.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(6, 5);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(59, 13);
            label5.TabIndex = 0;
            label5.Text = "安装位置";
            // 
            // chkInstall
            // 
            chkInstall.AutoSize = true;
            chkInstall.Location = new System.Drawing.Point(20, 21);
            chkInstall.Name = "chkInstall";
            chkInstall.Size = new System.Drawing.Size(91, 17);
            chkInstall.TabIndex = 1;
            chkInstall.Text = "安装客户端";
            chkInstall.UseVisualStyleBackColor = true;
            chkInstall.CheckedChanged += chkInstall_CheckedChanged;
            // 
            // lblInstallName
            // 
            lblInstallName.AutoSize = true;
            lblInstallName.Location = new System.Drawing.Point(17, 156);
            lblInstallName.Name = "lblInstallName";
            lblInstallName.Size = new System.Drawing.Size(62, 13);
            lblInstallName.TabIndex = 8;
            lblInstallName.Text = "安装名称:";
            // 
            // txtInstallName
            // 
            txtInstallName.Location = new System.Drawing.Point(182, 153);
            txtInstallName.Name = "txtInstallName";
            txtInstallName.Size = new System.Drawing.Size(170, 22);
            txtInstallName.TabIndex = 9;
            txtInstallName.TextChanged += HasChangedSettingAndFilePath;
            txtInstallName.KeyPress += txtInstallname_KeyPress;
            // 
            // txtRegistryKeyName
            // 
            txtRegistryKeyName.Location = new System.Drawing.Point(182, 324);
            txtRegistryKeyName.Name = "txtRegistryKeyName";
            txtRegistryKeyName.Size = new System.Drawing.Size(201, 22);
            txtRegistryKeyName.TabIndex = 17;
            txtRegistryKeyName.TextChanged += HasChangedSetting;
            // 
            // lblExtension
            // 
            lblExtension.AutoSize = true;
            lblExtension.Location = new System.Drawing.Point(352, 159);
            lblExtension.Name = "lblExtension";
            lblExtension.Size = new System.Drawing.Size(27, 13);
            lblExtension.TabIndex = 10;
            lblExtension.Text = ".exe";
            // 
            // lblRegistryKeyName
            // 
            lblRegistryKeyName.AutoSize = true;
            lblRegistryKeyName.Location = new System.Drawing.Point(17, 327);
            lblRegistryKeyName.Name = "lblRegistryKeyName";
            lblRegistryKeyName.Size = new System.Drawing.Size(62, 13);
            lblRegistryKeyName.TabIndex = 16;
            lblRegistryKeyName.Text = "启动名称:";
            // 
            // chkStartup
            // 
            chkStartup.AutoSize = true;
            chkStartup.Location = new System.Drawing.Point(20, 298);
            chkStartup.Name = "chkStartup";
            chkStartup.Size = new System.Drawing.Size(169, 17);
            chkStartup.TabIndex = 15;
            chkStartup.Text = "计算机启动时运行客户端";
            chkStartup.UseVisualStyleBackColor = true;
            chkStartup.CheckedChanged += chkStartup_CheckedChanged;
            // 
            // rbAppdata
            // 
            rbAppdata.AutoSize = true;
            rbAppdata.Checked = true;
            rbAppdata.Location = new System.Drawing.Point(241, 45);
            rbAppdata.Name = "rbAppdata";
            rbAppdata.Size = new System.Drawing.Size(129, 17);
            rbAppdata.TabIndex = 3;
            rbAppdata.TabStop = true;
            rbAppdata.Text = "用户应用程序数据";
            rbAppdata.UseVisualStyleBackColor = true;
            rbAppdata.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // lblInstallDirectory
            // 
            lblInstallDirectory.AutoSize = true;
            lblInstallDirectory.Location = new System.Drawing.Point(17, 47);
            lblInstallDirectory.Name = "lblInstallDirectory";
            lblInstallDirectory.Size = new System.Drawing.Size(62, 13);
            lblInstallDirectory.TabIndex = 2;
            lblInstallDirectory.Text = "安装目录:";
            // 
            // lblInstallSubDirectory
            // 
            lblInstallSubDirectory.AutoSize = true;
            lblInstallSubDirectory.Location = new System.Drawing.Point(17, 126);
            lblInstallSubDirectory.Name = "lblInstallSubDirectory";
            lblInstallSubDirectory.Size = new System.Drawing.Size(75, 13);
            lblInstallSubDirectory.TabIndex = 6;
            lblInstallSubDirectory.Text = "安装子目录:";
            // 
            // lblPreviewPath
            // 
            lblPreviewPath.AutoSize = true;
            lblPreviewPath.Location = new System.Drawing.Point(17, 218);
            lblPreviewPath.Name = "lblPreviewPath";
            lblPreviewPath.Size = new System.Drawing.Size(88, 13);
            lblPreviewPath.TabIndex = 12;
            lblPreviewPath.Text = "安装位置预览:";
            // 
            // txtInstallSubDirectory
            // 
            txtInstallSubDirectory.Location = new System.Drawing.Point(182, 123);
            txtInstallSubDirectory.Name = "txtInstallSubDirectory";
            txtInstallSubDirectory.Size = new System.Drawing.Size(201, 22);
            txtInstallSubDirectory.TabIndex = 7;
            txtInstallSubDirectory.TextChanged += HasChangedSettingAndFilePath;
            txtInstallSubDirectory.KeyPress += txtInstallsub_KeyPress;
            // 
            // txtPreviewPath
            // 
            txtPreviewPath.Location = new System.Drawing.Point(20, 234);
            txtPreviewPath.Name = "txtPreviewPath";
            txtPreviewPath.ReadOnly = true;
            txtPreviewPath.Size = new System.Drawing.Size(363, 22);
            txtPreviewPath.TabIndex = 13;
            txtPreviewPath.TabStop = false;
            // 
            // assemblyPage
            // 
            assemblyPage.BackColor = System.Drawing.SystemColors.Control;
            assemblyPage.Controls.Add(btnClone);
            assemblyPage.Controls.Add(iconPreview);
            assemblyPage.Controls.Add(btnBrowseIcon);
            assemblyPage.Controls.Add(txtIconPath);
            assemblyPage.Controls.Add(line8);
            assemblyPage.Controls.Add(label11);
            assemblyPage.Controls.Add(chkChangeAsmInfo);
            assemblyPage.Controls.Add(txtFileVersion);
            assemblyPage.Controls.Add(line9);
            assemblyPage.Controls.Add(lblProductName);
            assemblyPage.Controls.Add(label12);
            assemblyPage.Controls.Add(chkChangeIcon);
            assemblyPage.Controls.Add(lblFileVersion);
            assemblyPage.Controls.Add(txtProductName);
            assemblyPage.Controls.Add(txtProductVersion);
            assemblyPage.Controls.Add(lblDescription);
            assemblyPage.Controls.Add(lblProductVersion);
            assemblyPage.Controls.Add(txtDescription);
            assemblyPage.Controls.Add(txtOriginalFilename);
            assemblyPage.Controls.Add(lblCompanyName);
            assemblyPage.Controls.Add(lblOriginalFilename);
            assemblyPage.Controls.Add(txtCompanyName);
            assemblyPage.Controls.Add(txtTrademarks);
            assemblyPage.Controls.Add(lblCopyright);
            assemblyPage.Controls.Add(lblTrademarks);
            assemblyPage.Controls.Add(txtCopyright);
            assemblyPage.Location = new System.Drawing.Point(140, 4);
            assemblyPage.Name = "assemblyPage";
            assemblyPage.Size = new System.Drawing.Size(391, 376);
            assemblyPage.TabIndex = 2;
            assemblyPage.Text = "程序集设置";
            // 
            // btnClone
            // 
            btnClone.Location = new System.Drawing.Point(9, 343);
            btnClone.Name = "btnClone";
            btnClone.Size = new System.Drawing.Size(152, 23);
            btnClone.TabIndex = 43;
            btnClone.Text = "从 Exe 复制程序集信息";
            btnClone.UseVisualStyleBackColor = true;
            btnClone.Click += btnClone_Click;
            // 
            // iconPreview
            // 
            iconPreview.BackColor = System.Drawing.Color.Transparent;
            iconPreview.Location = new System.Drawing.Point(319, 302);
            iconPreview.Name = "iconPreview";
            iconPreview.Size = new System.Drawing.Size(64, 64);
            iconPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            iconPreview.TabIndex = 42;
            iconPreview.TabStop = false;
            // 
            // btnBrowseIcon
            // 
            btnBrowseIcon.Location = new System.Drawing.Point(177, 343);
            btnBrowseIcon.Name = "btnBrowseIcon";
            btnBrowseIcon.Size = new System.Drawing.Size(125, 23);
            btnBrowseIcon.TabIndex = 41;
            btnBrowseIcon.Text = "浏览...";
            btnBrowseIcon.UseVisualStyleBackColor = true;
            btnBrowseIcon.Click += btnBrowseIcon_Click;
            // 
            // txtIconPath
            // 
            txtIconPath.Location = new System.Drawing.Point(20, 315);
            txtIconPath.Name = "txtIconPath";
            txtIconPath.Size = new System.Drawing.Size(282, 22);
            txtIconPath.TabIndex = 39;
            // 
            // line8
            // 
            line8.BackColor = System.Drawing.SystemColors.Control;
            line8.Location = new System.Drawing.Point(84, 5);
            line8.Name = "line8";
            line8.Size = new System.Drawing.Size(299, 13);
            line8.TabIndex = 36;
            line8.TabStop = false;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(6, 5);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(72, 13);
            label11.TabIndex = 35;
            label11.Text = "程序集信息";
            // 
            // chkChangeAsmInfo
            // 
            chkChangeAsmInfo.AutoSize = true;
            chkChangeAsmInfo.Location = new System.Drawing.Point(20, 21);
            chkChangeAsmInfo.Name = "chkChangeAsmInfo";
            chkChangeAsmInfo.Size = new System.Drawing.Size(117, 17);
            chkChangeAsmInfo.TabIndex = 0;
            chkChangeAsmInfo.Text = "修改程序集信息";
            chkChangeAsmInfo.UseVisualStyleBackColor = true;
            chkChangeAsmInfo.CheckedChanged += chkChangeAsmInfo_CheckedChanged;
            // 
            // txtFileVersion
            // 
            txtFileVersion.Location = new System.Drawing.Point(182, 240);
            txtFileVersion.Name = "txtFileVersion";
            txtFileVersion.Size = new System.Drawing.Size(201, 22);
            txtFileVersion.TabIndex = 16;
            txtFileVersion.TextChanged += HasChangedSetting;
            // 
            // line9
            // 
            line9.BackColor = System.Drawing.SystemColors.Control;
            line9.Location = new System.Drawing.Point(83, 276);
            line9.Name = "line9";
            line9.Size = new System.Drawing.Size(300, 13);
            line9.TabIndex = 38;
            line9.TabStop = false;
            // 
            // lblProductName
            // 
            lblProductName.AutoSize = true;
            lblProductName.Location = new System.Drawing.Point(17, 47);
            lblProductName.Name = "lblProductName";
            lblProductName.Size = new System.Drawing.Size(62, 13);
            lblProductName.TabIndex = 1;
            lblProductName.Text = "产品名称:";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(6, 276);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(72, 13);
            label12.TabIndex = 0;
            label12.Text = "程序集图标";
            // 
            // chkChangeIcon
            // 
            chkChangeIcon.AutoSize = true;
            chkChangeIcon.Location = new System.Drawing.Point(20, 294);
            chkChangeIcon.Name = "chkChangeIcon";
            chkChangeIcon.Size = new System.Drawing.Size(117, 17);
            chkChangeIcon.TabIndex = 2;
            chkChangeIcon.Text = "修改程序集图标";
            chkChangeIcon.UseVisualStyleBackColor = true;
            chkChangeIcon.CheckedChanged += chkChangeIcon_CheckedChanged;
            // 
            // lblFileVersion
            // 
            lblFileVersion.AutoSize = true;
            lblFileVersion.Location = new System.Drawing.Point(17, 243);
            lblFileVersion.Name = "lblFileVersion";
            lblFileVersion.Size = new System.Drawing.Size(62, 13);
            lblFileVersion.TabIndex = 15;
            lblFileVersion.Text = "文件版本:";
            // 
            // txtProductName
            // 
            txtProductName.Location = new System.Drawing.Point(182, 44);
            txtProductName.Name = "txtProductName";
            txtProductName.Size = new System.Drawing.Size(201, 22);
            txtProductName.TabIndex = 2;
            txtProductName.TextChanged += HasChangedSetting;
            // 
            // txtProductVersion
            // 
            txtProductVersion.Location = new System.Drawing.Point(182, 212);
            txtProductVersion.Name = "txtProductVersion";
            txtProductVersion.Size = new System.Drawing.Size(201, 22);
            txtProductVersion.TabIndex = 14;
            txtProductVersion.TextChanged += HasChangedSetting;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new System.Drawing.Point(17, 75);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(36, 13);
            lblDescription.TabIndex = 3;
            lblDescription.Text = "描述:";
            // 
            // lblProductVersion
            // 
            lblProductVersion.AutoSize = true;
            lblProductVersion.Location = new System.Drawing.Point(17, 215);
            lblProductVersion.Name = "lblProductVersion";
            lblProductVersion.Size = new System.Drawing.Size(62, 13);
            lblProductVersion.TabIndex = 13;
            lblProductVersion.Text = "产品版本:";
            // 
            // txtDescription
            // 
            txtDescription.Location = new System.Drawing.Point(182, 72);
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new System.Drawing.Size(201, 22);
            txtDescription.TabIndex = 4;
            txtDescription.TextChanged += HasChangedSetting;
            // 
            // txtOriginalFilename
            // 
            txtOriginalFilename.Location = new System.Drawing.Point(182, 184);
            txtOriginalFilename.Name = "txtOriginalFilename";
            txtOriginalFilename.Size = new System.Drawing.Size(201, 22);
            txtOriginalFilename.TabIndex = 12;
            txtOriginalFilename.TextChanged += HasChangedSetting;
            // 
            // lblCompanyName
            // 
            lblCompanyName.AutoSize = true;
            lblCompanyName.Location = new System.Drawing.Point(17, 103);
            lblCompanyName.Name = "lblCompanyName";
            lblCompanyName.Size = new System.Drawing.Size(62, 13);
            lblCompanyName.TabIndex = 5;
            lblCompanyName.Text = "公司名称:";
            // 
            // lblOriginalFilename
            // 
            lblOriginalFilename.AutoSize = true;
            lblOriginalFilename.Location = new System.Drawing.Point(17, 187);
            lblOriginalFilename.Name = "lblOriginalFilename";
            lblOriginalFilename.Size = new System.Drawing.Size(75, 13);
            lblOriginalFilename.TabIndex = 11;
            lblOriginalFilename.Text = "原始文件名:";
            // 
            // txtCompanyName
            // 
            txtCompanyName.Location = new System.Drawing.Point(182, 100);
            txtCompanyName.Name = "txtCompanyName";
            txtCompanyName.Size = new System.Drawing.Size(201, 22);
            txtCompanyName.TabIndex = 6;
            txtCompanyName.TextChanged += HasChangedSetting;
            // 
            // txtTrademarks
            // 
            txtTrademarks.Location = new System.Drawing.Point(182, 156);
            txtTrademarks.Name = "txtTrademarks";
            txtTrademarks.Size = new System.Drawing.Size(201, 22);
            txtTrademarks.TabIndex = 10;
            txtTrademarks.TextChanged += HasChangedSetting;
            // 
            // lblCopyright
            // 
            lblCopyright.AutoSize = true;
            lblCopyright.Location = new System.Drawing.Point(17, 131);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new System.Drawing.Size(36, 13);
            lblCopyright.TabIndex = 7;
            lblCopyright.Text = "版权:";
            // 
            // lblTrademarks
            // 
            lblTrademarks.AutoSize = true;
            lblTrademarks.Location = new System.Drawing.Point(17, 159);
            lblTrademarks.Name = "lblTrademarks";
            lblTrademarks.Size = new System.Drawing.Size(36, 13);
            lblTrademarks.TabIndex = 9;
            lblTrademarks.Text = "商标:";
            // 
            // txtCopyright
            // 
            txtCopyright.Location = new System.Drawing.Point(182, 128);
            txtCopyright.Name = "txtCopyright";
            txtCopyright.Size = new System.Drawing.Size(201, 22);
            txtCopyright.TabIndex = 8;
            txtCopyright.TextChanged += HasChangedSetting;
            // FrmBuilder
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(535, 424);
            Controls.Add(builderTabs);
            Controls.Add(btnBuild);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmBuilder";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "客户端生成器";
            FormClosing += FrmBuilder_FormClosing;
            Load += FrmBuilder_Load;
            ((System.ComponentModel.ISupportInitialize)picUAC2).EndInit();
            ((System.ComponentModel.ISupportInitialize)picUAC1).EndInit();
            contextMenuStrip.ResumeLayout(false);
            builderTabs.ResumeLayout(false);
            generalPage.ResumeLayout(false);
            generalPage.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            connectionPage.ResumeLayout(false);
            connectionPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownDelay).EndInit();
            installationPage.ResumeLayout(false);
            installationPage.PerformLayout();
            assemblyPage.ResumeLayout(false);
            assemblyPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)iconPreview).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.Label lblDelay;
        private System.Windows.Forms.CheckBox chkInstall;
        private System.Windows.Forms.TextBox txtInstallName;
        private System.Windows.Forms.Label lblInstallName;
        private System.Windows.Forms.TextBox txtMutex;
        private System.Windows.Forms.Label lblMutex;
        private System.Windows.Forms.Label lblExtension;
        private System.Windows.Forms.Label lblInstallDirectory;
        private System.Windows.Forms.RadioButton rbAppdata;
        private System.Windows.Forms.TextBox txtInstallSubDirectory;
        private System.Windows.Forms.Label lblInstallSubDirectory;
        private System.Windows.Forms.Label lblPreviewPath;
        private System.Windows.Forms.TextBox txtPreviewPath;
        private System.Windows.Forms.Button btnMutex;
        private System.Windows.Forms.TextBox txtRegistryKeyName;
        private System.Windows.Forms.Label lblRegistryKeyName;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.Label lblMS;
        private System.Windows.Forms.RadioButton rbSystem;
        private System.Windows.Forms.RadioButton rbProgramFiles;
        private System.Windows.Forms.PictureBox picUAC1;
        private System.Windows.Forms.PictureBox picUAC2;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.CheckBox chkChangeIcon;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.TextBox txtProductName;
        private System.Windows.Forms.Label lblProductName;
        private System.Windows.Forms.TextBox txtOriginalFilename;
        private System.Windows.Forms.Label lblOriginalFilename;
        private System.Windows.Forms.TextBox txtTrademarks;
        private System.Windows.Forms.Label lblTrademarks;
        private System.Windows.Forms.TextBox txtCopyright;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.TextBox txtCompanyName;
        private System.Windows.Forms.Label lblCompanyName;
        private System.Windows.Forms.TextBox txtFileVersion;
        private System.Windows.Forms.Label lblFileVersion;
        private System.Windows.Forms.TextBox txtProductVersion;
        private System.Windows.Forms.Label lblProductVersion;
        private System.Windows.Forms.CheckBox chkChangeAsmInfo;
        private Controls.DotNetBarTabControl builderTabs;
        private System.Windows.Forms.TabPage connectionPage;
        private System.Windows.Forms.TabPage installationPage;
        private System.Windows.Forms.TabPage assemblyPage;
        private System.Windows.Forms.ListBox lstHosts;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.Button btnAddHost;
        private System.Windows.Forms.ToolStripMenuItem removeHostToolStripMenuItem;
        private Controls.Line line1;
        private System.Windows.Forms.Label label1;
        private Controls.Line line3;
        private System.Windows.Forms.Label label4;
        private Controls.Line line4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage generalPage;
        private System.Windows.Forms.TextBox txtTag;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblTag;
        private Controls.Line line5;
        private System.Windows.Forms.Label label6;
        private Controls.Line line6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private Controls.Line line7;
        private System.Windows.Forms.Label label10;
        private Controls.Line line8;
        private System.Windows.Forms.Label label11;
        private Controls.Line line9;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.Button btnBrowseIcon;
        private System.Windows.Forms.TextBox txtIconPath;
        private System.Windows.Forms.PictureBox iconPreview;
        private System.Windows.Forms.NumericUpDown numericUpDownDelay;
        private System.Windows.Forms.NumericUpDown numericUpDownPort;
        private Line line11;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtPastebin;
        private System.Windows.Forms.Button btnClone;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbX64;
        private System.Windows.Forms.RadioButton rbX86;
        private System.Windows.Forms.Label lblTargetArch;
    }
}
