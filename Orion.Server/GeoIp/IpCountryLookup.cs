using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Orion.Server.GeoIp
{
    /// <summary>
    /// Resolves the country of an IPv4 address from a locally embedded range table built from the
    /// RIR delegation statistics files.
    /// </summary>
    /// <remarks>
    /// The table removes any need for the client (or the server) to query a third party geolocation
    /// service: the address is already known from the socket and the lookup is a binary search over
    /// an in-memory array, so it neither adds latency to the handshake nor generates outbound
    /// traffic that would disclose the operation to a third party.
    /// </remarks>
    public sealed class IpCountryLookup
    {
        private const byte Version = 1;
        private const string TableFileName = "ipcountry.dat";
        private const string UnknownCountryCode = "??";

        private readonly string[] _countries;
        private readonly uint[] _starts;
        private readonly ushort[] _countryIndexes;

        private static IpCountryLookup _instance;
        private static readonly object InstanceLock = new object();

        private IpCountryLookup(string[] countries, uint[] starts, ushort[] countryIndexes)
        {
            _countries = countries;
            _starts = starts;
            _countryIndexes = countryIndexes;
        }

        /// <summary>
        /// Gets the shared lookup instance, or <c>null</c> when no table could be loaded.
        /// </summary>
        public static IpCountryLookup Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = TryLoad();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets whether a usable table is loaded.
        /// </summary>
        public bool IsAvailable
        {
            get { return _starts != null && _starts.Length > 0; }
        }

        /// <summary>
        /// Tries to resolve the country code of the given address.
        /// </summary>
        /// <param name="address">The address to look up.</param>
        /// <param name="countryCode">The resolved two letter country code.</param>
        /// <returns><c>true</c> when the address could be attributed to a country.</returns>
        public bool TryGetCountry(IPAddress address, out string countryCode)
        {
            countryCode = null;

            if (!IsAvailable)
            {
                return false;
            }

            uint value;
            if (!TryToUInt32(address, out value))
            {
                return false;
            }

            int low = 0;
            int high = _starts.Length - 1;
            int found = -1;

            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (_starts[middle] <= value)
                {
                    found = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            if (found < 0)
            {
                return false;
            }

            var code = _countries[_countryIndexes[found]];
            if (string.IsNullOrEmpty(code) || code == UnknownCountryCode)
            {
                return false;
            }

            countryCode = code;
            return true;
        }

        /// <summary>
        /// Tries to resolve the country code of the given address string.
        /// </summary>
        public bool TryGetCountry(string ipAddress, out string countryCode)
        {
            countryCode = null;

            IPAddress address;
            if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out address))
            {
                return false;
            }

            return TryGetCountry(address, out countryCode);
        }

        /// <summary>
        /// Loads the table from the application directory, falling back to the embedded resource.
        /// </summary>
        public static IpCountryLookup TryLoad()
        {
            try
            {
                var onDisk = Path.Combine(AppContext.BaseDirectory, "GeoIp", TableFileName);
                if (File.Exists(onDisk))
                {
                    var parsed = TryParse(File.ReadAllBytes(onDisk));
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
                // fall through to the embedded resource
            }

            return TryLoadEmbedded();
        }

        private static IpCountryLookup TryLoadEmbedded()
        {
            try
            {
                var assembly = typeof(IpCountryLookup).Assembly;
                foreach (var name in assembly.GetManifestResourceNames())
                {
                    if (!name.EndsWith(TableFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (var stream = assembly.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                        {
                            continue;
                        }

                        using (var buffer = new MemoryStream())
                        {
                            stream.CopyTo(buffer);
                            var parsed = TryParse(buffer.ToArray());
                            if (parsed != null)
                            {
                                return parsed;
                            }
                        }
                    }
                }
            }
            catch
            {
                // no table available
            }

            return null;
        }

        /// <summary>
        /// Parses the binary table format written by build_ip_country_table.py.
        /// </summary>
        private static IpCountryLookup TryParse(byte[] data)
        {
            if (data == null || data.Length < 11)
            {
                return null;
            }

            try
            {
                using (var stream = new MemoryStream(data, false))
                using (var reader = new BinaryReader(stream))
                {
                    var magic = reader.ReadBytes(4);
                    if (magic.Length != 4 || magic[0] != (byte)'I' || magic[1] != (byte)'P'
                        || magic[2] != (byte)'C' || magic[3] != (byte)'T')
                    {
                        return null;
                    }

                    if (reader.ReadByte() != Version)
                    {
                        return null;
                    }

                    int countryCount = reader.ReadUInt16();
                    if (countryCount == 0 || countryCount > 1024)
                    {
                        return null;
                    }

                    var countries = new string[countryCount];
                    for (int i = 0; i < countryCount; i++)
                    {
                        countries[i] = Encoding.ASCII.GetString(reader.ReadBytes(2));
                    }

                    long entryCount = reader.ReadUInt32();
                    long remaining = data.Length - stream.Position;
                    if (entryCount <= 0 || entryCount * 6 > remaining)
                    {
                        return null;
                    }

                    var starts = new uint[entryCount];
                    var indexes = new ushort[entryCount];
                    for (long i = 0; i < entryCount; i++)
                    {
                        starts[i] = reader.ReadUInt32();
                        indexes[i] = reader.ReadUInt16();

                        if (indexes[i] >= countryCount)
                        {
                            return null;
                        }
                    }

                    return new IpCountryLookup(countries, starts, indexes);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an address to its 32 bit representation.
        /// </summary>
        private static bool TryToUInt32(IPAddress address, out uint value)
        {
            value = 0;

            if (address == null)
            {
                return false;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var bytes = address.GetAddressBytes();
            value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            return true;
        }

        /// <summary>
        /// Gets the English display name of a country code.
        /// </summary>
        /// <param name="countryCode">The two letter country code.</param>
        /// <returns>The country name, or the code itself when it cannot be resolved.</returns>
        public static string GetCountryName(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            {
                return null;
            }

            try
            {
                return new RegionInfo(countryCode.Trim().ToUpperInvariant()).EnglishName;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
