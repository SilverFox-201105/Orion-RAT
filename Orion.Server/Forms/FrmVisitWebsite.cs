using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmVisitWebsite : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Url { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Hidden { get; set; }

        private readonly int _selectedClients;

        public FrmVisitWebsite(int selected)
        {
            _selectedClients = selected;
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
        }

        private void FrmVisitWebsite_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("访问网站", _selectedClients);
        }

        private void btnVisitWebsite_Click(object sender, EventArgs e)
        {
            Url = txtURL.Text;
            Hidden = chkVisitHidden.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}