using Orion.Common.Messages.Other;

namespace Orion.Common.Networking
{
    public interface ISender
    {
        void Send<T>(T message) where T : IMessage;
        void Disconnect();
    }
}
