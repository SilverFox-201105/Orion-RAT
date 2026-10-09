using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class DoStartupItemRemove : IMessage
    {
        [Key(1)]
        public StartupItem StartupItem { get; set; }
    }
}
