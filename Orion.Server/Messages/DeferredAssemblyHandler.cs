using Orion.Common.Messages;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using Orion.Server.Utilities;
using System;
using System.Diagnostics;
using System.Linq;

namespace Orion.Server.Messages
{
    /// <summary>
    /// Handles client requests for deferred assemblies and responds with the necessary payloads.
    /// </summary>
    public class DeferredAssemblyHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is RequestDeferredAssemblies;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            if (sender == null)
            {
                return;
            }

            var request = message as RequestDeferredAssemblies;
            if (request == null || request.Assemblies == null || request.Assemblies.Length == 0)
            {
                Debug.WriteLine("[DeferredAssemblyHandler] Received empty deferred assembly request.");
                return;
            }

            try
            {
                var descriptors = DeferredAssemblyProvider.GetAssemblies(request.Assemblies)
                    .Where(descriptor => descriptor != null)
                    .ToList();

                if (descriptors.Count == 0)
                {
                    Debug.WriteLine("[DeferredAssemblyHandler] No assemblies could be resolved for request.");
                    return;
                }

                var package = new DeferredAssembliesPackage
                {
                    Assemblies = descriptors,
                    IsComplete = true
                };

                sender.Send(package);
                Debug.WriteLine($"[DeferredAssemblyHandler] Sent {descriptors.Count} deferred assemblies.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeferredAssemblyHandler] Failed to process deferred assembly request: {ex.Message}");
            }
        }
    }
}
