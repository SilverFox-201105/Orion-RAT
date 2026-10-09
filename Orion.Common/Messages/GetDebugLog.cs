using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class GetDebugLog : IMessage
    {
        [Key(1)]
        public string Log { get; set; }
    }
}
