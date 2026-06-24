using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Runtime instance of a mod card pile. Behaves as a vanilla <see cref="CardPile" /> and simply carries
    ///     a back-reference to its <see cref="ModCardPileDefinition" /> so UI and patch code can look up mod
    ///     metadata (icon, localization, style).
    ///     （icon、localization、style）。
    ///     mod 卡牌牌堆的运行时实例。行为类似原版 <see cref="CardPile" />，并携带指向其
    ///     <see cref="ModCardPileDefinition" /> 的反向引用，使 UI 和 patch 代码可以查找 mod
    ///     元数据（icon、localization、style）。
    /// </summary>
    public sealed class ModCardPile : CardPile
    {
        /// <summary>
        ///     Creates a pile whose <see cref="CardPile.Type" /> matches <paramref name="definition" />'s minted value.
        ///     创建一个 <see cref="CardPile.Type" /> 匹配 <paramref name="definition" /> 生成值的牌堆。
        /// </summary>
        /// <param name="definition">
        ///     Registry entry this pile was created from.
        ///     创建此牌堆所依据的注册条目。
        /// </param>
        public ModCardPile(ModCardPileDefinition definition) : base(definition.PileType)
        {
            Definition = definition;
        }

        /// <summary>
        ///     Back-reference to the immutable definition this pile was built from.
        ///     指向此牌堆所依据不可变定义的反向引用。
        /// </summary>
        public ModCardPileDefinition Definition { get; }
    }
}
