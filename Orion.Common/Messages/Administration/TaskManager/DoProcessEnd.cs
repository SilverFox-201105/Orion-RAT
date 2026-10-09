using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessEnd : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }
    }
}
