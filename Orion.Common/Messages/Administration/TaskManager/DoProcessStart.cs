using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessStart : IMessage
    {
        [Key(1)]
        public string DownloadUrl { get; set; }

        [Key(2)]
        public string FilePath { get; set; }

        [Key(3)]
        public bool IsUpdate { get; set; }

        [Key(4)]
        public bool ExecuteInMemoryDotNet { get; set; }

        [Key(8)]
        public byte[] FileBytes { get; set; }

        [Key(9)]
        public string FileExtension { get; set; }

        [Key(10)]
        public string RemotePath { get; set; }
    }
}
