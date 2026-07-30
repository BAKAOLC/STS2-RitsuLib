using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Screens;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the pile, player, and UI state passed to <see cref="ModCardPileSpec.OnOpen" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供传给 <see cref="ModCardPileSpec.OnOpen" /> 的牌堆、玩家与界面状态。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The UI invokes the callback only for a non-empty pile. A callback can call
    ///         <see cref="ShowDefaultPileScreen" />, open a custom capstone screen with
    ///         <see cref="OpenCapstoneScreen(ICapstoneScreen)" />, or leave the click unhandled.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         界面仅会为非空牌堆调用回调。回调可以调用 <see cref="ShowDefaultPileScreen" />，通过
    ///         <see cref="OpenCapstoneScreen(ICapstoneScreen)" /> 打开自定义顶层界面，也可以不处理此次点击。
    ///     </para>
    /// </remarks>
    public sealed class ModCardPileOpenContext
    {
        internal ModCardPileOpenContext(
            ModCardPileDefinition definition,
            ModCardPile pile,
            Player player,
            NModCardPileButton? button)
        {
            Definition = definition;
            Pile = pile;
            Player = player;
            Button = button;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition of the opened pile.</para>
        ///     <para xml:lang="zh-CN">获取所打开牌堆的注册定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the current pile instance resolved for <see cref="Player" />.</para>
        ///     <para xml:lang="zh-CN">获取为 <see cref="Player" /> 解析出的当前牌堆实例。</para>
        /// </summary>
        public ModCardPile Pile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local player associated with the open request.</para>
        ///     <para xml:lang="zh-CN">获取与此次打开请求关联的本地玩家。</para>
        /// </summary>
        public Player Player { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the button that initiated the request, or <see langword="null" /> for programmatic
        ///         requests.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取发起此次请求的按钮；以编程方式发起请求时为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public NModCardPileButton? Button { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens the current pile in the vanilla <see cref="NCardPileScreen" />, using the registered
        ///         hotkeys when present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用原版 <see cref="NCardPileScreen" /> 打开当前牌堆，并在已注册快捷键时使用这些快捷键。
        ///     </para>
        /// </summary>
        public void ShowDefaultPileScreen()
        {
            NCardPileScreen.ShowScreen(Pile, Definition.Hotkeys ?? []);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens <paramref name="screen" /> through <see cref="ModScreenService.Open" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="ModScreenService.Open" /> 打开 <paramref name="screen" />。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Any currently open capstone screen is closed before the requested screen is shown.
        ///     </para>
        ///     <para xml:lang="zh-CN">显示指定界面前会先关闭当前打开的顶层界面。</para>
        /// </remarks>
        /// <param name="screen">
        ///     <para xml:lang="en">The custom capstone screen to open.</para>
        ///     <para xml:lang="zh-CN">要打开的自定义顶层界面。</para>
        /// </param>
        public void OpenCapstoneScreen(ICapstoneScreen screen)
        {
            ModScreenService.Open(screen);
        }
    }
}
