using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TCPConnections
{
    [MessagePackObject]
    public class DoCloseConnection : IMessage
    {
        [Key(1)]
        public string LocalAddress { get; set; }

        [Key(2)]
        public ushort LocalPort { get; set; }

        [Key(3)]
        public string RemoteAddress { get; set; }

        [Key(4)]
        public ushort RemotePort { get; set; }
    }
}
