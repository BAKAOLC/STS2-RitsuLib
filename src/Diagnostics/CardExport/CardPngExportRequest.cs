namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     Parameters for a batch PNG export of <see cref="MegaCrit.Sts2.Core.Models.CardModel" /> instances.
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel" /> 实例批量 PNG 导出的参数。
    /// </summary>
    public readonly struct CardPngExportRequest
    {
        /// <summary>
        ///     Absolute or Godot <c>user://</c> / <c>res://</c> output directory. Invalid path characters in card ids are
        ///     stripped from file names.
        ///     绝对路径或 Godot <c>user://</c> / <c>res://</c> 输出目录。卡牌 id 中的无效路径字符会
        ///     从文件名中剔除。
        /// </summary>
        public string OutputDirectory { get; init; }

        /// <summary>
        ///     Uniform scale applied to the card (and panel layout). Values below 1 shrink; above 1 enlarge (e.g. 2 for
        ///     higher-resolution exports).
        ///     应用到卡牌（和面板布局）的统一缩放。小于 1 会缩小，大于 1 会放大（例如 2 用于
        ///     更高分辨率导出）。
        /// </summary>
        public float Scale { get; init; }

        /// <summary>
        ///     Rasterization mode.
        ///     栅格化模式。
        /// </summary>
        public CardPngExportCaptureMode CaptureMode { get; init; }

        /// <summary>
        ///     When true, also exports an <c>_upgraded</c> PNG for cards where
        ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.IsUpgradable" /> is true.
        ///     为 true 时，还会为
        ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.IsUpgradable" /> 为 true 的卡牌导出 <c>_upgraded</c> PNG。
        /// </summary>
        public bool IncludeUpgradedVariants { get; init; }

        /// <summary>
        ///     When set, only cards whose <see cref="MegaCrit.Sts2.Core.Models.ModelId.Entry" /> contains this substring
        ///     (ordinal ignore-case) are exported.
        ///     设置后，仅导出 <see cref="MegaCrit.Sts2.Core.Models.ModelId.Entry" /> 包含此子串的卡牌
        ///     （ordinal ignore-case）。
        /// </summary>
        public string? IdFilterSubstring { get; init; }

        /// <summary>
        ///     When positive, stops after this many <em>base</em> cards (upgraded variants do not count toward the cap).
        ///     为正数时，在导出这么多张 <em>基础</em>卡牌后停止（升级变体不计入上限）。
        /// </summary>
        public int MaxBaseCards { get; init; }

        /// <summary>
        ///     When false (default), only exports cards that appear in the in-game card library
        ///     (<see cref="MegaCrit.Sts2.Core.Models.CardModel.ShouldShowInCardLibrary" />), matching the compendium set.
        ///     When true, also includes registered cards that are hidden from the library.
        ///     为 false（默认）时，只导出游戏内卡牌库中出现的卡牌
        ///     （<see cref="MegaCrit.Sts2.Core.Models.CardModel.ShouldShowInCardLibrary" />），与概要集合匹配。
        ///     为 true 时，还会包含已注册但从库中隐藏的卡牌。
        /// </summary>
        public bool IncludeCardsHiddenFromLibrary { get; init; }

        /// <summary>
        ///     Defaults: <see cref="Scale" /> = 1, <see cref="CaptureMode" /> = <see cref="CardPngExportCaptureMode.CardOnly" />,
        ///     <see cref="IncludeUpgradedVariants" /> = true.
        ///     默认值：<see cref="Scale" /> = 1，<see cref="CaptureMode" /> = <see cref="CardPngExportCaptureMode.CardOnly" />，
        ///     <see cref="IncludeUpgradedVariants" /> = true。
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
