using System;
using System.Drawing;
using System.Windows.Forms;

namespace Orion.Server.Utilities
{
    /// <summary>
    /// 图标 DPI 自动适配。
    ///
    /// 稳定做法（幂等、不叠加、不受框架自动缩放是否生效影响）：
    /// - 自己读当前控件的真实 DPI，算出目标像素尺寸 = 16 × DPI。
    /// - 把容器的 ImageScalingSize 直接设为该绝对像素值。
    /// - 每个图标用 ImageScaling = SizeToFit，让框架把图标缩放到 ImageScalingSize，
    ///   但绝不替换 item.Image 的 bitmap（替换才会导致多次调用叠加放大）。
    /// 因为我们只设置尺寸、不改图源，无论调用多少次结果都一致。
    /// </summary>
    public static class DpiImageScaling
    {
        // 96 DPI 下的逻辑图标尺寸。
        private const int LogicalMenuIconSize = 16;

        public static float GetScaleFactor(Control control)
        {
            if (control == null || control.IsDisposed)
            {
                return 1f;
            }

            try
            {
                using var g = control.CreateGraphics();
                return g.DpiX / 96f;
            }
            catch
            {
                return 1f;
            }
        }

        private static Size TargetIconSize(float scaleFactor)
        {
            int px = Math.Max(16, (int)Math.Round(LogicalMenuIconSize * scaleFactor));
            return new Size(px, px);
        }

        /// <summary>
        /// 让一个 ToolStrip（顶部菜单栏 / 状态栏 / 右键菜单 / 子菜单皆通用）按 DPI 显示图标。幂等。
        /// </summary>
        public static void ApplyToToolStrip(ToolStrip toolStrip, float scaleFactor)
        {
            if (toolStrip == null)
            {
                return;
            }

            Size target = TargetIconSize(scaleFactor);

            // 社区推荐序列：先关 AutoSize，设置绝对像素的 ImageScalingSize，再恢复 AutoSize，
            // 这样能覆盖设计器写死的值，且不与框架自动缩放累乘（绝对赋值而非累乘）。
            bool prevAutoSize = toolStrip.AutoSize;
            toolStrip.SuspendLayout();
            toolStrip.AutoSize = false;
            toolStrip.ImageScalingSize = target;

            foreach (ToolStripItem item in toolStrip.Items)
            {
                ApplyToToolStripItem(item, target);
            }

            toolStrip.AutoSize = prevAutoSize;
            toolStrip.ResumeLayout(true);
        }

        private static void ApplyToToolStripItem(ToolStripItem item, Size target)
        {
            if (item == null)
            {
                return;
            }

            // SizeToFit：让图标被约束到容器 ImageScalingSize；不替换 bitmap，保证幂等。
            item.ImageScaling = ToolStripItemImageScaling.SizeToFit;

            if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
            {
                dropDownItem.DropDown.ImageScalingSize = target;

                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                {
                    ApplyToToolStripItem(child, target);
                }
            }
        }

        // 缓存图标按钮的原始图像，保证每次都从原图缩放（幂等，不叠加）。
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Button, Image> _originalButtonImages =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Button, Image>();

        /// <summary>
        /// 让一个图标按钮（客户端预览下方的快捷按钮）按 DPI 缩放。
        /// 始终从缓存的原图缩放到目标尺寸并居中，幂等（多次调用不叠加）。
        /// </summary>
        public static void ApplyToIconButton(Button button, float scaleFactor)
        {
            if (button == null)
            {
                return;
            }

            if (!_originalButtonImages.TryGetValue(button, out Image original))
            {
                if (button.Image == null)
                {
                    return;
                }

                original = (Image)button.Image.Clone();
                _originalButtonImages.Add(button, original);
            }

            // 目标图标尺寸 = 原图逻辑尺寸 × DPI（原图快捷按钮图标为 16px）。
            int target = Math.Max(16, (int)Math.Round(16 * scaleFactor));
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.Image = ScaleImage(original, target);
        }

        /// <summary>
        /// 为「运行时会切换图标」的按钮设置图标并按 DPI 缩放。
        /// 与 <see cref="ApplyToIconButton"/> 不同：不缓存原图，每次都以传入的
        /// <paramref name="logicalImage"/> 为源缩放，因此适合切换状态时调用（如鼠标/键盘开关）。
        /// </summary>
        public static void SetScaledButtonIcon(Button button, Image logicalImage, float scaleFactor, int logicalSize = LogicalMenuIconSize)
        {
            if (button == null || logicalImage == null)
            {
                return;
            }

            int target = Math.Max(logicalSize, (int)Math.Round(logicalSize * scaleFactor));
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.Image = ScaleImage(logicalImage, target);
        }

        private static Image ScaleImage(Image source, int size)
        {
            return ScaleImage(source, new Size(size, size));
        }

