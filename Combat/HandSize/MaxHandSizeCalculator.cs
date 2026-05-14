using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Calculator for effective max-hand-size values.
    ///     有效最大手牌数值的计算器。
    /// </summary>
    public static class MaxHandSizeCalculator
    {
        private const int DefaultMaxHandSize = 10;

        private static readonly IEqualityComparer<IMaxHandSizeModifier> ModifierReferenceComparer =
            new MaxHandSizeModifierReferenceComparer();

        /// <summary>
        ///     Calculates the effective max hand size for <paramref name="player" />.
        ///     Uses BaseLib value as the base amount when available, then applies
        ///     RitsuLib hook-listener modifiers exactly once.
        ///     计算 <paramref name="player" /> 的有效最大手牌数。
        ///     可用时使用 BaseLib 值作为基础数值，然后只应用一次
        ///     RitsuLib hook-listener modifier。
        ///     RitsuLib hook-listener modifier。
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
        ///     在现有基础数值上应用战斗 hook-listener modifier。
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
                .Distinct(ModifierReferenceComparer)
                .ToArray();
        }

        private sealed class MaxHandSizeModifierReferenceComparer : IEqualityComparer<IMaxHandSizeModifier>
        {
            public bool Equals(IMaxHandSizeModifier? x, IMaxHandSizeModifier? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IMaxHandSizeModifier obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
