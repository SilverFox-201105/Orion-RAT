using Orion.Common.Messages.Other;

namespace Orion.Common.Models
{
    public class KematianZipMessage : IMessage
    {
        public byte[] ZipFile { get; set; }
    }
}