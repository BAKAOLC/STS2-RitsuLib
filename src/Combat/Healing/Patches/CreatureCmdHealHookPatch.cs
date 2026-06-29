using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Healing.Patches
{
    /// <summary>
    ///     Applies RitsuLib healing amount hooks before <see cref="CreatureCmd.Heal" /> resolves.
    ///     在 <see cref="CreatureCmd.Heal" /> 结算前应用 RitsuLib 治疗数值 hook。
    /// </summary>
    internal sealed class CreatureCmdHealHookPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_creature_cmd_heal_amount_hooks";

        public static string Description => "Dispatch RitsuLib heal amount hooks for CreatureCmd.Heal";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                PatchTarget.Method(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal),
                    typeof(bool)),
            ];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(Creature creature, ref decimal amount, bool playAnim)
        {
            if (creature == null)
                return;

            if (amount <= 0m)
            {
                amount = Math.Max(0m, amount);
                return;
            }

            amount = HealHook.ModifyAmount(new(creature, amount, playAnim), amount);
        }
    }
}
