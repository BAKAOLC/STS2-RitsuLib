using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Replaces the result of <see cref="AttackCommand.GetPossibleTargets" /> when custom filtered targets are
    ///         attached.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当攻击命令附加了自定义筛选目标时，替换 <see cref="AttackCommand.GetPossibleTargets" /> 的返回结果。
    ///     </para>
    /// </summary>
    internal sealed class AttackCommandGetPossibleTargetsCustomTargetTypePatch : IPatchMethod
    {
        /// <summary>
        ///     <para xml:lang="en">Stores custom targets for each command instance.</para>
        ///     <para xml:lang="zh-CN">按命令实例保存自定义目标集合。</para>
        /// </summary>
        internal static readonly ConditionalWeakTable<AttackCommand, StrongBox<IReadOnlyList<Creature>>>
            CustomTargets = [];

        public static string PatchId => "card_target_custom_attack_command_get_possible_targets";

        public static string Description => "Allow AttackCommand to use custom filtered target lists";

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
