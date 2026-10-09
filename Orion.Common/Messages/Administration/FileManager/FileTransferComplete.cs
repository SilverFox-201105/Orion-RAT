using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class FileTransferComplete : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public string FilePath { get; set; }
    }
}
