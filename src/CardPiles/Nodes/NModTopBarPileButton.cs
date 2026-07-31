namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     <para xml:lang="en">Creates mod card-pile buttons intended for top-bar placement.</para>
    ///     <para xml:lang="zh-CN">创建用于放置在顶部栏的模组卡牌牌堆按钮。</para>
    /// </summary>
    // ReSharper disable once ConvertToStaticClass
    public sealed class NModTopBarPileButton
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a pile button for <paramref name="definition" /> using
        ///         <see cref="NModCardPileButton.Create" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="NModCardPileButton.Create" /> 为 <paramref name="definition" /> 创建牌堆按钮。
        ///     </para>
        /// </summary>
        public static NModCardPileButton Create(ModCardPileDefinition definition)
        {
            return NModCardPileButton.Create(definition);
        }
    }
}
