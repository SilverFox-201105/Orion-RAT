using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LiveChartsCore.SkiaSharpView.WPF;
using Orion.Server.Statistics;

#nullable enable

namespace Orion.Server.Controls.Wpf
{
    public partial class StatsView : UserControl
    {
        private readonly StatsViewModel _viewModel;
        private readonly CartesianChart _newClientsChart;
        private readonly PieChart _countryChart;
        private readonly PieChart _operatingSystemChart;

        public StatsView()
        {
            InitializeComponent();
            _viewModel = new StatsViewModel();
            DataContext = _viewModel;

            Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            _newClientsChart = CreateCartesianChart();
            _countryChart = CreatePieChart();
            _operatingSystemChart = CreatePieChart();

            NewClientsChartHost.Content = _newClientsChart;
            CountryChartHost.Content = _countryChart;
            OperatingSystemChartHost.Content = _operatingSystemChart;

            Bind(_newClientsChart, CartesianChart.SeriesProperty, nameof(StatsViewModel.NewClientsSeries));
            Bind(_newClientsChart, CartesianChart.XAxesProperty, nameof(StatsViewModel.NewClientsXAxes));
            Bind(_newClientsChart, CartesianChart.YAxesProperty, nameof(StatsViewModel.NewClientsYAxes));

            Bind(_countryChart, PieChart.SeriesProperty, nameof(StatsViewModel.ClientsByCountrySeries));
            Bind(_operatingSystemChart, PieChart.SeriesProperty, nameof(StatsViewModel.ClientsByOperatingSystemSeries));
        }

        private void OnDispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is NullReferenceException &&
                e.Exception.StackTrace?.Contains("LiveChartsCore.SkiaSharpView.WPF.Rendering.CompositionTargetTicker.DisposeTicker", StringComparison.Ordinal) == true)
            {
                e.Handled = true;
            }
        }

        public void ShowLoading()
        {
            Dispatcher.Invoke(() => _viewModel.SetLoading());
        }

        public void ShowError(string message)
        {
            Dispatcher.Invoke(() => _viewModel.SetError(message));
        }

        public void UpdateSnapshot(ClientStatisticsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            Dispatcher.Invoke(() => _viewModel.UpdateSnapshot(snapshot));
        }

        public void ApplyTheme(bool isDarkMode)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateBrush("StatsBackgroundBrush", isDarkMode ? "#FF000000" : "#FFF8F9FB");
                UpdateBrush("CardBackgroundBrush", isDarkMode ? "#FF111111" : "#FFFFFFFF");
                UpdateBrush("CardBorderBrush", isDarkMode ? "#FF222222" : "#FFE5E7EB");
                UpdateBrush("CardForegroundBrush", isDarkMode ? "#FFE5E7EB" : "#FF1A1A2E");
                UpdateBrush("MutedTextBrush", isDarkMode ? "#FF8B92A5" : "#FF6B7280");
                UpdateBrush("AccentBrush", isDarkMode ? "#FF60A5FA" : "#FF3B82F6");
                UpdateBrush("PositiveAccentBrush", isDarkMode ? "#FF34D399" : "#FF10B981");
                UpdateBrush("NegativeAccentBrush", isDarkMode ? "#FFF87171" : "#FFEF4444");
                UpdateBrush("SectionHeaderBrush", isDarkMode ? "#FFF1F5F9" : "#FF1A1A2E");
                UpdateBrush("ChartBackgroundBrush", isDarkMode ? "#FF111111" : "#FFFFFFFF");
                UpdateBrush("ChartBorderBrush", isDarkMode ? "#FF222222" : "#FFE5E7EB");
                UpdateBrush("ScrollBarTrackBrush", isDarkMode ? "#FF0A0A0A" : "#FFF1F1F1");
                UpdateBrush("ScrollBarThumbBrush", isDarkMode ? "#FF333333" : "#FFC4C4C4");
                UpdateBrush("ScrollBarThumbHoverBrush", isDarkMode ? "#FF4A4A4A" : "#FFA3A3A3");
                UpdateBrush("ScrollBarThumbPressedBrush", isDarkMode ? "#FF666666" : "#FF787878");
                UpdateColor("ShadowColor", isDarkMode ? "#40000000" : "#20000000");

                LayoutRoot.Background = (Brush)Resources["StatsBackgroundBrush"];
                ApplyChartTheme();
                _viewModel.UpdateTheme(isDarkMode);
            });
        }

        private void UpdateColor(string resourceKey, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            Resources[resourceKey] = color;
        }

        private void UpdateBrush(string resourceKey, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            if (Resources[resourceKey] is SolidColorBrush brush)
            {
                if (!brush.IsFrozen)
                {
                    brush.Color = color;
                }
                else
                {
                    var mutable = brush.Clone();
                    mutable.Color = color;
                    Resources[resourceKey] = mutable;
                }
            }
            else
            {
                Resources[resourceKey] = new SolidColorBrush(color);
            }
        }

        private void ApplyChartTheme()
        {
            if (Resources["ChartBackgroundBrush"] is not SolidColorBrush chartBackgroundBrush)
            {
                return;
            }

            if (Resources["ChartBorderBrush"] is not SolidColorBrush chartBorderBrush)
            {
                return;
            }

            _newClientsChart.Background = chartBackgroundBrush;
            _countryChart.Background = chartBackgroundBrush;
            _operatingSystemChart.Background = chartBackgroundBrush;

            _newClientsChart.BorderThickness = new Thickness(0);
            _countryChart.BorderThickness = new Thickness(0);
            _operatingSystemChart.BorderThickness = new Thickness(0);
        }

        private static CartesianChart CreateCartesianChart()
        {
            return new CartesianChart
            {
                Height = 260,
                Padding = new Thickness(4),
                BorderThickness = new Thickness(0)
            };
        }

        private static PieChart CreatePieChart()
        {
            return new PieChart
            {
                Height = 180,
                Padding = new Thickness(4),
                BorderThickness = new Thickness(0)
            };
        }

        private static Binding CreateOneWayBinding(string path)
        {
            return new Binding(path)
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
        }

        private static void Bind(FrameworkElement element, DependencyProperty property, string path)
        {
            element.SetBinding(property, CreateOneWayBinding(path));
        }
    }
}
