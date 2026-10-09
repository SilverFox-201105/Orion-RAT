using Orion.Common.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Orion.Common.IO
{
    public class FileSplit : IEnumerable<FileChunk>, IDisposable
    {
        /// <summary>
        /// The maximum size per file chunk.
        /// </summary>
        public readonly int MaxChunkSize = 65535;

        /// <summary>
        /// The file path of the opened file.
        /// </summary>
        public string FilePath => _fileStream.Name;

        /// <summary>
        /// The file size of the opened file.
        /// </summary>
        public long FileSize => _fileStream.Length;

        /// <summary>
        /// The file stream of the opened file.
        /// </summary>
        private readonly FileStream _fileStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSplit"/> class using the given file path and access mode.
        /// </summary>
        /// <param name="filePath">The path to the file to open.</param>
        /// <param name="fileAccess">The file access mode for opening the file. Allowed are <see cref="FileAccess.Read"/> and <see cref="FileAccess.Write"/>.</param>
        public FileSplit(string filePath, FileAccess fileAccess)
        {
            try
            {
                switch (fileAccess)
                {
                    case FileAccess.Read:
                        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        break;
                    case FileAccess.Write:
                        _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        break;
                    default:
                        throw new ArgumentException($"{nameof(fileAccess)} must be either Read or Write.");
                }
            }
            catch (IOException ex)
            {
                const uint ERROR_SHARING_VIOLATION = 32;
                // HResult low word contains the Win32 error code for IO exceptions
                if (((uint)ex.HResult & 0xFFFFu) == ERROR_SHARING_VIOLATION)
                {
                    try
                    {
                        MessageBox.Show($"The file '{filePath}' is in use by another process.", "File in use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch
                    {
                        // If MessageBox fails (no UI available), swallow and continue to throw the original exception
                    }

                    // Throw a clearer exception so callers can handle it explicitly
                    throw new IOException($"The file '{filePath}' is in use by another process.", ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Writes a chunk to the file. In other words.
        /// </summary>
        /// <param name="chunk"></param>
        public void WriteChunk(FileChunk chunk)
        {
            _fileStream.Seek(chunk.Offset, SeekOrigin.Begin);
            _fileStream.Write(chunk.Data, 0, chunk.Data.Length);
        }

        /// <summary>
        /// Flushes any buffered data to disk. Call once after a transfer (or segment) completes
        /// instead of flushing on every single chunk — flushing per chunk on large files causes
        /// thousands of synchronous disk writes and makes the transfer appear to freeze.
        /// </summary>
        public void Flush()
        {
            _fileStream.Flush();
        }

        /// <summary>
        /// Pre-sizes the underlying file so random-access writes (parallel / multi-segment downloads)
        /// always land inside a correctly sized file.
        /// </summary>
        public void SetLength(long length)
        {
            _fileStream.SetLength(length);
        }

        /// <summary>
        /// Reads a chunk of the file.
        /// </summary>
        /// <param name="offset">Offset of the file, must be a multiple of <see cref="MaxChunkSize"/> for proper reconstruction.</param>
        /// <returns>The read file chunk at the given offset.</returns>
        /// <remarks>
        /// The returned file chunk can be smaller than <see cref="MaxChunkSize"/> iff the
        /// remaining file size from the offset is smaller than <see cref="MaxChunkSize"/>,
        /// then the remaining file size is used.
        /// </remarks>
        public FileChunk ReadChunk(long offset)
        {
            _fileStream.Seek(offset, SeekOrigin.Begin);

            long chunkSize = _fileStream.Length - _fileStream.Position < MaxChunkSize
                ? _fileStream.Length - _fileStream.Position
                : MaxChunkSize;

            var chunkData = new byte[chunkSize];
            // Stream.Read is not guaranteed to fill the buffer in a single call;
            // read until the buffer is full (or EOF) to avoid silent data corruption
            // when reconstructing split files.
            int totalRead = 0;
            while (totalRead < chunkData.Length)
            {
                int bytesRead = _fileStream.Read(chunkData, totalRead, chunkData.Length - totalRead);
                if (bytesRead == 0) break; // unexpected EOF (short file)
                totalRead += bytesRead;
            }

            return new FileChunk
            {
                Data = chunkData,
                Offset = _fileStream.Position - chunkData.Length
            };
        }

        /// <summary>
        /// Returns an enumerator that iterates through the file chunks.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the file chunks.</returns>
        public IEnumerator<FileChunk> GetEnumerator()
        {
            if (_fileStream.Length == 0)
            {
                yield return ReadChunk(0);
                yield break;
            }

            for (long offset = 0; offset < _fileStream.Length; offset += MaxChunkSize)
            {
                yield return ReadChunk(offset);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream.Dispose();
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
