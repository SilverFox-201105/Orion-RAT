using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Preview
{
    [MessagePackObject]
    public class GetPreviewImage : IMessage
    {
        [Key(2)]
        public int Quality { get; set; }

        [Key(3)]
        public int DisplayIndex { get; set; }
    }
}
