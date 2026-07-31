namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes an optional card-library compendium filter for a standalone
    ///         <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述独立牌池 <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> 的可选卡牌库图鉴筛选器。
    ///     </para>
    /// </summary>
    public sealed class CardLibraryCompendiumSharedPoolFilterRegistration
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that registered this filter.</para>
        ///     <para xml:lang="zh-CN">获取注册此筛选器的模组 ID。</para>
        /// </summary>
        public required string OwningModId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the globally unique filter ID, consisting only of ASCII letters, digits, and underscores.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取全局唯一的筛选器 ID；该 ID 仅可包含 ASCII 字母、数字与下划线。
        ///     </para>
        /// </summary>
        public required string StableId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the Godot resource path of the filter-button icon.</para>
        ///     <para xml:lang="zh-CN">获取筛选器按钮图标的 Godot 资源路径。</para>
        /// </summary>
        public required string IconTexturePath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the concrete card-pool type whose <c>AllCardIds</c> define the filter contents.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取具体牌池类型；其 <c>AllCardIds</c> 定义筛选器包含的卡牌。
        ///     </para>
        /// </summary>
        public required Type CardPoolType { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional ordered placement preferences. A missing or empty list appends the filter
        ///         to the end of the strip.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的有序放置偏好。未提供或为空时，会将筛选器追加到筛选器条末尾。
        ///     </para>
        /// </summary>
        public IReadOnlyList<CardLibraryCompendiumPlacementRule>? PlacementRules { get; init; }
    }
}
