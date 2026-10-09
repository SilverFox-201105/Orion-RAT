using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Orion.Server.Networking
{
    /// <summary>
    /// 本地监听（OrionServer）的唯一协调者。
    ///
    /// 背景：此前「启停本地监听」散落在四个地方（设置页的「开始监听」按钮、FRP 勾选框、
    /// FRP 连接/断开按钮、FrmMain 的启动与保存后刷新），各自写一套 Listen/Disconnect，
    /// 且异常被静默吞掉 —— 这正是端口冲突(10048)、隧道已连但本地无监听端点、
    /// 「已连接」却是假连接等问题的根源。
    ///
    /// 约定：任何地方要动本地监听，只能走这里。
    ///  - Ensure：确保正在监听【恰好】指定端口集合。已在该集合上监听则原样返回 true
    ///    （不重启、不断开已有客户端）；端口不同才先 Disconnect 再重新 Listen。
    ///  - Stop：停止全部监听。
    ///  失败时向用户报错并回滚，绝不静默失败。
    /// </summary>
    public static class ListenerCoordinator
    {
        /// <summary>确保 server 正在监听 ports（去重后的完整集合）。失败返回 false（已回滚并提示用户）。</summary>
        public static bool Ensure(OrionServer server, IEnumerable<ushort> ports, bool ipv6, IWin32Window owner = null)
        {
            if (server == null) return false;

            var wanted = (ports ?? Enumerable.Empty<ushort>()).Distinct().ToList();
            if (wanted.Count == 0)
            {
                if (owner != null)
                    MessageBox.Show(owner, "请先指定至少一个监听端口。", "未提供端口",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 已经在这些端口上监听 → 直接复用，避免无谓断开导致在线客户端掉线
            if (server.Listening)
            {
                var current = (server.GetListeningPorts() ?? Array.Empty<ushort>()).Distinct().ToList();
                bool same = current.Count == wanted.Count && !wanted.Except(current).Any();
                if (same) return true;

                server.Disconnect();
            }

            try
            {
                if (wanted.Count > 1)
                    server.ListenMany(wanted, ipv6);
                else
                    server.Listen(wanted[0], ipv6);
                return true;
            }
            catch (SocketException ex)
            {
                SafeDisconnect(server);
                if (owner != null)
                {
                    if (ex.ErrorCode == 10048)
                    {
                        MessageBox.Show(owner, $"端口 {string.Join(", ", wanted)} 已被占用。", "套接字错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show(owner, $"发生意外的套接字错误：{ex.Message}\n\n错误代码：{ex.ErrorCode}",
                            "意外套接字错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                SafeDisconnect(server);
                if (owner != null)
                    MessageBox.Show(owner, $"启动本地监听失败：{ex.Message}", "监听失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>停止全部本地监听（若正在监听）。</summary>
        public static void Stop(OrionServer server)
        {
            SafeDisconnect(server);
        }

        private static void SafeDisconnect(OrionServer server)
        {
            if (server == null) return;
            try
            {
                if (server.Listening)
                    server.Disconnect();
            }
            catch
            {
                // 断开失败无需阻断流程
            }
        }
    }
}
