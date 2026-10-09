namespace Orion.Server.Forms
{
    partial class FrmRemoteShell
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteShell));
            txtConsoleOutput = new System.Windows.Forms.RichTextBox();
            SuspendLayout();
            // 
            // txtConsoleOutput
            // 
            txtConsoleOutput.BackColor = System.Drawing.Color.Black;
            txtConsoleOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtConsoleOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            txtConsoleOutput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtConsoleOutput.ForeColor = System.Drawing.Color.WhiteSmoke;
            txtConsoleOutput.Location = new System.Drawing.Point(0, 0);
            txtConsoleOutput.Name = "txtConsoleOutput";
            //  原生终端风格：不自动换行 + 横纵滚动条
            txtConsoleOutput.WordWrap = false;
            txtConsoleOutput.ReadOnly = true;
            txtConsoleOutput.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            txtConsoleOutput.Size = new System.Drawing.Size(637, 323);
            txtConsoleOutput.TabIndex = 0;
            txtConsoleOutput.Text = "";
            txtConsoleOutput.KeyDown += txtConsoleOutput_KeyDown;
            txtConsoleOutput.KeyPress += txtConsoleOutput_KeyPress;
            // 
            // FrmRemoteShell
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(637, 323);
            Controls.Add(txtConsoleOutput);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(400, 250);
            Name = "FrmRemoteShell";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "远程终端 []";
            FormClosing += FrmRemoteShell_FormClosing;
            Load += FrmRemoteShell_Load;
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtConsoleOutput;
    }
}
