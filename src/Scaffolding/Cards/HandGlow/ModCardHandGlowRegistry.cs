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
        private static int _sequence;

        private static readonly Func<ModCardHandGlowRules, Func<CardModel, bool>?> GoldSelector =
            static rules => rules.GoldWhenBonusActive;

        private static readonly Func<ModCardHandGlowRules, Func<CardModel, bool>?> RedSelector =
            static rules => rules.RedWhenHandWarning;

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

            if (cardType.IsAbstract || !typeof(CardModel).IsAssignableFrom(cardType))
                throw new ArgumentException(
                    $"Type '{cardType.FullName}' must be a concrete subtype of {typeof(CardModel).FullName}.",
                    nameof(cardType));

            var registered = new RegisteredRules(rules, Interlocked.Increment(ref _sequence));
            RulesByCardType.AddOrUpdate(
                cardType,
                _ => [registered],
                (_, existing) => [.. existing, registered]);
        }

        /// <summary>
        ///     <para xml:lang="en">Clears all rules for tests or hot-reload tooling.</para>
        ///     <para xml:lang="zh-CN">清除所有规则，供测试或热重载工具使用。</para>
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
            LoggedRuleFailures.Clear();
        }

        internal static bool EvaluateRegistryGold(CardModel card)
        {
            return EvaluateChannel(card, GoldSelector, "gold");
        }

        internal static bool EvaluateRegistryRed(CardModel card)
        {
            return EvaluateChannel(card, RedSelector, "red");
        }

        private static bool EvaluateChannel(
            CardModel card,
            Func<ModCardHandGlowRules, Func<CardModel, bool>?> selector,
            string channel)
        {
            for (var t = card.GetType();
                 t != null && typeof(CardModel).IsAssignableFrom(t);
                 t = t.BaseType)
            {
                if (!RulesByCardType.TryGetValue(t, out var registrations))
                    continue;

                foreach (var registration in registrations)
                {
                    var pred = selector(registration.Rules);
                    if (pred == null)
                        continue;

                    try
                    {
                        if (pred(card))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        var warningKey = $"{registration.Sequence}|{channel}";
                        if (LoggedRuleFailures.TryAdd(warningKey, 0))
                            RitsuLibFramework.Logger.Warn(
                                $"[CardHandGlow] {channel} rule {registration.Sequence} for {t.FullName} threw " +
                                $"and was ignored: {ex}");
                    }
                }
            }

            return false;
        }

        private readonly record struct RegisteredRules(ModCardHandGlowRules Rules, int Sequence);
    }
}
