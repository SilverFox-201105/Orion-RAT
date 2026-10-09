using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Orion.Server.GeoIp;

namespace Orion.Server.Controls.Wpf
{
    /// <summary>
    /// Shared, thread-safe bridge between the WinForms flag <see cref="System.Windows.Forms.ImageList"/>
    /// (loaded from the server resources) and WPF <see cref="ImageSource"/> instances used by the
    /// client list. A single decoded bitmap is cached per flag index so the same flag is never
    /// decoded twice (rows and group headers share the cache).
    /// </summary>
    public static class WpfFlagProvider
    {
        private static System.Windows.Forms.ImageList _flags;
        private static readonly Dictionary<int, ImageSource> _cache = new Dictionary<int, ImageSource>();
        private static readonly object _sync = new object();

        /// <summary>
        /// Hands the provider the flag image list. Call this once, after the image list has been
        /// populated from resources (i.e. right after <c>InitializeComponent</c> in the main form).
        /// </summary>
        public static void Initialize(System.Windows.Forms.ImageList imageList)
        {
            _flags = imageList;
        }

        /// <summary>
        /// Drops all cached flag bitmaps. Call when the underlying image list is rebuilt (e.g. DPI
        /// rescaling) so stale bitmaps are not reused.
        /// </summary>
        public static void ClearCache()
        {
            lock (_sync)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Returns the WPF flag image for the given ISO country code, or <c>null</c> when the code
        /// is unknown / has no flag.
        /// </summary>
        public static ImageSource GetFlag(string countryCode)
        {
            return GetFlagByIndex(CountryFlags.GetImageIndex(countryCode));
        }

        /// <summary>
        /// Returns the WPF flag image for the given flag image-list index, or <c>null</c> for an
        /// out-of-range index.
        /// </summary>
        public static ImageSource GetFlagByIndex(int index)
        {
            if (index < 0 || _flags == null || index >= _flags.Images.Count)
            {
                return null;
            }

            lock (_sync)
            {
                if (_cache.TryGetValue(index, out var cached))
                {
                    return cached;
                }

                ImageSource source;
                try
                {
                    using var ms = new MemoryStream();
                    _flags.Images[index].Save(ms, ImageFormat.Png);
                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(ms.ToArray()))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }

                    bitmap.Freeze();
                    source = bitmap;
                }
                catch
                {
                    source = null;
                }

                _cache[index] = source;
                return source;
            }
        }
    }
}
