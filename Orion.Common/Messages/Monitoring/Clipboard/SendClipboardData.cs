using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Monitoring.Clipboard
{
    [MessagePackObject]
    public class SendClipboardData : IMessage
    {
        [Key(1)]
        public string ClipboardText { get; set; }
    }
}
