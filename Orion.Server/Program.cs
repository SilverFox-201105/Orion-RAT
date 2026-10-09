using Orion.Server.Forms;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Orion.Server
{
    static class Program
    {
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveChartingAssemblies;
        }

        /// <summary>
        /// LiveCharts WPF builds reference SkiaSharp 2.88 while NuGet resolves SkiaSharp 3.x.
        /// Load loose DLLs from the output directory when Costura is not embedding them.
        /// </summary>
        private static Assembly? ResolveChartingAssemblies(object? sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            switch (requested.Name)
            {
                case "SkiaSharp":
                case "SkiaSharp.HarfBuzz":
                case "SkiaSharp.Views.Desktop.Common":
                case "SkiaSharp.Views.WPF":
                case "LiveChartsCore":
                case "LiveChartsCore.SkiaSharpView":
                case "LiveChartsCore.SkiaSharpView.WPF":
                case "HarfBuzzSharp":
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, requested.Name + ".dll");
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }

                    break;
                }
            }

            return null;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // WPF controls hosted via ElementHost (client list, LiveCharts views) require a WPF
            // Application in the process. Without Application.Current, DataGrid grouping renders
            // group headers but fails to realize the rows inside groups (expand/collapse broken).
            if (System.Windows.Application.Current == null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };
                Application.ApplicationExit += (_, _) => System.Windows.Application.Current?.Shutdown();
            }

            using (FrmMain mainForm = new FrmMain())
            {
                var customMainForm = Plugins.UIExtensionManager.GetCustomMainForm();
                if (customMainForm != null)
                {
                    mainForm.Hide();
                    customMainForm.Show();
                    
                    customMainForm.FormClosed += (s, e) => 
                    {
                        mainForm.Close();
                    };
                    
                    customMainForm.Disposed += (s, e) => 
                    {
                        if (!mainForm.IsDisposed)
                        {
                            mainForm.Close();
                        }
                    };
                    
                    Application.Run(customMainForm);
                }
                else
                {
                    Application.Run(mainForm);
                }
            }
        }
    }
}