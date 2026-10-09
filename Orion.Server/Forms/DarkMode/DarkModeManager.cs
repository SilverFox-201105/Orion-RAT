using DarkModeForms;
using Orion.Server.Utilities;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Orion.Server.Forms.DarkMode
{
    public class DarkModeManager
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int DWMWA_BORDER_COLOR = 34;

        public static void ApplyDarkMode(Form form)
        {
            DarkModeCS _ = new DarkModeCS(form)
            {
                ColorMode = DarkModeCS.DisplayMode.DarkMode,
                ColorizeIcons = false,
            };

            SetBorderColor(form, Color.DimGray);
            DpiImageScaling.ApplyToForm(form);
        }

        private static void SetBorderColor(Form form, Color color)
        {
            int colorValue = color.R | (color.G << 8) | (color.B << 16);
            DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref colorValue, sizeof(int));
        }
    }
}
