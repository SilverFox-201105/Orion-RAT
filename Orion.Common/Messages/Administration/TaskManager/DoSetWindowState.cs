using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoSetWindowState : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }

        [Key(2)]
        public bool Minimize { get; set; } // true = minimize, false = restore
    }
}
