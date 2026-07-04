#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        private IReadOnlyList<Creature> _targets;

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
            CardModel? cardSource
#if STS2_AT_LEAST_0_108_0
            ,
            CardPlay? cardPlay
#endif
        )
        {
            CombatState = combatState;
            ChoiceContext = choiceContext;
            Attack = attack;
            _targets = targets;
            HitIndex = hitIndex;
            TotalHitCount = totalHitCount;
            Damage = damage;
            DamageProps = damageProps;
            Dealer = dealer;
            CardSource = cardSource;
#if STS2_AT_LEAST_0_108_0
            CardPlay = cardPlay;
#endif
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
        ///     Mutate this to change flags such as <see cref="ValueProp.Unblockable" /> for only this hit.
        ///     本段使用的伤害属性。可修改此值，只影响本段，例如添加 <see cref="ValueProp.Unblockable" />。
        /// </summary>
        public ValueProp DamageProps { get; set; }

        /// <summary>
        ///     Dealer passed to the damage command.
        ///     Mutate this to change the damage dealer seen by damage hooks for only this hit.
        ///     传给伤害命令的伤害来源生物。可修改此值，只影响本段伤害 hook 看到的来源。
        /// </summary>
        public Creature? Dealer { get; set; }

        /// <summary>
        ///     Card source passed to the damage command, when any.
        ///     Mutate this to change the card source seen by damage hooks for only this hit.
        ///     传给伤害命令的卡牌来源（如果存在）。可修改此值，只影响本段伤害 hook 看到的卡牌来源。
        /// </summary>
        public CardModel? CardSource { get; set; }

#if STS2_AT_LEAST_0_108_0
        /// <summary>
        ///     Card play passed to the damage command, when any.
        ///     Mutate this to change the card play seen by damage hooks for only this hit.
        ///     传给伤害命令的卡牌打出上下文（如果存在）。可修改此值，只影响本段伤害 hook 看到的卡牌打出上下文。
        /// </summary>
        public CardPlay? CardPlay { get; set; }
#endif

        /// <summary>
        ///     Targets passed to the damage command for this hit.
        ///     Mutate this to retarget only this hit.
        ///     本段传给伤害命令的目标列表。可修改此值，只重定向本段。
        /// </summary>
        public IReadOnlyList<Creature> Targets
        {
            get => _targets;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                foreach (var target in value)
                    ArgumentNullException.ThrowIfNull(target);
                _targets = value;
            }
        }

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
