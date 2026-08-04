namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies options for exporting compendium detail views for relics and potions to PNG.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定将遗物和药水的图鉴详情视图批量导出为 PNG 的选项。
    ///     </para>
    /// </summary>
    public readonly struct CompendiumPngExportRequest
    {
        /// <summary>
        ///     <para xml:lang="en">The absolute or Godot <c>user://</c> or <c>res://</c> output directory.</para>
        ///     <para xml:lang="zh-CN">绝对路径或 Godot <c>user://</c>、<c>res://</c> 输出目录。</para>
        /// </summary>
        public required string OutputDirectory { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The uniform output scale.</para>
        ///     <para xml:lang="zh-CN">统一输出缩放比例。</para>
        /// </summary>
        public double Scale { get; init; }

        /// <summary>
        ///     <para xml:lang="en">An optional case-insensitive model-ID substring filter.</para>
        ///     <para xml:lang="zh-CN">可选的忽略大小写模型 ID 子字符串筛选条件。</para>
        /// </summary>
        public string? IdFilterSubstring { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Whether to export relic inspection views.</para>
        ///     <para xml:lang="zh-CN">是否导出遗物查看视图。</para>
        /// </summary>
        public bool Relics { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Whether to export potion-lab focus views with hover tips.</para>
        ///     <para xml:lang="zh-CN">是否导出包含悬停提示的药水实验室聚焦视图。</para>
        /// </summary>
        public bool Potions { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Whether relic exports include hover-tip columns.</para>
        ///     <para xml:lang="zh-CN">遗物导出是否包含悬停提示列。</para>
        /// </summary>
        public bool IncludeRelicHoverTips { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Whether output filenames use each relic or potion's localized title in the language active when the
        ///         export starts. Missing or unusable titles fall back to the model ID, and duplicate titles receive a
        ///         stable disambiguating suffix.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         输出文件名是否使用导出开始时当前语言下的遗物或药水标题。标题缺失或无法用于文件名时会回退到模型 ID，重复标题会附加稳定的
        ///         区分后缀。
        ///     </para>
        /// </summary>
        public bool UseLocalizedFileNames { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a request with scale <c>1</c> and both relic and potion export enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建缩放比例为 <c>1</c>且启用遗物和药水导出的请求。
        ///     </para>
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
