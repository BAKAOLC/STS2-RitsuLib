#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace STS2RitsuLib.Combat.AttackHits
{
    /// <summary>
    ///     Mutable context for a single <see cref="AttackCommand" /> hit.
    ///     单次 <see cref="AttackCommand" /> 命中的可变上下文。
    /// </summary>
    public sealed class AttackHitContext
    {
        internal AttackHitContext(
            CombatStateLike combatState,
            PlayerChoiceContext choiceContext,
            AttackCommand attack,
            IReadOnlyList<Creature> targets,
            int hitIndex,
            decimal totalHitCount,
            decimal damage,
            ValueProp damageProps,
            Creature? dealer,
            CardModel? cardSource)
        {
            CombatState = combatState;
            ChoiceContext = choiceContext;
            Attack = attack;
            Targets = targets;
            HitIndex = hitIndex;
            TotalHitCount = totalHitCount;
            Damage = damage;
            DamageProps = damageProps;
            Dealer = dealer;
            CardSource = cardSource;
        }

        /// <summary>
        ///     Combat state that owns the attack.
        ///     攻击所属战斗状态。
        /// </summary>
        public CombatStateLike CombatState { get; }

        /// <summary>
        ///     Choice context passed to the damage command.
        ///     传给伤害命令的选择上下文。
        /// </summary>
        public PlayerChoiceContext ChoiceContext { get; }

        /// <summary>
        ///     Attack command currently resolving.
        ///     当前正在结算的攻击命令。
        /// </summary>
        public AttackCommand Attack { get; }

        /// <summary>
        ///     Zero-based hit index.
        ///     从零开始的命中序号。
        /// </summary>
        public int HitIndex { get; }

        /// <summary>
        ///     One-based hit number.
        ///     从一开始的命中编号。
        /// </summary>
        public int HitNumber => HitIndex + 1;

        /// <summary>
        ///     Total hit count currently used by the running attack loop.
        ///     当前攻击循环使用的总段数。
        /// </summary>
        public decimal TotalHitCount { get; }

        /// <summary>
        ///     Damage amount passed to <c>CreatureCmd.Damage</c> for this hit.
        ///     本段传给 <c>CreatureCmd.Damage</c> 的伤害值。
        /// </summary>
        public decimal Damage { get; set; }

        /// <summary>
        ///     Damage properties used for this hit.
        ///     本段使用的伤害属性。
        /// </summary>
        public ValueProp DamageProps { get; }

        /// <summary>
        ///     Dealer passed to the damage command.
        ///     传给伤害命令的伤害来源生物。
        /// </summary>
        public Creature? Dealer { get; }

        /// <summary>
        ///     Card source passed to the damage command, when any.
        ///     传给伤害命令的卡牌来源（如果存在）。
        /// </summary>
        public CardModel? CardSource { get; }

        /// <summary>
        ///     Targets passed to the damage command for this hit.
        ///     本段传给伤害命令的目标列表。
        /// </summary>
        public IReadOnlyList<Creature> Targets { get; }

        /// <summary>
        ///     Single target for this hit when exactly one target is being damaged.
        ///     当本段只伤害一个目标时的单体目标。
        /// </summary>
        public Creature? SingleTarget => Targets.Count == 1 ? Targets[0] : null;

        /// <summary>
        ///     Damage results after this hit has resolved. Empty before after-hit hooks run.
        ///     本段结算后的伤害结果。在后置 hook 运行前为空。
        /// </summary>
        public IReadOnlyList<DamageResult> Results { get; private set; } = [];

        internal void SetResults(IReadOnlyList<DamageResult> results)
        {
            Results = results;
        }
    }
}
