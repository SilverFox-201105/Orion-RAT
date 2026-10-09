using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmShowMessagebox : Form
    {
        private readonly int _selectedClients;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string MsgBoxCaption { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string MsgBoxText { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string MsgBoxButton { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string MsgBoxIcon { get; set; }


        public FrmShowMessagebox(int selected)
        {
            _selectedClients = selected;

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
        }

        private void FrmShowMessagebox_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("显示消息框", _selectedClients);

            cmbMsgButtons.Items.AddRange(new string[]
            {"中止/重试/忽略", "确定", "确定/取消", "重试/取消", "是/否", "是/否/取消"});
            cmbMsgButtons.SelectedIndex = 0;
            cmbMsgIcon.Items.AddRange(new string[]
            {"无", "错误", "问号", "警告", "信息"});
            cmbMsgIcon.SelectedIndex = 0;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            MessageBox.Show(null, txtText.Text, txtCaption.Text,
                (MessageBoxButtons)
                    Enum.Parse(typeof(MessageBoxButtons), GetMessageBoxButton(cmbMsgButtons.SelectedIndex)),
                (MessageBoxIcon)Enum.Parse(typeof(MessageBoxIcon), GetMessageBoxIcon(cmbMsgIcon.SelectedIndex)));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            MsgBoxCaption = txtCaption.Text;
            MsgBoxText = txtText.Text;
            MsgBoxButton = GetMessageBoxButton(cmbMsgButtons.SelectedIndex);
            MsgBoxIcon = GetMessageBoxIcon(cmbMsgIcon.SelectedIndex);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private string GetMessageBoxButton(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    return "AbortRetryIgnore";
                case 1:
                    return "OK";
                case 2:
                    return "OKCancel";
                case 3:
                    return "RetryCancel";
                case 4:
                    return "YesNo";
                case 5:
                    return "YesNoCancel";
                default:
                    return "OK";
            }
        }

        private string GetMessageBoxIcon(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    return "None";
                case 1:
                    return "Error";
                case 2:
                    return "Question";
                case 3:
                    return "Warning";
                case 4:
                    return "Information";
                default:
                    return "None";
            }
        }
    }
}