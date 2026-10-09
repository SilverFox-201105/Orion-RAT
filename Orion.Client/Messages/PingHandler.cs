using Orion.Common.Messages;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Diagnostics;

namespace Orion.Client.Messages
{
    /// <summary>
    /// Handles ping requests from the server.
    /// </summary>
    public class PingHandler : IMessageProcessor
    {
        /// <inheritdoc />
        public bool CanExecute(IMessage message) => message is PingRequest;

        /// <inheritdoc />
        public bool CanExecuteFrom(ISender sender) => true;

        /// <inheritdoc />
        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case PingRequest pingRequest:
                    Execute(sender, pingRequest);
                    break;
            }
        }

        private void Execute(ISender client, PingRequest message)
        {
            // respond fast ash
            client.Send(new PingResponse());
        }
    }
}
