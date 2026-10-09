using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDirectory : IMessage
    {
        [Key(1)]
        public string RemotePath { get; set; }
    }
}
