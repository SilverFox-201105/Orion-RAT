using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Monitoring.RemoteDesktop
{
    [MessagePackObject]
    public class DoKeyboardEvent : IMessage
    {
        [Key(1)]
        public byte Key { get; set; }

        [Key(2)]
        public bool KeyDown { get; set; }
    }
}
