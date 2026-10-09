namespace Orion.Server.Forms
{
    partial class FrmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAbout));
            picIcon = new System.Windows.Forms.PictureBox();
            lblTitle = new System.Windows.Forms.Label();
            lblVersion = new System.Windows.Forms.Label();
            btnOkay = new System.Windows.Forms.Button();
            rtxtContent = new System.Windows.Forms.RichTextBox();
            lblLicense = new System.Windows.Forms.Label();
            lnkTelegram = new System.Windows.Forms.LinkLabel();
            lblSubTitle = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            cntTxtContent = new System.Windows.Forms.RichTextBox();
            button1 = new System.Windows.Forms.Button();
            linkLabel1 = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            SuspendLayout();
            // 
            // picIcon
            // 
            picIcon.BackColor = System.Drawing.Color.Transparent;
            picIcon.Image = Properties.Resources.Orion_Server;
            picIcon.Location = new System.Drawing.Point(12, 12);
            picIcon.Name = "picIcon";
            picIcon.Size = new System.Drawing.Size(64, 64);
            picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;
            picIcon.Click += picIcon_Click;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.BackColor = System.Drawing.Color.Transparent;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 19.8000011F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitle.ForeColor = System.Drawing.SystemColors.Control;
            lblTitle.Location = new System.Drawing.Point(82, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(177, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Orion";
            // 
            // lblVersion
            // 
            lblVersion.BackColor = System.Drawing.Color.Transparent;
            lblVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblVersion.ForeColor = System.Drawing.SystemColors.Control;
            lblVersion.Location = new System.Drawing.Point(438, 20);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new System.Drawing.Size(75, 17);
            lblVersion.TabIndex = 2;
            lblVersion.Text = "%VERSION%";
            lblVersion.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // btnOkay
            // 
            btnOkay.BackColor = System.Drawing.Color.Transparent;
            btnOkay.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnOkay.ForeColor = System.Drawing.SystemColors.WindowText;
            btnOkay.Location = new System.Drawing.Point(438, 370);
            btnOkay.Name = "btnOkay";
            btnOkay.Size = new System.Drawing.Size(75, 33);
            btnOkay.TabIndex = 8;
            btnOkay.Text = "确定";
            btnOkay.UseVisualStyleBackColor = false;
            btnOkay.Click += btnOkay_Click;
            // 
            // rtxtContent
            // 
            rtxtContent.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            rtxtContent.Location = new System.Drawing.Point(15, 265);
            rtxtContent.Name = "rtxtContent";
            rtxtContent.ReadOnly = true;
            rtxtContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            rtxtContent.Size = new System.Drawing.Size(498, 99);
            rtxtContent.TabIndex = 7;
            rtxtContent.Text = "";
            // 
            // lblLicense
            // 
            lblLicense.AutoSize = true;
            lblLicense.BackColor = System.Drawing.Color.Transparent;
            lblLicense.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblLicense.ForeColor = System.Drawing.SystemColors.Control;
            lblLicense.Location = new System.Drawing.Point(12, 247);
            lblLicense.Name = "lblLicense";
            lblLicense.Size = new System.Drawing.Size(76, 15);
            lblLicense.TabIndex = 6;
            lblLicense.Text = "源代码许可";
            // 
            // lnkTelegram
            // 
            lnkTelegram.AutoSize = true;
            lnkTelegram.BackColor = System.Drawing.Color.Transparent;
            lnkTelegram.Location = new System.Drawing.Point(432, 41);
            lnkTelegram.Name = "lnkTelegram";
            lnkTelegram.Size = new System.Drawing.Size(81, 13);
            lnkTelegram.TabIndex = 4;
            lnkTelegram.TabStop = true;
            lnkTelegram.Text = "Telegram 群聊";
            lnkTelegram.LinkClicked += lnkTelegram_LinkClicked;
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.BackColor = System.Drawing.Color.Transparent;
            lblSubTitle.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblSubTitle.ForeColor = System.Drawing.SystemColors.Control;
            lblSubTitle.Location = new System.Drawing.Point(82, 54);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new System.Drawing.Size(132, 25);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "远程控制工具";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = System.Drawing.Color.Transparent;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label1.ForeColor = System.Drawing.SystemColors.Control;
            label1.Location = new System.Drawing.Point(12, 108);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(228, 15);
            label1.TabIndex = 9;
            label1.Text = "感谢以下贡献者使这个项目成为问世：";
            // 
            // cntTxtContent
            // 
            cntTxtContent.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            cntTxtContent.Location = new System.Drawing.Point(14, 131);
            cntTxtContent.Name = "cntTxtContent";
            cntTxtContent.ReadOnly = true;
            cntTxtContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            cntTxtContent.Size = new System.Drawing.Size(498, 108);
            cntTxtContent.TabIndex = 10;
            cntTxtContent.Text = "";
            cntTxtContent.TextChanged += cntTxtContent_TextChanged_1;
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.Color.Transparent;
            button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            button1.ForeColor = System.Drawing.SystemColors.WindowText;
            button1.Location = new System.Drawing.Point(14, 370);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(111, 33);
            button1.TabIndex = 12;
            button1.Text = "开发者留言";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.BackColor = System.Drawing.Color.Transparent;
            linkLabel1.Location = new System.Drawing.Point(400, 63);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new System.Drawing.Size(112, 13);
            linkLabel1.TabIndex = 13;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Quasar GitHub 页面";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // FrmAbout
            // 
            AcceptButton = btnOkay;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.ControlDarkDark;
            BackgroundImage = Properties.Resources.AboutWallpaper;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            CancelButton = btnOkay;
            ClientSize = new System.Drawing.Size(527, 405);
            Controls.Add(linkLabel1);
            Controls.Add(button1);
            Controls.Add(cntTxtContent);
            Controls.Add(label1);
            Controls.Add(lblSubTitle);
            Controls.Add(lnkTelegram);
            Controls.Add(lblLicense);
            Controls.Add(rtxtContent);
            Controls.Add(btnOkay);
            Controls.Add(lblVersion);
            Controls.Add(lblTitle);
            Controls.Add(picIcon);
            DoubleBuffered = true;
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmAbout";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Orion - 关于";
            Load += FrmAbout_Load;
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Button btnOkay;
        private System.Windows.Forms.RichTextBox rtxtContent;
        private System.Windows.Forms.Label lblLicense;
        private System.Windows.Forms.LinkLabel lnkTelegram;
        private System.Windows.Forms.Label lblSubTitle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox cntTxtContent;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.LinkLabel linkLabel1;
    }
}