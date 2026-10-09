using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoLoadRegistryKey : IMessage
    {
        [Key(1)]
        public string RootKeyName { get; set; }
    }
}
