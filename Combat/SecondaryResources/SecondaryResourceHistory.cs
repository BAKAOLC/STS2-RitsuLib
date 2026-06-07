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
    ///     Sidecar combat history for secondary-resource events.
    ///     次级资源事件的 sidecar 战斗历史。
    /// </summary>
    public static class SecondaryResourceHistory
    {
        private static readonly AttachedState<CombatHistory, SecondaryResourceHistoryBag> Bags = new(() => new());

        /// <summary>
        ///     Returns all sidecar entries for this combat history without creating a bag.
        ///     返回该战斗历史的所有 sidecar 条目，不创建新 bag。
        /// </summary>
        public static IReadOnlyList<SecondaryResourceHistoryEntry> Entries(CombatHistory history)
        {
            ArgumentNullException.ThrowIfNull(history);
            return Bags.TryGetValue(history, out var bag) ? bag.Entries : [];
        }

        /// <summary>
        ///     Returns change entries.
        ///     返回变化条目。
        /// </summary>
        public static IEnumerable<SecondaryResourceChangedEntry> Changes(CombatHistory history)
        {
            return Entries(history).OfType<SecondaryResourceChangedEntry>();
        }

        /// <summary>
        ///     Returns spend entries.
        ///     返回消耗条目。
        /// </summary>
        public static IEnumerable<SecondaryResourceSpentEntry> Spends(CombatHistory history)
        {
            return Entries(history).OfType<SecondaryResourceSpentEntry>();
        }

        /// <summary>
        ///     Returns reset entries.
        ///     返回重置条目。
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
    ///     Base sidecar history entry for secondary-resource events.
    ///     次级资源事件的 sidecar 历史条目基类。
    /// </summary>
    public abstract class SecondaryResourceHistoryEntry
    {
        private readonly Dictionary<ulong, int> _playerTurnNumbers = [];

        /// <summary>
        ///     Creates a sidecar history entry.
        ///     创建一个 sidecar 历史条目。
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
        ///     Player whose resource changed.
        ///     资源发生变化的玩家。
        /// </summary>
        public Player Player { get; }

        /// <summary>
        ///     Resource definition.
        ///     资源定义。
        /// </summary>
        public SecondaryResourceDefinition Definition { get; }

        /// <summary>
        ///     Optional model source.
        ///     可选的模型来源。
        /// </summary>
        public AbstractModel? Source { get; }

        /// <summary>
        ///     Combat round number at entry creation.
        ///     条目创建时的战斗轮数。
        /// </summary>
        public int RoundNumber { get; }

        /// <summary>
        ///     Combat side at entry creation.
        ///     条目创建时的战斗方。
        /// </summary>
        public CombatSide CurrentSide { get; }

        /// <summary>
        ///     Human-readable diagnostic text.
        ///     供诊断使用的人类可读文本。
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        ///     Returns true when the entry happened during the current player turn.
        ///     当该条目发生在当前玩家回合内时返回 true。
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
        ///     Returns true when the entry happened during the previous turn for <paramref name="player" />.
        ///     当该条目发生在 <paramref name="player" /> 的上一回合时返回 true。
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
    ///     Current amount changed.
    ///     当前数量发生变化。
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
        ///     Amount before the change.
        ///     变化前的数量。
        /// </summary>
        public int OldAmount { get; }

        /// <summary>
        ///     Amount after the change.
        ///     变化后的数量。
        /// </summary>
        public int NewAmount { get; }

        /// <summary>
        ///     Signed amount delta.
        ///     带符号的数量差值。
        /// </summary>
        public int Delta { get; }

        /// <summary>
        ///     Reason supplied by the command.
        ///     命令提供的原因。
        /// </summary>
        public SecondaryResourceChangeReason Reason { get; }

        /// <inheritdoc />
        public override string Description =>
            $"{Player.Character.Id.Entry} changed {Definition.Id} by {Delta} ({OldAmount}->{NewAmount})";
    }

    /// <summary>
    ///     Resource amount was spent for a card or command.
    ///     资源数量被卡牌或命令消耗。
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
        ///     Card associated with the spend, if any.
        ///     与该消耗关联的卡牌；如果存在。
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     Amount spent.
        ///     已消耗数量。
        /// </summary>
        public int Amount { get; }

        /// <inheritdoc />
        public override string Description =>
            $"{Player.Character.Id.Entry} spent {Amount} {Definition.Id}";
    }

    /// <summary>
    ///     Resource amount was reset by a built-in policy or command.
    ///     资源数量被内建策略或命令重置。
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
        ///     Amount before the reset.
        ///     重置前的数量。
        /// </summary>
        public int OldAmount { get; }

        /// <summary>
        ///     Amount after the reset.
        ///     重置后的数量。
        /// </summary>
        public int NewAmount { get; }

        /// <summary>
        ///     Reason supplied by the command.
        ///     命令提供的原因。
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
