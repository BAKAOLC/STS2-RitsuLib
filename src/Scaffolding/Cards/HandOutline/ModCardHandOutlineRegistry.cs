using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers custom hand-card outline colors. Rules for base card types also apply to derived types.
    ///     </para>
    ///     <para xml:lang="zh-CN">注册自定义手牌描边颜色；为卡牌基类注册的规则也适用于其派生类型。</para>
    /// </summary>
    public static class ModCardHandOutlineRegistry
    {
        private static int _hasAny;
        private static int _sequence;

        private static readonly ConcurrentDictionary<Type, List<RegisteredRule>> RulesByCardType = new();
        private static readonly ConcurrentDictionary<int, byte> LoggedRuleFailures = new();

        internal static bool HasAny => Volatile.Read(ref _hasAny) != 0;

        /// <summary>
        ///     <para xml:lang="en">Registers an untyped rule set for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册非泛型规则集。</para>
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineRules rules) where TCard : CardModel
        {
            Register(typeof(TCard), rules);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a typed rule set for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册泛型规则集。</para>
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineRules<TCard> rules) where TCard : CardModel
        {
            Register<TCard>(rules.ToUntyped());
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a typed rule for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册泛型规则。</para>
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineSwitchRule<TCard> rule) where TCard : CardModel
        {
#pragma warning disable CS0618 // Type or member is obsolete.
            Register<TCard>(rule.ToUntyped());
#pragma warning restore CS0618 // Type or member is obsolete.
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an untyped rule for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册非泛型规则。</para>
        /// </summary>
        [Obsolete(
            "Use Register<TCard>(ModCardHandOutlineSwitchRule<TCard>) or Register<TCard>(ModCardHandOutlineRules<TCard>).")]
        public static void Register<TCard>(ModCardHandOutlineSwitchRule rule) where TCard : CardModel
        {
            Register(typeof(TCard), rule);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers typed rules for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册多条泛型规则。</para>
        /// </summary>
        public static void Register<TCard>(params ModCardHandOutlineSwitchRule<TCard>[] rules) where TCard : CardModel
        {
            Register(ModCardHandOutlineRules<TCard>.Of(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers untyped rules for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册多条非泛型规则。</para>
        /// </summary>
        [Obsolete(
            "Use Register<TCard>(params ModCardHandOutlineSwitchRule<TCard>[]) or Register<TCard>(ModCardHandOutlineRules<TCard>).")]
        public static void Register<TCard>(params ModCardHandOutlineSwitchRule[] rules) where TCard : CardModel
        {
            Register<TCard>(ModCardHandOutlineRules.Of(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an untyped rule set for a <see cref="CardModel" /> subtype.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="CardModel" /> 子类型注册非泛型规则集。</para>
        /// </summary>
        public static void Register(Type cardType, ModCardHandOutlineRules rules)
        {
            ValidateRegistration(cardType);

            foreach (var rule in rules.Enumerate())
                Register(cardType, rule);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an untyped rule for a <see cref="CardModel" /> subtype.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="CardModel" /> 子类型注册非泛型规则。</para>
        /// </summary>
        public static void Register(Type cardType, ModCardHandOutlineSwitchRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule.ColorWhen);
            ValidateRegistration(cardType);

            var seq = Interlocked.Increment(ref _sequence);
            var wrapped = new RegisteredRule(rule, seq);

            RulesByCardType.AddOrUpdate(
                cardType,
                _ => [wrapped],
                (_, existing) =>
                {
                    var copy = new List<RegisteredRule>(existing) { wrapped };
                    return copy;
                });
            Volatile.Write(ref _hasAny, 1);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers untyped rules for a <see cref="CardModel" /> subtype.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="CardModel" /> 子类型注册多条非泛型规则。</para>
        /// </summary>
        public static void Register(Type cardType, params ModCardHandOutlineSwitchRule[] rules)
        {
            Register(cardType, ModCardHandOutlineRules.Of(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a legacy rule for <typeparamref name="TCard" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TCard" /> 注册旧版规则。</para>
        /// </summary>
        [Obsolete("Use Register<TCard>(ModCardHandOutlineRules) or Register<TCard>(ModCardHandOutlineSwitchRule).")]
        public static void Register<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            Register<TCard>(rule.ToSwitchRule());
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a legacy rule for a <see cref="CardModel" /> subtype.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="CardModel" /> 子类型注册旧版规则。</para>
        /// </summary>
        [Obsolete("Use Register(Type, ModCardHandOutlineRules) or Register(Type, ModCardHandOutlineSwitchRule).")]
        public static void Register(Type cardType, ModCardHandOutlineRule rule)
        {
            Register(cardType, rule.ToSwitchRule());
        }

        /// <summary>
        ///     <para xml:lang="en">Clears all rules for tests or tooling.</para>
        ///     <para xml:lang="zh-CN">清除所有规则，供测试或工具使用。</para>
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
            LoggedRuleFailures.Clear();
            Volatile.Write(ref _hasAny, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the highest-priority matching outline to a hand-card holder.</para>
        ///     <para xml:lang="zh-CN">将优先级最高的匹配描边应用到手牌容器。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if an outline was applied.</para>
        ///     <para xml:lang="zh-CN">成功应用描边时为 <see langword="true" />。</para>
        /// </returns>
        public static bool TryRefreshOutlineForHolder(NHandCardHolder? holder)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!ModCardHandOutlinePatchHelper.TryGetRule(holder, out var model, out var evaluation))
                return false;

            return ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, evaluation);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies the highest-priority matching outline only when its rule requests per-frame refresh.
        ///     </para>
        ///     <para xml:lang="zh-CN">仅当优先级最高的匹配规则要求逐帧刷新时应用描边。</para>
        /// </summary>
        public static bool TryRefreshDynamicOutlineForHolder(NHandCardHolder? holder)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(holder, out var model, out var evaluation) ||
                !evaluation.Rule.RefreshEveryFrame)
                return false;

            return ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, evaluation);
        }

        internal static ModCardHandOutlineEvaluation? EvaluateBest(CardModel model)
        {
            RegisteredRule? best = null;
            Color bestColor = default;

            for (var t = model.GetType();
                 t != null && typeof(CardModel).IsAssignableFrom(t);
                 t = t.BaseType)
            {
                if (!RulesByCardType.TryGetValue(t, out var list))
                    continue;

                foreach (var entry in list)
                {
                    Color? color;
                    try
                    {
                        color = entry.Rule.ResolveColor(model);
                    }
                    catch (Exception ex)
                    {
                        if (LoggedRuleFailures.TryAdd(entry.Sequence, 0))
                            RitsuLibFramework.Logger.Warn(
                                $"[CardHandOutline] Rule {entry.Sequence} for {t.FullName} threw and was ignored: {ex}");
                        continue;
                    }

                    if (!color.HasValue)
                        continue;

                    if (best is not null && IsLowerPriority(entry, best.Value))
                        continue;

                    best = entry;
                    bestColor = color.Value;
                }
            }

            return best.HasValue ? new(best.Value.Rule, bestColor) : null;
        }

        private static bool IsLowerPriority(RegisteredRule candidate, RegisteredRule best)
        {
            return candidate.Rule.Priority < best.Rule.Priority
                   || (candidate.Rule.Priority == best.Rule.Priority && candidate.Sequence <= best.Sequence);
        }

        private static void ValidateRegistration(Type cardType)
        {
            ArgumentNullException.ThrowIfNull(cardType);

            if (ModContentRegistry.IsFrozen)
                throw new InvalidOperationException(
                    "Cannot register card hand outline rules after content registration has been frozen. " +
                    "Register from your mod initializer before ModelDb initializes.");

            if (!typeof(CardModel).IsAssignableFrom(cardType))
                throw new ArgumentException(
                    $"Type '{cardType.FullName}' must be a subtype of {typeof(CardModel).FullName}.",
                    nameof(cardType));
        }

        private readonly record struct RegisteredRule(ModCardHandOutlineSwitchRule Rule, int Sequence);
    }
}
