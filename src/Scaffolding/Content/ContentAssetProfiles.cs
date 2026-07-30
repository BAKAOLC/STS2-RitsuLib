using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides runtime context to card-pool deck-view style callbacks.</para>
    ///     <para xml:lang="zh-CN">为牌池的牌组查看界面样式回调提供运行时上下文。</para>
    /// </summary>
    public sealed record CardPoolDeckViewStyleContext(
        Player Player,
        CharacterModel Character,
        CardPoolModel CardPool,
        NDeckViewScreen Screen);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional visual overrides for the base deck-view screen when it displays a deck associated
    ///         with this card pool.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义游戏本体的牌组查看界面显示此牌池所属牌组时使用的可选外观替换。
    ///     </para>
    /// </summary>
    public sealed record CardPoolDeckViewStyle
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional sorting-toolbar background texture path. <see langword="null" /> preserves the
        ///         scene default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的排序工具栏背景纹理路径；<see langword="null" /> 表示保留场景默认值。
        ///     </para>
        /// </summary>
        public string? ToolbarBackgroundTexturePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional material applied to the sorting-toolbar background.</para>
        ///     <para xml:lang="zh-CN">获取应用到排序工具栏背景的可选材质。</para>
        /// </summary>
        public Material? ToolbarBackgroundMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime material callback for the sorting-toolbar background. A non-null result
        ///         takes precedence over <see cref="ToolbarBackgroundMaterial" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取排序工具栏背景的可选运行时材质回调。回调返回非空值时优先于
        ///         <see cref="ToolbarBackgroundMaterial" />。
        ///     </para>
        /// </summary>
        public Func<CardPoolDeckViewStyleContext, Material?>? ToolbarBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional HSV shader material used to tint sort buttons.</para>
        ///     <para xml:lang="zh-CN">获取用于为排序按钮着色的可选 HSV 着色器材质。</para>
        /// </summary>
        public ShaderMaterial? SortButtonHueMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime callback for the sort-button tint material. A non-null result takes
        ///         precedence over <see cref="SortButtonHueMaterial" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取排序按钮着色材质的可选运行时回调。回调返回非空值时优先于
        ///         <see cref="SortButtonHueMaterial" />。
        ///     </para>
        /// </summary>
        public Func<CardPoolDeckViewStyleContext, ShaderMaterial?>? SortButtonHueMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether RitsuLib should skip <c>NCardViewSortButton.SetHue</c> while applying this style.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 RitsuLib 应用此样式时是否跳过 <c>NCardViewSortButton.SetHue</c>。
        ///     </para>
        /// </summary>
        public bool? DisableSortButtonHue { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional background texture path applied to each sort button.</para>
        ///     <para xml:lang="zh-CN">获取应用到每个排序按钮的可选背景纹理路径。</para>
        /// </summary>
        public string? SortButtonBackgroundTexturePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional background material applied to each sort button.</para>
        ///     <para xml:lang="zh-CN">获取应用到每个排序按钮的可选背景材质。</para>
        /// </summary>
        public Material? SortButtonBackgroundMaterial { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional runtime callback for each sort button's background material. A non-null result
        ///         takes precedence over <see cref="SortButtonBackgroundMaterial" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取每个排序按钮背景材质的可选运行时回调。回调返回非空值时优先于
        ///         <see cref="SortButtonBackgroundMaterial" />。
        ///     </para>
        /// </summary>
        public Func<CardPoolDeckViewStyleContext, Material?>? SortButtonBackgroundMaterialProvider { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional text color for the upgrade-preview toggle label.</para>
        ///     <para xml:lang="zh-CN">获取升级预览开关标签的可选文字颜色。</para>
        /// </summary>
        public Color? UpgradePreviewLabelColor { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional outline color for the upgrade-preview toggle label.</para>
        ///     <para xml:lang="zh-CN">获取升级预览开关标签的可选描边颜色。</para>
        /// </summary>
        public Color? UpgradePreviewLabelOutlineColor { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional card-pool presentation assets.</para>
    ///     <para xml:lang="zh-CN">定义可选的牌池表现资源。</para>
    /// </summary>
    /// <param name="DeckViewStyle">
    ///     <para xml:lang="en">The optional deck-view screen style.</para>
    ///     <para xml:lang="zh-CN">可选的牌组查看界面样式。</para>
    /// </param>
    public sealed record CardPoolAssetProfile(CardPoolDeckViewStyle? DeckViewStyle = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no card-pool presentation overrides.</para>
        ///     <para xml:lang="zh-CN">获取不包含牌池表现替换的空配置。</para>
        /// </summary>
        public static CardPoolAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Provides merge operations for <see cref="CardPoolAssetProfile" />.</para>
    ///     <para xml:lang="zh-CN">提供 <see cref="CardPoolAssetProfile" /> 的合并操作。</para>
    /// </summary>
    public static class CardPoolAssetProfiles
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges profiles field by field, preferring non-null values from <paramref name="profile" /> and
        ///         falling back to <paramref name="fallback" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         逐字段合并配置，优先使用 <paramref name="profile" /> 中的非空值，否则回退到
        ///         <paramref name="fallback" />。
        ///     </para>
        /// </summary>
        public static CardPoolAssetProfile Merge(CardPoolAssetProfile? fallback, CardPoolAssetProfile? profile)
        {
            fallback ??= CardPoolAssetProfile.Empty;
            profile ??= CardPoolAssetProfile.Empty;

            return new(MergeDeckViewStyle(fallback.DeckViewStyle, profile.DeckViewStyle));
        }

        private static CardPoolDeckViewStyle? MergeDeckViewStyle(
            CardPoolDeckViewStyle? fallback,
            CardPoolDeckViewStyle? profile)
        {
            if (fallback == null)
                return profile;

            if (profile == null)
                return fallback;

            return new()
            {
                ToolbarBackgroundTexturePath =
                    profile.ToolbarBackgroundTexturePath ?? fallback.ToolbarBackgroundTexturePath,
                ToolbarBackgroundMaterial = profile.ToolbarBackgroundMaterial ?? fallback.ToolbarBackgroundMaterial,
                ToolbarBackgroundMaterialProvider =
                    profile.ToolbarBackgroundMaterialProvider ?? fallback.ToolbarBackgroundMaterialProvider,
                SortButtonHueMaterial = profile.SortButtonHueMaterial ?? fallback.SortButtonHueMaterial,
                SortButtonHueMaterialProvider =
                    profile.SortButtonHueMaterialProvider ?? fallback.SortButtonHueMaterialProvider,
                DisableSortButtonHue = profile.DisableSortButtonHue ?? fallback.DisableSortButtonHue,
                SortButtonBackgroundTexturePath =
                    profile.SortButtonBackgroundTexturePath ?? fallback.SortButtonBackgroundTexturePath,
                SortButtonBackgroundMaterial =
                    profile.SortButtonBackgroundMaterial ?? fallback.SortButtonBackgroundMaterial,
                SortButtonBackgroundMaterialProvider =
                    profile.SortButtonBackgroundMaterialProvider ?? fallback.SortButtonBackgroundMaterialProvider,
                UpgradePreviewLabelColor = profile.UpgradePreviewLabelColor ?? fallback.UpgradePreviewLabelColor,
                UpgradePreviewLabelOutlineColor =
                    profile.UpgradePreviewLabelOutlineColor ?? fallback.UpgradePreviewLabelOutlineColor,
            };
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the card layout used by <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCard" />.</para>
    ///     <para xml:lang="zh-CN">指定 <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCard" /> 使用的卡牌布局。</para>
    /// </summary>
    public enum CardVisualStyle
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses the base rarity check, under which <see cref="CardRarity.Ancient" /> cards use the Ancient
        ///         layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用游戏本体的稀有度判定，其中 <see cref="CardRarity.Ancient" /> 卡牌使用先古卡牌布局。
        ///     </para>
        /// </summary>
        Default,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses the standard card layout regardless of whether the rarity is
        ///         <see cref="CardRarity.Ancient" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         无论稀有度是否为 <see cref="CardRarity.Ancient" />，都使用普通卡牌布局。
        ///     </para>
        /// </summary>
        Standard,

        /// <summary>
        ///     <para xml:lang="en">Uses the Ancient card layout without changing the card's gameplay rarity.</para>
        ///     <para xml:lang="zh-CN">使用先古卡牌布局，但不改变卡牌在游戏逻辑中的稀有度。</para>
        /// </summary>
        Ancient,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional paths and materials for a mod card's portrait, frame, energy icon, overlay, banner,
    ///         and Ancient-layout elements.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义模组卡牌的肖像、边框、能量图标、覆盖层、横幅和先古卡牌布局元素所使用的可选路径与材质。
    ///     </para>
    /// </summary>
    /// <param name="PortraitPath">
    ///     <para xml:lang="en">The main card portrait texture path.</para>
    ///     <para xml:lang="zh-CN">卡牌主肖像纹理路径。</para>
    /// </param>
    /// <param name="BetaPortraitPath">
    ///     <para xml:lang="en">The optional beta-art portrait texture path.</para>
    ///     <para xml:lang="zh-CN">可选的测试版卡图肖像纹理路径。</para>
    /// </param>
    /// <param name="FramePath">
    ///     <para xml:lang="en">The card-frame texture path.</para>
    ///     <para xml:lang="zh-CN">卡牌边框纹理路径。</para>
    /// </param>
    /// <param name="PortraitBorderPath">
    ///     <para xml:lang="en">The portrait-border texture path.</para>
    ///     <para xml:lang="zh-CN">肖像边框纹理路径。</para>
    /// </param>
    /// <param name="EnergyIconPath">
    ///     <para xml:lang="en">The card's energy-icon texture path.</para>
    ///     <para xml:lang="zh-CN">卡牌的能量图标纹理路径。</para>
    /// </param>
    /// <param name="FrameMaterialPath">
    ///     <para xml:lang="en">The card-frame material resource path.</para>
    ///     <para xml:lang="zh-CN">卡牌边框材质资源路径。</para>
    /// </param>
    /// <param name="OverlayScenePath">
    ///     <para xml:lang="en">The packed-scene path for the card overlay.</para>
    ///     <para xml:lang="zh-CN">卡牌覆盖层的打包场景路径。</para>
    /// </param>
    /// <param name="BannerTexturePath">
    ///     <para xml:lang="en">The card-banner texture path.</para>
    ///     <para xml:lang="zh-CN">卡牌横幅纹理路径。</para>
    /// </param>
    /// <param name="BannerMaterialPath">
    ///     <para xml:lang="en">The card-banner material resource path.</para>
    ///     <para xml:lang="zh-CN">卡牌横幅材质资源路径。</para>
    /// </param>
    /// <param name="FrameMaterial">
    ///     <para xml:lang="en">The direct card-frame material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的卡牌边框材质。</para>
    /// </param>
    /// <param name="BannerMaterial">
    ///     <para xml:lang="en">The direct card-banner material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的卡牌横幅材质。</para>
    /// </param>
    /// <param name="PortraitMaterialPath">
    ///     <para xml:lang="en">The portrait material resource path.</para>
    ///     <para xml:lang="zh-CN">肖像材质资源路径。</para>
    /// </param>
    /// <param name="PortraitMaterial">
    ///     <para xml:lang="en">The direct portrait material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的肖像材质。</para>
    /// </param>
    /// <param name="AncientBorderPath">
    ///     <para xml:lang="en">The Ancient card-border texture path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌边框纹理路径。</para>
    /// </param>
    /// <param name="AncientTextBgPath">
    ///     <para xml:lang="en">The Ancient card text-background texture path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌文本背景纹理路径。</para>
    /// </param>
    /// <param name="PortraitBorderMaterialPath">
    ///     <para xml:lang="en">The portrait-border material resource path.</para>
    ///     <para xml:lang="zh-CN">肖像边框材质资源路径。</para>
    /// </param>
    /// <param name="PortraitBorderMaterial">
    ///     <para xml:lang="en">The direct portrait-border material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的肖像边框材质。</para>
    /// </param>
    /// <param name="EnergyIconMaterialPath">
    ///     <para xml:lang="en">The energy-icon material resource path.</para>
    ///     <para xml:lang="zh-CN">能量图标材质资源路径。</para>
    /// </param>
    /// <param name="EnergyIconMaterial">
    ///     <para xml:lang="en">The direct energy-icon material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的能量图标材质。</para>
    /// </param>
    /// <param name="AncientBorderMaterialPath">
    ///     <para xml:lang="en">The Ancient card-border material resource path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌边框材质资源路径。</para>
    /// </param>
    /// <param name="AncientBorderMaterial">
    ///     <para xml:lang="en">The direct Ancient card-border material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的先古卡牌边框材质。</para>
    /// </param>
    /// <param name="AncientTextBgMaterialPath">
    ///     <para xml:lang="en">The Ancient card text-background material resource path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌文本背景材质资源路径。</para>
    /// </param>
    /// <param name="AncientTextBgMaterial">
    ///     <para xml:lang="en">The direct Ancient card text-background material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的先古卡牌文本背景材质。</para>
    /// </param>
    /// <param name="AncientBannerPath">
    ///     <para xml:lang="en">The Ancient card title-banner texture path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌标题横幅纹理路径。</para>
    /// </param>
    /// <param name="AncientBannerMaterialPath">
    ///     <para xml:lang="en">The Ancient card title-banner material resource path.</para>
    ///     <para xml:lang="zh-CN">先古卡牌标题横幅材质资源路径。</para>
    /// </param>
    /// <param name="AncientBannerMaterial">
    ///     <para xml:lang="en">The direct Ancient card title-banner material override.</para>
    ///     <para xml:lang="zh-CN">直接指定的先古卡牌标题横幅材质。</para>
    /// </param>
    /// <param name="VisualStyle">
    ///     <para xml:lang="en">
    ///         The card-layout override. <see cref="CardVisualStyle.Default" /> preserves the base rarity check.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         卡牌布局替换。<see cref="CardVisualStyle.Default" /> 保留游戏本体的稀有度判定。
    ///     </para>
    /// </param>
    public sealed record CardAssetProfile(
        string? PortraitPath = null,
        string? BetaPortraitPath = null,
        string? FramePath = null,
        string? PortraitBorderPath = null,
        string? EnergyIconPath = null,
        string? FrameMaterialPath = null,
        string? OverlayScenePath = null,
        string? BannerTexturePath = null,
        string? BannerMaterialPath = null,
        Material? FrameMaterial = null,
        Material? BannerMaterial = null,
        string? PortraitMaterialPath = null,
        Material? PortraitMaterial = null,
        string? AncientBorderPath = null,
        string? AncientTextBgPath = null,
        string? PortraitBorderMaterialPath = null,
        Material? PortraitBorderMaterial = null,
        string? EnergyIconMaterialPath = null,
        Material? EnergyIconMaterial = null,
        string? AncientBorderMaterialPath = null,
        Material? AncientBorderMaterial = null,
        string? AncientTextBgMaterialPath = null,
        Material? AncientTextBgMaterial = null,
        string? AncientBannerPath = null,
        string? AncientBannerMaterialPath = null,
        Material? AncientBannerMaterial = null,
        CardVisualStyle VisualStyle = CardVisualStyle.Default)
    {
        /// <summary>
        ///     <para xml:lang="en">Preserves the original constructor signature for binary compatibility.</para>
        ///     <para xml:lang="zh-CN">保留原始构造函数签名以维持二进制兼容性。</para>
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the constructor signature that introduced direct frame and banner materials.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         保留引入直接边框与横幅材质时的构造函数签名。
        ///     </para>
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath,
            Material? FrameMaterial,
            Material? BannerMaterial)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                FrameMaterial,
                BannerMaterial,
                null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the constructor signature that introduced portrait materials.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         保留引入肖像材质时的构造函数签名。
        ///     </para>
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath,
            Material? FrameMaterial,
            Material? BannerMaterial,
            string? PortraitMaterialPath,
            Material? PortraitMaterial)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                FrameMaterial,
                BannerMaterial,
                PortraitMaterialPath,
                PortraitMaterial,
                null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the constructor signature that introduced Ancient-layout textures and materials.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         保留引入先古卡牌布局纹理与材质时的构造函数签名。
        ///     </para>
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath,
            Material? FrameMaterial,
            Material? BannerMaterial,
            string? PortraitMaterialPath,
            Material? PortraitMaterial,
            string? AncientBorderPath,
            string? AncientTextBgPath,
            string? PortraitBorderMaterialPath,
            Material? PortraitBorderMaterial,
            string? EnergyIconMaterialPath,
            Material? EnergyIconMaterial,
            string? AncientBorderMaterialPath,
            Material? AncientBorderMaterial,
            string? AncientTextBgMaterialPath,
            Material? AncientTextBgMaterial)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                FrameMaterial,
                BannerMaterial,
                PortraitMaterialPath,
                PortraitMaterial,
                AncientBorderPath,
                AncientTextBgPath,
                PortraitBorderMaterialPath,
                PortraitBorderMaterial,
                EnergyIconMaterialPath,
                EnergyIconMaterial,
                AncientBorderMaterialPath,
                AncientBorderMaterial,
                AncientTextBgMaterialPath,
                AncientTextBgMaterial,
                null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Preserves the constructor signature that introduced the Ancient title banner.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         保留引入先古卡牌标题横幅时的构造函数签名。
        ///     </para>
        /// </summary>
        public CardAssetProfile(
            string? PortraitPath,
            string? BetaPortraitPath,
            string? FramePath,
            string? PortraitBorderPath,
            string? EnergyIconPath,
            string? FrameMaterialPath,
            string? OverlayScenePath,
            string? BannerTexturePath,
            string? BannerMaterialPath,
            Material? FrameMaterial,
            Material? BannerMaterial,
            string? PortraitMaterialPath,
            Material? PortraitMaterial,
            string? AncientBorderPath,
            string? AncientTextBgPath,
            string? PortraitBorderMaterialPath,
            Material? PortraitBorderMaterial,
            string? EnergyIconMaterialPath,
            Material? EnergyIconMaterial,
            string? AncientBorderMaterialPath,
            Material? AncientBorderMaterial,
            string? AncientTextBgMaterialPath,
            Material? AncientTextBgMaterial,
            string? AncientBannerPath,
            string? AncientBannerMaterialPath,
            Material? AncientBannerMaterial)
            : this(
                PortraitPath,
                BetaPortraitPath,
                FramePath,
                PortraitBorderPath,
                EnergyIconPath,
                FrameMaterialPath,
                OverlayScenePath,
                BannerTexturePath,
                BannerMaterialPath,
                FrameMaterial,
                BannerMaterial,
                PortraitMaterialPath,
                PortraitMaterial,
                AncientBorderPath,
                AncientTextBgPath,
                PortraitBorderMaterialPath,
                PortraitBorderMaterial,
                EnergyIconMaterialPath,
                EnergyIconMaterial,
                AncientBorderMaterialPath,
                AncientBorderMaterial,
                AncientTextBgMaterialPath,
                AncientTextBgMaterial,
                AncientBannerPath,
                AncientBannerMaterialPath,
                AncientBannerMaterial,
                CardVisualStyle.Default)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths or materials.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径或材质的空配置。</para>
        /// </summary>
        public static CardAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional relic icon paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的遗物图标路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The primary relic icon texture path.</para>
    ///     <para xml:lang="zh-CN">遗物主图标纹理路径。</para>
    /// </param>
    /// <param name="IconOutlinePath">
    ///     <para xml:lang="en">The relic outline texture path.</para>
    ///     <para xml:lang="zh-CN">遗物轮廓纹理路径。</para>
    /// </param>
    /// <param name="BigIconPath">
    ///     <para xml:lang="en">The large relic illustration path.</para>
    ///     <para xml:lang="zh-CN">遗物大型插图路径。</para>
    /// </param>
    public sealed record RelicAssetProfile(
        string? IconPath = null,
        string? IconOutlinePath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static RelicAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional power icon paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的能力图标路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The power icon texture path.</para>
    ///     <para xml:lang="zh-CN">能力图标纹理路径。</para>
    /// </param>
    /// <param name="BigIconPath">
    ///     <para xml:lang="en">The large power illustration path.</para>
    ///     <para xml:lang="zh-CN">能力大型插图路径。</para>
    /// </param>
    public sealed record PowerAssetProfile(
        string? IconPath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static PowerAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional orb icon and combat-visuals paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的充能球图标和战斗形象路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The orb icon texture path.</para>
    ///     <para xml:lang="zh-CN">充能球图标纹理路径。</para>
    /// </param>
    /// <param name="VisualsScenePath">
    ///     <para xml:lang="en">The orb combat-visuals scene path.</para>
    ///     <para xml:lang="zh-CN">充能球战斗形象场景路径。</para>
    /// </param>
    public sealed record OrbAssetProfile(
        string? IconPath = null,
        string? VisualsScenePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static OrbAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional potion image and outline paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的药水图像和轮廓路径。</para>
    /// </summary>
    /// <param name="ImagePath">
    ///     <para xml:lang="en">The potion image texture path.</para>
    ///     <para xml:lang="zh-CN">药水图像纹理路径。</para>
    /// </param>
    /// <param name="OutlinePath">
    ///     <para xml:lang="en">The potion outline texture path.</para>
    ///     <para xml:lang="zh-CN">药水轮廓纹理路径。</para>
    /// </param>
    public sealed record PotionAssetProfile(
        string? ImagePath = null,
        string? OutlinePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static PotionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional affliction card-overlay scene path.</para>
    ///     <para xml:lang="zh-CN">定义可选的侵蚀卡牌覆盖层场景路径。</para>
    /// </summary>
    /// <param name="OverlayScenePath">
    ///     <para xml:lang="en">The affliction overlay packed-scene path.</para>
    ///     <para xml:lang="zh-CN">侵蚀覆盖层的打包场景路径。</para>
    /// </param>
    public sealed record AfflictionAssetProfile(
        string? OverlayScenePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static AfflictionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional enchantment icon path.</para>
    ///     <para xml:lang="zh-CN">定义可选的附魔图标路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The enchantment icon texture path.</para>
    ///     <para xml:lang="zh-CN">附魔图标纹理路径。</para>
    /// </param>
    public sealed record EnchantmentAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static EnchantmentAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional run-modifier icon path.</para>
    ///     <para xml:lang="zh-CN">定义可选的一局游戏修正项图标路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The modifier icon texture path.</para>
    ///     <para xml:lang="zh-CN">修正项图标纹理路径。</para>
    /// </param>
    public sealed record ModifierAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static ModifierAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional act background, map, rest-site, and treasure-chest assets.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的章节背景、地图、休息处和宝箱资源。
    ///     </para>
    /// </summary>
    /// <param name="BackgroundScenePath">
    ///     <para xml:lang="en">The main act-background scene path.</para>
    ///     <para xml:lang="zh-CN">章节主背景场景路径。</para>
    /// </param>
    /// <param name="RestSiteBackgroundPath">
    ///     <para xml:lang="en">The rest-site background scene path.</para>
    ///     <para xml:lang="zh-CN">休息处背景场景路径。</para>
    /// </param>
    /// <param name="MapTopBgPath">
    ///     <para xml:lang="en">The top act-map background texture path.</para>
    ///     <para xml:lang="zh-CN">章节地图顶层背景纹理路径。</para>
    /// </param>
    /// <param name="MapMidBgPath">
    ///     <para xml:lang="en">The middle act-map background texture path.</para>
    ///     <para xml:lang="zh-CN">章节地图中层背景纹理路径。</para>
    /// </param>
    /// <param name="MapBotBgPath">
    ///     <para xml:lang="en">The bottom act-map background texture path.</para>
    ///     <para xml:lang="zh-CN">章节地图底层背景纹理路径。</para>
    /// </param>
    /// <param name="ChestSpineResourcePath">
    ///     <para xml:lang="en">The treasure-room chest Spine resource path.</para>
    ///     <para xml:lang="zh-CN">宝藏房宝箱的 Spine 资源路径。</para>
    /// </param>
    /// <param name="BackgroundLayersDirectoryPath">
    ///     <para xml:lang="en">
    ///         The optional <c>res://</c> directory scanned using the base game's
    ///         <c>scenes/backgrounds/&lt;act&gt;/layers</c> naming rules.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的 <c>res://</c> 目录，按照游戏本体 <c>scenes/backgrounds/&lt;act&gt;/layers</c>
    ///         的命名规则扫描。
    ///     </para>
    /// </param>
    public sealed record ActAssetProfile(
        string? BackgroundScenePath = null,
        string? RestSiteBackgroundPath = null,
        string? MapTopBgPath = null,
        string? MapMidBgPath = null,
        string? MapBotBgPath = null,
        string? ChestSpineResourcePath = null,
        string? BackgroundLayersDirectoryPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static ActAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines an optional creature-visuals scene path for
    ///         <see cref="MegaCrit.Sts2.Core.Models.MonsterModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <see cref="MegaCrit.Sts2.Core.Models.MonsterModel" /> 的可选生物形象场景路径。
    ///     </para>
    /// </summary>
    /// <param name="VisualsScenePath">
    ///     <para xml:lang="en">The creature-visuals packed-scene path.</para>
    ///     <para xml:lang="zh-CN">生物形象打包场景路径。</para>
    /// </param>
    public sealed record MonsterAssetProfile(string? VisualsScenePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static MonsterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional encounter-scene, combat-background, boss-map-node, preload, and run-history assets.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的遭遇场景、战斗背景、首领地图节点、预加载和游戏历史资源。
    ///     </para>
    /// </summary>
    /// <param name="EncounterScenePath">
    ///     <para xml:lang="en">The packed scene returned by <c>EncounterModel.CreateScene</c>.</para>
    ///     <para xml:lang="zh-CN"><c>EncounterModel.CreateScene</c> 返回的打包场景路径。</para>
    /// </param>
    /// <param name="BackgroundScenePath">
    ///     <para xml:lang="en">The encounter-specific combat-background scene path.</para>
    ///     <para xml:lang="zh-CN">遭遇专属战斗背景场景路径。</para>
    /// </param>
    /// <param name="BackgroundLayersDirectoryPath">
    ///     <para xml:lang="en">The <c>res://</c> directory containing <c>_bg_</c> and <c>_fg_</c> layers.</para>
    ///     <para xml:lang="zh-CN">包含 <c>_bg_</c> 和 <c>_fg_</c> 图层的 <c>res://</c> 目录。</para>
    /// </param>
    /// <param name="BossNodeSpinePath">
    ///     <para xml:lang="en">The Spine resource path for the boss or elite map node.</para>
    ///     <para xml:lang="zh-CN">首领或精英地图节点的 Spine 资源路径。</para>
    /// </param>
    /// <param name="ExtraAssetPaths">
    ///     <para xml:lang="en">Additional paths included in <c>GetAssetPaths</c>.</para>
    ///     <para xml:lang="zh-CN">额外加入 <c>GetAssetPaths</c> 的路径。</para>
    /// </param>
    /// <param name="MapNodeAssetPaths">
    ///     <para xml:lang="en">The optional replacement for this encounter's <c>MapNodeAssetPaths</c>.</para>
    ///     <para xml:lang="zh-CN">此遭遇的可选 <c>MapNodeAssetPaths</c> 替换值。</para>
    /// </param>
    /// <param name="RunHistoryIconPath">
    ///     <para xml:lang="en">The game-history and top-bar main icon texture path.</para>
    ///     <para xml:lang="zh-CN">游戏历史和顶部栏主图标纹理路径。</para>
    /// </param>
    /// <param name="RunHistoryIconOutlinePath">
    ///     <para xml:lang="en">The game-history icon outline texture path.</para>
    ///     <para xml:lang="zh-CN">游戏历史图标轮廓纹理路径。</para>
    /// </param>
    public sealed record EncounterAssetProfile(
        string? EncounterScenePath = null,
        string? BackgroundScenePath = null,
        string? BackgroundLayersDirectoryPath = null,
        string? BossNodeSpinePath = null,
        string[]? ExtraAssetPaths = null,
        string[]? MapNodeAssetPaths = null,
        string? RunHistoryIconPath = null,
        string? RunHistoryIconOutlinePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static EncounterAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional event layout, portrait, background, and VFX scene paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的事件布局、肖像、背景和特效场景路径。</para>
    /// </summary>
    /// <param name="LayoutScenePath">
    ///     <para xml:lang="en">The packed-scene path for the event layout root.</para>
    ///     <para xml:lang="zh-CN">事件布局根节点的打包场景路径。</para>
    /// </param>
    /// <param name="InitialPortraitPath">
    ///     <para xml:lang="en">The initial portrait texture path.</para>
    ///     <para xml:lang="zh-CN">初始肖像纹理路径。</para>
    /// </param>
    /// <param name="BackgroundScenePath">
    ///     <para xml:lang="en">The background packed-scene path.</para>
    ///     <para xml:lang="zh-CN">背景打包场景路径。</para>
    /// </param>
    /// <param name="VfxScenePath">
    ///     <para xml:lang="en">The optional event-VFX packed-scene path.</para>
    ///     <para xml:lang="zh-CN">可选的事件特效打包场景路径。</para>
    /// </param>
    public sealed record EventAssetProfile(
        string? LayoutScenePath = null,
        string? InitialPortraitPath = null,
        string? BackgroundScenePath = null,
        string? VfxScenePath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static EventAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional map, game-history, and procedural stage assets for an Ancient event.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义先古之民事件可选的地图、游戏历史和程序化舞台资源。
    ///     </para>
    /// </summary>
    /// <param name="MapIconPath">
    ///     <para xml:lang="en">The map-node icon texture path.</para>
    ///     <para xml:lang="zh-CN">地图节点图标纹理路径。</para>
    /// </param>
    /// <param name="MapIconOutlinePath">
    ///     <para xml:lang="en">The map-node outline texture path.</para>
    ///     <para xml:lang="zh-CN">地图节点轮廓纹理路径。</para>
    /// </param>
    /// <param name="RunHistoryIconPath">
    ///     <para xml:lang="en">The game-history main icon texture path.</para>
    ///     <para xml:lang="zh-CN">游戏历史主图标纹理路径。</para>
    /// </param>
    /// <param name="RunHistoryIconOutlinePath">
    ///     <para xml:lang="en">The game-history icon outline texture path.</para>
    ///     <para xml:lang="zh-CN">游戏历史图标轮廓纹理路径。</para>
    /// </param>
    /// <param name="StageProcedural">
    ///     <para xml:lang="en">
    ///         The optional procedural stage that replaces the packed background in <c>NAncientEventLayout</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         用于替换 <c>NAncientEventLayout</c> 中打包背景的可选程序化舞台。
    ///     </para>
    /// </param>
    public sealed record AncientEventPresentationAssetProfile(
        string? MapIconPath = null,
        string? MapIconOutlinePath = null,
        string? RunHistoryIconPath = null,
        string? RunHistoryIconOutlinePath = null,
        AncientEventStageProceduralVisualSet? StageProcedural = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths or procedural stage.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径或程序化舞台的空配置。</para>
        /// </summary>
        public static AncientEventPresentationAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional icon path for a mod rest-site option.</para>
    ///     <para xml:lang="zh-CN">定义模组休息处选项的可选图标路径。</para>
    /// </summary>
    /// <param name="IconPath">
    ///     <para xml:lang="en">The custom icon texture path.</para>
    ///     <para xml:lang="zh-CN">自定义图标纹理路径。</para>
    /// </param>
    public sealed record RestSiteOptionAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static RestSiteOptionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional timeline epoch portrait paths.</para>
    ///     <para xml:lang="zh-CN">定义可选的时间线纪元肖像路径。</para>
    /// </summary>
    /// <param name="PackedPortraitPath">
    ///     <para xml:lang="en">The atlas-sprite resource path for the small timeline portrait.</para>
    ///     <para xml:lang="zh-CN">时间线小型肖像的图集精灵资源路径。</para>
    /// </param>
    /// <param name="BigPortraitPath">
    ///     <para xml:lang="en">The large epoch portrait texture path.</para>
    ///     <para xml:lang="zh-CN">纪元大型肖像纹理路径。</para>
    /// </param>
    public sealed record EpochAssetProfile(
        string? PackedPortraitPath = null,
        string? BigPortraitPath = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty profile with no custom paths.</para>
        ///     <para xml:lang="zh-CN">获取不包含自定义路径的空配置。</para>
        /// </summary>
        public static EpochAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides factories that build base-game <c>res://</c> asset paths from base-game folder and atlas-entry
    ///         names. These methods do not infer mod asset paths from model IDs; mod-owned assets should be supplied
    ///         explicitly through profiles.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供根据游戏本体的文件夹名称和图集条目名称创建 <c>res://</c> 资源路径的工厂。这些方法不会根据
    ///         模型 ID 推断模组资源路径；模组自有资源应通过配置显式提供。
    ///     </para>
    /// </summary>
    public static class ContentAssetProfiles
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style portrait, beta-art, and overlay paths for
        ///         <paramref name="cardEntry" /> in <paramref name="poolEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="poolEntry" /> 中的 <paramref name="cardEntry" /> 创建游戏本体风格的肖像、
        ///         测试版卡图和覆盖层路径。
        ///     </para>
        /// </summary>
        public static CardAssetProfile Card(string poolEntry, string cardEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(cardEntry);

            var normalizedPool = Normalize(poolEntry);
            var normalizedCard = Normalize(cardEntry);
            return new(
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/{normalizedCard}.png"),
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/beta/{normalizedCard}.png"),
                OverlayScenePath: SceneHelper.GetScenePath($"cards/overlays/{normalizedCard}"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style portrait, overlay, border, title-banner, and text-background paths for an
        ///         Ancient card.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为先古卡牌创建游戏本体风格的肖像、覆盖层、边框、标题横幅和文本背景路径。
        ///     </para>
        /// </summary>
        public static CardAssetProfile AncientCard(string poolEntry, string cardEntry, CardType cardType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(cardEntry);

            var normalizedPool = Normalize(poolEntry);
            var normalizedCard = Normalize(cardEntry);
            var normalizedType = NormalizeAncientCardType(cardType);
            return new(
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/{normalizedCard}.png"),
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/beta/{normalizedCard}.png"),
                OverlayScenePath: SceneHelper.GetScenePath($"cards/overlays/{normalizedCard}"),
                AncientBorderPath: ImageHelper.GetImagePath(
                    "atlases/compressed_atlas.sprites/ancient_card_border.png.tres"),
                AncientTextBgPath: ImageHelper.GetImagePath(
                    $"atlases/compressed_atlas.sprites/ancient_text_bg_{normalizedType}.png.tres"),
                AncientBannerPath: ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/ancient_banner.tres"),
                VisualStyle: CardVisualStyle.Ancient);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds base-game-style relic icon paths for <paramref name="relicEntry" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="relicEntry" /> 创建游戏本体风格的遗物图标路径。</para>
        /// </summary>
        public static RelicAssetProfile Relic(string relicEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relicEntry);

            var normalized = Normalize(relicEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/relic_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/relic_outline_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"relics/{normalized}.png"));
        }

        /// <summary>
        ///     <para xml:lang="en">Builds base-game-style power icon paths for <paramref name="powerEntry" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="powerEntry" /> 创建游戏本体风格的能力图标路径。</para>
        /// </summary>
        public static PowerAssetProfile Power(string powerEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(powerEntry);

            var normalized = Normalize(powerEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"powers/{normalized}.png"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style orb icon and combat-visuals paths for <paramref name="orbEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="orbEntry" /> 创建游戏本体风格的充能球图标和战斗形象路径。
        ///     </para>
        /// </summary>
        public static OrbAssetProfile Orb(string orbEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orbEntry);

            var normalized = Normalize(orbEntry);
            return new(
                ImageHelper.GetImagePath($"orbs/{normalized}.png"),
                SceneHelper.GetScenePath($"orbs/orb_visuals/{normalized}"));
        }

        /// <summary>
        ///     <para xml:lang="en">Builds base-game-style potion image paths for <paramref name="potionEntry" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="potionEntry" /> 创建游戏本体风格的药水图像路径。</para>
        /// </summary>
        public static PotionAssetProfile Potion(string potionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(potionEntry);

            var normalized = Normalize(potionEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/potion_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/potion_outline_atlas.sprites/{normalized}.tres"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a base-game-style affliction overlay path for <paramref name="afflictionEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="afflictionEntry" /> 创建原版游戏风格的侵蚀覆盖层路径。
        ///     </para>
        /// </summary>
        public static AfflictionAssetProfile Affliction(string afflictionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(afflictionEntry);

            var normalized = Normalize(afflictionEntry);
            return new(
                SceneHelper.GetScenePath($"cards/overlays/afflictions/{normalized}"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a base-game-style enchantment icon path for <paramref name="enchantmentEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="enchantmentEntry" /> 创建游戏本体风格的附魔图标路径。
        ///     </para>
        /// </summary>
        public static EnchantmentAssetProfile Enchantment(string enchantmentEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(enchantmentEntry);

            var normalized = Normalize(enchantmentEntry);
            return new(
                ImageHelper.GetImagePath($"enchantments/{normalized}.png"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a base-game-style run-modifier icon path for <paramref name="modifierEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modifierEntry" /> 创建游戏本体风格的一局游戏修正项图标路径。
        ///     </para>
        /// </summary>
        public static ModifierAssetProfile Modifier(string modifierEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modifierEntry);

            var normalized = Normalize(modifierEntry);
            return new(
                ImageHelper.GetImagePath($"packed/modifiers/{normalized}.png"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a complete <see cref="ActAssetProfile" /> for a base-game act folder, including the main
        ///         background, layer directory, map textures, rest site, and chest Spine resource. The argument is a
        ///         base-game folder name such as <c>hive</c>, not a mod model ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为游戏本体的章节文件夹创建完整 <see cref="ActAssetProfile" />，包括主背景、图层目录、地图纹理、
        ///         休息处和宝箱 Spine 资源。参数应为 <c>hive</c> 等游戏本体文件夹名称，而不是模组模型 ID。
        ///     </para>
        /// </summary>
        public static ActAssetProfile FromVanillaActId(string vanillaActId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vanillaActId);

            var normalized = Normalize(vanillaActId);
            return new(
                SceneHelper.GetScenePath($"backgrounds/{normalized}/{normalized}_background"),
                SceneHelper.GetScenePath($"rest_site/{normalized}_rest_site"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_top_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_middle_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_bottom_{normalized}.png"),
                $"res://animations/backgrounds/treasure_room/chest_room_act_{normalized}_skel_data.tres",
                ActVanillaBackgroundLayersDirectory(normalized));
        }

        /// <summary>
        ///     <para xml:lang="en">Calls <see cref="FromVanillaActId" />.</para>
        ///     <para xml:lang="zh-CN">调用 <see cref="FromVanillaActId" />。</para>
        /// </summary>
        public static ActAssetProfile Act(string actEntry)
        {
            return FromVanillaActId(actEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the base-game combat-background layer directory for
        ///         <paramref name="vanillaActFolderName" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="vanillaActFolderName" /> 创建游戏本体的战斗背景图层目录。
        ///     </para>
        /// </summary>
        /// <param name="vanillaActFolderName">
        ///     <para xml:lang="en">The base-game act folder name, such as <c>hive</c>.</para>
        ///     <para xml:lang="zh-CN">游戏本体的章节文件夹名称，例如 <c>hive</c>。</para>
        /// </param>
        public static string ActVanillaBackgroundLayersDirectory(string vanillaActFolderName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(vanillaActFolderName);
            return $"res://scenes/backgrounds/{Normalize(vanillaActFolderName)}/layers";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style scene, background, boss-node, and game-history paths for
        ///         <paramref name="encounterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="encounterEntry" /> 创建游戏本体风格的场景、背景、首领节点和游戏历史路径。
        ///     </para>
        /// </summary>
        /// <param name="encounterEntry">
        ///     <para xml:lang="en">The base-game encounter folder and animation slug.</para>
        ///     <para xml:lang="zh-CN">游戏本体的遭遇文件夹和动画短名称。</para>
        /// </param>
        /// <param name="runHistoryIconPath">
        ///     <para xml:lang="en">
        ///         An optional main game-history icon path. When <see langword="null" />, the path is derived from
        ///         <paramref name="encounterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的游戏历史主图标路径。值为 <see langword="null" /> 时，根据
        ///         <paramref name="encounterEntry" /> 推导路径。
        ///     </para>
        /// </param>
        /// <param name="runHistoryIconOutlinePath">
        ///     <para xml:lang="en">
        ///         An optional game-history outline path. When <see langword="null" />, the path is derived from
        ///         <paramref name="encounterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的游戏历史轮廓路径。值为 <see langword="null" /> 时，根据
        ///         <paramref name="encounterEntry" /> 推导路径。
        ///     </para>
        /// </param>
        public static EncounterAssetProfile Encounter(string encounterEntry,
            string? runHistoryIconPath = null,
            string? runHistoryIconOutlinePath = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encounterEntry);

            var normalized = Normalize(encounterEntry);
            var rhMain = runHistoryIconPath ?? ImageHelper.GetImagePath($"ui/run_history/{normalized}.png");
            var rhOut = runHistoryIconOutlinePath ??
                        ImageHelper.GetImagePath($"ui/run_history/{normalized}_outline.png");
            return new(
                SceneHelper.GetScenePath($"encounters/{normalized}"),
                SceneHelper.GetScenePath($"backgrounds/{normalized}/{normalized}_background"),
                $"res://scenes/backgrounds/{normalized}/layers",
                $"res://animations/map/{normalized}/{normalized}_node_skel_data.tres",
                RunHistoryIconPath: rhMain,
                RunHistoryIconOutlinePath: rhOut);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the base-game combat-background layer directory for an encounter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建游戏本体的遭遇战斗背景图层目录。
        ///     </para>
        /// </summary>
        public static string EncounterVanillaBackgroundLayersDirectory(string encounterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encounterEntry);
            return $"res://scenes/backgrounds/{Normalize(encounterEntry)}/layers";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a base-game-style creature-visuals path for <paramref name="monsterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="monsterEntry" /> 创建游戏本体风格的生物形象路径。
        ///     </para>
        /// </summary>
        public static MonsterAssetProfile Monster(string monsterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(monsterEntry);
            return new(SceneHelper.GetScenePath($"creature_visuals/{Normalize(monsterEntry)}"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style portrait, background, and VFX paths for a default-layout or combat-style
        ///         event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为使用默认布局或战斗风格的事件创建游戏本体风格的肖像、背景和特效路径。
        ///     </para>
        /// </summary>
        public static EventAssetProfile Event(string eventEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventEntry);

            var normalized = Normalize(eventEntry);
            return new(
                InitialPortraitPath: ImageHelper.GetImagePath($"events/{normalized}.png"),
                BackgroundScenePath: SceneHelper.GetScenePath($"events/background_scenes/{normalized}"),
                VfxScenePath: SceneHelper.GetScenePath($"vfx/events/{normalized}_vfx"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the base-game custom-layout scene path for <paramref name="eventEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="eventEntry" /> 创建游戏本体的自定义布局场景路径。
        ///     </para>
        /// </summary>
        public static string EventCustomLayoutScenePath(string eventEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventEntry);
            return SceneHelper.GetScenePath($"events/custom/{Normalize(eventEntry)}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style map and game-history paths for <paramref name="ancientEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="ancientEntry" /> 创建游戏本体风格的先古之民地图和游戏历史路径。
        ///     </para>
        /// </summary>
        public static AncientEventPresentationAssetProfile AncientPresentation(string ancientEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntry);

            var normalized = Normalize(ancientEntry);
            return new(
                ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{normalized}_outline.png"),
                ImageHelper.GetImagePath($"ui/run_history/{normalized}.png"),
                ImageHelper.GetImagePath($"ui/run_history/{normalized}_outline.png"));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds base-game-style timeline portrait paths for <paramref name="epochId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="epochId" /> 创建游戏本体风格的时间线纪元肖像路径。
        ///     </para>
        /// </summary>
        public static EpochAssetProfile Epoch(string epochId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            var normalized = Normalize(epochId);
            return new(
                ImageHelper.GetImagePath($"atlases/epoch_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"timeline/epoch_portraits/{normalized}.png"));
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        private static string NormalizeAncientCardType(CardType cardType)
        {
            return cardType switch
            {
                CardType.None or CardType.Status or CardType.Curse => "skill",
                CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest =>
                    cardType.ToString().ToLowerInvariant(),
                _ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null),
            };
        }
    }
}
