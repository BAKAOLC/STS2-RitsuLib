using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Extends <see cref="NCard.FindOnTable" /> so cards resident in a visible mod pile (currently only the
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> style) resolve to the live <c>NCard</c> instance managed
    ///     by <c>NModExtraHand</c>. Non-visible mod piles intentionally return <c>null</c> (matching vanilla's
    ///     Draw / Discard / Exhaust behaviour).
    /// </summary>
    /// <remarks>
    ///     Implemented as a Prefix because vanilla's switch hits
    ///     <c>
    ///         _ =&gt; throw new
    ///         ArgumentOutOfRangeException()
    ///     </c>
    ///     for any pile type it doesn't know, which would otherwise abort
    ///     every runtime card lookup while a mod pile hosts the card.
    /// </remarks>
    public sealed class ModCardPileFindOnTablePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_ncard_find_on_table_mod_route";

        /// <inheritdoc />
        public static string Description =>
            "Resolve NCard.FindOnTable for cards held in visible mod piles (ExtraHand containers)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCard), nameof(NCard.FindOnTable))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Short-circuits vanilla for cards whose resolved pile is a mod pile.
        /// </summary>
        public static bool Prefix(CardModel card, PileType? overridePile, ref NCard? __result)
        {
            var pileType = card.Pile?.Type ?? overridePile;
            if (pileType == null)
                return true;
            if (!ModCardPileRegistry.TryGetByPileType(pileType.Value, out var definition))
                return true;

            __result = definition.CardShouldBeVisible
                ? ModCardPileButtonRegistry.TryGetExtraHand(definition)?.GetCard(card)
                : null;
            return false;
        }
        // ReSharper restore InconsistentNaming
    }
}
