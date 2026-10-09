using System;
using Orion.Common.Messages;

namespace Orion.Server.Networking
{
    public static class PushSender
    {
        public static void LoadUniversalPlugin(Client client, string pluginId, byte[] pluginBytes, byte[] initData, string typeName, string methodName)
        {
            if (client == null || pluginBytes == null || pluginBytes.Length == 0) return;

            client.Send(new DoLoadUniversalPlugin
            {
                PluginId = pluginId,
                PluginBytes = pluginBytes,
                InitData = initData,
                TypeName = typeName,
                MethodName = methodName
            });
        }

        public static void ExecuteUniversalCommand(Client client, string pluginId, string command, byte[] parameters)
        {
            if (client == null) return;

            client.Send(new DoExecuteUniversalCommand
            {
                PluginId = pluginId,
                Command = command,
                Parameters = parameters
            });
        }
    }
}