        private static Image ScaleImage(Image source, Size size)
        {
            if (source.Width == size.Width && source.Height == size.Height)
            {
                return new Bitmap(source);
            }

            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(source, 0, 0, size.Width, size.Height);
            }

            return bmp;
        }

        // 缓存 PictureBox 的原始图像，保证每次都从原图缩放（幂等）。
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PictureBox, Image> _originalPictureBoxImages =
            new System.Runtime.CompilerServices.ConditionalWeakTable<PictureBox, Image>();

        /// <summary>
        /// 让一个小图标 PictureBox（如生成器里的 UAC 盾牌）按 DPI 缩放。
        /// 始终从缓存原图缩放到 逻辑尺寸 × DPI，幂等。
        /// </summary>
        public static void ApplyToIconPictureBox(PictureBox pictureBox, float scaleFactor, int logicalSize = LogicalMenuIconSize)
        {
            if (pictureBox == null)
            {
                return;
            }

            if (!_originalPictureBoxImages.TryGetValue(pictureBox, out Image original))
            {
                if (pictureBox.Image == null)
                {
                    return;
                }

                original = (Image)pictureBox.Image.Clone();
                _originalPictureBoxImages.Add(pictureBox, original);
            }

            int target = Math.Max(logicalSize, (int)Math.Round(logicalSize * scaleFactor));
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Size = new Size(target, target);
            pictureBox.Image = ScaleImage(original, target);
        }

        /// <summary>
        /// 为运行时动态添加的插件菜单项配置图标缩放。幂等。
        /// </summary>
        public static void ApplyToPluginMenuItem(ToolStripMenuItem item, float scaleFactor)
        {
            if (item == null)
            {
                return;
            }

            Size target = TargetIconSize(scaleFactor);
            item.ImageScaling = ToolStripItemImageScaling.SizeToFit;

            if (item.Owner != null)
            {
                item.Owner.ImageScalingSize = target;
            }
        }

        /// <summary>
        /// 通用入口：递归遍历整个窗体的控件树，自动适配所有带图标的控件。
        /// 覆盖：菜单栏 / 工具栏 / 状态栏 / 右键菜单及子菜单、ListView 的 ImageList。
        /// 幂等（只设尺寸/SizeToFit，不叠加缩放）。在窗体构造里 InitializeComponent 后调用一次即可。
        /// </summary>
        public static void ApplyToForm(Form form)
        {
            if (form == null)
            {
                return;
            }

            float scale = GetScaleFactor(form);
            WalkControls(form, scale);
        }

        private static void WalkControls(Control parent, float scale)
        {
            // 控件自带的右键菜单（ContextMenuStrip）。
            if (parent.ContextMenuStrip != null)
            {
                ApplyToToolStrip(parent.ContextMenuStrip, scale);
            }

            switch (parent)
            {
                case ToolStrip toolStrip:
                    ApplyToToolStrip(toolStrip, scale);
                    break;

                case ListView listView:
                    ApplyToListViewImageLists(listView, scale);
                    break;
            }

            foreach (Control child in parent.Controls)
            {
                WalkControls(child, scale);
            }
        }

        // 记录已缩放过的 ImageList，避免同一个 ImageList 被多个 ListView 引用时重复缩放（叠加）。
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ImageList, object> _scaledImageLists =
            new System.Runtime.CompilerServices.ConditionalWeakTable<ImageList, object>();

        private static void ApplyToListViewImageLists(ListView listView, float scale)
        {
            ScaleListViewImageList(listView.SmallImageList, scale);
            ScaleListViewImageList(listView.LargeImageList, scale);
            ScaleListViewImageList(listView.StateImageList, scale);
        }

        private static void ScaleListViewImageList(ImageList imageList, float scale)
        {
            if (imageList == null || Math.Abs(scale - 1f) < 0.01f)
            {
                return;
            }

            if (_scaledImageLists.TryGetValue(imageList, out _))
            {
                return; // 已缩放过，跳过（幂等）。
            }

            var targetSize = new Size(
                Math.Max(8, (int)Math.Round(imageList.ImageSize.Width * scale)),
                Math.Max(8, (int)Math.Round(imageList.ImageSize.Height * scale)));

            var scaled = new System.Collections.Generic.List<(string Key, Image Bitmap)>();
            for (int i = 0; i < imageList.Images.Count; i++)
            {
                string key = imageList.Images.Keys[i];
                scaled.Add((key, ScaleImage(imageList.Images[i], targetSize)));
            }

            imageList.Images.Clear();
            imageList.ImageSize = targetSize;
            imageList.ColorDepth = ColorDepth.Depth32Bit;

            foreach (var (key, bitmap) in scaled)
            {
                imageList.Images.Add(key, bitmap);
            }

            _scaledImageLists.Add(imageList, new object());
        }
    }
}
