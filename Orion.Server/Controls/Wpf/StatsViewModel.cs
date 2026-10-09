using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Orion.Server.Statistics;
using SkiaSharp;

#nullable enable

namespace Orion.Server.Controls.Wpf
{
    public sealed class StatsViewModel : INotifyPropertyChanged
    {
        private static readonly SKColor[] LightPalette =
        {
            SKColor.Parse("#3B82F6"),
            SKColor.Parse("#10B981"),
            SKColor.Parse("#F59E0B"),
            SKColor.Parse("#8B5CF6"),
            SKColor.Parse("#EC4899"),
            SKColor.Parse("#06B6D4"),
            SKColor.Parse("#84CC16"),
            SKColor.Parse("#F97316")
        };

        private static readonly SKColor[] DarkPalette =
        {
            SKColor.Parse("#60A5FA"),
            SKColor.Parse("#34D399"),
            SKColor.Parse("#FBBF24"),
            SKColor.Parse("#A78BFA"),
            SKColor.Parse("#F472B6"),
            SKColor.Parse("#22D3EE"),
            SKColor.Parse("#A3E635"),
            SKColor.Parse("#FB923C")
        };

        private readonly ObservableCollection<StatCardViewModel> _statCards = new()
        {
            new StatCardViewModel("总客户端数"),
            new StatCardViewModel("当前在线"),
            new StatCardViewModel("离线"),
            new StatCardViewModel("近7天新增")
        };

        private ISeries[] _newClientsSeries = Array.Empty<ISeries>();
        private Axis[] _newClientsXAxes = Array.Empty<Axis>();
        private Axis[] _newClientsYAxes = Array.Empty<Axis>();
        private ISeries[] _clientsByCountrySeries = Array.Empty<ISeries>();
        private ISeries[] _clientsByOsSeries = Array.Empty<ISeries>();
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string _lastUpdated = "";
        private bool _isDarkMode;
        private ClientStatisticsSnapshot? _lastSnapshot;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<StatCardViewModel> StatCards { get; }

        public ISeries[] NewClientsSeries
        {
            get => _newClientsSeries;
            private set => SetField(ref _newClientsSeries, value);
        }

        public Axis[] NewClientsXAxes
        {
            get => _newClientsXAxes;
            private set => SetField(ref _newClientsXAxes, value);
        }

        public Axis[] NewClientsYAxes
        {
            get => _newClientsYAxes;
            private set => SetField(ref _newClientsYAxes, value);
        }

        public ISeries[] ClientsByCountrySeries
        {
            get => _clientsByCountrySeries;
            private set => SetField(ref _clientsByCountrySeries, value);
        }

        public ISeries[] ClientsByOperatingSystemSeries
        {
            get => _clientsByOsSeries;
            private set => SetField(ref _clientsByOsSeries, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetField(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                if (SetField(ref _hasError, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool IsContentVisible => !IsLoading && !HasError;

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetField(ref _errorMessage, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            private set => SetField(ref _lastUpdated, value);
        }

        public StatsViewModel()
        {
            StatCards = new ReadOnlyObservableCollection<StatCardViewModel>(_statCards);
        }

        public void SetLoading()
        {
            ErrorMessage = null;
            HasError = false;
            IsLoading = true;
        }

        public void SetError(string message)
        {
            _lastSnapshot = null;
            ErrorMessage = message;
            HasError = true;
            IsLoading = false;
            LastUpdated = string.Empty;
            ClearSeries();
        }

        public void UpdateSnapshot(ClientStatisticsSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            ErrorMessage = snapshot.ErrorMessage;
            HasError = snapshot.HasError;
            IsLoading = false;

            if (snapshot.HasError)
            {
                LastUpdated = string.Empty;
                ClearSeries();
                return;
            }

            LastUpdated = $"更新于 {snapshot.GeneratedAtUtc.ToLocalTime():g}";
            UpdateCards(snapshot);
            BuildSeries();
        }

        public void UpdateTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            BuildSeries();
        }

        private void UpdateCards(ClientStatisticsSnapshot snapshot)
        {
            _statCards[0].Update(snapshot.TotalClients.ToString("N0"), "已记录的唯一客户端");
            _statCards[1].Update(snapshot.OnlineClients.ToString("N0"), "当前在线");
            _statCards[2].Update(snapshot.OfflineClients.ToString("N0"), "已记录但离线");
            _statCards[3].Update(snapshot.NewClientsLast7Days.ToString("N0"), "近 7 天新增");
        }

        private void BuildSeries()
        {
            if (_lastSnapshot == null || _lastSnapshot.HasError)
            {
                ClearSeries();
                return;
            }

            var accent = GetAccentColor();
            var axisText = GetAxisTextColor();
            var separator = GetSeparatorColor();

            var dailyValues = _lastSnapshot.NewClientsByDay.Select(d => d.Count).ToArray();
            var labels = _lastSnapshot.NewClientsByDay.Select(d => d.Date.ToString("M月d日")).ToArray();

            NewClientsSeries = new ISeries[]
            {
                CreateColumnSeries(dailyValues, accent, axisText)
            };

            NewClientsXAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(axisText),
                    Name = "日期",
                    NamePaint = new SolidColorPaint(axisText),
                    TextSize = 13,
                    Padding = new Padding(10, 0, 10, 0),
                    SeparatorsPaint = new SolidColorPaint(separator) { StrokeThickness = 1 }
                }
            };

            NewClientsYAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(axisText),
                    TextSize = 13,
                    Name = "客户端数",
                    NamePaint = new SolidColorPaint(axisText),
                    MinLimit = 0,
                    SeparatorsPaint = new SolidColorPaint(separator) { StrokeThickness = 1 }
                }
            };

            ClientsByCountrySeries = BuildPieSeries(_lastSnapshot.ClientsByCountry);
            ClientsByOperatingSystemSeries = BuildPieSeries(_lastSnapshot.ClientsByOperatingSystem);
        }

