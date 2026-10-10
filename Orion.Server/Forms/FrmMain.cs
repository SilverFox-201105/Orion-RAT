using Newtonsoft.Json;
using Orion.Common.Enums;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.Actions;
using Orion.Common.Messages.ClientManagement;
using Orion.Common.Messages.ClientManagement.UAC;
using Orion.Common.Messages.Monitoring.VirtualMonitor;
using Orion.Common.Messages.Preview;
using Orion.Common.Messages.QuickCommands;
using Orion.Common.Messages.UserSupport.MessageBox;
using Orion.Common.Messages.UserSupport.Website;
using Orion.Server.Controls;
using Orion.Server.Controls.Wpf;
using Orion.Server.Extensions;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Messages;
using Orion.Server.Models;
using Orion.Server.Networking;
using Orion.Server.Persistence;
using Orion.Server.Statistics;
using Orion.Server.Utilities;
using Orion.Server.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ImageSource = System.Windows.Media.ImageSource;

namespace Orion.Server.Forms
{
    /// <summary>
    /// Defines how auto tasks should be executed when clients connect.
    /// </summary>
    public enum AutoTaskExecutionMode
    {
        /// <summary>
        /// Auto tasks run only once per client (based on client ID).
        /// </summary>
        OncePerClient,

        /// <summary>
        /// Auto tasks run every time a client connects.
        /// </summary>
        EveryConnection
    }

    public partial class FrmMain : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OrionServer ListenServer { get; set; }

        private const int STATUS_ID = 5;
        private const int CURRENTWINDOW_ID = 6;
        private const int USERSTATUS_ID = 7;

