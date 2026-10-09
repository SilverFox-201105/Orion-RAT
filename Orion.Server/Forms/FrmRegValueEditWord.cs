using Microsoft.Win32;
using Orion.Common.Models;
using Orion.Common.Utilities;
using Orion.Server.Enums;
using Orion.Server.Forms.DarkMode;
using System;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRegValueEditWord : Form
    {
        private readonly RegValueData _value;

        private const string DWORD_WARNING = "输入的十进制值超出 DWORD（32 位）最大值，是否截断后继续？";
        private const string QWORD_WARNING = "输入的十进制值超出 QWORD（64 位）最大值，是否截断后继续？";

        public FrmRegValueEditWord(RegValueData value)
        {
            _value = value;

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);

            this.valueNameTxtBox.Text = value.Name;

            if (value.Kind == RegistryValueKind.DWord)
            {
                this.Text = "编辑 DWORD (32 位) 值";
                this.valueDataTxtBox.Type = WordType.DWORD;
                this.valueDataTxtBox.Text = ByteConverter.ToUInt32(value.Data).ToString("x");
            }
            else
            {
                this.Text = "编辑 QWORD (64 位) 值";
                this.valueDataTxtBox.Type = WordType.QWORD;
                this.valueDataTxtBox.Text = ByteConverter.ToUInt64(value.Data).ToString("x");
            }
        }

        private void radioHex_CheckboxChanged(object sender, EventArgs e)
        {
            if (valueDataTxtBox.IsHexNumber == radioHexa.Checked)
                return;

            if (valueDataTxtBox.IsConversionValid() || IsOverridePossible())
                valueDataTxtBox.IsHexNumber = radioHexa.Checked;
            else
                radioDecimal.Checked = true;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (valueDataTxtBox.IsConversionValid() || IsOverridePossible())
            {
                _value.Data = _value.Kind == RegistryValueKind.DWord
                    ? ByteConverter.GetBytes(valueDataTxtBox.UIntValue)
                    : ByteConverter.GetBytes(valueDataTxtBox.ULongValue);
                this.Tag = _value;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.None;
            }

            this.Close();
        }

        private DialogResult ShowWarning(string msg, string caption)
        {
            return MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }

        private bool IsOverridePossible()
        {
            string message = _value.Kind == RegistryValueKind.DWord ? DWORD_WARNING : QWORD_WARNING;

            return ShowWarning(message, "溢出") == DialogResult.Yes;
        }
    }
}
