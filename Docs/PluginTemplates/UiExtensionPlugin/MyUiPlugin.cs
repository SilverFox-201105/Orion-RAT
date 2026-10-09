using System;
using System.Drawing;
using System.Windows.Forms;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Plugins;

namespace MyUiPlugin
{
    /// <summary>
    /// UI 扩展插件模板：往主界面加 Tab 页签、工具栏按钮、菜单项。
    ///
    /// 重要：
    /// 1. 所有返回值都必须非 null，不需要就返回 Array.Empty<T>()。
    /// 2. 每次热重载这些方法都会被调用一次，不要在里面持有外部可变状态。
    /// 3. CustomizeControl 会对主窗体的【每一个】控件（含嵌套）调用，逻辑要极轻量。
    /// </summary>
    public sealed class MyUiPlugin : IUIExtensionPlugin
    {
        private IServerContext _context;

        // ---------- IServerPlugin ----------

        public string Name => "我的 UI 插件";
        public Version Version => new Version(1, 0, 0, 0);
        public string Description => "UI 扩展模板：自定义页签 + 工具栏 + 菜单。";
        public string Type => "UI";

        public void Initialize(IServerContext context)
        {
            _context = context;
            _context.Log($"[{Name}] 已加载");
        }

        // ---------- IUIExtensionPlugin ----------

        /// <summary>数字越小越靠前（列表按升序排序）。</summary>
        public int UIPriority => 100;

        /// <summary>
        /// 是否用 CreateCustomMainForm() 的返回值替换 Orion 主窗体。
        /// 除非你非常确定，否则保持 false。
        /// </summary>
        public bool ShouldReplaceMainForm => false;

        public TabPage[] CreateCustomTabs()
        {
            var page = new TabPage("我的面板")
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Padding = new Padding(8)
            };

            var btn = new Button
            {
                Text = "点我",
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            btn.Click += (s, e) =>
            {
                MessageBox.Show("插件页签里的按钮被点击", Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var txt = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(37, 37, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = "这是插件注入的页签。\r\n\r\n每次插件热重载都会重新创建本页签。"
            };

            page.Controls.Add(txt);   // Fill 的控件先加，后加的在上层
            page.Controls.Add(btn);

            return new[] { page };
        }

        public ToolStripItem[] CreateToolbarItems()
        {
            var btn = new ToolStripButton("插件按钮")
            {
                ForeColor = Color.White,
                ToolTipText = "来自 MyUiPlugin"
            };
            btn.Click += (s, e) =>
            {
                MessageBox.Show("插件工具栏按钮被点击", Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            return new ToolStripItem[] { btn };
        }

        public ToolStripMenuItem[] CreateMenuItems()
        {
            // 这些项会被加到主菜单里名为 toolsToolStripMenuItem 的菜单下
            var item = new ToolStripMenuItem("我的插件功能")
            {
                ForeColor = Color.White
            };
            item.Click += (s, e) =>
            {
                ShowOwnWindow();
            };

            return new[] { item };
        }

        public void CustomizeForm(Form form)
        {
            // 对主窗体做整体调整。示例：不改任何东西。
            // form.Text += "  [MyUiPlugin]";
        }

        public void CustomizeControl(Control control)
        {
            // ⚠️ 这个方法会对主窗体的每一个控件（递归）调用一次。
            // 一定要先做类型/Name 判断，逻辑要极轻量。
            //
            // if (control is ListView lv && lv.Name == "lstClients")
            // {
            //     lv.BackColor = Color.FromArgb(37, 37, 40);
            // }
        }

        public Form CreateCustomMainForm()
        {
            // ShouldReplaceMainForm == false 时不会被调用
            return null;
        }

        // ---------- 私有 ----------

        private void ShowOwnWindow()
        {
            var form = new Form
            {
                Text = Name,
                ClientSize = new Size(480, 320),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 插件自己 new 出来的 Form 需要手动套深色主题
            DarkModeManager.ApplyDarkMode(form);

            var label = new Label
            {
                Text = "这是插件自己的窗口。\r\nDarkModeManager.ApplyDarkMode(this) 会递归染色所有子控件。",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            form.Controls.Add(label);

            form.ShowDialog();
        }
    }
}
