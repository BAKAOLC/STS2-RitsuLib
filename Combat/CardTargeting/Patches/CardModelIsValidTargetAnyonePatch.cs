using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Overrides <see cref="CardModel.IsValidTarget" /> for <see cref="CustomTargetType.Anyone" />:
    ///     any living creature is valid.
    /// </summary>
    internal sealed class CardModelIsValidTargetAnyonePatch : IPatchMethod
    {
        public static string PatchId => "card_anyone_is_valid_target";

        public static string Description =>
            "Treat any living creature as valid for Anyone IsValidTarget";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.IsValidTarget))];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance.TargetType != CustomTargetType.Anyone)
                return true;

            __result = target is { IsAlive: true };
            return false;
        }
    }
}
