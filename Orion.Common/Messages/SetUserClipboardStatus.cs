using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class SetUserClipboardStatus : IMessage
    {
        [Key(1)]
        public string ClipboardText { get; set; }
    }
}
