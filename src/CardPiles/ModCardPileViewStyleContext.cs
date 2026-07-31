using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the live objects available to an optional mod pile view-style callback.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供模组牌堆可选查看界面样式回调可使用的实时对象。
    ///     </para>
    /// </summary>
    /// <param name="Definition">
    ///     <para xml:lang="en">The registered definition of the displayed pile.</para>
    ///     <para xml:lang="zh-CN">所显示牌堆的已注册定义。</para>
    /// </param>
    /// <param name="Pile">
    ///     <para xml:lang="en">The pile currently displayed by the screen.</para>
    ///     <para xml:lang="zh-CN">界面当前显示的牌堆。</para>
    /// </param>
    /// <param name="Screen">
    ///     <para xml:lang="en">The live base-game pile screen being styled.</para>
    ///     <para xml:lang="zh-CN">正在应用样式的游戏原有牌堆界面。</para>
    /// </param>
    public sealed record ModCardPileViewStyleContext(
        ModCardPileDefinition Definition,
        CardPile Pile,
        NCardPileScreen Screen);
}
