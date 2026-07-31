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
    ///     <para xml:lang="en">Provides mutable context for one hit of an <see cref="AttackCommand" />.</para>
    ///     <para xml:lang="zh-CN">提供 <see cref="AttackCommand" /> 单次命中的可变上下文。</para>
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
        ///     <para xml:lang="en">Gets the combat state that owns the attack.</para>
        ///     <para xml:lang="zh-CN">获取攻击所属的战斗状态。</para>
        /// </summary>
        public CombatStateLike CombatState { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the choice context passed to the damage command.</para>
        ///     <para xml:lang="zh-CN">获取传给伤害命令的选择上下文。</para>
        /// </summary>
        public PlayerChoiceContext ChoiceContext { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the attack command currently resolving.</para>
        ///     <para xml:lang="zh-CN">获取当前正在结算的攻击命令。</para>
        /// </summary>
        public AttackCommand Attack { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the zero-based hit index.</para>
        ///     <para xml:lang="zh-CN">获取从零开始的命中索引。</para>
        /// </summary>
        public int HitIndex { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the one-based hit number.</para>
        ///     <para xml:lang="zh-CN">获取从一开始的命中编号。</para>
        /// </summary>
        public int HitNumber => HitIndex + 1;

        /// <summary>
        ///     <para xml:lang="en">Gets the total hit count used by the current attack loop.</para>
        ///     <para xml:lang="zh-CN">获取当前攻击循环使用的总命中次数。</para>
        /// </summary>
        public decimal TotalHitCount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the damage passed to <c>CreatureCmd.Damage</c> for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中传给 <c>CreatureCmd.Damage</c> 的伤害值。</para>
        /// </summary>
        public decimal Damage { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the damage properties used for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中使用的伤害属性。</para>
        /// </summary>
        public ValueProp DamageProps { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the damage dealer passed to the damage command for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中传给伤害命令的伤害来源生物。</para>
        /// </summary>
        public Creature? Dealer { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the card source passed to the damage command for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中传给伤害命令的来源卡牌。</para>
        /// </summary>
        public CardModel? CardSource { get; set; }

#if STS2_AT_LEAST_0_108_0
        /// <summary>
        ///     <para xml:lang="en">Gets or sets the card play passed to the damage command for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中传给伤害命令的卡牌打出上下文。</para>
        /// </summary>
        public CardPlay? CardPlay { get; set; }
#endif

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the targets passed to the damage command for this hit.</para>
        ///     <para xml:lang="zh-CN">获取或设置此次命中传给伤害命令的目标列表。</para>
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
        ///     <para xml:lang="en">Gets the target when this hit damages exactly one creature; otherwise <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">此次命中只伤害一个生物时获取该目标；否则为 <see langword="null" />。</para>
        /// </summary>
        public Creature? SingleTarget => Targets.Count == 1 ? Targets[0] : null;

        /// <summary>
        ///     <para xml:lang="en">Gets the damage results after this hit resolves. Empty before after-hit hooks run.</para>
        ///     <para xml:lang="zh-CN">获取此次命中结算后的伤害结果；后置命中钩子运行前为空。</para>
        /// </summary>
        public IReadOnlyList<DamageResult> Results { get; private set; } = [];

        internal void SetResults(IReadOnlyList<DamageResult> results)
        {
            Results = results;
        }
    }
}
