namespace Orion.Server.Forms
{
    partial class FrmRemoteExecution
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteExecution));
            btnExecute = new System.Windows.Forms.Button();
            txtURL = new System.Windows.Forms.TextBox();
            lblURL = new System.Windows.Forms.Label();
            groupLocalFile = new System.Windows.Forms.GroupBox();
            btnBrowse = new System.Windows.Forms.Button();
            txtPath = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            groupURL = new System.Windows.Forms.GroupBox();
            radioLocalFile = new System.Windows.Forms.RadioButton();
            radioURL = new System.Windows.Forms.RadioButton();
            lstTransfers = new Orion.Server.Controls.AeroListView();
            hClient = new System.Windows.Forms.ColumnHeader();
            hStatus = new System.Windows.Forms.ColumnHeader();
            chkBoxReflectionExecute = new System.Windows.Forms.CheckBox();
            groupLocalFile.SuspendLayout();
            groupURL.SuspendLayout();
            SuspendLayout();
            // 
            // btnExecute
            // 
            btnExecute.Location = new System.Drawing.Point(353, 488);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new System.Drawing.Size(138, 27);
            btnExecute.TabIndex = 6;
            btnExecute.Text = "远程执行";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += btnExecute_Click;
            // 
            // txtURL
            // 
            txtURL.Location = new System.Drawing.Point(59, 25);
            txtURL.Name = "txtURL";
            txtURL.Size = new System.Drawing.Size(317, 26);
            txtURL.TabIndex = 1;
            // 
            // lblURL
            // 
            lblURL.AutoSize = true;
            lblURL.Location = new System.Drawing.Point(9, 25);
            lblURL.Name = "lblURL";
            lblURL.Size = new System.Drawing.Size(48, 19);
            lblURL.TabIndex = 0;
            lblURL.Text = "URL：";
            // 
            // groupLocalFile
            // 
            groupLocalFile.Controls.Add(btnBrowse);
            groupLocalFile.Controls.Add(txtPath);
            groupLocalFile.Controls.Add(label1);
            groupLocalFile.Location = new System.Drawing.Point(12, 65);
            groupLocalFile.Name = "groupLocalFile";
            groupLocalFile.Size = new System.Drawing.Size(479, 75);
            groupLocalFile.TabIndex = 1;
            groupLocalFile.TabStop = false;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new System.Drawing.Point(398, 27);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new System.Drawing.Size(75, 28);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "浏览...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // txtPath
            // 
            txtPath.Location = new System.Drawing.Point(59, 24);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new System.Drawing.Size(317, 26);
            txtPath.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 24);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(51, 19);
            label1.TabIndex = 0;
            label1.Text = "路径：";
            // 
            // groupURL
            // 
            groupURL.Controls.Add(txtURL);
            groupURL.Controls.Add(lblURL);
            groupURL.Enabled = false;
            groupURL.Location = new System.Drawing.Point(12, 169);
            groupURL.Name = "groupURL";
            groupURL.Size = new System.Drawing.Size(479, 75);
            groupURL.TabIndex = 3;
            groupURL.TabStop = false;
            // 
            // radioLocalFile
            // 
            radioLocalFile.AutoSize = true;
            radioLocalFile.Checked = true;
            radioLocalFile.Location = new System.Drawing.Point(12, 12);
            radioLocalFile.Name = "radioLocalFile";
            radioLocalFile.Size = new System.Drawing.Size(114, 23);
            radioLocalFile.TabIndex = 0;
            radioLocalFile.TabStop = true;
            radioLocalFile.Text = "执行本地文件";
            radioLocalFile.UseVisualStyleBackColor = true;
            radioLocalFile.CheckedChanged += radioLocalFile_CheckedChanged;
            // 
            // radioURL
            // 
            radioURL.AutoSize = true;
            radioURL.Location = new System.Drawing.Point(12, 146);
            radioURL.Name = "radioURL";
            radioURL.Size = new System.Drawing.Size(97, 23);
            radioURL.TabIndex = 2;
            radioURL.Text = "从URL执行";
            radioURL.UseVisualStyleBackColor = true;
            radioURL.CheckedChanged += radioURL_CheckedChanged;
            // 
            // lstTransfers
            // 
            lstTransfers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstTransfers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hClient, hStatus });
            lstTransfers.FullRowSelect = true;
            lstTransfers.Location = new System.Drawing.Point(12, 250);
            lstTransfers.Name = "lstTransfers";
            lstTransfers.Size = new System.Drawing.Size(479, 230);
            lstTransfers.TabIndex = 4;
            lstTransfers.UseCompatibleStateImageBehavior = false;
            lstTransfers.View = System.Windows.Forms.View.Details;
            // 
            // hClient
            // 
            hClient.Text = "客户端";
            hClient.Width = 302;
            // 
            // hStatus
            // 
            hStatus.Text = "状态";
            hStatus.Width = 173;
            // 
            // chkBoxReflectionExecute
            // 
            chkBoxReflectionExecute.Location = new System.Drawing.Point(128, 12);
            chkBoxReflectionExecute.Name = "chkBoxReflectionExecute";
            chkBoxReflectionExecute.Size = new System.Drawing.Size(177, 26);
            chkBoxReflectionExecute.TabIndex = 7;
            chkBoxReflectionExecute.Text = "内存执行（仅.NET）";
            chkBoxReflectionExecute.UseVisualStyleBackColor = true;
            chkBoxReflectionExecute.CheckedChanged += chkBoxReflectionExecute_CheckedChanged;
            // 
            // FrmRemoteExecution
            // 
            AcceptButton = btnExecute;
            AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(503, 521);
            Controls.Add(chkBoxReflectionExecute);
            Controls.Add(lstTransfers);
            Controls.Add(radioURL);
            Controls.Add(radioLocalFile);
            Controls.Add(groupURL);
            Controls.Add(groupLocalFile);
            Controls.Add(btnExecute);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(400, 350);
            Name = "FrmRemoteExecution";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "远程执行 []";
            FormClosing += FrmRemoteExecution_FormClosing;
            Load += FrmRemoteExecution_Load;
            groupLocalFile.ResumeLayout(false);
            groupLocalFile.PerformLayout();
            groupURL.ResumeLayout(false);
            groupURL.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.TextBox txtURL;
        private System.Windows.Forms.Label lblURL;
        private System.Windows.Forms.GroupBox groupLocalFile;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupURL;
        private System.Windows.Forms.RadioButton radioLocalFile;
        private System.Windows.Forms.RadioButton radioURL;
        private System.Windows.Forms.Button btnBrowse;
        private Controls.AeroListView lstTransfers;
        private System.Windows.Forms.ColumnHeader hClient;
        private System.Windows.Forms.ColumnHeader hStatus;
        private System.Windows.Forms.CheckBox chkBoxReflectionExecute;
    }
}