using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Screens;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.OnOpen" /> when a mod pile's UI button is released.
    ///     Exposes the backing pile plus convenience helpers so handlers can swap in a custom
    ///     <see cref="ICapstoneScreen" /> (or invoke the default <see cref="NCardPileScreen" />) without
    ///     hand-wiring <see cref="NCapstoneContainer" />.
    /// </summary>
    /// <remarks>
    ///     Handlers may:
    ///     <list type="bullet">
    ///         <item>Call <see cref="ShowDefaultPileScreen" /> to reuse the vanilla <see cref="NCardPileScreen" />.</item>
    ///         <item>
    ///             Call <see cref="OpenCapstoneScreen(ICapstoneScreen)" /> to mount a custom
    ///             <see cref="ICapstoneScreen" /> (for example a Godot scene script).
    ///         </item>
    ///         <item>Do nothing — the button returns to its idle state after the tween.</item>
    ///     </list>
    ///     Handlers are invoked from the button's release handler after the click tween starts and after
    ///     ritsulib already ensured the pile is non-empty (empty piles trigger
    ///     <see cref="ModCardPileDefinition.EmptyPileMessage" /> via a thought bubble and skip the callback).
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

        /// <summary>Definition of the pile whose button was pressed.</summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>Live <see cref="ModCardPile" /> resolved for <see cref="Player" />.</summary>
        public ModCardPile Pile { get; }

        /// <summary>The local player this button is bound to.</summary>
        public Player Player { get; }

        /// <summary>
        ///     The clicked button, when the open was triggered from a
        ///     <see cref="ModCardPileUiStyle.TopBarDeck" /> / <see cref="ModCardPileUiStyle.BottomLeft" /> /
        ///     <see cref="ModCardPileUiStyle.BottomRight" /> UI node. Null for programmatic invocations.
        /// </summary>
        public NModCardPileButton? Button { get; }

        /// <summary>
        ///     Launches the vanilla <see cref="NCardPileScreen" /> for the current pile, re-using
        ///     <see cref="ModCardPileDefinition.Hotkeys" /> when set. This is exactly what the default open
        ///     handler does when <see cref="ModCardPileSpec.OnOpen" /> is null.
        /// </summary>
        public void ShowDefaultPileScreen()
        {
            NCardPileScreen.ShowScreen(Pile, Definition.Hotkeys ?? []);
        }

        /// <summary>
        ///     Opens <paramref name="screen" /> through <see cref="ModScreenService.Open" />. If a capstone
        ///     is already showing, it is closed first so the new screen can take the stage.
        /// </summary>
        /// <remarks>
        ///     Thin convenience forwarder — the actual capstone plumbing lives in
        ///     <see cref="ModScreenService" /> so any mod code can open custom screens without pulling in
        ///     the card-pile subsystem.
        /// </remarks>
        /// <param name="screen">Custom screen implementing <see cref="ICapstoneScreen" />.</param>
        public void OpenCapstoneScreen(ICapstoneScreen screen)
        {
            ModScreenService.Open(screen);
        }
    }
}
