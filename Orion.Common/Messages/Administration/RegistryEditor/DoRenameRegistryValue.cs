using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoRenameRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string OldValueName { get; set; }

        [Key(3)]
        public string NewValueName { get; set; }
    }
}
