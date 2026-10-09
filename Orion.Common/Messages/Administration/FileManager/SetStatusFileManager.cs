using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class SetStatusFileManager : IMessage
    {
        [Key(1)]
        public string Message { get; set; }

        [Key(2)]
        public bool SetLastDirectorySeen { get; set; }
    }
}
