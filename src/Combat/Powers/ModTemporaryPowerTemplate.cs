using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Combat.Powers
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an extensible temporary-power wrapper that applies an internal power and removes the applied
    ///         amount when the configured turn duration expires.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供可扩展的临时能力包装：应用一个内部能力，并在配置的回合持续时间结束时移除已应用的数值。
    ///     </para>
    /// </summary>
    public abstract class ModTemporaryPowerTemplate : ModPowerTemplate, ITemporaryPower
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The reserved dynamic-variable name used to track remaining extra expiry cycles and optionally expose
        ///         them to localization as <c>{ExtraTurns}</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于记录剩余额外到期周期的保留动态变量名；也可通过 <c>{ExtraTurns}</c> 将其用于本地化文本。
        ///     </para>
        /// </summary>
        public const string ExtraTurnCyclesVarName = "ExtraTurns";

        private static readonly MethodInfo ApplyInternalPowerGenericMethod =
            typeof(ModTemporaryPowerTemplate).GetMethod(nameof(ApplyInternalPowerGeneric),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private ApplyInternalPowerInvoker? _cachedInternalPowerInvoker;

        private Type? _cachedInternalPowerType;

        private bool _shouldIgnoreNextInstance;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the temporary effect applies a positive amount and is presented as a buff. Override
        ///         with <see langword="false" /> to invert the applied amount and present the wrapper as a debuff.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取临时效果是否应用正数并显示为增益。重写为 <see langword="false" /> 时会反转应用数值，
        ///         并将包装能力显示为减益。
        ///     </para>
        /// </summary>
        protected virtual bool IsPositive => true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the effect expires at the end of the opposing side's turn instead of the owner's
        ///         participating turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取效果是否在对方回合结束时到期，而不是在拥有者参与的回合结束时到期。
        ///     </para>
        /// </summary>
        protected virtual bool UntilEndOfOtherSideTurn => false;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the number of additional qualifying turn ends to wait before the effect expires. A positive
        ///         value also makes each application a separate power instance. Negative values are invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取效果到期前额外等待的有效回合结束次数。值为正数时，每次应用还会创建独立的能力实例。
        ///         负数无效。
        ///     </para>
        /// </summary>
        protected virtual int LastForXExtraTurns => 0;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets additional dynamic variables for localization. The sequence and its entries must not be
        ///         <see langword="null" />, and it must not define <see cref="ExtraTurnCyclesVarName" /> because the
        ///         template always supplies that variable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用于本地化的额外动态变量。序列及其中的项均不得为 <see langword="null" />，且不得定义
        ///         <see cref="ExtraTurnCyclesVarName" />，因为该变量始终由模板提供。
        ///     </para>
        /// </summary>
        protected virtual IEnumerable<DynamicVar> AdditionalCanonicalVars => [];

        /// <inheritdoc />
        public override PowerType Type => IsPositive ? PowerType.Buff : PowerType.Debuff;

        /// <inheritdoc />
        public override PowerStackType StackType => PowerStackType.Counter;

        /// <inheritdoc />
        public override bool AllowNegative => true;

        /// <inheritdoc />
#if !STS2_AT_LEAST_0_105_0
        public override bool IsInstanced => ValidatedExtraTurnCycles > 0;
#else
        public override PowerInstanceType InstanceType =>
            ValidatedExtraTurnCycles > 0 ? PowerInstanceType.Instanced : PowerInstanceType.None;
#endif

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the remaining extra expiry cycles stored in
        ///         <see cref="ExtraTurnCyclesVarName" />. Assigned negative values are clamped to zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置存储在 <see cref="ExtraTurnCyclesVarName" /> 中的剩余额外到期周期。
        ///         赋入的负数会被限制为零。
        ///     </para>
        /// </summary>
        public int RemainingExtraTurnCycles
        {
            get => (int)DynamicVars[ExtraTurnCyclesVarName].BaseValue;
            set => DynamicVars[ExtraTurnCyclesVarName].BaseValue = Math.Max(value, 0);
        }

        /// <inheritdoc />
        public override LocString Title => OriginModel.ResolveTitleOr(InternallyAppliedPower.Title);

        /// <inheritdoc />
        protected override IEnumerable<IHoverTip> AdditionalHoverTips => ResolveExtraHoverTips();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the template-controlled canonical variables. The template always defines
        ///         <see cref="ExtraTurnCyclesVarName" /> and appends <see cref="AdditionalCanonicalVars" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取由模板控制的规范变量。模板始终定义 <see cref="ExtraTurnCyclesVarName" />，
        ///         并在其后追加 <see cref="AdditionalCanonicalVars" />。
        ///     </para>
        /// </summary>
        protected sealed override IEnumerable<DynamicVar> CanonicalVars => BuildCanonicalVars();

        private int ValidatedExtraTurnCycles
        {
            get
            {
                var extraTurnCycles = LastForXExtraTurns;
                if (extraTurnCycles < 0)
                    throw new InvalidOperationException(
                        $"{nameof(LastForXExtraTurns)} cannot be negative.");
                return extraTurnCycles;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the model that granted this temporary power. It is used to resolve the title and any supported
        ///         source hover tip.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取授予该临时能力的模型，用于解析标题以及受支持的来源悬停提示。
        ///     </para>
        /// </summary>
        public abstract AbstractModel OriginModel { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the internal power whose amount is applied while this temporary wrapper exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在该临时包装存在期间应用其数值的内部能力。
        ///     </para>
        /// </summary>
        public abstract PowerModel InternallyAppliedPower { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Suppresses the next application or amount-change callback, matching the base temporary-power
        ///         behavior used when an application must not alter the internal power.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         忽略下一次应用或数值变更回调，与原版临时能力在某次应用不应改变内部能力时的行为一致。
        ///     </para>
        /// </summary>
        public void IgnoreNextInstance()
        {
            _shouldIgnoreNextInstance = true;
        }

        /// <inheritdoc />
        public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier,
            CardModel? cardSource)
        {
            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
                return;
            }

            if (RemainingExtraTurnCycles == 0)
                RemainingExtraTurnCycles = ValidatedExtraTurnCycles;
            await ApplyInternalPower(new ThrowingPlayerChoiceContext(), target, SignedAmount(amount), applier,
                cardSource, true);
        }

        /// <inheritdoc />
#if !STS2_AT_LEAST_0_104_0
        public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
            CardModel? cardSource)
#else
        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
            decimal amount,
            Creature? applier, CardModel? cardSource)
#endif
        {
            if (amount == Amount || power != this)
                return;

            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
                return;
            }

#if !STS2_AT_LEAST_0_104_0
            await ApplyInternalPower(new ThrowingPlayerChoiceContext(), Owner, SignedAmount(amount), applier,
                cardSource, true);
#else
            await ApplyInternalPower(choiceContext, Owner, SignedAmount(amount), applier, cardSource, true);
#endif
        }

        /// <inheritdoc />
