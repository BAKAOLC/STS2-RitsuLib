using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Optional per-character ordering for the card-library compendium pool-filter row. When the property is
    ///     <c>null</c> or empty, <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" /> applies.
    ///     <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" />。
    ///     卡牌库 compendium 牌池过滤行的可选逐角色排序。当属性为
    ///     <c>null</c> 或空时，应用 <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" />。
    ///     <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" />。
    /// </summary>
    public interface IModCharacterCardLibraryCompendiumPlacement
    {
        /// <summary>
        ///     Priority-ordered placement rules; the first rule with a resolvable vanilla anchor sets the base index,
        ///     and mod-to-mod constraints from the full list are merged afterward. When <c>null</c> or empty, the
        ///     default character row rules are used.
        ///     按优先级排序的放置规则；第一个具有可解析原版锚点的规则会设置基础索引，
        ///     随后合并完整列表中的 mod 到 mod 约束。为 <c>null</c> 或空时，使用
        ///     默认角色行规则。
        /// </summary>
        IReadOnlyList<CardLibraryCompendiumPlacementRule>? CardLibraryCompendiumPlacementRules { get; }
    }
}
