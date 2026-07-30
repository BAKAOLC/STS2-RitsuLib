using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the registration and runtime state passed to <see cref="ModCardPileSpec.VisibleWhen" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供传给 <see cref="ModCardPileSpec.VisibleWhen" /> 的注册信息与运行时状态。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="Player" /> and <see cref="Pile" /> can be <see langword="null" /> while the run or
    ///         combat UI is being initialized. Predicates that require either value should handle that state.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         一局游戏或战斗界面仍在初始化时，<see cref="Player" /> 和 <see cref="Pile" /> 可能为
    ///         <see langword="null" />。依赖其中任一值的谓词应处理这种状态。
    ///     </para>
    /// </remarks>
    public sealed class ModCardPileVisibilityContext
    {
        internal ModCardPileVisibilityContext(
            ModCardPileDefinition definition,
            Player? player,
            NModCardPileButton? button,
            ModCardPile? pile)
        {
            Definition = definition;
            Player = player;
            Button = button;
            Pile = pile;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition of the pile.</para>
        ///     <para xml:lang="zh-CN">获取该牌堆的注册定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local player associated with the control, when available.</para>
        ///     <para xml:lang="zh-CN">获取与该控件关联的本地玩家（若可用）。</para>
        /// </summary>
        public Player? Player { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the pile's UI button, when available.</para>
        ///     <para xml:lang="zh-CN">获取牌堆的界面按钮（若可用）。</para>
        /// </summary>
        public NModCardPileButton? Button { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the runtime pile instance attached by <see cref="NModCardPileButton.Initialize" />, when
        ///         available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取由 <see cref="NModCardPileButton.Initialize" /> 关联的运行时牌堆实例（若可用）。
        ///     </para>
        /// </summary>
        public ModCardPile? Pile { get; }
    }
}
