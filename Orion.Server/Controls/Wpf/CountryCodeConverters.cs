using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Orion.Server.GeoIp;

namespace Orion.Server.Controls.Wpf
{
    /// <summary>
    /// Converts an ISO 3166-1 alpha-2 country code into the matching WPF flag image, or
    /// <c>null</c> when no flag exists (so the bound <see cref="System.Windows.Controls.Image"/>
    /// can collapse itself).
    /// </summary>
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public sealed class CountryCodeToFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var code = value as string;
            return WpfFlagProvider.GetFlag(code);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts an ISO 3166-1 alpha-2 country code into a human readable country name. Unknown /
    /// unresolved codes fall back to "未知".
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public sealed class CountryCodeToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var code = value as string;
            var name = IpCountryLookup.GetCountryName(code);
            return string.IsNullOrWhiteSpace(name) ? "未知" : name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Used by the tag-grouping header. A client tag is shown verbatim; an empty / null tag
    /// falls back to "未标记" so the group header is never blank.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public sealed class TagToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tag = value as string;
            return string.IsNullOrWhiteSpace(tag) ? "未标记" : tag;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
