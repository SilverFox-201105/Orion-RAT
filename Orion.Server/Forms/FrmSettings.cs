using Orion.Server.Forms.DarkMode;
using Orion.Server.Models;
using Orion.Server.Networking;
using Orion.Server.Utilities;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using Orion.Server.TelegramSender;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace Orion.Server.Forms
{
    public partial class FrmSettings : Form
    {
        private readonly OrionServer _listenServer;
        private readonly ToolTip _tip;

        public FrmSettings(OrionServer listenServer)
        {
            this._listenServer = listenServer;

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);

            ToggleListenerSettings(!listenServer.Listening);
            UpdateModeStates();

            // 「启动时监听新连接」现在也是 FRP 开机自连的开关，说明一下避免误解
            _tip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 300, ShowAlways = true };
            _tip.SetToolTip(chkAutoListen,
                "启动时自动开始监听本地端口。\n若同时启用了「启用远程FRP隧道」，启动时也会自动连接服务器；\n未勾选时只保留勾选状态，需要手动点下方的「连接」。");

            // 关闭窗体时退订 FRP 事件，避免已释放的控件被回调引用造成泄漏/异常
            this.FormClosing += (s, e) =>
            {
                var m = Frp.FrpTunnelManager.Instance;
                m.StatusChanged -= FrpStatus_Changed;
                m.LogReceived -= FrpLog_Received;
                _tip?.Dispose();
                // 勾选框现在是纯开关：关闭窗体时不再按「已保存配置」去补启/补停任何东西。
                // 唯一兜底：本次会话取消了勾选、可隧道还在跑（罕见竞态）→ 停掉，避免后台空跑。
                if (!_initializing && !chkFrpEnabled.Checked && Frp.FrpTunnelManager.Instance.IsRunning)
                {
                    m.Stop();
                    Networking.ListenerCoordinator.Stop(_listenServer);
                }
            };
        }

        /// <summary>
        /// 钉死两个多行控件的高度。
        ///
        /// 本窗体使用 AutoScaleMode.Font（AutoScaleDimensions 6F,13F），运行时实际字体比设计
        /// 基准高约 1.45 倍：控件的 Y 坐标按该比例缩放一次，但多行 TextBox / RichTextBox 的高度
        /// 在缩放时还会按字体行高再算一次（实测渲染高度 ≈ 设计值的 1.37 倍）。结果就是粘贴框
        /// 越长越往下吃，把「状态」标签和「连接」按钮整个盖住——而这两个控件在 z-order 上位于
        /// 框的前面，所以表现为“按钮浮在框上、框遮住一片”。
        ///
        /// 解决：Designer 坐标只决定谁在谁上面，高度改在这里于窗体缩放完成之后（Load，此时
        /// 缩放已结束）按【下方控件的实际位置】反推可用空间来设定，任何 DPI / 字体下都不会越界。
        /// </summary>
        private void PinMultilineHeights()
        {
            // 粘贴框：下边界不得越过「状态」标签
            int boxTop = txtFrpConfig.Top;
            int boxAvail = lblFrpStatus.Top - boxTop - 12;
            int boxWanted = (int)(grpFrp.Height * 0.36);
            txtFrpConfig.Height = Math.Max(80, Math.Min(boxAvail, boxWanted));

            // 控制台：下边界不得越过分组框底部
            int rtbTop = rtbFrpcConsole.Top;
            int rtbAvail = grpFrp.Height - rtbTop - 40;
            int rtbWanted = (int)(grpFrp.Height * 0.14);
            rtbFrpcConsole.Height = Math.Max(50, Math.Min(rtbAvail, rtbWanted));
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            PinMultilineHeights();

            // 进入加载态：后续对 chkFrpEnabled.Checked 等控件的编程赋值不会触发 (dis)connect 副作用
            _initializing = true;
            try
            {
            ncPort.Value = Settings.ListenPort;
            var allPorts = new List<ushort> { Settings.ListenPort };
            if (Settings.ListenPorts != null)
                allPorts.AddRange(Settings.ListenPorts);
            allPorts = allPorts.Distinct().ToList();
            if (allPorts.Count > 0)
            {
                txtMultiPorts.Text = string.Join(", ", allPorts);
                txtMultiPorts.ForeColor = System.Drawing.Color.Black;
            }
            else
            {
                txtMultiPorts.Text = "端口1 端口2 等..";
                txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            }
            chkIPv6Support.Checked = Settings.IPv6Support;
            chkAutoListen.Checked = Settings.AutoListen;
            chkPopup.Checked = Settings.ShowPopup;
            chkEventLog.Checked = Settings.EventLog;
            chkShowCountryGroups.Checked = Settings.ShowCountryGroups;
            chkShowTagGroups.Checked = Settings.ShowTagGroups;
            UpdateGroupingCheckBoxStates();

            // FRP / C2 隧道
            chkFrpEnabled.Checked = Settings.FrpEnabled;
            // 直接粘贴由服务商提供的原始 frpc 配置文件，Orion 原样写盘并启动，
            // 避免与第三方托管 frp(如 ChmlFrp)的云端配置校验冲突。
            txtFrpConfig.Text = Settings.FrpRawConfig;
            btnFrpToggle.Enabled = chkFrpEnabled.Checked;
            if (Frp.FrpTunnelManager.Instance.IsRunning)
                _frpStatusText = "已连接 (C2 隧道在线)";
            else if (chkFrpEnabled.Checked)
                _frpStatusText = "未连接（点下方「连接」启动隧道）";
            else
                _frpStatusText = "未连接（未启用）";
            UpdateFrpStatusLabel();
            // 配置格式下拉：决定写盘扩展名(.toml/.ini)——frpc 按扩展名选解析器
            string fmt = (Settings.FrpConfigFormat ?? "Auto").ToLowerInvariant();
            cboFrpFormat.SelectedIndex = fmt == "ini" ? 2 : (fmt == "toml" ? 1 : 0);
            RefreshFrpcBinaryLabel();
            // 订阅 FRP 控制台输出与状态变更（先 -= 去重，避免重复打开设置页时叠加多个处理器）
            var frpMgr = Frp.FrpTunnelManager.Instance;
            frpMgr.StatusChanged -= FrpStatus_Changed;
            frpMgr.StatusChanged += FrpStatus_Changed;
            frpMgr.LogReceived -= FrpLog_Received;
            frpMgr.LogReceived += FrpLog_Received;
            // 回放已累积的全部 frpc 终端历史（关闭设置页再打开也能看到完整输出，不再丢）
            var hist = frpMgr.GetHistory();
            if (hist.Count == 0)
                AppendFrpConsole("（暂无输出，连接后这里会显示 frpc 终端日志）");
            else
                foreach (var line in hist)
                    AppendFrpConsole(line);
            _frpRenderedCount = hist.Count;
            // FRP 与手动本地监听互斥：根据当前状态置灰对应控件
            UpdateModeStates();
            txtTelegramChatID.Text = Settings.TelegramChatID;
            txtTelegramToken.Text = Settings.TelegramBotToken;
            chkTelegramNotis.Checked = Settings.TelegramNotifications;

            string OrionPath = Path.Combine(Application.StartupPath, "OrionStuff");
            string filePath = Path.Combine(OrionPath, "blocked.json");

            try
            {
                if (!(Directory.Exists(OrionPath) && File.Exists(filePath)))
                {
                    Directory.CreateDirectory(OrionPath);
                    File.WriteAllText(filePath, "[]");
                }
                string json = File.ReadAllText(filePath);
                var blockedIPs = JsonConvert.DeserializeObject<List<string>>(json);
                if (blockedIPs != null && blockedIPs.Count > 0)
                {
                    BlockedRichTB.Text = string.Join(Environment.NewLine, blockedIPs);
                }
                else
                {
                    BlockedRichTB.Text = string.Empty;
                }
            }
            catch (Exception)
            {

            }
            } // 关闭 _initializing 包裹的 try
            finally
            {
                // 离开加载态：此后用户对 chkFrpEnabled 的真实点击才会触发 (dis)connect
                _initializing = false;
            }
        }

        private void txtMultiPorts_Enter(object sender, EventArgs e)
        {
            if (txtMultiPorts.Text == "端口1 端口2 等..")
            {
                txtMultiPorts.Text = "";
                txtMultiPorts.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void txtMultiPorts_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMultiPorts.Text))
            {
                txtMultiPorts.Text = "端口1 端口2 等..";
                txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private ushort GetPortSafe()
        {
            var portValue = ncPort.Value.ToString(CultureInfo.InvariantCulture);
            ushort port;
            return (!ushort.TryParse(portValue, out port)) ? (ushort)0 : port;
        }

        private static IEnumerable<ushort> ParsePorts(string input)
        {
            var list = new List<ushort>();
            if (string.IsNullOrWhiteSpace(input) || input == "端口1 端口2 等..") return list;

            foreach (var token in input.Split(new[] { ',', ';', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = token.Trim();
                if (part.Contains("-"))
                {
                    var bounds = part.Split('-');
                    if (bounds.Length == 2 && ushort.TryParse(bounds[0], out var a) && ushort.TryParse(bounds[1], out var b))
                    {
                        if (a > b) { var t = a; a = b; b = t; }
                        for (var p = a; p <= b; p++) list.Add((ushort)p);
                    }
                }
                else if (ushort.TryParse(part, out var single))
                {
                    if (single >= 1 && single <= 65535)
                    {
                        list.Add(single);
                    }
                }
            }
            return list.Distinct();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var port = GetPortSafe();
            if (port == 0) return;

            var existing = txtMultiPorts.Text;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(existing))
                parts.AddRange(existing.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

            if (!parts.Contains(port.ToString()))
            {
                parts.Add(port.ToString());
                txtMultiPorts.Text = string.Join(",", parts);
            }
        }

        private void txtMultiPorts_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == ',' || e.KeyChar == '-' || e.KeyChar == ';' || e.KeyChar == ' ') return;
            e.Handled = true;
        }

        private void txtMultiPorts_TextChanged(object sender, EventArgs e)
        {
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            var allPorts = ParsePorts(txtMultiPorts.Text).Distinct().ToList();
            if (allPorts.Count == 0)
            {
                MessageBox.Show("请在\"监听端口\"中输入至少一个端口。", "未提供端口", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (btnListen.Text == "开始监听" && !_listenServer.Listening)
            {
                // 本地监听统一交给 ListenerCoordinator：端口冲突等失败会直接提示用户，不再静默
                if (Networking.ListenerCoordinator.Ensure(_listenServer, allPorts, chkIPv6Support.Checked, this))
                {
                    ToggleListenerSettings(false);
                    UpdateModeStates();
                }
            }
            else if (btnListen.Text == "停止监听" && _listenServer.Listening)
            {
                Networking.ListenerCoordinator.Stop(_listenServer);
                ToggleListenerSettings(true);
                UpdateModeStates();
                FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
                if (mainForm != null)
                {
                    mainForm.EventLog("服务器已停止监听连接。", "info");
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var ports = ParsePorts(txtMultiPorts.Text).Distinct().ToArray();

            bool frpMode = chkFrpEnabled.Checked;
            var frpRaw = txtFrpConfig.Text;
            int frpLocalPort = frpMode ? ParseFrpLocalPort(frpRaw) : 0;

            if (chkFrpEnabled.Checked)
            {
                // FRP 模式：连接目标全部来自粘贴的 frpc 配置文件，必须已粘贴且含本地转发端口。
                if (string.IsNullOrWhiteSpace(txtFrpConfig.Text))
                {
                    MessageBox.Show("FRP 模式下必须先粘贴 frpc 配置文件内容。", "FRP 配置不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (frpLocalPort < 1)
                {
                    MessageBox.Show("配置里缺少有效的 local_port（本地转发端口），Orion 需要通过它接收隧道流量。", "FRP 本地端口无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // FRP 模式：Orion 监听端口以配置里的 local_port 为准，不再读取左侧端口列表
                Settings.ListenPort = (ushort)frpLocalPort;
                Settings.ListenPorts = new ushort[0];
            }
            else
            {
                if (ports.Length == 0)
                {
                    MessageBox.Show("请在\"监听端口\"中输入至少一个端口。", "未提供端口", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Settings.ListenPort = ports[0];
                Settings.ListenPorts = ports.Skip(1).ToArray();
            }
            
            txtMultiPorts.Text = string.Join(", ", ports);

            // 一次性原子落盘：整个表单只写一次 settings.json。
            // 此前每个 Settings.X = value 都会立刻写盘，中途任何异常/半截状态都会把
            // 磁盘上的配置写坏（历史 bug：token 被清空、端口变成 1）。
            Settings.Update(m =>
            {
                // FRP 模式下本地转发端口以配置里的 local_port 为准，且不再额外监听其它端口
                m.ListenPort = frpMode ? (ushort)(frpLocalPort > 0 ? frpLocalPort : ports[0]) : ports[0];
                m.ListenPorts = frpMode ? new ushort[0] : ports.Skip(1).ToArray();
                m.IPv6Support = chkIPv6Support.Checked;
                m.AutoListen = chkAutoListen.Checked;
                m.ShowPopup = chkPopup.Checked;
                m.EventLog = chkEventLog.Checked;
                m.ShowCountryGroups = chkShowCountryGroups.Checked;
                m.ShowTagGroups = chkShowTagGroups.Checked;
                m.FrpEnabled = frpMode;
                m.FrpRawConfig = frpRaw;
                m.FrpConfigFormat = cboFrpFormat.SelectedIndex == 2 ? "Ini"
                                    : (cboFrpFormat.SelectedIndex == 1 ? "Toml" : "Auto");
                m.TelegramChatID = txtTelegramChatID.Text;
                m.TelegramBotToken = txtTelegramToken.Text;
                m.TelegramNotifications = chkTelegramNotis.Checked;
            });

            FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
            if (mainForm != null)
            {
                mainForm.EventLogVisability();
                mainForm.RefreshClientGroups();
                mainForm.RefreshClientTheme();
                mainForm.RefreshFrpTunnel();
            }

            // Save the blocked-IP list to the SAME file that FrmMain.IsClientBlocked and
            // FrmSettings_Load read from (<StartupPath>\OrionStuff\blocked.json). It used to be
            // written to a CWD-relative "blocked.json", so edits (e.g. removing a banned IP)
            // never reached the real blacklist and the IP kept being blocked after a restart.
            string orionPath = Path.Combine(Application.StartupPath, "OrionStuff");
            string filePath = Path.Combine(orionPath, "blocked.json");

            // RichTextBox.Text does not guarantee "\r\n" as the line separator, so split on
            // every known separator, trim entries and drop empties/duplicates.
            var blockedIPs = BlockedRichTB.Text
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim())
                .Where(ip => ip.Length > 0)
                .Distinct()
                .ToList();

            try
            {
                if (!Directory.Exists(orionPath))
                    Directory.CreateDirectory(orionPath);
                string json = JsonConvert.SerializeObject(blockedIPs, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("放弃更改?", "取消", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
                this.Close();
        }

        private void ToggleListenerSettings(bool enabled)
        {
            btnListen.Text = enabled ? "开始监听" : "停止监听";
            ncPort.Enabled = false;
            chkIPv6Support.Enabled = enabled;
            txtMultiPorts.Enabled = enabled;
        }

        private void TelegramControlHandler(bool enable)
        {
            txtTelegramToken.Enabled = enable;
            txtTelegramChatID.Enabled = enable;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkTelegramNotis_CheckedChanged(object sender, EventArgs e)
        {
            TelegramControlHandler(chkTelegramNotis.Checked);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTelegramToken.Text))
                {
                    MessageBox.Show("错误: 请确保您已与 Bot 开始对话");
                    return;
                }

                string[] tokenParts = txtTelegramToken.Text.Split(':');
                if (tokenParts.Length != 2 ||
                    !tokenParts[0].All(char.IsDigit) ||
                    tokenParts[1].Length != 35 ||
                    !tokenParts[1].All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                {
                    MessageBox.Show("错误: 请确保您已与 Bot 开始对话");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtTelegramChatID.Text))
                {
                    MessageBox.Show("错误: 请确保您已与 Bot 开始对话");
                    return;
                }

                if (!long.TryParse(txtTelegramChatID.Text, out long chatId))
                {
                    MessageBox.Show("错误: 请确保您已与 Bot 开始对话");
                    return;
                }

                string response = await Orion.Server.TelegramSender.Send.SendConnectionMessage(
                    txtTelegramToken.Text,
                    txtTelegramChatID.Text,
                    "TestClient123",
                    "192.168.1.100",
                    "TestLand"
                );
                MessageBox.Show("检查完成，运行正常");
            }
            catch (Exception)
            {
                MessageBox.Show("错误: 请确保您已与 Bot 开始对话");
            }
        }
        private void txtNoIPHost_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkShowCountryGroups_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowCountryGroups.Checked)
            {
                chkShowTagGroups.Checked = false;
            }

            UpdateGroupingCheckBoxStates();
        }

        private void chkShowTagGroups_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowTagGroups.Checked)
            {
                chkShowCountryGroups.Checked = false;
            }

            UpdateGroupingCheckBoxStates();
        }

        /// <summary>
        /// Keeps the two grouping options mutually exclusive: enabling one greys out (disables) the
        /// other so it cannot be turned on until the active one is switched off. With neither checked
        /// both are enabled and the client list is shown flat (ungrouped).
        /// </summary>
        private void UpdateGroupingCheckBoxStates()
        {
            bool country = chkShowCountryGroups.Checked;
            bool tag = chkShowTagGroups.Checked;

            if (country && tag)
            {
                chkShowTagGroups.Checked = false;
                tag = false;
            }

            chkShowCountryGroups.Enabled = !tag;
            chkShowTagGroups.Enabled = !country;
        }

        #region "FRP / C2 隧道"

        private string _frpStatusText = "未连接";

        // 加载期间置 true，拦截 chkFrpEnabled.Checked 在 Load 中编程赋值触发的 CheckedChanged
        // 副作用（否则会拿尚未填充的空文本框去写回设置并重启隧道，把已保存的 token 清空、
        // 并把已经在跑的隧道 kill 掉）。用户真实点击时此标志为 false，副作用正常执行。
        private bool _initializing = false;

        // 记录已渲染到控制台的 frpc 历史行数，用于回放时只追加新增行、避免重复
        private int _frpRenderedCount = 0;
        private readonly object _frpFlushLock = new object();

        /// <summary>从一份 frpc 配置原文里解析本地转发端口(local_port/localPort)。解析失败返回 0。</summary>
        private int ParseFrpLocalPort(string cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg)) return 0;
            var m = Regex.Match(cfg, @"local_port\s*=\s*(\d+)|localPort\s*=\s*(\d+)");
            if (m.Success)
                return int.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
            return 0;
        }

        /// <summary>
        /// 建立 FRP 隧道：先用配置里的 local_port 在本地起监听（隧道终点），再原样写盘并拉起 frpc。
        /// 全程【不写 Settings.FrpRawConfig】（那一步由保存负责），只返回成功/失败 —— 失败时保持
        /// 勾选状态不变，由调用方决定如何提示。
        /// </summary>
        private bool StartFrpFromUi()
        {
            string raw = txtFrpConfig.Text;
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show(this, "请先在上方粘贴 frpc 配置文件内容（从服务商复制的原始配置）。",
                    "FRP 配置为空", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int localPort = ParseFrpLocalPort(raw);
            if (localPort < 1)
            {
                MessageBox.Show(this, "配置里缺少或无效的 local_port（本地转发端口）。Orion 需要通过它接收隧道流量。",
                    "FRP 本地端口无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 本地监听是隧道的转发终点：必须先确保它在这个端口上活着，且失败要可见。
            // Ensure 已在该端口监听时会直接复用，不会无谓断开在线客户端。
            if (_listenServer != null &&
                !Networking.ListenerCoordinator.Ensure(_listenServer,
                    new[] { (ushort)localPort }, chkIPv6Support.Checked, this))
            {
                return false;
            }

            // 让 Orion 监听端口与隧道终点保持一致（连接目标/客户端构建器依赖它）
            Settings.ListenPort = (ushort)localPort;
            Frp.FrpTunnelManager.Instance.StartFromRaw(raw);
            _frpStatusText = "正在连接 FRPS...";
            return true;
        }

        /// <summary>退出 FRP：停隧道 + 清空控制台（本地监听的关闭由调用方统一负责）。</summary>
        private void StopFrp()
        {
            Frp.FrpTunnelManager.Instance.Stop();
            ClearFrpConsole();
            _frpStatusText = "未连接";
        }

        /// <summary>
        /// 根据「是否已启用远程FRP隧道」与「隧道是否正在运行」刷新左右两侧控件的可用性：
        ///  - 左侧本地监听控件：隧道运行中才由它接管本地监听，此时整块置灰；断开后立即恢复；
        ///  - 右侧 FRPC 配置栏：只要隧道没在跑就可改（与是否勾选无关）；运行中要改配置必须先点「断开」，
        ///    改完再点「连接」才会生效（FRPC 只在建链时读取配置）。
        /// 左侧其余基础配置（隐藏窗口/自动监听/弹窗/日志/分组/Telegram/屏蔽 IP）不受 FRP 影响。
        /// </summary>
        private void UpdateModeStates()
        {
            bool remoteMode = chkFrpEnabled.Checked;
            bool listeningActive = _listenServer != null && _listenServer.Listening;
            // 隧道是否正在连接/已连接（运行中）——连接中右侧配置不可改（改了不生效）
            bool isRunning = Frp.FrpTunnelManager.Instance.IsRunning;

            // 左侧本地监听控件：只有隧道【真的接管了本地监听】时才置灰。
            // 仅勾选但尚未连接时不夺走用户的本地监听能力——否则会出现“勾了 FRP 又没连上、
            // 本地监听也被禁用”两头不到岸的空窗。
            bool localCtl = !(remoteMode && isRunning);
            txtMultiPorts.Enabled = localCtl && !listeningActive;
            chkIPv6Support.Enabled = localCtl && !listeningActive;
            btnListen.Enabled = localCtl;   // 本地模式下「开始/停止监听」按钮可用
            ncPort.Enabled = false;

            // 右侧 FRP 配置栏：只要隧道没在跑就【始终可编辑】——勾选与否不该决定能不能粘贴配置，
            // 否则首次打开（默认未勾选）时用户根本无从粘贴 frpc 配置，
            // 只能先勾选、被自动连接逻辑判失败回滚，形成“勾不上又填不了”的死锁。
            // 仅连接中(isRunning)置灰：此时改 FRPC 配置不会即时生效，必须先断开再改。
            bool configEditable = !isRunning;
            txtFrpConfig.Enabled = configEditable;
            cboFrpFormat.Enabled = configEditable;
            // 「连接 / 断开」按钮仅在「启用远程FRP隧道」勾选后可用（勾选只解锁按钮，不会自行建链）
            btnFrpToggle.Enabled = remoteMode;
        }

        private void chkFrpEnabled_CheckedChanged(object sender, EventArgs e)
        {
            // 加载期(编程赋值)不执行任何副作用
            if (_initializing)
                return;

            // 勾选框现在是纯粹的「启用模式」开关，不负责握手/拆链：
            // 勾选 ≠ 连接，只解锁下方的「连接」按钮；真正的建链/断链一律由 btnFrpToggle 触发。
            // 这样用户可以先填完 FRPS 地址/Token/端口，再决定是否连接。
            if (!chkFrpEnabled.Checked)
            {
                // 取消启用时若隧道还在跑，按「用户不想用了」处理：拆隧道 + 收回本地监听，
                // 左侧配置恢复可编辑，但不替用户自动起监听。
                if (Frp.FrpTunnelManager.Instance.IsRunning)
                {
                    StopFrp();
                    Networking.ListenerCoordinator.Stop(_listenServer);
                }
                ToggleListenerSettings(true);
                _frpStatusText = "未连接（未启用）";
            }
            else
            {
                _frpStatusText = Frp.FrpTunnelManager.Instance.IsRunning
                    ? "已连接 (C2 隧道在线)"
                    : "未连接（点下方「连接」启动隧道）";
            }

            UpdateFrpStatusLabel();
            UpdateModeStates();
        }

        private void txtFrpConfig_TextChanged(object sender, EventArgs e)
        {
            // 仅内存态：粘贴的配置在「保存」时才落盘；「连接」按钮直接读取本框文本，无需提前持久化。
            // 留空实现以保持 Designer 事件接线，不在此触发磁盘写入（避免逐字符写盘）。
        }

        private void btnFrpToggle_Click(object sender, EventArgs e)
        {
            // 唯一的手动「连接 / 断开」入口：勾选「启用远程FRP隧道」不会自动建链，
            // 必须走到这里。因此不存在“配置还没填就被迫尝试连接、失败后勾不上”的死锁。
            if (!chkFrpEnabled.Checked)
            {
                MessageBox.Show(this, "请先勾选「启用远程FRP隧道」，填完 FRPS 地址、Token 与端口后再连接。",
                    "未启用 FRP 隧道", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mgr = Frp.FrpTunnelManager.Instance;

            if (mgr.IsRunning)
            {
                // 断开：停隧道 + 清控制台 + 收回本地监听，左侧控件恢复可用。
                // 勾选框保持原状（它代表“要不要用隧道”，而非“现在连没连”）。
                StopFrp();
                Networking.ListenerCoordinator.Stop(_listenServer);
                ToggleListenerSettings(true);
                _frpStatusText = "未连接（点下方「连接」重新建链）";
            }
            else
            {
                // 连接前先确认 frpc.exe 已就位：缺失则弹出官方压缩包选择框引导安装。
                if (!mgr.IsBinaryAvailable)
                {
                    if (!PromptInstallFrpc())
                    {
                        _frpStatusText = "未安装 frpc.exe（点「安装/更新 frpc.exe」选择官方压缩包）";
                        UpdateFrpStatusLabel();
                        return;
                    }
                }
                // 连接：失败不回滚勾选框，只把状态标出来，让用户改完配置再点一次。
                if (StartFrpFromUi())
                {
                    ToggleListenerSettings(false);
                    _frpStatusText = "正在连接 FRPS...";
                }
                else
                {
                    _frpStatusText = "连接失败（已保留配置，改完再点「连接」）";
                }
            }

            UpdateFrpStatusLabel();
            UpdateModeStates();   // 连接状态变化后刷新配置栏的灰/亮
        }

        private void FrpStatus_Changed(string status)
        {
            _frpStatusText = status;
            // 状态行已由 FrpTunnelManager 写入历史缓冲，统一通过 FrpLiveFlush 渲染，避免重复
            FrpLiveFlush();
            if (this.IsDisposed || lblFrpStatus.IsDisposed)
                return;
            // 连接/断开状态翻转后，右侧配置栏的灰/亮也要跟着刷新（连接中灰、断开后可改）
            if (lblFrpStatus.InvokeRequired)
                lblFrpStatus.BeginInvoke(new Action(() =>
                {
                    if (this.IsDisposed || lblFrpStatus.IsDisposed) return;
                    UpdateFrpStatusLabel();
                    UpdateModeStates();
                }));
            else
            {
                UpdateFrpStatusLabel();
                UpdateModeStates();
            }
        }

        private void FrpLog_Received(string line)
        {
            FrpLiveFlush();
        }

        /// <summary>
        /// 把历史缓冲中尚未渲染的行追加到控制台。用 _frpRenderedCount 记录已渲染行数，
        /// 既保证关闭重开后能回放完整历史，又避免实时事件与回放之间的重复输出。
        /// </summary>
        private void FrpLiveFlush()
        {
            lock (_frpFlushLock)
            {
                var hist = Frp.FrpTunnelManager.Instance.GetHistory();
                if (hist.Count <= _frpRenderedCount)
                    return;
                for (int i = _frpRenderedCount; i < hist.Count; i++)
                    AppendFrpConsole(hist[i]);
                _frpRenderedCount = hist.Count;
            }
        }

        /// <summary>
        /// 清空 FRP 控制台：清掉可见文本、重置已渲染计数，并清空历史缓冲，
        /// 使下次重新打开设置页也不会回放出已清理的内容。
        /// </summary>
        private void ClearFrpConsole()
        {
            lock (_frpFlushLock)
            {
                Frp.FrpTunnelManager.Instance.ClearHistory();
                _frpRenderedCount = 0;
            }
            if (rtbFrpcConsole != null && !rtbFrpcConsole.IsDisposed)
            {
                if (rtbFrpcConsole.InvokeRequired)
                    rtbFrpcConsole.BeginInvoke(new Action(() => rtbFrpcConsole.Clear()));
                else
                    rtbFrpcConsole.Clear();
            }
        }

        /// <summary>
        /// 线程安全地把一行文本追加到 FRPC 控制台（黑底绿字，错误/失败行标红）。
        /// frpc 的 stdout/stderr 来自子进程回调线程，需通过 Invoke 切回 UI 线程。
        /// </summary>
        private void AppendFrpConsole(string text)
        {
            if (rtbFrpcConsole == null || rtbFrpcConsole.IsDisposed)
                return;
            if (rtbFrpcConsole.InvokeRequired)
            {
                rtbFrpcConsole.BeginInvoke(new Action<string>(AppendFrpConsole), text);
                return;
            }
            rtbFrpcConsole.SelectionStart = rtbFrpcConsole.TextLength;
            rtbFrpcConsole.SelectionLength = 0;
            string lower = (text ?? "").ToLowerInvariant();
            if (lower.Contains("error") || lower.Contains("fail") || lower.Contains("dial tcp")
                || lower.Contains("refused") || lower.Contains("timeout") || lower.Contains("mismatch"))
                rtbFrpcConsole.SelectionColor = System.Drawing.Color.Red;
            else if (lower.Contains("success") || lower.Contains("connected") || lower.Contains("已连接"))
                rtbFrpcConsole.SelectionColor = System.Drawing.Color.Cyan;
            else
                rtbFrpcConsole.SelectionColor = rtbFrpcConsole.ForeColor;
            rtbFrpcConsole.AppendText(text + Environment.NewLine);
            rtbFrpcConsole.SelectionColor = rtbFrpcConsole.ForeColor;
            rtbFrpcConsole.ScrollToCaret();
        }

        private void UpdateFrpStatusLabel()
        {
            if (lblFrpStatus.IsDisposed)
                return;
            lblFrpStatus.Text = "状态: " + _frpStatusText;

            if (!btnFrpToggle.IsDisposed)
            {
                bool running = Frp.FrpTunnelManager.Instance.IsRunning;
                // 运行中 →「断开」；其余（含“已启用但没在跑”“连接失败”）一律显示「连接」，
                // 因为它是唯一的手动建链入口，不该出现"重新连接"这种暗示已连上的措辞。
                btnFrpToggle.Text = running ? "断开" : "连接";
            }
        }

        /// <summary>
        /// 刷新「frpc 客户端」状态标签：显示已安装版本，或「未安装」并提示如何获取。
        /// </summary>
        private void RefreshFrpcBinaryLabel()
        {
            if (lblFrpcBinary.IsDisposed)
                return;
            var mgr = Frp.FrpTunnelManager.Instance;
            if (mgr.IsBinaryAvailable)
            {
                string v = mgr.GetInstalledVersion();
                lblFrpcBinary.Text = "frpc 客户端: 已安装" + (string.IsNullOrEmpty(v) ? "" : " v" + v);
                lblFrpcBinary.ForeColor = System.Drawing.Color.LimeGreen;
            }
            else
            {
                lblFrpcBinary.Text = "frpc 客户端: 未安装（点「安装/更新 frpc.exe」选官方压缩包）";
                lblFrpcBinary.ForeColor = System.Drawing.Color.Orange;
            }
        }

        /// <summary>
        /// 弹出官方 frp 压缩包选择框，安全提取 frpc.exe 到 FRPC/。返回安装后 frpc 是否就位。
        /// </summary>
        private bool PromptInstallFrpc()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "frp 压缩包 (*.zip)|*.zip|所有文件 (*.*)|*.*";
                ofd.Title = "选择官方 frp 压缩包（frpc.exe 将解压到 FRPC/）";
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return false;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    Frp.FrpTunnelManager.Instance.InstallFrpcFromZip(ofd.FileName);
                    RefreshFrpcBinaryLabel();
                    var v = Frp.FrpTunnelManager.Instance.GetInstalledVersion();
                    MessageBox.Show(this,
                        $"frpc.exe 安装成功（版本 {v}）。\n已解压到 FRPC/ 目录。",
                        "安装成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return Frp.FrpTunnelManager.Instance.IsBinaryAvailable;
                }
                catch (Exception ex)
                {
                    RefreshFrpcBinaryLabel();
                    MessageBox.Show(this,
                        "安装失败: " + ex.Message + "\n请确认下载的是官方 frp Windows 发行包（含 frpc.exe）。",
                        "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void btnFrpInstall_Click(object sender, EventArgs e)
        {
            PromptInstallFrpc();
        }

        private void cboFrpFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 实时写入：配置格式只在下次生成 frpc 配置时生效。
            if (_initializing) return;
            string fmt = cboFrpFormat.SelectedIndex == 2 ? "Ini"
                        : (cboFrpFormat.SelectedIndex == 1 ? "Toml" : "Auto");
            Settings.FrpConfigFormat = fmt;
        }

        #endregion
    }
}