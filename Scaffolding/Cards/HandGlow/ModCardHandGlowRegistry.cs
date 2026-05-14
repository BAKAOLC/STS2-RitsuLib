using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     Global registration of per–card-type hand glow rules, merged into <see cref="CardModel.ShouldGlowGold" /> and
    ///     <see cref="CardModel.ShouldGlowRed" /> via framework Harmony patches. Prefer
    ///     <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> so registration respects the same freeze rules as
    ///     other
    ///     content.
    ///     逐卡牌类型手牌发光规则的全局注册，经框架 Harmony patch 合并到 <see cref="CardModel.ShouldGlowGold" /> 和
    ///     <see cref="CardModel.ShouldGlowRed" />。优先使用
    ///     <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" />，让注册遵循与其它
    ///     内容相同的冻结规则。
    /// </summary>
    public static class ModCardHandGlowRegistry
    {
        private static readonly ConcurrentDictionary<Type, ModCardHandGlowRules> RulesByCardType = new();

        /// <summary>
        ///     Registers rules for <typeparamref name="TCard" />. Multiple calls for the same type OR-merge channels.
        ///     Throws if <see cref="ModContentRegistry.IsFrozen" />.
        ///     为 <typeparamref name="TCard" /> 注册规则。对同一类型多次调用会按 OR 合并通道。
        ///     如果 <see cref="ModContentRegistry.IsFrozen" /> 则抛出。
        /// </summary>
        public static void Register<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            Register(typeof(TCard), rules);
        }

        /// <summary>
        ///     Registers rules for <paramref name="cardType" />. Must be a concrete <see cref="CardModel" /> subtype.
        ///     为 <paramref name="cardType" /> 注册规则。必须是具体 <see cref="CardModel" /> 子类型。
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

            RulesByCardType.AddOrUpdate(cardType, rules, (_, existing) => existing.Or(rules));
        }

        /// <summary>
        ///     Clears all rules (intended for tests or hot reload tooling).
        ///     清除所有规则（用于测试或热重载工具）。
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
        }

        internal static bool EvaluateRegistryGold(CardModel card)
        {
            return EvaluateChannel(card, static r => r.GoldWhenBonusActive);
        }

        internal static bool EvaluateRegistryRed(CardModel card)
        {
            return EvaluateChannel(card, static r => r.RedWhenHandWarning);
        }

        private static bool EvaluateChannel(CardModel card, Func<ModCardHandGlowRules, Func<CardModel, bool>?> selector)
        {
            for (var t = card.GetType();
                 t != null && typeof(CardModel).IsAssignableFrom(t);
                 t = t.BaseType)
            {
                if (!RulesByCardType.TryGetValue(t, out var rules))
                    continue;

                var pred = selector(rules);
                if (pred != null && pred(card))
                    return true;
            }

            return false;
        }
    }
}
