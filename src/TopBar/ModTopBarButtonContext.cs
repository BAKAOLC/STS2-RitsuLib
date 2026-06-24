using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Screens;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Context passed to <see cref="ModTopBarButtonSpec.OnClick" /> and
    ///     <see cref="ModTopBarButtonSpec.VisibleWhen" /> callbacks. Exposes the registry definition, the
    ///     local <see cref="Player" /> this button is bound to, and convenience forwarders to
    ///     <see cref="ModScreenService" /> so handlers don't need to pull in capstone plumbing directly.
    ///     传给 <see cref="ModTopBarButtonSpec.OnClick" /> 和
    ///     <see cref="ModTopBarButtonSpec.VisibleWhen" /> 回调的上下文。公开注册表定义、
    ///     此按钮绑定到的本地 <see cref="Player" />，以及到
    ///     <see cref="ModScreenService" /> 的便捷转发，使处理器无需直接引入 capstone 管线。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A single context instance is constructed per click / visibility probe — it is not cached.
    ///         <see cref="Player" /> may be null when visibility is probed before the local player has been
    ///         resolved (for example, between runs), in which case <see cref="ModTopBarButtonSpec.VisibleWhen" />
    ///         handlers should be prepared to return false.
    ///     </para>
    ///     <para>
    ///         每次点击/可见性探测都会构造一个上下文实例，不会缓存。
    ///         在本地玩家解析完成前（例如跑局之间）探测可见性时，<see cref="Player" /> 可能为 null，
    ///         此时 <see cref="ModTopBarButtonSpec.VisibleWhen" />
    ///         处理器应准备好返回 false。
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
        ///     Registry definition that produced the button.
        ///     生成该按钮的注册表定义。
        /// </summary>
        public ModTopBarButtonDefinition Definition { get; }

        /// <summary>
        ///     Local player the button is currently bound to (null while the run is still booting).
        ///     按钮当前绑定到的本地玩家 (跑局仍在启动时为 null)。
        /// </summary>
        public Player? Player { get; }

        /// <summary>
        ///     The Godot button node, when the callback is coming from a real UI click. Shared with the
        ///     card-pile subsystem — action-mode buttons are instances of <see cref="NModCardPileButton" />
        ///     with <see cref="NModCardPileButton.ActionDefinition" /> set rather than a pile, so the UI
        ///     layer is identical to <see cref="STS2RitsuLib.CardPiles.ModCardPileRegistry" /> buttons.
        ///     Godot 按钮节点；当回调来自真实 UI 点击时提供。与
        ///     牌堆子系统共享；动作模式按钮是 <see cref="NModCardPileButton" /> 的实例，
        ///     其中设置的是 <see cref="NModCardPileButton.ActionDefinition" /> 而不是牌堆，因此 UI
        ///     层与 <see cref="STS2RitsuLib.CardPiles.ModCardPileRegistry" /> 按钮相同。
        /// </summary>
        public NModCardPileButton? Button { get; }

        /// <summary>
        ///     Opens <paramref name="screen" /> via <see cref="ModScreenService.Open" />.
        ///     打开 <paramref name="screen" /> 通过 <see cref="ModScreenService.Open" />。
        /// </summary>
        public bool OpenCapstoneScreen(ICapstoneScreen screen)
        {
            return ModScreenService.Open(screen);
        }

        /// <summary>
        ///     Toggles <paramref name="screen" /> — opens it if not currently mounted, closes it otherwise.
        ///     切换 <paramref name="screen" />；当前未挂载则打开, 否则关闭。
        /// </summary>
        public bool ToggleCapstoneScreen(ICapstoneScreen screen)
        {
            return ModScreenService.Toggle(screen);
        }

        /// <summary>
        ///     Closes the current capstone, if any.
        ///     关闭当前 capstone（如果存在）。
        /// </summary>
        public bool CloseCapstoneScreen()
        {
            return ModScreenService.Close();
        }
    }
}
