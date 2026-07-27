using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Input metadata carried by a model right-click request.
    ///     模型右键请求携带的输入元数据。
    /// </summary>
    /// <param name="IsController">
    ///     True when the request came from controller cancel on a focused control.
    ///     当请求来自聚焦控件上的手柄 cancel 输入时为 true。
    /// </param>
    /// <param name="Metadata">
    ///     Optional mod-defined metadata for custom handlers.
    ///     可选的 mod 自定义元数据，供自定义 handler 使用。
    /// </param>
    public readonly record struct ModRightClickTrigger(bool IsController = false, string? Metadata = null)
    {
        /// <summary>
        ///     Creates a source-aware trigger.
        ///     创建带 UI 来源的 trigger。
        /// </summary>
        /// <param name="isController">
        ///     True when the request came from controller cancel on a focused control.
        ///     当请求来自聚焦控件上的手柄 cancel 输入时为 true。
        /// </param>
        /// <param name="metadata">
        ///     Optional mod-defined metadata for custom handlers.
        ///     可选的 mod 自定义元数据，供自定义 handler 使用。
        /// </param>
        /// <param name="source">
        ///     UI surface that initiated the request.
        ///     发起请求的 UI 表面。
        /// </param>
        /// <param name="expectedCardPile">
        ///     Card pile captured by a combat pile screen request.
        ///     战斗牌堆界面请求捕获的卡牌堆。
        /// </param>
        public ModRightClickTrigger(
            bool isController,
            string? metadata,
            ModRightClickSource source,
            PileType? expectedCardPile = null)
            : this(isController, metadata)
        {
            Source = source;
            ExpectedCardPile = expectedCardPile;
        }

        /// <summary>
        ///     UI surface that initiated the request.
        ///     发起请求的 UI 表面。
        /// </summary>
        public ModRightClickSource Source { get; init; }

        /// <summary>
        ///     Card pile captured by a combat pile screen request.
        ///     战斗牌堆界面请求捕获的卡牌堆。
        /// </summary>
        public PileType? ExpectedCardPile { get; init; }
    }
}
