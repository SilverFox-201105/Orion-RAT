using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Orion.Server.Plugins
{
    public sealed class PluginManager : IDisposable
    {
        private readonly IServerContext _context;
        private readonly List<IServerPlugin> _plugins = new List<IServerPlugin>();
        private readonly Dictionary<string, PluginMetadata> _pluginMetadata = new Dictionary<string, PluginMetadata>(StringComparer.OrdinalIgnoreCase);
        private FileSystemWatcher _watcher;
        private readonly object _lock = new object();
        private readonly object _reloadLock = new object();
        // 导入插件包时挂起 watcher 触发的重载，避免逐文件写入导致菜单被反复清空重建（闪烁）。
        private volatile bool _watcherSuspended;
        // 去抖：把解压/拖放期间的一连串文件变更事件合并成一次重载。
        private CancellationTokenSource _watcherReloadCts;
        public event EventHandler PluginsChanged;

        public class PluginMetadata
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public string[] ServerDlls { get; set; }
            public string[] ClientDlls { get; set; }
            public string FolderPath { get; set; }
        }

        public PluginManager(IServerContext context)
        {
            _context = context;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name).Name;
                var folder = _watcher?.Path;
                if (string.IsNullOrEmpty(folder)) return null;

                // Search in all subdirectories of the plugin folder
                foreach (var subDir in Directory.GetDirectories(folder))
                {
                    var dllPath = Path.Combine(subDir, assemblyName + ".dll");
                    if (File.Exists(dllPath))
                    {
                        return Assembly.Load(File.ReadAllBytes(dllPath));
                    }
                }
            }
            catch { }
            return null;
        }

        public IReadOnlyList<IServerPlugin> Plugins => _plugins;

        public PluginMetadata GetMetadata(string pluginName)
        {
            return _pluginMetadata.TryGetValue(pluginName, out var meta) ? meta : null;
        }

        public void LoadFrom(string folder)
        {
            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(folder)) 
                    {
                        Directory.CreateDirectory(folder);
                        _context.Log("Created plugin directory: " + folder);
                    }
                    
                    var subdirectories = Directory.GetDirectories(folder);
                    _context.Log($"Scanning {subdirectories.Length} subdirectories for plugins in: {folder}");

                    foreach (var subDir in subdirectories)
                    {
                        LoadPluginFromSubfolder(subDir);
                    }

                    _context.Log($"Loaded {_plugins.Count} plugins successfully");
                    StartWatcher(folder);
                }
                catch (Exception ex)
                {
                    _context.Log("PluginManager error: " + ex.Message);
                }
            }
        }

        private void LoadPluginFromSubfolder(string subDir)
        {
            try
            {
                var manifestPath = Path.Combine(subDir, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    // Fallback to legacy scanning if no plugin.json? 
                    // User said "更改识别逻辑，给每个插件分文件夹", implying plugin.json is required now.
                    return;
                }

                var json = File.ReadAllText(manifestPath);
                var manifest = JObject.Parse(json);

                var metadata = new PluginMetadata
                {
                    Name = manifest["name"]?.ToString(),
                    Version = manifest["version"]?.ToString(),
                    Description = manifest["description"]?.ToString(),
                    ServerDlls = manifest["server"]?.ToObject<string[]>() ?? Array.Empty<string>(),
                    ClientDlls = manifest["client"]?.ToObject<string[]>() ?? Array.Empty<string>(),
                    FolderPath = subDir
                };

                if (string.IsNullOrEmpty(metadata.Name)) metadata.Name = Path.GetFileName(subDir);

                foreach (var dllName in metadata.ServerDlls)
                {
                    var dllPath = Path.Combine(subDir, dllName);
                    if (File.Exists(dllPath))
                    {
                        var pluginName = TryLoadDll(dllPath);
                        if (!string.IsNullOrEmpty(pluginName))
                        {
                            _pluginMetadata[pluginName] = metadata;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Log($"Failed to load plugin from {subDir}: {ex.Message}");
            }
        }

        private void StartWatcher(string folder)
        {
            try
            {
                _watcher = new FileSystemWatcher(folder);
                _watcher.Filter = "*";
                _watcher.IncludeSubdirectories = true;
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                _watcher.Created += OnFileChanged;
                _watcher.Changed += OnFileChanged;
                _watcher.Deleted += OnFileChanged;
                _watcher.Renamed += OnFileRenamed;
                _watcher.Error += OnWatcherError;
                _watcher.EnableRaisingEvents = true;
                _context.Log("Plugin watcher started for: " + folder);
            }
            catch (Exception ex)
            {
                _context.Log("Plugin watcher error: " + ex.Message);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // 挂起检查必须放在最前（含日志）！之前日志写在挂起检查之前，
            // 导入/卸载大插件时每个文件变更都向 UI 日志框 BeginInvoke 一条消息，
            // 数千文件即数千条日志（每条都 RichTextBox 追加+滚动），UI 线程被洪泛淹没
            // —— 表现为进度条窗体永远不刷新（卡死）、Close() 排不上队。
            if (_watcherSuspended) return;
            _context.Log($"File/Folder {e.ChangeType}: {e.Name}");
            ScheduleReload();
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (_watcherSuspended) return;
            _context.Log($"File/Folder renamed: {e.OldName} -> {e.Name}");
            ScheduleReload();
        }

        /// <summary>
        /// 把一连串文件变更事件合并成一次重载：每次事件取消上一次待执行的重载，
        /// 仅在“最后一条事件后等待一段时间”才真正 ReloadPlugins。导入期间挂起时直接忽略。
        /// </summary>
        private void ScheduleReload()
        {
            if (_watcherSuspended) return;

            CancellationTokenSource cts;
            lock (_reloadLock)
            {
                _watcherReloadCts?.Cancel();
                _watcherReloadCts?.Dispose();
                _watcherReloadCts = cts = new CancellationTokenSource();
            }

            Task.Delay(1500, cts.Token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                try
                {
                    ReloadPlugins();
                }
                catch (Exception ex)
                {
                    _context.Log($"Plugin reload error: {ex.Message}");
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// 导入/卸载等“已知会改动插件目录”的操作前调用，挂起 watcher 触发的自动重载，
        /// 避免逐文件写入引发的菜单反复清空重建（表现为插件闪烁）。操作结束后再调用 ResumeWatcher。
        /// </summary>
        public void SuspendWatcher()
        {
            _watcherSuspended = true;
        }

        public void ResumeWatcher()
        {
            _watcherSuspended = false;
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _context.Log($"Plugin watcher error: {e.GetException().Message}");
            try
            {
                _watcher?.Dispose();
                var folder = _watcher?.Path;
                if (!string.IsNullOrEmpty(folder))
                {
                    StartWatcher(folder);
                }
            }
            catch (Exception ex)
            {
                _context.Log($"Failed to restart watcher: {ex.Message}");
            }
        }

        // 与 _lock 分开的串行锁：保证 ReloadPlugins 不会并发执行，
        // 同时允许在「非 _lock 保护区」做跨线程 UI 调用（见下）。
        private readonly object _reloadSerializationLock = new object();

        public void ReloadPlugins()
        {
            lock (_reloadSerializationLock)
            {
                bool fireEvent = false;
                try
                {
                    var folder = _watcher.Path;
                    if (!Directory.Exists(folder))
                    {
                        _context.Log("Plugin directory does not exist: " + folder);
                        return;
                    }

                    // 阶段1：释放旧插件、清空数据集合。持 _lock，期间绝不做任何跨线程 UI 调用。
                    lock (_lock)
                    {
                        _pluginMetadata.Clear();
                        foreach (var plugin in _plugins)
                        {
                            if (plugin is IDisposable disposable)
                            {
                                try { disposable.Dispose(); } catch { }
                            }
                        }
                        _plugins.Clear();
                    }

                    // 阶段2：清空 UI 扩展与插件菜单项（必须在重新加载之前完成）。
                    // 必须在 _lock 之外！ClearPluginMenuItems 内部是同步 _form.Invoke 等 UI 线程，
                    // 若持 _lock 等 UI，而 UI 线程此时正调用 ReloadPlugins 抢 _lock，
                    // 就形成「UI 等 _lock / 后台持 _lock 等 UI」的死锁 —— 表现就是导入/卸载时控端卡死、
                    // 进度条永远关不掉。
                    UIExtensionManager.ClearExtensions();
                    _context.ClearPluginMenuItems();

                    // 阶段3：重新加载所有插件（Assembly.Load 可能很慢，故本方法宜在后台线程调用）。
                    var subdirectories = Directory.GetDirectories(folder);
                    lock (_lock)
                    {
                        foreach (var subDir in subdirectories)
                        {
                            LoadPluginFromSubfolder(subDir);
                        }
                    }

                    fireEvent = true;
                }
                catch (Exception ex)
                {
                    _context.Log("Plugin reload error: " + ex.Message);
                    fireEvent = true;
                }

                // 阶段4：触发变更事件（锁外；订阅者已改用 BeginInvoke，不会反向阻塞本线程）。
                if (fireEvent)
                {
                    PluginsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string TryLoadDll(string path)
        {
            try
            {
                if (IsClientPluginFile(path))
                {
                    return null;
                }

                var dllName = Path.GetFileName(path);
                var alreadyLoaded = _plugins.Any(p => 
                {
                    var loadedDllName = p.GetType().Assembly.GetName().Name + ".dll";
                    return string.Equals(loadedDllName, dllName, StringComparison.OrdinalIgnoreCase);
                });

                if (alreadyLoaded)
                {
                    return null;
                }

                var asmBytes = File.ReadAllBytes(path);
                var asm = Assembly.Load(asmBytes);
                var types = asm.GetTypes().Where(t => !t.IsAbstract && typeof(IServerPlugin).IsAssignableFrom(t));
                string loadedPlugin = null;
                foreach (var t in types)
                {
                    var pluginName = TryInit(t, Path.GetFileName(path));
                    if (!string.IsNullOrEmpty(pluginName))
                        loadedPlugin = pluginName;
                }
                return loadedPlugin;
            }
            catch (Exception ex)
            {
                _context.Log("Plugin load error: " + ex.Message);
            }
            return null;
        }

        private string TryInit(Type t, string source)
        {
            try
            {
                if (Activator.CreateInstance(t) is IServerPlugin p)
                {
                    p.Initialize(_context);
                    _plugins.Add(p);
                    _context.Log("Loaded plugin '" + p.Name + "' from " + source);
                    
                    if (p is IUIExtensionPlugin uiPlugin)
                    {
                        UIExtensionManager.RegisterUIExtension(uiPlugin);
                    }
                    
                    return p.Name;
                }
            }
            catch (Exception ex)
            {
                _context.Log("Plugin init failed: " + ex.Message);
            }
            return null;
        }

        public void Dispose()
        {
            foreach (var plugin in _plugins)
            {
                if (plugin is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _plugins.Clear();
            _watcher?.Dispose();
        }

        private static bool IsClientPluginFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var fileName = Path.GetFileName(path);
            return fileName != null && fileName.EndsWith(".Client.dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}
