using Orion.Common.Messages.Other;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Networking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orion.Server.Forms.RemoteDesktopPopUp
{
    public partial class FrmOpenApplicationOnMonitor : Form
    {
        private readonly Client _client;
        private readonly int _displayIndex;

        public FrmOpenApplicationOnMonitor(Client c, int displayIndex)
        {
            _client = c;
            _displayIndex = displayIndex;

            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _client.Send(new StartProcessOnMonitor
            {
                Application = txtBoxPathAndArgs.Text,
                MonitorID = _displayIndex

            });
        }
    }
}
