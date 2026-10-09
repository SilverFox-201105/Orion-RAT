using System;
using System.Collections.Generic;

namespace Orion.Server.Statistics
{
    public sealed class ClientGeoSnapshot
    {
        public ClientGeoSnapshot(
            int totalClients,
            int mappedClients,
            int unknownClients,
            IReadOnlyList<GeoCountryCount> countries,
            DateTime generatedAtUtc,
            string? errorMessage = null)
        {
            TotalClients = totalClients;
            MappedClients = mappedClients;
            UnknownClients = unknownClients;
            Countries = countries ?? Array.Empty<GeoCountryCount>();
            GeneratedAtUtc = generatedAtUtc;
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
        }

        public int TotalClients { get; }

        public int MappedClients { get; }

        public int UnknownClients { get; }

        public IReadOnlyList<GeoCountryCount> Countries { get; }

        public DateTime GeneratedAtUtc { get; }

        public string? ErrorMessage { get; }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public int UniqueCountryCount => Countries?.Count ?? 0;

        public static ClientGeoSnapshot CreateError(string message)
        {
            return new ClientGeoSnapshot(
                0,
                0,
                0,
                Array.Empty<GeoCountryCount>(),
                DateTime.UtcNow,
                message);
        }
    }

    public sealed class GeoCountryCount
    {
        public GeoCountryCount(string countryCode2, string countryCode3, string name, int count, double share)
        {
            CountryCode2 = string.IsNullOrWhiteSpace(countryCode2) ? "" : countryCode2;
            CountryCode3 = string.IsNullOrWhiteSpace(countryCode3) ? "" : countryCode3;
            Name = string.IsNullOrWhiteSpace(name) ? "未知" : name;
            Count = count;
            Share = share;
        }

        public string CountryCode2 { get; }

        public string CountryCode3 { get; }

        public string Name { get; }

        public int Count { get; }

        public double Share { get; }
    }
}
