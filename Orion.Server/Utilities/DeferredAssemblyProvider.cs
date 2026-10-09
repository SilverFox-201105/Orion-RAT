using Orion.Common.Messages.Other;
using Orion.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Orion.Server.Utilities
{
    /// <summary>
    /// Provides deferred assemblies from the server-side cache for delivery to clients on demand.
    /// </summary>
    internal static class DeferredAssemblyProvider
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, DeferredAssemblyDescriptor> Cache =
            new Dictionary<string, DeferredAssemblyDescriptor>(StringComparer.OrdinalIgnoreCase);
        private static string[] _assemblyDirectories;

        /// <summary>
        /// Retrieves the set of assembly descriptors for the requested names.
        /// </summary>
        public static IEnumerable<DeferredAssemblyDescriptor> GetAssemblies(IEnumerable<string> assemblyNames)
        {
            EnsureInitialized();

            if (assemblyNames == null)
            {
                yield break;
            }

            foreach (var name in assemblyNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                yield return GetAssembly(name);
            }
        }

        /// <summary>
        /// Retrieves a single assembly descriptor by name.
        /// </summary>
        public static DeferredAssemblyDescriptor GetAssembly(string assemblyName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return new DeferredAssemblyDescriptor { Name = assemblyName };
            }

            if (Cache.TryGetValue(assemblyName, out var cached))
            {
                return cached;
            }

            var path = ResolveAssemblyPath(assemblyName);
            if (path == null || !File.Exists(path))
            {
                Debug.WriteLine($"[DeferredAssemblyProvider] Missing deferred assembly '{assemblyName}'.");
                return new DeferredAssemblyDescriptor { Name = assemblyName };
            }

            try
            {
                var data = File.ReadAllBytes(path);
                var version = TryGetAssemblyVersion(path);
                var descriptor = new DeferredAssemblyDescriptor
                {
                    Name = assemblyName,
                    Data = data,
                    Version = version,
                    Sha256 = ComputeSha256(data)
                };

                Cache[assemblyName] = descriptor;
                return descriptor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyProvider] Failed to load '{assemblyName}': {ex.Message}");
                return new DeferredAssemblyDescriptor { Name = assemblyName };
            }
        }

        private static void EnsureInitialized()
        {
            if (_assemblyDirectories != null)
            {
                return;
            }

            lock (Sync)
            {
                if (_assemblyDirectories != null)
                {
                    return;
                }

                var baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

                var directories = new List<string>
                {
                    Path.Combine(baseDir, "DeferredAssemblies"),
                    Path.Combine(baseDir, "Build", "DeferredAssemblies")
                };

                foreach (var directory in directories)
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DeferredAssemblyProvider] Failed to create directory '{directory}': {ex.Message}");
                    }
                }

                _assemblyDirectories = directories.ToArray();
            }
        }

        private static string ResolveAssemblyPath(string assemblyName)
        {
            if (_assemblyDirectories == null || _assemblyDirectories.Length == 0)
            {
                return null;
            }

            var fileName = SanitizeFileName(assemblyName) + ".dll";
            foreach (var directory in _assemblyDirectories)
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                var candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Default to the first directory even if the file does not yet exist so callers can write into it later.
            return Path.Combine(_assemblyDirectories[0], fileName);
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                builder.Append(invalidChars.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }

        private static string TryGetAssemblyVersion(string path)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(path);
                return assemblyName?.Version?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeSha256(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
