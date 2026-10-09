using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages
{
    [MessagePackObject]
    public sealed class DoLoadUniversalPlugin : IMessage
    {
        [Key(1)]
        public string PluginId { get; set; }

        [Key(2)]
        public byte[] PluginBytes { get; set; }

        [Key(3)]
        public byte[] InitData { get; set; }

        [Key(4)]
        public string TypeName { get; set; }

        [Key(5)]
        public string MethodName { get; set; }
    }
}

