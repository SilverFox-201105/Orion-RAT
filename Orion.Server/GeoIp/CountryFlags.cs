using System;
using System.Collections.Generic;

namespace Orion.Server.GeoIp
{
    /// <summary>
    /// Maps an ISO 3166-1 alpha-2 country code to the index of the matching flag image in the
    /// server side flag image list.
    /// </summary>
    public static class CountryFlags
    {
        /// <summary>
        /// Index of the "question mark" image used when a country has no flag or is unknown.
        /// </summary>
        public const int UnknownImageIndex = 247;

        /// <summary>
        /// The flag image keys, in the exact order in which they are stored in the server resources.
        /// </summary>
        private static readonly string[] ImageList =
        {
            "ad", "ae", "af", "ag", "ai", "al",
            "am", "an", "ao", "ar", "as", "at", "au", "aw", "ax", "az", "ba",
            "bb", "bd", "be", "bf", "bg", "bh", "bi", "bj", "bm", "bn", "bo",
            "br", "bs", "bt", "bv", "bw", "by", "bz", "ca", "catalonia", "cc",
            "cd", "cf", "cg", "ch", "ci", "ck", "cl", "cm", "cn", "co", "cr",
            "cs", "cu", "cv", "cx", "cy", "cz", "de", "dj", "dk", "dm", "do",
            "dz", "ec", "ee", "eg", "eh", "england", "er", "es", "et",
            "europeanunion", "fam", "fi", "fj", "fk", "fm", "fo", "fr", "ga",
            "gb", "gd", "ge", "gf", "gh", "gi", "gl", "gm", "gn", "gp", "gq",
            "gr", "gs", "gt", "gu", "gw", "gy", "hk", "hm", "hn", "hr", "ht",
            "hu", "id", "ie", "il", "in", "io", "iq", "ir", "is", "it", "jm",
            "jo", "jp", "ke", "kg", "kh", "ki", "km", "kn", "kp", "kr", "kw",
            "ky", "kz", "la", "lb", "lc", "li", "lk", "lr", "ls", "lt", "lu",
            "lv", "ly", "ma", "mc", "md", "me", "mg", "mh", "mk", "ml", "mm",
            "mn", "mo", "mp", "mq", "mr", "ms", "mt", "mu", "mv", "mw", "mx",
            "my", "mz", "na", "nc", "ne", "nf", "ng", "ni", "nl", "no", "np",
            "nr", "nu", "nz", "om", "pa", "pe", "pf", "pg", "ph", "pk", "pl",
            "pm", "pn", "pr", "ps", "pt", "pw", "py", "qa", "re", "ro", "rs",
            "ru", "rw", "sa", "sb", "sc", "scotland", "sd", "se", "sg", "sh",
            "si", "sj", "sk", "sl", "sm", "sn", "so", "sr", "st", "sv", "sy",
            "sz", "tc", "td", "tf", "tg", "th", "tj", "tk", "tl", "tm", "tn",
            "to", "tr", "tt", "tv", "tw", "tz", "ua", "ug", "um", "us", "uy",
            "uz", "va", "vc", "ve", "vg", "vi", "vn", "vu", "wales", "wf",
            "ws", "ye", "yt", "za", "zm", "zw"
        };

        private static readonly Dictionary<string, int> IndexByName = BuildIndex();

        private static Dictionary<string, int> BuildIndex()
        {
            var map = new Dictionary<string, int>(ImageList.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ImageList.Length; i++)
            {
                map[ImageList[i]] = i;
            }
            return map;
        }

        /// <summary>
        /// Resolves the flag image index for the given country code.
        /// </summary>
        /// <param name="countryCode">The two letter country code.</param>
        /// <returns>The image index, or <see cref="UnknownImageIndex"/> when unknown.</returns>
        public static int GetImageIndex(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return UnknownImageIndex;
            }

            int index;
            return IndexByName.TryGetValue(countryCode.Trim(), out index) ? index : UnknownImageIndex;
        }
    }
}
