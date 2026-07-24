namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Thin specialization of <see cref="NModCardPileButton" /> for top-bar piles. Presently reuses the
    ///     base button unchanged; placement differences (size, margins) are handled by the top-bar injection
    ///     patch rather than by this class. The type still exists so style-specific behaviour can be added
    ///     later without breaking callers.
    ///     针对 top-bar 牌堆的 <see cref="NModCardPileButton" /> 薄 specialization。目前原样复用基类按钮；
    ///     placement 差异（尺寸、margin）由 top-bar 注入 patch 处理，而不是由此类处理。保留此类型是为了之后
    ///     可以添加 style-specific 行为而不破坏调用方。
    /// </summary>
    // ReSharper disable once ConvertToStaticClass
    public sealed partial class NModTopBarPileButton
    {
        /// <summary>
        ///     Builds a new top-bar button for <paramref name="definition" />. This currently produces the
        ///     same node as <see cref="NModCardPileButton.Create" />; a dedicated class simplifies identifying
        ///     top-bar instances in the scene tree.
        ///     为 <paramref name="definition" /> 构建新的 top-bar 按钮。目前它生成与
        ///     <see cref="NModCardPileButton.Create" /> 相同的节点；独立类可以简化在 scene tree 中识别 top-bar 实例。
        /// </summary>
        public static NModCardPileButton Create(ModCardPileDefinition definition)
        {
            return NModCardPileButton.Create(definition);
        }
    }
}
