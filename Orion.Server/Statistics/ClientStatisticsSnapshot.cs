using System;
using System.Collections.Generic;
using System.Linq;

namespace Orion.Server.Statistics
{
    public sealed class ClientStatisticsSnapshot
    {
        public ClientStatisticsSnapshot(
            int totalClients,
            int onlineClients,
            int offlineClients,
            int newClientsLast7Days,
            IReadOnlyList<DailyCount> newClientsByDay,
            IReadOnlyList<CategoryCount> clientsByCountry,
            IReadOnlyList<CategoryCount> clientsByOperatingSystem,
            IReadOnlyList<CategoryCount> topTags,
            DateTime generatedAtUtc,
            string? errorMessage = null)
        {
            TotalClients = totalClients;
            OnlineClients = onlineClients;
            OfflineClients = offlineClients;
            NewClientsLast7Days = newClientsLast7Days;
            NewClientsByDay = newClientsByDay ?? Array.Empty<DailyCount>();
            ClientsByCountry = clientsByCountry ?? Array.Empty<CategoryCount>();
            ClientsByOperatingSystem = clientsByOperatingSystem ?? Array.Empty<CategoryCount>();
            TopTags = topTags ?? Array.Empty<CategoryCount>();
            GeneratedAtUtc = generatedAtUtc;
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
        }

        public int TotalClients { get; }

        public int OnlineClients { get; }

        public int OfflineClients { get; }

        public int NewClientsLast7Days { get; }

        public IReadOnlyList<DailyCount> NewClientsByDay { get; }

        public IReadOnlyList<CategoryCount> ClientsByCountry { get; }

        public IReadOnlyList<CategoryCount> ClientsByOperatingSystem { get; }

        public IReadOnlyList<CategoryCount> TopTags { get; }

        public DateTime GeneratedAtUtc { get; }

        public string? ErrorMessage { get; }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public static ClientStatisticsSnapshot CreateError(string message)
        {
            return new ClientStatisticsSnapshot(
                0,
                0,
                0,
                0,
                Array.Empty<DailyCount>(),
                Array.Empty<CategoryCount>(),
                Array.Empty<CategoryCount>(),
                Array.Empty<CategoryCount>(),
                DateTime.UtcNow,
                message);
        }
    }

    public sealed class DailyCount
    {
        public DailyCount(DateTime date, int count)
        {
            Date = date;
            Count = count;
        }

        public DateTime Date { get; }

        public int Count { get; }
    }

    public sealed class CategoryCount
    {
        public CategoryCount(string label, int count, double share)
        {
            Label = string.IsNullOrWhiteSpace(label) ? "未知" : label;
            Count = count;
            Share = share;
        }

        public string Label { get; }

        public int Count { get; }

        public double Share { get; }

        public override string ToString()
        {
            return $"{Label} ({Count})";
        }
    }
}
