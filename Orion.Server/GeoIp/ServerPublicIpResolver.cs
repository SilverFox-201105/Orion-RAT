using System;
using System.Net.Http;
using System.Text.Json;

namespace Orion.Server.GeoIp
{
    /// <summary>
    /// Resolves the operator's own public IP and country so that clients connecting from loopback or
    /// a private (LAN) address can be labelled with the operator's physical location instead of an
    /// "unknown" icon. Internal addresses are never looked up; the operator's egress geo is fetched
    /// directly from <c>https://ipwho.is/</c>.
    /// </summary>
    /// <remarks>
    /// The resolution runs on the server (operator) machine, never on the agent, so it does not
    /// reintroduce an outbound beacon into the client binary. The result is fetched once and cached
    /// for the process lifetime. When the service cannot be reached (no egress, air-gapped dev box)
    /// the caller falls back to the server's configured region, then to the "unknown" icon.
    /// </remarks>
    public static class ServerPublicIpResolver
    {
        private static readonly object Lock = new object();
        private static (string Ip, string CountryCode) _cached;
        private static bool _resolved;

        /// <summary>
        /// Gets the operator's public IP and ISO country code, resolved once and cached for the
        /// process lifetime.
        /// </summary>
        /// <returns>
        /// A tuple with the public IP and two letter country code (e.g. "CN"), each <c>null</c> when
        /// it cannot be determined.
        /// </returns>
        public static (string Ip, string CountryCode) GetOperatorGeo()
        {
            lock (Lock)
            {
                if (_resolved)
                {
                    return _cached;
                }
            }

            var result = ResolveFromIpWhoIs();

            lock (Lock)
            {
                _cached = result;
                _resolved = true;
            }

            return result;
        }

        /// <summary>
        /// Queries <c>https://ipwho.is/</c> for the operator's public IP and its country code.
        /// </summary>
        private static (string Ip, string CountryCode) ResolveFromIpWhoIs()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                    // Synchronous on a server worker thread (no SynchronizationContext) is safe.
                    var json = client.GetStringAsync("https://ipwho.is/").GetAwaiter().GetResult();

                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;

                        if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
                        {
                            return (null, null);
                        }

                        string ip = null;
                        string countryCode = null;

                        if (root.TryGetProperty("ip", out var ipEl))
                        {
                            var ipVal = ipEl.GetString();
                            if (!string.IsNullOrWhiteSpace(ipVal))
                            {
                                ip = ipVal.Trim();
                            }
                        }

                        if (root.TryGetProperty("country_code", out var cc))
                        {
                            var code = cc.GetString();
                            if (!string.IsNullOrWhiteSpace(code) && code.Trim().Length == 2)
                            {
                                countryCode = code.Trim().ToUpperInvariant();
                            }
                        }

                        return (ip, countryCode);
                    }
                }
            }
            catch
            {
                // unable to reach the service or parse the response; caller falls back to the
                // server's configured region, then to the unknown icon.
            }

            return (null, null);
        }
    }
}
