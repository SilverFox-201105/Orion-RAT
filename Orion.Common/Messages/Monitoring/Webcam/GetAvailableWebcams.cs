using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetAvailableWebcams : IMessage
    {
    }
}
