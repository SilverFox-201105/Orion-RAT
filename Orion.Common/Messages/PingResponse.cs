using System;
using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class PingResponse : IMessage
    {
    }
}
