using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     <para xml:lang="en">Calculates effective maximum hand sizes.</para>
    ///     <para xml:lang="zh-CN">计算实际手牌上限。</para>
    /// </summary>
    public static class MaxHandSizeCalculator
    {
        private const int DefaultMaxHandSize = 10;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Calculates the effective maximum hand size for <paramref name="player" />. Uses BaseLib's value as
        ///         the base when available, then applies RitsuLib hook-listener modifiers once.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         计算 <paramref name="player" /> 的实际手牌上限。BaseLib 的值可用时以其为基础，然后应用一次
        ///         RitsuLib 钩子监听器修正。
        ///     </para>
        /// </summary>
        public static int Calculate(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return BaseLibMaxHandSizeBridge.TryGetMaxHandSizeFromBaseLib(player, out var amount)
                ? ApplyHookListenerModifiers(player, amount)
                : ApplyHookListenerModifiers(player, DefaultMaxHandSize);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies combat hook-listener modifiers to an existing base hand size.</para>
        ///     <para xml:lang="zh-CN">在现有基础手牌上限上应用战斗钩子监听器修正。</para>
        /// </summary>
        public static int ApplyHookListenerModifiers(Player player, int currentMaxHandSize)
        {
            ArgumentNullException.ThrowIfNull(player);

            if (player.Creature?.CombatState is not { } combatState)
                return Math.Max(0, currentMaxHandSize);

            IMaxHandSizeModifier? singleModifier = null;
            List<IMaxHandSizeModifier>? modifiers = null;
            foreach (var entry in ModelHookListenerDispatcher.FromCombat<IMaxHandSizeModifier>(combatState))
            {
                if (singleModifier == null)
                {
                    singleModifier = entry.Listener;
                    continue;
                }

                modifiers ??= [singleModifier];
                modifiers.Add(entry.Listener);
            }

            if (singleModifier == null)
                return Math.Max(0, currentMaxHandSize);

            var amount = currentMaxHandSize;
            if (modifiers == null)
            {
                amount = singleModifier.ModifyMaxHandSize(player, amount);
                amount = singleModifier.ModifyMaxHandSizeLate(player, amount);
                return Math.Max(0, amount);
            }

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var modifier in modifiers)
                amount = modifier.ModifyMaxHandSize(player, amount);

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var modifier in modifiers)
                amount = modifier.ModifyMaxHandSizeLate(player, amount);

            return Math.Max(0, amount);
        }

        internal static int CalculateFromCardOwner(CardModel? card)
        {
            return card?.Owner is { } player ? Calculate(player) : DefaultMaxHandSize;
        }
    }
}
