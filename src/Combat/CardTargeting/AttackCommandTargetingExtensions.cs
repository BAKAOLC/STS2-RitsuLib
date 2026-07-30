using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Combat.CardTargeting.Patches;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">Provides custom target assignment for <see cref="AttackCommand" />.</para>
    ///     <para xml:lang="zh-CN">提供为 <see cref="AttackCommand" /> 指定自定义目标的方法。</para>
    /// </summary>
    public static class AttackCommandTargetingExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Restricts the attack command to a snapshot of <paramref name="targets" />.</para>
        ///     <para xml:lang="zh-CN">将攻击命令限制为 <paramref name="targets" /> 的当前快照。</para>
        /// </summary>
        /// <param name="command">
        ///     <para xml:lang="en">The attack command to configure.</para>
        ///     <para xml:lang="zh-CN">待配置的攻击命令。</para>
        /// </param>
        /// <param name="targets">
        ///     <para xml:lang="en">The allowed targets to snapshot.</para>
        ///     <para xml:lang="zh-CN">待建立快照的可用目标。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured <paramref name="command" />.</para>
        ///     <para xml:lang="zh-CN">配置后的 <paramref name="command" />。</para>
        /// </returns>
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
