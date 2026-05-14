namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Godot <c>%</c> unique names of all vanilla <c>NCardPoolFilter</c> pool toggles in the card-library
    ///     compendium strip (see
    ///     <c>library.GetNodeOrNull&lt;NCardPoolFilter&gt;(...)</c> in game <c>NCardLibrary</c>). Set
    ///     <see cref="CardLibraryCompendiumPlacementRule.VanillaFilterAnchorUniqueName" /> to one of these
    ///     constants with
    ///     <see cref="CardLibraryCompendiumFilterInsertRelation" /> to place mod filter rows before or after
    ///     the corresponding vanilla control. Order of constants below matches the in-scene layout from left
    ///     to right in base game.
    ///     卡牌库概要条中所有原版 <c>NCardPoolFilter</c> 池开关的 Godot <c>%</c> 唯一名称（参见
    ///     游戏 <c>NCardLibrary</c> 中的 <c>library.GetNodeOrNull&lt;NCardPoolFilter&gt;(...)</c>）。将
    ///     <see cref="CardLibraryCompendiumPlacementRule.VanillaFilterAnchorUniqueName" /> 设为其中一个
    ///     常量，并配合
    ///     <see cref="CardLibraryCompendiumFilterInsertRelation" /> 将 mod 筛选器行放在
    ///     对应原版控件之前或之后。下面常量的顺序与基础游戏中场景布局从左
    ///     到右一致。
    /// </summary>
    public static class CardLibraryCompendiumVanillaFilterNames
    {
        /// <summary>
        ///     Ironclad (red) character pool filter.
        ///     Ironclad（红色）角色池筛选器。
        /// </summary>
        public const string IroncladPool = "%IroncladPool";

        /// <summary>
        ///     Silent (green) character pool filter.
        ///     Silent（绿色）角色池筛选器。
        /// </summary>
        public const string SilentPool = "%SilentPool";

        /// <summary>
        ///     Defect (blue) character pool filter.
        ///     Defect（蓝色）角色池筛选器。
        /// </summary>
        public const string DefectPool = "%DefectPool";

        /// <summary>
        ///     Regent (purple) character pool filter.
        ///     Regent（紫色）角色池筛选器。
        /// </summary>
        public const string RegentPool = "%RegentPool";

        /// <summary>
        ///     Necrobinder (orange) character pool filter.
        ///     Necrobinder（橙色）角色池筛选器。
        /// </summary>
        public const string NecrobinderPool = "%NecrobinderPool";

        /// <summary>
        ///     Colorless pool filter.
        ///     无色池筛选器。
        /// </summary>
        public const string ColorlessPool = "%ColorlessPool";

        /// <summary>
        ///     Ancients pool filter.
        ///     Ancients 池筛选器。
        /// </summary>
        public const string AncientsPool = "%AncientsPool";

        /// <summary>
        ///     Misc (token) pool filter.
        ///     Misc（衍生物）池筛选器。
        /// </summary>
        public const string MiscPool = "%MiscPool";

        private static readonly string[] AllInStripOrderArray =
        [
            IroncladPool, SilentPool, DefectPool, RegentPool, NecrobinderPool,
            ColorlessPool, AncientsPool, MiscPool,
        ];

        /// <summary>
        ///     The eight vanilla <c>%</c> unique names in compendium strip order (left to right, same as
        ///     <c>NCardLibrary</c> field setup). For iteration when resolving anchors; prefer the
        ///     <see cref="IroncladPool" />–<see cref="MiscPool" /> constants for single anchors.
        ///     概要条顺序中的八个原版 <c>%</c> 唯一名称（从左到右，与
        ///     <c>NCardLibrary</c> 字段设置相同）。用于解析锚点时迭代；单个锚点优先使用
        ///     <see cref="IroncladPool" />–<see cref="MiscPool" /> 常量。
        /// </summary>
        public static ReadOnlySpan<string> AllInStripOrder => AllInStripOrderArray;
    }
}
