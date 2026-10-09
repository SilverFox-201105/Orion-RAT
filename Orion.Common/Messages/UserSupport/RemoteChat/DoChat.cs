using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.UserSupport.RemoteChat
{
    [MessagePackObject]
    public class DoChat : IMessage
    {
        [Key(1)]
        public string PacketDms { get; set; }
        [Key(2)]
        public string User { get; set; }
    }
}
