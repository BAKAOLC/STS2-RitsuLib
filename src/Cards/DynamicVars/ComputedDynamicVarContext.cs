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
    ///     <para xml:lang="en">Provides input to a context-aware computed dynamic-variable evaluator.</para>
    ///     <para xml:lang="zh-CN">为上下文感知的计算型动态变量求值器提供输入。</para>
    /// </summary>
    public sealed class ComputedDynamicVarContext
    {
        private static readonly AsyncLocal<HashSet<DynamicVar>?> EvaluationStack = new();

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
        ///     <para xml:lang="en">Gets the dynamic variable currently being evaluated.</para>
        ///     <para xml:lang="zh-CN">获取当前正在求值的动态变量。</para>
        /// </summary>
        public DynamicVar Variable { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the model assigned through <see cref="DynamicVar.SetOwner" />.</para>
        ///     <para xml:lang="zh-CN">获取通过 <see cref="DynamicVar.SetOwner" /> 指定的模型。</para>
        /// </summary>
        public AbstractModel? ModelOwner { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the effective card for this evaluation. During an enchantment preview, this may differ from
        ///         <see cref="ModelOwner" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此次求值使用的有效卡牌。在附魔预览期间，它可能不同于 <see cref="ModelOwner" />。
        ///     </para>
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the current preview or explicit calculation target.</para>
        ///     <para xml:lang="zh-CN">获取当前预览目标或显式计算目标。</para>
        /// </summary>
        public Creature? Target { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the preview mode, or <see langword="null" /> during current-value evaluation.</para>
        ///     <para xml:lang="zh-CN">获取预览模式；计算当前值时为 <see langword="null" />。</para>
        /// </summary>
        public CardPreviewMode? PreviewMode { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether global hooks should participate in this preview evaluation.</para>
        ///     <para xml:lang="zh-CN">获取此次预览求值是否应运行全局钩子。</para>
        /// </summary>
        public bool RunGlobalHooks { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the variable name.</para>
        ///     <para xml:lang="zh-CN">获取变量名称。</para>
        /// </summary>
        public string Name => Variable.Name;

        /// <summary>
        ///     <para xml:lang="en">Gets the variable's stored, mutable base value.</para>
        ///     <para xml:lang="zh-CN">获取变量存储的可变基础值。</para>
        /// </summary>
        public decimal BaseValue => Variable.BaseValue;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this invocation is evaluating a card preview.</para>
        ///     <para xml:lang="zh-CN">获取此次调用是否正在计算卡牌预览值。</para>
        /// </summary>
        public bool IsPreview => PreviewMode.HasValue;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this invocation is evaluating the current value.</para>
        ///     <para xml:lang="zh-CN">获取此次调用是否正在计算当前值。</para>
        /// </summary>
        public bool IsCurrentValue => !IsPreview;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this is a normal card preview.</para>
        ///     <para xml:lang="zh-CN">获取此次调用是否为普通卡牌预览。</para>
        /// </summary>
        public bool IsNormalPreview => PreviewMode == CardPreviewMode.Normal;

        /// <summary>
        ///     <para xml:lang="en">Gets whether an effective card is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在有效卡牌。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool HasCard => Card != null;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a target is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在目标。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Target))]
        public bool HasTarget => Target != null;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the effective card is mutable.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌是否为可变实例。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsMutableCard => Card is { IsMutable: true };

        /// <summary>
        ///     <para xml:lang="en">Gets whether the effective card is canonical.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌是否为规范实例。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsCanonicalCard => Card is { IsCanonical: true };

        /// <summary>
        ///     <para xml:lang="en">Gets whether the effective card is upgraded.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌是否已升级。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsUpgraded => Card?.IsUpgraded == true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the effective card is an enchantment preview.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌是否为附魔预览。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsEnchantmentPreview => Card?.IsEnchantmentPreview == true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this is an upgrade preview.</para>
        ///     <para xml:lang="zh-CN">获取此次调用是否为升级预览。</para>
        /// </summary>
        public bool IsUpgradePreview => PreviewMode == CardPreviewMode.Upgrade;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this is a multi-creature targeting preview.</para>
        ///     <para xml:lang="zh-CN">获取此次调用是否为多生物目标预览。</para>
        /// </summary>
        public bool IsMultiTargetPreview => PreviewMode == CardPreviewMode.MultiCreatureTargeting;

        /// <summary>
        ///     <para xml:lang="en">Gets whether this preview should apply global hooks.</para>
        ///     <para xml:lang="zh-CN">获取此次预览是否应应用全局钩子。</para>
        /// </summary>
        public bool ShouldRunGlobalHooks => IsPreview && RunGlobalHooks;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the player who owns the effective mutable card. For canonical cards, returns
        ///         <see langword="null" /> without invoking the guarded <c>Owner</c> getter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取有效可变卡牌的所属玩家。对于规范卡牌，不调用受保护的 <c>Owner</c> getter，而是返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        public Player? Player => Card is { IsMutable: true } ? Card.Owner : null;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a player owner is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在所属玩家。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Player))]
        public bool HasPlayer => Player != null;

        /// <summary>
        ///     <para xml:lang="en">Gets the creature that owns or uses the effective card, if available.</para>
        ///     <para xml:lang="zh-CN">获取拥有或使用有效卡牌的生物（如果有）。</para>
        /// </summary>
        public Creature? SourceCreature => Player?.Creature;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a source creature is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在来源生物。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(SourceCreature))]
        public bool HasSourceCreature => SourceCreature != null;

        /// <summary>
        ///     <para xml:lang="en">Gets the run containing the effective card, if available.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌所属的一局游戏（如果有）。</para>
        /// </summary>
        public IRunState? RunState => Card?.RunState;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a run state is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在局内状态。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(RunState))]
        public bool HasRunState => RunState != null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the active combat associated with the card or its owner. For cards outside combat piles, this
        ///         falls back to the owner's combat.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取与卡牌或其拥有者关联的当前战斗。卡牌不在战斗牌堆中时，回退到拥有者的战斗。
        ///     </para>
        /// </summary>
        public ICombatState? CombatState => Card?.CombatState ?? SourceCreature?.CombatState;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a combat state is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在战斗状态。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(CombatState))]
        public bool HasCombatState => CombatState != null;

        /// <summary>
        ///     <para xml:lang="en">Gets the lowest-level scope reported by the card.</para>
        ///     <para xml:lang="zh-CN">获取卡牌报告的最小作用域。</para>
        /// </summary>
        public ICardScope? CardScope => Card?.CardScope;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a card scope is available.</para>
        ///     <para xml:lang="zh-CN">获取是否存在卡牌作用域。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(CardScope))]
        public bool HasCardScope => CardScope != null;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the card belongs to a run.</para>
        ///     <para xml:lang="zh-CN">获取卡牌是否属于一局游戏。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(RunState))]
        public bool IsInRun => HasRunState;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the card owner is currently in combat. This may be <see langword="true" /> for a card in
        ///         a non-combat pile such as the deck.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌拥有者当前是否处于战斗。即使卡牌位于牌组等非战斗牌堆中，此值也可能为
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(CombatState))]
        public bool IsInCombat => HasCombatState;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the card itself currently reports a combat state.</para>
        ///     <para xml:lang="zh-CN">获取卡牌自身当前是否报告战斗状态。</para>
        /// </summary>
        [MemberNotNullWhen(true, nameof(Card))]
        public bool IsCardInCombat => Card?.CombatState != null;

        /// <summary>
        ///     <para xml:lang="en">Gets the effective card's dynamic variables, if available.</para>
        ///     <para xml:lang="zh-CN">获取有效卡牌的动态变量（如果有）。</para>
        /// </summary>
        public DynamicVarSet? CardVars => Card?.DynamicVars;

        /// <summary>
        ///     <para xml:lang="en">Attempts to get a dynamic variable from <see cref="CardVars" />.</para>
        ///     <para xml:lang="zh-CN">尝试从 <see cref="CardVars" /> 获取动态变量。</para>
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
        ///     <para xml:lang="en">Attempts to get a dynamic variable of type <typeparamref name="TVar" /> from <see cref="CardVars" />.</para>
        ///     <para xml:lang="zh-CN">尝试从 <see cref="CardVars" /> 获取 <typeparamref name="TVar" /> 类型的动态变量。</para>
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
        ///     <para xml:lang="en">Gets a required card variable of type <typeparamref name="TVar" />.</para>
        ///     <para xml:lang="zh-CN">获取必需的 <typeparamref name="TVar" /> 类型卡牌变量。</para>
        /// </summary>
        public TVar GetRequiredCardVar<TVar>(string name) where TVar : DynamicVar
        {
            if (TryGetCardVar<TVar>(name, out var dynamicVar))
                return dynamicVar;

            throw new KeyNotFoundException(
                $"Card dynamic var '{name}' was missing or was not a {typeof(TVar).Name}.");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a card variable's base value, or <paramref name="defaultValue" /> when absent.</para>
        ///     <para xml:lang="zh-CN">获取卡牌变量的基础值；变量不存在时返回 <paramref name="defaultValue" />。</para>
        /// </summary>
        public decimal GetCardBaseValueOrDefault(string name, decimal defaultValue = 0m)
        {
            return TryGetCardVar(name, out var dynamicVar) ? dynamicVar.BaseValue : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a card variable's integer value, or <paramref name="defaultValue" /> when absent.</para>
        ///     <para xml:lang="zh-CN">获取卡牌变量的整数值；变量不存在时返回 <paramref name="defaultValue" />。</para>
        /// </summary>
        public int GetCardIntOrDefault(string name, int defaultValue = 0)
        {
            return TryGetCardVar(name, out var dynamicVar) ? dynamicVar.IntValue : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Evaluates a computed card variable for the current target, or reads a regular variable's base value.
        ///         Returns <paramref name="defaultValue" /> when the variable is absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用当前目标计算计算型卡牌变量；对于普通变量则读取基础值。变量不存在时返回
        ///         <paramref name="defaultValue" />。
        ///     </para>
        /// </summary>
        public decimal EvaluateCardVarOrDefault(string name, decimal defaultValue = 0m)
        {
            if (!TryGetCardVar(name, out var dynamicVar))
                return defaultValue;

            if (ReferenceEquals(dynamicVar, Variable))
                return BaseValue;

            var previousStack = EvaluationStack.Value;
            if (previousStack?.Contains(dynamicVar) == true)
                return dynamicVar.BaseValue;

            var currentStack = previousStack is null
                ? new HashSet<DynamicVar>(ReferenceEqualityComparer.Instance)
                : new HashSet<DynamicVar>(previousStack, ReferenceEqualityComparer.Instance);
            currentStack.Add(dynamicVar);
            EvaluationStack.Value = currentStack;

            try
            {
                if (PreviewMode is not { } previewMode || Card == null)
                    return dynamicVar is IComputedDynamicVar computed
                        ? computed.Calculate(Target)
                        : dynamicVar.BaseValue;
                dynamicVar.UpdateCardPreview(Card, previewMode, Target, RunGlobalHooks);
                return dynamicVar.PreviewValue;
            }
            finally
            {
                EvaluationStack.Value = previousStack;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Computes a dynamic value from an evaluation context.</para>
    ///     <para xml:lang="zh-CN">根据求值上下文计算动态值。</para>
    /// </summary>
    public delegate decimal ComputedDynamicVarFactory(ComputedDynamicVarContext context);

    /// <summary>
    ///     <para xml:lang="en">Defines current-value evaluation for RitsuLib computed dynamic variables.</para>
    ///     <para xml:lang="zh-CN">定义 RitsuLib 计算型动态变量的当前值求值接口。</para>
    /// </summary>
    public interface IComputedDynamicVar
    {
        /// <summary>
        ///     <para xml:lang="en">Computes the current value for an optional target.</para>
        ///     <para xml:lang="zh-CN">计算可选目标对应的当前值。</para>
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
