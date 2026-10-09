using MessagePack;

namespace Orion.Common.Messages.Other
{
    [MessagePackObject]
    public sealed class SecureMessageEnvelope : IMessage
    {
        [Key(0)]
        public byte[] Payload { get; set; }
    }
}
