using MessagePack;
using Orion.Common.Enums;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class DoPathDelete : IMessage
    {
        [Key(1)]
        public string Path { get; set; }

        [Key(2)]
        public FileType PathType { get; set; }
    }
}
