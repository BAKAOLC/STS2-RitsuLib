using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers card-type rules that contribute to <see cref="CardModel.ShouldGlowGold" /> and
    ///         <see cref="CardModel.ShouldGlowRed" />. Rules registered for a base card type also apply to derived types.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册参与计算 <see cref="CardModel.ShouldGlowGold" /> 与 <see cref="CardModel.ShouldGlowRed" />
    ///         的卡牌类型规则；为卡牌基类注册的规则也适用于其派生类型。
    ///     </para>
    /// </summary>
    public static class ModCardHandGlowRegistry
    {
        private static readonly ConcurrentDictionary<string, byte> LoggedRuleFailures = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<Type, List<RegisteredRules>> RulesByCardType = new();
        private static readonly ConcurrentDictionary<Type, ResolvedChannelRules> ResolvedByCardType = new();
        private static int _sequence;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers rules for <typeparamref name="TCard" />. Every matching registration is evaluated until
        ///         one returns <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TCard" /> 注册规则。所有匹配的注册会依次评估，直至其中一项返回
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        public static void Register<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            Register(typeof(TCard), rules);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers rules for a concrete <see cref="CardModel" /> subtype.
        ///     </para>
        ///     <para xml:lang="zh-CN">为具体的 <see cref="CardModel" /> 子类型注册规则。</para>
        /// </summary>
        public static void Register(Type cardType, ModCardHandGlowRules rules)
        {
            ArgumentNullException.ThrowIfNull(cardType);
            if (ModContentRegistry.IsFrozen)
                throw new InvalidOperationException(
                    "Cannot register card hand glow rules after content registration has been frozen. " +
                    "Register from your mod initializer before ModelDb initializes.");

            if (cardType.IsAbstract || cardType.IsInterface || cardType.ContainsGenericParameters ||
                !typeof(CardModel).IsAssignableFrom(cardType))
                throw new ArgumentException(
                    $"Type '{cardType.FullName}' must be a closed concrete subtype of {typeof(CardModel).FullName}.",
                    nameof(cardType));

            var registered = new RegisteredRules(rules, Interlocked.Increment(ref _sequence));
            RulesByCardType.AddOrUpdate(
                cardType,
                _ => [registered],
                (_, existing) => [.. existing, registered]);
            ResolvedByCardType.Clear();
        }

        /// <summary>
        ///     <para xml:lang="en">Clears all rules for tests or hot-reload tooling.</para>
        ///     <para xml:lang="zh-CN">清除所有规则，供测试或热重载工具使用。</para>
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
            ResolvedByCardType.Clear();
            LoggedRuleFailures.Clear();
        }

        internal static bool EvaluateRegistryGold(CardModel card)
        {
            if (RulesByCardType.IsEmpty)
                return false;

            var resolved = ResolvedByCardType.GetOrAdd(card.GetType(), BuildResolvedRules);
            return EvaluateChannel(card, resolved.GoldRules, "gold");
        }

        internal static bool EvaluateRegistryRed(CardModel card)
        {
            if (RulesByCardType.IsEmpty)
                return false;

            var resolved = ResolvedByCardType.GetOrAdd(card.GetType(), BuildResolvedRules);
            return EvaluateChannel(card, resolved.RedRules, "red");
        }

        private static bool EvaluateChannel(
            CardModel card,
            ResolvedRule[] rules,
            string channel)
        {
            foreach (var rule in rules)
            {
                try
                {
                    if (rule.Predicate(card))
                        return true;
                }
                catch (Exception ex)
                {
                    var warningKey = $"{rule.Sequence}|{channel}";
                    if (LoggedRuleFailures.TryAdd(warningKey, 0))
                        RitsuLibFramework.Logger.Warn(
                            $"[CardHandGlow] {channel} rule {rule.Sequence} for {rule.OwnerType.FullName} threw " +
                            $"and was ignored: {ex}");
                }
            }

            return false;
        }

        private static ResolvedChannelRules BuildResolvedRules(Type cardType)
        {
            List<ResolvedRule>? gold = null;
            List<ResolvedRule>? red = null;

            for (var t = cardType;
                 t != null && typeof(CardModel).IsAssignableFrom(t);
                 t = t.BaseType)
            {
                if (!RulesByCardType.TryGetValue(t, out var registrations))
                    continue;

                foreach (var registration in registrations)
                {
                    if (registration.Rules.GoldWhenBonusActive is { } goldPredicate)
                    {
                        gold ??= [];
                        gold.Add(new(goldPredicate, registration.Sequence, t));
                    }

                    if (registration.Rules.RedWhenHandWarning is { } redPredicate)
                    {
                        red ??= [];
                        red.Add(new(redPredicate, registration.Sequence, t));
                    }
                }
            }

            return new(gold?.ToArray() ?? [], red?.ToArray() ?? []);
        }

        private readonly record struct RegisteredRules(ModCardHandGlowRules Rules, int Sequence);

        private readonly record struct ResolvedRule(Func<CardModel, bool> Predicate, int Sequence, Type OwnerType);

        private readonly record struct ResolvedChannelRules(ResolvedRule[] GoldRules, ResolvedRule[] RedRules);
    }
}
