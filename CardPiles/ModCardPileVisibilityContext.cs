using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.VisibleWhen" />. Exposes the pile definition, the bound
    ///     <see cref="Player" />, the live button node, and the resolved <see cref="ModCardPile" /> when available.
    /// </summary>
    /// <remarks>
    ///     <see cref="Player" /> or <see cref="Pile" /> may be null while the run or combat UI is still wiring up;
    ///     predicates should return false when required state is missing unless the pile should show anyway.
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

        /// <summary>Registry definition for this pile.</summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>Local player the button is bound to, when known.</summary>
        public Player? Player { get; }

        /// <summary>The pile UI button instance.</summary>
        public NModCardPileButton? Button { get; }

        /// <summary>Runtime pile instance, when <see cref="NModCardPileButton.Initialize" /> has attached it.</summary>
        public ModCardPile? Pile { get; }
    }
}
