using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects <see cref="ModCardPileUiStyle.ExtraHand" /> containers (<see cref="Nodes.NModExtraHand" />)
    ///     into <see cref="NCombatUi" /> on ready so they live alongside the vanilla player hand.
    /// </summary>
    public sealed class ModCardPileCombatUiReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_ui_ready_extra_hand";

        /// <inheritdoc />
        public static string Description => "Inject ExtraHand mod pile containers into NCombatUi";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi._Ready))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Wires up ExtraHand containers after vanilla resolves its child references.</summary>
        public static void Postfix(NCombatUi __instance)
        {
            ModCardPileInjector.InjectExtraHandContainers(__instance);
        }
        // ReSharper restore InconsistentNaming
    }

    /// <summary>
    ///     Activates ExtraHand containers with the current combat state so they bind to the local player
    ///     and begin listening to <c>CardPile.CardAdded</c> / <c>CardPile.CardRemoved</c>.
    /// </summary>
    public sealed class ModCardPileCombatUiActivatePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_ui_activate_extra_hand";

        /// <inheritdoc />
        public static string Description => "Activate ExtraHand mod pile containers alongside NCombatUi.Activate";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)])];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Binds each injected ExtraHand container to the local player.</summary>
        public static void Postfix(NCombatUi __instance, CombatState state)
        {
            var me = LocalContext.GetMe(state);
            if (me == null)
                return;
            foreach (var hand in __instance.GetChildren().OfType<NModExtraHand>())
                hand.Initialize(me);
        }
        // ReSharper restore InconsistentNaming
    }
}
