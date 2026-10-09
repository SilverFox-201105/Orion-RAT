namespace Orion.Server.Forms
{
    partial class FrmShowMessagebox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmShowMessagebox));
            groupMsgSettings = new System.Windows.Forms.GroupBox();
            cmbMsgIcon = new System.Windows.Forms.ComboBox();
            lblMsgIcon = new System.Windows.Forms.Label();
            cmbMsgButtons = new System.Windows.Forms.ComboBox();
            lblMsgButtons = new System.Windows.Forms.Label();
            txtText = new System.Windows.Forms.TextBox();
            txtCaption = new System.Windows.Forms.TextBox();
            lblText = new System.Windows.Forms.Label();
            lblCaption = new System.Windows.Forms.Label();
            btnPreview = new System.Windows.Forms.Button();
            btnSend = new System.Windows.Forms.Button();
            groupMsgSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupMsgSettings
            // 
            groupMsgSettings.Controls.Add(cmbMsgIcon);
            groupMsgSettings.Controls.Add(lblMsgIcon);
            groupMsgSettings.Controls.Add(cmbMsgButtons);
            groupMsgSettings.Controls.Add(lblMsgButtons);
            groupMsgSettings.Controls.Add(txtText);
            groupMsgSettings.Controls.Add(txtCaption);
            groupMsgSettings.Controls.Add(lblText);
            groupMsgSettings.Controls.Add(lblCaption);
            groupMsgSettings.Location = new System.Drawing.Point(12, 12);
            groupMsgSettings.Name = "groupMsgSettings";
            groupMsgSettings.Size = new System.Drawing.Size(325, 146);
            groupMsgSettings.TabIndex = 0;
            groupMsgSettings.TabStop = false;
            groupMsgSettings.Text = "消息框设置";
            // 
            // cmbMsgIcon
            // 
            cmbMsgIcon.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbMsgIcon.FormattingEnabled = true;
            cmbMsgIcon.Location = new System.Drawing.Point(147, 107);
            cmbMsgIcon.Name = "cmbMsgIcon";
            cmbMsgIcon.Size = new System.Drawing.Size(162, 27);
            cmbMsgIcon.TabIndex = 8;
            // 
            // lblMsgIcon
            // 
            lblMsgIcon.AutoSize = true;
            lblMsgIcon.Location = new System.Drawing.Point(23, 107);
            lblMsgIcon.Name = "lblMsgIcon";
            lblMsgIcon.Size = new System.Drawing.Size(93, 19);
            lblMsgIcon.TabIndex = 7;
            lblMsgIcon.Text = "消息框图标：";
            // 
            // cmbMsgButtons
            // 
            cmbMsgButtons.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbMsgButtons.FormattingEnabled = true;
            cmbMsgButtons.Location = new System.Drawing.Point(147, 80);
            cmbMsgButtons.Name = "cmbMsgButtons";
            cmbMsgButtons.Size = new System.Drawing.Size(162, 27);
            cmbMsgButtons.TabIndex = 6;
            // 
            // lblMsgButtons
            // 
            lblMsgButtons.AutoSize = true;
            lblMsgButtons.Location = new System.Drawing.Point(23, 83);
            lblMsgButtons.Name = "lblMsgButtons";
            lblMsgButtons.Size = new System.Drawing.Size(93, 19);
            lblMsgButtons.TabIndex = 5;
            lblMsgButtons.Text = "消息框按钮：";
            // 
            // txtText
            // 
            txtText.Location = new System.Drawing.Point(60, 49);
            txtText.MaxLength = 256;
            txtText.Name = "txtText";
            txtText.Size = new System.Drawing.Size(249, 26);
            txtText.TabIndex = 4;
            txtText.Text = "您正在运行 Orion。";
            // 
            // txtCaption
            // 
            txtCaption.Location = new System.Drawing.Point(60, 21);
            txtCaption.MaxLength = 256;
            txtCaption.Name = "txtCaption";
            txtCaption.Size = new System.Drawing.Size(249, 26);
            txtCaption.TabIndex = 2;
            txtCaption.Text = "提示信息";
            // 
            // lblText
            // 
            lblText.AutoSize = true;
            lblText.Location = new System.Drawing.Point(6, 52);
            lblText.Name = "lblText";
            lblText.Size = new System.Drawing.Size(51, 19);
            lblText.TabIndex = 3;
            lblText.Text = "文本：";
            // 
            // lblCaption
            // 
            lblCaption.AutoSize = true;
            lblCaption.Location = new System.Drawing.Point(6, 24);
            lblCaption.Name = "lblCaption";
            lblCaption.Size = new System.Drawing.Size(51, 19);
            lblCaption.TabIndex = 1;
            lblCaption.Text = "标题：";
            // 
            // btnPreview
            // 
            btnPreview.Location = new System.Drawing.Point(181, 164);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new System.Drawing.Size(75, 29);
            btnPreview.TabIndex = 1;
            btnPreview.Text = "预览";
            btnPreview.UseVisualStyleBackColor = true;
            btnPreview.Click += btnPreview_Click;
            // 
            // btnSend
            // 
            btnSend.Location = new System.Drawing.Point(262, 164);
            btnSend.Name = "btnSend";
            btnSend.Size = new System.Drawing.Size(75, 29);
            btnSend.TabIndex = 2;
            btnSend.Text = "发送";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // FrmShowMessagebox
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(349, 199);
            Controls.Add(btnSend);
            Controls.Add(btnPreview);
            Controls.Add(groupMsgSettings);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmShowMessagebox";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "显示消息框 []";
            Load += FrmShowMessagebox_Load;
            groupMsgSettings.ResumeLayout(false);
            groupMsgSettings.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupMsgSettings;
        private System.Windows.Forms.ComboBox cmbMsgIcon;
        private System.Windows.Forms.Label lblMsgIcon;
        private System.Windows.Forms.ComboBox cmbMsgButtons;
        private System.Windows.Forms.Label lblMsgButtons;
        private System.Windows.Forms.TextBox txtText;
        private System.Windows.Forms.TextBox txtCaption;
        private System.Windows.Forms.Label lblText;
        private System.Windows.Forms.Label lblCaption;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Button btnSend;
    }
}