using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Calculator for effective max-hand-size values.
    /// </summary>
    public static class MaxHandSizeCalculator
    {
        private const int DefaultMaxHandSize = 10;

        /// <summary>
        ///     Calculates the effective max hand size for <paramref name="player" />.
        ///     Uses BaseLib value as the base amount when available, then applies
        ///     RitsuLib hook-listener modifiers exactly once.
        /// </summary>
        public static int Calculate(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return BaseLibMaxHandSizeBridge.TryGetMaxHandSizeFromBaseLib(player, out var amount)
                ? ApplyHookListenerModifiers(player, amount)
                : ApplyHookListenerModifiers(player, DefaultMaxHandSize);
        }

        /// <summary>
        ///     Applies combat hook-listener modifiers on top of an existing base amount.
        /// </summary>
        public static int ApplyHookListenerModifiers(Player player, int currentMaxHandSize)
        {
            ArgumentNullException.ThrowIfNull(player);

            var amount = currentMaxHandSize;
            var hookModifiers = GetHookListenerModifiers(player);
            amount = hookModifiers.Aggregate(amount,
                (current, modifier) => modifier.ModifyMaxHandSize(player, current));

            amount = hookModifiers.Aggregate(amount,
                (current, modifier) => modifier.ModifyMaxHandSizeLate(player, current));

            return Math.Max(0, amount);
        }

        internal static int CalculateFromCardOwner(CardModel? card)
        {
            return card?.Owner is { } player ? Calculate(player) : DefaultMaxHandSize;
        }

        private static IMaxHandSizeModifier[] GetHookListenerModifiers(Player player)
        {
            if (player.Creature?.CombatState is not { } combatState)
                return [];

            return combatState.IterateHookListeners()
                .OfType<IMaxHandSizeModifier>()
                .ToArray();
        }
    }
}
