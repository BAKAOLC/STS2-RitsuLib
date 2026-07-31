using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">Provides access to a saved-data slot shared by the whole run.</para>
    ///     <para xml:lang="zh-CN">提供对整局游戏共享的保存数据槽位的访问。</para>
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
        ///     <para xml:lang="en">Gets the lobby staging accessor used before the run snapshot is committed.</para>
        ///     <para xml:lang="zh-CN">获取局内快照提交前使用的大厅暂存访问器。</para>
        /// </summary>
        public RunSavedDataLobbyScope<T> Lobby { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the current value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取当前值；若不存在，则创建默认值。</para>
        /// </summary>
        public T Get(RunState runState)
        {
            return _slot.GetOrCreate(runState);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get an existing value without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取现有值，而不创建新值。</para>
        /// </summary>
        public bool TryGet(RunState runState, out T value)
        {
            return _slot.TryGet(runState, out value);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the value for the run.</para>
        ///     <para xml:lang="zh-CN">设置此局的值。</para>
        /// </summary>
        public void Set(RunState runState, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _slot.Set(runState, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the value from the run.</para>
        ///     <para xml:lang="zh-CN">从此局中移除该值。</para>
        /// </summary>
        public bool Remove(RunState runState)
        {
            return _slot.Remove(runState);
        }

        /// <summary>
        ///     <para xml:lang="en">Mutates the value and marks the slot as changed.</para>
        ///     <para xml:lang="zh-CN">修改该值，并将槽位标记为已变更。</para>
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
    ///     <para xml:lang="en">Provides access to run saved data stored separately for each player.</para>
    ///     <para xml:lang="zh-CN">提供对按玩家分别存储的局内保存数据的访问。</para>
    /// </summary>
    public sealed class PlayerRunSavedData<T> where T : class, new()
    {
        private readonly RunSavedDataPlayerSlot<T> _slot;

        internal PlayerRunSavedData(RunSavedDataPlayerSlot<T> slot)
        {
            _slot = slot;
            Lobby = new(slot);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the lobby staging accessor used before the run snapshot is committed.</para>
        ///     <para xml:lang="zh-CN">获取局内快照提交前使用的大厅暂存访问器。</para>
        /// </summary>
        public PlayerRunSavedDataLobbyScope<T> Lobby { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets a player's value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取玩家的值；若不存在，则创建默认值。</para>
        /// </summary>
        public T Get(RunState runState, ulong netId)
        {
            return _slot.GetOrCreate(runState, netId);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a player's value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取玩家的值；若不存在，则创建默认值。</para>
        /// </summary>
        public T Get(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return Get(GetRunState(player), player.NetId);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a player's existing value without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取玩家的现有值，而不创建新值。</para>
        /// </summary>
        public bool TryGet(RunState runState, ulong netId, out T value)
        {
            return _slot.TryGet(runState, netId, out value);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a player's value.</para>
        ///     <para xml:lang="zh-CN">设置玩家的值。</para>
        /// </summary>
        public void Set(RunState runState, ulong netId, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _slot.Set(runState, netId, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a player's value.</para>
        ///     <para xml:lang="zh-CN">移除玩家的值。</para>
        /// </summary>
        public bool Remove(RunState runState, ulong netId)
        {
            return _slot.Remove(runState, netId);
        }

        /// <summary>
        ///     <para xml:lang="en">Mutates a player's value and marks the slot as changed.</para>
        ///     <para xml:lang="zh-CN">修改玩家的值，并将槽位标记为已变更。</para>
        /// </summary>
        public T Modify(RunState runState, ulong netId, Action<T> mutate)
        {
            return _slot.Modify(runState, netId, mutate);
        }

        /// <summary>
        ///     <para xml:lang="en">Mutates a player's value and marks the slot as changed.</para>
        ///     <para xml:lang="zh-CN">修改玩家的值，并将槽位标记为已变更。</para>
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
