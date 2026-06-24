namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Whether a compendium pool filter should appear immediately before or after its anchor.
    ///     概要池筛选器应显示在其锚点的正前方还是正后方。
    /// </summary>
    public enum CardLibraryCompendiumFilterInsertRelation
    {
        /// <summary>
        ///     Insert at the anchor node’s current sibling index (pushes the anchor and later nodes right).
        ///     插入到锚点节点当前的同级索引处（将锚点及之后的节点向右推）。
        /// </summary>
        Before,

        /// <summary>
        ///     Insert immediately after the anchor (<c>anchorIndex + 1</c>).
        ///     紧接锚点之后插入（<c>anchorIndex + 1</c>）。
        /// </summary>
        After,
    }

    /// <summary>
    ///     One placement preference in a priority list: the first rule whose anchor can be resolved wins for
    ///     vanilla anchors; mod-to-mod constraints from all rules are merged afterward (see
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.CardLibraryCompendiumPatch" /> summary).
    ///     优先级列表中的一个放置偏好：对于原版锚点，第一个能解析锚点的规则胜出；
    ///     随后会合并所有规则中的 mod 到 mod 约束（参见
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.CardLibraryCompendiumPatch" /> 摘要）。
    /// </summary>
    public sealed class CardLibraryCompendiumPlacementRule
    {
        /// <summary>
        ///     Unique name of a vanilla pool filter in the compendium strip. Prefer
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames" /> constants
        ///     (e.g. <see cref="CardLibraryCompendiumVanillaFilterNames.MiscPool" />).
        ///     (e.g. <c>CardLibraryCompendiumVanillaFilterNames.MiscPool</c>).
        ///     概要条中某个原版池筛选器的唯一名称。优先使用
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames" /> 常量
        ///     （例如 <see cref="CardLibraryCompendiumVanillaFilterNames.MiscPool" />）。
        /// </summary>
        public string? VanillaFilterAnchorUniqueName { get; init; }

        /// <summary>
        ///     Another mod character’s public <c>ModelId.Entry</c> (same string used in filter node
        ///     <c>MOD_FILTER_*</c>).
        ///     另一个 mod 角色的公共 <c>ModelId.Entry</c>（与筛选器节点中使用的字符串相同：
        ///     <c>MOD_FILTER_*</c>）。
        /// </summary>
        public string? ModCharacterModelIdEntry { get; init; }

        /// <summary>
        ///     Stable id of another mod-registered shared compendium filter (<c>MOD_FILTER_SHARED_*</c>).
        ///     另一个由 mod 注册的共享概要筛选器的稳定 id（<c>MOD_FILTER_SHARED_*</c>）。
        /// </summary>
        public string? ModSharedCompendiumFilterStableId { get; init; }

        /// <inheritdoc cref="CardLibraryCompendiumFilterInsertRelation" />
        public CardLibraryCompendiumFilterInsertRelation Relation { get; init; }

        internal void ThrowIfInvalid()
        {
            var n = (string.IsNullOrWhiteSpace(VanillaFilterAnchorUniqueName) ? 0 : 1)
                    + (string.IsNullOrWhiteSpace(ModCharacterModelIdEntry) ? 0 : 1)
                    + (string.IsNullOrWhiteSpace(ModSharedCompendiumFilterStableId) ? 0 : 1);
            if (n != 1)
                throw new InvalidOperationException(
                    "Each placement rule must set exactly one of VanillaFilterAnchorUniqueName, " +
                    "ModCharacterModelIdEntry, or ModSharedCompendiumFilterStableId.");
        }

        internal static void ThrowIfInvalidRules(IReadOnlyList<CardLibraryCompendiumPlacementRule>? rules)
        {
            if (rules is null)
                return;

            foreach (var r in rules)
                r.ThrowIfInvalid();
        }
    }

    /// <summary>
    ///     Built-in placement rule lists for card-library compendium mod rows.
    ///     卡牌库概要中 mod 行的内置放置规则列表。
    /// </summary>
    public static class CardLibraryCompendiumPlacementDefaults
    {
        /// <summary>
        ///     Default mod-character row behavior: try immediately before colorless, then ancients, then misc (same
        ///     ordering as the original <c>CardLibraryCompendiumPatch</c> anchor scan).
        ///     默认 mod 角色行行为：依次尝试紧挨无色之前、ancients 之前、misc 之前（与原始
        ///     <c>CardLibraryCompendiumPatch</c> 锚点扫描顺序相同）。
        ///     与原始 <c>CardLibraryCompendiumPatch</c> 锚点扫描顺序相同。
        /// </summary>
        public static IReadOnlyList<CardLibraryCompendiumPlacementRule> DefaultCharacterRowRules { get; } =
        [
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.ColorlessPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.AncientsPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.MiscPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
        ];
    }
}
