using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetAvailableWebcamsResponse : IMessage
    {
        [Key(1)]
        public string[] Webcams { get; set; }
    }
}
