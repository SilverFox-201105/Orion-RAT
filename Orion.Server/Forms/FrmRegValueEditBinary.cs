using Orion.Common.Models;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Registry;
using System;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRegValueEditBinary : Form
    {
        private readonly RegValueData _value;

        private const string INVALID_BINARY_ERROR = "二进制值无效，无法正确转换。";

        public FrmRegValueEditBinary(RegValueData value)
        {
            _value = value;

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);

            this.valueNameTxtBox.Text = RegValueHelper.GetName(value.Name);
            hexEditor.HexTable = value.Data;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            byte[] bytes = hexEditor.HexTable;
            if (bytes != null)
            {
                try
                {
                    _value.Data = bytes;
                    this.DialogResult = DialogResult.OK;
                    this.Tag = _value;
                }
                catch
                {
                    ShowWarning(INVALID_BINARY_ERROR, "警告");
                    this.DialogResult = DialogResult.None;
                }
            }

            this.Close();
        }

        private void ShowWarning(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
