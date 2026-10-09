using MessagePack;
using System.Collections.Generic;

namespace Orion.Common.Messages.Other
{
    /// <summary>
    /// Package containing one or more deferred assemblies delivered from the server to the client.
    /// </summary>
    [MessagePackObject]
    public class DeferredAssembliesPackage : IMessage
    {
        /// <summary>
        /// The assemblies delivered within this package.
        /// </summary>
        [Key(1)]
        public List<DeferredAssemblyDescriptor> Assemblies { get; set; }

        /// <summary>
        /// Indicates whether this is the final package in the current transfer sequence.
        /// </summary>
        [Key(2)]
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Descriptor describing a single deferred assembly payload.
    /// </summary>
    [MessagePackObject]
    public class DeferredAssemblyDescriptor
    {
        /// <summary>
        /// Simple assembly name, e.g. "SharpDX.Direct3D11".
        /// </summary>
        [Key(1)]
        public string Name { get; set; }

        /// <summary>
        /// Optional assembly version information.
        /// </summary>
        [Key(2)]
        public string Version { get; set; }

        /// <summary>
        /// Raw assembly bytes. May be null if the server could not locate the assembly.
        /// </summary>
        [Key(3)]
        public byte[] Data { get; set; }

        /// <summary>
        /// SHA-256 hash of the assembly bytes for integrity validation (hex encoded).
        /// </summary>
        [Key(4)]
        public string Sha256 { get; set; }
    }
}
