using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Ensures cards with <see cref="CustomTargetType.Everyone" /> display multi-target visuals (reticles on every
    ///     creature) similar to vanilla multi-target selection types.
    /// </summary>
    internal sealed class NCardPlayShowMultiCreatureTargetingVisualsEveryonePatch : IPatchMethod
    {
        public static string PatchId => "card_target_everyone_show_multi_target_visuals";

        public static string Description =>
            "Show multi-creature targeting visuals for CustomTargetType.Everyone";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(NCardPlay __instance)
            // ReSharper restore InconsistentNaming
        {
            if (__instance.Card is not { TargetType: var type } || type != CustomTargetType.Everyone)
                return;

            __instance.CardNode?.UpdateVisuals(
                __instance.Card.Pile!.Type,
                CardPreviewMode.MultiCreatureTargeting);

            var room = NCombatRoom.Instance;
            if (room == null)
                return;

            foreach (var creatureNode in room.CreatureNodes)
                creatureNode.ShowMultiselectReticle();
        }
    }
}
