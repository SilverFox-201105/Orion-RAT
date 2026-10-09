using Orion.Server.Forms.DarkMode;
using Orion.Server.Plugins;
using Orion.Server.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmPluginManager : Form
    {
        private readonly PluginManager _pluginManager;
        private readonly List<PluginInfo> _pluginInfos = new List<PluginInfo>();
        private int _lastSortColumn = -1;
        private bool _sortAscending = true;

        public FrmPluginManager(PluginManager pluginManager)
        {
            try
            {
                _pluginManager = pluginManager;
                InitializeComponent();
                DarkModeManager.ApplyDarkMode(this);


                this.listView.Resize += (s, e) => AdjustColumnWidths();
                this.Resize += (s, e) => AdjustColumnWidths();
                AdjustColumnWidths();

                LoadPlugins();
                SetupEventHandlers();

                this.ShowInTaskbar = true;
                this.TopMost = false;

                try
                {
                    var res = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
                    var settingsIcon = res.GetObject("$this.Icon") as Icon;
                    this.Icon = settingsIcon ?? this.Owner?.Icon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    this.ShowIcon = true;
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化插件管理器时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public class NonResizableListView : ListView
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x0201)
                {
                    var hitTest = this.HitTest(new Point((int)m.LParam & 0xFFFF, (int)m.LParam >> 16));
                    if (hitTest.Location == ListViewHitTestLocations.None)
                    {
                        return;
                    }
                }
                base.WndProc(ref m);
            }
        }

        private void AdjustColumnWidths()
        {
            try
            {
                int totalWidth = this.listView.ClientSize.Width;
                if (totalWidth <= 0) return;

                int versionWidth = 110;
                int statusWidth = 110;
                int actionsBaseWidth = this.colActions.Width > 0 ? this.colActions.Width : 100;
                int padding = 0;

                int fixedWithoutActions = versionWidth + statusWidth + padding;
                int remaining = Math.Max(120, totalWidth - fixedWithoutActions);

                int nameWidth = (int)(remaining * 0.6);
                int typeWidth = remaining - nameWidth;

                if (nameWidth < 160) nameWidth = 160;
                if (typeWidth < 120) typeWidth = 120;

                int usedWithoutActions = nameWidth + typeWidth + versionWidth + statusWidth + padding;
                int actionsWidth = Math.Max(actionsBaseWidth, totalWidth - usedWithoutActions);

                int sum = usedWithoutActions + actionsWidth;
                if (sum > totalWidth)
                {
                    int overflow = sum - totalWidth;
                    int reduceName = Math.Min(overflow, Math.Max(0, nameWidth - 120));
                    nameWidth -= reduceName;
                    overflow -= reduceName;
                    if (overflow > 0)
                    {
                        int reduceType = Math.Min(overflow, Math.Max(0, typeWidth - 100));
                        typeWidth -= reduceType;
                    }
                    usedWithoutActions = nameWidth + typeWidth + versionWidth + statusWidth + padding;
                    actionsWidth = Math.Max(actionsBaseWidth, totalWidth - usedWithoutActions);
                }

                this.colName.Width = nameWidth;
                this.colVersion.Width = versionWidth;
                this.colStatus.Width = statusWidth;
                this.colType.Width = typeWidth;
                this.colActions.Width = actionsWidth;
            }
            catch { }
        }
        private void SetupEventHandlers()
        {
            this.btnManage.Click += BtnManage_Click;
            this.btnRefresh.Click += (s, e) => LoadPlugins();
            this.btnImport.Click += BtnImport_Click;
            this.btnBrowse.Click += BtnBrowse_Click;
            this.btnClose.Click += (s, e) => this.Close();
            _pluginManager.PluginsChanged += PluginManager_PluginsChanged;
        }

        private void BtnManage_Click(object sender, EventArgs e)
        {
            try
            {
                var pluginsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    Directory.CreateDirectory(pluginsDir);
                }
                System.Diagnostics.Process.Start("explorer.exe", pluginsDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开插件文件夹失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://t.me/OrionPlugin",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开插件市场失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Orion 插件包 (*.Orion)|*.Orion";
                    ofd.Title = "导入插件包";
                    ofd.Multiselect = true;

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        await ImportPackages(ofd.FileNames);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入插件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>插件包大小阈值：超过此值（5MB）的导入/卸载才显示进度条；更小则直接操作（通常 1 秒内完成）。</summary>
        private const long PluginSizeThresholdBytes = 5L * 1024 * 1024;

        /// <summary>递归计算目录总字节数（用于判断卸载是否需要进度条）。</summary>
        private static long GetFolderSize(string folder)
        {
            try
            {
                return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                    .Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
            }
            catch { return 0L; }
        }

        /// <summary>
        /// 在后台线程解压导入插件包，并显示“正在解压”进度窗体（不再卡死 UI）。
        /// 导入期间挂起插件目录的 FileSystemWatcher，避免逐文件写入触发反复重载（插件闪烁）。
        /// 导入完成后只做一次干净的重载。
        /// 仅当待导入包总大小 > 5MB 时才显示进度条，小插件直接静默导入。
        /// </summary>
        private async Task ImportPackages(IEnumerable<string> packages)
        {
            var files = packages
                .Where(f => f.EndsWith(".Orion", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (files.Count == 0) return;

            var pluginsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
            if (!Directory.Exists(pluginsDir)) Directory.CreateDirectory(pluginsDir);

            // 5MB 阈值：小插件（<5MB）解压通常 1 秒内完成，不弹进度条；大插件才显示，避免一闪而过的弹窗
            long totalBytes = files.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
            bool showProgress = totalBytes > PluginSizeThresholdBytes;

            // 后台线程与 UI 线程之间唯一的桥梁：只写这两个字段，UI 侧由 Timer 轮询读取。
            var state = new ProgressState();

            // 按用户要求：导入统一用 marquee 滑动进度条，不做实时百分比（更简单可靠）
            using (var progressForm = new ImportProgressForm("正在导入插件包", marquee: true))
            {
                if (showProgress)
                {
                    progressForm.Show(this);
                    // UI 线程 Timer 轮询共享状态：后台线程不再做任何 UI 封送（BeginInvoke/Invoke），
                    // 从根上消除「后台 → UI」这条导致卡死的链路。
                    progressForm.StartPolling(state);
                }

                int imported = 0;
                var errors = new List<string>();
                // 后台线程只写共享状态变量（开销极小），绝不触碰任何 UI 控件。
                Action<int, string> onProgress = (pct, status) =>
                {
                    state.Percent = pct;
                    if (!string.IsNullOrEmpty(status)) state.Status = status;
                };
                onProgress(0, "正在准备…");

                try
                {
                    _pluginManager.SuspendWatcher();
                    await Task.Run(() =>
                    {
                        int total = files.Count;
                        for (int i = 0; i < files.Count; i++)
                        {
                            var file = files[i];
                            try
                            {
                                onProgress((int)(100.0 * i / total), $"正在解压: {Path.GetFileName(file)}");
                                PluginPackageImporter.Import(file, pluginsDir, (filePct, entryName) =>
                                {
                                    int overall = total <= 1 ? filePct : (int)(100.0 * i / total + filePct / (double)total);
                                    if (overall > 100) overall = 100;
                                    onProgress(overall, $"正在解压: {Path.GetFileName(file)}  →  {entryName}");
                                });
                                imported++;
                            }
                            catch (Exception exFile)
                            {
                                errors.Add($"导入 {Path.GetFileName(file)} 失败: {exFile.Message}");
                            }
                        }
                        onProgress(100, "正在加载插件…");
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"导入失败: {ex.Message}");
                }
                finally
                {
                    _pluginManager.ResumeWatcher();
                    LoadPlugins();
                    // 关键：进度条的生命周期只覆盖「解压」这一纯 IO 阶段，解压完立刻关闭，
                    // 绝不等待 ReloadPlugins（它要重新 Assembly.Load 所有插件，可能很慢甚至卡住）。
                    if (showProgress) progressForm.Close();
                }

                // 重载放在进度条关闭之后，后台 fire-and-forget：
                // 即使重载慢或卡住，也不会再把进度条钉在屏幕上。
                if (imported > 0)
                {
                    ReloadPluginsInBackground();
                }

                if (errors.Count > 0)
                    MessageBox.Show(string.Join("\n", errors), "导入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (imported > 0)
                    MessageBox.Show($"成功导入 {imported} 个插件包。", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 在后台线程重载插件（重新加载所有插件程序集，可能耗时），完成后回 UI 线程刷新列表。
        /// 采用 fire-and-forget：调用方不等待它，因此它绝不会阻塞/钉住任何进度条。
        /// </summary>
        private void ReloadPluginsInBackground()
        {
            Task.Run(() =>
            {
                try { _pluginManager.ReloadPlugins(); }
                catch { }
            }).ContinueWith(_ =>
            {
                try
                {
                    if (!IsDisposed && !Disposing && IsHandleCreated)
                    {
                        BeginInvoke(new Action(LoadPlugins));
                    }
                }
                catch { }
            });
        }

        private void PluginManager_PluginsChanged(object sender, EventArgs e)
        {
            // 改 BeginInvoke：订阅 PluginsChanged 时本方法可能在后台线程被调用，
            // 用 Invoke 会阻塞后台线程等 UI；BeginInvoke 不阻塞，避免与 PluginManager 的 _lock 形成死锁。
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LoadPlugins()));
            }
            else
            {
                LoadPlugins();
            }
        }

        private void LoadPlugins()
        {
            try
            {
                this.listView.BeginUpdate();
                this.listView.Items.Clear();
                _pluginInfos.Clear();

                var pluginsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");

                if (!Directory.Exists(pluginsDir))
                {
                    return;
                }

                var subdirectories = Directory.GetDirectories(pluginsDir);

                foreach (var subDir in subdirectories)
                {
                    var manifestPath = Path.Combine(subDir, "plugin.json");
                    if (!File.Exists(manifestPath)) continue;

                    try
                    {
                        var json = File.ReadAllText(manifestPath);
                        var manifest = Newtonsoft.Json.Linq.JObject.Parse(json);

                        var name = manifest["name"]?.ToString() ?? Path.GetFileName(subDir);
                        var version = manifest["version"]?.ToString() ?? "1.0.0.0";
                        var description = manifest["description"]?.ToString() ?? "";
                        var serverDlls = manifest["server"]?.ToObject<string[]>() ?? Array.Empty<string>();
                        var clientDlls = manifest["client"]?.ToObject<string[]>() ?? Array.Empty<string>();

                        if (serverDlls.Length == 0 && clientDlls.Length == 0) continue;

                        string type = "未知";
                        if (serverDlls.Length > 0 && clientDlls.Length > 0) type = "服务端 & 客户端";
                        else if (serverDlls.Length > 0) type = "服务端";
                        else if (clientDlls.Length > 0) type = "客户端";

                        var firstDll = serverDlls.Length > 0 ? serverDlls[0] : clientDlls[0];
                        var dllPath = Path.Combine(subDir, firstDll);
                        var disabledPath = dllPath + ".disabled";

                        // 禁用逻辑已移除：任何遗留的 .disabled 一律恢复为已加载（一次性迁移）。
                        if (!File.Exists(dllPath) && File.Exists(disabledPath))
                        {
                            try { File.Move(disabledPath, dllPath); } catch { }
                        }

                        // 插件只有两种状态：在目录里（已加载）或不在（已删除）。
                        if (!File.Exists(dllPath)) continue;

                        var pluginInfo = new PluginInfo
                        {
                            Name = name,
                            FilePath = dllPath,
                            Enabled = true,
                            Version = version,
                            Description = description,
                            Type = type
                        };
                        _pluginInfos.Add(pluginInfo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing plugin.json in {subDir}: {ex.Message}");
                    }
                }

                var sortedPlugins = _pluginInfos
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var plugin in sortedPlugins)
                {
                    var item = new ListViewItem(plugin.Name)
                    {
                        Tag = plugin
                    };
                    item.SubItems.Add(plugin.Version);
                    item.SubItems.Add("已加载");
                    item.SubItems.Add(plugin.Type);
                    item.SubItems.Add("⚙");
                    item.ForeColor = Color.White;
                    this.listView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Plugin manager load error: {ex.Message}");
            }
            finally
            {
                this.listView.EndUpdate();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _pluginManager.PluginsChanged -= PluginManager_PluginsChanged;
            base.OnFormClosing(e);
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _lastSortColumn)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortAscending = true;
            }

            _lastSortColumn = e.Column;
            SortListView(e.Column);
        }

        private void SortListView(int columnIndex)
        {
            this.listView.BeginUpdate();

            var items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView.Items)
            {
                items.Add(item);
            }

            items.Sort((x, y) =>
            {
                string xText = x.SubItems[columnIndex].Text;
                string yText = y.SubItems[columnIndex].Text;

                int result;

                switch (columnIndex)
                {
                    case 0:
                    case 1:
                    case 3:
                        result = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
                        break;
                    case 2:
                        result = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
                        break;
                    default:
                        result = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                return _sortAscending ? result : -result;
            });

            this.listView.Items.Clear();
            foreach (var item in items)
            {
                this.listView.Items.Add(item);
            }

            this.listView.EndUpdate();
            this.listView.Invalidate();
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            var hitTest = this.listView.HitTest(e.Location);
            if (hitTest.Item != null && hitTest.SubItem != null)
            {
                if (hitTest.SubItem == hitTest.Item.SubItems[4])
                {
                    if (hitTest.Item.Tag is PluginInfo pluginInfo)
                    {
                        ShowPluginDetails(pluginInfo);
                    }
                }
            }
        }

        private void ShowPluginDetails(PluginInfo pluginInfo)
        {
            var detailsForm = new Form
            {
                Text = $"插件详情 - {pluginInfo.Name}",
                ClientSize = new Size(480, 340),
                MinimumSize = new Size(460, 300),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this,
                Icon = this.Icon,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            DarkModeManager.ApplyDarkMode(detailsForm);

            var contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 28, 16),
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
            var labelFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
            var valueFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);

            contentPanel.Controls.Add(CreateFieldLabel("插件名称", labelFont), 0, 0);
            contentPanel.Controls.Add(CreateFieldValue(pluginInfo.Name, titleFont), 1, 0);
            contentPanel.Controls.Add(CreateFieldLabel("版本", labelFont), 0, 1);
            contentPanel.Controls.Add(CreateFieldValue(pluginInfo.Version, valueFont), 1, 1);
            contentPanel.Controls.Add(CreateFieldLabel("类型", labelFont), 0, 2);
            contentPanel.Controls.Add(CreateFieldValue(pluginInfo.Type, valueFont), 1, 2);
            contentPanel.Controls.Add(CreateFieldLabel("状态", labelFont), 0, 3);
            contentPanel.Controls.Add(CreateFieldValue(
                "已加载",
                valueFont,
                Color.LightGreen), 1, 3);
            contentPanel.Controls.Add(CreateFieldLabel("描述", labelFont, ContentAlignment.TopLeft), 0, 4);

            var descriptionBox = new TextBox
            {
                Text = pluginInfo.Description,
                Font = valueFont,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 37, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                TabStop = false
            };
            contentPanel.Controls.Add(descriptionBox, 1, 4);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(20, 8, 20, 12),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            var closeButton = CreateDialogButton("关闭");
            closeButton.Margin = new Padding(6, 0, 6, 0);
            closeButton.Click += (s, ev) => detailsForm.Close();

            var uninstallButton = CreateDialogButton("卸载");
            uninstallButton.Margin = new Padding(6, 0, 6, 0);
            uninstallButton.Click += (s, ev) => UninstallPlugin(pluginInfo, () => detailsForm.Close());

            buttonPanel.Controls.Add(closeButton);
            buttonPanel.Controls.Add(uninstallButton);

            detailsForm.Controls.Add(contentPanel);
            detailsForm.Controls.Add(buttonPanel);
            detailsForm.AcceptButton = closeButton;

            detailsForm.ShowDialog();
        }

        /// <summary>
        /// 卸载插件：先热重载释放资源，再删除整个插件目录；删除失败时标记为待删除。
        /// 仅当插件文件夹 > 5MB 时显示 marquee 进度条（删除大文件夹可能耗时），小插件直接静默删除。
        /// 删除/标记完成后调用 onDone（如关闭详情窗体）。
        /// </summary>
        private async void UninstallPlugin(PluginInfo pluginInfo, Action onDone)
        {
            var confirm = MessageBox.Show(
                $"确定要移除 {pluginInfo.Name} 吗? 此操作将删除整个插件文件夹。",
                "确认卸载",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var pluginFolder = Path.GetDirectoryName(pluginInfo.FilePath);

                // 5MB 阈值：大插件（文件夹 >5MB）删除可能耗时，显示进度条；小插件直接删，通常 1 秒内完成。
                // 必须在后台线程统计体积：大插件目录递归枚举文件会冻结 UI 线程，
                // 那样 marquee 进度条（依赖 UI 消息泵）会直接定住不动。
                long folderSize = await Task.Run(() => GetFolderSize(pluginFolder));
                bool showProgress = folderSize > PluginSizeThresholdBytes;

                Exception deleteError = null;
                bool renamed = false;
                string pendingPath = null;

                var state = new ProgressState { Status = $"正在卸载: {pluginInfo.Name}" };

                using (var progressForm = new ImportProgressForm("正在卸载插件…", marquee: true))
                {
                    if (showProgress)
                    {
                        progressForm.Show(this);
                        // 同样改用 UI 线程 Timer 轮询，后台线程不做任何 UI 封送。
                        progressForm.StartPolling(state);
                    }

                    // 卸载期间挂起 watcher：Directory.Delete 会触发大量文件变更事件，
                    // 若不挂起会并发触发多个后台 ReloadPlugins，极易把 UI 线程锁死（卡死）。
                    _pluginManager.SuspendWatcher();
                    try
                    {
                        // 删除逻辑放到后台线程，避免大文件夹删除冻结 UI；marquee 进度条表示进行中。
                        // 注意：这里不再先做 ReloadPlugins —— 进度条只覆盖「删除」这一纯 IO 阶段，
                        // 重载（要重新加载所有插件程序集，可能很慢）改到进度条关闭之后再异步做。
                        await Task.Run(async () =>
                        {
                            // 后台线程只写共享状态字段（由 UI 侧 Timer 轮询读取），绝不做任何 UI 封送。
                            state.Status = $"正在删除文件: {pluginInfo.Name}";
                            bool success = false;
                            for (int i = 0; i < 3; i++)
                            {
                                try
                                {
                                    if (Directory.Exists(pluginFolder))
                                    {
                                        Directory.Delete(pluginFolder, true);
                                    }
                                    success = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    deleteError = ex;
                                    if (i < 2) state.Status = $"文件被占用，正在重试 ({i + 1}/3)…";
                                    await Task.Delay(500);
                                }
                            }

                            if (!success)
                            {
                                // 如果最终还是失败，尝试重命名以使其失效
                                state.Status = "正在标记为待删除…";
                                try
                                {
                                    pendingPath = pluginFolder + ".pending_delete";
                                    if (Directory.Exists(pendingPath)) Directory.Delete(pendingPath, true);
                                    Directory.Move(pluginFolder, pendingPath);
                                    renamed = true;
                                }
                                catch (Exception ex)
                                {
                                    deleteError = ex;
                                }
                            }
                            else
                            {
                                state.Status = "正在刷新插件列表…";
                            }
                        });
                    }
                    finally
                    {
                        _pluginManager.ResumeWatcher();
                    }

                    if (showProgress) progressForm.Close();
                }

                if (renamed && pendingPath != null)
                {
                    MessageBox.Show($"文件夹被占用，已标记为待删除。请在重启 Orion 后手动清理 {pendingPath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (deleteError != null && !renamed)
                {
                    throw deleteError;
                }

                LoadPlugins();
                // 重载放到进度条关闭之后、后台 fire-and-forget：释放旧插件实例并重建菜单。
                ReloadPluginsInBackground();
                onDone?.Invoke();
            }
            catch (Exception exDel)
            {
                MessageBox.Show($"卸载失败: {exDel.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label CreateFieldLabel(string text, Font font, ContentAlignment alignment = ContentAlignment.MiddleLeft)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = Color.FromArgb(180, 180, 180),
                Dock = DockStyle.Fill,
                TextAlign = alignment,
                AutoEllipsis = true
            };
        }

        private static Label CreateFieldValue(string text, Font font, Color? foreColor = null)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = foreColor ?? Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
        }

        private static Button CreateDialogButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(90, 24),
                Padding = new Padding(12, 3, 12, 3),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            return button;
        }

        private void listView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Any(f => f.EndsWith(".Orion", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private async void listView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                var files = ((string[])e.Data.GetData(DataFormats.FileDrop)) ?? new string[0];
                var packages = files.Where(f => f.EndsWith(".Orion", StringComparison.OrdinalIgnoreCase)).ToList();
                if (packages.Count == 0) return;

                await ImportPackages(packages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装插件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView.SelectedItems.Count == 1)
            {
                var item = this.listView.SelectedItems[0];
                if (item?.Tag is PluginInfo pi)
                {
                    ShowPluginDetails(pi);
                }
            }
        }

        private void listView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            bool isSorted = e.ColumnIndex == _lastSortColumn;

            Color backgroundColor = isSorted ? Color.FromArgb(70, 130, 180) : Color.FromArgb(60, 60, 63);
            Color textColor = isSorted ? Color.White : Color.White;

            Rectangle headerFill = e.Bounds;
            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                headerFill = new Rectangle(e.Bounds.X, e.Bounds.Y, this.listView.ClientSize.Width - e.Bounds.X, e.Bounds.Height);
            }
            using (SolidBrush brush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(brush, headerFill);
            }

            var headerFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                               (e.Header == this.colActions ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left);
            Rectangle headerBounds = e.Bounds;
            if (e.Header == this.colStatus)
            {
                headerBounds = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 30), e.Bounds.Height);
            }
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, headerBounds, textColor, headerFlags);

            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                int right = this.listView.ClientSize.Width;
                int fillWidth = Math.Max(0, right - e.Bounds.Right);
                if (fillWidth > 0)
                {
                    using (SolidBrush brush = new SolidBrush(backgroundColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Right, e.Bounds.Top, fillWidth, e.Bounds.Height));
                    }
                }
            }
        }

        private void listView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // 第一列（插件名称）自绘：不再有复选框，禁用逻辑已移除。
            if (e.ColumnIndex == 0)
            {
                Color bg = e.Item.Selected
                    ? Color.FromArgb(60, 90, 120)
                    : (e.Item.BackColor.IsEmpty ? this.listView.BackColor : e.Item.BackColor);
                using (SolidBrush brush = new SolidBrush(bg))
                    e.Graphics.FillRectangle(brush, e.Bounds);

                Color nameColor = e.Item.ForeColor.IsEmpty ? Color.White : e.Item.ForeColor;
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, e.Bounds, nameColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                return;
            }

            Color textColor = e.Item.ForeColor;

            Rectangle rowFill = e.Bounds;
            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                rowFill = new Rectangle(e.Bounds.X, e.Bounds.Y, this.listView.ClientSize.Width - e.Bounds.X, e.Bounds.Height);
            }
            using (SolidBrush brush = new SolidBrush(e.Item.BackColor))
            {
                e.Graphics.FillRectangle(brush, rowFill);
            }

            var cellFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                            (e.ColumnIndex == 4 ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left);
            Rectangle cellBounds = e.Bounds;
            if (e.ColumnIndex == 2)
            {
                cellBounds = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 30), e.Bounds.Height);
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, cellBounds, textColor, cellFlags);

            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                int right = this.listView.ClientSize.Width;
                int fillWidth = Math.Max(0, right - e.Bounds.Right);
                if (fillWidth > 0)
                {
                    using (SolidBrush brush = new SolidBrush(e.Item.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Right, e.Bounds.Top, fillWidth, e.Bounds.Height));
                    }
                }
            }
        }

        private class PluginInfo
        {
            public string Name { get; set; }
            public string FilePath { get; set; }
            public bool Enabled { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 后台线程与 UI 线程之间共享的进度状态。
        /// 后台线程只写、UI 线程（Timer）只读，双方都不做控件访问，彻底避免跨线程封送。
        /// </summary>
        private sealed class ProgressState
        {
            public volatile int Percent;
            public volatile string Status = "正在准备…";
        }

        /// <summary>
        /// 导入插件包时显示的进度窗体（代码内联构建，无需 Designer 文件）。
        /// 不接受用户关闭，导入完成由 ImportPackages 主动 Close。
        /// 进度由 UI 线程 Timer 轮询 ProgressState 更新，后台线程不调用本窗体的任何方法。
        /// </summary>
        private sealed class ImportProgressForm : Form
        {
            private readonly Label _lblStatus;
            private readonly ProgressBar _progressBar;
            private readonly bool _marquee;
            private System.Windows.Forms.Timer _pollTimer;

            public ImportProgressForm(string title = "正在导入插件包", bool marquee = false)
            {
                _marquee = marquee;
                Text = title;
                ClientSize = new Size(440, 116);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ControlBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                BackColor = Color.FromArgb(45, 45, 48);
                ForeColor = Color.White;

                _lblStatus = new Label
                {
                    Left = 18,
                    Top = 18,
                    Width = 404,
                    Height = 40,
                    Text = "正在准备…",
                    ForeColor = Color.White,
                    UseCompatibleTextRendering = false
                };

                _progressBar = new ProgressBar
                {
                    Left = 18,
                    Top = 68,
                    Width = 404,
                    Height = 22,
                    Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous,
                    MarqueeAnimationSpeed = 30,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                Controls.Add(_lblStatus);
                Controls.Add(_progressBar);
            }

            /// <summary>
            /// 启动 UI 线程轮询：每 100ms 从共享状态读取并刷新界面。
            /// 取代原先后台线程 BeginInvoke/Invoke 到本窗体的做法（那是卡死根源）。
            /// </summary>
            public void StartPolling(ProgressState state)
            {
                try
                {
                    _pollTimer = new System.Windows.Forms.Timer { Interval = 100 };
                    _pollTimer.Tick += (s, e) =>
                    {
                        try
                        {
                            if (IsDisposed || Disposing) return;
                            if (!_marquee)
                            {
                                _progressBar.Value = Math.Max(0, Math.Min(100, state.Percent));
                            }
                            var text = state.Status;
                            if (!string.IsNullOrEmpty(text)) _lblStatus.Text = text;
                        }
                        catch { }
                    };
                    _pollTimer.Start();
                }
                catch { }
            }

            /// <summary>仅更新文字（marquee 模式下不碰进度条数值）。UI 线程调用。</summary>
            public void SetStatus(string status)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<string>(SetStatus), status);
                    return;
                }
                if (!string.IsNullOrEmpty(status)) _lblStatus.Text = status;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        if (_pollTimer != null)
                        {
                            _pollTimer.Stop();
                            _pollTimer.Dispose();
                            _pollTimer = null;
                        }
                    }
                    catch { }
                }
                base.Dispose(disposing);
            }
        }
    }
}
