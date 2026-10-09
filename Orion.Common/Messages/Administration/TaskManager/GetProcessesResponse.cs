using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class GetProcessesResponse : IMessage
    {
        [Key(1)]
        public Process[] Processes { get; set; }

        [Key(2)]
        public int? RatPid { get; set; }
    }
}
