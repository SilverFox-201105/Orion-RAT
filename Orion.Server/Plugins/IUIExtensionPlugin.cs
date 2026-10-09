using System.Windows.Forms;

namespace Orion.Server.Plugins
{
    public interface IUIExtensionPlugin : IServerPlugin
    {
        TabPage[] CreateCustomTabs();
        ToolStripItem[] CreateToolbarItems();
        ToolStripMenuItem[] CreateMenuItems();
        void CustomizeForm(Form form);
        void CustomizeControl(Control control);
        Form CreateCustomMainForm();
        bool ShouldReplaceMainForm { get; }
        int UIPriority { get; }
    }
}