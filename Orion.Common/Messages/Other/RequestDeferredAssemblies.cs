using MessagePack;

namespace Orion.Common.Messages.Other
{
    /// <summary>
    /// Client request asking the server to deliver one or more deferred assemblies.
    /// </summary>
    [MessagePackObject]
    public class RequestDeferredAssemblies : IMessage
    {
        /// <summary>
        /// Gets or sets the list of assembly simple names the client is missing.
        /// </summary>
        [Key(1)]
        public string[] Assemblies { get; set; }

        /// <summary>
        /// Optional version information so the server can pick an appropriate bundle.
        /// </summary>
        [Key(2)]
        public string ClientVersion { get; set; }
    }
}
