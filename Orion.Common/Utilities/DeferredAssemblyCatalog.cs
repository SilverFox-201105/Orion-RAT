using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Orion.Common.Utilities
{
    /// <summary>
    /// Defines the set of assemblies that are considered secondary payloads and may be delivered on-demand.
    /// </summary>
    public static class DeferredAssemblyCatalog
    {
        private static readonly string[] _secondaryAssemblies = new[]
        {
            "AForge",
            "AForge.Video",
            "AForge.Video.DirectShow",
            "NAudio.Core",
            "NAudio.Wasapi",
            "NAudio.WinForms",
            "NAudio.WinMM",
            "SharpDX",
            "SharpDX.Direct2D1",
            "SharpDX.Direct3D11",
            "SharpDX.DXGI",
            "SharpDX.D3DCompiler",
            "SharpDX.Mathematics"
        };

        private static readonly ReadOnlyCollection<string> _secondaryAssembliesReadonly =
            Array.AsReadOnly(_secondaryAssemblies);

        private static readonly HashSet<string> _secondaryAssembliesSet =
            new HashSet<string>(_secondaryAssemblies, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the ordered list of assemblies that should be delivered after the initial connection.
        /// </summary>
        public static IReadOnlyList<string> SecondaryAssemblies => _secondaryAssembliesReadonly;

        /// <summary>
        /// Checks whether the provided assembly name is part of the deferred assembly list.
        /// </summary>
        /// <param name="assemblyName">The simple assembly name.</param>
        public static bool IsDeferredAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }

            return _secondaryAssembliesSet.Contains(assemblyName);
        }
    }
}