        private ISeries[] BuildPieSeries(IReadOnlyCollection<CategoryCount> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return Array.Empty<ISeries>();
            }

            var palette = _isDarkMode ? DarkPalette : LightPalette;
            var axisText = GetAxisTextColor();
            var stroke = GetSeparatorColor();

            var series = categories
                .Select((entry, index) =>
                {
                    var pieSeries = new PieSeries<int>
                    {
                        Values = new[] { entry.Count },
                        Name = entry.Label,
                        Fill = new SolidColorPaint(palette[index % palette.Length]),
                        Stroke = new SolidColorPaint(stroke) { StrokeThickness = 2f },
                        DataLabelsPaint = new SolidColorPaint(axisText),
                        DataLabelsSize = 11,
                        DataLabelsPosition = PolarLabelsPosition.Outer,
                        DataLabelsFormatter = point =>
                        {
                            return entry.Share >= 0.05
                                ? $"{entry.Label} ({entry.Share:P0})"
                                : "";
                        },
                        InnerRadius = 40
                    };

                    return pieSeries;
                })
                .Cast<ISeries>()
                .ToArray();

            return series;
        }

        private static ColumnSeries<int> CreateColumnSeries(int[] values, SKColor accent, SKColor axisText)
        {
            var series = new ColumnSeries<int>
            {
                Values = values,
                Fill = new SolidColorPaint(accent),
                Stroke = null,
                Rx = 4,
                Ry = 4,
                MaxBarWidth = 32,
                Padding = 8
            };

            if (values.Length <= 10 && values.Any(v => v > 0))
            {
                series.DataLabelsPaint = new SolidColorPaint(axisText);
                series.DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top;
                series.DataLabelsSize = 12;
                series.DataLabelsFormatter = point => point.Model > 0 ? point.Model.ToString("N0") : "";
            }

            return series;
        }

        private void ClearSeries()
        {
            NewClientsSeries = Array.Empty<ISeries>();
            NewClientsXAxes = Array.Empty<Axis>();
            NewClientsYAxes = Array.Empty<Axis>();
            ClientsByCountrySeries = Array.Empty<ISeries>();
            ClientsByOperatingSystemSeries = Array.Empty<ISeries>();
        }

        private SKColor GetAccentColor() => _isDarkMode ? SKColor.Parse("#60A5FA") : SKColor.Parse("#3B82F6");

        private SKColor GetAxisTextColor() => _isDarkMode ? SKColor.Parse("#CBD5E1") : SKColor.Parse("#374151");

        private SKColor GetSeparatorColor() => _isDarkMode ? SKColor.Parse("#2A2D3A") : SKColor.Parse("#E5E7EB");

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class StatCardViewModel : INotifyPropertyChanged
    {
        private string _value = "0";
        private string _subtitle = string.Empty;

        public StatCardViewModel(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public string Value
        {
            get => _value;
            private set => SetField(ref _value, value);
        }

        public string Subtitle
        {
            get => _subtitle;
            private set => SetField(ref _subtitle, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update(string value, string subtitle)
        {
            Value = value;
            Subtitle = subtitle;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}
