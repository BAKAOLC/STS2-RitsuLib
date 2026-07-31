using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Immutable registration data for a mod keyword, including localization sources, display behavior, and an
    ///         optional icon.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         模组关键词的不可变注册数据，包括本地化来源、显示行为和可选图标。
    ///     </para>
    /// </summary>
    public sealed record ModKeywordDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a definition with the legacy fields plus card-description placement and hover-tip
        ///         inclusion behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用旧版字段以及卡牌描述插入位置和悬停提示包含行为初始化定义。
        ///     </para>
        /// </summary>
        public ModKeywordDefinition(
            string ModId,
            string Id,
            string TitleTable,
            string TitleKey,
            string DescriptionTable,
            string DescriptionKey,
            string? IconPath = null,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None,
            bool includeInCardHoverTip = true)
        {
            this.ModId = ModId;
            this.Id = Id;
            this.TitleTable = TitleTable;
            this.TitleKey = TitleKey;
            this.DescriptionTable = DescriptionTable;
            this.DescriptionKey = DescriptionKey;
            this.IconPath = IconPath;
            CardDescriptionPlacement = cardDescriptionPlacement;
            IncludeInCardHoverTip = includeInCardHoverTip;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the owning mod's manifest ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取所属模组的清单 ID。
        ///     </para>
        /// </summary>
        public string ModId { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the normalized keyword ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取规范化后的关键词 ID。
        ///     </para>
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the localization table containing the title.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标题所在的本地化表。
        ///     </para>
        /// </summary>
        public string TitleTable { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the title's localization key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取标题的本地化键。
        ///     </para>
        /// </summary>
        public string TitleKey { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the localization table containing the description.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取描述所在的本地化表。
        ///     </para>
        /// </summary>
        public string DescriptionTable { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the description's localization key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取描述的本地化键。
        ///     </para>
        /// </summary>
        public string DescriptionKey { get; init; } = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional Godot resource path for the hover-tip icon.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取悬停提示图标的可选 Godot 资源路径。
        ///     </para>
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether and where the keyword's BBCode is injected into card descriptions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否以及在何处将关键词 BBCode 注入卡牌描述。
        ///     </para>
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; init; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether card and template hover-tip helpers include this keyword. This does not affect
        ///         registration or card-description injection.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌和模板的悬停提示辅助方法是否包含此关键词。此设置不影响注册或卡牌描述注入。
        ///     </para>
        /// </summary>
        public bool IncludeInCardHoverTip { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deterministic <see cref="CardKeyword" /> value minted for this keyword above the native enum
        ///         range. The value is stored directly in <c>CardModel.Keywords</c>, allowing native lookup, cloning,
        ///         canonical seeding, and run-save paths to carry the keyword without parallel state.
        ///         <see cref="ModKeywordRegistry" /> populates it during registration; definitions created outside the
        ///         registry retain <see cref="CardKeyword.None" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取为此关键词确定性生成且位于原版枚举范围以上的 <see cref="CardKeyword" /> 值。该值直接存入
        ///         <c>CardModel.Keywords</c>，因此原版查询、克隆、初始关键词填充和单局存档流程均可携带该关键词，
        ///         无需维护并行状态。<see cref="ModKeywordRegistry" /> 会在注册时填充此值；在注册表外创建的定义
        ///         保持为 <see cref="CardKeyword.None" />。
        ///     </para>
        /// </summary>
        public CardKeyword CardKeywordValue { get; init; } = CardKeyword.None;
    }
}
