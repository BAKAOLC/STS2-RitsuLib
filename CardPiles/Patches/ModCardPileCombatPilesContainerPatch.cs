using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects bottom-row mod pile buttons (<see cref="ModCardPileUiStyle.BottomLeft" /> /
    ///     <see cref="ModCardPileUiStyle.BottomRight" />) into <see cref="NCombatPilesContainer" /> after its
    ///     vanilla <c>_Ready</c> finishes wiring up draw / discard / exhaust.
    /// </summary>
    public sealed class ModCardPileCombatPilesContainerReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_piles_container_ready";

        /// <inheritdoc />
        public static string Description => "Inject mod card pile buttons into NCombatPilesContainer on ready";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer._Ready))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Injects mod bottom-row pile buttons after vanilla wiring completes.</summary>
        public static void Postfix(NCombatPilesContainer __instance)
        {
            ModCardPileInjector.InjectCombatButtons(__instance);
        }
        // ReSharper restore InconsistentNaming
    }

    /// <summary>
    ///     Forwards <see cref="NCombatPilesContainer.Initialize" /> to every injected
    ///     <see cref="NModCardPileButton" /> so each mod pile binds to the active <see cref="Player" />
    ///     alongside the vanilla draw / discard / exhaust buttons.
    /// </summary>
    public sealed class ModCardPileCombatPilesContainerInitializePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_piles_container_initialize";

        /// <inheritdoc />
        public static string Description => "Initialize injected mod pile buttons with the current player";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCombatPilesContainer), nameof(NCombatPilesContainer.Initialize), [typeof(Player)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Binds each mod pile button to the current player.</summary>
        public static void Postfix(NCombatPilesContainer __instance, Player player)
        {
            foreach (var button in __instance.GetChildren().OfType<NModCardPileButton>())
                button.Initialize(player);
        }
        // ReSharper restore InconsistentNaming
    }
}
