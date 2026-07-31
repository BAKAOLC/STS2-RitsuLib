using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Stores lazily created mod card piles by their owning combat or player state.</para>
    ///     <para xml:lang="zh-CN">按所属战斗状态或玩家状态存储延迟创建的模组卡牌牌堆。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="ModCardPileScope.CombatOnly" /> piles are keyed by <see cref="PlayerCombatState" />
    ///         and implicitly disposed with the combat (the <c>AllPiles</c> postfix adds them into the vanilla
    ///         cleanup sweep).
    ///     </para>
    ///     <para xml:lang="en">
    ///         <see cref="ModCardPileScope.RunPersistent" /> piles are keyed by <see cref="Player" /> and
    ///         persist across combats for the lifetime of the player instance. Their contents are serialized
    ///         by <see cref="ModCardPilePersistence" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ModCardPileScope.CombatOnly" /> 牌堆按 <see cref="PlayerCombatState" /> 索引，
    ///         并随战斗状态释放；<c>AllPiles</c> 补丁会将它们纳入原版的清理流程。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ModCardPileScope.RunPersistent" /> 牌堆按 <see cref="Player" /> 索引，并在玩家实例
    ///         生命周期内跨战斗保留。其内容由 <see cref="ModCardPilePersistence" /> 序列化。
    ///     </para>
    /// </remarks>
    internal static class ModCardPileStorage
    {
        private static readonly ConditionalWeakTable<PlayerCombatState, Dictionary<PileType, ModCardPile>>
            CombatPiles = new();

        private static readonly ConditionalWeakTable<Player, Dictionary<PileType, ModCardPile>>
            RunPiles = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or creates the <paramref name="type" /> pile associated with <paramref name="player" />,
        ///         or returns <see langword="null" /> when the type is unregistered or its required state is
        ///         unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或创建与 <paramref name="player" /> 关联的 <paramref name="type" /> 牌堆；类型未注册或
        ///         所需状态不可用时返回 <see langword="null" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns a snapshot of existing mod piles owned by <paramref name="state" /> without creating
        ///         missing piles.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="state" /> 当前已有模组牌堆的快照，不创建缺失的牌堆。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns every registered combat-only pile for <paramref name="state" />, creating missing
        ///         instances.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="state" /> 的所有已注册战斗专用牌堆，并创建缺失的实例。
        ///     </para>
        /// </summary>
        public static IReadOnlyCollection<ModCardPile> GetOrCreateCombatPiles(PlayerCombatState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            var definitions = ModCardPileRegistry.GetCombatDefinitionsSnapshot();
            if (definitions.Length == 0)
                return [];

            var dict = CombatPiles.GetValue(state, static _ => []);
            lock (dict)
            {
                foreach (var definition in definitions)
                    if (!dict.ContainsKey(definition.PileType))
                        dict[definition.PileType] = new(definition);

                return [.. dict.Values];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a snapshot of run-persistent piles owned by <paramref name="player" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="player" /> 拥有的局内持久牌堆快照。</para>
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
