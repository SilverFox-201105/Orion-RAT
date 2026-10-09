using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Monitoring.Clipboard
{
    [MessagePackObject]
    public class DoSendAddress : IMessage
    {
        [Key(1)]
        public string Address { get; set; }
    }
}
