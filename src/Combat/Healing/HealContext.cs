#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     Context for a creature healing amount modification.
    ///     生物治疗数值修正的上下文。
    /// </summary>
    public sealed class HealContext
    {
        internal HealContext(Creature creature, decimal originalAmount, bool playAnim)
        {
            Creature = creature;
            OriginalAmount = originalAmount;
            PlayAnim = playAnim;
            CombatState = creature.CombatState;
            RunState = creature.Player?.RunState ?? creature.CombatState?.RunState;
        }

        /// <summary>
        ///     Creature receiving healing.
        ///     接受治疗的生物。
        /// </summary>
        public Creature Creature { get; }

        /// <summary>
        ///     Amount passed to <c>CreatureCmd.Heal</c> before RitsuLib listeners modify it.
        ///     RitsuLib 监听器修正前传给 <c>CreatureCmd.Heal</c> 的数值。
        /// </summary>
        public decimal OriginalAmount { get; }

        /// <summary>
        ///     Whether the vanilla heal animation argument is enabled for this heal command.
        ///     此治疗命令的原版治疗动画参数是否启用。
        /// </summary>
        public bool PlayAnim { get; }

        /// <summary>
        ///     Combat state containing the creature, when the heal happens in combat.
        ///     包含该生物的战斗状态；非战斗治疗时可能为空。
        /// </summary>
        public CombatStateLike? CombatState { get; }

        /// <summary>
        ///     Run state associated with this heal, when available.
        ///     与本次治疗关联的跑局状态；不可用时为空。
        /// </summary>
        public IRunState? RunState { get; }

        /// <summary>
        ///     Remaining HP this creature can currently recover.
        ///     该生物当前还能恢复的生命值。
        /// </summary>
        public decimal MissingHp => Math.Max(0m, Creature.MaxHp - Creature.CurrentHp);
    }
}
