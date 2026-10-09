using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Orion.Server.Networking;
using Orion.Server.Plugins;

namespace MyServerPlugin
{
    /// <summary>
    /// 最小可用的服务端插件模板。
    /// 契约：public + 非抽象 + 公共无参构造函数 + 实现 IServerPlugin。
    /// </summary>
    public sealed class HelloWorldPlugin : IServerPlugin, IDisposable
    {
        private IServerContext _context;

        // ---------- IServerPlugin ----------

        public string Name => "HelloWorld 插件";
        public Version Version => new Version(1, 0, 0, 0);
        public string Description => "服务端插件模板：加菜单、读客户端列表、发消息。";
        public string Type => "Template";

        /// <summary>
        /// 是否随客户端上线自动下发内嵌的 ClientModule。默认 false。
        /// 详见教程第 6 章。
        /// </summary>
        public bool AutoLoadToClients => false;

        public void Initialize(IServerContext context)
        {
            _context = context;
            _context.Log($"[{Name}] v{Version} 已加载");

            // 1) 顶层菜单项
            _context.AddClientContextMenuItem("Hello World", OnHelloWorld);

            // 2) 一级分组下的菜单项
            _context.AddClientContextMenuItem("插件模板", "统计客户端数量", OnCountClients);

            // 3) 多级路径菜单（分隔符支持 \ 和 /）
            _context.AddClientContextMenuItemPath(
                @"插件模板\子菜单\列出所有客户端",
                "列出全部",
                OnListClients);
        }

        // ---------- 菜单回调 ----------
        // 参数 clients = 当前在主界面选中的客户端列表

        private void OnHelloWorld(IReadOnlyList<Client> clients)
        {
            _context.Log($"HelloWorld：当前选中 {clients?.Count ?? 0} 个客户端");

            MessageBox.Show(
                $"你好，Orion 插件世界！\n\n当前选中客户端：{clients?.Count ?? 0}",
                Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnCountClients(IReadOnlyList<Client> clients)
        {
            var all = _context.Server?.ConnectedClients;

            MessageBox.Show(
                $"已连接：{all?.Length ?? 0}\n本次选中：{clients?.Count ?? 0}",
                Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnListClients(IReadOnlyList<Client> clients)
        {
            var all = _context.Server?.ConnectedClients;
            if (all == null || all.Length == 0)
            {
                MessageBox.Show("当前没有客户端在线。", Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var c in all)
            {
                sb.AppendLine($"{c.EndPoint} | 已连接={c.Connected}");
            }

            MessageBox.Show(sb.ToString(), "客户端列表",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- 资源清理 ----------
        // PluginManager.ReloadPlugins() 会对所有 IDisposable 插件调 Dispose()。
        // 如果你注册了 UniversalPluginResponseHandler 的回调，务必在这里反注册，
        // 否则热重载后会重复触发（静态字典不会自动清）。

        public void Dispose()
        {
            _context = null;
        }
    }
}
