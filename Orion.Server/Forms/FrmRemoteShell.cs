using Orion.Common.Messages;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using Orion.Server.Messages;
using Orion.Server.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmRemoteShell : Form
    {
        /// <summary>
        /// The client which can be used for the remote shell.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        public readonly RemoteShellHandler RemoteShellHandler;

        /// <summary>
        /// Holds the opened remote shell form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmRemoteShell> OpenedForms = new Dictionary<Client, FrmRemoteShell>();

        // ---- 单屏终端模型（输入行由模型管理，光标停在提示符路径之后）----
        /// <summary>已提交输出区与可编辑输入行的分界位置（RichTextBox 文本索引）。</summary>
        private int _inputStart;
        /// <summary>当前命令行文本。</summary>
        private string _inputLine = string.Empty;
        /// <summary>光标在命令行中的相对位置。</summary>
        private int _caret;
        /// <summary>刚发送、等待服务器回显剥除的命令，避免命令重复显示。</summary>
        private string _pendingCommand;

        /// <summary>命令历史（最新在末尾）。</summary>
        private readonly List<string> _history = new List<string>();
        /// <summary>当前在历史中的位置；等于 _history.Count 时处于"草稿/最新"态。</summary>
        private int _historyIndex;
        /// <summary>离开历史底部时暂存的草稿输入。</summary>
        private string _draft = string.Empty;

        public static FrmRemoteShell CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
                return OpenedForms[client];
            FrmRemoteShell f = new FrmRemoteShell(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        public FrmRemoteShell(Client client)
        {
            _connectClient = client;
            RemoteShellHandler = new RemoteShellHandler(client);

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);

            _inputLine = string.Empty;
            _caret = 0;
            _historyIndex = 0;

            EditRtb(() => txtConsoleOutput.AppendText(">> 输入 'exit' 关闭此会话" + Environment.NewLine));
            _inputStart = txtConsoleOutput.TextLength;
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            RemoteShellHandler.ProgressChanged += CommandOutput;
            RemoteShellHandler.CommandError += CommandError;
            MessageHandler.Register(RemoteShellHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(RemoteShellHandler);
            RemoteShellHandler.ProgressChanged -= CommandOutput;
            RemoteShellHandler.CommandError -= CommandError;
            _connectClient.ClientState -= ClientDisconnected;
        }

        // ===== 输出 =====

        private void CommandOutput(object sender, string output)
        {
            output = StripEchoedCommand(output);
            if (!string.IsNullOrEmpty(output))
                AppendOutput(output, Color.WhiteSmoke);
        }

        private void CommandError(object sender, string output)
        {
            output = StripEchoedCommand(output);
            if (!string.IsNullOrEmpty(output))
                AppendOutput(output, Color.Red);
        }

        /// <summary>
        /// 将输出插入到输入行之前（_inputStart 处），输入行随之下移到底部，保证正在输入的
        /// 命令行始终停在控件最下方。输出到达时用户中途打字也不会丢失。
        /// </summary>
        private void AppendOutput(string output, Color color)
        {
            if (string.IsNullOrEmpty(output)) return;
            EditRtb(() =>
            {
                txtConsoleOutput.SelectionStart = _inputStart;
                txtConsoleOutput.SelectionColor = color;
                txtConsoleOutput.SelectedText = output;
                // 插入后光标停在新输出末尾，即为准确的输入行新起点（避免 \r\n 计数误差）。
                _inputStart = txtConsoleOutput.SelectionStart;
            });
            ScrollToBottom();
        }

        /// <summary>
        /// 去除被控端 cmd 可能回显的命令行（首块匹配即剥离，兼容带 "C:\>" 前缀或裸命令），
        /// 无论被控端是否回显都能保证命令只显示一次（已通过本地回显显示）。
        /// </summary>
        private string StripEchoedCommand(string output)
        {
            if (_pendingCommand == null) return output;
            if (string.IsNullOrEmpty(output)) return output;

            string pending = _pendingCommand.TrimEnd('\r', '\n');
            if (pending.Length == 0) { _pendingCommand = null; return output; }

            int nl = output.IndexOf('\n');
            string firstLine = (nl >= 0) ? output.Substring(0, nl) : output;
            firstLine = firstLine.TrimEnd('\r');

            string cmdInLine = firstLine;
            int gt = firstLine.LastIndexOf('>');
            cmdInLine = (gt >= 0) ? firstLine.Substring(gt + 1).Trim() : firstLine.Trim();

            if (cmdInLine == pending)
            {
                int consumeTo = (nl >= 0) ? nl + 1 : output.Length;
                output = output.Substring(consumeTo);
            }
            // 首块只尝试一次，避免误删真实输出。
            _pendingCommand = null;
            return output;
        }

        // ===== 输入行渲染 / 滚动 =====

        /// <summary>在 ReadOnly 的 RichTextBox 上做程序化编辑的包装。</summary>
        private void EditRtb(Action action)
        {
            bool ro = txtConsoleOutput.ReadOnly;
            txtConsoleOutput.ReadOnly = false;
            try { action(); }
            finally { txtConsoleOutput.ReadOnly = ro; }
        }

        /// <summary>用 _inputLine 重绘输入行（移除旧输入行再写入新内容），并把光标置于行内。</summary>
        private void RenderInputLine()
        {
            EditRtb(() =>
            {
                txtConsoleOutput.SelectionStart = _inputStart;
                txtConsoleOutput.SelectionLength = txtConsoleOutput.TextLength - _inputStart;
                txtConsoleOutput.SelectedText = _inputLine;
            });
            SetCaret();
            ScrollToBottom();
        }

        private void SetCaret()
        {
            txtConsoleOutput.SelectionStart = _inputStart + _caret;
            txtConsoleOutput.SelectionLength = 0;
        }

        /// <summary>把视图固定到最底部（输入行始终可见），不改变光标位置。</summary>
        private void ScrollToBottom()
        {
            if (IsDisposed) return;
            NativeMethodsHelper.ScrollToBottom(txtConsoleOutput.Handle);
            // 布局可能尚未完成，等下一轮消息再滚一次，确保落在真正底部。
            BeginInvoke((MethodInvoker)(() =>
            {
                if (IsDisposed) return;
                NativeMethodsHelper.ScrollToBottom(txtConsoleOutput.Handle);
            }));
        }

        // ===== 键盘 =====

        private void txtConsoleOutput_KeyDown(object sender, KeyEventArgs e)
        {
            // 原生快捷键：Ctrl+C 复制选中，Ctrl+A 全选，Ctrl+V 粘贴到输入行。无需右键菜单。
            if (e.Control)
            {
                if (e.KeyCode == Keys.C)
                {
                    if (txtConsoleOutput.SelectionLength > 0)
                    {
                        try { Clipboard.SetText(txtConsoleOutput.SelectedText); } catch { }
                    }
                    e.SuppressKeyPress = true; e.Handled = true; return;
                }
                if (e.KeyCode == Keys.A) { txtConsoleOutput.SelectAll(); e.SuppressKeyPress = true; e.Handled = true; return; }
                if (e.KeyCode == Keys.V) { PasteFromClipboard(); e.SuppressKeyPress = true; e.Handled = true; return; }
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true; e.Handled = true;
                    CommitCurrentLine();
                    break;

                case Keys.Back:
                    e.SuppressKeyPress = true; e.Handled = true;
                    if (_caret > 0)
                    {
                        _inputLine = _inputLine.Remove(_caret - 1, 1);
                        _caret--;
                        RenderInputLine();
                    }
                    break;

                case Keys.Delete:
                    e.SuppressKeyPress = true; e.Handled = true;
                    if (_caret < _inputLine.Length)
                    {
                        _inputLine = _inputLine.Remove(_caret, 1);
                        RenderInputLine();
                    }
                    break;

                case Keys.Left:
                    e.SuppressKeyPress = true; e.Handled = true;
                    if (_caret > 0) { _caret--; SetCaret(); }
                    break;

                case Keys.Right:
                    e.SuppressKeyPress = true; e.Handled = true;
                    if (_caret < _inputLine.Length) { _caret++; SetCaret(); }
                    break;

                case Keys.Home:
                    e.SuppressKeyPress = true; e.Handled = true;
                    _caret = 0; SetCaret();
                    break;

                case Keys.End:
                    e.SuppressKeyPress = true; e.Handled = true;
                    _caret = _inputLine.Length; SetCaret();
                    break;

                case Keys.Up:
                    e.SuppressKeyPress = true; e.Handled = true;
                    NavigateHistory(-1);
                    break;

                case Keys.Down:
                    e.SuppressKeyPress = true; e.Handled = true;
                    NavigateHistory(1);
                    break;
            }
        }

        private void txtConsoleOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 可打印字符插入到光标处；控制字符（含 Ctrl 组合）忽略。
            if (!char.IsControl(e.KeyChar))
            {
                _inputLine = _inputLine.Insert(_caret, e.KeyChar.ToString());
                _caret++;
                RenderInputLine();
                e.Handled = true;
            }
        }

        /// <summary>提交当前输入行：保留命令在屏幕上（本地回显），把输入起点后移到命令之后，
        /// 使后续服务器输出显示在命令下方；随后清空输入行准备接收下一条命令。</summary>
        private void CommitCurrentLine()
        {
            string cmd = _inputLine;
            string trimmed = cmd.Trim().ToLowerInvariant();

            if (trimmed == "exit") { this.Close(); return; }
            if (trimmed == "cls") { ClearScreen(); return; }

            // 在命令后补换行并把输入起点后移。
            EditRtb(() =>
            {
                int pos = _inputStart + _inputLine.Length;
                txtConsoleOutput.SelectionStart = pos;
                txtConsoleOutput.SelectionLength = 0;
                txtConsoleOutput.SelectedText = Environment.NewLine;
                _inputStart = txtConsoleOutput.SelectionStart;
            });
            _pendingCommand = cmd;
            if (!string.IsNullOrWhiteSpace(cmd)) AddHistory(cmd);

            _inputLine = string.Empty;
            _caret = 0;
            ScrollToBottom();
            RemoteShellHandler.SendCommand(cmd);
        }

        private void ClearScreen()
        {
            EditRtb(() => txtConsoleOutput.Clear());
            _inputStart = 0;
            _inputLine = string.Empty;
            _caret = 0;
            ScrollToBottom();
        }

        private void AddHistory(string cmd)
        {
            if (_history.Count == 0 || _history[_history.Count - 1] != cmd)
                _history.Add(cmd);
            _historyIndex = _history.Count;
            _draft = string.Empty;
        }

        /// <summary>上下方向键浏览历史命令（direction &lt; 0 更旧，&gt; 0 更新）。</summary>
        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0) return;

            // 从底部（草稿态）离开时，把当前输入存为草稿。
            if (_historyIndex == _history.Count)
                _draft = _inputLine;

            int next = _historyIndex + direction;
            if (next < 0) next = 0;
            if (next > _history.Count) next = _history.Count;
            _historyIndex = next;

            _inputLine = (next == _history.Count) ? _draft : _history[next];
            _caret = _inputLine.Length;
            RenderInputLine();
        }

        private void PasteFromClipboard()
        {
            string text;
            try { text = Clipboard.GetText(); }
            catch { return; }
            if (string.IsNullOrEmpty(text)) return;
            _inputLine = _inputLine.Insert(_caret, text);
            _caret += text.Length;
            RenderInputLine();
        }

        // ===== 生命周期 =====

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                this.Invoke((MethodInvoker)this.Close);
        }

        private void FrmRemoteShell_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.Text = WindowHelper.GetWindowTitle("远程Shell", _connectClient);
            // 唤醒 Session 以刷出初始提示符
            RemoteShellHandler.SendCommand(string.Empty);
        }

        private void FrmRemoteShell_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            RemoteShellHandler.Dispose();
            if (_connectClient.Connected)
                RemoteShellHandler.SendCommand("exit");
        }
    }
}
