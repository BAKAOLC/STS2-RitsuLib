using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Records the last pile a card was removed from, to help flight/VFX patches recover "old pile"
    ///     information when the call-site does not provide it.
    /// </summary>
    public sealed class ModCardPileFlightHistoryCardPilePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_mod_pile_flight_history_card_pile_remove";

        /// <inheritdoc />
        public static string Description => "Track last removed CardPile for flight start recovery";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPile), nameof(CardPile.RemoveInternal), [typeof(CardModel), typeof(bool)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Records the pile a card was removed from so later VFX patches can recover the "old pile"
        ///     context when it is not provided by the call-site.
        /// </summary>
        public static void Prefix(CardPile __instance, CardModel card, bool silent)
        {
            if (silent)
                return;
            ModCardPileFlightHistory.RecordRemoved(__instance, card);
        }
        // ReSharper restore InconsistentNaming
    }
}
