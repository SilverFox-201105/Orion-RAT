using MessagePack;
using Orion.Common.Enums;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class SetUserStatus : IMessage
    {
        [Key(1)]
        public UserStatus Message { get; set; }
    }
}
