using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="CardModel.IsValidTarget" /> correctly validate
    ///         <see cref="TargetType.AnyPlayer" /> targets. Vanilla rejects non-null targets and accepts a null target,
    ///         preventing multiplayer target selection.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <see cref="CardModel.IsValidTarget" /> 正确验证 <see cref="TargetType.AnyPlayer" /> 目标。
    ///         原版会拒绝非空目标并接受空目标，导致多人模式无法正常选择目标。
    ///     </para>
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

        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
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
