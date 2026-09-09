using System.IO.Compression;
using System.Text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Reads resources by relative path from a ZIP file or a directory.</para>
    ///     <para xml:lang="zh-CN">通过相对路径读取 ZIP 文件或目录中的资源。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Each instance owns its source independently. ZIP files are opened on first use and retained until disposal;
    ///         resource contents are read on demand without a content cache or manifest. Calls on one instance are serialized.
    ///         Dispose and create another instance before replacing an open ZIP. Directory reads use the current file contents.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         每个实例独立持有自己的来源。ZIP 在首次使用时打开，并保持到实例释放；
    ///         资源内容按需读取，不使用内容缓存或清单。同一实例的操作串行执行。
    ///         替换已打开的 ZIP 前应释放实例并重新创建；目录读取使用文件当前的内容。
    ///     </para>
    /// </remarks>
    public sealed class ResourcePack : IDisposable
    {
        private readonly Lock _sync = new();
        private readonly string _sourcePath;
        private readonly bool _isZip;
        private ZipArchive? _archive;
        private bool _disposed;

        private ResourcePack(string path, bool isZip)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            _sourcePath = Path.GetFullPath(path);
            _isZip = isZip;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a ZIP-backed reader without opening or checking the archive.</para>
        ///     <para xml:lang="zh-CN">创建 ZIP 资源读取器，不打开或检查归档。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">ZIP file path; relative paths are resolved against the current directory now.</para>
        ///     <para xml:lang="zh-CN">ZIP 文件路径；相对路径在创建时根据当前目录解析。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A reader owned and disposed by the caller.</para>
        ///     <para xml:lang="zh-CN">由调用方持有并负责释放的读取器。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or invalid.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或无效。</para>
        /// </exception>
        public static ResourcePack FromZip(string path)
        {
            return new(path, true);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a directory-backed reader without checking the directory.</para>
        ///     <para xml:lang="zh-CN">创建目录资源读取器，不检查目录是否存在。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">Root directory path; relative paths are resolved against the current directory now.</para>
        ///     <para xml:lang="zh-CN">根目录路径；相对路径在创建时根据当前目录解析。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A reader owned and disposed by the caller.</para>
        ///     <para xml:lang="zh-CN">由调用方持有并负责释放的读取器。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or invalid.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或无效。</para>
        /// </exception>
        public static ResourcePack FromDirectory(string path)
        {
            return new(path, false);
        }

        /// <summary>
        ///     <para xml:lang="en">Reads one resource into a new caller-owned byte array.</para>
        ///     <para xml:lang="zh-CN">将一个资源读取为由调用方持有的新字节数组。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">
        ///         Nonempty relative file path. Both slash styles are accepted; rooted paths, colons, empty segments,
        ///         and dot segments are rejected. ZIP names are case-sensitive; directory names follow the file system.
        ///         For duplicate ZIP names, the first matching entry is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         非空相对文件路径，接受两种斜线；不允许绝对路径、冒号、空路径段及点路径段。
        ///         ZIP 名称区分大小写，目录名称遵循文件系统规则；ZIP 重名条目使用第一个匹配项。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resource bytes, independent of the pack's lifetime.</para>
        ///     <para xml:lang="zh-CN">资源字节，其生命周期与资源包无关。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The relative path is invalid.</para>
        ///     <para xml:lang="zh-CN">相对路径无效。</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     <para xml:lang="en">The reader has been disposed.</para>
        ///     <para xml:lang="zh-CN">读取器已释放。</para>
        /// </exception>
        /// <exception cref="IOException">
        ///     <para xml:lang="en">The source or resource is missing, unreadable, or too large for one byte array.</para>
        ///     <para xml:lang="zh-CN">来源或资源缺失、无法读取，或超出单个字节数组的容量。</para>
        /// </exception>
        /// <exception cref="InvalidDataException">
        ///     <para xml:lang="en">ZIP data is invalid or uses unsupported compression.</para>
        ///     <para xml:lang="zh-CN">ZIP 数据无效或使用不支持的压缩方式。</para>
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     <para xml:lang="en">The file system denies access.</para>
        ///     <para xml:lang="zh-CN">文件系统拒绝访问。</para>
        /// </exception>
        public byte[] ReadAllBytes(string path)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                path = NormalizePath(path, false);
                if (!_isZip)
                    return File.ReadAllBytes(Path.Combine(_sourcePath, path));

                var entry = GetArchive().GetEntry(path)
                            ?? throw new FileNotFoundException($"Resource not found: {path}", path);
                if (entry.Length > Array.MaxLength)
                    throw new IOException($"Resource exceeds the maximum byte array length: {path}");
                using var stream = entry.Open();
                var bytes = new byte[(int)entry.Length];
                stream.ReadExactly(bytes);
                return bytes;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Reads resource text with BOM detection, using UTF-8 unless another encoding is provided.</para>
        ///     <para xml:lang="zh-CN">读取资源文本并识别 BOM；未指定其他编码时使用 UTF-8。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">Relative resource path with the same rules as <see cref="ReadAllBytes"/>.</para>
        ///     <para xml:lang="zh-CN">资源相对路径，规则与 <see cref="ReadAllBytes"/> 相同。</para>
        /// </param>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding used when no BOM is present; null selects UTF-8.</para>
        ///     <para xml:lang="zh-CN">没有 BOM 时使用的编码；null 表示 UTF-8。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Decoded text; read errors propagate as documented by <see cref="ReadAllBytes"/>.</para>
        ///     <para xml:lang="zh-CN">解码后的文本；读取错误按 <see cref="ReadAllBytes"/> 的说明传播。</para>
        /// </returns>
        public string ReadAllText(string path, Encoding? encoding = null)
        {
            using var stream = new MemoryStream(ReadAllBytes(path), false);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        /// <summary>
        ///     <para xml:lang="en">Lists resource files in a directory without reading their contents.</para>
        ///     <para xml:lang="zh-CN">列出目录中的资源文件，不读取文件内容。</para>
        /// </summary>
        /// <param name="directory">
        ///     <para xml:lang="en">Relative directory following <see cref="ReadAllBytes"/> path rules; empty means the pack root.</para>
        ///     <para xml:lang="zh-CN">遵循 <see cref="ReadAllBytes"/> 路径规则的相对目录；空字符串表示资源包根目录。</para>
        /// </param>
        /// <param name="recursive">
        ///     <para xml:lang="en">Whether to include files in subdirectories.</para>
        ///     <para xml:lang="zh-CN">是否包含子目录中的文件。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A snapshot of pack-relative paths with forward slashes, deduplicated and sorted ordinally.
        ///         An absent ZIP prefix yields an empty list. Missing file-system directories and read errors propagate normally.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用正斜线的包内相对路径快照，按序号规则去重并排序。
        ///         ZIP 中不存在的目录前缀返回空列表；文件系统目录缺失及读取错误正常向外传播。
        ///     </para>
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///     <para xml:lang="en">The reader has been disposed.</para>
        ///     <para xml:lang="zh-CN">读取器已释放。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The relative directory is invalid.</para>
        ///     <para xml:lang="zh-CN">相对目录无效。</para>
        /// </exception>
        public IReadOnlyList<string> EnumerateFiles(string directory = "", bool recursive = false)
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                directory = NormalizePath(directory, true);
                IEnumerable<string> paths;
                if (_isZip)
                {
                    var prefix = directory.Length == 0 ? "" : directory + "/";
                    paths = GetArchive().Entries
                        .Where(entry => entry.Name.Length != 0
                                        && entry.FullName.StartsWith(prefix, StringComparison.Ordinal)
                                        && (recursive || !entry.FullName[prefix.Length..].Contains('/')))
                        .Select(entry => entry.FullName);
                }
                else
                    paths = Directory.EnumerateFiles(Path.Combine(_sourcePath, directory), "*",
                            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Select(path => Path.GetRelativePath(_sourcePath, path).Replace('\\', '/'));

                return [.. paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Closes the owned archive after any active read completes. Repeated calls have no effect.</para>
        ///     <para xml:lang="zh-CN">等待当前读取结束后关闭持有的归档；重复调用无额外效果。</para>
        /// </summary>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _archive?.Dispose();
                _archive = null;
            }
        }

        internal static string NormalizePath(string path, bool allowEmpty)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (allowEmpty && path.Length == 0)
                return path;
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains(':') || normalized.Split('/').Any(part => part is "" or "." or ".."))
                throw new ArgumentException("Expected a relative resource path without empty or dot segments.",
                    nameof(path));
            return normalized;
        }

        private ZipArchive GetArchive()
        {
            return _archive ??= ZipFile.OpenRead(_sourcePath);
        }
    }
}
