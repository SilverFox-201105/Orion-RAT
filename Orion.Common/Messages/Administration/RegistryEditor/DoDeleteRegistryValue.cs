using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public class DoDeleteRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string ValueName { get; set; }
    }
}
