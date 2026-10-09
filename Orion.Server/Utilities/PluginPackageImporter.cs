using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Orion.Server.Utilities
{
    internal static class PluginPackageImporter
    {
        /// <summary>
        /// 导入插件包。
        /// </summary>
        /// <param name="packagePath">.Orion 包路径</param>
        /// <param name="pluginsDir">插件根目录（Plugins）</param>
        /// <param name="onProgress">进度回调：(已解压百分比 0-100, 当前正在解压的条目名)；可为 null。</param>
        public static void Import(string packagePath, string pluginsDir, Action<int, string> onProgress = null)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                throw new FileNotFoundException("Package not found", packagePath);

            using (var fs = File.OpenRead(packagePath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var manifestEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.Name, "plugin.json", StringComparison.OrdinalIgnoreCase));
                string subfolderName = null;

                if (manifestEntry != null)
                {
                    using (var reader = new StreamReader(manifestEntry.Open()))
                    {
                        var json = reader.ReadToEnd();
                        var manifest = JObject.Parse(json);
                        subfolderName = manifest["subfolder"]?.ToString() ?? manifest["name"]?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(subfolderName))
                {
                    subfolderName = Path.GetFileNameWithoutExtension(packagePath);
                }

                // The manifest is untrusted input: a crafted "subfolder"/"name" could contain
                // path separators or ".." and escape the Plugins directory.
                subfolderName = SanitizeRelativeSegment(subfolderName);
                if (string.IsNullOrEmpty(subfolderName))
                {
                    subfolderName = SanitizeRelativeSegment(Path.GetFileNameWithoutExtension(packagePath));
                }

                if (string.IsNullOrEmpty(subfolderName))
                {
                    throw new InvalidDataException("Cannot determine a safe target folder for the plugin package.");
                }

                var targetDir = Path.GetFullPath(Path.Combine(pluginsDir, subfolderName));
                var targetRoot = EnsureTrailingSeparator(targetDir);
                var pluginsRoot = EnsureTrailingSeparator(Path.GetFullPath(pluginsDir));

                // Defence in depth: the extracted folder must stay inside the Plugins directory.
                if (!targetRoot.StartsWith(pluginsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Refusing to import plugin package: target folder '{subfolderName}' escapes the Plugins directory.");
                }

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                // 解压进度：按各条目未压缩字节数累加，给出平滑的百分比。
                long totalBytes = 0;
                foreach (var e in zip.Entries)
                {
                    if (!string.IsNullOrEmpty(e.Name) && e.Length > 0) totalBytes += e.Length;
                }
                long doneBytes = 0;
                var throttle = Stopwatch.StartNew();
                int lastPct = -1;

                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // Skip directory entries

                    // Preserve the in-package directory layout (libs\, x64\, ...) instead of
                    // flattening everything into the target folder, which used to break dependencies.
                    var relative = NormalizeRelativePath(entry.FullName);
                    if (string.IsNullOrEmpty(relative))
                    {
                        continue;
                    }

                    // Zip Slip guard: reject rooted paths and any ".." traversal component.
                    if (Path.IsPathRooted(relative) || HasTraversal(relative))
                    {
                        continue;
                    }

                    var outPath = Path.GetFullPath(Path.Combine(targetDir, relative));
                    if (!outPath.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ExtractEntry(entry, outPath);

                    doneBytes += entry.Length;
                    int pct = totalBytes > 0 ? (int)(100.0 * doneBytes / totalBytes) : 0;
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    // 节流：至少前进 1% 或距上次上报 ≥50ms 才回调一次。
                    // 之前每文件都同步 Invoke 会阻塞解压线程并刷爆 UI 线程，导致卡顿且解压变慢。
                    if (pct != lastPct && (pct - lastPct >= 1 || throttle.ElapsedMilliseconds >= 50))
                    {
                        lastPct = pct;
                        throttle.Restart();
                        onProgress?.Invoke(pct, entry.Name);
                    }
                }

                onProgress?.Invoke(100, "完成");
            }
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var sep = Path.DirectorySeparatorChar;
            return path.EndsWith(sep.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                ? path
                : path + sep;
        }

        /// <summary>
        /// 自定义解压单个条目：显式大缓冲（80KB）+ SequentialScan，降低海量小文件的 IO 开销。
        /// </summary>
        private static void ExtractEntry(ZipArchiveEntry entry, string outPath)
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var src = entry.Open();
            using var dst = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.SequentialScan);
            src.CopyTo(dst, 81920);
        }

        /// <summary>
        /// Normalizes a zip entry path to a relative path using the local directory separator.
        /// Returns null when the entry cannot be expressed as a safe relative path.
        /// </summary>
        private static string NormalizeRelativePath(string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath)) return null;

            var normalized = entryPath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .Trim();

            // Drop any leading separators so a "/libs/x.dll" style entry stays relative.
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalized.Length == 0 ? null : normalized;
        }

        private static bool HasTraversal(string relativePath)
        {
            var parts = relativePath.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.None);

            foreach (var part in parts)
            {
                if (part == "..") return true;

                // Reject anything with invalid path characters (wildcards, trailing spaces/dots, ...).
                if (part.Length > 0 && part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return true;
            }

            return false;
        }

        /// <summary>
        /// Reduces a manifest-provided folder name to a single safe directory segment.
        /// </summary>
        private static string SanitizeRelativeSegment(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var candidate = name.Trim();

            // Keep only the last segment if a path was supplied.
            candidate = candidate.Split(
                new[] { '\\', '/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();

            if (string.IsNullOrWhiteSpace(candidate)) return null;

            // Strip characters that are illegal in file names.
            var buffer = candidate.ToCharArray();
            var invalid = Path.GetInvalidFileNameChars();
            for (var i = 0; i < buffer.Length; i++)
            {
                if (Array.IndexOf(invalid, buffer[i]) >= 0) buffer[i] = '_';
            }

            candidate = new string(buffer).Trim().TrimEnd('.');

            if (candidate.Length == 0) return null;
            if (candidate == "." || candidate == "..") return null;

            return candidate;
        }
    }
}
