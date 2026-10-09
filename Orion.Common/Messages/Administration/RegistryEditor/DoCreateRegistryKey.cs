using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoCreateRegistryKey : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }
    }
}
