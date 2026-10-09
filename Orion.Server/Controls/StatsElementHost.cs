using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Orion.Server.Controls.Wpf;
using Orion.Server.Statistics;

#nullable enable

namespace Orion.Server.Controls
{
    public sealed class StatsElementHost : ElementHost
    {
        private readonly StatsView _statsView;
        private ClientStatisticsSnapshot? _lastSnapshot;

        public StatsElementHost()
        {
            _statsView = new StatsView();
            Child = _statsView;
            Dock = DockStyle.Fill;
        }

        public void ShowLoading()
        {
            _statsView.ShowLoading();
        }

        public void ShowError(string message)
        {
            _statsView.ShowError(message);
        }

        public void UpdateSnapshot(ClientStatisticsSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            _statsView.UpdateSnapshot(snapshot);
        }

        public void ApplyTheme(bool isDarkMode)
        {
            _statsView.ApplyTheme(isDarkMode);
            if (_lastSnapshot != null && !_lastSnapshot.HasError)
            {
                _statsView.UpdateSnapshot(_lastSnapshot);
            }
        }
    }
}
