using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class GetMonitorsResponse : IMessage
    {
        [Key(1)]
        public int Number { get; set; }
    }
}
