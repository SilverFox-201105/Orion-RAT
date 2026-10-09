using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orion.Server.Persistence;

namespace Orion.Server.Statistics
{
    public static class ClientStatisticsService
    {
        private const int DefaultHistoryDays = 14;
        private const int MaxPieSegments = 7;
        private const int MaxTagRows = 10;

        public static ClientStatisticsSnapshot CreateSnapshot(IEnumerable<OfflineClientRecord>? records)
        {
            try
            {
                var list = records?.ToList() ?? new List<OfflineClientRecord>();
                var nowUtc = DateTime.UtcNow;
                var total = list.Count;
                var online = list.Count(r => r.IsOnline);
                var offline = total - online;

                var dayRange = BuildDayRange(nowUtc.Date, DefaultHistoryDays);
                var firstSeenGroups = list
                    .Where(r => r.FirstSeenUtc.HasValue)
                    .GroupBy(r => r.FirstSeenUtc!.Value.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var daily = dayRange
                    .Select(date => new DailyCount(date, firstSeenGroups.TryGetValue(date, out var value) ? value : 0))
                    .ToList();

                var sevenDayThreshold = nowUtc.Date.AddDays(-6);
                var newSevenDayCount = daily.Where(d => d.Date >= sevenDayThreshold).Sum(d => d.Count);

                var countryStats = BuildCategoryList(list, r => Normalize(r.Country), total, MaxPieSegments);
                var osStats = BuildCategoryList(list, r => Normalize(r.OperatingSystem), total, MaxPieSegments);
                var tagStats = BuildCategoryList(list, r => string.IsNullOrWhiteSpace(r.Tag) ? "(none)" : r.Tag, total, MaxTagRows);

                return new ClientStatisticsSnapshot(
                    total,
                    online,
                    offline,
                    newSevenDayCount,
                    daily,
                    countryStats,
                    osStats,
                    tagStats,
                    nowUtc);
            }
            catch (Exception ex)
            {
                return ClientStatisticsSnapshot.CreateError(ex.Message);
            }
        }

        private static IReadOnlyList<CategoryCount> BuildCategoryList(IEnumerable<OfflineClientRecord> source, Func<OfflineClientRecord, string> selector, int total, int maxItems)
        {
            var grouped = source
                .GroupBy(selector)
                .Select(g => new { Label = string.IsNullOrWhiteSpace(g.Key) ? "未知" : g.Key.Trim(), Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Label)
                .ToList();

            if (grouped.Count == 0)
            {
                return Array.Empty<CategoryCount>();
            }

            if (maxItems <= 0 || grouped.Count <= maxItems)
            {
                return grouped
                    .Select(g => new CategoryCount(g.Label, g.Count, total > 0 ? (double)g.Count / total : 0))
                    .ToList();
            }

            var top = grouped.Take(maxItems - 1).ToList();
            var otherCount = grouped.Skip(maxItems - 1).Sum(g => g.Count);

            var result = top
                .Select(g => new CategoryCount(g.Label, g.Count, total > 0 ? (double)g.Count / total : 0))
                .ToList();

            if (otherCount > 0)
            {
                result.Add(new CategoryCount("Other", otherCount, total > 0 ? (double)otherCount / total : 0));
            }

            return result;
        }

        private static IReadOnlyList<DateTime> BuildDayRange(DateTime endDateInclusive, int days)
        {
            if (days <= 0)
            {
                return new[] { endDateInclusive };
            }

            var start = endDateInclusive.AddDays(-(days - 1));
            var result = new List<DateTime>(days);
            for (var d = start; d <= endDateInclusive; d = d.AddDays(1))
            {
                result.Add(d);
            }

            return result;
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "未知";
            }

            return value.Trim();
        }
    }
}
