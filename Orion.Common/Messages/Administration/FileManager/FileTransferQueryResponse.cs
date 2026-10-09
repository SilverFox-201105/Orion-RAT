using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    /// <summary>
    /// Client -> Server: reply to <see cref="FileTransferQuery"/> carrying the file size.
    /// </summary>
    [MessagePackObject]
    public class FileTransferQueryResponse : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public bool Exists { get; set; }

        [Key(3)]
        public long FileSize { get; set; }
    }
}
