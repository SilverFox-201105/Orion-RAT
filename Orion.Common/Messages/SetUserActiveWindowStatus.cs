using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class SetUserActiveWindowStatus : IMessage
    {
        [Key(1)]
        public string WindowTitle { get; set; }
    }
}