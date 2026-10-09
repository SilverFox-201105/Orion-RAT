using MessagePack;
using Orion.Common.Messages.Other;
using System;
using System.Collections.Generic;

namespace Orion.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetOutputDeviceResponse : IMessage
    {
        [Key(1)]
        public List<Tuple<int, string>> DeviceInfos { get; set; }
    }
}
