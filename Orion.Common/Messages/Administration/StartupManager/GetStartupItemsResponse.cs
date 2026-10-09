using MessagePack;
using Orion.Common.Messages.Other;
using Orion.Common.Models;
using System.Collections.Generic;

namespace Orion.Common.Messages.Administration.StartupManager
{
    [MessagePackObject]
    public class GetStartupItemsResponse : IMessage
    {
        [Key(1)]
        public List<StartupItem> StartupItems { get; set; }
    }
}
