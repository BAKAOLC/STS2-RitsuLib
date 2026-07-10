using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     Per–card-type custom outline colors for the in-hand <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />.
    ///     Applied after vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder.UpdateCard" /> via Harmony.
    ///     手牌中 <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" /> 的逐卡牌类型自定义描边颜色。
    ///     在原版 <see cref="MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder.UpdateCard" /> 之后通过 Harmony 应用。
    /// </summary>
    public static class ModCardHandOutlineRegistry
    {
        private static int _sequence;

        private static readonly ConcurrentDictionary<Type, List<RegisteredRule>> RulesByCardType = new();

        /// <summary>
        ///     Registers a rule for <typeparamref name="TCard" />. Throws if <see cref="ModContentRegistry.IsFrozen" />.
        ///     为 <typeparamref name="TCard" /> 注册规则。如果 <see cref="ModContentRegistry.IsFrozen" /> 则抛出。
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineRules rules) where TCard : CardModel
        {
            Register(typeof(TCard), rules);
        }

        /// <summary>
        ///     Registers rules for <typeparamref name="TCard" />. Throws if <see cref="ModContentRegistry.IsFrozen" />.
        ///     为 <typeparamref name="TCard" /> 注册规则。如果 <see cref="ModContentRegistry.IsFrozen" /> 则抛出。
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineRules<TCard> rules) where TCard : CardModel
        {
            Register<TCard>(rules.ToUntyped());
        }

        /// <summary>
        ///     Registers a rule for <typeparamref name="TCard" />. Throws if <see cref="ModContentRegistry.IsFrozen" />.
        ///     为 <typeparamref name="TCard" /> 注册规则。如果 <see cref="ModContentRegistry.IsFrozen" /> 则抛出。
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineSwitchRule<TCard> rule) where TCard : CardModel
        {
#pragma warning disable CS0618 // Type or member is obsolete.
            Register<TCard>(rule.ToUntyped());
#pragma warning restore CS0618 // Type or member is obsolete.
        }

        /// <summary>
        ///     Registers a type-erased rule for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册类型擦除规则。
        /// </summary>
        [Obsolete(
            "Use Register<TCard>(ModCardHandOutlineSwitchRule<TCard>) or Register<TCard>(ModCardHandOutlineRules<TCard>).")]
        public static void Register<TCard>(ModCardHandOutlineSwitchRule rule) where TCard : CardModel
        {
            Register(typeof(TCard), rule);
        }

        /// <summary>
        ///     Registers several rules for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册多条规则。
        /// </summary>
        public static void Register<TCard>(params ModCardHandOutlineSwitchRule<TCard>[] rules) where TCard : CardModel
        {
            Register(ModCardHandOutlineRules<TCard>.Of(rules));
        }

        /// <summary>
        ///     Registers several type-erased rules for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册多条类型擦除规则。
        /// </summary>
        [Obsolete(
            "Use Register<TCard>(params ModCardHandOutlineSwitchRule<TCard>[]) or Register<TCard>(ModCardHandOutlineRules<TCard>).")]
        public static void Register<TCard>(params ModCardHandOutlineSwitchRule[] rules) where TCard : CardModel
        {
            Register<TCard>(ModCardHandOutlineRules.Of(rules));
        }

        /// <summary>
        ///     Registers rules for <paramref name="cardType" /> (<see cref="CardModel" /> subtype).
        ///     为 <paramref name="cardType" />（<see cref="CardModel" /> 子类型）注册规则。
        /// </summary>
        public static void Register(Type cardType, ModCardHandOutlineRules rules)
        {
            ArgumentNullException.ThrowIfNull(cardType);

            foreach (var rule in rules.Enumerate())
                Register(cardType, rule);
        }

        /// <summary>
        ///     Registers a rule for <paramref name="cardType" /> (<see cref="CardModel" /> subtype).
        ///     为 <paramref name="cardType" />（<see cref="CardModel" /> 子类型）注册规则。
        /// </summary>
        public static void Register(Type cardType, ModCardHandOutlineSwitchRule rule)
        {
            ArgumentNullException.ThrowIfNull(cardType);
            ArgumentNullException.ThrowIfNull(rule.ColorWhen);

            if (ModContentRegistry.IsFrozen)
                throw new InvalidOperationException(
                    "Cannot register card hand outline rules after content registration has been frozen. " +
                    "Register from your mod initializer before ModelDb initializes.");

            if (!typeof(CardModel).IsAssignableFrom(cardType))
                throw new ArgumentException(
                    $"Type '{cardType.FullName}' must be a subtype of {typeof(CardModel).FullName}.",
                    nameof(cardType));

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
        }

        /// <summary>
        ///     Registers several rules for <paramref name="cardType" /> (<see cref="CardModel" /> subtype).
        ///     为 <paramref name="cardType" />（<see cref="CardModel" /> 子类型）注册多条规则。
        /// </summary>
        public static void Register(Type cardType, params ModCardHandOutlineSwitchRule[] rules)
        {
            Register(cardType, ModCardHandOutlineRules.Of(rules));
        }

        /// <summary>
        ///     Registers a legacy rule for <typeparamref name="TCard" />. Throws if <see cref="ModContentRegistry.IsFrozen" />.
        ///     为 <typeparamref name="TCard" /> 注册旧版规则。如果 <see cref="ModContentRegistry.IsFrozen" /> 则抛出。
        /// </summary>
        [Obsolete("Use Register<TCard>(ModCardHandOutlineRules) or Register<TCard>(ModCardHandOutlineSwitchRule).")]
        public static void Register<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            Register<TCard>(rule.ToSwitchRule());
        }

        /// <summary>
        ///     Registers a legacy rule for <paramref name="cardType" /> (<see cref="CardModel" /> subtype).
        ///     为 <paramref name="cardType" />（<see cref="CardModel" /> 子类型）注册旧版规则。
        /// </summary>
        [Obsolete("Use Register(Type, ModCardHandOutlineRules) or Register(Type, ModCardHandOutlineSwitchRule).")]
        public static void Register(Type cardType, ModCardHandOutlineRule rule)
        {
            Register(cardType, rule.ToSwitchRule());
        }

        /// <summary>
        ///     Clears all rules (tests / tooling).
        ///     清除所有规则（测试 / 工具使用）。
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
        }

        /// <summary>
        ///     Applies the best matching registered outline for this holder
        ///     为此 holder 应用最佳匹配的已注册描边。
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if a rule was applied.
        ///     应用了规则时返回 <see langword="true" />。
        /// </returns>
        public static bool TryRefreshOutlineForHolder(NHandCardHolder? holder)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(holder, out var model, out var evaluation))
                return false;

            ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, evaluation);
            return true;
        }

        /// <summary>
        ///     Applies outline only when the matching rule requests per-frame refresh.
        ///     仅当匹配规则请求逐帧刷新时应用描边。
        /// </summary>
        public static bool TryRefreshDynamicOutlineForHolder(NHandCardHolder? holder)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(holder, out var model, out var evaluation) ||
                !evaluation.Rule.RefreshEveryFrame)
                return false;

            ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, evaluation);
            return true;
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
                    var color = entry.Rule.ResolveColor(model);
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

        private readonly record struct RegisteredRule(ModCardHandOutlineSwitchRule Rule, int Sequence);
    }
}
