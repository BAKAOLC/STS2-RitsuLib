using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Registry and calculator for RitsuLib max-hand-size modifiers.
    /// </summary>
    public static class MaxHandSizeRegistry
    {
        private const int DefaultMaxHandSize = 10;
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, RegisteredModifier> RegisteredModifiers = [];
        private static long _nextOrder;

        /// <summary>
        ///     Registers or replaces a max-hand-size modifier source.
        /// </summary>
        public static void Register(string modId, string sourceId, IMaxHandSizeModifier modifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
            ArgumentNullException.ThrowIfNull(modifier);

            var key = BuildKey(modId, sourceId);
            lock (Gate)
            {
                RegisteredModifiers[key] = new(modId, sourceId, modifier, _nextOrder++);
            }
        }

        /// <summary>
        ///     Registers or replaces a max-hand-size modifier source.
        /// </summary>
        public static void Register<TModifier>(string modId, string? sourceId = null)
            where TModifier : IMaxHandSizeModifier, new()
        {
            Register(modId, sourceId ?? typeof(TModifier).FullName ?? typeof(TModifier).Name, new TModifier());
        }

        /// <summary>
        ///     Calculates the effective max hand size for <paramref name="player" />.
        ///     Uses BaseLib as authoritative source when available, while preserving RitsuLib modifier data via bridge.
        /// </summary>
        public static int GetMaxHandSize(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            return BaseLibMaxHandSizeBridge.TryGetMaxHandSizeFromBaseLib(player, out var amount)
                ? amount
                : ApplyRegisteredModifiers(player, DefaultMaxHandSize);
        }

        /// <summary>
        ///     Applies only RitsuLib-registered modifiers on top of an existing base amount.
        /// </summary>
        public static int ApplyRegisteredModifiers(Player player, int currentMaxHandSize)
        {
            ArgumentNullException.ThrowIfNull(player);

            RegisteredModifier[] snapshot;
            lock (Gate)
            {
                snapshot = RegisteredModifiers.Values
                    .OrderBy(entry => entry.Order)
                    .ToArray();
            }

            var amount = snapshot.Aggregate(currentMaxHandSize,
                (current, entry) => entry.Modifier.ModifyMaxHandSize(player, current));
            amount = snapshot.Aggregate(amount,
                (current, entry) => entry.Modifier.ModifyMaxHandSizeLate(player, current));
            return Math.Max(0, amount);
        }

        internal static int GetMaxHandSizeFromCard(CardModel? card)
        {
            return card?.Owner is { } player ? GetMaxHandSize(player) : DefaultMaxHandSize;
        }

        private static string BuildKey(string modId, string sourceId)
        {
            return $"{modId}::{sourceId}";
        }

        private readonly record struct RegisteredModifier(
            string ModId,
            string SourceId,
            IMaxHandSizeModifier Modifier,
            long Order);
    }
}
