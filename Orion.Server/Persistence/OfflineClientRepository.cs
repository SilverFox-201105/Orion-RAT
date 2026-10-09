using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Orion.Server.Networking;

namespace Orion.Server.Persistence
{
    public static class OfflineClientRepository
    {
        private static readonly object InitLock = new object();
        private static readonly string OrionStuffDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrionStuff");
        private static readonly string DatabasePath = Path.Combine(OrionStuffDirectory, "clients.db");
        private static readonly string ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (_initialized)
                {
                    return;
                }

                Directory.CreateDirectory(OrionStuffDirectory);

                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");
                ExecuteNonQuery(connection, "PRAGMA synchronous=NORMAL;");

                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Clients (
                        ClientId TEXT PRIMARY KEY,
                        Username TEXT,
                        PcName TEXT,
                        UserAtPc TEXT,
                        PublicIP TEXT,
                        Tag TEXT,
                        OperatingSystem TEXT,
                        Version TEXT,
                        Country TEXT,
                        CountryCode TEXT,
                        AccountType TEXT,
                        ImageIndex INTEGER,
                        FirstSeenUtc TEXT,
                        LastSeenUtc TEXT,
                        IsOnline INTEGER NOT NULL DEFAULT 0
                    );");

                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Clients_IsOnline_LastSeen ON Clients(IsOnline, LastSeenUtc DESC);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Clients_UserAtPc ON Clients(UserAtPc);");

