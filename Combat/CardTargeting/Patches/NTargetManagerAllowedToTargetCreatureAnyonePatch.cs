using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Extends <see cref="NTargetManager.AllowedToTargetCreature" /> to allow targeting any living creature when
    ///     the active valid-targets type is <see cref="CustomTargetType.Anyone" />.
    /// </summary>
    internal sealed class NTargetManagerAllowedToTargetCreatureAnyonePatch : IPatchMethod
    {
        public static string PatchId => "card_anyone_allowed_to_target_creature";

        public static string Description =>
            "Allow targeting any living creature for CustomTargetType.Anyone";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTargetManager), nameof(NTargetManager.AllowedToTargetCreature))];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NTargetManager __instance, Creature creature, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance._validTargetsType != CustomTargetType.Anyone)
                return true;

            __result = creature is { IsAlive: true };
            return false;
        }
    }
}
