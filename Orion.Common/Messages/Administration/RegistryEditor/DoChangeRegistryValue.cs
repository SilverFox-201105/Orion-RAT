using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoChangeRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public RegValueData Value { get; set; }
    }
}
