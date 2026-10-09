using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDirectoryResponse : IMessage
    {
        [Key(1)]
        public string RemotePath { get; set; }

        [Key(2)]
        public FileSystemEntry[] Items { get; set; }
    }
}
