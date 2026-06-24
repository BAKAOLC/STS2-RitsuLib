namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    /// <summary>
    ///     Batch export of compendium-style detail panels (relic inspect popup, potion lab focus + hovers) to PNG.
    ///     将概要风格详情面板（遗物查看弹窗、药水实验室聚焦 + 悬停）批量导出为 PNG。
    /// </summary>
    public readonly struct CompendiumPngExportRequest
    {
        /// <summary>
        ///     Absolute or Godot <c>user://</c> / <c>res://</c> output directory.
        ///     绝对路径或 Godot <c>user://</c> / <c>res://</c> 输出目录。
        /// </summary>
        public required string OutputDirectory { get; init; }

        /// <summary>
        ///     Uniform scale (same as card export).
        ///     统一缩放（与卡牌导出相同）。
        /// </summary>
        public double Scale { get; init; }

        /// <summary>
        ///     Optional id substring filter; null / empty = all.
        ///     可选 id 子串筛选器；null/空 = 全部。
        /// </summary>
        public string? IdFilterSubstring { get; init; }

        /// <summary>
        ///     Export the relic inspection popup (same <c>inspect_relic_screen</c> <c>Popup</c> content as the main menu
        ///     compendium when an entry is selected).
        ///     导出遗物查看弹窗（与主菜单概要中选中条目时相同的 <c>inspect_relic_screen</c> <c>Popup</c> 内容）。
        /// </summary>
        public bool Relics { get; init; }

        /// <summary>
        ///     Export the potion lab “focus” view (1.2x <c>NPotion</c> + character pool outline, with hover tips to the
        ///     right).
        ///     导出药水实验室“聚焦”视图（1.2x <c>NPotion</c> + 角色池轮廓，悬停提示位于
        ///     右侧）。
        /// </summary>
        public bool Potions { get; init; }

        /// <summary>
        ///     When true, relic export includes the secondary hover columns.
        ///     为 true 时，遗物导出包含次级悬停列。
        /// </summary>
        public bool IncludeRelicHoverTips { get; init; }

        /// <summary>
        ///     Default <see cref="Scale" /> = 1; both <see cref="Relics" /> and <see cref="Potions" /> true.
        ///     默认 <see cref="Scale" /> = 1；<see cref="Relics" /> 和 <see cref="Potions" /> 均为 true。
        /// </summary>
        public static CompendiumPngExportRequest CreateDefault(string outputDirectory)
        {
            return new()
            {
                OutputDirectory = outputDirectory,
                Scale = 1.0,
                Relics = true,
                Potions = true,
                IncludeRelicHoverTips = true,
            };
        }
    }
}
