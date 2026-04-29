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
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Combat.Powers
{
    /// <summary>
    ///     Extensible temporary-power template that temporarily applies an arbitrary internal power model.
    /// </summary>
    public abstract class ModTemporaryPowerTemplate : ModPowerTemplate, ITemporaryPower
    {
        /// <summary>
        ///     Reserved dynamic var name used by this template to track extra expiry cycles and (optionally) expose the value
        ///     to localization formatting (e.g. <c>{ExtraTurns}</c>).
        /// </summary>
        public const string ExtraTurnCyclesVarName = "ExtraTurns";

        private static readonly MethodInfo ApplyInternalPowerGenericMethod =
            typeof(ModTemporaryPowerTemplate).GetMethod(nameof(ApplyInternalPowerGeneric),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private bool _shouldIgnoreNextInstance;

        /// <summary>
        ///     Matches vanilla temporary Strength/Dexterity/Focus semantics:
        ///     true means this temporary power is treated as positive; false as negative.
        /// </summary>
        protected virtual bool IsPositive => true;

        /// <summary>
        ///     When true, expires on the other side's turn end; otherwise on owner's side turn end.
        /// </summary>
        protected virtual bool UntilEndOfOtherSideTurn => false;

        /// <summary>
        ///     Extra owner/opponent turn cycles before this temporary effect expires.
        /// </summary>
        protected virtual int LastForXExtraTurns => 0;

        /// <summary>
        ///     Additional dynamic vars for localization display.
        ///     Use this instead of overriding <see cref="CanonicalVars" />, which is reserved by the template.
        ///     <para>
        ///         <see cref="ExtraTurnCyclesVarName" /> is reserved and will always be provided by the template.
        ///         Do not include it in <see cref="AdditionalCanonicalVars" />.
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
        public override bool IsInstanced => LastForXExtraTurns != 0;

        /// <summary>
        ///     Remaining extra expiry cycles before removal.
        ///     Stored in <see cref="ExtraTurnCyclesVarName" /> for optional localization display.
        /// </summary>
        public int RemainingExtraTurnCycles
        {
            get => (int)DynamicVars[ExtraTurnCyclesVarName].BaseValue;
            set => DynamicVars[ExtraTurnCyclesVarName].BaseValue = Math.Max(value, 0);
        }

        /// <inheritdoc />
        public override LocString Title => ResolveOriginTitle();

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
        /// </summary>
        protected sealed override IEnumerable<DynamicVar> CanonicalVars => BuildCanonicalVars();

        /// <summary>
        ///     The model that granted this temporary power (card/potion/relic/power/orb/etc.).
        ///     Used for title and hover-tip resolution.
        /// </summary>
        public abstract AbstractModel OriginModel { get; }

        /// <summary>
        ///     The internal power model that is applied/removed while this temporary wrapper exists.
        /// </summary>
        public abstract PowerModel InternallyAppliedPower { get; }

        /// <summary>
        ///     Suppresses the next application/amount-change instance, matching vanilla temporary power semantics.
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
#if STS2_V_0_103_2
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

#if STS2_V_0_103_2
            await ApplyInternalPower(new ThrowingPlayerChoiceContext(), Owner, SignedAmount(amount), applier,
                cardSource, true);
#else
            await ApplyInternalPower(choiceContext, Owner, SignedAmount(amount), applier, cardSource, true);
#endif
        }

        /// <inheritdoc />
        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            var expiresOnThisSide = UntilEndOfOtherSideTurn ? side != Owner.Side : side == Owner.Side;
            if (!expiresOnThisSide)
                return;

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

        private LocString ResolveOriginTitle()
        {
            return OriginModel switch
            {
                CardModel cardModel => cardModel.TitleLocString,
                PotionModel potionModel => potionModel.Title,
                RelicModel relicModel => relicModel.Title,
                PowerModel powerModel => powerModel.Title,
                OrbModel orbModel => orbModel.Title,
                CharacterModel characterModel => characterModel.Title,
                MonsterModel monsterModel => monsterModel.Title,
                _ => InternallyAppliedPower.Title,
            };
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

        private Task ApplyInternalPower(PlayerChoiceContext choiceContext, Creature target, decimal amount,
            Creature? applier,
            CardModel? cardSource, bool silent = false)
        {
            var method = ApplyInternalPowerGenericMethod.MakeGenericMethod(InternallyAppliedPower.GetType());
            if (method.Invoke(null, [choiceContext, target, amount, applier, cardSource, silent]) is Task task)
                return task;
            return Task.CompletedTask;
        }

        private static Task ApplyInternalPowerGeneric<TPower>(PlayerChoiceContext choiceContext, Creature target,
            decimal amount,
            Creature? applier, CardModel? cardSource, bool silent) where TPower : PowerModel
        {
#if STS2_V_0_103_2
            return PowerCmd.Apply<TPower>(target, amount, applier, cardSource, silent);
#else
            return PowerCmd.Apply<TPower>(choiceContext, target, amount, applier, cardSource, silent);
#endif
        }
    }

    /// <summary>
    ///     Generic helper that binds a temporary wrapper to a specific origin model and internal power type.
    /// </summary>
    /// <typeparam name="TOriginModel">Source model that grants this temporary power.</typeparam>
    /// <typeparam name="TPower">Internal power type that gets temporarily applied.</typeparam>
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
