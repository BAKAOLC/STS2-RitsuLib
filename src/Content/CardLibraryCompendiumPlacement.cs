namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">Specifies whether a compendium pool filter is inserted before or after its anchor.</para>
    ///     <para xml:lang="zh-CN">指定图鉴牌池筛选器插入到其锚点之前还是之后。</para>
    /// </summary>
    public enum CardLibraryCompendiumFilterInsertRelation
    {
        /// <summary>
        ///     <para xml:lang="en">Inserts immediately before the anchor.</para>
        ///     <para xml:lang="zh-CN">紧接锚点之前插入。</para>
        /// </summary>
        Before,

        /// <summary>
        ///     <para xml:lang="en">Inserts immediately after the anchor.</para>
        ///     <para xml:lang="zh-CN">紧接锚点之后插入。</para>
        /// </summary>
        After,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one anchor-relative placement preference for a card-library compendium filter.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述卡牌库图鉴筛选器的一项相对锚点放置偏好。</para>
    /// </summary>
    public sealed class CardLibraryCompendiumPlacementRule
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the unique node name of a vanilla pool-filter anchor.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取原版牌池筛选器锚点的唯一节点名称。</para>
        /// </summary>
        public string? VanillaFilterAnchorUniqueName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets another mod character's public <c>ModelId.Entry</c> to use as the anchor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用作锚点的另一模组角色公共 <c>ModelId.Entry</c>。
        ///     </para>
        /// </summary>
        public string? ModCharacterModelIdEntry { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the stable ID of another registered shared-pool filter to use as the anchor.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取用作锚点的另一已注册共享牌池筛选器稳定 ID。</para>
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
    ///     <para xml:lang="en">Provides built-in placement rules for mod filters in the card-library compendium.</para>
    ///     <para xml:lang="zh-CN">提供卡牌库图鉴中模组筛选器的内置放置规则。</para>
    /// </summary>
    public static class CardLibraryCompendiumPlacementDefaults
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the default character-filter preferences: before Colorless, then Ancients, then Misc.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取默认角色筛选器放置偏好：依次尝试放在 Colorless、Ancients 与 Misc 之前。
        ///     </para>
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
