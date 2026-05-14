using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Ensures the game recognizes <see cref="CustomTargetType.Anyone" /> as a single-target type.
    ///     确保游戏将 <see cref="CustomTargetType.Anyone" /> 识别为单体目标类型。
    /// </summary>
    internal sealed class ActionTargetExtensionsIsSingleTargetAnyonePatch : IPatchMethod
    {
        public static string PatchId => "card_anyone_is_single_target";

        public static string Description =>
            "Treat CustomTargetType.Anyone as a single-target type";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActionTargetExtensions), nameof(ActionTargetExtensions.IsSingleTarget))];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(TargetType targetType, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result)
                return;

            if (targetType == CustomTargetType.Anyone)
                __result = true;
        }
    }
}
