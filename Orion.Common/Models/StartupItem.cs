using MessagePack;
using Orion.Common.Enums;

namespace Orion.Common.Models
{
    [MessagePackObject]
    public class StartupItem
    {
        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public string Path { get; set; }

        [Key(3)]
        public StartupType Type { get; set; }
    }
}
