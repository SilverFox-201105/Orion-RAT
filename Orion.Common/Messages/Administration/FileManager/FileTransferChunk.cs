using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class FileTransferChunk : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public string FilePath { get; set; }

        [Key(3)]
        public long FileSize { get; set; }

        [Key(4)]
        public FileChunk Chunk { get; set; }

        [Key(5)]
        public string FileExtension { get; set; }
    }
}
