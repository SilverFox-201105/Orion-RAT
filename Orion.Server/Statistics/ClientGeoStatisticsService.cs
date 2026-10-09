using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Orion.Server.Persistence;

namespace Orion.Server.Statistics
{
    public static class ClientGeoStatisticsService
    {
        private static readonly IReadOnlyDictionary<string, CountryInfo> CountriesByAlpha2;
        private static readonly IReadOnlyDictionary<string, CountryInfo> CountriesByAlpha3;
        private static readonly IReadOnlyDictionary<string, CountryInfo> CountriesByName;
        private static readonly IReadOnlyDictionary<string, CountryInfo> ManualOverrides;

        static ClientGeoStatisticsService()
        {
            CountriesByAlpha2 = BuildAlpha2Map();
            CountriesByAlpha3 = BuildAlpha3Map(CountriesByAlpha2);
            CountriesByName = BuildNameMap(CountriesByAlpha2);
            ManualOverrides = BuildManualOverrides();
        }

        public static ClientGeoSnapshot CreateSnapshot(IEnumerable<OfflineClientRecord>? records, DateTime? generatedAtUtc = null)
        {
            try
            {
                var list = records?.Where(r => r != null).ToList() ?? new List<OfflineClientRecord>();
                if (list.Count == 0)
                {
                    return new ClientGeoSnapshot(0, 0, 0, Array.Empty<GeoCountryCount>(), generatedAtUtc ?? DateTime.UtcNow);
                }

                var total = list.Count;
                var mappedCount = 0;
                var grouped = new Dictionary<string, CountryAggregate>(StringComparer.OrdinalIgnoreCase);

                foreach (var record in list)
                {
                    var info = ResolveCountry(record.CountryCode, record.Country);
                    if (info == null)
                    {
                        continue;
                    }

                    mappedCount++;
                    var key = info.Value.Alpha3;

                    if (!grouped.TryGetValue(key, out var aggregate))
                    {
                        grouped[key] = new CountryAggregate(info.Value)
                        {
                            DisplayName = !string.IsNullOrWhiteSpace(record.Country) ? record.Country.Trim() : info.Value.EnglishName,
                            Count = 1
                        };
                    }
                    else
                    {
                        aggregate.Count++;
                    }
                }

                var unknown = total - mappedCount;

                var countries = grouped.Values
                    .Select(aggregate => new GeoCountryCount(
                        aggregate.Info.Alpha2,
                        aggregate.Info.Alpha3,
                        aggregate.DisplayName,
                        aggregate.Count,
                        total > 0 ? (double)aggregate.Count / total : 0))
                    .OrderByDescending(c => c.Count)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ClientGeoSnapshot(total, mappedCount, unknown, countries, generatedAtUtc ?? DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                return ClientGeoSnapshot.CreateError(ex.Message);
            }
        }

        private static CountryInfo? ResolveCountry(string? countryCode, string? countryName)
        {
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                var normalized = countryCode.Trim();
                if (TryResolveByCode(normalized, out var byCode))
                {
                    return byCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(countryName))
            {
                var normalizedName = NormalizeName(countryName);
                if (CountriesByName.TryGetValue(normalizedName, out var byName))
                {
                    return byName;
                }

                if (ManualOverrides.TryGetValue(normalizedName, out var manual))
                {
                    return manual;
                }
            }

            return null;
        }

        private static bool TryResolveByCode(string code, out CountryInfo info)
        {
            if (code.Length == 2)
            {
                if (CountriesByAlpha2.TryGetValue(code.ToUpperInvariant(), out info))
                {
                    return true;
                }

                if (ManualOverrides.TryGetValue($"__ALPHA2__{code.ToUpperInvariant()}", out info))
                {
                    return true;
                }
            }
            else if (code.Length == 3)
            {
                if (CountriesByAlpha3.TryGetValue(code.ToUpperInvariant(), out info))
                {
                    return true;
                }

                if (ManualOverrides.TryGetValue($"__ALPHA3__{code.ToUpperInvariant()}", out info))
                {
                    return true;
                }
            }

            info = default;
            return false;
        }

        private static IReadOnlyDictionary<string, CountryInfo> BuildAlpha2Map()
        {
            var map = new Dictionary<string, CountryInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(culture.Name);
                    if (!map.ContainsKey(region.TwoLetterISORegionName))
                    {
                        map[region.TwoLetterISORegionName] = new CountryInfo(region.TwoLetterISORegionName, region.ThreeLetterISORegionName, region.EnglishName);
                    }
                }
                catch
                {
                    // Ignore cultures without associated region information.
                }
            }