        private bool _titleUpdateRunning;
        private bool _processingClientConnections;
        private readonly ClientStatusHandler _clientStatusHandler;
        private readonly ClientDebugLog _clientDebugLogHandler;
        private readonly DeferredAssemblyHandler _deferredAssemblyHandler = new DeferredAssemblyHandler();
        private readonly UniversalPluginResponseHandler _universalPluginResponseHandler = new UniversalPluginResponseHandler();
        private readonly BlockingCollection<KeyValuePair<Client, bool>> _clientConnections = new(new ConcurrentQueue<KeyValuePair<Client, bool>>());
        private readonly object _clientConnectionsStartLock = new object();
        private Task _clientConnectionsConsumerTask;
        private CancellationTokenSource _clientConnectionsCts;
        private readonly object _lockClients = new object();
        private readonly object _offlineRefreshLock = new object();
        private readonly object _statsRefreshLock = new object();
        private bool _offlineRefreshPending;
        private bool _statsRefreshPending;
        private readonly HashSet<Client> _visibleClients = new HashSet<Client>();
        private readonly Dictionary<Client, ClientListEntry> _clientEntryMap = new();
        private readonly Dictionary<Client, ListViewItem> _clientListViewItems = new();
        private readonly Dictionary<Client, Button> _clientStarButtons = new();
        private readonly Dictionary<string, ListViewGroup> _countryGroups = new(StringComparer.OrdinalIgnoreCase);
        private readonly IComparer _clientListViewComparer = new ClientListViewComparer();
        private readonly Dictionary<int, ImageSource> _flagImageCache = new();
        private readonly System.Windows.Forms.Timer _clientSortTimer;
        private bool _clientSortPending;
        private bool _syncingSelection;
        private IReadOnlyList<OfflineClientRecord> _cachedOfflineClients = Array.Empty<OfflineClientRecord>();
        private PreviewHandler _previewImageHandler;
        private static readonly string OrionStuffDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrionStuff");
        private readonly string AutoTasksFilePath = Path.Combine(OrionStuffDir, "autotasks.json");
        private readonly string NotificationStateFilePath = Path.Combine(OrionStuffDir, "notifications.json");
        private const int MaxNotificationHistoryEntries = 2000;
        private const long NotificationHistoryPruneThresholdBytes = 20L * 1024 * 1024; // 20 MB
        private readonly List<NotificationEntry> _notificationHistory = new();
        private readonly object _notificationLock = new();
        private Font _notificationUnreadFont;
        private Dictionary<string, AutoTaskBehavior> _autoTaskBehaviors = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, AutoTaskBehavior> _autoTaskBehaviorsByTitle = new(StringComparer.OrdinalIgnoreCase);
        private AutoTaskExecutionContext _currentAutoTaskContext;
        private readonly HashSet<string> _executedTaskClientCombinations = new HashSet<string>();
        private static readonly HashSet<string> _connectionTerminatingAutoTaskIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "reconnectToolStripMenuItem",
            "disconnectToolStripMenuItem",
            "uninstallToolStripMenuItem",
            "shutdownToolStripMenuItem",
            "restartToolStripMenuItem",
            "standbyToolStripMenuItem"
        };

        // Plugin System Fields
        private PluginManager _pluginManager;
        private FrmPluginManager _pluginManagerForm;
        private ServerContext _serverContext;
        private readonly HashSet<Client> _clientsWithPluginsLoaded = new HashSet<Client>();
        private ClientPluginCatalog _clientPluginCatalog;
        private readonly Dictionary<Client, HashSet<string>> _clientAutoPluginState = new();
        private readonly object _clientAutoPluginLock = new();

        public FrmMain()
        {
            _clientStatusHandler = new ClientStatusHandler();
            _clientDebugLogHandler = new ClientDebugLog();
            RegisterMessageHandler();
            OfflineClientRepository.Initialize();
            OfflineClientRepository.ResetOnlineState();
            InitializeComponent();
            WpfFlagProvider.Initialize(imgFlags);
            ApplyHighDpiImages();
            _clientSortTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _clientSortTimer.Tick += ClientSortTimer_Tick;
            Text = ServerVersion.WindowTitle;
            statsElementHost?.ShowLoading();
            heatMapElementHost?.ShowLoading();
            // 旧版 WinForms 客户端列表已被 WPF 的 wpfClientsHost 接管，设计器里不再实例化
            // lstClients / lstOfflineClients，但本文件仍有遗留代码引用它们（列宽同步、标题统计、
            // 星级收藏等）。这里补两个隐藏的兜底实例：既不参与绘制（遗留路径本身有
            // `if (!lstClients.Visible) return;` 保护），也不会让反射/属性访问拿到 null 而崩溃。
            if (lstClients == null)
            {
                lstClients = new AeroListView { Visible = false };
            }
            if (lstOfflineClients == null)
            {
                lstOfflineClients = new AeroListView { Visible = false };
            }
            SetDoubleBuffered(lstClients, true);
            SetDoubleBuffered(lstOfflineClients, true);
            lstClients.Visible = false;
            wpfClientsHost.SelectionChanged += WpfClientsHost_SelectionChanged;
            wpfClientsHost.FavoriteToggled += WpfClientsHost_FavoriteToggled;
            DarkModeManager.ApplyDarkMode(this);
            RefreshClientTheme();
            
            // 如果主窗体设置了背景图，则同步应用到客户端列表 (WPF DataGrid)
            if (this.BackgroundImage != null)
            {
                wpfClientsHost?.SetBackgroundImage(this.BackgroundImage, 0.3f);
            }

            tableLayoutPanel1.VisibleChanged += TableLayoutPanel1_VisibleChanged;

            addTaskToolStripMenuItem.DropDownItems.Clear();
            InitializeAutoTaskBehaviors();
            BuildAutoTaskMenu();

            InitializeSearch();
            InitializeNotificationTracking();

            // Initialize Plugin System
            InitializePlugins();
        }

        /// <summary>
        /// 打开 ListView 的双缓冲（非公开属性，只能反射设置）。
        /// 目标为 null 时直接返回——FrmMain 里这两个列表是遗留字段，
        /// 传 null 给 InvokeMember 会抛 TargetException（非静态方法需要一个目标）。
        /// </summary>
        private static void SetDoubleBuffered(ListView list, bool enabled)
        {
            if (list == null) return;
            try
            {
                typeof(ListView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic,
                    null, list, new object[] { enabled });
            }
            catch
            {
                // 双缓冲只是绘制优化，失败不影响功能
            }
        }

        private void OnLogReceived(object sender, Client client, string log)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnLogReceived(sender, client, log)));
                return;
            }
        }

        private void RegisterMessageHandler()
        {
            MessageHandler.Register(_clientDebugLogHandler);
            _clientDebugLogHandler.DebugLogReceived += OnLogReceived;
            MessageHandler.Register(_clientStatusHandler);
            _clientStatusHandler.StatusUpdated += SetStatusByClient;
            _clientStatusHandler.UserStatusUpdated += SetUserStatusByClient;
            _clientStatusHandler.UserActiveWindowStatusUpdated += SetUserActiveWindowByClient;
            _clientStatusHandler.UserClipboardStatusUpdated += SetUserClipboardByClient;
            MessageHandler.Register(_deferredAssemblyHandler);
            MessageHandler.Register(_universalPluginResponseHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_clientDebugLogHandler);
            _clientDebugLogHandler.DebugLogReceived -= OnLogReceived;
            MessageHandler.Unregister(_clientStatusHandler);
            _clientStatusHandler.StatusUpdated -= SetStatusByClient;
            _clientStatusHandler.UserStatusUpdated -= SetUserStatusByClient;
            _clientStatusHandler.UserActiveWindowStatusUpdated -= SetUserActiveWindowByClient;
            _clientStatusHandler.UserClipboardStatusUpdated -= SetUserClipboardByClient;
            MessageHandler.Unregister(_deferredAssemblyHandler);
            MessageHandler.Unregister(_universalPluginResponseHandler);
        }

        public void UpdateWindowTitle()
        {
            if (_titleUpdateRunning) return;
            _titleUpdateRunning = true;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        int selected = lstClients.SelectedItems.Count;
                        int connected = ListenServer?.ConnectedClients?.Length ?? 0;
                        string baseTitle = ServerVersion.WindowTitle;
                        this.Text = (selected > 0)
                            ? $"{baseTitle} - 已连接：{connected} [已选：{selected}]"
                            : $"{baseTitle} - 已连接：{connected}";
                    }
                    finally
                    {
                        _titleUpdateRunning = false;
                    }
                });
            }
            catch (Exception)
            {
                _titleUpdateRunning = false;
            }
        }

        private void InitializeServer()
        {
            X509Certificate2 serverCertificate;
#if DEBUG
            serverCertificate = new DummyCertificate();
#else
            if (!File.Exists(Settings.CertificatePath))
            {
                using (var certificateSelection = new FrmCertificate())
                {
                    while (certificateSelection.ShowDialog() != DialogResult.OK)
                    { }
                }
            }
            serverCertificate = new X509Certificate2(Settings.CertificatePath);
#endif
            ListenServer = new OrionServer(serverCertificate);
            ListenServer.ServerState += ServerState;
            ListenServer.ClientConnected += ClientConnected;
            ListenServer.ClientDisconnected += ClientDisconnected;
        }

        /// <summary>
        /// 启动本地监听。唯一入口是 ListenerCoordinator：
        /// 已在同一组端口上监听时直接复用（幂等，不会重复绑定触发 10048，也不会误杀已有监听）；
        /// 端口冲突等失败由协调器统一提示并回滚。
        /// </summary>
        /// <param name="singlePortOnly">
        /// true = 只监听 Settings.ListenPort 一个端口。FRP 模式下必须如此，
        /// 否则会把 ListenPorts 里陈旧的额外端口一起绑上（隧道并不需要它们）。
        /// </param>
        private void StartConnectionListener(bool singlePortOnly = false)
        {
            if (ListenServer == null) return;

            var allPorts = new List<ushort> { Settings.ListenPort };
            if (!singlePortOnly && Settings.ListenPorts != null)
                allPorts.AddRange(Settings.ListenPorts);

            Networking.ListenerCoordinator.Ensure(ListenServer, allPorts.Distinct().ToList(),
                Settings.IPv6Support, this);
        }

        private void AutostartListening()
        {
            // FRP 模式下本地监听由隧道统一接管（只监听单个「本地转发端口」），
            // 这里不要先用左侧的多端口起一遍监听 —— 否则紧接着的单端口 Ensure 会把它断掉重绑，
            // 白白让端口闪断一次。FRP 分支会自行 Ensure 正确的端口。
            if (Settings.FrpEnabled)
                return;

            if (Settings.AutoListen)
            {
                StartConnectionListener();
            }
        }

        public void EventLogVisability()
        {
            if (Settings.EventLog)
            {
                DebugLogRichBox.Visible = true;
                splitter1.Visible = true;
            }
            else
            {
                DebugLogRichBox.Visible = false;
                splitter1.Visible = false;
            }
        }

        public void RefreshClientGroups()
        {
            wpfClientsHost?.SetGrouping(GetClientGroupMode());
            ResetCountryGroups();
            SortClientsByFavoriteStatus(forceImmediate: true);
        }

        /// <summary>
        /// 根据当前设置启动或停止 C2 隧道（FRP）。在设置保存后由 FrmSettings 调用，
        /// 也用于开关 FRP 时热更新。本地转发端口取自监听端口。
        /// </summary>
        public void RefreshFrpTunnel()
        {
            var mgr = Frp.FrpTunnelManager.Instance;

            if (!Models.Settings.FrpEnabled)
            {
                mgr.Stop();
                return;
            }

            // 配置没变且隧道已在跑 → 什么都不做。否则哪怕只改了分组/Telegram/屏蔽 IP，
            // 保存一次也会把隧道断掉重连，白白掉线几秒。
            var signature = Frp.FrpTunnelManager.CurrentSettingsSignature;
            if (mgr.IsRunningWith(signature))
                return;

            // FRP 隧道终点 = frpc 配置里的 local_port。让 Orion 监听该端口，并同步 Settings.ListenPort，
            // 否则隧道把流量送到 local_port 而 Orion 却监听别的端口，被控端连不上。
            if (mgr.TryGetLocalPort(out int frpLocalPort) && frpLocalPort > 0)
                Settings.ListenPort = (ushort)frpLocalPort;

            // FRP 只需要一个本地转发端口（隧道终点）：确保它在线，失败则放弃启动隧道
            StartConnectionListener(singlePortOnly: true);
            if (ListenServer != null && !ListenServer.Listening)
                return;

            mgr.StartFromSettings();
        }

        private static ClientGroupMode GetClientGroupMode()
        {
            if (Settings.ShowTagGroups)
            {
                return ClientGroupMode.ByTag;
            }

            if (Settings.ShowCountryGroups)
            {
                return ClientGroupMode.ByCountry;
            }

            return ClientGroupMode.None;
        }

        public void RefreshClientTheme()
        {
            wpfClientsHost?.ApplyTheme(true);
            statsElementHost?.ApplyTheme(true);
            heatMapElementHost?.ApplyTheme(true);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            wpfClientsHost?.SetGrouping(GetClientGroupMode());
            RefreshClientTheme();
            ApplyWpfSearchFilter();

            int accountTypeIndex = -1;
            for (int i = 0; i < lstClients.Columns.Count; i++)
            {
                if (lstClients.Columns[i] == hAccountType)
                {
                    accountTypeIndex = i;
                    break;
                }
            }

            if (accountTypeIndex >= 0)
            {
                lstClients.StretchColumnByIndex(accountTypeIndex);
            }

            EventLog($"欢迎使用 {ServerVersion.AppName}。", "info");
            InitializeServer();
            AutostartListening();
            EventLogVisability();

            // FRP / C2 隧道：控端主动连 FRPS，把本地监听端口暴露到公网，被控端无需直连控端。
            // 「启动时监听新连接」(AutoListen) 是启动期自动动作的总开关：
            //  - 勾选 + 已保存「启用远程FRP隧道」→ 开机即连服务器（等同于自动按一次「连接」）；
            //  - 未勾选 → 只保留已启用的勾选状态，不自动连，等用户在设置页手动点「连接」。
            Frp.FrpTunnelManager.Instance.StatusChanged += (s) => EventLog("C2 隧道: " + s, "info");
            if (Models.Settings.FrpEnabled)
            {
                if (Models.Settings.AutoListen)
                {
                    EventLog("正在启动 C2 隧道 (FRP)...", "info");
                    RefreshFrpTunnel();
                }
                else
                {
                    EventLog("C2 隧道: 已启用「启用远程FRP隧道」，但未勾选「启动时监听新连接」——请在设置页手动点「连接」。", "info");
                }
            }
            notifyIcon.Visible = false;
            Favorites.LoadFavorites();

            lstClients.ColumnWidthChanging += lstClients_ColumnWidthChanging;

            LoadNotificationHistory();
            LoadAutoTasks();
            InitializeAutoTasksMenu();
            ScheduleOfflineListRefresh();
            ScheduleStatsRefresh();
        }

        private void InitializeAutoTasksMenu()
        {
            var separator = new ToolStripSeparator();
            TasksContextMenuStrip.Items.Add(separator);

            var executeAllMenuItem = new ToolStripMenuItem("在所有客户端上执行")
            {
                Image = Properties.Resources.application_go,
                ToolTipText = "在尚未执行自动任务的所有已连接客户端上执行"
            };
            executeAllMenuItem.Click += (s, e) => ExecuteAutoTasksOnAllClients();
            TasksContextMenuStrip.Items.Add(executeAllMenuItem);

            var forceExecuteMenuItem = new ToolStripMenuItem("强制在所有已连接客户端上执行")
            {
                Image = Properties.Resources.application_add,
                ToolTipText = "强制在所有已连接客户端上执行自动任务，忽略之前的执行记录"
            };
            forceExecuteMenuItem.Click += (s, e) => ForceExecuteAutoTasksOnAllClients();
            TasksContextMenuStrip.Items.Add(forceExecuteMenuItem);

            var resetTrackingMenuItem = new ToolStripMenuItem("重置执行跟踪")
            {
                Image = Properties.Resources.refresh,
                ToolTipText = "重置跟踪，使自动任务将在所有客户端上重新运行"
            };
            resetTrackingMenuItem.Click += (s, e) => ResetAutoTaskTracking();
            TasksContextMenuStrip.Items.Add(resetTrackingMenuItem);

            var separator2 = new ToolStripSeparator();
            TasksContextMenuStrip.Items.Add(separator2);

            var toggleModeMenuItem = new ToolStripMenuItem("切换执行模式")
            {
                Image = Properties.Resources.refresh,
                ToolTipText = "在'每个客户端一次'和'每次连接'之间切换所选任务的执行模式"
            };
            toggleModeMenuItem.Click += (s, e) => ToggleSelectedTasksExecutionMode();
            TasksContextMenuStrip.Items.Add(toggleModeMenuItem);

            var aboutModesMenuItem = new ToolStripMenuItem("关于任务执行模式")
            {
                Image = Properties.Resources.information,
                ToolTipText = "了解任务执行模式"
            };
            aboutModesMenuItem.Click += (s, e) => ShowTaskExecutionModeDialog();
            TasksContextMenuStrip.Items.Add(aboutModesMenuItem);
        }

        private void lstClients_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            if (e.ColumnIndex != hAccountType.Index)
            {
                lstClients.StretchColumnByIndex(hAccountType.Index);
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveNotificationHistory();
                SaveAutoTasks();

                Networking.ListenerCoordinator.Stop(ListenServer);

                Frp.FrpTunnelManager.Instance.Stop();

                UnregisterMessageHandler();

                _pluginManager?.Dispose();
                _clientPluginCatalog?.Dispose();

                if (_pluginManager != null)
                    _pluginManager.PluginsChanged -= OnPluginsChanged;
                if (_clientPluginCatalog != null)
                    _clientPluginCatalog.PluginsChanged -= OnClientPluginsChanged;

                _visibleClients.Clear();
                _clientEntryMap.Clear();
                _clientListViewItems.Clear();
                foreach (var button in _clientStarButtons.Values)
                {
                    if (!button.IsDisposed)
                        button.Dispose();
                }
                _clientStarButtons.Clear();
                _flagImageCache.Clear();
                WpfFlagProvider.ClearCache();
                _notificationHistory.Clear();
                _autoTaskBehaviors.Clear();
                _autoTaskBehaviorsByTitle.Clear();
                _executedTaskClientCombinations.Clear();
                _clientsWithPluginsLoaded.Clear();
                _clientAutoPluginState.Clear();

                if (_previewImageHandler != null)
                {
                    MessageHandler.Unregister(_previewImageHandler);
                    _previewImageHandler.Dispose();
                }

                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                try
                {
                    _statusUpdateTimer?.Dispose();
                    _statusUpdateTimer = null;
                }
                catch { }

                try
                {
                    if (_clientConnectionsCts != null)
                    {
                        _clientConnectionsCts.Cancel();
                        _clientConnections.CompleteAdding();
                        _clientConnectionsConsumerTask?.Wait(2000);
                        _clientConnectionsCts.Dispose();
                        _clientConnectionsCts = null;
                        try
                        {
                            _clientConnections?.Dispose();
                        }
                        catch { }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                // Log the error but don't prevent form closing
                System.Diagnostics.Debug.WriteLine($"Error during form closing: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void EventLog(string message, string level)
        {
            try
            {
                // 窗体/控件已释放（例如插件重载或程序退出后后台线程仍写日志）时直接忽略
                if (IsDisposed || Disposing || DebugLogRichBox.IsDisposed || DebugLogRichBox.Disposing)
                {
                    return;
                }

                if (!DebugLogRichBox.IsHandleCreated)
                {
                    return;
                }

                if (DebugLogRichBox.InvokeRequired)
                {
                    DebugLogRichBox.BeginInvoke(new Action(() => LogMessage(message, level)));
                }
                else
                {
                    LogMessage(message, level);
                }
            }
            catch (ObjectDisposedException)
            {
                // 竞态：检查后、执行前控件被释放 —— 静默忽略
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"事件日志错误：{ex.Message}");
            }
        }

        private void LogMessage(string message, string level)
        {
            try
            {
                if (DebugLogRichBox.IsDisposed || DebugLogRichBox.Disposing || !DebugLogRichBox.IsHandleCreated)
                {
                    return;
                }
                string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                string formattedMessage = $"[{timestamp}] {message}";
                Color logColor = Color.DodgerBlue;

                switch (level.ToLowerInvariant())
                {
                    case "normal":
                        logColor = Color.White;
                        break;

                    case "info":
                        logColor = Color.DodgerBlue;
                        break;

                    case "error":
                        logColor = Color.Red;
                        break;

                    default:
                        logColor = Color.DodgerBlue;
                        break;
                }

                int originalSelectionStart = DebugLogRichBox.SelectionStart;
                Color originalSelectionColor = DebugLogRichBox.SelectionColor;

                DebugLogRichBox.SelectionStart = DebugLogRichBox.TextLength;
                DebugLogRichBox.SelectionLength = 0;

                DebugLogRichBox.SelectionColor = logColor;

                DebugLogRichBox.AppendText(formattedMessage + Environment.NewLine);
                DebugLogRichBox.SelectionStart = DebugLogRichBox.TextLength;
                DebugLogRichBox.SelectionLength = 0;
                DebugLogRichBox.ScrollToCaret();

                DebugLogRichBox.SelectionStart = originalSelectionStart;
                DebugLogRichBox.SelectionColor = originalSelectionColor;
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放（窗体关闭/插件重载后残余日志），静默忽略
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志消息错误：{ex.Message}");
            }
        }

        private void TableLayoutPanel1_VisibleChanged(object sender, EventArgs e)
        {
            AdjustClientListViewSize();
        }

        private void AdjustClientListViewSize()
        {
            int targetWidth;
            if (tableLayoutPanel1.Visible)
            {
                targetWidth = this.ClientSize.Width - tableLayoutPanel1.Width;
            }
            else
            {
                targetWidth = this.ClientSize.Width;
            }

            lstClients.Width = targetWidth;
            if (wpfClientsHost != null)
            {
                wpfClientsHost.Width = targetWidth;
            }
        }

        private void lstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncWpfSelectionFromList();
            UpdateWindowTitle();

            var selectedClients = GetSelectedClients();
            if (selectedClients.Length == 1)
            {
                if (_previewImageHandler != null)
                {
                    MessageHandler.Unregister(_previewImageHandler);
                    _previewImageHandler.Dispose();
                }

                _previewImageHandler = new PreviewHandler(selectedClients[0], pictureBoxMain, clientInfoListView);
                MessageHandler.Register(_previewImageHandler);

                if (!chkDisablePreview.Checked)
                {
                    selectedClients[0].Send(new GetPreviewImage
                    {
                        Quality = 20,
                        DisplayIndex = 0
                    });
                }
            }
            else if (selectedClients.Length == 0)
            {
                pictureBoxMain.Image = Properties.Resources.nopreviewbest_cropped;
                clientInfoListView.Items.Clear();

                var defaultStats = new (string Label, string Value)[]
                {
                    ("CPU", "不可用"),
                    ("GPU", "不可用"),
                    ("RAM", "0 GB"),
                    ("运行时间", "不可用"),
                    ("杀毒软件", "不可用"),
                    ("默认浏览器", "不可用"),
                    ("Ping", "不可用"),
                    ("摄像头", "不可用"),
                    ("离开时间", "不可用")
                };

                foreach (var (label, value) in defaultStats)
                {
                    var item = new ListViewItem(label);
                    item.SubItems.Add(value);
                    clientInfoListView.Items.Add(item);
                }
            }
        }

        private void ServerState(Networking.Server server, bool listening, ushort port)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (!listening)
                    {
                        ClearAllStarButtons();
                        lstClients.Items.Clear();
                        lstClients.Groups.Clear();
                        _visibleClients.Clear();
                        _clientListViewItems.Clear();
                        _allClientItems.Clear();
                        _clientEntryMap.Clear();
                        wpfClientsHost?.ClearClients();
                    }

                    var statusText = ResolveListeningStatusText();

                    listenToolStripStatusLabel.Text = statusText;
                });
                UpdateWindowTitle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private string ResolveListeningStatusText()
        {
            try
            {
                if (ListenServer == null || !ListenServer.Listening)
                    return "未监听";

                var ports = (ListenServer.GetListeningPorts() ?? Array.Empty<ushort>()).Distinct().ToList();
                if (ports.Count == 0)
                    return "未监听";

                if (ports.Count <= 3)
                    return $"正在监听端口 {string.Join(", ", ports)}";

                var firstThree = ports.Take(3);
                var remaining = ports.Count - 3;
                return $"正在监听端口 {string.Join(", ", firstThree)} 等 {remaining} 个端口";
            }
            catch
            {
                return "未监听";
            }
        }

        private bool IsClientBlocked(string clientAddress)
        {
            string filePath = Path.Combine(OrionStuffDir, "blocked.json");

            if (!Directory.Exists(OrionStuffDir))
            {
                Directory.CreateDirectory(OrionStuffDir);
            }

            if (!(File.Exists(filePath)))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var blockedIPs = JsonConvert.DeserializeObject<List<string>>(json);
                return blockedIPs.Contains(clientAddress.ToString());
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ClientConnected(Client client)
        {
            if (IsClientBlocked(client.EndPoint.Address.ToString()))
            {
                client.Send(new DoClientUninstall());
                EventLog("被阻止的 IP 尝试连接 " + client.EndPoint.Address, "error");
            }
            else
            {
                OfflineClientRepository.UpsertClient(client);
                ScheduleOfflineListRefresh();
                ScheduleStatsRefresh();
                if (!ListenServer.Listening) return;
                _clientConnections.Add(new KeyValuePair<Client, bool>(client, true));

                lock (_clientConnectionsStartLock)
                {
                    if (!_processingClientConnections)
                    {
                        _processingClientConnections = true;
                        _clientConnectionsCts = new CancellationTokenSource();
                        _clientConnectionsConsumerTask = Task.Run(() => ProcessClientConnectionsLoop(_clientConnectionsCts.Token));
                    }
                }
                UpdateConnectedClientsCount();

                // Load plugins for the new client
                LoadPluginsForClient(client);
            }
        }

        private void ClientDisconnected(Client client)
        {
            OfflineClientRepository.MarkClientOffline(client);
            ScheduleOfflineListRefresh();
            ScheduleStatsRefresh();

            _clientsWithPluginsLoaded.Remove(client);
            lock (_clientAutoPluginLock)
            {
                _clientAutoPluginState.Remove(client);
            }
            if (!ListenServer.Listening) return;
            _clientConnections.Add(new KeyValuePair<Client, bool>(client, false));

            lock (_clientConnectionsStartLock)
            {
                if (!_processingClientConnections)
                {
                    _processingClientConnections = true;
                    _clientConnectionsCts = new CancellationTokenSource();
                    _clientConnectionsConsumerTask = Task.Run(() => ProcessClientConnectionsLoop(_clientConnectionsCts.Token));
                }
            }

            UpdateConnectedClientsCount();
        }

        private bool _countUpdateRunning;

        private void UpdateConnectedClientsCount()
        {
            if (_countUpdateRunning) return;
            _countUpdateRunning = true;

            try
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        var count = ListenServer?.ConnectedClients?.Length ?? 0;
                        var enabledPlugins = _pluginManager?.Plugins?.Count ?? 0;
                        connectedToolStripStatusLabel.Text = $"已连接：{count} | 活跃插件：{enabledPlugins}";
                    }
                    finally
                    {
                        _countUpdateRunning = false;
                    }
                });
            }
            catch (Exception)
            {
                _countUpdateRunning = false;
            }
        }

        private void ProcessClientConnectionsLoop(CancellationToken token)
        {
            const int batchSize = 10; // up to 10 clients at once
            var batch = new List<KeyValuePair<Client, bool>>(batchSize);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    batch.Clear();

                    KeyValuePair<Client, bool> first;
                    try
                    {
                        first = _clientConnections.Take(token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (!ListenServer.Listening)
                    {
                        while (_clientConnections.TryTake(out _)) { }
                        break;
                    }

                    batch.Add(first);
                    while (batch.Count < batchSize && _clientConnections.TryTake(out var next))
                    {
                        batch.Add(next);
                    }

                    foreach (var client in batch)
                    {
                        if (client.Key != null)
                        {
                            switch (client.Value)
                            {
                                case true:
                                    AddClientToListview(client.Key);
                                    ThreadPool.QueueUserWorkItem(_ => StartAutomatedTask(client.Key));
                                    if (Settings.ShowPopup)
                                        ThreadPool.QueueUserWorkItem(_ => ShowPopup(client.Key));
                                    break;

                                case false:
                                    RemoveClientFromListview(client.Key);
                                    break;
                            }
                        }
                    }
                }
            }
            finally
            {
                lock (_clientConnectionsStartLock)
                {
                    _processingClientConnections = false;
                }
                RefreshStarButtons();
            }
        }

        private void StartAutomatedTask(Client client)
        {
            if (client == null)
            {
                return;
            }

            if (IsClientBlocked(client.EndPoint.Address.ToString()))
            {
                Debug.WriteLine("Blocked client, skipping automated tasks.");
                return;
            }

            string clientId = client.Value?.Id;
            if (string.IsNullOrEmpty(clientId))
            {
                Debug.WriteLine("Client ID is null or empty, skipping automated tasks.");
                return;
            }

            Debug.WriteLine($"Starting automated tasks for client {clientId}.");

            if (lstTasks.InvokeRequired)
            {
                lstTasks.Invoke(new Action(() => StartAutomatedTask(client)));
                return;
            }

            foreach (ListViewItem item in lstTasks.Items)
            {
                var task = ExtractAutoTask(item);
                if (task == null) continue;

                if (task.ExecutionMode == AutoTaskExecutionMode.OncePerClient)
                {
                    string taskClientKey = $"{task.Identifier}_{clientId}";
                    lock (_executedTaskClientCombinations)
                    {
                        if (_executedTaskClientCombinations.Contains(taskClientKey))
                        {
                            Debug.WriteLine($"Task '{task.Title}' already executed for client {clientId}, skipping.");
                            continue;
                        }
                        _executedTaskClientCombinations.Add(taskClientKey);
                    }
                    Debug.WriteLine($"Executing task '{task.Title}' for client {clientId} (OncePerClient mode).");
                }
                else
                {
                    Debug.WriteLine($"Executing task '{task.Title}' for client {clientId} (EveryConnection mode).");
                }

                if (!TryExecuteAutoTask(task, client))
                {
                    ExecuteLegacyAutoTask(task, client);
                }
            }
        }

        /// <summary>
        /// Executes auto tasks on all connected clients, respecting each task's execution mode.
        /// </summary>
        public void ExecuteAutoTasksOnAllClients()
        {
            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("未配置自动任务。", "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var clientsProcessed = 0;

            lock (_lockClients)
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client && client.Value != null)
                    {
                        ThreadPool.QueueUserWorkItem(_ => StartAutomatedTask(client));
                        clientsProcessed++;
                    }
                }
            }

            MessageBox.Show($"自动任务已为 {clientsProcessed} 个客户端排队（遵循各自的执行模式）。",
                           "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Resets the tracking of which task-client combinations have been executed, allowing them to run again.
        /// </summary>
        public void ResetAutoTaskTracking()
        {
            lock (_executedTaskClientCombinations)
            {
                var count = _executedTaskClientCombinations.Count;
                _executedTaskClientCombinations.Clear();
                MessageBox.Show($"已重置 {count} 个任务-客户端组合的自动任务跟踪。\n具有'每个客户端一次'模式的任务将在所有客户端上重新运行。",
                               "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Forces auto tasks to execute on selected clients, regardless of whether they've been executed before.
        /// </summary>
        public void ForceExecuteAutoTasksOnSelectedClients()
        {
            var selectedClients = GetSelectedClients();
            if (selectedClients.Length == 0)
            {
                MessageBox.Show("请先选择一个或多个客户端。", "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("未配置自动任务。", "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var client in selectedClients)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Debug.WriteLine($"Force executing automated tasks for client {client.Value?.Id}.");

                    if (lstTasks.InvokeRequired)
                    {
                        lstTasks.Invoke(new Action(() =>
                        {
                            foreach (ListViewItem item in lstTasks.Items)
                            {
                                var task = ExtractAutoTask(item);
                                if (!TryExecuteAutoTask(task, client))
                                {
                                    ExecuteLegacyAutoTask(task, client);
                                }
                            }
                        }));
                    }
                    else
                    {
                        foreach (ListViewItem item in lstTasks.Items)
                        {
                            var task = ExtractAutoTask(item);
                            if (!TryExecuteAutoTask(task, client))
                            {
                                ExecuteLegacyAutoTask(task, client);
                            }
                        }
                    }
                });
            }

            MessageBox.Show($"自动任务已为 {selectedClients.Length} 个所选客户端排队。",
                           "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Forces auto tasks to execute on all connected clients, regardless of whether they've been executed before.
        /// </summary>
        public void ForceExecuteAutoTasksOnAllClients()
        {
            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("未配置自动任务。", "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var clientsProcessed = 0;

            lock (_lockClients)
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client && client.Value != null)
                    {
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            // Force execute auto tasks without checking/updating the tracking
                            Debug.WriteLine($"Force executing automated tasks for client {client.Value?.Id}.");

                            if (lstTasks.InvokeRequired)
                            {
                                lstTasks.Invoke(new Action(() =>
                                {
                                    foreach (ListViewItem taskItem in lstTasks.Items)
                                    {
                                        var task = ExtractAutoTask(taskItem);
                                        if (!TryExecuteAutoTask(task, client))
                                        {
                                            ExecuteLegacyAutoTask(task, client);
                                        }
                                    }
                                }));
                            }
                            else
                            {
                                foreach (ListViewItem taskItem in lstTasks.Items)
                                {
                                    var task = ExtractAutoTask(taskItem);
                                    if (!TryExecuteAutoTask(task, client))
                                    {
                                        ExecuteLegacyAutoTask(task, client);
                                    }
                                }
                            }
                        });
                        clientsProcessed++;
                    }
                }
            }

            MessageBox.Show($"自动任务已为 {clientsProcessed} 个已连接客户端排队（强制执行）。",
                           "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows information about task execution modes and how to configure them.
        /// </summary>
        private void ShowTaskExecutionModeDialog()
        {
            string message = "自动任务执行模式：\n\n" +
                           "- 每个客户端一次：任务仅在每个客户端上运行一次（默认）\n" +
                           "- 每次连接：每次客户端连接时都运行任务\n\n" +
                           "要更改任务的执行模式：\n" +
                           "1. 右键点击列表中的任务\n" +
                           "2. 选择 '切换执行模式'\n\n" +
                           "执行模式显示在'参数'列中：\n" +
                           "- '[单次]' = 每个客户端一次\n" +
                           "- '[每次]' = 每次连接";

            MessageBox.Show(message, "自动任务执行模式",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Toggles the execution mode for selected tasks in the tasks list.
        /// </summary>
        private void ToggleSelectedTasksExecutionMode()
        {
            if (lstTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择一个或多个任务以切换其执行模式。",
                               "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var updatedCount = 0;
            foreach (ListViewItem item in lstTasks.SelectedItems)
            {
                var task = ExtractAutoTask(item);
                if (task != null)
                {
                    task.ExecutionMode = task.ExecutionMode == AutoTaskExecutionMode.OncePerClient
                        ? AutoTaskExecutionMode.EveryConnection
                        : AutoTaskExecutionMode.OncePerClient;

                    var newItem = CreateAutoTaskListViewItem(task);
                    var index = lstTasks.Items.IndexOf(item);
                    lstTasks.Items[index] = newItem;
                    lstTasks.Items[index].Selected = true;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                SaveAutoTasks();
                MessageBox.Show($"已切换 {updatedCount} 个任务的执行模式。",
                               "自动任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }



        private void RefreshStarButtons()
        {
            if (!lstClients.Visible)
            {
                return;
            }

            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        var visibleClients = new HashSet<Client>();

                        foreach (ListViewItem item in lstClients.Items)
                        {
                            if (item?.Tag is Client client)
                            {
                                visibleClients.Add(client);
                                AddStarButton(item, client);
                            }
                        }

                        var staleClients = _clientStarButtons.Keys
                            .Where(client => !visibleClients.Contains(client))
                            .ToList();

                        foreach (var client in staleClients)
                        {
                            RemoveStarButton(client);
                        }
                    }
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void AddStarButton(ListViewItem item, Client client)
        {
            if (item == null || client == null)
            {
                return;
            }

            if (!lstClients.Visible || item.ListView != lstClients)
            {
                RemoveStarButton(client);
                return;
            }

            if (_clientStarButtons.TryGetValue(client, out var existing) && existing != null && !existing.IsDisposed)
            {
                existing.Tag = client;
                existing.Image = Favorites.IsFavorite(client.Value.UserAtPc)
                    ? Properties.Resources.star_filled
                    : Properties.Resources.star_empty;
                UpdateStarButtonPosition(existing, item);
                return;
            }

            RemoveStarButton(client);

            var starButton = new Button
            {
                Size = new System.Drawing.Size(20, 20),
                FlatStyle = FlatStyle.Flat,
                Tag = client,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                TabStop = false
            };

            starButton.Image = Favorites.IsFavorite(client.Value.UserAtPc)
                ? Properties.Resources.star_filled
                : Properties.Resources.star_empty;

            starButton.Click += StarButton_Click;

            lstClients.Controls.Add(starButton);

            UpdateStarButtonPosition(starButton, item);
            starButton.BringToFront();

            _clientStarButtons[client] = starButton;
        }

        private void RemoveStarButton(Client client)
        {
            if (client == null)
            {
                return;
            }

            if (_clientStarButtons.TryGetValue(client, out var button))
            {
                _clientStarButtons.Remove(client);

                if (button != null)
                {
                    button.Click -= StarButton_Click;
                    lstClients.Controls.Remove(button);
                    if (!button.IsDisposed)
                    {
                        button.Dispose();
                    }
                }
            }
        }

        private void ClearAllStarButtons()
        {
            var clients = _clientStarButtons.Keys.ToList();
            foreach (var client in clients)
            {
                RemoveStarButton(client);
            }
        }

        private sealed class ClientListViewComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x is not ListViewItem left || y is not ListViewItem right)
                {
                    return 0;
                }

                var leftClient = left.Tag as Client;
                var rightClient = right.Tag as Client;

                bool leftFavorite = leftClient != null && Favorites.IsFavorite(leftClient.Value?.UserAtPc ?? string.Empty);
                bool rightFavorite = rightClient != null && Favorites.IsFavorite(rightClient.Value?.UserAtPc ?? string.Empty);

                if (leftFavorite != rightFavorite)
                {
                    return rightFavorite.CompareTo(leftFavorite);
                }

                var leftCountry = leftClient?.Value?.Country ?? "未知";
                var rightCountry = rightClient?.Value?.Country ?? "未知";
                int countryCompare = string.Compare(leftCountry, rightCountry, StringComparison.OrdinalIgnoreCase);
                if (countryCompare != 0)
                {
                    return countryCompare;
                }

                var leftUser = leftClient?.Value?.UserAtPc ?? string.Empty;
                var rightUser = rightClient?.Value?.UserAtPc ?? string.Empty;
                int userCompare = string.Compare(leftUser, rightUser, StringComparison.OrdinalIgnoreCase);
                if (userCompare != 0)
                {
                    return userCompare;
                }

                return string.Compare(left.Text, right.Text, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void UpdateStarButtonPosition(Control starControl, ListViewItem item)
        {
            if (item == null || starControl == null) return;

            var bounds = item.Bounds;
            int rightMargin = 23;

            if (lstClients.Items.Count > lstClients.ClientSize.Height / lstClients.GetItemRect(0).Height)
            {
                rightMargin += SystemInformation.VerticalScrollBarWidth;
            }

            starControl.Location = new Point(lstClients.Width - rightMargin, bounds.Top + (bounds.Height - starControl.Height) / 2);
        }

        private void StarButton_Click(object sender, EventArgs e)
        {
            if (sender is Control starControl && starControl.Tag is Client client)
            {
                Favorites.ToggleFavorite(client.Value.UserAtPc);

                if (starControl is Button button)
                {
                    button.Image = Favorites.IsFavorite(client.Value.UserAtPc) ?
                        Properties.Resources.star_filled : Properties.Resources.star_empty;
                }

                SortClientsByFavoriteStatus(forceImmediate: true);
            }
        }

        private void SortClientsByFavoriteStatus(bool forceImmediate = false)
        {
            if (lstClients == null || lstClients.IsDisposed)
            {
                return;
            }

            if (forceImmediate)
            {
                _clientSortTimer.Stop();
                _clientSortPending = false;
                PerformClientListSort();
                return;
            }

            _clientSortPending = true;
            _clientSortTimer.Stop();
            _clientSortTimer.Start();
        }

        private void ClientSortTimer_Tick(object sender, EventArgs e)
        {
            _clientSortTimer.Stop();

            if (!_clientSortPending)
            {
                return;
            }

            _clientSortPending = false;
            PerformClientListSort();
        }

        private void PerformClientListSort()
        {
            if (lstClients == null || lstClients.IsDisposed)
            {
                return;
            }

            if (!lstClients.Visible)
            {
                wpfClientsHost?.RefreshSort();
                ApplyWpfSearchFilter();
                return;
            }

            lstClients.BeginUpdate();
            try
            {
                lstClients.ListViewItemSorter = _clientListViewComparer;
                lstClients.Sort();
                lstClients.ListViewItemSorter = null;

                if (!Settings.ShowCountryGroups && lstClients.Groups.Count > 0)
                {
                    ResetCountryGroups();
                }

                _visibleClients.Clear();

                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item?.Tag is not Client client)
                    {
                        continue;
                    }

                    _visibleClients.Add(client);
                    _clientListViewItems[client] = item;

                    if (Settings.ShowCountryGroups)
                    {
                        var group = GetGroupFromCountry(client.Value?.Country ?? "未知",
                            client.Value?.CountryWithCode ?? "未知");
                        item.Group = group;
                    }
                    else if (item.Group != null)
                    {
                        item.Group = null;
                    }

                    AddStarButton(item, client);
                }
            }
            finally
            {
                lstClients.EndUpdate();
            }

            wpfClientsHost?.RefreshSort();
            ApplyWpfSearchFilter();
            RefreshStarButtons();
        }

        private static string TranslateUserStatus(UserStatus userStatus)
        {
            switch (userStatus)
            {
                case UserStatus.Active:
                    return "活跃";
                case UserStatus.Idle:
                    return "闲置";
                default:
                    return userStatus.ToString();
            }
        }

        private static string TranslateAccountType(string accountType)
        {
            if (string.IsNullOrEmpty(accountType))
                return string.Empty;

            switch (accountType)
            {
                case "Admin":
                    return "管理员";
                case "User":
                    return "普通用户";
                case "Guest":
                    return "访客";
                case "System":
                    return "系统";
                case "Unknown":
                    return "未知";
                default:
                    return accountType;
            }
        }

        private ListViewGroup GetGroupFromCountry(string country, string countryWithCode)
        {
            if (!Settings.ShowCountryGroups || lstClients == null || lstClients.IsDisposed)
            {
                return null;
            }

            if (!_countryGroups.TryGetValue(country, out var group) || group == null || !lstClients.Groups.Contains(group))
            {
                group = new ListViewGroup
                {
                    Name = country,
                    Header = countryWithCode
                };

                _countryGroups[country] = group;
                lstClients.Groups.Add(group);
            }
            else
            {
                group.Header = countryWithCode;
            }

            return group;
        }

        private void ResetCountryGroups()
        {
            if (lstClients == null || lstClients.IsDisposed)
            {
                return;
            }

            lstClients.Groups.Clear();
            _countryGroups.Clear();
        }

        private void WpfClientsHost_SelectionChanged(object sender, EventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            try
            {
                _syncingSelection = true;
                SyncListViewSelectionFromWpf();
            }
            finally
            {
                _syncingSelection = false;
            }

            UpdateWindowTitle();
        }

        private void SyncListViewSelectionFromWpf()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            var selectedClients = wpfClientsHost.SelectedClients;

            lstClients.BeginUpdate();
            try
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client)
                    {
                        item.Selected = selectedClients.Contains(client);
                    }
                }
            }
            finally
            {
                lstClients.EndUpdate();
            }
        }

        private void SyncWpfSelectionFromList()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (_syncingSelection)
            {
                return;
            }

            try
            {
                _syncingSelection = true;
                var selectedClients = lstClients.SelectedItems.Cast<ListViewItem>()
                    .Select(item => item.Tag as Client)
                    .Where(client => client != null)
                    .Cast<Client>()
                    .ToList();

                wpfClientsHost.SetSelectedClients(selectedClients);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void WpfClientsHost_FavoriteToggled(object sender, Client client)
        {
            if (client == null)
            {
                return;
            }

            Favorites.ToggleFavorite(client.Value.UserAtPc);

            lstClients.BeginInvoke((MethodInvoker)(() =>
            {
                if (_clientListViewItems.TryGetValue(client, out var item) && item != null)
                {
                    if (_clientStarButtons.TryGetValue(client, out var starButton) && starButton != null && !starButton.IsDisposed)
                    {
                        starButton.Image = Favorites.IsFavorite(client.Value.UserAtPc)
                            ? Properties.Resources.star_filled
                            : Properties.Resources.star_empty;
                    }

                    SyncWpfEntryFromListViewItem(item);
                }
            }));
        }

        private ImageSource GetFlagImageSource(int imageIndex)
        {
            return WpfFlagProvider.GetFlagByIndex(imageIndex);
        }

        private void SyncWpfEntryFromListViewItem(ListViewItem item)
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (item?.Tag is not Client client)
            {
                return;
            }

            var entry = wpfClientsHost.AddOrUpdate(client, e =>
            {
                e.Ip = item.SubItems[0].Text;
                e.Nickname = item.SubItems[1].Text;
                e.Tag = item.SubItems[2].Text;
                e.UserAtPc = item.SubItems[3].Text;
                e.Version = item.SubItems[4].Text;
                e.Status = item.SubItems[STATUS_ID].Text;
                e.CurrentWindow = item.SubItems[CURRENTWINDOW_ID].Text;
                e.UserStatus = item.SubItems[USERSTATUS_ID].Text;
                e.CountryWithCode = item.SubItems[8].Text;
                e.Country = client.Value?.Country ?? "未知";
                e.CountryCode = client.Value?.CountryCode ?? "-";
                e.OperatingSystem = item.SubItems.Count > 9 ? item.SubItems[9].Text : string.Empty;
                e.AccountType = item.SubItems.Count > 10 ? TranslateAccountType(item.SubItems[10].Text) : string.Empty;
                e.IsFavorite = Favorites.IsFavorite(client.Value.UserAtPc);
                e.ImageIndex = item.ImageIndex;
                e.FlagImage = GetFlagImageSource(item.ImageIndex);
                e.ToolTip = item.ToolTipText ?? string.Empty;
            });

            _clientEntryMap[client] = entry;
        }

        private void ApplyWpfSearchFilter()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                wpfClientsHost.ApplyFilter(null);
            }
            else
            {
                wpfClientsHost.ApplyFilter(entry => ShouldShowClientInSearch(entry.Client, entry));
            }
        }

        private void AddClientToListview(Client client)
        {
            if (client == null) return;
            string nickname = GetClientNickname(client);

            try
            {
                ListViewItem lvi = new ListViewItem(new string[]
                {
                    " " + (client.Value.PublicIP ?? client.EndPoint.Address.ToString()), nickname, client.Value.Tag,
                    client.Value.UserAtPc, client.Value.Version, "已连接", "", "活跃", client.Value.CountryWithCode,
                    client.Value.OperatingSystem, TranslateAccountType(client.Value.AccountType)
                })

                { Tag = client, ImageIndex = client.Value.ImageIndex };

                lstClients.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        lstClients.BeginUpdate();

                        _clientListViewItems[client] = lvi;

                        if (Settings.ShowCountryGroups)
                        {
                            string country = client.Value?.Country ?? "未知";
                            string countryWithCode = client.Value?.CountryWithCode ?? "未知";
                            lvi.Group = GetGroupFromCountry(country, countryWithCode);
                        }
                        else
                        {
                            lvi.Group = null;
                        }

                        if (ShouldShowClientInSearch(client, lvi))
                        {
                            lstClients.Items.Add(lvi);
                            AddStarButton(lvi, client);
                            _visibleClients.Add(client);
                        }
                        else
                        {
                            _allClientItems[client] = lvi;
                            _visibleClients.Remove(client);
                        }

                        SortClientsByFavoriteStatus();
                        lstClients.EndUpdate();
                    }
                    lvi.UseItemStyleForSubItems = false;
                    lvi.SubItems[4].ForeColor = client.Value.Version == ServerVersion.Current ? Color.Green : Color.Red;
                });
                SyncWpfEntryFromListViewItem(lvi);
                ApplyWpfSearchFilter();
                EventLog(client.Value.UserAtPc + " 已连接。", "normal");
                UpdateWindowTitle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private string GetClientNickname(Client client)
        {
            if (client?.Value?.Id == null)
                return string.Empty;

            string jsonFilePath = Path.Combine(OrionStuffDir, "client_info.json");

            try
            {
                var clientInfos = LoadClientInfos(jsonFilePath);
                if (clientInfos != null && clientInfos.ContainsKey(client.Value.Id))
                {
                    return clientInfos[client.Value.Id]?.Nickname ?? string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading client nickname: {ex.Message}");
                return string.Empty;
            }
        }

        private Dictionary<string, ClientInfo> LoadClientInfos(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new Dictionary<string, ClientInfo>();

                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return new Dictionary<string, ClientInfo>();

                try
                {
                    var clientInfos = JsonConvert.DeserializeObject<Dictionary<string, ClientInfo>>(json);
                    return clientInfos ?? new Dictionary<string, ClientInfo>();
                }
                catch (JsonException)
                {
                    Debug.WriteLine("Failed to parse");
                }

                return new Dictionary<string, ClientInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading client info: {ex.Message}");
                return new Dictionary<string, ClientInfo>();
            }
        }

        private void RemoveClientFromListview(Client client)
        {
            if (client == null) return;

            try
            {
                lstClients.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        lstClients.BeginUpdate();
                        if (_clientListViewItems.TryGetValue(client, out var listViewItem) && listViewItem != null)
                        {
                            RemoveStarButton(client);

                            if (listViewItem.ListView == lstClients)
                            {
                                lstClients.Items.Remove(listViewItem);
                            }
                        }
                        else
                        {
                            foreach (ListViewItem lvi in lstClients.Items.Cast<ListViewItem>()
                                .Where(lvi => lvi != null && client.Equals(lvi.Tag)))
                            {
                                RemoveStarButton(client);
                                lvi.Remove();
                                break;
                            }
                        }
                        lstClients.EndUpdate();
                    }
                });

                _allClientItems.Remove(client);
                _visibleClients.Remove(client);
                _clientEntryMap.Remove(client);
                _clientListViewItems.Remove(client);
                wpfClientsHost?.Remove(client);
                ApplyWpfSearchFilter();

                UpdateWindowTitle();
                EventLog(client.Value.UserAtPc + " 已断开连接。", "normal");
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region Offline clients

        private void ScheduleStatsRefresh()
        {
            if (statsElementHost == null && heatMapElementHost == null)
            {
                return;
            }

            lock (_statsRefreshLock)
            {
                if (_statsRefreshPending)
                {
                    return;
                }

                _statsRefreshPending = true;
            }

            statsElementHost?.ShowLoading();
            heatMapElementHost?.ShowLoading();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                ClientStatisticsSnapshot statsSnapshot;
                ClientGeoSnapshot geoSnapshot;

                try
                {
                    var allClients = OfflineClientRepository.GetAllClients();
                    statsSnapshot = ClientStatisticsService.CreateSnapshot(allClients);
                    geoSnapshot = ClientGeoStatisticsService.CreateSnapshot(allClients, statsSnapshot.GeneratedAtUtc);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    statsSnapshot = ClientStatisticsSnapshot.CreateError(message);
                    geoSnapshot = ClientGeoSnapshot.CreateError(message);
                }
                finally
                {
                    lock (_statsRefreshLock)
                    {
                        _statsRefreshPending = false;
                    }
                }

                if (!IsHandleCreated || IsDisposed)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        if (statsSnapshot.HasError)
                        {
                            var message = statsSnapshot.ErrorMessage ?? "未知错误";
                            statsElementHost?.ShowError(message);
                            heatMapElementHost?.ShowError(message);
                            return;
                        }

                        statsElementHost?.UpdateSnapshot(statsSnapshot);

                        if (geoSnapshot.HasError)
                        {
                            heatMapElementHost?.ShowError(geoSnapshot.ErrorMessage ?? "未知错误");
                        }
                        else
                        {
                            heatMapElementHost?.UpdateSnapshot(geoSnapshot);
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        private void ScheduleOfflineListRefresh()
        {
            lock (_offlineRefreshLock)
            {
                if (_offlineRefreshPending)
                {
                    return;
                }

                _offlineRefreshPending = true;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<OfflineClientRecord> offlineClients;

                try
                {
                    offlineClients = OfflineClientRepository.GetClientsByOnlineState(false)?.ToList()
                        ?? new List<OfflineClientRecord>();
                    ApplyOfflineNicknames(offlineClients);
                    _cachedOfflineClients = offlineClients;
                }
                catch (Exception ex)
                {
                    offlineClients = new List<OfflineClientRecord>();
                    _cachedOfflineClients = offlineClients;
                    EventLog($"加载离线客户端失败：{ex.Message}", "error");
                }
                finally
                {
                    lock (_offlineRefreshLock)
                    {
                        _offlineRefreshPending = false;
                    }
                }

                if (!IsHandleCreated || IsDisposed)
                {
                    return;
                }

                try
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        PopulateOfflineClientsList(_cachedOfflineClients);
                        UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                    }));
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        private void ApplyOfflineNicknames(IList<OfflineClientRecord> clients)
        {
            if (clients == null || clients.Count == 0)
            {
                return;
            }

            string jsonFilePath = Path.Combine(OrionStuffDir, "client_info.json");
            var nicknameMap = LoadClientInfos(jsonFilePath);

            foreach (var record in clients)
            {
                if (!string.IsNullOrEmpty(record.ClientId) && nicknameMap.TryGetValue(record.ClientId, out var info))
                {
                    record.Nickname = info?.Nickname;
                }
            }
        }

        private void PopulateOfflineClientsList(IReadOnlyList<OfflineClientRecord> clients)
        {
            if (lstOfflineClients == null || lstOfflineClients.IsDisposed)
            {
                return;
            }

            lstOfflineClients.BeginUpdate();
            try
            {
                lstOfflineClients.Items.Clear();

                if (clients == null)
                {
                    return;
                }

                foreach (var record in clients)
                {
                    var userAtPc = !string.IsNullOrEmpty(record.UserAtPc)
                        ? record.UserAtPc
                        : ComposeUserAtPc(record);

                    var lastSeen = record.LastSeenUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";
                    var firstSeen = record.FirstSeenUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                    var countryWithCode = GetCountryWithCode(record);

                    var item = new ListViewItem(new[]
                    {
                        " " + (record.PublicIP ?? "未知"),
                        record.Nickname ?? string.Empty,
                        record.Tag ?? string.Empty,
                        userAtPc,
                        record.Version ?? string.Empty,
                        lastSeen,
                        firstSeen,
                        countryWithCode,
                        record.OperatingSystem ?? string.Empty,
                        TranslateAccountType(record.AccountType) ?? string.Empty
                    })
                    {
                        Tag = record
                    };

                    if (record.ImageIndex >= 0 && record.ImageIndex < imgFlags.Images.Count)
                    {
                        item.ImageIndex = record.ImageIndex;
                    }

                    // Set version color for offline clients
                    item.UseItemStyleForSubItems = false;
                    item.SubItems[4].ForeColor = (record.Version ?? string.Empty) == ServerVersion.Current ? Color.Green : Color.Red;

                    lstOfflineClients.Items.Add(item);
                }
            }
            finally
            {
                lstOfflineClients.EndUpdate();
            }
        }

        private static string ComposeUserAtPc(OfflineClientRecord record)
        {
            var username = record.Username ?? string.Empty;
            var pcName = record.PcName ?? string.Empty;

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(pcName))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(pcName))
            {
                return $"{username}@{pcName}";
            }

            return username + pcName;
        }

        private static string GetCountryWithCode(OfflineClientRecord record)
        {
            if (string.IsNullOrEmpty(record.Country))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(record.CountryCode))
            {
                return record.Country;
            }

            return $"{record.Country} [{record.CountryCode}]";
        }

        private void UpdateOfflineTabHeader(int offlineCount)
        {
            if (tabOfflineClients == null || tabOfflineClients.IsDisposed)
            {
                return;
            }

            var title = offlineCount > 0 ? $"离线 ({offlineCount})" : "离线";
            tabOfflineClients.Text = title;
        }

        #endregion

        private readonly Dictionary<Client, Dictionary<string, object>> _pendingStatusUpdates = new Dictionary<Client, Dictionary<string, object>>();
        private readonly object _statusUpdateLock = new object();
        private System.Threading.Timer _statusUpdateTimer;
        private const int StatusUpdateDebounceMs = 50;

        private string _currentSearchFilter = "";
        private readonly Dictionary<Client, ListViewItem> _allClientItems = new Dictionary<Client, ListViewItem>();

        private void SetStatusByClient(object sender, Client client, string text)
        {
            QueueStatusUpdate(client, "status", text);
        }

        private void SetUserStatusByClient(object sender, Client client, UserStatus userStatus)
        {
            QueueStatusUpdate(client, "userStatus", TranslateUserStatus(userStatus));
        }

        private void SetUserActiveWindowByClient(object sender, Client client, string newWindow)
        {
            QueueStatusUpdate(client, "window", newWindow);
        }

        private void QueueStatusUpdate(Client client, string field, object value)
        {
            if (client == null) return;

            lock (_statusUpdateLock)
            {
                if (!_pendingStatusUpdates.ContainsKey(client))
                    _pendingStatusUpdates[client] = new Dictionary<string, object>();

                _pendingStatusUpdates[client][field] = value;

                if (_statusUpdateTimer == null)
                {
                    _statusUpdateTimer = new System.Threading.Timer(_ => ProcessStatusUpdatesTimerCallback(), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                }

                _statusUpdateTimer.Change(StatusUpdateDebounceMs, System.Threading.Timeout.Infinite);
            }
        }

        private void ProcessStatusUpdatesTimerCallback()
        {
            Dictionary<Client, Dictionary<string, object>> updates;
            lock (_statusUpdateLock)
            {
                if (_pendingStatusUpdates.Count == 0) return;
                updates = new Dictionary<Client, Dictionary<string, object>>(_pendingStatusUpdates);
                _pendingStatusUpdates.Clear();
            }

            if (updates.Count == 0) return;

            this.BeginInvoke((MethodInvoker)delegate
            {
                lstClients.BeginUpdate();
                try
                {
                    foreach (var update in updates)
                    {
                        if (!_clientListViewItems.TryGetValue(update.Key, out var item) || item == null)
                        {
                            continue;
                        }

                        foreach (var fieldUpdate in update.Value)
                        {
                            switch (fieldUpdate.Key)
                            {
                                case "status":
                                    item.SubItems[STATUS_ID].Text = fieldUpdate.Value?.ToString();
                                    break;

                                case "userStatus":
                                    item.SubItems[USERSTATUS_ID].Text = fieldUpdate.Value?.ToString();
                                    break;

                                case "window":
                                    item.SubItems[CURRENTWINDOW_ID].Text = fieldUpdate.Value?.ToString();
                                    break;
                            }
                        }

                        SyncWpfEntryFromListViewItem(item);
                    }
                }
                finally
                {
                    lstClients.EndUpdate();
                }
            });
        }

        private void SetUserClipboardByClient(object sender, Client client, string clipboardText)
        {
            // looks clean
        }

        private ListViewItem GetListViewItemByClient(Client client)
        {
            if (client == null) return null;

            if (lstClients.InvokeRequired)
            {
                return (ListViewItem)lstClients.Invoke(new Func<Client, ListViewItem>(GetListViewItemByClientInternal), client);
            }

            return GetListViewItemByClientInternal(client);
        }

        private ListViewItem GetListViewItemByClientInternal(Client client)
        {
            if (client == null) return null;
            return _clientListViewItems.TryGetValue(client, out var itemClient) ? itemClient : null;
        }

        public Client[] GetSelectedClients()
        {
            if (_currentAutoTaskContext != null)
            {
                return _currentAutoTaskContext.Clients;
            }

            if (wpfClientsHost != null)
            {
                return wpfClientsHost.SelectedClients.ToArray();
            }

            List<Client> clients = new List<Client>();

            lstClients.Invoke((MethodInvoker)delegate
            {
                lock (_lockClients)
                {
                    if (lstClients.SelectedItems.Count == 0) return;
                    clients.AddRange(
                        lstClients.SelectedItems.Cast<ListViewItem>()
                            .Where(lvi => lvi != null)
                            .Select(lvi => lvi.Tag as Client));
                }
            });

            return clients.ToArray();
        }

        private void ShowPopup(Client c)
        {
            try
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (c == null || c.Value == null) return;

                    bool wasVisible = notifyIcon.Visible;
                    if (!wasVisible)
                        notifyIcon.Visible = true;

                    notifyIcon.ShowBalloonTip(4000, string.Format("客户端已连接，来自 {0}！", c.Value.Country ?? "未知"),
                        string.Format("IP 地址: {0}\n操作系统: {1}\n用户名: {2}@{3}",
                            c.Value.PublicIP ?? "未知",
                            c.Value.OperatingSystem ?? "未知",
                            c.Value.Username ?? "未知",
                            c.Value.PcName ?? "未知"), ToolTipIcon.Info);

                    System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer();
                    hideTimer.Interval = 5000;
                    EventHandler onTick = null;
                    onTick = (s, e) =>
                    {
                        hideTimer.Stop();
                        hideTimer.Tick -= onTick;
                        hideTimer.Dispose();
                        if (!wasVisible) notifyIcon.Visible = false;
                    };
                    hideTimer.Tick += onTick;
                    hideTimer.Start();
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region "ContextMenuStrip"

        #region "Client Management"

        private void elevateClientPermissionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoAskElevate());
            }
        }

        private void elevateToSystemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoElevateSystem());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deElevateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoDeElevate());
            }
        }

        private void nicknameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmNickname frmSi = new FrmNickname(c);
                frmSi.NicknameSaved += FrmSi_NicknameSaved;
                frmSi.Show();
                frmSi.Focus();
            }
        }

        private void FrmSi_NicknameSaved(object sender, EventArgs e)
        {
            if (sender is FrmNickname frmNickname)
            {
                UpdateClientNickname(frmNickname.GetClient());
            }
        }

        private void UpdateClientNickname(Client client)
        {
            var item = GetListViewItemByClient(client);
            if (item != null)
            {
                item.SubItems[1].Text = GetClientNickname(client);
                // Synchronize with WPF ListView
                SyncWpfEntryFromListViewItem(item);
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void reconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoClientReconnect());
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoClientDisconnect());
            }
        }

        private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            bool proceed = true;
            if (_currentAutoTaskContext == null)
            {
                proceed = MessageBox.Show(
                    string.Format("确定要卸载 {0} 台计算机上的客户端吗？", clients.Length),
                    "卸载确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }

            if (!proceed)
            {
                return;
            }

            foreach (Client c in clients)
            {
                c.Send(new DoClientUninstall());
            }
        }

        #endregion "Client Management"

        #region "Administration"

        private void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmSystemInformation frmSi = FrmSystemInformation.CreateNewOrGetExisting(c);
                frmSi.Show();
                frmSi.Focus();
            }
        }

        private void fileManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmFileManager frmFm = FrmFileManager.CreateNewOrGetExisting(c);
                frmFm.Show();
                frmFm.Focus();
            }
        }

        private void startupManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmStartupManager frmStm = FrmStartupManager.CreateNewOrGetExisting(c);
                frmStm.Show();
                frmStm.Focus();
            }
        }

        private void taskManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmTaskManager frmTm = FrmTaskManager.CreateNewOrGetExisting(c);
                frmTm.Show();
                frmTm.Focus();
            }
        }

        private void remoteShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmRemoteShell frmRs = FrmRemoteShell.CreateNewOrGetExisting(c);
                frmRs.Show();
                frmRs.Focus();
            }
        }

        private void connectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmConnections frmCon = FrmConnections.CreateNewOrGetExisting(c);
                frmCon.Show();
                frmCon.Focus();
            }
        }

        private void registryEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            foreach (Client c in clients)
            {
                FrmRegistryEditor frmRe = FrmRegistryEditor.CreateNewOrGetExisting(c);
                frmRe.Show();
                frmRe.Focus();
            }
        }

        private void localFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void webFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void shutdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Shutdown });
            }
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Restart });
            }
        }

        private void standbyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Standby });
            }
        }

        #endregion "Administration"

        #region "Monitoring"

        private void remoteDesktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteDesktop.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        private void webcamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmWebcam = FrmRemoteWebcam.CreateNewOrGetExisting(c);
                frmWebcam.Show();
                frmWebcam.Focus();
            }
        }

        private void installVirtualMonitorToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void uninstallVirtualMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        #endregion "Monitoring"

        #region "User Support"

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param1))
            {
                string url = autoTask.Param1;
                bool hidden = false;
                if (!string.IsNullOrWhiteSpace(autoTask.Param3))
                {
                    bool.TryParse(autoTask.Param3, out hidden);
                }
                else if (!string.IsNullOrWhiteSpace(autoTask.Param2))
                {
                    hidden = string.Equals(autoTask.Param2, "Hidden", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(autoTask.Param2, "隐藏", StringComparison.OrdinalIgnoreCase);
                }

                foreach (Client c in clients)
                {
                    c.Send(new DoVisitWebsite
                    {
                        Url = url,
                        Hidden = hidden
                    });
                }

                return;
            }

            using (var frm = new FrmVisitWebsite(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoVisitWebsite
                        {
                            Url = frm.Url,
                            Hidden = frm.Hidden
                        });
                    }
                }
            }
        }

        private void showMessageboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param1))
            {
                string caption = autoTask.Param1;
                string text = autoTask.Param2 ?? string.Empty;
                string button = string.IsNullOrWhiteSpace(autoTask.Param3) ? "OK" : autoTask.Param3;
                string icon = string.IsNullOrWhiteSpace(autoTask.Param4) ? "None" : autoTask.Param4;

                foreach (Client c in clients)
                {
                    c.Send(new DoShowMessageBox
                    {
                        Caption = caption,
                        Text = text,
                        Button = button,
                        Icon = icon
                    });
                }

                return;
            }

            using (var frm = new FrmShowMessagebox(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoShowMessageBox
                        {
                            Caption = frm.MsgBoxCaption,
                            Text = frm.MsgBoxText,
                            Button = frm.MsgBoxButton,
                            Icon = frm.MsgBoxIcon
                        });
                    }
                }
            }
        }

        #endregion "User Support"

        #region "Quick Commands"



        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoEnableTaskManager());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void disableTaskManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoDisableTaskManager());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void enableUACToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoEnableUAC());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void disableUACToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoDisableUAC());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion "Quick Commands"



        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lstClients.SelectAllItems();
        }

        #endregion "ContextMenuStrip"

        #region "MenuStrip"

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmSettings(ListenServer))
            {
                frm.ShowDialog();
            }
        }

        private void builderToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if DEBUG
            MessageBox.Show("客户端构建器在 DEBUG 配置下不可用。\n请使用 RELEASE 配置构建项目。", "不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
#else
            using (var frm = new FrmBuilder())
            {
                frm.ShowDialog();
            }
#endif
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAbout())
            {
                frm.ShowDialog();
            }
        }

        #endregion "MenuStrip"

        #region "NotifyIcon"

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        }

        #endregion "NotifyIcon"

        private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void addKeywordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmKeywords())
            {
                frm.ShowDialog();
            }
        }

        #region Notification Centre

        private void InitializeNotificationTracking()
        {
            if (lstNoti == null)
            {
                return;
            }

            _notificationUnreadFont ??= new Font(lstNoti.Font, FontStyle.Bold);
            UpdateNotificationStatusLabel();
        }

        private bool ShouldMarkNotificationsAsReadOnArrival()
        {
            return MainTabControl?.SelectedTab == tabPage2 && WindowState != FormWindowState.Minimized && Visible && ContainsFocus;
        }

        public static void AddNotiEvent(FrmMain frmMain, string client, string keywords, string windowText)
        {
            if (frmMain.lstNoti.InvokeRequired)
            {
                frmMain.lstNoti.Invoke(new Action(() => AddNotiEvent(frmMain, client, keywords, windowText)));
                return;
            }

            var entry = new NotificationEntry
            {
                Client = client,
                Timestamp = DateTime.Now,
                Event = keywords,
                Parameter = windowText ?? string.Empty,
                IsRead = frmMain.ShouldMarkNotificationsAsReadOnArrival()
            };

            frmMain.AddNotification(entry, true);
            frmMain.ShowNotificationToast(entry);
        }

        private void AddNotification(NotificationEntry entry, bool persist)
        {
            if (entry == null) return;

            lock (_notificationLock)
            {
                _notificationHistory.Add(entry);
                var item = CreateNotificationListViewItem(entry);
                lstNoti.Items.Add(item);

                TrimNotificationHistoryLocked();
            }

            UpdateNotificationStatusLabel();

            if (persist)
            {
                SaveNotificationHistory();
            }
        }

        private ListViewItem CreateNotificationListViewItem(NotificationEntry entry)
        {
            string timestamp = entry.Timestamp.ToString("yyyy/MM/dd HH:mm:ss");
            string eventName = entry.Event ?? string.Empty;
            string parameter = entry.Parameter ?? string.Empty;
            string displayText = parameter.Length > 100 ? parameter.Substring(0, 100) + "..." : parameter;

            var item = new ListViewItem(entry.Client ?? string.Empty);
            item.SubItems.Add(timestamp);
            item.SubItems.Add(eventName);
            item.SubItems.Add(displayText);
            item.ToolTipText = parameter;
            item.Tag = entry;

            ApplyNotificationItemStyle(item, entry.IsRead);
            return item;
        }

        private void ApplyNotificationItemStyle(ListViewItem item, bool isRead)
        {
            if (item == null) return;

            if (_notificationUnreadFont == null)
            {
                _notificationUnreadFont = new Font(lstNoti.Font, FontStyle.Bold);
            }

            item.Font = isRead ? lstNoti.Font : _notificationUnreadFont;
        }

        private void UpdateNotificationStatusLabel()
        {
            int pending;
            int total;

            lock (_notificationLock)
            {
                pending = _notificationHistory.Count(n => !n.IsRead);
                total = _notificationHistory.Count;
            }

            const string centerTitle = "通知中心";

            if (lblNotificationStatus != null)
            {
                if (pending == 0)
                {
                    lblNotificationStatus.Text = total == 0
                        ? "一切正常。暂无通知。"
                        : $"一切正常。已记录 {total} 条通知。";
                }
                else
                {
                    lblNotificationStatus.Text = total > pending
                        ? $"待处理通知：{total} 条中的 {pending} 条"
                        : $"待处理通知：{pending} 条";
                }
            }

            if (tabPage2 != null)
            {
                tabPage2.Text = pending == 0
                    ? centerTitle
                    : $"{centerTitle} ({pending})";
            }

            if (notificationCentreToolStripMenuItem != null)
            {
                notificationCentreToolStripMenuItem.Text = pending == 0
                    ? centerTitle
                    : $"({pending}) {centerTitle}";
            }
        }

        private void MarkAllNotificationsAsRead()
        {
            bool anyChanges = false;

            lock (_notificationLock)
            {
                foreach (ListViewItem item in lstNoti.Items)
                {
                    if (item?.Tag is NotificationEntry entry && !entry.IsRead)
                    {
                        entry.IsRead = true;
                        ApplyNotificationItemStyle(item, true);
                        anyChanges = true;
                    }
                }
            }

            if (anyChanges)
            {
                UpdateNotificationStatusLabel();
                SaveNotificationHistory();
            }
        }

        private void ShowNotificationToast(NotificationEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            string notificationTitle = $"关键词触发：{entry.Event}";
            string parameter = entry.Parameter ?? string.Empty;
            string notificationText;

            if (!string.IsNullOrEmpty(entry.Event) && entry.Event.Contains("(Clipboard)"))
            {
                string clipboardPreview = parameter.Length > 50 ? parameter.Substring(0, 50) + "..." : parameter;
                notificationText = $"客户端：{entry.Client}\n剪贴板：{clipboardPreview}";
            }
            else
            {
                notificationText = $"客户端：{entry.Client}\n窗口：{parameter}";
            }

            try
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    bool wasVisible = notifyIcon.Visible;
                    if (!wasVisible)
                        notifyIcon.Visible = true;

                    notifyIcon.ShowBalloonTip(4000, notificationTitle, notificationText, ToolTipIcon.Info);

                    System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer();
                    hideTimer.Interval = 5000;
                    EventHandler onTick = null;
                    onTick = (s, e) =>
                    {
                        hideTimer.Stop();
                        hideTimer.Tick -= onTick;
                        hideTimer.Dispose();
                        if (!wasVisible) notifyIcon.Visible = false;
                    };
                    hideTimer.Tick += onTick;
                    hideTimer.Start();
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void SaveNotificationHistory()
        {
            try
            {
                lock (_notificationLock)
                {
                    if (!Directory.Exists(OrionStuffDir))
                    {
                        Directory.CreateDirectory(OrionStuffDir);
                    }

                    TrimNotificationHistoryLocked();

                    using var stream = File.Create(NotificationStateFilePath);
                    using var writer = new StreamWriter(stream);
                    using var jsonWriter = new JsonTextWriter(writer)
                    {
                        Formatting = Formatting.Indented
                    };

                    var serializer = JsonSerializer.CreateDefault();

                    jsonWriter.WriteStartArray();
                    foreach (var entry in _notificationHistory)
                    {
                        serializer.Serialize(jsonWriter, entry);
                    }
                    jsonWriter.WriteEndArray();
                }
            }
            catch (Exception ex)
            {
                EventLog($"保存通知失败：{ex.Message}", "error");
            }
        }

        private void LoadNotificationHistory()
        {
            if (lstNoti == null)
            {
                return;
            }

            try
            {
                bool historyTrimmed = false;

                lock (_notificationLock)
                {
                    _notificationHistory.Clear();
                    lstNoti.Items.Clear();

                    if (File.Exists(NotificationStateFilePath))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(NotificationStateFilePath);
                            if (fileInfo.Length > NotificationHistoryPruneThresholdBytes)
                            {
                                EventLog($"通知文件大小为 {fileInfo.Length / (1024 * 1024)} MB。仅加载最近的 {MaxNotificationHistoryEntries:N0} 条记录。", "info");
                            }

                            var loaded = ReadNotificationEntries(NotificationStateFilePath);
                            if (loaded.Count > 0)
                            {
                                _notificationHistory.AddRange(loaded.OrderBy(n => n.Timestamp));
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            EventLog($"解析通知失败：{jsonEx.Message}", "error");
                        }
                    }

                    historyTrimmed = TrimNotificationHistoryLocked();

                    lstNoti.BeginUpdate();
                    foreach (var entry in _notificationHistory)
                    {
                        lstNoti.Items.Add(CreateNotificationListViewItem(entry));
                    }
                    lstNoti.EndUpdate();
                }

                if (historyTrimmed)
                {
                    SaveNotificationHistory();
                }
            }
            catch (Exception ex)
            {
                EventLog($"加载通知失败：{ex.Message}", "error");
            }

            UpdateNotificationStatusLabel();
        }

        private static List<NotificationEntry> ReadNotificationEntries(string filePath)
        {
            var queue = new Queue<NotificationEntry>(MaxNotificationHistoryEntries);

            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = JsonSerializer.CreateDefault();

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    var entry = serializer.Deserialize<NotificationEntry>(jsonReader);
                    if (entry != null)
                    {
                        queue.Enqueue(entry);
                        if (queue.Count > MaxNotificationHistoryEntries)
                        {
                            queue.Dequeue();
                        }
                    }
                }
            }

            return queue.ToList();
        }

        private bool TrimNotificationHistoryLocked()
        {
            if (_notificationHistory.Count <= MaxNotificationHistoryEntries)
            {
                return false;
            }

            int removeCount = _notificationHistory.Count - MaxNotificationHistoryEntries;
            _notificationHistory.RemoveRange(0, removeCount);

            if (lstNoti != null && !lstNoti.IsDisposed && lstNoti.Items.Count > 0)
            {
                lstNoti.BeginUpdate();
                try
                {
                    int toRemove = Math.Min(removeCount, lstNoti.Items.Count);
                    while (toRemove > 0)
                    {
                        lstNoti.Items.RemoveAt(0);
                        toRemove--;
                    }
                }
                finally
                {
                    lstNoti.EndUpdate();
                }
            }

            return true;
        }

        #endregion

        private void clientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage1);
        }

        private void notificationCentreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage2);
        }

        private void offlineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabOfflineClients);
        }

        private void clearOfflineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "这将永久删除所有离线客户端记录。是否继续？";
            if (MessageBox.Show(message, "清除离线客户端", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                OfflineClientRepository.ClearOfflineClients();
                _cachedOfflineClients = Array.Empty<OfflineClientRecord>();
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(0);
                ScheduleStatsRefresh();
            }
            catch (Exception ex)
            {
                EventLog($"清除离线客户端失败：{ex.Message}", "error");
                MessageBox.Show($"清除离线客户端失败。\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void removeOfflineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstOfflineClients.SelectedItems.Count == 0)
            {
                return;
            }

            var selectedRecords = lstOfflineClients.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as OfflineClientRecord)
                .Where(record => record != null && !string.IsNullOrWhiteSpace(record.ClientId))
                .ToList();

            if (selectedRecords.Count == 0)
            {
                return;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            var clientIds = selectedRecords
                .Select(record => record.ClientId)
                .Distinct(comparer)
                .ToList();

            if (clientIds.Count == 0)
            {
                return;
            }

            var prompt = clientIds.Count == 1
                ? "删除选中的离线客户端？"
                : $"删除选中的 {clientIds.Count} 个离线客户端？";

            if (MessageBox.Show(prompt, "删除离线客户端", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                OfflineClientRepository.RemoveOfflineClients(clientIds);

                var removedSet = new HashSet<string>(clientIds, comparer);
                var remaining = _cachedOfflineClients?
                    .Where(record => record != null && (string.IsNullOrWhiteSpace(record.ClientId) || !removedSet.Contains(record.ClientId)))
                    .ToList()
                    ?? new List<OfflineClientRecord>();

                _cachedOfflineClients = remaining;
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                ScheduleStatsRefresh();
            }
            catch (Exception ex)
            {
                EventLog($"移除离线客户端失败：{ex.Message}", "error");
                MessageBox.Show($"移除离线客户端失败。\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabStats);
        }

        private void mapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabHeatMap);
        }

        private void clearSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstNoti.SelectedItems.Count == 0)
            {
                return;
            }

            bool removed = false;

            lock (_notificationLock)
            {
                foreach (ListViewItem item in lstNoti.SelectedItems.Cast<ListViewItem>().ToArray())
                {
                    if (item.Tag is NotificationEntry entry)
                    {
                        _notificationHistory.Remove(entry);
                        removed = true;
                    }
                    lstNoti.Items.Remove(item);
                }
            }

            if (removed)
            {
                SaveNotificationHistory();
                UpdateNotificationStatusLabel();
            }
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void deleteTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTasks.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in lstTasks.SelectedItems)
                {
                    lstTasks.Items.Remove(item);
                }
                SaveAutoTasks();
            }
        }

        private void autoTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage4);
        }

        private void remoteExecuteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskExecute())
            {
                frm.ShowDialog();
            }
        }

        private void shellCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskCommand())
            {
                frm.ShowDialog();
            }
        }

        private void showMessageBoxToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskMessageBox())
            {
                frm.ShowDialog();
            }
        }

        public void AddTask(string title, string param1, string param2)
        {
            AddTask(new AutoTask
            {
                Title = title ?? string.Empty,
                Param1 = param1 ?? string.Empty,
                Param2 = param2 ?? string.Empty
            });
        }

        public void AddTask(AutoTask task, bool persist = true)
        {
            if (task == null)
            {
                return;
            }

            NormalizeAutoTask(task);

            var newItem = CreateAutoTaskListViewItem(task);
            lstTasks.Items.Add(newItem);

            if (persist)
            {
                SaveAutoTasks();
            }
        }



        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmFileManager frmFm = FrmFileManager.CreateNewOrGetExisting(c);
                frmFm.Show();
                frmFm.Focus();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmRemoteShell frmRs = FrmRemoteShell.CreateNewOrGetExisting(c);
                frmRs.Show();
                frmRs.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteDesktop.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmWebcam = FrmRemoteWebcam.CreateNewOrGetExisting(c);
                frmWebcam.Show();
                frmWebcam.Focus();
            }
        }










        private void taskTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                saveFileDialog.Title = "保存日志文件";
                saveFileDialog.DefaultExt = "txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, DebugLogRichBox.Text);
                }
            }
        }

        private void saveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DebugLogRichBox.SelectedText))
            {
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                saveFileDialog.Title = "保存选中文本";
                saveFileDialog.DefaultExt = "txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, DebugLogRichBox.SelectedText);
                }
            }
        }

        private void clearLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLogRichBox.Clear();
        }

        private void clearSelectedToolStripMenuItem1_Click(object sender, EventArgs e)
        {
        }

        private void lstClients_Resize(object sender, EventArgs e)
        {
            int accountTypeIndex = -1;
            for (int i = 0; i < lstClients.Columns.Count; i++)
            {
                if (lstClients.Columns[i] == hAccountType)
                {
                    accountTypeIndex = i;
                    break;
                }
            }

            if (accountTypeIndex >= 0)
            {
                lstClients.StretchColumnByIndex(accountTypeIndex);
            }

            UpdateAllStarPositions();
        }

        private void UpdateAllStarPositions()
        {
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateAllStarPositionsInternal()));
                }
                else
                {
                    UpdateAllStarPositionsInternal();
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                EventLog($"更新星标位置出错：{ex.Message}", "error");
            }
        }

        private void UpdateAllStarPositionsInternal()
        {
            if (this.IsDisposed || !this.IsHandleCreated || lstClients == null || lstClients.IsDisposed)
                return;

            var staleClients = new List<Client>();

            foreach (var kvp in _clientStarButtons.ToList())
            {
                var client = kvp.Key;
                var button = kvp.Value;

                if (button == null || button.IsDisposed)
                {
                    staleClients.Add(client);
                    continue;
                }

                if (!_clientListViewItems.TryGetValue(client, out var item) || item == null || item.ListView != lstClients || !lstClients.Items.Contains(item))
                {
                    staleClients.Add(client);
                    continue;
                }

                UpdateStarButtonPosition(button, item);
            }

            foreach (var client in staleClients)
            {
                RemoveStarButton(client);
            }
        }

        private void lstClients_MouseWheel(object sender, MouseEventArgs e)
        {
            UpdateAllStarPositions();
        }

        private void lstClients_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!lstClients.IsStretched(hAccountType.Index))
            { lstClients.StretchColumnByIndex(hAccountType.Index); }
            UpdateAllStarPositions();
        }

        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MainTabControl.SelectedTab == tabPage1)
            {
                RefreshStarButtons();
                SortClientsByFavoriteStatus(forceImmediate: true);
            }
            else if (MainTabControl.SelectedTab == tabPage2)
            {
                MarkAllNotificationsAsRead();
            }
            else if (MainTabControl.SelectedTab == tabOfflineClients)
            {
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                ScheduleOfflineListRefresh();
                ScheduleStatsRefresh();
            }
            else if (MainTabControl.SelectedTab == tabStats)
            {
                ScheduleStatsRefresh();
            }
        }

        private void lstClients_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void remoteScriptingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param3))
            {
                string language = string.IsNullOrWhiteSpace(autoTask.Param1) ? "Powershell" : autoTask.Param1;
                string script = autoTask.Param3;
                bool hidden = false;
                if (!string.IsNullOrWhiteSpace(autoTask.Param4))
                {
                    bool.TryParse(autoTask.Param4, out hidden);
                }
                else if (!string.IsNullOrWhiteSpace(autoTask.Param2))
                {
                    hidden = string.Equals(autoTask.Param2, "Hidden", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(autoTask.Param2, "隐藏", StringComparison.OrdinalIgnoreCase);
                }

                foreach (Client c in clients)
                {
                    c.Send(new DoExecScript
                    {
                        Language = language,
                        Script = script,
                        Hidden = hidden
                    });
                }

                return;
            }

            using (var frm = new FrmRemoteScripting(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoExecScript
                        {
                            Language = frm.Lang,
                            Script = frm.Script,
                            Hidden = frm.Hidden
                        });
                    }
                }
            }
        }

        private void audioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmAudio = FrmRemoteMic.CreateNewOrGetExisting(c);
                frmAudio.Show();
                frmAudio.Focus();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmAudio = FrmRemoteMic.CreateNewOrGetExisting(c);
                frmAudio.Show();
                frmAudio.Focus();
            }
        }

        private void remoteSystemAudioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmSysAudio = FrmRemoteSystemAudio.CreateNewOrGetExisting(c);
                frmSysAudio.Show();
                frmSysAudio.Focus();
            }
        }

        private void blockIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            bool proceed = true;
            if (_currentAutoTaskContext == null)
            {
                proceed = MessageBox.Show(
                    string.Format("确定要封锁 {0} 个 IP 吗？", clients.Length),
                    "封锁确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }

            if (!proceed)
            {
                return;
            }

            string filePath = Path.Combine(OrionStuffDir, "blocked.json");
            List<string> blockedIPs = new List<string>();

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    blockedIPs = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception)
            {
            }

            foreach (Client c in clients)
            {
                string clientAddress = c.EndPoint.Address.ToString();
                if (!blockedIPs.Contains(clientAddress))
                {
                    blockedIPs.Add(clientAddress);
                }
                c.Send(new DoClientUninstall());
            }

            try
            {
                string updatedJson = JsonConvert.SerializeObject(blockedIPs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception)
            {
            }
        }

        private void remoteChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteChat.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        #region AutoTaskStuff

        private void SaveAutoTasks()
        {
            try
            {
                var tasks = lstTasks.Items.Cast<ListViewItem>()
                    .Select(ExtractAutoTask)
                    .ToList();

                File.WriteAllText(AutoTasksFilePath, JsonConvert.SerializeObject(tasks, Formatting.Indented));
            }
            catch (Exception ex)
            {
                EventLog($"保存自动任务失败：{ex.Message}", "error");
            }
        }

        private void LoadAutoTasks()
        {
            lstTasks.Items.Clear();

            if (!File.Exists(AutoTasksFilePath))
            {
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(AutoTasksFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    return;
                }

                var tasks = JsonConvert.DeserializeObject<List<AutoTask>>(fileContent);
                if (tasks == null)
                {
                    return;
                }

                foreach (var task in tasks)
                {
                    AddTask(task, persist: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载自动任务失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private AutoTask ExtractAutoTask(ListViewItem item)
        {
            if (item.Tag is AutoTask task)
            {
                task.Title = item.Text ?? string.Empty;
                task.Param1 = item.SubItems.Count > 1 ? item.SubItems[1].Text ?? string.Empty : string.Empty;
                task.Param2 = item.SubItems.Count > 2 ? item.SubItems[2].Text ?? string.Empty : string.Empty;
                NormalizeAutoTask(task);
                return task;
            }

            var taskFromItem = new AutoTask
            {
                Title = item.Text ?? string.Empty,
                Param1 = item.SubItems.Count > 1 ? item.SubItems[1].Text ?? string.Empty : string.Empty,
                Param2 = item.SubItems.Count > 2 ? item.SubItems[2].Text ?? string.Empty : string.Empty
            };

            NormalizeAutoTask(taskFromItem);
            return taskFromItem;
        }

        private void NormalizeAutoTask(AutoTask task)
        {
            if (task == null)
            {
                return;
            }

            task.Title ??= string.Empty;
            task.Param1 ??= string.Empty;
            task.Param2 ??= string.Empty;
            task.Param3 ??= string.Empty;
            task.Param4 ??= string.Empty;
            task.Identifier ??= string.Empty;

            if (string.IsNullOrWhiteSpace(task.Identifier) && _autoTaskBehaviorsByTitle.TryGetValue(task.Title, out var behavior))
            {
                task.Identifier = behavior.Id;
                if (string.IsNullOrWhiteSpace(task.Title))
                {
                    task.Title = behavior.DisplayName;
                }
            }

            EnforceConnectionTerminatingAutoTaskRules(task);
        }

        private void EnforceConnectionTerminatingAutoTaskRules(AutoTask task)
        {
            if (task == null)
            {
                return;
            }

            if (task.ExecutionMode != AutoTaskExecutionMode.EveryConnection)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(task.Identifier))
            {
                return;
            }

            if (_connectionTerminatingAutoTaskIds.Contains(task.Identifier))
            {
                task.ExecutionMode = AutoTaskExecutionMode.OncePerClient;
                EventLog($"自动任务'{task.Title}'会导致客户端断开或重新连接。执行模式已自动设置为'每个客户端一次'以防止重连循环。", "warning");
            }
        }

        private ListViewItem CreateAutoTaskListViewItem(AutoTask task)
        {
            var item = new ListViewItem(task.Title ?? string.Empty);
            item.SubItems.Add(task.Param1 ?? string.Empty);

            string executionModeText = task.ExecutionMode == AutoTaskExecutionMode.OncePerClient ? "[单次]" : "[每次]";
            string argumentsText = task.Param2 ?? string.Empty;
            if (!string.IsNullOrEmpty(argumentsText))
            {
                argumentsText = $"{executionModeText} {argumentsText}";
            }
            else
            {
                argumentsText = executionModeText;
            }

            item.SubItems.Add(argumentsText);
            item.Tag = task;
            return item;
        }

        private void InitializeAutoTaskBehaviors()
        {
            _autoTaskBehaviors.Clear();
            _autoTaskBehaviorsByTitle.Clear();

            // Administration
            RegisterDefaultAutoTask(systemInformationToolStripMenuItem, systemInformationToolStripMenuItem_Click);
            RegisterDefaultAutoTask(fileManagerToolStripMenuItem, fileManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(startupManagerToolStripMenuItem, startupManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(taskManagerToolStripMenuItem, taskManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(remoteShellToolStripMenuItem, remoteShellToolStripMenuItem_Click);
            RegisterDefaultAutoTask(connectionsToolStripMenuItem, connectionsToolStripMenuItem_Click);
            RegisterDefaultAutoTask(registryEditorToolStripMenuItem, registryEditorToolStripMenuItem_Click);

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                remoteExecuteToolStripMenuItem.Name,
                "远程执行（本地文件）",
                remoteExecuteToolStripMenuItem,
                CreateRemoteExecuteLocalAutoTask,
                (frm, client, task) => frm.ExecuteRemoteExecuteLocalAutoTask(client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                remoteExecuteToolStripMenuItem.Name,
                "远程执行（Web 文件）",
                remoteExecuteToolStripMenuItem,
                CreateRemoteExecuteWebAutoTask,
                (frm, client, task) => frm.ExecuteRemoteExecuteWebAutoTask(client, task)));

            RegisterDefaultAutoTask(shutdownToolStripMenuItem, shutdownToolStripMenuItem_Click);
            RegisterDefaultAutoTask(restartToolStripMenuItem, restartToolStripMenuItem_Click);
            RegisterDefaultAutoTask(standbyToolStripMenuItem, standbyToolStripMenuItem_Click);

            // Monitoring
            RegisterDefaultAutoTask(remoteDesktopToolStripMenuItem2, remoteDesktopToolStripMenuItem_Click);
            RegisterDefaultAutoTask(webcamToolStripMenuItem, webcamToolStripMenuItem_Click);
            RegisterDefaultAutoTask(remoteSystemAudioToolStripMenuItem, remoteSystemAudioToolStripMenuItem_Click);
            RegisterDefaultAutoTask(audioToolStripMenuItem, audioToolStripMenuItem_Click);
            RegisterDefaultAutoTask(installToolStripMenuItem, installToolStripMenuItem_Click);
            RegisterDefaultAutoTask(uninstallToolStripMenuItem, uninstallToolStripMenuItem1_Click);

            // User support
            RegisterDefaultAutoTask(remoteChatToolStripMenuItem, remoteChatToolStripMenuItem_Click);
            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                remoteScriptingToolStripMenuItem.Name,
                remoteScriptingToolStripMenuItem.Text,
                remoteScriptingToolStripMenuItem,
                CreateRemoteScriptingAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(remoteScriptingToolStripMenuItem, remoteScriptingToolStripMenuItem_Click, client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                showMessageboxToolStripMenuItem.Name,
                showMessageboxToolStripMenuItem.Text,
                showMessageboxToolStripMenuItem,
                CreateShowMessageBoxAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(showMessageboxToolStripMenuItem, showMessageboxToolStripMenuItem_Click, client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                visitWebsiteToolStripMenuItem.Name,
                visitWebsiteToolStripMenuItem.Text,
                visitWebsiteToolStripMenuItem,
                CreateVisitWebsiteAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(visitWebsiteToolStripMenuItem, visitWebsiteToolStripMenuItem_Click, client, task)));

            // Quick commands

            RegisterDefaultAutoTask(enableToolStripMenuItem, enableToolStripMenuItem_Click);
            RegisterDefaultAutoTask(disableTaskManagerToolStripMenuItem, disableTaskManagerToolStripMenuItem_Click);

            // Client management
            RegisterDefaultAutoTask(elevateClientPermissionsToolStripMenuItem, elevateClientPermissionsToolStripMenuItem_Click);
            RegisterDefaultAutoTask(elevateToSystemToolStripMenuItem, elevateToSystemToolStripMenuItem_Click);
            RegisterDefaultAutoTask(deElevateFromSystemToolStripMenuItem, deElevateToolStripMenuItem_Click);
            RegisterDefaultAutoTask(nicknameToolStripMenuItem, nicknameToolStripMenuItem_Click);
            RegisterDefaultAutoTask(blockIPToolStripMenuItem, blockIPToolStripMenuItem_Click);
            RegisterDefaultAutoTask(updateToolStripMenuItem, updateToolStripMenuItem_Click);
            RegisterDefaultAutoTask(reconnectToolStripMenuItem, reconnectToolStripMenuItem_Click);
            RegisterDefaultAutoTask(disconnectToolStripMenuItem, disconnectToolStripMenuItem_Click);
            RegisterDefaultAutoTask(uninstallToolStripMenuItem, uninstallToolStripMenuItem_Click);
        }

        private void RegisterDefaultAutoTask(ToolStripMenuItem menuItem, EventHandler handler, string displayName = null)
        {
            if (menuItem == null || handler == null)
            {
                return;
            }

            var behavior = new AutoTaskBehavior(
                menuItem.Name,
                displayName ?? menuItem.Text,
                menuItem,
                (frm, _) => new AutoTask { Title = displayName ?? menuItem.Text },
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(menuItem, handler, client, task));

            RegisterAutoTaskBehavior(behavior);
        }

        private void RegisterAutoTaskBehavior(AutoTaskBehavior behavior)
        {
            if (behavior == null || string.IsNullOrWhiteSpace(behavior.Id))
            {
                return;
            }

            _autoTaskBehaviors[behavior.Id] = behavior;

            if (!_autoTaskBehaviorsByTitle.ContainsKey(behavior.DisplayName))
            {
                _autoTaskBehaviorsByTitle[behavior.DisplayName] = behavior;
            }

            if (behavior.MenuItem != null && !_autoTaskBehaviorsByTitle.ContainsKey(behavior.MenuItem.Text))
            {
                _autoTaskBehaviorsByTitle[behavior.MenuItem.Text] = behavior;
            }
        }

        private void BuildAutoTaskMenu()
        {
            if (addTaskToolStripMenuItem == null || contextMenuStrip == null)
            {
                return;
            }

            addTaskToolStripMenuItem.DropDownItems.Clear();

            foreach (ToolStripItem item in contextMenuStrip.Items)
            {
                var cloned = CloneMenuItemForAutoTasks(item);
                if (cloned != null)
                {
                    addTaskToolStripMenuItem.DropDownItems.Add(cloned);
                }
            }

            TrimEmptySeparators(addTaskToolStripMenuItem.DropDownItems);
        }

        private ToolStripItem CloneMenuItemForAutoTasks(ToolStripItem item)
        {
            if (item is ToolStripSeparator)
            {
                return new ToolStripSeparator();
            }

            if (item is not ToolStripMenuItem menuItem)
            {
                return null;
            }

            var clone = new ToolStripMenuItem(menuItem.Text, menuItem.Image);

            if (_autoTaskBehaviors.TryGetValue(menuItem.Name, out var behavior))
            {
                clone.Tag = behavior;
                clone.Text = behavior.DisplayName;
                clone.Click += AutoTaskMenuItem_Click;
            }

            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                var clonedChild = CloneMenuItemForAutoTasks(child);
                if (clonedChild != null)
                {
                    clone.DropDownItems.Add(clonedChild);
                }
            }

            TrimEmptySeparators(clone.DropDownItems);

            if (clone.DropDownItems.Count == 0 && clone.Tag == null)
            {
                return null;
            }

            return clone;
        }

        private static void TrimEmptySeparators(ToolStripItemCollection items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is ToolStripSeparator)
                {
                    bool remove = i == 0 || i == items.Count - 1;
                    if (!remove && items[i - 1] is ToolStripSeparator)
                    {
                        remove = true;
                    }
                    if (!remove && i + 1 < items.Count && items[i + 1] is ToolStripSeparator)
                    {
                        remove = true;
                    }
                    if (remove)
                    {
                        items.RemoveAt(i);
                    }
                }
            }
        }

        private void AutoTaskMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not AutoTaskBehavior behavior)
            {
                return;
            }

            var task = behavior.CreateTask(this);
            if (task == null)
            {
                return;
            }

            task.Title = behavior.DisplayName;
            task.Identifier = behavior.Id;

            NormalizeAutoTask(task);
            AddTask(task);
        }

        private bool TryExecuteAutoTask(AutoTask task, Client client)
        {
            if (task == null || client == null)
            {
                return false;
            }

            AutoTaskBehavior behavior = null;

            if (!string.IsNullOrWhiteSpace(task.Identifier) && _autoTaskBehaviors.TryGetValue(task.Identifier, out behavior))
            {
                // behavior found by identifier
            }
            else if (!string.IsNullOrWhiteSpace(task.Title) && _autoTaskBehaviorsByTitle.TryGetValue(task.Title, out behavior))
            {
                task.Identifier = behavior.Id;
            }

            if (behavior == null)
            {
                return false;
            }

            behavior.Execute(this, client, task);
            return true;
        }

        private void ExecuteMenuItemAutoTask(ToolStripMenuItem menuItem, EventHandler handler, Client client, AutoTask task)
        {
            if (menuItem == null || handler == null || client == null)
            {
                return;
            }

            var previousContext = _currentAutoTaskContext;
            try
            {
                _currentAutoTaskContext = new AutoTaskExecutionContext(new[] { client }, task);
                handler(menuItem, EventArgs.Empty);
            }
            finally
            {
                _currentAutoTaskContext = previousContext;
            }
        }

        private void ExecuteLegacyAutoTask(AutoTask task, Client client)
        {
            if (task == null || client == null || string.IsNullOrWhiteSpace(task.Title))
            {
                return;
            }

            switch (task.Title)
            {
                case "远程执行":
                    ExecuteRemoteExecuteLocalAutoTask(client, task);
                    break;
                case "Shell 命令":
                    if (!string.IsNullOrWhiteSpace(task.Param2))
                    {
                        string host = string.IsNullOrWhiteSpace(task.Param1) ? "cmd.exe" : task.Param1;
                        client.Send(new DoSendQuickCommand { Host = host, Command = task.Param2 });
                    }
                    break;

                case "消息框":
                    client.Send(new DoShowMessageBox
                    {
                        Caption = task.Param1 ?? string.Empty,
                        Text = task.Param2 ?? string.Empty,
                        Button = string.IsNullOrWhiteSpace(task.Param3) ? "OK" : task.Param3,
                        Icon = string.IsNullOrWhiteSpace(task.Param4) ? "None" : task.Param4
                    });
                    break;
            }
        }

        private AutoTask CreateRemoteExecuteLocalAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "选择要在新客户端上执行的文件",
                Filter = "可执行文件 (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1|所有文件 (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog(form) != DialogResult.OK)
            {
                return null;
            }

            string path = openFileDialog.FileName;
            return new AutoTask
            {
                Title = menuItem.Text,
                Param1 = path,
                Param2 = Path.GetFileName(path)
            };
        }

        private void ExecuteRemoteExecuteLocalAutoTask(Client client, AutoTask task)
        {
            string filePath = task?.Param1 ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                EventLog($"自动任务远程执行已跳过 - 文件未找到：{filePath}", "error");
                return;
            }

            try
            {
                using (var fileHandler = new FileManagerHandler(client))
                using (var taskHandler = new TaskManagerHandler(client))
                {
                    MessageHandler.Register(taskHandler);
                    try
                    {
                        fileHandler.BeginUploadFile(filePath, string.Empty);
                        taskHandler.StartProcess(filePath);
                    }
                    finally
                    {
                        MessageHandler.Unregister(taskHandler);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog($"自动任务远程执行失败，文件'{filePath}'：{ex.Message}", "error");
            }
        }

        private AutoTask CreateRemoteExecuteWebAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            string url = string.Empty;
            if (InputBox.Show("远程执行（Web 文件）", "输入要下载并执行的 URL：", ref url) != DialogResult.OK)
            {
                return null;
            }

            url = (url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(form, "URL 不能为空。", "远程执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var updateResult = MessageBox.Show(form, "将此载荷视为更新？", "远程执行", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (updateResult == DialogResult.Cancel)
            {
                return null;
            }

            bool isUpdate = updateResult == DialogResult.Yes;

            var reflectionResult = MessageBox.Show(form, "使用 .NET 反射在内存中执行（适用于托管载荷）？", "远程执行", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (reflectionResult == DialogResult.Cancel)
            {
                return null;
            }

            bool executeInMemory = reflectionResult == DialogResult.Yes;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            return new AutoTask
            {
                Title = menuItem.Text,
                Param1 = url,
                Param2 = isUpdate ? "更新模式" : "执行",
                Param3 = isUpdate.ToString(),
                Param4 = executeInMemory.ToString()
            };
        }

        private void ExecuteRemoteExecuteWebAutoTask(Client client, AutoTask task)
        {
            string url = task?.Param1 ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            bool isUpdate = bool.TryParse(task?.Param3, out var update) && update;
            bool executeInMemory = bool.TryParse(task?.Param4, out var reflection) && reflection;

            try
            {
                using (var taskHandler = new TaskManagerHandler(client))
                {
                    taskHandler.StartProcessFromWeb(url, isUpdate, executeInMemory);
                }
            }
            catch (Exception ex)
            {
                EventLog($"自动任务远程执行（Web）失败，URL'{url}'：{ex.Message}", "error");
            }
        }

        private AutoTask CreateRemoteScriptingAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmRemoteScripting(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK)
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.Lang ?? string.Empty,
                    Param2 = dialog.Hidden ? "隐藏" : "可见",
                    Param3 = dialog.Script ?? string.Empty,
                    Param4 = dialog.Hidden.ToString()
                };
            }
        }

        private AutoTask CreateShowMessageBoxAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmShowMessagebox(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK)
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.MsgBoxCaption ?? string.Empty,
                    Param2 = dialog.MsgBoxText ?? string.Empty,
                    Param3 = dialog.MsgBoxButton ?? "OK",
                    Param4 = dialog.MsgBoxIcon ?? "None"
                };
            }
        }

        private AutoTask CreateVisitWebsiteAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmVisitWebsite(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Url))
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.Url ?? string.Empty,
                    Param2 = dialog.Hidden ? "隐藏" : "可见",
                    Param3 = dialog.Hidden.ToString()
                };
            }
        }

        private sealed class AutoTaskExecutionContext
        {
            public AutoTaskExecutionContext(Client[] clients, AutoTask task)
            {
                Clients = clients ?? Array.Empty<Client>();
                Task = task;
            }

            public Client[] Clients { get; }

            public AutoTask Task { get; }
        }

        private sealed class AutoTaskBehavior
        {
            private readonly Func<FrmMain, ToolStripMenuItem, AutoTask> _createTask;
            private readonly Action<FrmMain, Client, AutoTask> _executeTask;

            public AutoTaskBehavior(string id, string displayName, ToolStripMenuItem menuItem, Func<FrmMain, ToolStripMenuItem, AutoTask> createTask, Action<FrmMain, Client, AutoTask> executeTask)
            {
                Id = id ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
                MenuItem = menuItem;
                _createTask = createTask ?? ((form, item) => null);
                _executeTask = executeTask ?? ((form, client, task) => { });
            }

            public string Id { get; }

            public string DisplayName { get; }

            public ToolStripMenuItem MenuItem { get; }

            public AutoTask CreateTask(FrmMain form)
            {
                return _createTask(form, MenuItem);
            }

            public void Execute(FrmMain form, Client client, AutoTask task)
            {
                _executeTask(form, client, task);
            }
        }

        #region "Search Functionality"

        private TextBox _searchTextBox;
        private Label _searchLabel;
        private Label _searchResultsLabel;

        private void InitializeSearch()
        {
            _searchTextBox = new TextBox
            {
                Visible = false,
                Location = new System.Drawing.Point(220, 10),
                Size = new System.Drawing.Size(300, 23),
                TabIndex = 0,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                PlaceholderText = "搜索客户端...（按 Esc 关闭）"
            };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            _searchTextBox.KeyDown += SearchTextBox_KeyDown;

            _searchLabel = new Label
            {
                Text = "🔍 搜索：",
                Visible = false,
                Location = new System.Drawing.Point(150, 13),
                Size = new System.Drawing.Size(65, 15),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(64, 64, 64)
            };

            _searchResultsLabel = new Label
            {
                Text = "",
                Visible = false,
                Location = new System.Drawing.Point(530, 13),
                Size = new System.Drawing.Size(200, 15),
                Font = new System.Drawing.Font("Segoe UI", 8F),
                ForeColor = System.Drawing.Color.FromArgb(96, 96, 96)
            };

            tabPage1.Controls.Add(_searchTextBox);
            tabPage1.Controls.Add(_searchLabel);
            tabPage1.Controls.Add(_searchResultsLabel);

            _searchTextBox.BringToFront();
            _searchLabel.BringToFront();
            _searchResultsLabel.BringToFront();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                ShowSearch();
                return true;
            }
            else if (keyData == Keys.Escape && _searchTextBox.Visible)
            {
                HideSearch();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowSearch()
        {
            if (MainTabControl.SelectedTab != tabPage1) return;

            _searchLabel.Visible = true;
            _searchTextBox.Visible = true;
            _searchResultsLabel.Visible = true;
            _searchTextBox.Focus();
            _searchTextBox.SelectAll();
            UpdateSearchResultsCount();
        }

        private void HideSearch()
        {
            _searchLabel.Visible = false;
            _searchTextBox.Visible = false;
            _searchResultsLabel.Visible = false;
            _currentSearchFilter = "";
            _searchTextBox.Text = "";
            ApplySearchFilter();
            lstClients.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideSearch();
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            _currentSearchFilter = _searchTextBox.Text.Trim();
            ApplySearchFilter();
            UpdateSearchResultsCount();
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                ShowAllClients();
                return;
            }

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            lstClients.BeginUpdate();
            try
            {
                var itemsToRemove = new List<ListViewItem>();

                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client)
                    {
                        bool matches = false;

                        for (int i = 0; i < item.SubItems.Count; i++)
                        {
                            string text = item.SubItems[i].Text?.ToLowerInvariant() ?? "";
                            if (text.Contains(searchLower))
                            {
                                matches = true;
                                break;
                            }
                        }

                        if (!matches)
                        {
                            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? "";
                            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? "").ToLowerInvariant();

                            if (nickname.Contains(searchLower) || publicIp.Contains(searchLower))
                            {
                                matches = true;
                            }
                        }

                        if (!matches)
                        {
                            itemsToRemove.Add(item);
                        }
                    }
                }

                foreach (var item in itemsToRemove)
                {
                    if (item.Tag is Client client)
                    {
                        if (!_allClientItems.ContainsKey(client))
                        {
                            _allClientItems[client] = item;
                        }
                        _visibleClients.Remove(client);
                        RemoveStarButton(client);
                    }
                    lstClients.Items.Remove(item);
                }
            }
            finally
            {
                lstClients.EndUpdate();
            }

            ApplyWpfSearchFilter();
        }

        private void ShowAllClients()
        {
            lstClients.BeginUpdate();
            try
            {
                foreach (var kvp in _allClientItems)
                {
                    var client = kvp.Key;
                    var item = kvp.Value;

                    if (!lstClients.Items.Contains(item))
                    {
                        if (client.Value != null)
                        {
                            string country = client.Value.Country ?? "未知";
                            string countryWithCode = client.Value.CountryWithCode ?? "未知";
                            item.Group = GetGroupFromCountry(country, countryWithCode);
                        }
                        lstClients.Items.Add(item);
                        _visibleClients.Add(client);
                        _clientListViewItems[client] = item;
                        AddStarButton(item, client);
                        SyncWpfEntryFromListViewItem(item);
                    }
                }
                _allClientItems.Clear();

                SortClientsByFavoriteStatus(forceImmediate: true);
            }
            finally
            {
                lstClients.EndUpdate();
            }

            ApplyWpfSearchFilter();
        }

        private bool ShouldShowClientInSearch(Client client, ListViewItem item)
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
                return true;

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            for (int i = 0; i < item.SubItems.Count; i++)
            {
                string text = item.SubItems[i].Text?.ToLowerInvariant() ?? "";
                if (text.Contains(searchLower))
                {
                    return true;
                }
            }

            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? "";
            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? "").ToLowerInvariant();

            if (nickname.Contains(searchLower) || publicIp.Contains(searchLower))
            {
                return true;
            }

            return false;
        }

        private bool ShouldShowClientInSearch(Client client, ClientListEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_currentSearchFilter))
                return true;

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            static bool Matches(string value, string search)
                => !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

            if (Matches(entry.Ip, searchLower) || Matches(entry.Nickname, searchLower) || Matches(entry.Tag, searchLower) ||
                Matches(entry.UserAtPc, searchLower) || Matches(entry.Version, searchLower) || Matches(entry.Status, searchLower) ||
                Matches(entry.CurrentWindow, searchLower) || Matches(entry.UserStatus, searchLower) || Matches(entry.CountryWithCode, searchLower) ||
                Matches(entry.OperatingSystem, searchLower) || Matches(entry.AccountType, searchLower))
            {
                return true;
            }

            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? string.Empty;
            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? string.Empty).ToLowerInvariant();

            return nickname.Contains(searchLower) || publicIp.Contains(searchLower);
        }

        private void UpdateSearchResultsCount()
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                _searchResultsLabel.Text = "";
                return;
            }

            int visibleCount = lstClients.Items.Count;
            int totalCount = visibleCount + _allClientItems.Count;

            if (visibleCount == totalCount)
            {
                _searchResultsLabel.Text = $"显示全部 {totalCount} 个客户端";
            }
            else
            {
                _searchResultsLabel.Text = $"显示 {totalCount} 个客户端中的 {visibleCount} 个";
            }
        }

        #endregion

        #region Plugin System

        private void InitializePlugins()
        {
            try
            {
                _serverContext = new ServerContext(this);
                _pluginManager = new PluginManager(_serverContext);
                _pluginManager.PluginsChanged += OnPluginsChanged;

                var pluginsDir = Path.Combine(Application.StartupPath, "Plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    Directory.CreateDirectory(pluginsDir);
                }

                _pluginManager.LoadFrom(pluginsDir);

                _clientPluginCatalog = new ClientPluginCatalog(_serverContext);
                _clientPluginCatalog.PluginsChanged += OnClientPluginsChanged;
                _clientPluginCatalog.LoadFrom(pluginsDir);

                ApplyUIExtensions();

                UpdatePluginStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing plugins: {ex.Message}");
            }
        }

        private void OnPluginsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                // 改 BeginInvoke：避免在后台线程同步阻塞等待 UI，进而与 PluginManager._lock 形成死锁。
                BeginInvoke(new Action<object, EventArgs>(OnPluginsChanged), sender, e);
                return;
            }

            ApplyUIExtensions();
            UpdatePluginStatus();
        }

        private void OnClientPluginsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, EventArgs>(OnClientPluginsChanged), sender, e);
                return;
            }

            var currentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_clientPluginCatalog?.Plugins is { Count: > 0 } plugins)
            {
                foreach (var descriptor in plugins)
                {
                    currentKeys.Add(descriptor.CacheKey);
                }
            }

            lock (_clientAutoPluginLock)
            {
                if (currentKeys.Count == 0)
                {
                    _clientAutoPluginState.Clear();
                }
                else
                {
                    foreach (var state in _clientAutoPluginState.Values)
                    {
                        state.RemoveWhere(key => !currentKeys.Contains(key));
                    }
                }
            }

            UpdatePluginStatus();

            var clients = ListenServer?.ConnectedClients;
            if (clients == null)
            {
                return;
            }

            foreach (var client in clients)
            {
                if (client == null || !client.Connected) continue;
                SendClientOnlyPlugins(client);
            }
        }

        private void ApplyUIExtensions()
        {
            try
            {
                UIExtensionManager.ApplyFormCustomizations(this);
                UIExtensionManager.ApplyControlCustomizations(this);
                var tabControl = FindControl<TabControl>(this);
                if (tabControl != null)
                {
                    var customTabs = UIExtensionManager.GetCustomTabs();
                    foreach (var tab in customTabs)
                    {
                        if (!tabControl.TabPages.Contains(tab))
                        {
                            tabControl.TabPages.Add(tab);
                        }
                    }
                }

                var toolStrip = FindControl<ToolStrip>(this);
                if (toolStrip != null)
                {
                    var customItems = UIExtensionManager.GetCustomToolbarItems();
                    foreach (var item in customItems)
                    {
                        if (!toolStrip.Items.Contains(item))
                        {
                            toolStrip.Items.Add(item);
                        }
                    }
                }

                var menuStrip = FindControl<MenuStrip>(this);
                if (menuStrip != null)
                {
                    var customMenuItems = UIExtensionManager.GetCustomMenuItems();
                    foreach (var item in customMenuItems)
                    {
                        // Add to a specific menu or create a new one
                        var toolsMenu = menuStrip.Items.Find("toolsToolStripMenuItem", true).FirstOrDefault();
                        if (toolsMenu is ToolStripMenuItem toolsMenuItem)
                        {
                            toolsMenuItem.DropDownItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying UI extensions: {ex.Message}");
            }
        }

        private T FindControl<T>(Control parent) where T : Control
        {
            if (parent is T target)
                return target;

            foreach (Control child in parent.Controls)
            {
                var found = FindControl<T>(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void UpdatePluginStatus()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdatePluginStatus));
                return;
            }

            var enabledPlugins = 0;
            var clientPluginCount = 0;
            var connectedCount = 0;

            try
            {
                enabledPlugins = _pluginManager?.Plugins?.Count ?? 0;
                clientPluginCount = _clientPluginCatalog?.Plugins?.Count ?? 0;
                connectedCount = ListenServer?.ConnectedClients?.Length ?? 0;
            }
            catch (Exception ex)
            {
                EventLog("插件状态更新错误：" + ex.Message, "error");
            }
            finally
            {
                // Always update status bar, even if there was an error
                listenToolStripStatusLabel.Text = ResolveListeningStatusText();
                connectedToolStripStatusLabel.Text = $"已连接：{connectedCount} | 服务器插件：{enabledPlugins} | 客户端插件：{clientPluginCount}";
            }
        }

        private void LoadPluginsForClient(Client client)
        {
            try
            {
                if (_clientsWithPluginsLoaded.Contains(client))
                {
                    return;
                }

                if (_pluginManager?.Plugins != null)
                {
                    foreach (var plugin in _pluginManager.Plugins)
                    {
                        if (plugin is IServerPlugin serverPlugin && serverPlugin.AutoLoadToClients)
                        {
                            // Load universal plugins to the client
                            LoadUniversalPluginToClient(client, serverPlugin);
                        }
                    }
                }

                SendClientOnlyPlugins(client);

                _clientsWithPluginsLoaded.Add(client);
            }
            catch (Exception ex)
            {
                EventLog($"为客户端 {client.EndPoint} 加载插件出错：{ex.Message}", "error");
            }
        }

        private void SendClientOnlyPlugins(Client client)
        {
            if (_clientPluginCatalog == null)
            {
                return;
            }

            var plugins = _clientPluginCatalog.Plugins;
            if (plugins == null || plugins.Count == 0)
            {
                return;
            }

            List<ClientPluginDescriptor> pending;
            lock (_clientAutoPluginLock)
            {
                if (!_clientAutoPluginState.TryGetValue(client, out var sentSet))
                {
                    sentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _clientAutoPluginState[client] = sentSet;
                }

                pending = new List<ClientPluginDescriptor>();
                foreach (var descriptor in plugins)
                {
                    if (sentSet.Add(descriptor.CacheKey))
                    {
                        pending.Add(descriptor);
                    }
                }
            }

            foreach (var descriptor in pending)
            {
                try
                {
                    PushSender.LoadUniversalPlugin(client, descriptor.PluginId, descriptor.AssemblyBytes, descriptor.InitData, descriptor.TypeName, "Initialize");
                    EventLog($"已分发客户端插件 '{descriptor.PluginId}' v{descriptor.Version} 到 {client.EndPoint}", "info");
                }
                catch (Exception ex)
                {
                    EventLog($"发送客户端插件 '{descriptor.PluginId}' 到 {client.EndPoint} 失败：{ex.Message}", "error");
                }
            }
        }

        private void LoadUniversalPluginToClient(Client client, IServerPlugin serverPlugin)
        {
            try
            {
                // Get the plugin's client module bytes
                var pluginType = serverPlugin.GetType();
                var asm = pluginType.Assembly;

                // Look for embedded client module
                var clientModuleName = $"{pluginType.Name}.ClientModule.dll";
                var resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(clientModuleName, StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using (var stream = asm.GetManifestResourceStream(resourceName))
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        var clientBytes = ms.ToArray();

                        // Send the plugin to the client
                        var pluginId = $"{pluginType.Name}_{Guid.NewGuid():N}";
                        PushSender.LoadUniversalPlugin(client, pluginId, clientBytes, null,
                            $"{pluginType.Name}.ClientModule", "Initialize");
                    }
                }
                else
                {
                    // Try to load from file system
                    var pluginsDir = Path.Combine(Application.StartupPath, "Plugins");
                    var clientModulePath = Path.Combine(pluginsDir, $"{pluginType.Name}.ClientModule.dll");

                    if (File.Exists(clientModulePath))
                    {
                        var clientBytes = File.ReadAllBytes(clientModulePath);
                        var pluginId = $"{pluginType.Name}_{Guid.NewGuid():N}";
                        PushSender.LoadUniversalPlugin(client, pluginId, clientBytes, null,
                            $"{pluginType.Name}.ClientModule", "Initialize");
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog($"加载通用插件 {serverPlugin.GetType().Name} 到客户端出错：{ex.Message}", "error");
            }
        }

        private void UpdatePluginManagerWindow()
        {
            if (_pluginManagerForm?.IsDisposed == false)
            {
                _pluginManagerForm.Refresh();
            }
        }

        private void pluginManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_pluginManagerForm?.IsDisposed != false)
                {
                    _pluginManagerForm = new FrmPluginManager(_pluginManager);
                }
                _pluginManagerForm.Show();
                _pluginManagerForm.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开插件管理器出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Plugin System

        private void openClientFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmFileManager.OpenDownloadFolderFor(c);
            }

        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                //check if client is admin
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoInstallVirtualMonitor());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void uninstallToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                //check if client is admin
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoUninstallVirtualMonitor());
                }
                else
                {
                    MessageBox.Show("客户端未以管理员身份运行。请提升客户端权限后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void remoteExecuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void lockScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Lockscreen });
            }
        }

        private void deleteTempDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "这将删除客户端临时文件夹中的所有内容，包括释放的临时文件、DLL 以及其他写入磁盘的文件。\n\n是否继续？",
                "确认清理",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoClearTempDirectory());
            }
        }

        private void ApplyHighDpiImages()
        {
            float scale = DpiImageScaling.GetScaleFactor(this);

            // 通用递归适配（覆盖控件树里的 ToolStrip / 右键菜单 / ListView 的 ImageList，如国旗 imgFlags）。
            DpiImageScaling.ApplyToForm(this);

            DpiImageScaling.ApplyToToolStrip(menuStrip, scale);
            DpiImageScaling.ApplyToToolStrip(statusStrip, scale);
            DpiImageScaling.ApplyToToolStrip(contextMenuStrip, scale);
            DpiImageScaling.ApplyToToolStrip(OfflineClientsContextMenuStrip, scale);
            DpiImageScaling.ApplyToToolStrip(DebugContextMenuStrip, scale);
            DpiImageScaling.ApplyToToolStrip(NotificationContextMenuStrip, scale);
            DpiImageScaling.ApplyToToolStrip(TasksContextMenuStrip, scale);

            // 客户端预览下方的快捷按钮：改用 BackgroundImage + Zoom，
            // 让图标随（框架已按 DPI 放大的）按钮尺寸自动等比缩放。
            DpiImageScaling.ApplyToIconButton(btnQuickRemoteDesktop, scale);
            DpiImageScaling.ApplyToIconButton(btnQuickWebcam, scale);
            DpiImageScaling.ApplyToIconButton(btnQuickListenToMicrophone, scale);
            DpiImageScaling.ApplyToIconButton(btnQuickFileExplorer, scale);
            DpiImageScaling.ApplyToIconButton(btnQuickRemoteShell, scale);
            DpiImageScaling.ApplyToIconButton(btnQuickFileTransfer, scale);

            var quickTip = new ToolTip { InitialDelay = 300 };
            quickTip.SetToolTip(btnQuickRemoteDesktop, "远程桌面");
            quickTip.SetToolTip(btnQuickWebcam, "摄像头");
            quickTip.SetToolTip(btnQuickListenToMicrophone, "监听麦克风");
            quickTip.SetToolTip(btnQuickFileExplorer, "文件管理");
            quickTip.SetToolTip(btnQuickRemoteShell, "远程终端");
            quickTip.SetToolTip(btnQuickFileTransfer, "文件传输");
        }
    }

    public class NotificationEntry
    {
        public string Client { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Parameter { get; set; }
        public bool IsRead { get; set; }
    }

    public class AutoTask
    {
        public string Title { get; set; } = string.Empty;
        public string Param1 { get; set; } = string.Empty;
        public string Param2 { get; set; } = string.Empty;
        public string Param3 { get; set; } = string.Empty;
        public string Param4 { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public AutoTaskExecutionMode ExecutionMode { get; set; } = AutoTaskExecutionMode.OncePerClient;
    }

    #endregion AutoTaskStuff

    public class ClientInfo
    {
        public string ClientId { get; set; }
        public string Nickname { get; set; }
    }

    public sealed class ServerContext : IServerContext
    {
        private readonly FrmMain _form;
        private readonly List<ToolStripMenuItem> _pluginMenuItems = new List<ToolStripMenuItem>();
        private readonly object _pluginTag = new object();

        public ServerContext(FrmMain form) { _form = form; }
        public Form MainForm => _form;
        public OrionServer Server => _form.ListenServer;
        public void Log(string message) { _form.EventLog(message, "info"); }

        public void ClearPluginMenuItems()
        {
            if (_form == null || _form.IsDisposed) return;
            // 改 BeginInvoke：ReloadPlugins 常在后台线程调用本方法，同步 Invoke 会让后台线程
            // 阻塞等待 UI 线程，与 UI 线程的锁形成死锁（「主线程等后台 / 后台等主线程」）。
            // BeginInvoke 只投递不等待，彻底断开该等待环。
            _form.BeginInvoke((MethodInvoker)delegate
            {
                if (_form.IsDisposed || _form.contextMenuStrip == null) return;
                RemovePluginItems(_form.contextMenuStrip.Items);
                _pluginMenuItems.Clear();
                _form.contextMenuStrip.Refresh();
            });
        }

        private void RemovePluginItems(ToolStripItemCollection root)
        {
            if (root == null) return;
            for (int i = root.Count - 1; i >= 0; i--)
            {
                var item = root[i];
                if (item is ToolStripMenuItem mi)
                {
                    RemovePluginItems(mi.DropDownItems);
                }

                if (ReferenceEquals(item.Tag, _pluginTag))
                {
                    root.RemoveAt(i);
                    item.Dispose();
                }
            }
        }

        public void AddClientContextMenuItem(string text, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                var item = EnsureLeafItem(_form.contextMenuStrip.Items, text, onClick);
            });
        }
        public void AddClientContextMenuItem(string text, Icon icon, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                var item = EnsureLeafItem(_form.contextMenuStrip.Items, text, onClick, icon);
            });
        }
        public void AddClientContextMenuItem(string section, string text, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                ToolStripMenuItem parent = EnsureSection(_form.contextMenuStrip.Items, section);
                EnsureLeafItem(parent.DropDownItems, text, onClick);
            });
        }
        public void AddClientContextMenuItem(string section, string text, Icon icon, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                ToolStripMenuItem parent = EnsureSection(_form.contextMenuStrip.Items, section, icon);
                EnsureLeafItem(parent.DropDownItems, text, onClick);
            });
        }
        public void AddClientContextMenuItem(string[] sections, string text, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                ToolStripMenuItem parent = EnsureSection(_form.contextMenuStrip.Items, sections[0]);
                for (int i = 1; i < sections.Length; i++)
                {
                    parent = EnsureSection(parent.DropDownItems, sections[i]);
                }
                EnsureLeafItem(parent.DropDownItems, text, onClick);
            });
        }
        public void AddClientContextMenuItem(string[] sections, string text, Icon icon, Action<IReadOnlyList<Client>> onClick)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                if (_form.contextMenuStrip == null) return;
                ToolStripMenuItem parent = EnsureSection(_form.contextMenuStrip.Items, sections[0], icon);
                for (int i = 1; i < sections.Length; i++)
                {
                    parent = EnsureSection(parent.DropDownItems, sections[i]);
                }
                EnsureLeafItem(parent.DropDownItems, text, onClick);
            });
        }

        private ToolStripMenuItem EnsureLeafItem(ToolStripItemCollection root, string text, Action<IReadOnlyList<Client>> onClick, Icon icon = null)
         {
             var found = root.OfType<ToolStripMenuItem>().FirstOrDefault(mi => mi.Text == text);
             if (found != null && ReferenceEquals(found.Tag, _pluginTag))
             {
                 root.Remove(found);
                 found.Dispose();
                 found = null;
             }

             if (found == null)
             {
                 found = CreatePluginLeafItem(text);
                 if (icon != null) ApplyMenuIcon(found, icon);
                found.Click += (s, e) =>
                {
                    var clients = _form.GetSelectedClients();
                    onClick?.Invoke(clients);
                };
                InsertPluginItem(root, found);
                _pluginMenuItems.Add(found);
             }
 
             return found;
         }
         public void AddClientContextMenuItemPath(string path, string text, Action<IReadOnlyList<Client>> onClick)
        {
            var sections = path.Split('\\', '/');
            AddClientContextMenuItem(sections, text, onClick);
        }
        public void AddClientContextMenuItemPath(string path, string text, Icon icon, Action<IReadOnlyList<Client>> onClick)
        {
            var sections = path.Split('\\', '/');
            AddClientContextMenuItem(sections, text, icon, onClick);
        }
        public void ApplyTheme(Action<Form> apply)
        {
            if (_form == null || _form.IsDisposed) return;
            _form.Invoke((MethodInvoker)delegate
            {
                apply?.Invoke(_form);
            });
        }

        private ToolStripMenuItem EnsureSection(ToolStripItemCollection root, string name, Icon icon = null)
        {
            var found = root.OfType<ToolStripMenuItem>().FirstOrDefault(mi => mi.Text == name);
            if (found == null)
            {
                found = new ToolStripMenuItem(name);
                found.Tag = _pluginTag;
                ApplyMenuIcon(found, icon);
                InsertPluginItem(root, found);
                _pluginMenuItems.Add(found);
            }
            else if (icon != null && found.Image == null)
            {
                ApplyMenuIcon(found, icon);
            }

            ConfigurePluginSectionDropDown(found);
            return found;
        }

        // 所有插件新建的右键菜单统一放在「客户端管理」之下、「打开客户端文件夹」之上这一区域。
        // 顶层菜单（contextMenuStrip.Items）插入到「打开客户端文件夹」项之前；
        // 嵌套子菜单（DropDownItems）维持原样追加在末尾。
        private void InsertPluginItem(ToolStripItemCollection root, ToolStripMenuItem item)
        {
            int idx = GetPluginInsertIndex(root);
            if (idx >= root.Count) root.Add(item);
            else root.Insert(idx, item);
        }

        private int GetPluginInsertIndex(ToolStripItemCollection root)
        {
            if (root != _form.contextMenuStrip.Items) return root.Count;

            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is ToolStripMenuItem tsmi && tsmi.Name == "openClientFolderToolStripMenuItem")
                    return i;
            }
            return root.Count;
        }

        private static void ConfigurePluginSectionDropDown(ToolStripMenuItem section)
        {
            if (section?.DropDown is ToolStripDropDownMenu dropDownMenu)
            {
                // 保留图片边距，使分组内带图标的插件命令项能够正常显示图标。
                // 不带图标的项仍按文字渲染，仅左侧多出一列空白边距。
                dropDownMenu.ShowImageMargin = true;
            }
        }

        private ToolStripMenuItem CreatePluginLeafItem(string text)
        {
            return new ToolStripMenuItem(text)
            {
                Tag = _pluginTag,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
        }

        private void ApplyMenuIcon(ToolStripMenuItem item, Icon icon)
        {
            if (item == null || icon == null)
            {
                return;
            }

            // 直接用原图，交给容器 ImageScalingSize（按 DPI 绝对像素）统一约束，不替换 bitmap。
            item.Image = icon.ToBitmap();
            // 命令项默认 DisplayStyle = Text 会抑制图标渲染；仅当确实带图标时切回图文同显。
            item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            float scale = DpiImageScaling.GetScaleFactor(_form);
            DpiImageScaling.ApplyToPluginMenuItem(item, scale);
        }
    }
}