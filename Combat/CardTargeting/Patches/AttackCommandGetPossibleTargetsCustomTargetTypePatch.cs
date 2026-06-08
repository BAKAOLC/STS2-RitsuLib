using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Replaces <see cref="AttackCommand.GetPossibleTargets" /> result when custom filtered targets are attached.
    ///     当攻击命令附加了自定义筛选目标时，替换 <see cref="AttackCommand.GetPossibleTargets" /> 的返回结果。
    /// </summary>
    internal sealed class AttackCommandGetPossibleTargetsCustomTargetTypePatch : IPatchMethod
    {
        /// <summary>
        ///     Per-command custom target storage.
        ///     按命令实例保存的自定义目标集合。
        /// </summary>
        internal static readonly ConditionalWeakTable<AttackCommand, StrongBox<IReadOnlyList<Creature>>>
            CustomTargets = new();

        public static string PatchId => "card_target_custom_attack_command_get_possible_targets";

        public static string Description => "让 AttackCommand 支持自定义筛选目标列表";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AttackCommand), nameof(AttackCommand.GetPossibleTargets))];
        }

        public static bool Prefix(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
        {
            if (!CustomTargets.TryGetValue(__instance, out var box) || box.Value == null)
                return true;

            __result = box.Value;
            return false;
        }
    }
}
