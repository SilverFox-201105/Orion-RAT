using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    /// <summary>
    /// Server -> Client: asks the client for the size/existence of a remote file
    /// so the server can split a download into parallel segments (IDM-style).
    /// </summary>
    [MessagePackObject]
    public class FileTransferQuery : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public string RemotePath { get; set; }
    }
}