                _initialized = true;
            }
        }

        public static void ResetOnlineState()
        {
            Initialize();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            ExecuteNonQuery(connection, "UPDATE Clients SET IsOnline = 0;");
        }

        public static void UpsertClient(Client client)
        {
            if (client?.Value?.Id == null)
            {
                return;
            }

            Initialize();

            var nowUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Clients (
                    ClientId,
                    Username,
                    PcName,
                    UserAtPc,
                    PublicIP,
                    Tag,
                    OperatingSystem,
                    Version,
                    Country,
                    CountryCode,
                    AccountType,
                    ImageIndex,
                    FirstSeenUtc,
                    LastSeenUtc,
                    IsOnline
                ) VALUES (
                    $id,
                    $username,
                    $pcName,
                    $userAtPc,
                    $publicIp,
                    $tag,
                    $os,
                    $version,
                    $country,
                    $countryCode,
                    $accountType,
                    $imageIndex,
                    $firstSeen,
                    $lastSeen,
                    1
                )
                ON CONFLICT(ClientId) DO UPDATE SET
                    Username = excluded.Username,
                    PcName = excluded.PcName,
                    UserAtPc = excluded.UserAtPc,
                    PublicIP = excluded.PublicIP,
                    Tag = excluded.Tag,
                    OperatingSystem = excluded.OperatingSystem,
                    Version = excluded.Version,
                    Country = excluded.Country,
                    CountryCode = excluded.CountryCode,
                    AccountType = excluded.AccountType,
                    ImageIndex = excluded.ImageIndex,
                    LastSeenUtc = excluded.LastSeenUtc,
                    IsOnline = 1,
                    FirstSeenUtc = COALESCE(Clients.FirstSeenUtc, excluded.FirstSeenUtc);
            ";

            AddParameter(command, "$id", client.Value.Id);
            AddParameter(command, "$username", client.Value.Username);
            AddParameter(command, "$pcName", client.Value.PcName);
            AddParameter(command, "$userAtPc", client.Value.UserAtPc);
            AddParameter(command, "$publicIp", client.Value.PublicIP ?? client.EndPoint?.Address?.ToString());
            AddParameter(command, "$tag", client.Value.Tag);
            AddParameter(command, "$os", client.Value.OperatingSystem);
            AddParameter(command, "$version", client.Value.Version);
            AddParameter(command, "$country", client.Value.Country);
            AddParameter(command, "$countryCode", client.Value.CountryCode);
            AddParameter(command, "$accountType", client.Value.AccountType);
            AddParameter(command, "$imageIndex", client.Value.ImageIndex);
            AddParameter(command, "$firstSeen", nowUtc);
            AddParameter(command, "$lastSeen", nowUtc);

            command.ExecuteNonQuery();
        }

        public static void MarkClientOffline(Client client)
        {
            if (client?.Value?.Id == null)
            {
                return;
            }

            Initialize();
            var nowUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Clients
                SET IsOnline = 0,
                    LastSeenUtc = $lastSeen
                WHERE ClientId = $id;
            ";

            AddParameter(command, "$id", client.Value.Id);
            AddParameter(command, "$lastSeen", nowUtc);

            command.ExecuteNonQuery();
        }

        public static IReadOnlyList<OfflineClientRecord> GetClientsByOnlineState(bool isOnline)
        {
            Initialize();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ClientId,
                    Username,
                    PcName,
                    UserAtPc,
                    PublicIP,
                    Tag,
                    OperatingSystem,
                    Version,
                    Country,
                    CountryCode,
                    AccountType,
                    ImageIndex,
                    FirstSeenUtc,
                    LastSeenUtc,
                    IsOnline
                FROM Clients
                WHERE IsOnline = $isOnline
                ORDER BY
                    CASE WHEN LastSeenUtc IS NULL THEN 1 ELSE 0 END,
                    LastSeenUtc DESC,
                    UserAtPc;
            ";

            AddParameter(command, "$isOnline", isOnline ? 1 : 0);

            return ReadClients(command);
        }

        public static IReadOnlyList<OfflineClientRecord> GetAllClients()
        {
            Initialize();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ClientId,
                    Username,
                    PcName,
                    UserAtPc,
                    PublicIP,
                    Tag,
                    OperatingSystem,
                    Version,
                    Country,
                    CountryCode,
                    AccountType,
                    ImageIndex,
                    FirstSeenUtc,
                    LastSeenUtc,
                    IsOnline
                FROM Clients
                ORDER BY
                    CASE WHEN LastSeenUtc IS NULL THEN 1 ELSE 0 END,
                    LastSeenUtc DESC,
                    UserAtPc;
            ";

            return ReadClients(command);
        }

        public static void ClearOfflineClients()
        {
            Initialize();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            ExecuteNonQuery(connection, "DELETE FROM Clients WHERE IsOnline = 0;");
        }

        public static void RemoveOfflineClients(IEnumerable<string> clientIds)
        {
            if (clientIds == null)
            {
                return;
            }

            var ids = clientIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            Initialize();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var id in ids)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM Clients WHERE ClientId = $id;";
                    AddParameter(command, "$id", id);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static IReadOnlyList<OfflineClientRecord> ReadClients(SqliteCommand command)
        {
            var results = new List<OfflineClientRecord>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var record = new OfflineClientRecord
                {
                    ClientId = reader.GetString(0),
                    Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PcName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    UserAtPc = reader.IsDBNull(3) ? null : reader.GetString(3),
                    PublicIP = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Tag = reader.IsDBNull(5) ? null : reader.GetString(5),
                    OperatingSystem = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Version = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Country = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CountryCode = reader.IsDBNull(9) ? null : reader.GetString(9),
                    AccountType = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ImageIndex = reader.IsDBNull(11) ? -1 : reader.GetInt32(11),
                    FirstSeenUtc = ParseDate(reader, 12),
                    LastSeenUtc = ParseDate(reader, 13),
                    IsOnline = reader.IsDBNull(14) ? false : reader.GetInt32(14) == 1
                };

                results.Add(record);
            }

            return results;
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static void AddParameter(SqliteCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static DateTime? ParseDate(SqliteDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var value = reader.GetString(index);
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
            {
                return result.ToUniversalTime();
            }

            return null;
        }
    }

    public sealed class OfflineClientRecord
    {
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string PcName { get; set; }
        public string UserAtPc { get; set; }
        public string PublicIP { get; set; }
        public string Tag { get; set; }
        public string OperatingSystem { get; set; }
        public string Version { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string AccountType { get; set; }
        public int ImageIndex { get; set; }
        public DateTime? FirstSeenUtc { get; set; }
        public DateTime? LastSeenUtc { get; set; }
        public bool IsOnline { get; set; }
        public string Nickname { get; set; }
    }
}
