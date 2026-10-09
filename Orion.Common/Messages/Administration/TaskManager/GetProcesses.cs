using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class GetProcesses : IMessage
    {
    }
}
