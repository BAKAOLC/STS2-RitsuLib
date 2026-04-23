using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Weak per-state storage for <see cref="ModCardPile" /> instances. Piles are created lazily the first
    ///     time vanilla code asks for them via <see cref="Resolve" /> so state objects pay nothing for mods they
    ///     do not interact with.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ModCardPileScope.CombatOnly" /> piles are keyed by <see cref="PlayerCombatState" />
    ///         and implicitly disposed with the combat (the <c>AllPiles</c> postfix adds them into the vanilla
    ///         cleanup sweep).
    ///     </para>
    ///     <para>
    ///         <see cref="ModCardPileScope.RunPersistent" /> piles are keyed by <see cref="Player" /> and
    ///         persist across combats for the lifetime of the player instance. Serialization is a follow-up;
    ///         for now the piles refill from empty at run load.
    ///     </para>
    /// </remarks>
    internal static class ModCardPileStorage
    {
        private static readonly ConditionalWeakTable<PlayerCombatState, Dictionary<PileType, ModCardPile>>
            CombatPiles = new();

        private static readonly ConditionalWeakTable<Player, Dictionary<PileType, ModCardPile>>
            RunPiles = new();

        /// <summary>
        ///     Looks up or lazily creates the <see cref="ModCardPile" /> bound to <paramref name="player" /> for
        ///     <paramref name="type" />. Returns null when the minted type has no registered definition or when
        ///     the requested state (combat / player) is not yet available.
        /// </summary>
        public static ModCardPile? Resolve(PileType type, Player? player)
        {
            if (player == null)
                return null;
            if (!ModCardPileRegistry.TryGetByPileType(type, out var definition))
                return null;

            return definition.Scope switch
            {
                ModCardPileScope.CombatOnly => ResolveCombatPile(player.PlayerCombatState, definition),
                ModCardPileScope.RunPersistent => ResolveRunPile(player, definition),
                _ => null,
            };
        }

        /// <summary>
        ///     Returns the mod piles that currently belong to <paramref name="state" /> without creating new
        ///     ones. The returned collection is a snapshot and safe to enumerate while vanilla mutates piles.
        /// </summary>
        public static IReadOnlyCollection<ModCardPile> GetCombatPiles(PlayerCombatState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (!CombatPiles.TryGetValue(state, out var piles) || piles.Count == 0)
                return [];

            lock (piles)
            {
                return [.. piles.Values];
            }
        }

        /// <summary>
        ///     Snapshot of persistent piles owned by <paramref name="player" />.
        /// </summary>
        public static IReadOnlyCollection<ModCardPile> GetRunPiles(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            if (!RunPiles.TryGetValue(player, out var piles) || piles.Count == 0)
                return [];

            lock (piles)
            {
                return [.. piles.Values];
            }
        }

        private static ModCardPile? ResolveCombatPile(PlayerCombatState? state, ModCardPileDefinition definition)
        {
            if (state == null)
                return null;

            var dict = CombatPiles.GetValue(state, static _ => []);
            lock (dict)
            {
                if (dict.TryGetValue(definition.PileType, out var existing))
                    return existing;

                var created = new ModCardPile(definition);
                dict[definition.PileType] = created;
                return created;
            }
        }

        private static ModCardPile ResolveRunPile(Player player, ModCardPileDefinition definition)
        {
            var dict = RunPiles.GetValue(player, static _ => []);
            lock (dict)
            {
                if (dict.TryGetValue(definition.PileType, out var existing))
                    return existing;

                var created = new ModCardPile(definition);
                dict[definition.PileType] = created;
                return created;
            }
        }
    }
}
