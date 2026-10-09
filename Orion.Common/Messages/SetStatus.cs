using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class SetStatus : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
