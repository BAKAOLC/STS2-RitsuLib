namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies options for exporting <see cref="MegaCrit.Sts2.Core.Models.CardModel" /> instances to PNG.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定将 <see cref="MegaCrit.Sts2.Core.Models.CardModel" /> 实例批量导出为 PNG 的选项。
    ///     </para>
    /// </summary>
    public readonly struct CardPngExportRequest
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The absolute or Godot <c>user://</c> or <c>res://</c> output directory. Invalid filename characters
        ///         in card IDs are replaced.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         绝对路径或 Godot <c>user://</c>、<c>res://</c> 输出目录。卡牌 ID 中不适用于文件名的字符会被替换。
        ///     </para>
        /// </summary>
        public string OutputDirectory { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         The uniform scale applied to the card and surrounding layout. Values below <c>1</c> shrink the
        ///         output; values above <c>1</c> enlarge it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用于卡牌及周边布局的统一缩放比例。小于 <c>1</c> 时缩小，大于 <c>1</c> 时放大。
        ///     </para>
        /// </summary>
        public float Scale { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The capture mode.</para>
        ///     <para xml:lang="zh-CN">捕获模式。</para>
        /// </summary>
        public CardPngExportCaptureMode CaptureMode { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether to export an additional <c>_upgraded</c> PNG for each upgradable card.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         是否为每张可升级卡牌额外导出一个 <c>_upgraded</c> PNG。
        ///     </para>
        /// </summary>
        public bool IncludeUpgradedVariants { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         An optional case-insensitive substring filter applied to
        ///         <see cref="MegaCrit.Sts2.Core.Models.ModelId.Entry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的忽略大小写子字符串筛选条件，应用于
        ///         <see cref="MegaCrit.Sts2.Core.Models.ModelId.Entry" />。
        ///     </para>
        /// </summary>
        public string? IdFilterSubstring { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum number of base cards to export when positive. Upgraded variants do not count toward this
        ///         limit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为正数时表示最多导出的基础卡牌数；升级版本不计入此限制。
        ///     </para>
        /// </summary>
        public int MaxBaseCards { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether to include registered cards hidden from the in-game card library.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         是否包含未显示在游戏内卡牌图鉴中的已注册卡牌。
        ///     </para>
        /// </summary>
        public bool IncludeCardsHiddenFromLibrary { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether output filenames use each card's localized title in the language active when the export
        ///         starts. Missing or unusable titles fall back to the model ID, and duplicate titles receive a stable
        ///         disambiguating suffix.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         输出文件名是否使用导出开始时当前语言下的卡牌标题。标题缺失或无法用于文件名时会回退到模型 ID，重复标题会附加稳定的
        ///         区分后缀。
        ///     </para>
        /// </summary>
        public bool UseLocalizedFileNames { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a request with scale <c>1</c>, card-only capture, and upgraded variants enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建缩放比例为 <c>1</c>、仅捕获卡牌且包含升级版本的请求。
        ///     </para>
        /// </summary>
        public static CardPngExportRequest CreateDefault(string outputDirectory)
        {
            return new()
            {
                OutputDirectory = outputDirectory,
                Scale = 1f,
                CaptureMode = CardPngExportCaptureMode.CardOnly,
                IncludeUpgradedVariants = true,
                MaxBaseCards = 0,
            };
        }
    }
}
