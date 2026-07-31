using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Allows a character to customize the placement of its card-pool filter in the Card Library compendium.
    ///     </para>
    ///     <para xml:lang="zh-CN">允许角色自定义其牌池筛选项在卡牌总览中的位置。</para>
    /// </summary>
    public interface IModCharacterCardLibraryCompendiumPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Placement rules in priority order. The first resolvable base-game anchor determines the initial
        ///         index, after which constraints relative to other mod characters are applied. A
        ///         <see langword="null" /> or empty list uses the default character rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按优先级排列的位置规则。首个可解析的原版锚点决定初始索引，之后再应用相对于其他模组角色
        ///         的约束；列表为 <see langword="null" /> 或空时使用默认角色规则。
        ///     </para>
        /// </summary>
        IReadOnlyList<CardLibraryCompendiumPlacementRule>? CardLibraryCompendiumPlacementRules { get; }
    }
}
