using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Immutable input passed to a context-based computed dynamic-var factory.
    ///     传递给上下文式计算动态变量工厂的不可变输入。
    /// </summary>
    public sealed class ComputedDynamicVarContext
    {
        internal ComputedDynamicVarContext(
            DynamicVar variable,
            AbstractModel? modelOwner,
            CardModel? card,
            Creature? target,
            CardPreviewMode? previewMode,
            bool runGlobalHooks)
        {
            Variable = variable;
            ModelOwner = modelOwner;
            Card = card;
            Target = target;
            PreviewMode = previewMode;
            RunGlobalHooks = runGlobalHooks;
        }

        /// <summary>
        ///     Dynamic variable currently being evaluated.
        ///     当前正在求值的动态变量。
        /// </summary>
        public DynamicVar Variable { get; }

        /// <summary>
        ///     Model assigned through <see cref="DynamicVar.SetOwner" />.
        ///     通过 <see cref="DynamicVar.SetOwner" /> 分配的模型。
        /// </summary>
        public AbstractModel? ModelOwner { get; }

        /// <summary>
        ///     Effective card for this evaluation. During enchantment preview this may differ from
        ///     <see cref="ModelOwner" />.
        ///     此次求值使用的有效卡牌。附魔预览期间，它可能不同于 <see cref="ModelOwner" />。
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     Current preview or explicit calculation target.
        ///     当前预览目标或显式计算目标。
        /// </summary>
        public Creature? Target { get; }

        /// <summary>
        ///     Preview mode, or <see langword="null" /> during live/current-value evaluation.
        ///     预览模式；实时/当前值求值期间为 <see langword="null" />。
        /// </summary>
        public CardPreviewMode? PreviewMode { get; }

        /// <summary>
        ///     Whether global hooks should participate in this preview evaluation.
        ///     此次预览求值是否应运行全局 hook。
        /// </summary>
        public bool RunGlobalHooks { get; }

        /// <summary>
        ///     Variable name.
        ///     变量名称。
        /// </summary>
        public string Name => Variable.Name;

        /// <summary>
        ///     Mutable/upgradable base value stored by the variable.
        ///     变量存储的可变/可升级基础值。
        /// </summary>
        public decimal BaseValue => Variable.BaseValue;

        /// <summary>
        ///     Whether this invocation is calculating a card preview.
        ///     此次调用是否正在计算卡牌预览。
        /// </summary>
        public bool IsPreview => PreviewMode.HasValue;

        /// <summary>
        ///     Whether this invocation is calculating a live/current value.
        ///     此次调用是否正在计算实时/当前值。
        /// </summary>
        public bool IsCurrentValue => !IsPreview;

        /// <summary>
        ///     Whether this is a normal card preview.
        ///     是否为普通卡牌预览。
        /// </summary>
        public bool IsNormalPreview => PreviewMode == CardPreviewMode.Normal;

        /// <summary>
        ///     Whether a card is available.
        ///     是否存在卡牌。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool HasCard => Card != null;

        /// <summary>
        ///     Whether a target is available.
        ///     是否存在目标。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Target))]
        public bool HasTarget => Target != null;

        /// <summary>
        ///     Whether the effective card is mutable.
        ///     有效卡牌是否为可变实例。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsMutableCard => Card is { IsMutable: true };

        /// <summary>
        ///     Whether the effective card is canonical.
        ///     有效卡牌是否为 canonical 实例。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsCanonicalCard => Card is { IsCanonical: true };

        /// <summary>
        ///     Whether the effective card is upgraded.
        ///     有效卡牌是否已升级。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsUpgraded => Card?.IsUpgraded == true;

        /// <summary>
        ///     Whether the effective card is currently an enchantment preview.
        ///     有效卡牌当前是否为附魔预览。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsEnchantmentPreview => Card?.IsEnchantmentPreview == true;

        /// <summary>
        ///     Whether this is an upgrade preview.
        ///     是否为升级预览。
        /// </summary>
        public bool IsUpgradePreview => PreviewMode == CardPreviewMode.Upgrade;

        /// <summary>
        ///     Whether this is a multi-creature-targeting preview.
        ///     是否为多生物目标预览。
        /// </summary>
        public bool IsMultiTargetPreview => PreviewMode == CardPreviewMode.MultiCreatureTargeting;

        /// <summary>
        ///     Whether this preview should apply global hooks.
        ///     此次预览是否应应用全局 hook。
        /// </summary>
        public bool ShouldRunGlobalHooks => IsPreview && RunGlobalHooks;

        /// <summary>
        ///     Player owning the effective mutable card, when available. Canonical cards return
        ///     <see langword="null" /> without invoking their guarded <c>Owner</c> getter.
        ///     有效可变卡牌的玩家拥有者（如果存在）。canonical 卡牌不会调用受保护的 <c>Owner</c> getter，而是返回
        ///     <see langword="null" />。
        /// </summary>
        public Player? Player => Card is { IsMutable: true } ? Card.Owner : null;

        /// <summary>
        ///     Whether a player owner is available.
        ///     是否存在玩家拥有者。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Player))]
        public bool HasPlayer => Player != null;

        /// <summary>
        ///     Creature that owns/uses the effective card, when available.
        ///     拥有/使用有效卡牌的生物（如果存在）。
        /// </summary>
        public Creature? SourceCreature => Player?.Creature;

        /// <summary>
        ///     Whether a source creature is available.
        ///     是否存在来源生物。
        /// </summary>
        [MemberNotNullWhen(true, nameof(SourceCreature))]
        public bool HasSourceCreature => SourceCreature != null;

        /// <summary>
        ///     Run containing the effective card, when available.
        ///     包含有效卡牌的跑局（如果存在）。
        /// </summary>
        public IRunState? RunState => Card?.RunState;

        /// <summary>
        ///     Whether a run state is available.
        ///     是否存在跑局状态。
        /// </summary>
        [MemberNotNullWhen(true, nameof(RunState))]
        public bool HasRunState => RunState != null;

        /// <summary>
        ///     Active combat associated with the card or its owner. This intentionally falls back to the owner's
        ///     combat for cards in non-combat piles.
        ///     与卡牌或其拥有者关联的当前战斗。对于位于非战斗牌堆中的卡牌，此属性会有意回退到拥有者的战斗。
        /// </summary>
        public ICombatState? CombatState => Card?.CombatState ?? SourceCreature?.CombatState;

        /// <summary>
        ///     Whether a combat state is available.
        ///     是否存在战斗状态。
        /// </summary>
        [MemberNotNullWhen(true, nameof(CombatState))]
        public bool HasCombatState => CombatState != null;

        /// <summary>
        ///     Lowest-level scope reported by the card.
        ///     卡牌报告的最低层级作用域。
        /// </summary>
        public ICardScope? CardScope => Card?.CardScope;

        /// <summary>
        ///     Whether a card scope is available.
        ///     是否存在卡牌作用域。
        /// </summary>
        [MemberNotNullWhen(true, nameof(CardScope))]
        public bool HasCardScope => CardScope != null;

        /// <summary>
        ///     Whether the card belongs to a run.
        ///     卡牌是否属于某个跑局。
        /// </summary>
        [MemberNotNullWhen(true, nameof(RunState))]
        public bool IsInRun => HasRunState;

        /// <summary>
        ///     Whether the card owner currently participates in a combat. This may be true for a card in a
        ///     non-combat pile such as the deck.
        ///     卡牌拥有者当前是否处于战斗。即使卡牌位于牌库等非战斗牌堆中，此值也可能为 true。
        /// </summary>
        [MemberNotNullWhen(true, nameof(CombatState))]
        public bool IsInCombat => HasCombatState;

        /// <summary>
        ///     Whether the card itself currently reports a combat scope.
        ///     卡牌自身当前是否报告战斗作用域。
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsCardInCombat => Card?.CombatState != null;

        /// <summary>
        ///     Dynamic variables belonging to the effective card, when available.
        ///     有效卡牌拥有的动态变量（如果存在）。
        /// </summary>
        public DynamicVarSet? CardVars => Card?.DynamicVars;

        /// <summary>
        ///     Tries to read a dynamic variable from <see cref="CardVars" />.
        ///     尝试从 <see cref="CardVars" /> 读取动态变量。
        /// </summary>
        public bool TryGetCardVar(string name, [MaybeNullWhen(false)] out DynamicVar dynamicVar)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (CardVars is { } vars && vars.TryGetValue(name, out dynamicVar))
                return true;

            dynamicVar = null;
            return false;
        }

        /// <summary>
        ///     Tries to read a typed dynamic variable from <see cref="CardVars" />.
        ///     尝试从 <see cref="CardVars" /> 读取指定类型的动态变量。
        /// </summary>
        public bool TryGetCardVar<TVar>(string name, [MaybeNullWhen(false)] out TVar dynamicVar)
            where TVar : DynamicVar
        {
            if (TryGetCardVar(name, out var value) && value is TVar typed)
            {
                dynamicVar = typed;
                return true;
            }

            dynamicVar = null;
            return false;
        }

        /// <summary>
        ///     Reads a required typed card variable.
        ///     读取必需的指定类型卡牌变量。
        /// </summary>
        public TVar GetRequiredCardVar<TVar>(string name) where TVar : DynamicVar
        {
            if (TryGetCardVar<TVar>(name, out var dynamicVar))
                return dynamicVar;

            throw new KeyNotFoundException(
                $"Card dynamic var '{name}' was missing or was not a {typeof(TVar).Name}.");
        }

        /// <summary>
        ///     Reads a card variable's base value, or returns <paramref name="defaultValue" />.
        ///     读取卡牌变量的基础值；不存在时返回 <paramref name="defaultValue" />。
        /// </summary>
        public decimal GetCardBaseValueOrDefault(string name, decimal defaultValue = 0m)
        {
            return TryGetCardVar(name, out var dynamicVar) ? dynamicVar.BaseValue : defaultValue;
        }

        /// <summary>
        ///     Reads a card variable's integer base value, or returns <paramref name="defaultValue" />.
        ///     读取卡牌变量的整数基础值；不存在时返回 <paramref name="defaultValue" />。
        /// </summary>
        public int GetCardIntOrDefault(string name, int defaultValue = 0)
        {
            return TryGetCardVar(name, out var dynamicVar) ? dynamicVar.IntValue : defaultValue;
        }

        /// <summary>
        ///     Evaluates a computed card variable for the current target, or reads a regular variable's base value.
        ///     Missing variables return <paramref name="defaultValue" />.
        ///     使用当前目标求值计算型卡牌变量；普通变量则读取基础值。变量不存在时返回
        ///     <paramref name="defaultValue" />。
        /// </summary>
        public decimal EvaluateCardVarOrDefault(string name, decimal defaultValue = 0m)
        {
            if (!TryGetCardVar(name, out var dynamicVar))
                return defaultValue;

            if (ReferenceEquals(dynamicVar, Variable))
                return BaseValue;

            if (PreviewMode is not { } previewMode || Card == null)
                return dynamicVar is IComputedDynamicVar computed
                    ? computed.Calculate(Target)
                    : dynamicVar.BaseValue;
            dynamicVar.UpdateCardPreview(Card, previewMode, Target, RunGlobalHooks);
            return dynamicVar.PreviewValue;

        }
    }

    /// <summary>
    ///     Calculates a dynamic value from a stable evaluation context.
    ///     根据稳定的求值上下文计算动态值。
    /// </summary>
    public delegate decimal ComputedDynamicVarFactory(ComputedDynamicVarContext context);

    /// <summary>
    ///     Common live-value contract implemented by RitsuLib computed dynamic variables.
    ///     RitsuLib 计算型动态变量实现的通用实时值接口。
    /// </summary>
    public interface IComputedDynamicVar
    {
        /// <summary>
        ///     Calculates the current value for an optional target.
        ///     计算可选目标对应的当前值。
        /// </summary>
        decimal Calculate(Creature? target = null);
    }

    internal sealed class ComputedDynamicVarEvaluator
    {
        private readonly ComputedDynamicVarFactory? _contextFactory;
        private readonly Func<CardModel?, Creature?, decimal>? _currentValueFactory;
        private readonly Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? _previewValueFactory;

        internal ComputedDynamicVarEvaluator(
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
            _currentValueFactory = currentValueFactory;
            _previewValueFactory = previewValueFactory;
        }

        internal ComputedDynamicVarEvaluator(ComputedDynamicVarFactory contextFactory)
        {
            ArgumentNullException.ThrowIfNull(contextFactory);
            _contextFactory = contextFactory;
        }

        internal decimal Calculate(DynamicVar variable, AbstractModel? modelOwner, Creature? target)
        {
            return _contextFactory?.Invoke(new(variable, modelOwner, modelOwner as CardModel, target, null, false))
                   ?? _currentValueFactory!(modelOwner as CardModel, target);
        }

        internal decimal CalculatePreview(
            DynamicVar variable,
            AbstractModel? modelOwner,
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            if (_contextFactory != null)
                return _contextFactory(new(
                    variable,
                    modelOwner,
                    card,
                    target,
                    previewMode,
                    runGlobalHooks));

            return _previewValueFactory?.Invoke(card, previewMode, target, runGlobalHooks)
                   ?? _currentValueFactory!(card, target);
        }
    }
}
