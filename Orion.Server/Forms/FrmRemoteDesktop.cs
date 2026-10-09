using Gma.System.MouseKeyHook;
using Orion.Common.Enums;
using Orion.Common.Messages;
using Orion.Common.Messages.Monitoring.Clipboard;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Forms.RemoteDesktopPopUp;
using Orion.Server.Helper;
using Orion.Server.Messages;
using Orion.Server.Networking;
using Orion.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRemoteDesktop : Form
    {
        /// <summary>
        /// States whether remote mouse input is enabled.
        /// </summary>
        private bool _enableMouseInput;

        /// <summary>
        /// States whether remote keyboard input is enabled.
        /// </summary>
        private bool _enableKeyboardInput;

        /// <summary>
        /// States whether drawing mode is enabled.
        /// </summary>
        private bool _enableDrawingMode;

        /// <summary>
        /// States whether eraser mode is enabled.
        /// </summary>
        private bool _enableEraserMode;

        /// <summary>
        /// The current stroke width for drawing.
        /// </summary>
        private int _strokeWidth = 5;

        /// <summary>
        /// The current drawing color.
        /// </summary>
        private Color _drawingColor = Color.Red;

        /// <summary>
        /// Keeps track of the previous mouse position for drawing.
        /// </summary>
        private Point _previousMousePosition = Point.Empty;

        /// <summary>
        /// Holds the state of the local keyboard hooks.
        /// </summary>
        private IKeyboardMouseEvents _keyboardHook;

        /// <summary>
        /// Holds the state of the local mouse hooks.
        /// </summary>
        private IKeyboardMouseEvents _mouseHook;

        /// <summary>
        /// A list of pressed keys for synchronization between key down & -up events.
        /// </summary>
        private readonly List<Keys> _keysPressed;

        /// <summary>
        /// Monitors clipboard changes on the server to send to the client
        /// </summary>
        private ClipboardMonitor _clipboardMonitor;

        /// <summary>
        /// States whether bidirectional clipboard sync is enabled
        /// </summary>
        private bool _enableBidirectionalClipboard;

        /// <summary>
        /// The client which can be used for the remote desktop.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly RemoteDesktopHandler _remoteDesktopHandler;

        /// <summary>
        /// Holds the opened remote desktop form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmRemoteDesktop> OpenedForms = new Dictionary<Client, FrmRemoteDesktop>();

        private bool _useGPU = false;

        private int _sizeFrames = 0; // independent counter for size label updates
        private float _dpiScale = 1f; // 当前 DPI 缩放系数，用于运行时切换图标后重新缩放

        /// <summary>
        /// Creates a new remote desktop form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the remote desktop form.</param>
        /// <returns>
        /// Returns a new remote desktop form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmRemoteDesktop CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmRemoteDesktop r = new FrmRemoteDesktop(client);
            r.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, r);
            return r;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmRemoteDesktop"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the remote desktop form.</param>
        public FrmRemoteDesktop(Client client)
        {
            _connectClient = client;
            _remoteDesktopHandler = new RemoteDesktopHandler(client);
            _keysPressed = new List<Keys>();
            _clipboardMonitor = new ClipboardMonitor(client);

            RegisterMessageHandler();
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FrmRemoteDesktop.InitializeComponent failed: {ex}");
                MessageBox.Show(
                    $"初始化远程桌面窗体失败。\n\n{ex}",
                    "远程桌面错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }

            DarkModeManager.ApplyDarkMode(this);

            colorPicker.BackColor = _drawingColor;
            colorPicker.FlatStyle = FlatStyle.Flat;
            colorPicker.FlatAppearance.BorderColor = Color.White;
            colorPicker.Text = "颜色";
            colorPicker.ForeColor = Color.White;

            ConfigureDrawingButtons();

            btnShowDrawingTools.Enabled = false;

            strokeWidthTrackBar.Value = _strokeWidth;
            strokeWidthTrackBar.ValueChanged += strokeWidthTrackBar_ValueChanged;
            colorPicker.Click += colorPicker_Click;

            // Update tooltip text for bidirectional clipboard
            toolTipButtons.SetToolTip(this.btnBiDirectionalClipboard, "启用双向剪贴板同步");

            ApplyHighDpiImages();
        }

        private void ApplyHighDpiImages()
        {
            _dpiScale = DpiImageScaling.GetScaleFactor(this);

            // 固定图标按钮（不切换图标）。
            DpiImageScaling.ApplyToIconButton(btnBiDirectionalClipboard, _dpiScale);
            DpiImageScaling.ApplyToIconButton(btnStartProgramOnDisplay, _dpiScale);

            // 运行时会切换图标的按钮：以当前 Image 为源缩放，切换时再各自重新缩放。
            DpiImageScaling.SetScaledButtonIcon(btnMouse, btnMouse.Image, _dpiScale);
            DpiImageScaling.SetScaledButtonIcon(btnKeyboard, btnKeyboard.Image, _dpiScale);
            DpiImageScaling.SetScaledButtonIcon(enableGPU, enableGPU.Image, _dpiScale);
            DpiImageScaling.SetScaledButtonIcon(btnShowDrawingTools, btnShowDrawingTools.Image, _dpiScale);
        }

        /// <summary>
        /// Configures the drawing tool buttons with the correct styling and icons
        /// </summary>
        private void ConfigureDrawingButtons()
        {
            float scale = DpiImageScaling.GetScaleFactor(this);
            // 按钮尺寸随 DPI 缩放，BackgroundImage 用 Zoom 会自动跟着等比放大，避免高分辨率下图标偏小。
            Size btnSize = new Size((int)Math.Round(60 * scale), (int)Math.Round(28 * scale));

            // pencil
            btnDrawing.Size = btnSize;
            btnDrawing.FlatStyle = FlatStyle.Flat;
            btnDrawing.FlatAppearance.BorderSize = 1;
            btnDrawing.BackgroundImage = Properties.Resources.pencil;
            btnDrawing.BackgroundImageLayout = ImageLayout.Zoom;
            btnDrawing.Text = string.Empty;
            btnDrawing.BackColor = SystemColors.Control;
            btnDrawing.UseVisualStyleBackColor = false;
            toolTipButtons.SetToolTip(btnDrawing, "启用画笔");

            // eraser
            btnEraser.Size = btnSize;
            btnEraser.FlatStyle = FlatStyle.Flat;
            btnEraser.FlatAppearance.BorderSize = 1;
            btnEraser.BackgroundImage = Properties.Resources.eraser;
            btnEraser.BackgroundImageLayout = ImageLayout.Zoom;
            btnEraser.Text = string.Empty;
            btnEraser.BackColor = SystemColors.Control;
            btnEraser.UseVisualStyleBackColor = false;
            toolTipButtons.SetToolTip(btnEraser, "启用橡皮擦");

            // clear
            btnClearDrawing.Size = btnSize;
            btnClearDrawing.FlatStyle = FlatStyle.Flat;
            btnClearDrawing.FlatAppearance.BorderSize = 1;
            btnClearDrawing.BackgroundImage = Properties.Resources.clear;
            btnClearDrawing.BackgroundImageLayout = ImageLayout.Zoom;
            btnClearDrawing.Text = string.Empty;
            btnClearDrawing.BackColor = SystemColors.Control;
            btnClearDrawing.UseVisualStyleBackColor = false;
            toolTipButtons.SetToolTip(btnClearDrawing, "清除画布");
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        /// <summary>
        /// Registers the remote desktop message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _remoteDesktopHandler.DisplaysChanged += DisplaysChanged;
            _remoteDesktopHandler.ProgressChanged += UpdateImage;
            MessageHandler.Register(_remoteDesktopHandler);
        }

        /// <summary>
        /// Unregisters the remote desktop message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_remoteDesktopHandler);
            _remoteDesktopHandler.DisplaysChanged -= DisplaysChanged;
            _remoteDesktopHandler.ProgressChanged -= UpdateImage;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Subscribes to local mouse and keyboard events for remote desktop input.
        /// </summary>
        private void SubscribeEvents()
        {
            _keyboardHook = Hook.GlobalEvents();
            _keyboardHook.KeyDown += OnKeyDown;
            _keyboardHook.KeyUp += OnKeyUp;

            _mouseHook = Hook.AppEvents();
            _mouseHook.MouseWheel += OnMouseWheelMove;
        }

        /// <summary>
        /// Unsubscribes from local mouse and keyboard events.
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyDown -= OnKeyDown;
                _keyboardHook.KeyUp -= OnKeyUp;
                _keyboardHook.Dispose();
            }
            if (_mouseHook != null)
            {
                _mouseHook.MouseWheel -= OnMouseWheelMove;
                _mouseHook.Dispose();
            }
        }

        /// <summary>
        /// Starts the remote desktop stream and begin to receive desktop frames.
        /// </summary>
        private void StartStream(bool useGpu)
        {
            ToggleConfigurationControls(true);

            picDesktop.Start();
            // Subscribe to the new frame counter.
            picDesktop.SetFrameUpdatedEvent(frameCounter_FrameUpdated);

            this.ActiveControl = picDesktop;

            _remoteDesktopHandler.BeginReceiveFrames(barQuality.Value, cbMonitors.SelectedIndex, useGpu);

            btnShowDrawingTools.Enabled = true;
        }

        /// <summary>
        /// Stops the remote desktop stream.
        /// </summary>
        private void StopStream()
        {
            ToggleConfigurationControls(false);

            picDesktop.Stop();
            // Unsubscribe from the frame counter. It will be re-created when starting again.
            picDesktop.UnsetFrameUpdatedEvent(frameCounter_FrameUpdated);

            this.ActiveControl = picDesktop;

            _remoteDesktopHandler.EndReceiveFrames();

            btnShowDrawingTools.Enabled = false;

            if (panelDrawingTools.Visible)
            {
                panelDrawingTools.Visible = false;
                DpiImageScaling.SetScaledButtonIcon(btnShowDrawingTools, Properties.Resources.arrow_up, _dpiScale);
                toolTipButtons.SetToolTip(btnShowDrawingTools, "显示绘图工具");
            }

            _enableDrawingMode = false;
            _enableEraserMode = false;

            ConfigureDrawingButtons();

            picDesktop.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Toggles the activatability of configuration controls in the status/configuration panel.
        /// </summary>
        /// <param name="started">When set to <code>true</code> the configuration controls get enabled, otherwise they get disabled.</param>
        private void ToggleConfigurationControls(bool started)
        {
            btnStart.Enabled = !started;
            btnStop.Enabled = started;
            barQuality.Enabled = !started;
            cbMonitors.Enabled = !started;
        }

        /// <summary>
        /// Toggles the visibility of the status/configuration panel.
        /// </summary>
        /// <param name="visible">Decides if the panel should be visible.</param>
        private void TogglePanelVisibility(bool visible)
        {
            panelTop.Visible = visible;
            btnShow.Visible = !visible;
            this.ActiveControl = picDesktop;
        }

        /// <summary>
        /// Called whenever the remote displays changed.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="displays">The currently available displays.</param>
        private void DisplaysChanged(object sender, int displays)
        {
            cbMonitors.Items.Clear();
            for (int i = 0; i < displays; i++)
                cbMonitors.Items.Add($"显示器 {i + 1}");
            cbMonitors.SelectedIndex = 0;
        }

        private void UpdateImage(object sender, Bitmap bmp)
        {
            _sizeFrames++;
            if (_sizeFrames >= 60)
            {
                _sizeFrames = 0;
                long last = _remoteDesktopHandler.LastFrameSizeBytes;
                double avg = _remoteDesktopHandler.AverageFrameSizeBytes;
                if (last > 0 || avg > 0)
                {
                    double lastKB = last / 1024.0;
                    double avgKB = avg / 1024.0;
                    this.Invoke((MethodInvoker)delegate
                    {
                        sizeLabelCounter.Text = $"{avgKB:0.0}  KB";
                    });
                }
            }
            picDesktop.UpdateImage(bmp, false);
        }

        private void FrmRemoteDesktop_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("远程桌面", _connectClient);

            OnResize(EventArgs.Empty); // trigger resize event to align controls

            panelDrawingTools.Visible = false;
            DpiImageScaling.SetScaledButtonIcon(btnShowDrawingTools, Properties.Resources.arrow_up, _dpiScale);
            toolTipButtons.SetToolTip(btnShowDrawingTools, "显示绘图工具");

            _enableDrawingMode = false;
            _enableEraserMode = false;

            ConfigureDrawingButtons();

            _remoteDesktopHandler.RefreshDisplays();
        }

        /// <summary>
        /// Updates the title with the current frames per second.
        /// </summary>
        /// <param name="e">The new frames per second.</param>
        private void frameCounter_FrameUpdated(FrameUpdatedEventArgs e)
        {
            float fpsToShow = _remoteDesktopHandler.CurrentFps > 0 ? _remoteDesktopHandler.CurrentFps : e.CurrentFramesPerSecond;
            this.Text = string.Format("{0} - FPS: {1}", WindowHelper.GetWindowTitle("远程桌面", _connectClient), fpsToShow.ToString("0.00"));
        }

        private void FrmRemoteDesktop_FormClosing(object sender, FormClosingEventArgs e)
        {
            // all cleanup logic goes here
            SetClientClipboardSync(enabled: false);
            UnsubscribeEvents();
            if (_remoteDesktopHandler.IsStarted) StopStream();
            UnregisterMessageHandler();
            _remoteDesktopHandler.Dispose();
            _clipboardMonitor?.Dispose();

        }

        private void FrmRemoteDesktop_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            _remoteDesktopHandler.LocalResolution = picDesktop.ClientSize;
            btnShow.Left = (this.Width - btnShow.Width) / 2;
            btnShow.Top = this.Height - btnShow.Height - 40;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (cbMonitors.Items.Count == 0)
            {
                MessageBox.Show("未检测到远程显示器。\n请等待客户端发送可用显示器列表。",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SubscribeEvents();
            StartStream(_useGPU);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();
            StopStream();
        }

        #region Remote Desktop Input

        private void picDesktop_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.ContainsFocus)
            {
                if ((_enableDrawingMode || _enableEraserMode) && e.Button == MouseButtons.Left)
                {
                    _previousMousePosition = e.Location;
                }
                else if (_enableMouseInput && !(_enableDrawingMode || _enableEraserMode))
                {
                    MouseAction action = MouseAction.None;

                    if (e.Button == MouseButtons.Left)
                        action = MouseAction.LeftDown;
                    if (e.Button == MouseButtons.Right)
                        action = MouseAction.RightDown;

                    int selectedDisplayIndex = cbMonitors.SelectedIndex;

                    _remoteDesktopHandler.SendMouseEvent(action, true, e.X, e.Y, selectedDisplayIndex);
                }
            }
        }

        private void picDesktop_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.ContainsFocus)
            {
                if ((_enableDrawingMode || _enableEraserMode) && e.Button == MouseButtons.Left)
                {
                    if (_previousMousePosition != Point.Empty &&
                        (_previousMousePosition.X != e.X || _previousMousePosition.Y != e.Y))
                    {
                        int selectedDisplayIndex = cbMonitors.SelectedIndex;

                        bool useEraser = _enableEraserMode;

                        _remoteDesktopHandler.SendDrawingEvent(
                            e.X, e.Y,
                            _previousMousePosition.X, _previousMousePosition.Y,
                            _strokeWidth,
                            _drawingColor.ToArgb(),
                            useEraser,
                            false,
                            selectedDisplayIndex);

                        _previousMousePosition = e.Location;
                    }
                }
                else if (_enableMouseInput && !(_enableDrawingMode || _enableEraserMode))
                {
                    int selectedDisplayIndex = cbMonitors.SelectedIndex;

                    _remoteDesktopHandler.SendMouseEvent(MouseAction.MoveCursor, false, e.X, e.Y, selectedDisplayIndex);
                }
            }
        }

        private void picDesktop_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.ContainsFocus)
            {
                if ((_enableDrawingMode || _enableEraserMode) && e.Button == MouseButtons.Left)
                {
                    _previousMousePosition = Point.Empty;
                }
                else if (_enableMouseInput && !(_enableDrawingMode || _enableEraserMode))
                {
                    MouseAction action = MouseAction.None;

                    if (e.Button == MouseButtons.Left)
                        action = MouseAction.LeftUp;
                    if (e.Button == MouseButtons.Right)
                        action = MouseAction.RightUp;

                    int selectedDisplayIndex = cbMonitors.SelectedIndex;

                    _remoteDesktopHandler.SendMouseEvent(action, false, e.X, e.Y, selectedDisplayIndex);
                }
            }
        }

        private void OnMouseWheelMove(object sender, MouseEventArgs e)
        {
            if (_enableMouseInput && this.ContainsFocus)
            {
                _remoteDesktopHandler.SendMouseEvent(e.Delta == 120 ? MouseAction.ScrollUp : MouseAction.ScrollDown,
                    false, 0, 0, cbMonitors.SelectedIndex);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (picDesktop.HasFrame && _enableKeyboardInput && this.ContainsFocus)
            {
                if (!IsLockKey(e.KeyCode))
                    e.Handled = true;

                if (_keysPressed.Contains(e.KeyCode))
                    return;

                _keysPressed.Add(e.KeyCode);

                _remoteDesktopHandler.SendKeyboardEvent((byte)e.KeyCode, true);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (picDesktop.HasFrame && _enableKeyboardInput && this.ContainsFocus)
            {
                if (!IsLockKey(e.KeyCode))
                    e.Handled = true;

                _keysPressed.Remove(e.KeyCode);

                _remoteDesktopHandler.SendKeyboardEvent((byte)e.KeyCode, false);
            }
        }

        private bool IsLockKey(Keys key)
        {
            return ((key & Keys.CapsLock) == Keys.CapsLock)
                   || ((key & Keys.NumLock) == Keys.NumLock)
                   || ((key & Keys.Scroll) == Keys.Scroll);
        }

        #endregion Remote Desktop Input

        #region Remote Desktop Configuration

        private void barQuality_Scroll(object sender, EventArgs e)
        {
            int value = barQuality.Value;
            lblQualityShow.Text = value.ToString();

            if (value < 25)
                lblQualityShow.Text += "（低）";
            else if (value >= 85)
                lblQualityShow.Text += "（最佳）";
            else if (value >= 75)
                lblQualityShow.Text += "（高）";
            else if (value >= 25)
                lblQualityShow.Text += "（中）";

            this.ActiveControl = picDesktop;
        }

        private void btnMouse_Click(object sender, EventArgs e)
        {
            if (_enableMouseInput)
            {
                this.picDesktop.Cursor = Cursors.Default;
                DpiImageScaling.SetScaledButtonIcon(btnMouse, Properties.Resources.mouse_delete, _dpiScale);
                toolTipButtons.SetToolTip(btnMouse, "启用鼠标输入。");
                _enableMouseInput = false;
            }
            else
            {
                this.picDesktop.Cursor = Cursors.Hand;
                DpiImageScaling.SetScaledButtonIcon(btnMouse, Properties.Resources.mouse_add, _dpiScale);
                toolTipButtons.SetToolTip(btnMouse, "禁用鼠标输入。");
                _enableMouseInput = true;
            }

            UpdateInputButtonsVisualState();
            this.ActiveControl = picDesktop;
        }

        private void btnKeyboard_Click(object sender, EventArgs e)
        {
            if (_enableKeyboardInput)
            {
                this.picDesktop.Cursor = Cursors.Default;
                DpiImageScaling.SetScaledButtonIcon(btnKeyboard, Properties.Resources.keyboard_delete, _dpiScale);
                toolTipButtons.SetToolTip(btnKeyboard, "启用键盘输入。");
                _enableKeyboardInput = false;
            }
            else
            {
                this.picDesktop.Cursor = Cursors.Hand;
                DpiImageScaling.SetScaledButtonIcon(btnKeyboard, Properties.Resources.keyboard_add, _dpiScale);
                toolTipButtons.SetToolTip(btnKeyboard, "禁用键盘输入。");
                _enableKeyboardInput = true;
            }

            UpdateInputButtonsVisualState();
            this.ActiveControl = picDesktop;
        }

        private void enableGPU_Click(object sender, EventArgs e)
        {
            _useGPU = !_useGPU;
            if (_useGPU)
            {
                DpiImageScaling.SetScaledButtonIcon(enableGPU, Properties.Resources.computer_go, _dpiScale); // enable GPU
                toolTipButtons.SetToolTip(enableGPU, "禁用 GPU。");
            }
            else
            {
                DpiImageScaling.SetScaledButtonIcon(enableGPU, Properties.Resources.computer_error, _dpiScale); // disable GPU
                toolTipButtons.SetToolTip(enableGPU, "启用 GPU。");
            }
            UpdateInputButtonsVisualState();
        }

        private void btnDrawing_Click(object sender, EventArgs e)
        {
            if (!_remoteDesktopHandler.IsStarted)
            {
                MessageBox.Show("画笔功能仅在远程桌面启动后可用",
                    "画笔不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _enableDrawingMode = !_enableDrawingMode;

            if (_enableDrawingMode)
            {
                _enableEraserMode = false;

                btnEraser.BackColor = SystemColors.Control;
                toolTipButtons.SetToolTip(btnEraser, "启用橡皮擦");

                btnDrawing.BackColor = Color.FromArgb(120, 170, 120);
                toolTipButtons.SetToolTip(btnDrawing, "禁用画笔");
                picDesktop.Cursor = Cursors.Cross;
            }
            else
            {
                btnDrawing.BackColor = SystemColors.Control;
                toolTipButtons.SetToolTip(btnDrawing, "启用画笔");
                picDesktop.Cursor = Cursors.Default;
            }

            this.ActiveControl = picDesktop;
        }

        private void btnEraser_Click(object sender, EventArgs e)
        {
            if (!_remoteDesktopHandler.IsStarted)
            {
                MessageBox.Show("橡皮擦功能仅在远程桌面启动后可用",
                    "橡皮擦不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _enableEraserMode = !_enableEraserMode;

            if (_enableEraserMode)
            {
                _enableDrawingMode = false;

                btnDrawing.BackColor = SystemColors.Control;
                toolTipButtons.SetToolTip(btnDrawing, "启用画笔");

                btnEraser.BackColor = Color.FromArgb(120, 170, 120);
                toolTipButtons.SetToolTip(btnEraser, "禁用橡皮擦");
                picDesktop.Cursor = Cursors.Hand;
            }
            else
            {
                btnEraser.BackColor = SystemColors.Control;
                toolTipButtons.SetToolTip(btnEraser, "启用橡皮擦");
                picDesktop.Cursor = Cursors.Default;
            }

            this.ActiveControl = picDesktop;
        }

        private void btnShowDrawingTools_Click(object sender, EventArgs e)
        {
            ToggleDrawingPanelVisibility(!panelDrawingTools.Visible);
        }

        private void ToggleDrawingPanelVisibility(bool visible)
        {
            if (visible && !_remoteDesktopHandler.IsStarted)
            {
                MessageBox.Show("绘图工具仅在远程桌面启动后可用",
                    "绘图不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            panelDrawingTools.Visible = visible;
            DpiImageScaling.SetScaledButtonIcon(btnShowDrawingTools,
                visible ? Properties.Resources.arrow_down : Properties.Resources.arrow_up, _dpiScale);
            toolTipButtons.SetToolTip(btnShowDrawingTools,
                visible ? "隐藏绘图工具" : "显示绘图工具");
            this.ActiveControl = picDesktop;
        }

        private void btnClearDrawing_Click(object sender, EventArgs e)
        {
            if (!_remoteDesktopHandler.IsStarted)
            {
                MessageBox.Show("清除画布功能仅在远程桌面启动后可用",
                    "清除不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int displayIndex = cbMonitors.SelectedIndex;
            _remoteDesktopHandler.SendDrawingEvent(0, 0, 0, 0, 0, 0, false, true, displayIndex);
        }

        private void colorPicker_Click(object sender, EventArgs e)
        {
            if (!_remoteDesktopHandler.IsStarted)
            {
                MessageBox.Show("颜色选择功能仅在远程桌面启动后可用",
                    "颜色选择不可用", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = _drawingColor;
            colorDialog.AllowFullOpen = true;
            colorDialog.AnyColor = true;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                _drawingColor = colorDialog.Color;
                colorPicker.BackColor = _drawingColor;

                colorPicker.ForeColor = GetContrastColor(_drawingColor);
            }
        }

        private void strokeWidthTrackBar_ValueChanged(object sender, EventArgs e)
        {
            _strokeWidth = strokeWidthTrackBar.Value;
        }

        private Color GetContrastColor(Color color)
        {
            int brightness = (int)Math.Sqrt(
                color.R * color.R * 0.299 +
                color.G * color.G * 0.587 +
                color.B * color.B * 0.114);

            return brightness > 130 ? Color.Black : Color.White;
        }

        #endregion Remote Desktop Configuration

        /// <summary>
        /// Updates the visual state of the input buttons based on current input settings
        /// </summary>
        private void UpdateInputButtonsVisualState()
        {
            UpdateButtonState(btnMouse, _enableMouseInput);
            UpdateButtonState(btnKeyboard, _enableKeyboardInput);
            UpdateButtonState(btnBiDirectionalClipboard, _enableBidirectionalClipboard);
            UpdateButtonState(enableGPU, _useGPU);
        }

        private void UpdateButtonState(Button button, bool enabled)
        {
            if (enabled)
            {
                button.BackColor = Color.FromArgb(0, 120, 0); // Dark green
                button.FlatAppearance.BorderColor = Color.LimeGreen;
            }
            else
            {
                button.BackColor = Color.FromArgb(40, 40, 40); // Default dark
                button.FlatAppearance.BorderColor = Color.Gray;
            }
        }

        private void btnHide_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(false);
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(true);
        }

        private void btnStartProgramOnDisplay_Click(object sender, EventArgs e)
        {
            int currentDisplayIndex = cbMonitors.SelectedIndex;
            FrmOpenApplicationOnMonitor frm = new FrmOpenApplicationOnMonitor(_connectClient, currentDisplayIndex);
            frm.ShowDialog(this);
        }

        private void btnBiDirectionalClipboard_Click(object sender, EventArgs e)
        {
            _enableBidirectionalClipboard = !_enableBidirectionalClipboard;

            UpdateInputButtonsVisualState();

            // Server-side clipboard monitor only controls server->client sync
            _clipboardMonitor.IsEnabled = _enableBidirectionalClipboard;
            Debug.WriteLine(_clipboardMonitor.IsEnabled ? "Server->Client clipboard sync enabled." : "Server->Client clipboard sync disabled.");

            SetClientClipboardSync(_enableBidirectionalClipboard);

            if (_enableBidirectionalClipboard)
            {
                Thread clipboardThread = new Thread(() =>
                {
                    try
                    {
                        Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

                        if (Clipboard.ContainsText())
                        {
                            string clipboardText = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(clipboardText))
                            {
                                Debug.WriteLine($"Sending initial clipboard: {clipboardText.Substring(0, Math.Min(20, clipboardText.Length))}...");
                                _connectClient.Send(new SendClipboardData { ClipboardText = clipboardText });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error sending initial clipboard: {ex.Message}");
                    }
                });
                clipboardThread.SetApartmentState(ApartmentState.STA);
                clipboardThread.Start();
            }
        }

        private void SetClientClipboardSync(bool enabled)
        {
            try
            {
                if (_connectClient != null)
                {
                    _connectClient.ClipboardSyncEnabled = enabled;
                }

                _connectClient?.Send(new SetClipboardMonitoringEnabled { Enabled = enabled });
                Debug.WriteLine(enabled
                    ? "Requested client clipboard sync enable."
                    : "Requested client clipboard sync disable.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update client clipboard sync state: {ex.Message}");
            }
        }
    }
}