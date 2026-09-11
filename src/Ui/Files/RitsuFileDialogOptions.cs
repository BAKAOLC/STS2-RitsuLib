using Godot;

namespace STS2RitsuLib.Ui.Files
{
    /// <summary>
    ///     <para xml:lang="en">Configures a themed filesystem picker. Options are captured when the dialog opens.</para>
    ///     <para xml:lang="zh-CN">配置使用库主题的文件系统选择器；对话框打开时捕获这些选项。</para>
    /// </summary>
    public sealed class RitsuFileDialogOptions
    {
        /// <summary>
        ///     <para xml:lang="en">A nonblank title of up to 256 characters, or null to use the localized mode title.</para>
        ///     <para xml:lang="zh-CN">最多 256 个字符的非空白标题；null 表示使用所选模式对应的本地化标题。</para>
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The file, multi-file, directory, file-or-directory, or save selection mode.</para>
        ///     <para xml:lang="zh-CN">单文件、多文件、目录、文件或目录，以及保存文件的选择模式。</para>
        /// </summary>
        public FileDialog.FileModeEnum Mode { get; init; } = FileDialog.FileModeEnum.OpenFile;

        /// <summary>
        ///     <para xml:lang="en">
        ///         A nonblank key of at most 256 characters used to remember the last directory.
        ///         Use a mod-qualified purpose key to keep unrelated pickers separate. Favorites and recent directories
        ///         are shared across pickers on this device and are not synchronized to other devices.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于记住上次目录的非空白键，最多 256 个字符。使用带模组前缀的用途键可区分不同选择器。
        ///         收藏及最近目录由本设备上的选择器共用，不同步到其他设备。
        ///     </para>
        /// </summary>
        public string StateKey { get; init; } = "default";

        /// <summary>
        ///     <para xml:lang="en">
        ///         An optional initial filesystem directory. An existing explicit directory takes precedence over the
        ///         remembered directory; otherwise the picker uses the remembered directory or the user's home directory.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的初始文件系统目录。显式提供且存在的目录优先于记忆目录；
        ///         否则使用记忆目录或用户主目录。
        ///     </para>
        /// </summary>
        public string? InitialDirectory { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The initial file name, without directory components; empty leaves the field blank.</para>
        ///     <para xml:lang="zh-CN">不包含目录部分的初始文件名；空字符串使文件名字段保持空白。</para>
        /// </summary>
        public string InitialFile { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Up to 128 filters in Godot's "patterns;description" form, such as "*.png,*.jpg;Images".
        ///         Each filter may contain at most 512 characters. Empty selects all files.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         最多 128 个采用 Godot“模式;说明”格式的筛选项，例如“*.png,*.jpg;Images”；
        ///         每项最多 512 个字符。空列表表示所有文件。
        ///     </para>
        /// </summary>
        public IReadOnlyList<string> Filters { get; init; } = [];

        internal RitsuFileDialogOptions Snapshot()
        {
            if (Mode is < FileDialog.FileModeEnum.OpenFile or > FileDialog.FileModeEnum.SaveFile)
                throw new ArgumentOutOfRangeException(nameof(Mode));
            ArgumentException.ThrowIfNullOrWhiteSpace(StateKey);
            if (StateKey.Length > 256)
                throw new ArgumentOutOfRangeException(nameof(StateKey));
            if (Title != null && (string.IsNullOrWhiteSpace(Title) || Title.Length > 256))
                throw new ArgumentException("The title must contain 1 to 256 nonblank characters.", nameof(Title));
            ArgumentNullException.ThrowIfNull(InitialFile);
            if (InitialFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || InitialFile.Contains('/') ||
                InitialFile.Contains('\\'))
                throw new ArgumentException("Expected a file name without directory components.", nameof(InitialFile));
            ArgumentNullException.ThrowIfNull(Filters);
            if (Filters.Count > 128 || Filters.Any(filter => string.IsNullOrWhiteSpace(filter) || filter.Length > 512 ||
                                                             filter.Split(';')[0].Split(',',
                                                                 StringSplitOptions.TrimEntries |
                                                                 StringSplitOptions.RemoveEmptyEntries).Length == 0))
                throw new ArgumentException("Invalid file filters.", nameof(Filters));
            return new()
            {
                Title = Title,
                Mode = Mode,
                StateKey = StateKey,
                InitialDirectory = InitialDirectory,
                InitialFile = InitialFile,
                Filters = [.. Filters],
            };
        }
    }
}
