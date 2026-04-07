using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     Per–card-type custom outline colors for the in-hand <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />.
    ///     Applied after vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder.UpdateCard" /> via Harmony.
    /// </summary>
    public static class ModCardHandOutlineRegistry
    {
        private static int _sequence;

        private static readonly ConcurrentDictionary<Type, List<RegisteredRule>> RulesByCardType = new();

        /// <summary>
        ///     Registers a rule for <typeparamref name="TCard" />. Throws if <see cref="ModContentRegistry.IsFrozen" />.
        /// </summary>
        public static void Register<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            Register(typeof(TCard), rule);
        }

        /// <summary>
        ///     Registers a rule for <paramref name="cardType" /> (concrete <see cref="CardModel" /> subtype).
        /// </summary>
        public static void Register(Type cardType, ModCardHandOutlineRule rule)
        {
            ArgumentNullException.ThrowIfNull(cardType);
            ArgumentNullException.ThrowIfNull(rule.When);

            if (ModContentRegistry.IsFrozen)
                throw new InvalidOperationException(
                    "Cannot register card hand outline rules after content registration has been frozen. " +
                    "Register from your mod initializer before ModelDb initializes.");

            if (cardType.IsAbstract || !typeof(CardModel).IsAssignableFrom(cardType))
                throw new ArgumentException(
                    $"Type '{cardType.FullName}' must be a concrete subtype of {typeof(CardModel).FullName}.",
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
        ///     Clears all rules (tests / tooling).
        /// </summary>
        public static void ClearForTests()
        {
            RulesByCardType.Clear();
        }

        /// <summary>
        ///     Applies the best matching registered outline for this holder
        /// </summary>
        /// <returns><see langword="true" /> if a rule was applied.</returns>
        public static bool TryRefreshOutlineForHolder(NHandCardHolder? holder)
        {
            if (BaseLibModCardHandOutlineBridge.TryDelegatedRefresh(holder))
                return true;

            if (holder == null || !holder.IsNodeReady() || holder.CardNode?.Model is not { } model)
                return false;

            var rule = EvaluateBest(model);
            if (!rule.HasValue)
                return false;

            ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, rule.Value);
            return true;
        }

        /// <summary>
        ///     Applies outline only when the matching rule uses <see cref="ModCardHandOutlineRule.DynamicColor" />.
        /// </summary>
        public static bool TryRefreshDynamicOutlineForHolder(NHandCardHolder? holder)
        {
            if (holder == null || !holder.IsNodeReady() || holder.CardNode?.Model is not { } model)
                return false;

            var rule = EvaluateBest(model);
            if (!rule.HasValue || rule.Value.DynamicColor == null)
                return false;

            ModCardHandOutlinePatchHelper.ApplyHighlight(holder, model, rule.Value);
            return true;
        }

        internal static ModCardHandOutlineRule? EvaluateBest(CardModel model)
        {
            RegisteredRule? best = null;

            for (var t = model.GetType();
                 t != null && typeof(CardModel).IsAssignableFrom(t);
                 t = t.BaseType)
            {
                if (!RulesByCardType.TryGetValue(t, out var list))
                    continue;

                foreach (var entry in list.Where(entry => entry.Rule.When(model)).Where(entry => best is null
                             || entry.Rule.Priority > best.Value.Rule.Priority
                             || (entry.Rule.Priority == best.Value.Rule.Priority &&
                                 entry.Sequence > best.Value.Sequence)))
                    best = entry;
            }

            return best?.Rule;
        }

        private readonly record struct RegisteredRule(ModCardHandOutlineRule Rule, int Sequence);
    }
}
