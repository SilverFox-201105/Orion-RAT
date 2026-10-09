using MessagePack;
using Orion.Common.Messages.Other;
using System;
using System.Collections.Generic;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class GetSystemInfoResponse : IMessage
    {
        [Key(1)]
        public List<Tuple<string, string>> SystemInfos { get; set; }
    }
}
