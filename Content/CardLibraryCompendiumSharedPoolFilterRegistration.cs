namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Declares an optional card-library compendium pool filter for a stand-alone
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" />. By default such pools do not receive a filter row;
    ///     register this object to add one (icon path and stable id are required).
    ///     为独立的 <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> 声明一个可选的卡牌库概要池筛选器。
    ///     默认情况下这类池不会获得筛选器行；仅在需要该行时注册此对象（必须提供图标路径和稳定 id）。
    /// </summary>
    public sealed class CardLibraryCompendiumSharedPoolFilterRegistration
    {
        /// <summary>
        ///     Mod that registered this filter (for logging).
        ///     注册此筛选器的 mod（用于日志）。
        /// </summary>
        public required string OwningModId { get; init; }

        /// <summary>
        ///     Unique key among all compendium shared-pool filters (ASCII letters, digits, underscore only).
        ///     所有概要共享池筛选器中的唯一键（仅限 ASCII 字母、数字和下划线）。
        /// </summary>
        public required string StableId { get; init; }

        /// <summary>
        ///     Godot resource path for the filter button icon (64px-style texture, same usage as mod character icons).
        ///     筛选器按钮图标的 Godot 资源路径（64px 风格纹理，用法与 mod 角色图标相同）。
        /// </summary>
        public required string IconTexturePath { get; init; }

        /// <summary>
        ///     Concrete card pool type whose <c>AllCardIds</c> define the filter predicate.
        ///     具体卡牌池类型，其 <c>AllCardIds</c> 定义筛选谓词。
        /// </summary>
        public required Type CardPoolType { get; init; }

        /// <summary>
        ///     Optional placement rules; when <c>null</c> or empty, the filter row is appended after existing siblings
        ///     (end of the filter strip). When non-empty, the first rule with a resolvable vanilla anchor sets the base
        ///     index and mod-to-mod constraints from the full list are merged (see patch documentation).
        ///     可选放置规则；当为 <c>null</c> 或空时，筛选器行会追加到现有同级节点之后
        ///     （筛选器条末尾）。非空时，第一个带有可解析原版锚点的规则会设置基础
        ///     索引，并合并完整列表中的 mod 到 mod 约束（参见补丁文档）。
        /// </summary>
        public IReadOnlyList<CardLibraryCompendiumPlacementRule>? PlacementRules { get; init; }
    }
}