#if STS2_AT_LEAST_0_106_0
        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
            IEnumerable<Creature> participants)
#else
        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
#endif
        {
            if (UntilEndOfOtherSideTurn)
            {
                // Expire on the other side's turn end; Owner is never in the other side's participants.
                if (side == Owner.Side) return;
            }
            else
            {
#if STS2_AT_LEAST_0_106_0
                // Use participants rather than side so extra-turn firings don't prematurely expire
                // powers belonging to creatures that didn't participate in that extra turn.
                if (!participants.Contains(Owner)) return;
#else
                if (side != Owner.Side) return;
#endif
            }

            if (RemainingExtraTurnCycles > 0)
            {
                RemainingExtraTurnCycles--;
                return;
            }

            Flash();
            await PowerCmd.Remove(this);
            await ApplyInternalPower(choiceContext, Owner, -SignedAmount(Amount), Owner, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies the sign selected by <see cref="IsPositive" /> to <paramref name="amount" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据 <see cref="IsPositive" /> 为 <paramref name="amount" /> 应用对应的正负号。
        ///     </para>
        /// </summary>
        /// <param name="amount">
        ///     <para xml:lang="en">The unsigned effect amount.</para>
        ///     <para xml:lang="zh-CN">尚未确定正负号的效果数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The signed amount to apply to the internal power.</para>
        ///     <para xml:lang="zh-CN">要应用到内部能力的带符号数值。</para>
        /// </returns>
        protected virtual decimal SignedAmount(decimal amount)
        {
            return IsPositive ? amount : -amount;
        }

        private IEnumerable<DynamicVar> BuildCanonicalVars()
        {
            var additionalVars = AdditionalCanonicalVars?.ToArray()
                                 ?? throw new InvalidOperationException(
                                     $"{nameof(AdditionalCanonicalVars)} cannot be null.");
            if (additionalVars.Any(static dynVar => dynVar == null))
                throw new InvalidOperationException(
                    $"{nameof(AdditionalCanonicalVars)} cannot contain null entries.");
            if (additionalVars.Any(dynVar => dynVar.Name == ExtraTurnCyclesVarName))
                throw new ArgumentException(
                    $"'{ExtraTurnCyclesVarName}' is reserved by {nameof(ModTemporaryPowerTemplate)}. " +
                    $"Add a differently-named var via {nameof(AdditionalCanonicalVars)}."
                );

            yield return new IntVar(ExtraTurnCyclesVarName, 0);
            foreach (var dynVar in additionalVars)
                yield return dynVar;
        }

        private IEnumerable<IHoverTip> ResolveExtraHoverTips()
        {
            var tips = OriginModel switch
            {
                CardModel card => [HoverTipFactory.FromCard(card)],
                PotionModel potion => [HoverTipFactory.FromPotion(potion)],
                RelicModel relic => HoverTipFactory.FromRelic(relic).ToList(),
                PowerModel power => [HoverTipFactory.FromPower(power)],
                _ => [],
            };
            tips.Add(HoverTipFactory.FromPower(InternallyAppliedPower));
            return tips;
        }

        private Task ApplyInternalPower(
            PlayerChoiceContext choiceContext,
            Creature target,
            decimal amount,
            Creature? applier,
            CardModel? cardSource,
            bool silent = false)
        {
            var powerType = InternallyAppliedPower.GetType();
            if (_cachedInternalPowerType == powerType && _cachedInternalPowerInvoker != null)
                return _cachedInternalPowerInvoker(choiceContext, target, amount, applier, cardSource, silent);
            var method = ApplyInternalPowerGenericMethod.MakeGenericMethod(powerType);
            _cachedInternalPowerInvoker = method.CreateDelegate<ApplyInternalPowerInvoker>();
            _cachedInternalPowerType = powerType;

            return _cachedInternalPowerInvoker(choiceContext, target, amount, applier, cardSource, silent);
        }

        private static Task ApplyInternalPowerGeneric<TPower>(PlayerChoiceContext choiceContext, Creature target,
            decimal amount,
            Creature? applier, CardModel? cardSource, bool silent) where TPower : PowerModel
        {
#if !STS2_AT_LEAST_0_104_0
            return PowerCmd.Apply<TPower>(target, amount, applier, cardSource, silent);
#else
            return PowerCmd.Apply<TPower>(choiceContext, target, amount, applier, cardSource, silent);
#endif
        }

        private delegate Task ApplyInternalPowerInvoker(
            PlayerChoiceContext choiceContext,
            Creature target,
            decimal amount,
            Creature? applier,
            CardModel? cardSource,
            bool silent);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds a temporary-power wrapper to a specific origin model and internal power type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将临时能力包装绑定到指定的来源模型和内部能力类型。
    ///     </para>
    /// </summary>
    /// <typeparam name="TOriginModel">
    ///     <para xml:lang="en">The model type that grants the temporary power.</para>
    ///     <para xml:lang="zh-CN">授予该临时能力的模型类型。</para>
    /// </typeparam>
    /// <typeparam name="TPower">
    ///     <para xml:lang="en">The internal power type to apply temporarily.</para>
    ///     <para xml:lang="zh-CN">要临时应用的内部能力类型。</para>
    /// </typeparam>
    public abstract class ModTemporaryAppliedPowerTemplate<TOriginModel, TPower> : ModTemporaryPowerTemplate
        where TOriginModel : AbstractModel
        where TPower : PowerModel
    {
        /// <inheritdoc />
        public override AbstractModel OriginModel => ModelDb.GetById<AbstractModel>(ModelDb.GetId<TOriginModel>());

        /// <inheritdoc />
        public override PowerModel InternallyAppliedPower => ModelDb.Power<TPower>();
    }
}
