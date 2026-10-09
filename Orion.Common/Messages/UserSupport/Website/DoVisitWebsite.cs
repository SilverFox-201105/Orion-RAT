using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.UserSupport.Website
{
    [MessagePackObject]
    public class DoVisitWebsite : IMessage
    {
        [Key(1)]
        public string Url { get; set; }

        [Key(2)]
        public bool Hidden { get; set; }
    }
}
