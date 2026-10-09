using Orion.Server.Forms.DarkMode;
using DarkModeForms;
using Orion.Server.Utilities;
using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmAbout : Form
    {
        // 官方社区入口（公开仓库仅发布源码，不提供预编译二进制）
        private readonly string _telegramUrl = @"https://t.me/+h7gwb2c5NaM0M2M5";

        // 与窗体客户区等尺寸的背景缓存（已按 BackgroundImageLayout 完整渲染），
        // 供半透明面板按自身位置截取，保证与窗体背景严丝合缝。
        private Bitmap _backgroundCache;

        // 贡献者信息
        private const string ContributorsMessage = """
- 现任开发者(就我一个)
- [SilverFox](https://t.me/Silver_Fox_201105) - 产品运营与持续维护

- 原项目开发者(Pulsar)
- [KingKDot](https://github.com/KingKDot) - 主要开发者
- [TheChosenSkywalker](https://github.com/thechosenskywalker) - 主要开发者
- [MaxXor](https://github.com/MaxXor) - Pulsar RAT 原始作者
- [Twobit](https://github.com/officialtwobit) - 多功能模块开发
- [fedx](https://github.com/fedx-988) - README 设计 & Discord RPC
- [Java](https://github.com/JavaRenamed-dev) - 功能扩展
- [Rishie](https://github.com/rishieissocool) - 信息收集选项
- [Poli](https://github.com/paulmaster59) - Discord 服务器 & Orion 定制加密器
- [User76](https://github.com/user76-real) - 网络性能优化
""";

        public FrmAbout()
        {
            InitializeComponent();

            // 方案 A: 使用透明 Panel + Label 替换 RichTextBox 实现完美透明滚动
            ReplaceWithTransparentScrollLabel(rtxtContent, Properties.Resources.License);
            ReplaceWithTransparentScrollLabel(cntTxtContent, ContributorsMessage);

            // 关键修复：直接修改窗体的背景图，使其变淡
            if (this.BackgroundImage != null)
            {
                this.BackgroundImage = SetImageOpacity(this.BackgroundImage, 0.2f); // 壁纸只作淡淡纹理，避免抢走文字对比度
            }

            // 深底色：比按钮等控件（DarkModeCS 的深灰）更深一档，整体更透、更能衬托壁纸
            this.BackColor = AboutPalette.Surface;
            this.Opacity = 0.94D;
            DarkModeCS.ExcludeFromProcessing(this);

            foreach (Control control in this.Controls)
            {
                // Label, LinkLabel, PictureBox, Panel 保持透明以显示背景图
                if (control is Label || control is LinkLabel || control is PictureBox || control is Panel)
                {
                    control.BackColor = Color.Transparent;
                    DarkModeCS.ExcludeFromProcessing(control);
                    ApplyAboutPalette(control);

                    if (control is Panel p)
                    {
                        foreach (Control child in p.Controls)
                        {
                            child.BackColor = Color.Transparent;
                            DarkModeCS.ExcludeFromProcessing(child);
                            ApplyAboutPalette(child);
                        }
                    }
                }
                // 按钮不进行排除，让其遵循 DarkModeManager 的统一风格（深灰色背景）
            }

            DarkModeManager.ApplyDarkMode(this);
            DpiImageScaling.ApplyToIconPictureBox(picIcon, DpiImageScaling.GetScaleFactor(this), 64);

            this.FormClosed += (s, e) =>
            {
                _backgroundCache?.Dispose();
                _backgroundCache = null;
            };

            lblVersion.Text = ServerVersion.Display;
            lblTitle.Text = ServerVersion.AppName;
            Text = $"{ServerVersion.AppName} - 关于";

            lnkTelegram.Links.Add(new LinkLabel.Link { LinkData = _telegramUrl });
        }

        /// <summary>
        /// Label / LinkLabel 都被排除在深色模式之外，前景色不会被自动改亮，这里统一改成亮色。
        /// </summary>
        private static void ApplyAboutPalette(Control control)
        {
            // LinkLabel 继承自 Label，必须先判断 LinkLabel
            if (control is LinkLabel ll)
            {
                ll.ForeColor = AboutPalette.Text;
                ll.LinkColor = AboutPalette.Link;
                ll.ActiveLinkColor = AboutPalette.LinkActive;
                ll.VisitedLinkColor = AboutPalette.Link;
            }
            else if (control is Label lb)
            {
                lb.ForeColor = AboutPalette.Text;
            }
        }

        private void ReplaceWithTransparentScrollLabel(RichTextBox rtb, string text)
        {
            // 创建透明容器 Panel
            TransparentPanel panel = new TransparentPanel
            {
                Location = rtb.Location,
                Size = rtb.Size,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Name = "pnl_" + rtb.Name
            };

            // 使用 LinkLabel 支持 Markdown 链接
            LinkLabel label = new LinkLabel
            {
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                LinkColor = Color.FromArgb(0, 174, 219), // 亮蓝色，更像现代 UI 链接
                ActiveLinkColor = Color.FromArgb(0, 120, 215),
                VisitedLinkColor = Color.FromArgb(0, 174, 219),
                LinkBehavior = LinkBehavior.HoverUnderline,
                AutoSize = true,
                MaximumSize = new Size(rtb.Width - 25, 0),
                Location = new Point(0, 0),
                Font = new Font(rtb.Font.FontFamily, 8.5f, FontStyle.Regular),
                Name = "lbl_" + rtb.Name,
                UseCompatibleTextRendering = true // 提升布局和链接命中测试的精确度
            };

            // 解析 Markdown 链接 [text](url)
            string pattern = @"\[(.*?)\]\((.*?)\)";
            var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
            
            string plainText = text;
            int offset = 0;
            
            // 先处理文本内容
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string linkText = match.Groups[1].Value;
                int start = match.Index - offset;
                plainText = plainText.Remove(start, match.Length).Insert(start, linkText);
                offset += (match.Length - linkText.Length);
            }

            // 先设置 Text，这样 Links.Add 才能基于最终的字符索引工作
            label.Text = plainText;
            label.Links.Clear();

            // 重新遍历添加链接
            offset = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string linkText = match.Groups[1].Value;
                string url = match.Groups[2].Value;
                int start = match.Index - offset;
                label.Links.Add(start, linkText.Length, url);
                offset += (match.Length - linkText.Length);
            }

            label.LinkClicked += (s, e) =>
            {
                if (e.Link.LinkData is string target)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                    }
                    catch { }
                }
            };

            panel.Controls.Add(label);
            this.Controls.Add(panel);

            // 移除原始的 RichTextBox
            this.Controls.Remove(rtb);
            rtb.Dispose();
        }

        private void lnkTelegram_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(lnkTelegram, e);
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private static void OpenLink(LinkLabel label, LinkLabelLinkClickedEventArgs e)
        {
            if (label == null)
                return;

            label.LinkVisited = true;

            if (e.Link?.LinkData is string target)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"无法打开链接。\n\n{ex.Message}",
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// 取得与窗体客户区等尺寸的背景缓存位图。
        /// 这张图用与 WinForms 完全相同的 BackgroundImageLayout 规则渲染，
        /// 因此半透明面板从它身上按自身坐标截取时，不会受平铺(Tile)、DPI 缩放、图片越界影响。
        /// </summary>
        internal Bitmap GetBackgroundCache(bool force = false)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            if (w <= 0 || h <= 0)
                return null;

            if (!force && _backgroundCache != null &&
                _backgroundCache.Width == w && _backgroundCache.Height == h)
            {
                return _backgroundCache;
            }

            _backgroundCache?.Dispose();
            _backgroundCache = null;

            if (BackgroundImage == null)
                return null;

            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (SolidBrush brush = new SolidBrush(BackColor))
                {
                    g.FillRectangle(brush, 0, 0, w, h);
                }

                DrawBackgroundLayout(g, BackgroundImage, BackgroundImageLayout, new Rectangle(0, 0, w, h));
            }

            _backgroundCache = bmp;
            return _backgroundCache;
        }

        /// <summary>
        /// 按 ImageLayout 规则把背景图绘制到指定矩形，行为与 WinForms 内部绘制保持一致。
        /// </summary>
        private static void DrawBackgroundLayout(Graphics g, Image image, ImageLayout layout, Rectangle bounds)
        {
            if (image == null)
                return;

            switch (layout)
            {
                case ImageLayout.None:
                    g.DrawImage(image, bounds.X, bounds.Y, image.Width, image.Height);
                    break;

                case ImageLayout.Tile:
                    using (TextureBrush brush = new TextureBrush(image, WrapMode.Tile))
                    {
                        g.FillRectangle(brush, bounds);
                    }
                    break;

                case ImageLayout.Center:
                    g.DrawImageUnscaled(
                        image,
                        bounds.X + (bounds.Width - image.Width) / 2,
                        bounds.Y + (bounds.Height - image.Height) / 2);
                    break;

                case ImageLayout.Stretch:
                    g.DrawImage(image, bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                    break;

                case ImageLayout.Zoom:
                default:
                {
                    float scale = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
                    int iw = Math.Max(1, (int)Math.Round(image.Width * scale));
                    int ih = Math.Max(1, (int)Math.Round(image.Height * scale));
                    g.DrawImage(
                        image,
                        new Rectangle(bounds.X + (bounds.Width - iw) / 2, bounds.Y + (bounds.Height - ih) / 2, iw, ih),
                        0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                    break;
                }
            }
        }

        // 设置图片透明度
        private Image SetImageOpacity(Image image, float opacity)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        // 设置图片从上到下的透明度渐变
        private Image SetOpacityGradient(Image image)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            Bitmap src = new Bitmap(image);

            float startOpacity = 0.15f; // 顶部 15%
            float endOpacity = 0.01f;   // 底部 1%

            for (int y = 0; y < bmp.Height; y++)
            {
                float opacity = startOpacity +
                                (endOpacity - startOpacity) *
                                ((float)y / (bmp.Height - 1));

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = src.GetPixel(x, y);
                    Color newPixel = Color.FromArgb(
                        (int)(opacity * 255),
                        pixel.R, pixel.G, pixel.B
                    );
                    bmp.SetPixel(x, y, newPixel);
                }
            }

            return bmp;
        }

        private void FrmAbout_Load(object sender, EventArgs e)
        {
            picIcon.Cursor = Cursors.Hand;

            // DPI 缩放与深色模式都已定稿，此时重建背景缓存（force）
            GetBackgroundCache(force: true);
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            GetBackgroundCache(force: true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string disclaimer =
                "本软件以「源代码可见」方式发布，仅供学习与研究目的。\n\n" +
                "许可摘要（完整条款见 LICENSE.md）：\n" +
                "· 允许阅读、学习、审计，并为个人非商业目的在本地编译运行\n" +
                "· 禁止任何商业用途（销售、SaaS、付费服务、企业内部生产运营）\n" +
                "· 禁止二次开发与再分发（fork、镜像、修改版发布）\n\n" +
                "未经授权在非本人拥有或未明确允许的计算机上使用属于违法行为。\n\n" +
                "使用本软件即表示你需对自己的行为承担全部责任。";

            MessageBox.Show(
                disclaimer,
                "法律免责声明",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private void picIcon_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/BruceLi20110501/Orion_RAT",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开链接：" + ex.Message);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/quasar/Quasar",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开链接：" + ex.Message);
            }
        }

        private void cntTxtContent_TextChanged(object sender, EventArgs e)
        {
        }

        private void cntTxtContent_TextChanged_1(object sender, EventArgs e)
        {

        }
    }

    // 优化后的透明 Panel，解决黑色背景和滑动抽搐
    public class TransparentPanel : Panel
    {
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_SETREDRAW = 0x000B;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        public TransparentPanel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                     ControlStyles.UserPaint | 
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.Transparent;
        }

        // 终极杀招：启用复合绘制，从根本上解决所有子控件移动导致的撕裂和闪烁
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        // 拦截底层窗口消息，防止系统默认的 BitBlt 导致图像撕裂
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                // 停止系统绘制
                SendMessage(this.Handle, WM_SETREDRAW, false, 0);
                
                base.WndProc(ref m);
                
                // 恢复系统绘制并强制刷新
                SendMessage(this.Handle, WM_SETREDRAW, true, 0);
                this.Invalidate(true);
                return;
            }
            
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 不能用「原图 + 自身坐标」截取：BackgroundImageLayout 为 Tile 时窗体画的是平铺结果，
            // DPI 缩放后源矩形还可能超出图片边界，GDI+ 会做非预期缩放 → 背景扭曲。
            // 改为从窗体预渲染好的整屏背景缓存上按自身位置 1:1 截取。
            FrmAbout owner = null;
            for (Control p = this.Parent; p != null; p = p.Parent)
            {
                owner = p as FrmAbout;
                if (owner != null)
                    break;
            }

            Bitmap cache = owner?.GetBackgroundCache();
            if (cache != null && this.IsHandleCreated)
            {
                // 当前 Graphics 可能已被平移（透明子控件回绘父背景等场景），
                // 反推目标矩形起点，使缓存中 (loc.X, loc.Y) 恰好落在面板客户区原点。
                float ox = e.Graphics.Transform.OffsetX;
                float oy = e.Graphics.Transform.OffsetY;
                Point loc = owner.PointToClient(this.PointToScreen(Point.Empty));

                Rectangle src = new Rectangle(loc.X, loc.Y, this.Width, this.Height);
                Rectangle dest = new Rectangle((int)Math.Round(-ox), (int)Math.Round(-oy), this.Width, this.Height);

                // 越界裁剪，保证 src/dest 始终同尺寸（1:1，不产生缩放形变）
                if (src.X < 0)
                {
                    dest.X -= src.X;
                    dest.Width += src.X;
                    src.Width += src.X;
                    src.X = 0;
                }
                if (src.Y < 0)
                {
                    dest.Y -= src.Y;
                    dest.Height += src.Y;
                    src.Height += src.Y;
                    src.Y = 0;
                }
                if (src.Right > cache.Width)
                {
                    int over = src.Right - cache.Width;
                    src.Width -= over;
                    dest.Width -= over;
                }
                if (src.Bottom > cache.Height)
                {
                    int over = src.Bottom - cache.Height;
                    src.Height -= over;
                    dest.Height -= over;
                }

                if (src.Width > 0 && src.Height > 0 && dest.Width > 0 && dest.Height > 0)
                {
                    var oldInterp = e.Graphics.InterpolationMode;
                    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    e.Graphics.DrawImage(cache, dest, src, GraphicsUnit.Pixel);
                    e.Graphics.InterpolationMode = oldInterp;
                    return;
                }
            }

            base.OnPaintBackground(e);
        }
    }

    // About 窗体配色：深底 + 亮字，保证壁纸淡化后文字仍有足够对比度。
    // 注意：此类必须放在 FrmAbout 之后，否则 VS 设计器会因"第一个类不是可设计类"而拒绝加载。
    internal static class AboutPalette
    {
        // 窗体底色比按钮等控件（DarkModeCS 处理出的深灰）再深一档
        public static readonly Color Surface = Color.FromArgb(12, 14, 17);
        public static readonly Color Text = Color.FromArgb(226, 232, 240);
        public static readonly Color Link = Color.FromArgb(96, 190, 255);
        public static readonly Color LinkActive = Color.FromArgb(0, 120, 215);
    }
}
