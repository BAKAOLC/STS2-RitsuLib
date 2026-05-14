using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="CardModel.IsValidTarget" /> for <see cref="TargetType.AnyPlayer" />.
    ///     Vanilla always returns <c>false</c> for non-null targets with AnyPlayer, and always
    ///     returns <c>true</c> for null targets — breaking multiplayer card targeting.
    ///     修复 <see cref="CardModel.IsValidTarget" /> 对 <see cref="TargetType.AnyPlayer" /> 的处理。
    ///     原版对 AnyPlayer 的非 null 目标总是返回 <c>false</c>，对 null 目标总是返回 <c>true</c>，
    ///     从而破坏多人卡牌目标选择。
    /// </summary>
    internal sealed class CardModelIsValidTargetAnyPlayerPatch : IPatchMethod
    {
        public static string PatchId => "card_any_player_is_valid_target";

        public static string Description =>
            "Fix CardModel.IsValidTarget to correctly validate AnyPlayer targets";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.IsValidTarget), [typeof(Creature)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance.TargetType != TargetType.AnyPlayer)
                return true;

            if (target == null)
            {
                __result = __instance.Owner.RunState.Players.Count <= 1;
                return false;
            }

            __result = AnyPlayerCardTargetingHelper.IsAnyPlayerTargetValid(target);
            return false;
        }
    }
}
