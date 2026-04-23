using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Extends <see cref="PileTypeExtensions.IsCombatPile" /> to return <c>true</c> for
    ///     <see cref="ModCardPileScope.CombatOnly" /> mod piles. Uses a Postfix so that baselib's own Prefix (if
    ///     present) runs first; ritsulib only upgrades the result when everyone else said "no".
    /// </summary>
    public sealed class ModCardPileIsCombatPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_pile_is_combat_mod_augment";

        /// <inheritdoc />
        public static string Description =>
            "Treat CombatOnly mod card piles as combat piles for PileTypeExtensions.IsCombatPile";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Promotes mod-pile results to <c>true</c> after vanilla / baselib ran.
        /// </summary>
        public static void Postfix(PileType pileType, ref bool __result)
        {
            if (__result)
                return;
            if (!ModCardPileRegistry.TryGetByPileType(pileType, out var definition))
                return;
            if (definition.Scope != ModCardPileScope.CombatOnly)
                return;

            __result = true;
        }
        // ReSharper restore InconsistentNaming
    }
}
