using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.TCPConnections
{
    [MessagePackObject]
    public class GetConnectionsResponse : IMessage
    {
        [Key(1)]
        public TcpConnection[] Connections { get; set; }
    }
}