            return new ReadOnlyDictionary<string, CountryInfo>(map);
        }

        private static IReadOnlyDictionary<string, CountryInfo> BuildAlpha3Map(IReadOnlyDictionary<string, CountryInfo> alpha2Map)
        {
            var map = new Dictionary<string, CountryInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in alpha2Map.Values)
            {
                if (!map.ContainsKey(entry.Alpha3))
                {
                    map[entry.Alpha3] = entry;
                }
            }

            return new ReadOnlyDictionary<string, CountryInfo>(map);
        }

        private static IReadOnlyDictionary<string, CountryInfo> BuildNameMap(IReadOnlyDictionary<string, CountryInfo> alpha2Map)
        {
            var map = new Dictionary<string, CountryInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in alpha2Map.Values)
            {
                var englishName = NormalizeName(entry.EnglishName);
                if (!map.ContainsKey(englishName))
                {
                    map[englishName] = entry;
                }
            }

            return new ReadOnlyDictionary<string, CountryInfo>(map);
        }

        private static IReadOnlyDictionary<string, CountryInfo> BuildManualOverrides()
        {
            var map = new Dictionary<string, CountryInfo>(StringComparer.OrdinalIgnoreCase)
            {
                { NormalizeName("United States"), new CountryInfo("US", "USA", "United States") },
                { NormalizeName("United States of America"), new CountryInfo("US", "USA", "United States") },
                { NormalizeName("United Kingdom"), new CountryInfo("GB", "GBR", "United Kingdom") },
                { NormalizeName("Great Britain"), new CountryInfo("GB", "GBR", "United Kingdom") },
                { NormalizeName("Russia"), new CountryInfo("RU", "RUS", "Russia") },
                { NormalizeName("South Korea"), new CountryInfo("KR", "KOR", "South Korea") },
                { NormalizeName("North Korea"), new CountryInfo("KP", "PRK", "North Korea") },
                { NormalizeName("Viet Nam"), new CountryInfo("VN", "VNM", "Vietnam") },
                { NormalizeName("Czech Republic"), new CountryInfo("CZ", "CZE", "Czech Republic") },
                { NormalizeName("Ivory Coast"), new CountryInfo("CI", "CIV", "Côte d'Ivoire") },
                { NormalizeName("Bolivia"), new CountryInfo("BO", "BOL", "Bolivia") },
                { NormalizeName("Tanzania"), new CountryInfo("TZ", "TZA", "Tanzania") },
                { NormalizeName("Syria"), new CountryInfo("SY", "SYR", "Syria") },
                { NormalizeName("Moldova"), new CountryInfo("MD", "MDA", "Moldova") },
                { NormalizeName("Macau"), new CountryInfo("MO", "MAC", "Macau") },
                { NormalizeName("Hong Kong"), new CountryInfo("HK", "HKG", "Hong Kong") },
                { NormalizeName("Taiwan"), new CountryInfo("TW", "TWN", "Taiwan") },
                { NormalizeName("Cape Verde"), new CountryInfo("CV", "CPV", "Cabo Verde") },
                { NormalizeName("Laos"), new CountryInfo("LA", "LAO", "Laos") },
                { NormalizeName("Kosovo"), new CountryInfo("XK", "XKX", "Kosovo") },
                { NormalizeName("Palestine"), new CountryInfo("PS", "PSE", "Palestine") },
                { NormalizeName("Vatican"), new CountryInfo("VA", "VAT", "Vatican City") },
                { "__ALPHA2__XK", new CountryInfo("XK", "XKX", "Kosovo") },
                { "__ALPHA3__XKX", new CountryInfo("XK", "XKX", "Kosovo") }
            };

            return new ReadOnlyDictionary<string, CountryInfo>(map);
        }

        private static string NormalizeName(string value)
        {
            return new string(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private readonly struct CountryInfo
        {
            public CountryInfo(string alpha2, string alpha3, string englishName)
            {
                Alpha2 = alpha2;
                Alpha3 = alpha3;
                EnglishName = englishName;
            }

            public string Alpha2 { get; }

            public string Alpha3 { get; }

            public string EnglishName { get; }
        }

        private sealed class CountryAggregate
        {
            public CountryAggregate(CountryInfo info)
            {
                Info = info;
                DisplayName = info.EnglishName;
            }

            public CountryInfo Info { get; }

            public string DisplayName { get; set; }

            public int Count { get; set; }
        }
    }
}
