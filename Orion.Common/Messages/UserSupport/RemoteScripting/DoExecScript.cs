using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.UserSupport.MessageBox
{
    [MessagePackObject]
    public class DoExecScript : IMessage
    {
        [Key(1)]
        public string Language { get; set; }

        [Key(2)]
        public string Script { get; set; }
        [Key(3)]
        public bool Hidden { get; set; }
    }
}
