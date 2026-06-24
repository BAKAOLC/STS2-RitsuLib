using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Mutable per-player secondary-resource amounts for a combat.
    ///     每名玩家在一场战斗中的可变次级资源数量。
    /// </summary>
    public sealed class SecondaryResourceState
    {
        private readonly Dictionary<string, int> _amounts = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Returns true when this state has a material value.
        ///     当该状态中存在实际存储值时返回 true。
        /// </summary>
        public bool HasValues => _amounts.Count > 0;

        /// <summary>
        ///     Raised after a resource amount changes.
        ///     在资源数量变化后触发。
        /// </summary>
        public event Action<SecondaryResourceChangedEvent>? Changed;

        /// <summary>
        ///     Reads the current amount without creating state for unknown resources.
        ///     读取当前数量；不会为未知资源创建状态。
        /// </summary>
        public int Get(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (_amounts.TryGetValue(resourceId.Trim(), out var amount))
                return amount;

            return !ModSecondaryResourceRegistry.TryGet(resourceId, out var definition)
                ? 0
                : Clamp(definition, definition.DefaultAmount);
        }

        /// <summary>
        ///     Returns a deterministic snapshot of stored amounts.
        ///     返回已存储数量的确定性快照。
        /// </summary>
        public IReadOnlyDictionary<string, int> Snapshot()
        {
            return _amounts
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        internal int Set(
            Player player,
            SecondaryResourceDefinition definition,
            int amount,
            SecondaryResourceChangeReason reason,
            AbstractModel? source,
            bool emit = true)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(definition);

            var oldAmount = Get(definition.Id);
            var newAmount = Clamp(definition, amount);
            if (oldAmount == newAmount && _amounts.ContainsKey(definition.Id))
                return newAmount;

            _amounts[definition.Id] = newAmount;
            if (emit)
                Changed?.Invoke(new(player, definition, oldAmount, newAmount, reason, source));

            return newAmount;
        }

        internal bool Remove(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            return _amounts.Remove(resourceId.Trim());
        }

        private static int Clamp(SecondaryResourceDefinition definition, int amount)
        {
            return Math.Clamp(amount, definition.MinAmount, definition.HardMaxAmount);
        }
    }

    /// <summary>
    ///     Secondary-resource amount change notification.
    ///     次级资源数量变化通知。
    /// </summary>
    public sealed record SecondaryResourceChangedEvent(
        Player Player,
        SecondaryResourceDefinition Definition,
        int OldAmount,
        int NewAmount,
        SecondaryResourceChangeReason Reason,
        AbstractModel? Source)
    {
        /// <summary>
        ///     Signed delta from old to new amount.
        ///     从旧数量到新数量的带符号差值。
        /// </summary>
        public int Delta => NewAmount - OldAmount;
    }

    /// <summary>
    ///     Storage helper for per-player secondary-resource combat state.
    ///     按玩家存储次级资源战斗状态的辅助工具。
    /// </summary>
    public static class SecondaryResourceStateStore
    {
        private static readonly AttachedState<PlayerCombatState, SecondaryResourceState> States = new(() => new());

        /// <summary>
        ///     Gets state for <paramref name="player" />, creating it only when resources are registered.
        ///     获取 <paramref name="player" /> 的状态；仅在已有资源注册时创建。
        /// </summary>
        public static SecondaryResourceState Get(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            return !ModSecondaryResourceRegistry.HasAny
                ? throw new InvalidOperationException("No secondary resources are registered.")
                : States.GetOrCreate(GetPlayerCombatState(player));
        }

        /// <summary>
        ///     Attempts to get existing state without creating it.
        ///     尝试获取已有状态，不会创建新状态。
        /// </summary>
        public static bool TryGet(Player player, out SecondaryResourceState state)
        {
            ArgumentNullException.ThrowIfNull(player);
            state = null!;

            return ModSecondaryResourceRegistry.HasAny &&
                   player.PlayerCombatState != null &&
                   States.TryGetValue(player.PlayerCombatState, out state!);
        }

        /// <summary>
        ///     Reads a current amount without creating state.
        ///     读取当前数量，不会创建状态。
        /// </summary>
        public static int GetAmount(Player player, string resourceId)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (!ModSecondaryResourceRegistry.HasAny)
                return 0;

            if (TryGet(player, out var state))
                return state.Get(resourceId);

            return ModSecondaryResourceRegistry.TryGet(resourceId, out var definition)
                ? Math.Clamp(definition.DefaultAmount, definition.MinAmount, definition.HardMaxAmount)
                : 0;
        }

        /// <summary>
        ///     Calculates the current max amount for a registered resource.
        ///     计算已注册资源的当前最大数量。
        /// </summary>
        public static int? GetMaxAmount(Player player, string resourceId)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition) ||
                definition.BaseMaxAmount == null)
                return null;

            var combatState = player.Creature?.CombatState;
            if (combatState == null)
                return Math.Clamp(definition.BaseMaxAmount.Value, definition.MinAmount, definition.HardMaxAmount);

            var context = new SecondaryResourceMaxContext(combatState, player, definition);
            var modified = SecondaryResourceHook.ModifyMaxAmount(context, definition.BaseMaxAmount.Value);
            return Math.Clamp((int)Math.Floor(modified), definition.MinAmount, definition.HardMaxAmount);
        }

        internal static void SetFromPersistence(Player player, string resourceId, int amount)
        {
            if (!ModSecondaryResourceRegistry.TryGet(resourceId, out var definition))
                return;

            Get(player).Set(player, definition, amount, SecondaryResourceChangeReason.Set, null, false);
        }

        private static PlayerCombatState GetPlayerCombatState(Player player)
        {
            return player.PlayerCombatState ??
                   throw new InvalidOperationException("Player does not have a combat state.");
        }
    }
}
