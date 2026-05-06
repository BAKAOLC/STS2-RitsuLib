using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Short-circuits <see cref="CardPile.Get" /> for mod-minted <see cref="PileType" /> values, returning
    ///     the per-<see cref="Player" /> / per-combat instance resolved by <see cref="ModCardPileStorage" />.
    ///     Non-mod values defer to vanilla (and any other Prefix, such as baselib's <c>GetCombatPile</c>).
    /// </summary>
    /// <remarks>
    ///     Without this patch the vanilla switch falls through to <c>ArgumentOutOfRangeException</c> whenever a
    ///     caller uses a mod-minted pile id, which is why this must run as a Prefix rather than a Postfix.
    /// </remarks>
    public sealed class ModCardPileGetPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_pile_get_mod_route";

        /// <inheritdoc />
        public static string Description => "Route CardPile.Get to ModCardPileStorage for minted mod PileType values";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardPile), nameof(CardPile.Get))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Resolves mod piles before vanilla's switch throws; returns <c>true</c> to continue vanilla
        ///     execution for unrecognized values.
        /// </summary>
        public static bool Prefix(PileType type, Player player, ref CardPile? __result)
        {
            if (!ModCardPileRegistry.IsModPileType(type))
                return true;

            __result = ModCardPileStorage.Resolve(type, player);
            return false;
        }
        // ReSharper restore InconsistentNaming
    }
}
