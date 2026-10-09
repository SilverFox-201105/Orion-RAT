using Orion.Common.IO;
using Orion.Common.Utilities;
using Orion.Server.Enums;
using System;

namespace Orion.Server.Models
{
    public class FileTransfer : IEquatable<FileTransfer>
    {
        private static readonly SafeRandom Random = new SafeRandom();

        public int Id { get; set; }
        public TransferType Type { get; set; }
        public long Size { get; set; }
        public long TransferredSize { get; set; }
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
        public string Status { get; set; }
        public FileSplit FileSplit { get; set; }

        /// <summary>Timestamp (UTC) of the last speed sample. Used for the live speed/ETA display.</summary>
        public DateTime SpeedSampleTime { get; set; } = DateTime.MinValue;

        /// <summary>Transferred bytes at the last speed sample. Used for the live speed/ETA display.</summary>
        public long SpeedSampleBytes { get; set; }

        /// <summary>Smoothed transfer speed in bytes/second for the live display.</summary>
        public double SpeedBytesPerSecond { get; set; }

        public bool Equals(FileTransfer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Type == other.Type && Size == other.Size &&
                   TransferredSize == other.TransferredSize && string.Equals(LocalPath, other.LocalPath) &&
                   string.Equals(RemotePath, other.RemotePath) && string.Equals(Status, other.Status);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public FileTransfer Clone()
        {
            return new FileTransfer()
            {
                Id = Id,
                Type = Type,
                Size = Size,
                TransferredSize = TransferredSize,
                LocalPath = LocalPath,
                RemotePath = RemotePath,
                Status = Status,
                FileSplit = FileSplit,
                SpeedSampleTime = SpeedSampleTime,
                SpeedSampleBytes = SpeedSampleBytes,
                SpeedBytesPerSecond = SpeedBytesPerSecond
            };
        }

        public static bool operator ==(FileTransfer f1, FileTransfer f2)
        {
            if (ReferenceEquals(f1, null))
                return ReferenceEquals(f2, null);

            return f1.Equals(f2);
        }

        public static bool operator !=(FileTransfer f1, FileTransfer f2)
        {
            return !(f1 == f2);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileTransfer);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static int GetRandomTransferId()
        {
            return Random.Next(0, int.MaxValue);
        }
    }
}
