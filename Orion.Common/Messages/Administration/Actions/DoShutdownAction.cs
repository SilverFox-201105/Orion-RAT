using MessagePack;
using Orion.Common.Enums;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.Actions
{
    [MessagePackObject]
    public class DoShutdownAction : IMessage
    {
        [Key(1)]
        public ShutdownAction Action { get; set; }
    }
}
