using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Handle for a shared per-run saved data slot.
    ///     共享跑局保存数据槽位的句柄。
    /// </summary>
    public sealed class RunSavedData<T> where T : class, new()
    {
        private readonly RunSavedDataRunSlot<T> _slot;

        internal RunSavedData(RunSavedDataRunSlot<T> slot)
        {
            _slot = slot;
            Lobby = new(slot);
        }

        /// <summary>
        ///     Lobby staging accessor for this slot before the run snapshot is committed.
        ///     跑局快照提交前，此槽位的大厅暂存访问器。
        /// </summary>
        public RunSavedDataLobbyScope<T> Lobby { get; }

        /// <summary>
        ///     Gets the current value, creating it from the default factory if necessary.
        ///     获取当前值；必要时通过默认工厂创建。
        /// </summary>
        public T Get(RunState runState)
        {
            return _slot.GetOrCreate(runState);
        }

        /// <summary>
        ///     Attempts to get an existing value without creating one.
        ///     尝试获取已有值，但不创建新值。
        /// </summary>
        public bool TryGet(RunState runState, out T value)
        {
            return _slot.TryGet(runState, out value);
        }

        /// <summary>
        ///     Sets the value for the run.
        ///     设置跑局值。
        /// </summary>
        public void Set(RunState runState, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _slot.Set(runState, value);
        }

        /// <summary>
        ///     Removes the value from the run.
        ///     从跑局移除值。
        /// </summary>
        public bool Remove(RunState runState)
        {
            return _slot.Remove(runState);
        }

        /// <summary>
        ///     Mutates the value and marks it dirty.
        ///     修改值并标记为已变更。
        /// </summary>
        public T Modify(RunState runState, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = _slot.GetOrCreate(runState);
            mutate(value);
            _slot.Set(runState, value);
            return value;
        }
    }

    /// <summary>
    ///     Handle for per-player run saved data.
    ///     按玩家分桶的跑局保存数据句柄。
    /// </summary>
    public sealed class PlayerRunSavedData<T> where T : class, new()
    {
        private readonly RunSavedDataPlayerSlot<T> _slot;

        internal PlayerRunSavedData(RunSavedDataPlayerSlot<T> slot, Func<T>? defaultFactory)
        {
            _slot = slot;
            Lobby = new(slot, defaultFactory);
        }

        /// <summary>
        ///     Lobby staging accessor for this slot before the run snapshot is committed.
        ///     跑局快照提交前，此槽位的大厅暂存访问器。
        /// </summary>
        public PlayerRunSavedDataLobbyScope<T> Lobby { get; }

        /// <summary>
        ///     Gets a player's value, creating it if necessary.
        ///     获取玩家值；必要时创建。
        /// </summary>
        public T Get(RunState runState, ulong netId)
        {
            return _slot.GetOrCreate(runState, netId);
        }

        /// <summary>
        ///     Gets a player's value, creating it if necessary.
        ///     获取玩家值；必要时创建。
        /// </summary>
        public T Get(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return Get(GetRunState(player), player.NetId);
        }

        /// <summary>
        ///     Attempts to get a player's existing value without creating one.
        ///     尝试获取玩家已有值，但不创建新值。
        /// </summary>
        public bool TryGet(RunState runState, ulong netId, out T value)
        {
            return _slot.TryGet(runState, netId, out value);
        }

        /// <summary>
        ///     Sets a player's value.
        ///     设置玩家值。
        /// </summary>
        public void Set(RunState runState, ulong netId, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _slot.Set(runState, netId, value);
        }

        /// <summary>
        ///     Removes a player's value.
        ///     移除玩家值。
        /// </summary>
        public bool Remove(RunState runState, ulong netId)
        {
            return _slot.Remove(runState, netId);
        }

        /// <summary>
        ///     Mutates a player's value and marks it dirty.
        ///     修改玩家值并标记为已变更。
        /// </summary>
        public T Modify(RunState runState, ulong netId, Action<T> mutate)
        {
            return _slot.Modify(runState, netId, mutate);
        }

        /// <summary>
        ///     Mutates a player's value and marks it dirty.
        ///     修改玩家值并标记为已变更。
        /// </summary>
        public T Modify(Player player, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(player);
            return _slot.Modify(player, mutate);
        }

        private static RunState GetRunState(Player player)
        {
            if (player.RunState is RunState runState)
                return runState;

            throw new InvalidOperationException("Player does not belong to a concrete RunState.");
        }
    }
}
