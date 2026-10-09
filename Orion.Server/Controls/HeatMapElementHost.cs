using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Orion.Server.Controls.Wpf;
using Orion.Server.Statistics;

#nullable enable

namespace Orion.Server.Controls
{
    public sealed class HeatMapElementHost : ElementHost
    {
        private readonly HeatMapView _heatMapView;
        private ClientGeoSnapshot? _lastSnapshot;

        public HeatMapElementHost()
        {
            _heatMapView = new HeatMapView();
            Child = _heatMapView;
            Dock = DockStyle.Fill;
        }

        public void ShowLoading()
        {
            _heatMapView.ShowLoading();
        }

        public void ShowError(string message)
        {
            _heatMapView.ShowError(message);
        }

        public void UpdateSnapshot(ClientGeoSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            _heatMapView.UpdateSnapshot(snapshot);
        }

        public void ApplyTheme(bool isDarkMode)
        {
            _heatMapView.ApplyTheme(isDarkMode);
            if (_lastSnapshot != null && !_lastSnapshot.HasError)
            {
                _heatMapView.UpdateSnapshot(_lastSnapshot);
            }
        }
    }
}
