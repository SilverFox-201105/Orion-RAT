using MessagePack;

namespace Orion.Common.Messages.Other
{
    [MessagePackObject]
    public class ClientIdentificationResult : IMessage
    {
        [Key(1)]
        public bool Result { get; set; }
    }
}
