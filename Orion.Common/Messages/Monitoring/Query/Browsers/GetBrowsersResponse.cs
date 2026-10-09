using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models.Query.Browsers;
using System.Collections.Generic;

namespace Orion.Common.Messages.Monitoring.Query.Browsers
{
    [MessagePackObject]
    public class GetBrowsersResponse : IMessage
    {
        [Key(1)]
        public List<QueryBrowsers> QueryBrowsers { get; set; }
    }
}
