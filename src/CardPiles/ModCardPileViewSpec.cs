using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Configures optional capabilities and styling for the default pile screen.</para>
    ///     <para xml:lang="zh-CN">配置默认牌堆界面的可选能力与样式。</para>
    /// </summary>
    public sealed record ModCardPileViewSpec
    {
        private static readonly ModCardPileSortOption[] DefaultSortOptions =
        [
            ModCardPileSortOption.Obtained,
            ModCardPileSortOption.Type,
            ModCardPileSortOption.Cost,
            ModCardPileSortOption.Alphabetical,
        ];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets a specification that enables card inspection, upgrade previews, and the standard
        ///         deck-view sorting options.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取启用卡牌检查、升级预览与标准牌组查看排序选项的规范。
        ///     </para>
        /// </summary>
        public static ModCardPileViewSpec DeckLike { get; } = new()
        {
            EnableCardInspect = true,
            EnableUpgradePreviewToggle = true,
            EnableSortBar = true,
        };

        /// <summary>
        ///     <para xml:lang="en">Gets whether clicking a grid card opens the vanilla inspection screen.</para>
        ///     <para xml:lang="zh-CN">获取点击网格卡牌时是否打开原版卡牌检查界面。</para>
        /// </summary>
        public bool EnableCardInspect { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the screen includes an upgrade-preview toggle.</para>
        ///     <para xml:lang="zh-CN">获取界面是否包含升级预览开关。</para>
        /// </summary>
        public bool EnableUpgradePreviewToggle { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the screen includes a deck-view sort bar. Sorting does not change pile order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取界面是否包含牌组查看样式的排序栏。排序不会改变牌堆顺序。
        ///     </para>
        /// </summary>
        public bool EnableSortBar { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the sort buttons shown by the sort bar. <see langword="null" /> or an empty list uses
        ///         obtained, type, cost, and alphabetical sorting.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取排序栏显示的排序按钮。<see langword="null" /> 或空列表会使用获得顺序、类型、费用与
        ///         字母顺序排序。
        ///     </para>
        /// </summary>
        public IReadOnlyList<ModCardPileSortOption>? SortOptions { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the initial sorting priorities. <see langword="null" /> or an empty list uses ascending
        ///         pile order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取初始排序优先级。<see langword="null" /> 或空列表会使用牌堆正序。
        ///     </para>
        /// </summary>
        public IReadOnlyList<SortingOrders>? DefaultSorting { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional toolbar background texture path. <see langword="null" /> uses the vanilla
        ///         deck-view tab texture.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取工具栏背景的可选贴图路径。<see langword="null" /> 时使用原版牌组查看标签贴图。
        ///     </para>
        /// </summary>
        public string? ToolbarBackgroundTexturePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional material applied to the toolbar background.</para>
        ///     <para xml:lang="zh-CN">获取应用于工具栏背景的可选材质。</para>
        /// </summary>
        public Material? ToolbarBackgroundMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime provider for the toolbar background material. Its result takes
        ///         precedence over <see cref="ToolbarBackgroundMaterial" /> when non-null.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取工具栏背景材质的可选运行时提供器。其结果非空时优先于
        ///         <see cref="ToolbarBackgroundMaterial" />。
        ///     </para>
        /// </summary>
        public Func<ModCardPileViewStyleContext, Material?>? ToolbarBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional HSV shader material used to tint sort buttons.</para>
        ///     <para xml:lang="zh-CN">获取用于为排序按钮着色的可选 HSV 着色器材质。</para>
        /// </summary>
        public ShaderMaterial? SortButtonHueMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime provider for the sort-button tint material. Its result takes
        ///         precedence over <see cref="SortButtonHueMaterial" /> when non-null.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取排序按钮着色材质的可选运行时提供器。其结果非空时优先于
        ///         <see cref="SortButtonHueMaterial" />。
        ///     </para>
        /// </summary>
        public Func<ModCardPileViewStyleContext, ShaderMaterial?>? SortButtonHueMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether automatic sort-button hue assignment is disabled.</para>
        ///     <para xml:lang="zh-CN">获取是否禁用排序按钮的自动色相设置。</para>
        /// </summary>
        public bool DisableSortButtonHue { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional background texture path for each sort button.</para>
        ///     <para xml:lang="zh-CN">获取各排序按钮的可选背景贴图路径。</para>
        /// </summary>
        public string? SortButtonBackgroundTexturePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional material applied to each sort-button background.</para>
        ///     <para xml:lang="zh-CN">获取应用于各排序按钮背景的可选材质。</para>
        /// </summary>
        public Material? SortButtonBackgroundMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime provider for each sort-button background material. Its result
        ///         takes precedence over <see cref="SortButtonBackgroundMaterial" /> when non-null.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取各排序按钮背景材质的可选运行时提供器。其结果非空时优先于
        ///         <see cref="SortButtonBackgroundMaterial" />。
        ///     </para>
        /// </summary>
        public Func<ModCardPileViewStyleContext, Material?>? SortButtonBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional text color of the upgrade-preview toggle label.</para>
        ///     <para xml:lang="zh-CN">获取升级预览开关标签的可选文字颜色。</para>
        /// </summary>
        public Color? UpgradePreviewLabelColor { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional outline color of the upgrade-preview toggle label.</para>
        ///     <para xml:lang="zh-CN">获取升级预览开关标签的可选描边颜色。</para>
        /// </summary>
        public Color? UpgradePreviewLabelOutlineColor { get; init; }

        internal bool HasAnyCapability => EnableCardInspect || EnableUpgradePreviewToggle || EnableSortBar;

        internal IReadOnlyList<ModCardPileSortOption> GetSortOptions()
        {
            return SortOptions is { Count: > 0 } ? SortOptions : DefaultSortOptions;
        }

        internal List<SortingOrders> CreateDefaultSorting()
        {
            return DefaultSorting is { Count: > 0 } ? [.. DefaultSorting] : [SortingOrders.Ascending];
        }
    }
}
