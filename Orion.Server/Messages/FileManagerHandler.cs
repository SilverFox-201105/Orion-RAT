using Orion.Common.Enums;
using Orion.Common.IO;
using Orion.Common.Messages;
using Orion.Common.Messages.Administration.FileManager;
using Orion.Common.Messages.Other;
using Orion.Common.Models;
using Orion.Common.Networking;
using Orion.Server.Enums;
using Orion.Server.Models;
using Orion.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orion.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with remote files and directories.
    /// </summary>
    public class FileManagerHandler : MessageProcessorBase<string>, IDisposable
    {
        /// <summary>
        /// Represents the method that will handle drive changes.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="drives">All currently available drives.</param>
        public delegate void DrivesChangedEventHandler(object sender, Drive[] drives);

        /// <summary>
        /// Represents the method that will handle directory changes.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="remotePath">The remote path of the directory.</param>
        /// <param name="items">The directory content.</param>
        public delegate void DirectoryChangedEventHandler(object sender, string remotePath, FileSystemEntry[] items);

        /// <summary>
        /// Represents the method that will handle file transfer updates.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="transfer">The updated file transfer.</param>
        public delegate void FileTransferUpdatedEventHandler(object sender, FileTransfer transfer);

        /// <summary>
        /// Raised when drives changed.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event DrivesChangedEventHandler DrivesChanged;

        /// <summary>
        /// Raised when a directory changed.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event DirectoryChangedEventHandler DirectoryChanged;

        /// <summary>
        /// Raised when a file transfer updated.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event FileTransferUpdatedEventHandler FileTransferUpdated;

        /// <summary>
        /// Reports changed remote drives.
        /// </summary>
        /// <param name="drives">The current remote drives.</param>
        private void OnDrivesChanged(Drive[] drives)
        {
            SynchronizationContext.Post(d =>
            {
                var handler = DrivesChanged;
                handler?.Invoke(this, (Drive[])d);
            }, drives);
        }

        /// <summary>
        /// Reports a directory change.
        /// </summary>
        /// <param name="remotePath">The remote path of the directory.</param>
        /// <param name="items">The directory content.</param>
        private void OnDirectoryChanged(string remotePath, FileSystemEntry[] items)
        {
            SynchronizationContext.Post(i =>
            {
                var handler = DirectoryChanged;
                handler?.Invoke(this, remotePath, (FileSystemEntry[])i);
            }, items);
        }

        /// <summary>
        /// Reports updated file transfers.
        /// </summary>
        /// <param name="transfer">The updated file transfer.</param>
        private void OnFileTransferUpdated(FileTransfer transfer)
        {
            SynchronizationContext.Post(t =>
            {
                var handler = FileTransferUpdated;
                handler?.Invoke(this, (FileTransfer)t);
            }, transfer.Clone());
        }

        /// <summary>
        /// Keeps track of all active file transfers. Finished or canceled transfers get removed.
        /// </summary>
        private readonly List<FileTransfer> _activeFileTransfers = new List<FileTransfer>();

        /// <summary>
        /// Used in lock statements to synchronize access between UI thread and thread pool.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// Serializes writes to a transfer's underlying FileStream so multiple in-flight chunk
        /// messages (IDM-style parallel download) cannot corrupt the same stream concurrently.
        /// </summary>
        private readonly object _writeLock = new object();

        /// <summary>
        /// Tracks the individual segments of a parallel (multi-threaded) download.
        /// Keyed by the segment's own transfer id.
        /// </summary>
        private readonly Dictionary<int, SegmentInfo> _segments = new Dictionary<int, SegmentInfo>();

        /// <summary>
        /// Cancellation sources for the upload-confirmation watchdogs.
        /// </summary>
        private readonly Dictionary<int, CancellationTokenSource> _watchdogs = new Dictionary<int, CancellationTokenSource>();

        /// <summary>
        /// Maximum number of parallel segments for a single download (IDM-style).
        /// Configurable from the file-manager UI (default 8).
        /// </summary>
        public int MaxDownloadSegments { get; set; } = 8;

        /// <summary>
        /// Files smaller than this are downloaded in a single segment (legacy behaviour).
        /// </summary>
        private const long MinSegmentSize = 256 * 1024;

        /// <summary>
        /// Bookkeeping for one segment of a parallel download.
        /// </summary>
        private sealed class SegmentInfo
        {
            public int ParentId;
            public long StartOffset;
            public long Length;
            public long Received;
            public bool Done;
        }

        /// <summary>
        /// The client which is associated with this file manager handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// Used to only allow two simultaneous file uploads.
        /// </summary>
        private readonly Semaphore _limitThreads = new Semaphore(2, 2);

        /// <summary>
        /// Path to the base download directory of the client.
        /// </summary>
        private readonly string _baseDownloadPath;

        private readonly TaskManagerHandler _taskManagerHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManagerHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        /// <param name="subDirectory">Optional sub directory name.</param>
        public FileManagerHandler(Client client, string subDirectory = "") : base(true)
        {
            _client = client;
            _baseDownloadPath = Path.Combine(client.Value.DownloadDirectory, subDirectory);
            _taskManagerHandler = new TaskManagerHandler(client);
            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            MessageHandler.Register(_taskManagerHandler);
        }

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is FileTransferChunk ||
                                                             message is FileTransferCancel ||
                                                             message is FileTransferComplete ||
                                                             message is FileTransferQueryResponse ||
                                                             message is GetDrivesResponse ||
                                                             message is GetDirectoryResponse ||
                                                             message is SetStatusFileManager ||
                                                             message is DoZipFolder;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case FileTransferChunk file:
                    Execute(sender, file);
                    break;
                case FileTransferCancel cancel:
                    Execute(sender, cancel);
                    break;
                case FileTransferComplete complete:
                    Execute(sender, complete);
                    break;
                case FileTransferQueryResponse queryResponse:
                    Execute(sender, queryResponse);
                    break;
                case GetDrivesResponse drive:
                    Execute(sender, drive);
                    break;
                case GetDirectoryResponse directory:
                    Execute(sender, directory);
                    break;
                case SetStatusFileManager status:
                    Execute(sender, status);
                    break;
                case DoZipFolder zipFolder:
                    Execute(sender, zipFolder);
                    break;
            }
        }
        private void Execute(ISender client, DoZipFolder message)
        {
            client.Send(message);
        }

        public void ZipFolder(string sourcePath, string destinationPath, int compressionLevel)
        {
            var zipMessage = new DoZipFolder
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                CompressionLevel = compressionLevel
            };
            _client.Send(zipMessage);
        }

        /// <summary>
        /// Sanitizes a filename to prevent path traversal attacks.
        /// </summary>
        /// <param name="fileName">The filename to sanitize.</param>
        /// <returns>A safe filename or null if the filename is invalid.</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            fileName = Path.GetFileName(fileName);
            
            if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
                return null;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
                return null;

            if (fileName.StartsWith(".") || fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                return null;

            return fileName;
        }

        /// <summary>
        /// Begins downloading a file from the client.
        /// </summary>
        /// <param name="remotePath">The remote path of the file to download.</param>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="overwrite">Overwrite the local file with the newly downloaded.</param>
        public void BeginDownloadFile(string remotePath, string localFileName = "", bool overwrite = false)
        {
            if (string.IsNullOrEmpty(remotePath))
                return;

            int id = GetUniqueFileTransferId();

            if (!Directory.Exists(_baseDownloadPath))
                Directory.CreateDirectory(_baseDownloadPath);

            string fileName = string.IsNullOrEmpty(localFileName) ? Path.GetFileName(remotePath) : localFileName;

            // SECURITY FIX: Validate and sanitize filename to prevent path traversal
            fileName = SanitizeFileName(fileName);
            if (fileName == null)
            {
                OnReport("下载失败：无效的文件名");
                return;
            }

            string localPath = Path.Combine(_baseDownloadPath, fileName);

            // SECURITY FIX: Ensure the resolved path stays within the download directory
            try
            {
                string fullPath = Path.GetFullPath(localPath);
                string baseDir = Path.GetFullPath(_baseDownloadPath);

                if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    OnReport("下载失败：检测到路径穿越尝试");
                    return;
                }

                localPath = fullPath;
            }
            catch
            {
                OnReport("下载失败：无效路径");
                return;
            }

            int i = 1;
            while (!overwrite && File.Exists(localPath))
            {
                // rename file if it exists already
                var newFileName = string.Format("{0}({1}){2}", Path.GetFileNameWithoutExtension(localPath), i, Path.GetExtension(localPath));
                localPath = Path.Combine(_baseDownloadPath, newFileName);

                // Re-validate the new path
                try
                {
                    string fullPath = Path.GetFullPath(localPath);
                    string baseDir = Path.GetFullPath(_baseDownloadPath);

                    if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    {
                        OnReport("下载失败：路径验证错误");
                        return;
                    }

                    localPath = fullPath;
                }
                catch
                {
                    OnReport("下载失败：路径验证错误");
                    return;
                }

                i++;
            }

            var transfer = new FileTransfer
            {
                Id = id,
                Type = TransferType.Download,
                LocalPath = localPath,
                RemotePath = remotePath,
                Status = "查询文件信息...",
                TransferredSize = 0,
                Size = 0
            };

            try
            {
                transfer.FileSplit = new FileSplit(transfer.LocalPath, FileAccess.Write);
            }
            catch (Exception)
            {
                transfer.Status = "写入文件出错";
                OnFileTransferUpdated(transfer);
                return;
            }

            lock (_syncLock)
            {
                _activeFileTransfers.Add(transfer);
            }

            OnFileTransferUpdated(transfer);

            // Step 1: ask the client for the file size so we can split it into parallel
            // segments (IDM-style multi-threaded download).
            _client.Send(new FileTransferQuery { Id = id, RemotePath = remotePath });
        }

        /// <summary>
        /// Client replied with the remote file size. Splits the download into parallel segments
        /// (unless the file is tiny) and issues one <see cref="FileTransferRequest"/> per segment.
        /// </summary>
        private void Execute(ISender client, FileTransferQueryResponse message)
        {
            FileTransfer transfer;
            lock (_syncLock)
            {
                transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
            }

            if (transfer == null)
                return;

            if (!message.Exists)
            {
                transfer.Status = "文件不存在";
                RemoveFileTransfer(transfer.Id);
                OnFileTransferUpdated(transfer);
                return;
            }

            long total = message.FileSize;
            transfer.Size = total;

            try { transfer.FileSplit.SetLength(total); }
            catch { /* random-access writes will grow the file as needed */ }

            if (total == 0)
            {
                try { transfer.FileSplit.Flush(); } catch { }
                transfer.Status = "Completed";
                RemoveFileTransfer(transfer.Id);
                OnFileTransferUpdated(transfer);
                return;
            }

            // Decide how many parallel segments to use.
            int segCount = total < MinSegmentSize
                ? 1
                : (int)Math.Min(MaxDownloadSegments, Math.Ceiling((double)total / MinSegmentSize));
            long segSize = (long)Math.Ceiling((double)total / segCount);

            for (int s = 0; s < segCount; s++)
            {
                long start = s * segSize;
                long len = Math.Min(segSize, total - start);
                if (len <= 0) break;

                int segId = GetUniqueFileTransferId();
                lock (_syncLock)
                {
                    _segments[segId] = new SegmentInfo
                    {
                        ParentId = transfer.Id,
                        StartOffset = start,
                        Length = len,
                        Received = 0,
                        Done = false
                    };
                }

                _client.Send(new FileTransferRequest
                {
                    Id = segId,
                    RemotePath = transfer.RemotePath,
                    StartOffset = start,
                    Length = len
                });
            }

            transfer.Status = segCount == 1
                ? "下载中...(0%)"
                : $"下载中... (0/{segCount} 段)";
            OnFileTransferUpdated(transfer);
        }

        /// <summary>
        /// Begins uploading a file to the client.
        /// </summary>
        /// <param name="localPath">The local path of the file to upload.</param>
        /// <param name="remotePath">Save the uploaded file to this remote path. If empty, generate a temporary file name.</param>
        public void BeginUploadFile(string localPath, string remotePath = "")
        {
            new Thread(() =>
            {
                int id = GetUniqueFileTransferId();

                FileTransfer transfer = new FileTransfer
                {
                    Id = id,
                    Type = TransferType.Upload,
                    LocalPath = localPath,
                    RemotePath = remotePath,
                    Status = "等待中...",
                    TransferredSize = 0
                };

                try
                {
                    transfer.FileSplit = new FileSplit(localPath, FileAccess.Read);
                }
                catch (Exception)
                {
                    transfer.Status = "读取文件出错";
                    OnFileTransferUpdated(transfer);
                    return;
                }

                try
                {
                    transfer.Size = transfer.FileSplit.FileSize;
                }
                catch (Exception)
                {
                    transfer.Status = "读取文件出错";
                    OnFileTransferUpdated(transfer);
                    transfer.FileSplit?.Dispose();
                    return;
                }

                lock (_syncLock)
                {
                    _activeFileTransfers.Add(transfer);
                }

                OnFileTransferUpdated(transfer);

                _limitThreads.WaitOne();
                try
                {
                    foreach (var chunk in transfer.FileSplit)
                    {
                        bool canceled;
                        lock (_syncLock)
                        {
                            canceled = _activeFileTransfers.Count(f => f.Id == id) == 0;
                        }

                        if (canceled)
                        {
                            // RemoveFileTransfer already disposed the FileSplit.
                            return;
                        }

                        transfer.TransferredSize += chunk.Data.Length;
                        transfer.Status = BuildProgressStatus(transfer, "上传");
                        OnFileTransferUpdated(transfer);

                        _client.SendBlocking(new FileTransferChunk
                        {
                            Id = id,
                            Chunk = chunk,
                            FilePath = remotePath,
                            FileSize = transfer.Size,
                            FileExtension = Path.GetExtension(localPath)
                        });
                    }

                    // All bytes have been transmitted. Do NOT declare success here.
                    // The client writes the file to disk and confirms with FileTransferComplete,
                    // or reports a failure with FileTransferCancel. We wait for that real
                    // acknowledgement instead of blindly reporting "Completed".
                    transfer.Status = "已发送，等待客户端确认...";
                    OnFileTransferUpdated(transfer);
                    StartUploadWatchdog(transfer);
                }
                catch (Exception)
                {
                    lock (_syncLock)
                    {
                        if (_activeFileTransfers.Count(f => f.Id == id) == 0)
                        {
                            return;
                        }
                    }
                    transfer.Status = "读取文件出错";
                    OnFileTransferUpdated(transfer);
                    CancelFileTransfer(transfer.Id);
                }
                finally
                {
                    _limitThreads.Release();
                }
            }).Start();
        }

        /// <summary>
        /// Guards against an upload that never gets a confirmation from the client:
        /// if the client neither confirms (FileTransferComplete) nor rejects (FileTransferCancel)
        /// within the timeout, the transfer is marked as failed so the UI does not lie about success.
        /// </summary>
        private void StartUploadWatchdog(FileTransfer transfer)
        {
            int id = transfer.Id;
            var cts = new CancellationTokenSource();

            lock (_syncLock)
            {
                _watchdogs[id] = cts;
            }

            Task.Delay(TimeSpan.FromSeconds(60), cts.Token).ContinueWith(_ =>
            {
                FileTransfer tr;
                lock (_syncLock)
                {
                    _watchdogs.Remove(id);
                    tr = _activeFileTransfers.FirstOrDefault(t => t.Id == id);
                }

                if (tr != null && tr.Status != "Completed")
                {
                    tr.Status = "上传确认超时（客户端无响应）";
                    OnFileTransferUpdated(tr);
                    _client.Send(new FileTransferCancel { Id = id });
                    RemoveFileTransfer(id);
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Cancels a file transfer.
        /// </summary>
        /// <param name="transferId">The id of the file transfer to cancel.</param>
        public void CancelFileTransfer(int transferId)
        {
            List<int> segIds;
            lock (_syncLock)
            {
                segIds = _segments.Where(kv => kv.Value.ParentId == transferId).Select(kv => kv.Key).ToList();
            }

            foreach (var segId in segIds)
                _client.Send(new FileTransferCancel { Id = segId });

            _client.Send(new FileTransferCancel { Id = transferId });
            RemoveFileTransfer(transferId);
        }

        /// <summary>
        /// Renames a remote file or directory.
        /// </summary>
        /// <param name="remotePath">The remote file or directory path to rename.</param>
        /// <param name="newPath">The new name of the remote file or directory path.</param>
        /// <param name="type">The type of the file (file or directory).</param>
        public void RenameFile(string remotePath, string newPath, FileType type)
        {
            _client.Send(new DoPathRename
            {
                Path = remotePath,
                NewPath = newPath,
                PathType = type
            });
        }

        /// <summary>
        /// Deletes a remote file or directory.
        /// </summary>
        /// <param name="remotePath">The remote file or directory path.</param>
        /// <param name="type">The type of the file (file or directory).</param>
        public void DeleteFile(string remotePath, FileType type)
        {
            _client.Send(new DoPathDelete { Path = remotePath, PathType = type });
        }

        /// <summary>
        /// Starts a new process remotely.
        /// </summary>
        /// <param name="remotePath">The remote path used for starting the new process.</param>
        public void StartProcess(string remotePath)
        {
            _taskManagerHandler.StartProcess(remotePath);
        }

        /// <summary>
        /// Adds an item to the startup of the client.
        /// </summary>
        /// <param name="item">The startup item to add.</param>
        public void AddToStartup(StartupItem item)
        {
            _client.Send(new DoStartupItemAdd { StartupItem = item });
        }

        /// <summary>
        /// Gets the directory contents for the remote path.
        /// </summary>
        /// <param name="remotePath">The remote path of the directory.</param>
        public void GetDirectoryContents(string remotePath)
        {
            _client.Send(new GetDirectory { RemotePath = remotePath });
        }

        /// <summary>
        /// Refreshes the remote drives.
        /// </summary>
        public void RefreshDrives()
        {
            _client.Send(new GetDrives());
        }

        private void Execute(ISender client, FileTransferChunk message)
        {
            FileTransfer transfer;
            SegmentInfo segment = null;

            lock (_syncLock)
            {
                if (_segments.TryGetValue(message.Id, out segment))
                {
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == segment.ParentId);
                }
                else
                {
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
                }
            }

            if (transfer == null || transfer.FileSplit == null)
                return;

            lock (_writeLock)
            {
                try
                {
                    transfer.FileSplit.WriteChunk(message.Chunk);
                    transfer.TransferredSize += message.Chunk.Data.Length;
                    if (message.FileSize > 0)
                        transfer.Size = Math.Max(transfer.Size, message.FileSize);
                }
                catch (Exception)
                {
                    transfer.Status = "写入文件出错";
                    OnFileTransferUpdated(transfer);
                    CancelFileTransfer(transfer.Id);
                    return;
                }
            }

            if (segment != null)
            {
                bool allDone;
                lock (_syncLock)
                {
                    segment.Received += message.Chunk.Data.Length;
                    if (segment.Length > 0 && segment.Received >= segment.Length)
                        segment.Done = true;

                    var siblings = _segments.Values.Where(s => s.ParentId == segment.ParentId).ToList();
                    allDone = siblings.Count > 0 && siblings.All(s => s.Done);
                }

                if (allDone)
                {
                    CompleteDownload(transfer);
                    return;
                }
            }
            else
            {
                if (transfer.TransferredSize >= transfer.Size)
                {
                    CompleteDownload(transfer);
                    return;
                }
            }

            decimal overall = transfer.Size == 0 ? 100 : Math.Round((decimal)((double)transfer.TransferredSize / (double)transfer.Size * 100.0), 2);
            transfer.Status = BuildProgressStatus(transfer, "下载");
            OnFileTransferUpdated(transfer);
        }

        /// <summary>
        /// Finalizes a download: flushes buffered data, marks it completed and releases resources.
        /// </summary>
        private void CompleteDownload(FileTransfer transfer)
        {
            lock (_syncLock)
            {
                var segKeys = _segments.Where(kv => kv.Value.ParentId == transfer.Id).Select(kv => kv.Key).ToList();
                foreach (var k in segKeys)
                    _segments.Remove(k);
            }

            try { transfer.FileSplit.Flush(); } catch { }

            transfer.Status = "Completed";
            RemoveFileTransfer(transfer.Id);
            OnFileTransferUpdated(transfer);
        }

        /// <summary>
        /// Human-readable transfer speed, e.g. "12.3 MB/s".
        /// </summary>
        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec <= 0) return string.Empty;
            string[] units = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
            int u = 0;
            double v = bytesPerSec;
            while (v >= 1024 && u < units.Length - 1)
            {
                v /= 1024;
                u++;
            }
            return $"{v:0.##} {units[u]}";
        }

        /// <summary>
        /// Human-readable estimated time remaining, e.g. "2m13s".
        /// </summary>
        private static string FormatEta(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0) return "计算中";
            if (seconds < 1) return "<1s";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h{ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m{ts.Seconds}s";
            return $"{(int)ts.TotalSeconds}s";
        }

        /// <summary>
        /// Builds the in-progress status text with live percentage, speed and ETA.
        /// Speed is sampled at most every ~0.25s and exponentially smoothed to avoid jitter.
        /// </summary>
        private string BuildProgressStatus(FileTransfer t, string direction)
        {
            var now = DateTime.UtcNow;
            if (t.SpeedSampleTime != DateTime.MinValue)
            {
                double dt = (now - t.SpeedSampleTime).TotalSeconds;
                if (dt >= 0.25)
                {
                    long deltaBytes = t.TransferredSize - t.SpeedSampleBytes;
                    double instantaneous = dt > 0 ? deltaBytes / dt : 0;
                    t.SpeedBytesPerSecond = t.SpeedBytesPerSecond <= 0
                        ? instantaneous
                        : (t.SpeedBytesPerSecond * 0.6 + instantaneous * 0.4);
                    t.SpeedSampleTime = now;
                    t.SpeedSampleBytes = t.TransferredSize;
                }
            }
            else
            {
                t.SpeedSampleTime = now;
                t.SpeedSampleBytes = t.TransferredSize;
            }

            decimal progress = t.Size == 0 ? 100 : Math.Round((decimal)((double)t.TransferredSize / (double)t.Size * 100.0), 1);
            string text = $"{direction}中... ({progress}%)";

            if (t.SpeedBytesPerSecond > 0)
            {
                text += $"  {FormatSpeed(t.SpeedBytesPerSecond)}";
                if (t.Size > 0)
                {
                    double remaining = (t.Size - t.TransferredSize) / t.SpeedBytesPerSecond;
                    text += $"  剩余 {FormatEta(remaining)}";
                }
            }

            return text;
        }

        private void Execute(ISender client, FileTransferCancel message)
        {
            FileTransfer transfer;
            SegmentInfo segment = null;

            lock (_syncLock)
            {
                if (_segments.TryGetValue(message.Id, out segment))
                {
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == segment.ParentId);
                }
                else
                {
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
                }
            }

            if (transfer != null)
            {
                transfer.Status = message.Reason;
                OnFileTransferUpdated(transfer);
                RemoveFileTransfer(transfer.Id);
                // don't keep un-finished files
                if (transfer.Type == TransferType.Download)
                    File.Delete(transfer.LocalPath);
            }
        }

        private void Execute(ISender client, FileTransferComplete message)
        {
            FileTransfer transfer;
            SegmentInfo segment = null;

            lock (_syncLock)
            {
                if (_segments.TryGetValue(message.Id, out segment))
                {
                    segment.Done = true;
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == segment.ParentId);
                }
                else
                {
                    transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
                }
            }

            if (transfer == null)
                return;

            if (segment != null)
            {
                int total, done;
                lock (_syncLock)
                {
                    var siblings = _segments.Values.Where(s => s.ParentId == segment.ParentId).ToList();
                    total = siblings.Count;
                    done = siblings.Count(s => s.Done);
                }

                if (done >= total && total > 0)
                {
                    CompleteDownload(transfer);
                }
                else
                {
                    transfer.Status = $"下载中... ({done}/{total} 段)";
                    OnFileTransferUpdated(transfer);
                }
                return;
            }

            // Upload completion, or legacy single-segment download completion.
            transfer.RemotePath = message.FilePath; // required for temporary file names generated on the client
            transfer.Status = "Completed";
            RemoveFileTransfer(transfer.Id);
            OnFileTransferUpdated(transfer);
        }

        private void Execute(ISender client, GetDrivesResponse message)
        {
            if (message.Drives?.Length == 0)
                return;

            OnDrivesChanged(message.Drives);
        }

        private void Execute(ISender client, GetDirectoryResponse message)
        {
            if (message.Items == null)
            {
                message.Items = new FileSystemEntry[0];
            }
            OnDirectoryChanged(message.RemotePath, message.Items);
        }

        private void Execute(ISender client, SetStatusFileManager message)
        {
            OnReport(message.Message);
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            if (action != ProcessAction.Start) return;
            OnReport(result ? "进程启动成功" : "进程启动失败");
        }

        /// <summary>
        /// Removes a file transfer given the transfer id.
        /// </summary>
        /// <param name="transferId">The file transfer id.</param>
        private void RemoveFileTransfer(int transferId)
        {
            lock (_syncLock)
            {
                var segKeys = _segments.Where(kv => kv.Value.ParentId == transferId).Select(kv => kv.Key).ToList();
                foreach (var k in segKeys)
                    _segments.Remove(k);

                if (_watchdogs.TryGetValue(transferId, out var cts))
                {
                    _watchdogs.Remove(transferId);
                    try { cts.Cancel(); } catch { }
                    try { cts.Dispose(); } catch { }
                }

                var transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == transferId);
                transfer?.FileSplit?.Dispose();
                _activeFileTransfers.RemoveAll(s => s.Id == transferId);
            }
        }

        /// <summary>
        /// Generates a unique file transfer id.
        /// </summary>
        /// <returns>A unique file transfer id.</returns>
        private int GetUniqueFileTransferId()
        {
            int id;
            lock (_syncLock)
            {
                do
                {
                    id = FileTransfer.GetRandomTransferId();
                    // generate new id until we have a unique one across active transfers and segments
                } while (_activeFileTransfers.Any(f => f.Id == id) || _segments.ContainsKey(id));
            }

            return id;
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncLock)
                {
                    foreach (var transfer in _activeFileTransfers)
                    {
                        _client.Send(new FileTransferCancel { Id = transfer.Id });
                        transfer.FileSplit?.Dispose();
                        if (transfer.Type == TransferType.Download)
                            File.Delete(transfer.LocalPath);
                    }

                    _activeFileTransfers.Clear();
                }

                MessageHandler.Unregister(_taskManagerHandler);
                _taskManagerHandler.ProcessActionPerformed -= ProcessActionPerformed;
            }
        }
    }
}
