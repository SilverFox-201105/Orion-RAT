using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.RemoteShell
{
    [MessagePackObject]
    public class DoShellExecute : IMessage
    {
        [Key(1)]
        public string Command { get; set; }
    }
}
