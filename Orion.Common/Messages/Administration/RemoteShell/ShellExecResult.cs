using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.RemoteShell
{
    [MessagePackObject]
    public class ShellExecResult : IMessage
    {
        [Key(1)]
        public string Output { get; set; }

        [Key(2)]
        public bool IsError { get; set; }
    }
}
