using Orion.Client.Config;
using Orion.Common.Messages.Other;
using Orion.Common.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Orion.Client.Utilities
{
    /// <summary>
    /// Manages deferred assemblies that are delivered after the initial client connection.
    /// Assemblies are loaded in memory only (Assembly.Load from a byte blob) and never
    /// persisted to disk, so no cleartext payload DLLs are written to the filesystem.
    /// </summary>
    internal static class DeferredAssemblyManager
    {
        private sealed class PendingCallback
        {
            public PendingCallback(IEnumerable<string> assemblyNames, Action callback)
            {
                Names = assemblyNames?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
                Callback = callback;
            }

            public string[] Names { get; }
            public Action Callback { get; }
        }

        private static readonly ConcurrentDictionary<string, byte[]> _assemblyBlobs =
            new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, Assembly> _loadedAssemblies =
            new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<PendingCallback> _pendingCallbacks = new List<PendingCallback>();
        private static readonly object _callbackSync = new object();
        private static bool _initialized;

        /// <summary>
        /// Gets the list of known secondary assemblies required for optional features.
        /// </summary>
        public static IReadOnlyList<string> SecondaryAssemblies => DeferredAssemblyCatalog.SecondaryAssemblies;

        /// <summary>
        /// Initializes the manager and wires up the assembly resolve handler.
        /// Assemblies are loaded in memory on demand; nothing is read from disk.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

                _initialized = true;

                EvaluateCallbacks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers a package of assemblies delivered by the server.
        /// </summary>
        public static void RegisterPackage(DeferredAssembliesPackage package)
        {
            if (package?.Assemblies == null || package.Assemblies.Count == 0)
            {
                return;
            }

            foreach (var descriptor in package.Assemblies)
            {
                RegisterAssembly(descriptor);
            }
        }

        /// <summary>
        /// Registers a single assembly descriptor.
        /// </summary>
        public static void RegisterAssembly(DeferredAssemblyDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return;
            }

            var name = descriptor.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.WriteLine("[DeferredAssemblyManager] Received assembly descriptor without a name.");
                return;
            }

            if (descriptor.Data == null || descriptor.Data.Length == 0)
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Assembly '{name}' did not include payload data.");
                return;
            }

            var computedHash = ComputeSha256(descriptor.Data);
            if (!string.IsNullOrWhiteSpace(descriptor.Sha256) &&
                !computedHash.Equals(descriptor.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Hash mismatch for '{name}'. Expected {descriptor.Sha256}, got {computedHash}. Ignoring payload.");
                return;
            }

            _assemblyBlobs[name] = descriptor.Data;

            Debug.WriteLine($"[DeferredAssemblyManager] Registered deferred assembly '{name}'.");

            EvaluateCallbacks();
        }

        /// <summary>
        /// Returns the list of deferred assemblies that are still missing.
        /// </summary>
        public static string[] GetMissingAssemblies()
        {
            return DeferredAssemblyCatalog.SecondaryAssemblies
                .Where(name => !IsAssemblyAvailable(name))
                .ToArray();
        }

        /// <summary>
        /// Returns true if the assembly is either cached or already loaded into the AppDomain.
        /// </summary>
        public static bool IsAssemblyAvailable(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }

            if (_assemblyBlobs.ContainsKey(assemblyName))
            {
                return true;
            }

            if (_loadedAssemblies.ContainsKey(assemblyName))
            {
                return true;
            }

            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
            if (loaded != null)
            {
                _loadedAssemblies[assemblyName] = loaded;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to load the requested assemblies into the current AppDomain if available.
        /// </summary>
        public static bool TryEnsureAssembliesLoaded(params string[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                return true;
            }

            bool success = true;

            foreach (var assemblyName in assemblies)
            {
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    continue;
                }

                if (IsAssemblyLoaded(assemblyName))
                {
                    continue;
                }

                if (_assemblyBlobs.TryGetValue(assemblyName, out var blob))
                {
                    try
                    {
                        LoadAssembly(assemblyName, blob);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DeferredAssemblyManager] Failed to load assembly '{assemblyName}': {ex.Message}");
                        success = false;
                    }
                }
                else
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Registers a callback that will be executed once all specified assemblies become available.
        /// If they are already available, the callback is executed synchronously.
        /// </summary>
        public static void RunWhenAvailable(IEnumerable<string> assemblies, Action callback)
        {
            if (callback == null)
            {
                return;
            }

            var names = assemblies?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
            if (names.Length == 0 || names.All(IsAssemblyAvailable))
            {
                callback();
                return;
            }

            lock (_callbackSync)
            {
                _pendingCallbacks.Add(new PendingCallback(names, callback));
            }
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var requestedName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrWhiteSpace(requestedName))
                {
                    return null;
                }

                if (_loadedAssemblies.TryGetValue(requestedName, out var loaded))
                {
                    return loaded;
                }

                if (_assemblyBlobs.TryGetValue(requestedName, out var blob))
                {
                    return LoadAssembly(requestedName, blob);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Assembly resolve failed: {ex.Message}");
            }

            return null;
        }

        private static Assembly LoadAssembly(string name, byte[] blob)
        {
            if (blob == null || blob.Length == 0)
            {
                return null;
            }

            var assembly = Assembly.Load(blob);
            _loadedAssemblies[name] = assembly;
            return assembly;
        }

        private static bool IsAssemblyLoaded(string name)
        {
            if (_loadedAssemblies.ContainsKey(name))
            {
                return true;
            }

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
            if (assembly != null)
            {
                _loadedAssemblies[name] = assembly;
                return true;
            }

            return false;
        }

        private static void EvaluateCallbacks()
        {
            List<Action> readyCallbacks = null;

            lock (_callbackSync)
            {
                for (int i = _pendingCallbacks.Count - 1; i >= 0; i--)
                {
                    var pending = _pendingCallbacks[i];
                    if (pending.Names.All(IsAssemblyAvailable))
                    {
                        if (readyCallbacks == null)
                        {
                            readyCallbacks = new List<Action>();
                        }

                        readyCallbacks.Add(pending.Callback);
                        _pendingCallbacks.RemoveAt(i);
                    }
                }
            }

            if (readyCallbacks == null)
            {
                return;
            }

            foreach (var callback in readyCallbacks)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(_ => SafeInvoke(callback));
                }
                catch
                {
                    SafeInvoke(callback);
                }
            }
        }

        private static void SafeInvoke(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Callback execution failed: {ex.Message}");
            }
        }

        private static string ComputeSha256(byte[] data)
        {
            try
            {
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyManager] Failed to compute hash: {ex.Message}");
                return string.Empty;
            }
        }

        private static string SanitizeFileName(string input)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                builder.Append(invalidChars.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }
    }
}
