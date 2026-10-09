using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

#nullable enable

namespace Orion.Server.Controls.Wpf
{
    internal sealed class FavoriteToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isFavorite = value is bool flag && flag;
            return isFavorite ? Brushes.Gold : Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
