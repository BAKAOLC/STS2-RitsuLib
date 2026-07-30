#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Attaches secondary-resource events to the game's combat history.</para>
    ///     <para xml:lang="zh-CN">将次级资源事件附加到游戏的战斗历史。</para>
    /// </summary>
    public static class SecondaryResourceHistory
    {
        private static readonly AttachedState<CombatHistory, SecondaryResourceHistoryBag> Bags = new(() => new());

        /// <summary>
        ///     <para xml:lang="en">Returns all attached entries without allocating storage when none exist.</para>
        ///     <para xml:lang="zh-CN">返回所有附加条目；不存在条目时不会创建存储。</para>
        /// </summary>
        public static IReadOnlyList<SecondaryResourceHistoryEntry> Entries(CombatHistory history)
        {
            ArgumentNullException.ThrowIfNull(history);
            return Bags.TryGetValue(history, out var bag) ? bag.Entries : [];
        }

        /// <summary>
        ///     <para xml:lang="en">Returns amount-change entries.</para>
        ///     <para xml:lang="zh-CN">返回数量变化条目。</para>
        /// </summary>
        public static IEnumerable<SecondaryResourceChangedEntry> Changes(CombatHistory history)
        {
            return Entries(history).OfType<SecondaryResourceChangedEntry>();
        }

        /// <summary>
        ///     <para xml:lang="en">Returns payment entries.</para>
        ///     <para xml:lang="zh-CN">返回支付条目。</para>
        /// </summary>
        public static IEnumerable<SecondaryResourceSpentEntry> Spends(CombatHistory history)
        {
            return Entries(history).OfType<SecondaryResourceSpentEntry>();
        }

        /// <summary>
        ///     <para xml:lang="en">Returns reset entries.</para>
        ///     <para xml:lang="zh-CN">返回重置条目。</para>
        /// </summary>
        public static IEnumerable<SecondaryResourceResetEntry> Resets(CombatHistory history)
        {
            return Entries(history).OfType<SecondaryResourceResetEntry>();
        }

        internal static void Changed(CombatStateLike combatState, SecondaryResourceChangeContext context)
        {
            if (!ModSecondaryResourceRegistry.HasAny || context.OldAmount == context.NewAmount)
                return;

            Add(combatState, new SecondaryResourceChangedEntry(combatState, context));
        }

        internal static void Spent(CombatStateLike combatState, SecondaryResourceSpendContext context)
        {
            if (!ModSecondaryResourceRegistry.HasAny || context.Amount <= 0)
                return;

            Add(combatState, new SecondaryResourceSpentEntry(combatState, context));
        }

        internal static void Reset(CombatStateLike combatState, SecondaryResourceChangeContext context)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            Add(combatState, new SecondaryResourceResetEntry(combatState, context));
        }

        private static void Add(CombatStateLike combatState, SecondaryResourceHistoryEntry entry)
        {
            var history = CombatManager.Instance?.History;
            if (history == null)
                return;

            Bags.GetOrCreate(history).Add(entry);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides shared metadata for an attached secondary-resource history entry.</para>
    ///     <para xml:lang="zh-CN">提供附加次级资源历史条目的共享元数据。</para>
    /// </summary>
    public abstract class SecondaryResourceHistoryEntry
    {
        private readonly Dictionary<ulong, int> _playerTurnNumbers = [];

        /// <summary>
        ///     <para xml:lang="en">Initializes an attached history entry.</para>
        ///     <para xml:lang="zh-CN">初始化附加历史条目。</para>
        /// </summary>
        protected SecondaryResourceHistoryEntry(
            CombatStateLike combatState,
            Player player,
            SecondaryResourceDefinition definition,
            AbstractModel? source)
        {
            Player = player;
            Definition = definition;
            Source = source;
            RoundNumber = combatState.RoundNumber;
            CurrentSide = combatState.CurrentSide;

#if STS2_AT_LEAST_0_104_0
            foreach (var p in combatState.Players)
                if (p.PlayerCombatState != null)
                    _playerTurnNumbers[p.NetId] = p.PlayerCombatState.TurnNumber;
#endif
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the player whose resource was involved.</para>
        ///     <para xml:lang="zh-CN">获取涉及该资源事件的玩家。</para>
        /// </summary>
        public Player Player { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the resource definition.</para>
        ///     <para xml:lang="zh-CN">获取资源定义。</para>
        /// </summary>
        public SecondaryResourceDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the model that caused the event, if known.</para>
        ///     <para xml:lang="zh-CN">获取引发事件的模型（如已知）。</para>
        /// </summary>
        public AbstractModel? Source { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the combat round number recorded at creation.</para>
        ///     <para xml:lang="zh-CN">获取创建时记录的战斗轮数。</para>
        /// </summary>
        public int RoundNumber { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the active combat side recorded at creation.</para>
        ///     <para xml:lang="zh-CN">获取创建时记录的当前行动方。</para>
        /// </summary>
        public CombatSide CurrentSide { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets a human-readable diagnostic description.</para>
        ///     <para xml:lang="zh-CN">获取供诊断使用的可读说明。</para>
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        ///     <para xml:lang="en">Determines whether the entry occurred during the current player turn.</para>
        ///     <para xml:lang="zh-CN">判断条目是否发生在当前玩家回合。</para>
        /// </summary>
        public bool HappenedThisTurn(CombatStateLike? state)
        {
            if (state == null || RoundNumber != state.RoundNumber || CurrentSide != state.CurrentSide)
                return false;

            foreach (var (playerId, turnNumber) in _playerTurnNumbers)
            {
                var player = state.GetPlayer(playerId);
#if STS2_AT_LEAST_0_104_0
                if (player?.PlayerCombatState?.TurnNumber != turnNumber)
                    return false;
#else
                if (player == null)
                    return false;
#endif
            }

            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether the entry occurred during <paramref name="player" />'s previous turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         判断条目是否发生在 <paramref name="player" /> 的上一回合。
        ///     </para>
        /// </summary>
        public bool HappenedLastPlayerTurn(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
#if STS2_AT_LEAST_0_104_0
            return _playerTurnNumbers.TryGetValue(player.NetId, out var turnNumber) &&
                   player.PlayerCombatState?.TurnNumber - 1 == turnNumber;
#else
            return false;
#endif
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Records a change to a resource's current amount.</para>
    ///     <para xml:lang="zh-CN">记录资源当前数量的变化。</para>
    /// </summary>
    public sealed class SecondaryResourceChangedEntry : SecondaryResourceHistoryEntry
    {
        internal SecondaryResourceChangedEntry(CombatStateLike combatState, SecondaryResourceChangeContext context)
            : base(combatState, context.Player, context.Definition, context.Source)
        {
            OldAmount = context.OldAmount;
            NewAmount = context.NewAmount;
            Delta = context.Delta;
            Reason = context.Reason;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount before the change.</para>
        ///     <para xml:lang="zh-CN">获取变化前的数量。</para>
        /// </summary>
        public int OldAmount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount after the change.</para>
        ///     <para xml:lang="zh-CN">获取变化后的数量。</para>
        /// </summary>
        public int NewAmount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the saturating signed amount delta.</para>
        ///     <para xml:lang="zh-CN">获取饱和带符号数量差值。</para>
        /// </summary>
        public int Delta { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the reason assigned to the change.</para>
        ///     <para xml:lang="zh-CN">获取为本次变化指定的原因。</para>
        /// </summary>
        public SecondaryResourceChangeReason Reason { get; }

        /// <inheritdoc />
        public override string Description =>
            $"{Player.Character.Id.Entry} changed {Definition.Id} by {Delta} ({OldAmount}->{NewAmount})";
    }

    /// <summary>
    ///     <para xml:lang="en">Records resource paid by a card or direct command.</para>
    ///     <para xml:lang="zh-CN">记录由卡牌或直接命令支付的资源。</para>
    /// </summary>
    public sealed class SecondaryResourceSpentEntry : SecondaryResourceHistoryEntry
    {
        internal SecondaryResourceSpentEntry(CombatStateLike combatState, SecondaryResourceSpendContext context)
            : base(combatState, context.Player, context.Definition, context.Source)
        {
            Card = context.Card;
            Amount = context.Amount;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the card associated with the payment, if any.</para>
        ///     <para xml:lang="zh-CN">获取与支付关联的卡牌（如有）。</para>
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount paid.</para>
        ///     <para xml:lang="zh-CN">获取支付的数量。</para>
        /// </summary>
        public int Amount { get; }

        /// <inheritdoc />
        public override string Description =>
            $"{Player.Character.Id.Entry} spent {Amount} {Definition.Id}";
    }

    /// <summary>
    ///     <para xml:lang="en">Records a resource reset performed by a policy or command.</para>
    ///     <para xml:lang="zh-CN">记录由策略或命令执行的资源重置。</para>
    /// </summary>
    public sealed class SecondaryResourceResetEntry : SecondaryResourceHistoryEntry
    {
        internal SecondaryResourceResetEntry(CombatStateLike combatState, SecondaryResourceChangeContext context)
            : base(combatState, context.Player, context.Definition, context.Source)
        {
            OldAmount = context.OldAmount;
            NewAmount = context.NewAmount;
            Reason = context.Reason;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount before the reset.</para>
        ///     <para xml:lang="zh-CN">获取重置前的数量。</para>
        /// </summary>
        public int OldAmount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount after the reset.</para>
        ///     <para xml:lang="zh-CN">获取重置后的数量。</para>
        /// </summary>
        public int NewAmount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the reason assigned to the reset.</para>
        ///     <para xml:lang="zh-CN">获取为本次重置指定的原因。</para>
        /// </summary>
        public SecondaryResourceChangeReason Reason { get; }

        /// <inheritdoc />
        public override string Description =>
            $"{Player.Character.Id.Entry} reset {Definition.Id} ({OldAmount}->{NewAmount})";
    }

    internal sealed class SecondaryResourceHistoryBag
    {
        private readonly List<SecondaryResourceHistoryEntry> _entries = [];

        public IReadOnlyList<SecondaryResourceHistoryEntry> Entries => _entries;

        public void Add(SecondaryResourceHistoryEntry entry)
        {
            _entries.Add(entry);
        }
    }
}
