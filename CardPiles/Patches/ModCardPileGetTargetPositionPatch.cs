using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Intercepts <see cref="PileTypeExtensions.GetTargetPosition" /> for mod piles, returning the
    ///     coordinate of the pile's registered UI host (button or extra-hand container). Must run as a Prefix
    ///     because vanilla's switch would otherwise throw <see cref="ArgumentOutOfRangeException" /> for minted
    ///     values.
    /// </summary>
    public sealed class ModCardPileGetTargetPositionPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_pile_get_target_position_mod_route";

        /// <inheritdoc />
        public static string Description =>
            "Provide NCard fly-in targets for mod card piles before the vanilla switch throws";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetTargetPosition))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns false (skip vanilla) when the pile type was minted by ritsulib.
        /// </summary>
        public static bool Prefix(PileType pileType, NCard? node, ref Vector2 __result)
        {
            if (!ModCardPileRegistry.TryGetByPileType(pileType, out var definition))
                return true;

            __result = ModCardPileLayout.GetTargetPosition(definition, node);
            return false;
        }
        // ReSharper restore InconsistentNaming
    }
}
