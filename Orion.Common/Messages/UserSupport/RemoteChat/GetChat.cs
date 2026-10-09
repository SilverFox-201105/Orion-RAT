using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.UserSupport.RemoteChat
{
    [MessagePackObject]
    public class GetChat : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
