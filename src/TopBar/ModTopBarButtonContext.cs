using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Screens;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Context passed to top-bar button callbacks. It exposes the registered definition, the local
    ///         player, the mounted button, and helpers for managing capstone screens.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         传给顶部栏按钮回调的上下文。它提供已注册定义、本地玩家、已挂载按钮和管理 Capstone 屏幕的辅助方法。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         A new context is created for each callback. <see cref="Player" /> is
    ///         <see langword="null" /> until a local player is bound, including between runs.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         每次回调都会创建新的上下文。本地玩家尚未绑定时（包括两局游戏之间），
    ///         <see cref="Player" /> 为 <see langword="null" />。
    ///     </para>
    /// </remarks>
    public sealed class ModTopBarButtonContext
    {
        internal ModTopBarButtonContext(
            ModTopBarButtonDefinition definition,
            Player? player,
            NModCardPileButton? button)
        {
            Definition = definition;
            Player = player;
            Button = button;
        }

        /// <summary>
        ///     <para xml:lang="en">Registered definition that produced the callback.</para>
        ///     <para xml:lang="zh-CN">产生此次回调的已注册定义。</para>
        /// </summary>
        public ModTopBarButtonDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Local player currently bound to the button, or <see langword="null" /> before initialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当前绑定到按钮的本地玩家；尚未初始化时为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public Player? Player { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Mounted Godot button node, or <see langword="null" /> when no UI node supplied the callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已挂载的 Godot 按钮节点；回调并非由界面节点触发时为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public NModCardPileButton? Button { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens <paramref name="screen" /> through <see cref="ModScreenService.Open" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="ModScreenService.Open" /> 打开 <paramref name="screen" />。
        ///     </para>
        /// </summary>
        public bool OpenCapstoneScreen(ICapstoneScreen screen)
        {
            return ModScreenService.Open(screen);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens <paramref name="screen" /> when it is not current; otherwise closes it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="screen" /> 不是当前 Capstone 屏幕时将其打开，否则将其关闭。
        ///     </para>
        /// </summary>
        public bool ToggleCapstoneScreen(ICapstoneScreen screen)
        {
            return ModScreenService.Toggle(screen);
        }

        /// <summary>
        ///     <para xml:lang="en">Closes the current capstone screen, if any.</para>
        ///     <para xml:lang="zh-CN">关闭当前 Capstone 屏幕（如果存在）。</para>
        /// </summary>
        public bool CloseCapstoneScreen()
        {
            return ModScreenService.Close();
        }
    }
}
