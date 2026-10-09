using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Orion.Server.Plugins
{
    public static class UIExtensionManager
    {
        private static readonly List<IUIExtensionPlugin> _uiPlugins = new List<IUIExtensionPlugin>();
        private static Form _customMainForm;
        
        public static void RegisterUIExtension(IUIExtensionPlugin plugin)
        {
            if (plugin != null && !_uiPlugins.Contains(plugin))
            {
                _uiPlugins.Add(plugin);
                _uiPlugins.Sort((a, b) => a.UIPriority.CompareTo(b.UIPriority));
            }
        }
        
        public static void UnregisterUIExtension(IUIExtensionPlugin plugin)
        {
            _uiPlugins.Remove(plugin);
        }
        
        public static Form GetCustomMainForm()
        {
            if (_customMainForm != null)
                return _customMainForm;
                
            var replacementPlugin = _uiPlugins
                .Where(p => p.ShouldReplaceMainForm)
                .OrderByDescending(p => p.UIPriority)
                .FirstOrDefault();
                
            if (replacementPlugin != null)
            {
                try
                {
                    _customMainForm = replacementPlugin.CreateCustomMainForm();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating custom main form: {ex.Message}");
                }
            }
            
            return _customMainForm;
        }
        
        public static void ApplyFormCustomizations(Form form)
        {
            if (form == null) return;
            
            foreach (var plugin in _uiPlugins)
            {
                try
                {
                    plugin.CustomizeForm(form);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying form customizations: {ex.Message}");
                }
            }
        }
        
        public static void ApplyControlCustomizations(Control control)
        {
            if (control == null) return;
            
            foreach (var plugin in _uiPlugins)
            {
                try
                {
                    plugin.CustomizeControl(control);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying control customizations: {ex.Message}");
                }
            }
            
            foreach (Control child in control.Controls)
            {
                ApplyControlCustomizations(child);
            }
        }
        
        public static TabPage[] GetCustomTabs()
        {
            var tabs = new List<TabPage>();
            
            foreach (var plugin in _uiPlugins)
            {
                try
                {
                    var pluginTabs = plugin.CreateCustomTabs();
                    if (pluginTabs != null)
                        tabs.AddRange(pluginTabs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting custom tabs: {ex.Message}");
                }
            }
            
            return tabs.ToArray();
        }
        
        public static ToolStripItem[] GetCustomToolbarItems()
        {
            var items = new List<ToolStripItem>();
            
            foreach (var plugin in _uiPlugins)
            {
                try
                {
                    var pluginItems = plugin.CreateToolbarItems();
                    if (pluginItems != null)
                        items.AddRange(pluginItems);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting custom toolbar items: {ex.Message}");
                }
            }
            
            return items.ToArray();
        }
        
        public static ToolStripMenuItem[] GetCustomMenuItems()
        {
            var items = new List<ToolStripMenuItem>();
            
            foreach (var plugin in _uiPlugins)
            {
                try
                {
                    var pluginItems = plugin.CreateMenuItems();
                    if (pluginItems != null)
                        items.AddRange(pluginItems);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting custom menu items: {ex.Message}");
                }
            }
            
            return items.ToArray();
        }
        
        public static void ClearExtensions()
        {
            _uiPlugins.Clear();
            _customMainForm?.Dispose();
            _customMainForm = null;
        }
        
        public static IUIExtensionPlugin[] GetUIPlugins()
        {
            return _uiPlugins.ToArray();
        }
    }
}