using System;
using MessagePack;
using Orion.Common.Networking;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class PingRequest : IMessage
    {
    }
}
