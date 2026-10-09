using System;
using System.Collections.Generic;
using Orion.Common.Messages;
using Orion.Client.Networking;

namespace Orion.Client.Plugins
{
    public interface IClientPlugin
    {
        string Name { get; }
        Version Version { get; }
        void Register(List<IMessageProcessor> processors, OrionClient client);
    }
}
