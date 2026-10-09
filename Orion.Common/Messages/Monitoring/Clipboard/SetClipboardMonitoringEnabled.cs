using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Monitoring.Clipboard
{
    /// <summary>
    /// Message to enable or disable clipboard monitoring on the client.
    /// </summary>
    [MessagePackObject]
    public class SetClipboardMonitoringEnabled : IMessage
    {
        /// <summary>
        /// Gets or sets whether clipboard monitoring should be enabled.
        /// </summary>
        [Key(1)]
        public bool Enabled { get; set; }
    }
}
