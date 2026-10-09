using System.Drawing;

namespace Orion.Server.Forms.RemoteDesktopPopUp
{
    partial class FrmOpenApplicationOnMonitor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmOpenApplicationOnMonitor));
            this.txtBoxPathAndArgs = new System.Windows.Forms.TextBox();
            this.labelInstructions = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.labelExplanation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtBoxPathAndArgs
            // 
            this.txtBoxPathAndArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBoxPathAndArgs.Location = new System.Drawing.Point(12, 25);
            this.txtBoxPathAndArgs.Name = "txtBoxPathAndArgs";
            this.txtBoxPathAndArgs.Size = new System.Drawing.Size(639, 20);
            this.txtBoxPathAndArgs.TabIndex = 0;
            // 
            // labelInstructions
            //
            this.labelInstructions.AutoSize = true;
            this.labelInstructions.Location = new System.Drawing.Point(12, 9);
            this.labelInstructions.Name = "labelInstructions";
            this.labelInstructions.Size = new System.Drawing.Size(170, 13);
            this.labelInstructions.TabIndex = 1;
            this.labelInstructions.Text = "要启动的可执行文件路径及参数";
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.Location = new System.Drawing.Point(12, 51);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(639, 54);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "启动";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // labelExplanation
            // 
            this.labelExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))));
            this.labelExplanation.AutoSize = true;
            this.labelExplanation.Location = new System.Drawing.Point(278, 9);
            this.labelExplanation.Name = "labelExplanation";
            this.labelExplanation.Size = new System.Drawing.Size(373, 13);
            this.labelExplanation.TabIndex = 3;
            this.labelExplanation.Text = "启动应用程序并将其移动到当前正在查看的显示器上";
            // 
            // FrmOpenApplicationOnMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(663, 111);
            this.Controls.Add(this.labelExplanation);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.labelInstructions);
            this.Controls.Add(this.txtBoxPathAndArgs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(580, 150);
            this.Name = "FrmOpenApplicationOnMonitor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "自定义文件选择器";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBoxPathAndArgs;
        private System.Windows.Forms.Label labelInstructions;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label labelExplanation;
    }
}