using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDrivesResponse : IMessage
    {
        [Key(1)]
        public Drive[] Drives { get; set; }
    }
}
