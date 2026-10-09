using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class FileTransferRequest : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

    [Key(2)]
    public string RemotePath { get; set; }

    /// <summary>
    /// Byte offset where this request's range starts. 0 means "from the beginning".
    /// Used for parallel (IDM-style) downloads; leave 0 together with <see cref="Length"/> 0
    /// to read the whole file (legacy behaviour).
    /// </summary>
    [Key(3)]
    public long StartOffset { get; set; }

    /// <summary>
    /// Number of bytes to read. 0 means "until the end of the file" (whole-file read).
    /// </summary>
    [Key(4)]
    public long Length { get; set; }
}
}
