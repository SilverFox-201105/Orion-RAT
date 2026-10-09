using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;

namespace Orion.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class GetCreateRegistryKeyResponse : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }

        [Key(2)]
        public RegSeekerMatch Match { get; set; }

        [Key(3)]
        public bool IsError { get; set; }

        [Key(4)]
        public string ErrorMsg { get; set; }
    }
}
