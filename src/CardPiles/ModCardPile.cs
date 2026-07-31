using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a runtime <see cref="CardPile" /> created from a registered mod card-pile definition.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示根据已注册模组卡牌牌堆定义创建的运行时 <see cref="CardPile" />。
    ///     </para>
    /// </summary>
    public sealed class ModCardPile : CardPile
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a pile whose <see cref="CardPile.Type" /> is the dynamic value stored by
        ///         <paramref name="definition" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <see cref="CardPile.Type" /> 为 <paramref name="definition" /> 所存动态值的牌堆。
        ///     </para>
        /// </summary>
        /// <param name="definition">
        ///     <para xml:lang="en">The registered definition used to create the pile.</para>
        ///     <para xml:lang="zh-CN">用于创建牌堆的已注册定义。</para>
        /// </param>
        public ModCardPile(ModCardPileDefinition definition) : base(definition.PileType)
        {
            Definition = definition;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the definition used to create this pile.</para>
        ///     <para xml:lang="zh-CN">获取用于创建此牌堆的定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; }
    }
}
