using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Optional capabilities for the default mod card-pile screen. Leave null on
    ///     <see cref="ModCardPileSpec.View" /> to preserve legacy vanilla <c>NCardPileScreen</c> behavior.
    ///     默认 mod 牌堆 screen 的可选能力。<see cref="ModCardPileSpec.View" /> 保持 null 时保留旧的
    ///     原版 <c>NCardPileScreen</c> 行为。
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
        ///     Deck-like option set: card inspection, upgrade preview toggle, and obtained/type/cost/alphabet sorting.
        ///     类似牌组查看的选项：卡牌检查、升级预览开关，以及获得顺序/类型/费用/字母排序。
        /// </summary>
        public static ModCardPileViewSpec DeckLike { get; } = new()
        {
            EnableCardInspect = true,
            EnableUpgradePreviewToggle = true,
            EnableSortBar = true,
        };

        /// <summary>
        ///     When true, clicking a grid card opens the vanilla card inspection screen.
        ///     为 true 时，点击网格中的卡牌会打开原版卡牌检查界面。
        /// </summary>
        public bool EnableCardInspect { get; init; }

        /// <summary>
        ///     When true, adds a view-upgrades tickbox and forwards its state to <c>NCardGrid.IsShowingUpgrades</c>.
        ///     为 true 时，添加查看升级版开关，并将状态转发给 <c>NCardGrid.IsShowingUpgrades</c>。
        /// </summary>
        public bool EnableUpgradePreviewToggle { get; init; }

        /// <summary>
        ///     When true, adds a deck-view-style sort bar. Sorting is visual only; it does not mutate pile order.
        ///     为 true 时，添加类似牌组查看的排序栏。排序只影响显示，不改变牌堆顺序。
        /// </summary>
        public bool EnableSortBar { get; init; }

        /// <summary>
        ///     Sort buttons shown when <see cref="EnableSortBar" /> is true. Null or empty uses the deck-like
        ///     obtained/type/cost/alphabet set.
        ///     <see cref="EnableSortBar" /> 为 true 时显示的排序按钮。null 或空集合使用类似牌组查看的
        ///     获得顺序/类型/费用/字母集合。
        /// </summary>
        public IReadOnlyList<ModCardPileSortOption>? SortOptions { get; init; }

        /// <summary>
        ///     Initial sorting priority used by the optional view. Null uses ascending pile order.
        ///     可选查看界面的初始排序优先级。null 使用牌堆正序。
        /// </summary>
        public IReadOnlyList<SortingOrders>? DefaultSorting { get; init; }

        /// <summary>
        ///     Optional texture path for the sort/upgrade toolbar background. Null uses the vanilla deck-view
        ///     tab texture.
        ///     排序/升级工具栏背景的可选贴图路径。null 使用原版牌组查看的 tab 贴图。
        /// </summary>
        public string? ToolbarBackgroundTexturePath { get; init; }

        /// <summary>
        ///     Optional material applied to the toolbar background texture.
        ///     应用到工具栏背景贴图上的可选材质。
        /// </summary>
        public Material? ToolbarBackgroundMaterial { get; init; }

        /// <summary>
        ///     Optional runtime material provider for the toolbar background. When non-null, it takes
        ///     precedence over <see cref="ToolbarBackgroundMaterial" />.
        ///     工具栏背景的可选运行时材质 provider。非 null 时优先于 <see cref="ToolbarBackgroundMaterial" />。
        /// </summary>
        public Func<ModCardPileViewStyleContext, Material?>? ToolbarBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     Optional HSV shader material used to hue sort buttons, matching vanilla
        ///     <c>NCardViewSortButton.SetHue</c> semantics.
        ///     用于给排序按钮染色的可选 HSV shader 材质，语义与原版
        ///     <c>NCardViewSortButton.SetHue</c> 一致。
        /// </summary>
        public ShaderMaterial? SortButtonHueMaterial { get; init; }

        /// <summary>
        ///     Optional runtime provider for the sort-button hue material. When non-null, it takes
        ///     precedence over <see cref="SortButtonHueMaterial" />.
        ///     排序按钮 hue 材质的可选运行时 provider。非 null 时优先于
        ///     <see cref="SortButtonHueMaterial" />。
        /// </summary>
        public Func<ModCardPileViewStyleContext, ShaderMaterial?>? SortButtonHueMaterialProvider { get; init; }

        /// <summary>
        ///     When true, the optional view will not call <c>NCardViewSortButton.SetHue</c> on sort buttons.
        ///     为 true 时，可选查看界面不会对排序按钮调用 <c>NCardViewSortButton.SetHue</c>。
        /// </summary>
        public bool DisableSortButtonHue { get; init; }

        /// <summary>
        ///     Optional texture path applied to each sort button's background image.
        ///     应用到每个排序按钮背景图的可选贴图路径。
        /// </summary>
        public string? SortButtonBackgroundTexturePath { get; init; }

        /// <summary>
        ///     Optional material applied to each sort button's background image.
        ///     应用到每个排序按钮背景图的可选材质。
        /// </summary>
        public Material? SortButtonBackgroundMaterial { get; init; }

        /// <summary>
        ///     Optional runtime material provider for each sort button's background image. When non-null,
        ///     it takes precedence over <see cref="SortButtonBackgroundMaterial" />.
        ///     每个排序按钮背景图的可选运行时材质 provider。非 null 时优先于
        ///     <see cref="SortButtonBackgroundMaterial" />。
        /// </summary>
        public Func<ModCardPileViewStyleContext, Material?>? SortButtonBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     Optional text color for the upgrade-preview toggle label.
        ///     升级预览开关标签的可选文字颜色。
        /// </summary>
        public Color? UpgradePreviewLabelColor { get; init; }

        /// <summary>
        ///     Optional outline color for the upgrade-preview toggle label.
        ///     升级预览开关标签的可选描边颜色。
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
