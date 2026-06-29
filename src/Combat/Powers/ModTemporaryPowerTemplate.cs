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
    ///     Extensible temporary-power template that temporarily applies an arbitrary internal power model.
    ///     可扩展的临时能力模板，会临时应用任意内部能力模型。
    /// </summary>
    public abstract class ModTemporaryPowerTemplate : ModPowerTemplate, ITemporaryPower
    {
        /// <summary>
        ///     Reserved dynamic var name used by this template to track extra expiry cycles and (optionally) expose the value
        ///     to localization formatting (e.g. <c>{ExtraTurns}</c>).
        ///     此模板使用的保留动态变量名，用于跟踪额外过期周期，并可选将该值暴露
        ///     给本地化格式化（例如 <c>{ExtraTurns}</c>）。
        /// </summary>
        public const string ExtraTurnCyclesVarName = "ExtraTurns";

        private static readonly MethodInfo ApplyInternalPowerGenericMethod =
            typeof(ModTemporaryPowerTemplate).GetMethod(nameof(ApplyInternalPowerGeneric),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private ApplyInternalPowerInvoker? _cachedInternalPowerInvoker;

        private Type? _cachedInternalPowerType;

        private bool _shouldIgnoreNextInstance;

        /// <summary>
        ///     Matches vanilla temporary Strength/Dexterity/Focus semantics:
        ///     true means this temporary power is treated as positive; false as negative.
        ///     匹配原版临时 Strength/Dexterity/Focus 语义：
        ///     true 表示此临时能力视为正面；false 表示视为负面。
        /// </summary>
        protected virtual bool IsPositive => true;

        /// <summary>
        ///     When true, expires on the other side's turn end; otherwise on owner's side turn end.
        ///     为 true 时，在另一方回合结束时过期；否则在拥有者一方回合结束时过期。
        /// </summary>
        protected virtual bool UntilEndOfOtherSideTurn => false;

        /// <summary>
        ///     Extra owner/opponent turn cycles before this temporary effect expires.
        ///     此临时效果过期前的额外拥有者/对手回合周期。
        /// </summary>
        protected virtual int LastForXExtraTurns => 0;

        /// <summary>
        ///     Additional dynamic vars for localization display.
        ///     Use this instead of overriding <see cref="CanonicalVars" />, which is reserved by the template.
        ///     <para>
        ///         <see cref="ExtraTurnCyclesVarName" /> is reserved and will always be provided by the template.
        ///         Do not include it in <see cref="AdditionalCanonicalVars" />.
        ///     </para>
        ///     用于本地化显示的额外动态变量。
        ///     请使用此项，而不是重写 <see cref="CanonicalVars" />；后者由模板保留。
        ///     <para>
        ///         <see cref="ExtraTurnCyclesVarName" /> 为保留项，模板始终会提供它。
        ///         不要将它包含在 <see cref="AdditionalCanonicalVars" /> 中。
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
        public override bool IsInstanced => LastForXExtraTurns != 0;
#else
        public override PowerInstanceType InstanceType =>
            LastForXExtraTurns != 0 ? PowerInstanceType.Instanced : PowerInstanceType.None;
#endif

        /// <summary>
        ///     Remaining extra expiry cycles before removal.
        ///     Stored in <see cref="ExtraTurnCyclesVarName" /> for optional localization display.
        ///     移除前剩余的额外过期周期。
        ///     存储在 <see cref="ExtraTurnCyclesVarName" /> 中，可选用于本地化显示。
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

        /// <inheritdoc />
        /// <summary>
        ///     Canonical dynamic vars reserved by the template.
        ///     <para>
        ///         This template always defines <see cref="ExtraTurnCyclesVarName" /> (<c>{ExtraTurns}</c>) for its
        ///         internal expiry counter and for optional localization display. Do not attempt to override this.
        ///     </para>
        ///     <para>
        ///         To add additional dynamic vars for localization, override <see cref="AdditionalCanonicalVars" /> instead.
        ///     </para>
        ///     模板保留的规范动态变量。
        ///     <para>
        ///         此模板始终定义 <see cref="ExtraTurnCyclesVarName" />（<c>{ExtraTurns}</c>），用于其
        ///         内部过期计数器以及可选的本地化显示。不要尝试覆盖它。
        ///     </para>
        ///     <para>
        ///         若要为本地化添加额外动态变量，请改为重写 <see cref="AdditionalCanonicalVars" />。
        ///     </para>
        /// </summary>
        protected sealed override IEnumerable<DynamicVar> CanonicalVars => BuildCanonicalVars();

        /// <summary>
        ///     The model that granted this temporary power (card/potion/relic/power/orb/etc.).
        ///     Used for title and hover-tip resolution.
        ///     授予此临时能力的模型（卡牌/药水/遗物/能力/orb 等）。
        ///     用于标题和悬停提示解析。
        /// </summary>
        public abstract AbstractModel OriginModel { get; }

        /// <summary>
        ///     The internal power model that is applied/removed while this temporary wrapper exists.
        ///     此临时包装存在期间会被应用/移除的内部能力模型。
        /// </summary>
        public abstract PowerModel InternallyAppliedPower { get; }

        /// <summary>
        ///     Suppresses the next application/amount-change instance, matching vanilla temporary power semantics.
        ///     抑制下一次应用/数值变更实例，匹配原版临时能力语义。
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
                RemainingExtraTurnCycles = LastForXExtraTurns;
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
        ///     Applies vanilla-style sign mapping to amount using <see cref="IsPositive" />.
        ///     使用 <see cref="IsPositive" /> 对 amount 应用原版风格的符号映射。
        /// </summary>
        protected virtual decimal SignedAmount(decimal amount)
        {
            return IsPositive ? amount : -amount;
        }

        private IEnumerable<DynamicVar> BuildCanonicalVars()
        {
            if (AdditionalCanonicalVars.Any(dynVar => dynVar.Name == ExtraTurnCyclesVarName))
                throw new ArgumentException(
                    $"'{ExtraTurnCyclesVarName}' is reserved by {nameof(ModTemporaryPowerTemplate)}. " +
                    $"Add a differently-named var via {nameof(AdditionalCanonicalVars)}."
                );

            yield return new IntVar(ExtraTurnCyclesVarName, 0);
            foreach (var dynVar in AdditionalCanonicalVars)
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
    ///     Generic helper that binds a temporary wrapper to a specific origin model and internal power type.
    ///     将临时包装绑定到特定来源模型和内部能力类型的泛型辅助方法。
    /// </summary>
    /// <typeparam name="TOriginModel">
    ///     Source model that grants this temporary power.
    ///     授予此临时能力的来源模型。
    /// </typeparam>
    /// <typeparam name="TPower">
    ///     Internal power type that gets temporarily applied.
    ///     会被临时应用的内部能力类型。
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
