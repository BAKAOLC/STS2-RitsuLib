using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Combat.CardTargeting.Patches;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Extension helpers for custom AttackCommand target assignment.
    ///     用于自定义 AttackCommand 目标分配的扩展 helper。
    /// </summary>
    public static class AttackCommandTargetingExtensions
    {
        /// <summary>
        ///     Restricts the attack command to an explicit filtered target set.
        ///     将攻击命令限制到显式给出的筛选目标集合。
        /// </summary>
        public static AttackCommand TargetingFiltered(this AttackCommand command, IEnumerable<Creature> targets)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(targets);

            var list = targets.ToList();
            if (AttackCommandGetPossibleTargetsCustomTargetTypePatch.CustomTargets.TryGetValue(command, out var box))
                box.Value = list;
            else
                AttackCommandGetPossibleTargetsCustomTargetTypePatch.CustomTargets.Add(
                    command,
                    new(list));

            command._combatState = command.Attacker?.CombatState;
            return command;
        }
    }
}
