using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Carries input metadata for a model right-click request.</para>
    ///     <para xml:lang="zh-CN">携带模型右键请求的输入元数据。</para>
    /// </summary>
    /// <param name="IsController">
    ///     <para xml:lang="en">
    ///         Whether the request came from the controller's cancel input while a control had focus.
    ///     </para>
    ///     <para xml:lang="zh-CN">请求是否来自控件获得焦点时的手柄取消输入。</para>
    /// </param>
    /// <param name="Metadata">
    ///     <para xml:lang="en">Optional mod-defined metadata for custom handlers.</para>
    ///     <para xml:lang="zh-CN">供自定义处理器使用的可选模组元数据。</para>
    /// </param>
    public readonly record struct ModRightClickTrigger(bool IsController = false, string? Metadata = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a trigger that records its source UI.</para>
        ///     <para xml:lang="zh-CN">创建记录来源界面的触发信息。</para>
        /// </summary>
        /// <param name="isController">
        ///     <para xml:lang="en">
        ///         Whether the request came from the controller's cancel input while a control had focus.
        ///     </para>
        ///     <para xml:lang="zh-CN">请求是否来自控件获得焦点时的手柄取消输入。</para>
        /// </param>
        /// <param name="metadata">
        ///     <para xml:lang="en">Optional mod-defined metadata for custom handlers.</para>
        ///     <para xml:lang="zh-CN">供自定义处理器使用的可选模组元数据。</para>
        /// </param>
        /// <param name="source">
        ///     <para xml:lang="en">The UI from which the request originated.</para>
        ///     <para xml:lang="zh-CN">请求的来源界面。</para>
        /// </param>
        /// <param name="expectedCardPile">
        ///     <para xml:lang="en">The card pile captured from a combat-pile card request.</para>
        ///     <para xml:lang="zh-CN">从战斗牌堆卡牌请求中记录的牌堆。</para>
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
        ///     <para xml:lang="en">Gets the UI from which the request originated.</para>
        ///     <para xml:lang="zh-CN">获取请求的来源界面。</para>
        /// </summary>
        public ModRightClickSource Source { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card pile captured from a combat-pile card request.</para>
        ///     <para xml:lang="zh-CN">获取从战斗牌堆卡牌请求中记录的牌堆。</para>
        /// </summary>
        public PileType? ExpectedCardPile { get; init; }
    }
}
