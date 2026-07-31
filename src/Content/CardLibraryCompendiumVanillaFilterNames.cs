namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the Godot unique node names of vanilla card-library compendium pool filters.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供原版卡牌库图鉴牌池筛选器的 Godot 唯一节点名称。</para>
    /// </summary>
    public static class CardLibraryCompendiumVanillaFilterNames
    {
        /// <summary>
        ///     <para xml:lang="en">The Ironclad character-pool filter.</para>
        ///     <para xml:lang="zh-CN">铁甲战士角色牌池筛选器。</para>
        /// </summary>
        public const string IroncladPool = "%IroncladPool";

        /// <summary>
        ///     <para xml:lang="en">The Silent character-pool filter.</para>
        ///     <para xml:lang="zh-CN">静默猎手角色牌池筛选器。</para>
        /// </summary>
        public const string SilentPool = "%SilentPool";

        /// <summary>
        ///     <para xml:lang="en">The Defect character-pool filter.</para>
        ///     <para xml:lang="zh-CN">故障机器人角色牌池筛选器。</para>
        /// </summary>
        public const string DefectPool = "%DefectPool";

        /// <summary>
        ///     <para xml:lang="en">The Regent character-pool filter.</para>
        ///     <para xml:lang="zh-CN">储君角色牌池筛选器。</para>
        /// </summary>
        public const string RegentPool = "%RegentPool";

        /// <summary>
        ///     <para xml:lang="en">The Necrobinder character-pool filter.</para>
        ///     <para xml:lang="zh-CN">亡灵契约师角色牌池筛选器。</para>
        /// </summary>
        public const string NecrobinderPool = "%NecrobinderPool";

        /// <summary>
        ///     <para xml:lang="en">The Colorless card-pool filter.</para>
        ///     <para xml:lang="zh-CN">无色牌池筛选器。</para>
        /// </summary>
        public const string ColorlessPool = "%ColorlessPool";

        /// <summary>
        ///     <para xml:lang="en">The Ancients card-pool filter.</para>
        ///     <para xml:lang="zh-CN">先古牌池筛选器。</para>
        /// </summary>
        public const string AncientsPool = "%AncientsPool";

        /// <summary>
        ///     <para xml:lang="en">The Misc card-pool filter.</para>
        ///     <para xml:lang="zh-CN">杂项牌池筛选器。</para>
        /// </summary>
        public const string MiscPool = "%MiscPool";

        private static readonly string[] AllInStripOrderArray =
        [
            IroncladPool, SilentPool, DefectPool, RegentPool, NecrobinderPool,
            ColorlessPool, AncientsPool, MiscPool,
        ];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets all vanilla filter names in their left-to-right compendium-strip order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取全部原版筛选器名称，并按其在图鉴筛选器条中的从左到右顺序排列。
        ///     </para>
        /// </summary>
        public static ReadOnlySpan<string> AllInStripOrder => AllInStripOrderArray;
    }
}
