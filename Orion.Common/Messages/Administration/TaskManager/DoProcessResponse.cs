using MessagePack;
using Orion.Common.Enums;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessResponse : IMessage
    {
        [Key(1)]
        public ProcessAction Action { get; set; }

        [Key(2)]
        public bool Result { get; set; }
    }
}
