using System;
using System.Drawing;
using System.Windows.Forms;
using Orion.Server.Forms.DarkMode;

namespace Orion.Server.Controls
{
    public static class InputBox
    {
        public static DialogResult Show(string title, string promptText, ref string value)
        {
            DialogResult dialogResult = DialogResult.Cancel;
            using (var form = new Form())
            {
                form.SuspendLayout();

                form.AutoScaleDimensions = new SizeF(96F, 96F);
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Font = new Font("Segoe UI", 9F);
                form.Text = title;

                var label = new Label();
                var textBox = new TextBox();
                var buttonOk = new Button();
                var buttonCancel = new Button();

                label.Text = promptText;
                label.AutoSize = true;
                label.Location = new Point(16, 12);

                textBox.Text = value;
                textBox.Location = new Point(16, 36);
                textBox.Size = new Size(388, 23);
                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                buttonOk.Text = "确定";
                buttonOk.DialogResult = DialogResult.OK;
                buttonOk.Location = new Point(220, 72);
                buttonOk.Size = new Size(88, 30);
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                buttonCancel.Text = "取消";
                buttonCancel.DialogResult = DialogResult.Cancel;
                buttonCancel.Location = new Point(316, 72);
                buttonCancel.Size = new Size(88, 30);
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.ClientSize = new Size(420, 116);
                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                form.ResumeLayout(false);
                form.PerformLayout();

                DarkModeManager.ApplyDarkMode(form);

                dialogResult = form.ShowDialog();
                value = textBox.Text;
            }
            return dialogResult;
        }
    }
}
