using Orion.Common.DNS;
using Orion.Server.Forms.DarkMode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Orion.Server.Forms
{
    public partial class FrmTaskCommand : Form
    {
        public FrmTaskCommand()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void FrmTaskCommand_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string host;

            if (ShellComboBox.SelectedIndex == 0)
            {
                host = "cmd.exe";
            }
            else
            {
                host = "powershell.exe";
            }

            FrmMain frm = Application.OpenForms["FrmMain"] as FrmMain;
            if (frm != null)
            {
                frm.AddTask("Shell 命令", host, CTextBox.Text);
            }
        }
    }
}
