using Orion.Common.Messages;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using Orion.Server.Networking;
using System;
using System.Diagnostics;

namespace Orion.Server.Messages
{
    /// <summary>
    /// Handles ping messages for measuring network latency.
    /// </summary>
    public class PingHandler : MessageProcessorBase<int>
    {
        /// <summary>
        /// The client which is associated with this ping handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// Stores the timestamp when the ping request was sent.
        /// </summary>
        private long _pingRequestTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="PingHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        public PingHandler(Client client) : base(true)
        {
            _client = client;
        }

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is PingResponse;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case PingResponse pingResponse:
                    Execute(sender, pingResponse);
                    break;
            }
        }

        private void Execute(ISender client, PingResponse message)
        {
            double pingMs = 0;
            if (_pingRequestTimestamp != 0)
            {
                long freq = Stopwatch.Frequency;
                long now = Stopwatch.GetTimestamp();
                pingMs = ((now - _pingRequestTimestamp) * 1000.0) / freq;
                _pingRequestTimestamp = 0;
            }

            OnReport((int)Math.Round(pingMs));
        }

        /// <summary>
        /// Sends a ping request to the client.
        /// </summary>
        public void SendPing()
        {
            _pingRequestTimestamp = Stopwatch.GetTimestamp();
            _client.Send(new PingRequest());
        }
    }
}