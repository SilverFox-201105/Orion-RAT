using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetOutputResponse : IMessage
    {
        [Key(1)]
        public byte[] Audio { get; set; }

        [Key(2)]
        public int Device { get; set; }
    }
}
