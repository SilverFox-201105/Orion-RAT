using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.QuickCommands
{
    [MessagePackObject]
    public class DoSendQuickCommand : IMessage
    {
        [Key(1)]
        public string Command { get; set; }
        [Key(2)]
        public string Host { get; set; }
    }
}
