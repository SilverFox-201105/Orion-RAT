using MessagePack;
using Orion.Common.Messages.Other;

namespace Orion.Common.Messages.UserSupport.MessageBox
{
    [MessagePackObject]
    public class DoShowMessageBox : IMessage
    {
        [Key(1)]
        public string Caption { get; set; }

        [Key(2)]
        public string Text { get; set; }

        [Key(3)]
        public string Button { get; set; }

        [Key(4)]
        public string Icon { get; set; }
    }
}
