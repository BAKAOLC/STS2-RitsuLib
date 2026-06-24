using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Lobby-scoped staging for run saved data. Values are committed into the run snapshot when a new run begins.
    ///     跑局保存数据的大厅暂存区；新开局时会提交进跑局快照。
    /// </summary>
    public static class RunSavedDataLobby
    {
        /// <summary>
        ///     Publishes <see cref="RunSavedDataLobbyStagingEvent" /> for the current lobby session.
        ///     为当前大厅会话发布 <see cref="RunSavedDataLobbyStagingEvent" />。
        /// </summary>
        public static void NotifyStagingChanged(StartRunLobby lobby)
        {
            PublishStagingEvent(lobby, RunSavedDataLobbyStagingReason.Manual);
        }

        /// <summary>
        ///     Pushes the current machine's lobby staging to the host by reusing
        ///     <see cref="LobbyPlayerChangedCharacterMessage" /> (trailer appended on serialize), or merges locally on
        ///     host / singleplayer.
        ///     通过复用 <see cref="LobbyPlayerChangedCharacterMessage" />（序列化时附加尾部）将大厅暂存推送到主机，或在主机 / 单人下本地合并。
        /// </summary>
        public static bool TryPushContribution(StartRunLobby lobby)
        {
            return RunSavedDataLobbySync.TryPushContribution(lobby);
        }

        internal static void PublishStagingEvent(StartRunLobby lobby, RunSavedDataLobbyStagingReason reason)
        {
            if (!RunSavedDataRegistry.HasSlots)
                return;

            var netType = lobby.NetService.Type;
            RitsuLibFramework.PublishLifecycleEvent(
                new RunSavedDataLobbyStagingEvent(
                    lobby,
                    netType.IsMultiplayer(),
                    netType == NetGameType.Host,
                    reason,
                    DateTimeOffset.UtcNow),
                nameof(RunSavedDataLobbyStagingEvent));
        }

        internal static void CommitSession(StartRunLobby lobby, RunState runState)
        {
            if (!RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session))
                return;

            foreach (var slot in RunSavedDataRegistry.GetRegisteredSlots())
                slot.CommitLobbyStaging(session, runState);

            RunSavedDataLobbyRuntime.RemoveSession(lobby);
        }
    }

    /// <summary>
    ///     Lobby staging accessor for a shared per-run slot.
    ///     共享跑局槽位的大厅暂存访问器。
    /// </summary>
    public sealed class RunSavedDataLobbyScope<T> where T : class, new()
    {
        private readonly RunSavedDataRunSlot<T> _slot;

        internal RunSavedDataLobbyScope(RunSavedDataRunSlot<T> slot)
        {
            _slot = slot;
        }

        private void MaybeSync(StartRunLobby lobby)
        {
            if (_slot.Options.SyncLobbyOnChange)
                RunSavedDataLobby.TryPushContribution(lobby);
        }

        /// <summary>
        ///     Gets the staged value, creating it if necessary.
        ///     获取暂存值；必要时创建。
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby)
        {
            var session = RunSavedDataLobbyRuntime.GetSession(lobby);
            if (session.TryGetRun(_slot.SlotKey, out var raw) && raw is T typed)
                return typed;

            var created = new T();
            session.SetRun(_slot.SlotKey, created);
            return created;
        }

        /// <summary>
        ///     Attempts to get an existing staged value without creating one.
        ///     尝试获取已有暂存值，但不创建新值。
        /// </summary>
        public bool TryGet(StartRunLobby lobby, out T value)
        {
            if (RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session) &&
                session.TryGetRun(_slot.SlotKey, out var raw) &&
                raw is T typed)
            {
                value = typed;
                return true;
            }

            value = null!;
            return false;
        }

        /// <summary>
        ///     Sets the staged value.
        ///     设置暂存值。
        /// </summary>
        public void Set(StartRunLobby lobby, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            RunSavedDataLobbyRuntime.GetSession(lobby).SetRun(_slot.SlotKey, value);
            MaybeSync(lobby);
        }

        /// <summary>
        ///     Removes the staged value.
        ///     移除暂存值。
        /// </summary>
        public bool Remove(StartRunLobby lobby)
        {
            var removed = RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session) &&
                          session.RemoveRun(_slot.SlotKey);
            if (removed)
                MaybeSync(lobby);
            return removed;
        }

        /// <summary>
        ///     Mutates the staged value.
        ///     修改暂存值。
        /// </summary>
        public T Modify(StartRunLobby lobby, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = GetOrCreate(lobby);
            mutate(value);
            Set(lobby, value);
            return value;
        }
    }

    /// <summary>
    ///     Lobby staging accessor for a per-player slot.
    ///     按玩家槽位的大厅暂存访问器。
    /// </summary>
    public sealed class PlayerRunSavedDataLobbyScope<T> where T : class, new()
    {
        private readonly Func<T> _defaultFactory;
        private readonly RunSavedDataPlayerSlot<T> _slot;

        internal PlayerRunSavedDataLobbyScope(RunSavedDataPlayerSlot<T> slot, Func<T>? defaultFactory)
        {
            _slot = slot;
            _defaultFactory = defaultFactory ?? (() => new());
        }

        private void MaybeSync(StartRunLobby lobby)
        {
            if (_slot.Options.SyncLobbyOnChange)
                RunSavedDataLobby.TryPushContribution(lobby);
        }

        /// <summary>
        ///     Gets a player's staged value, creating it if necessary.
        ///     获取玩家暂存值；必要时创建。
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby, ulong netId)
        {
            var session = RunSavedDataLobbyRuntime.GetSession(lobby);
            if (session.TryGetPlayer(_slot.SlotKey, netId, out var raw) && raw is T typed)
                return typed;

            var created = _defaultFactory();
            session.SetPlayer(_slot.SlotKey, netId, created);
            return created;
        }

        /// <summary>
        ///     Gets a player's staged value, creating it if necessary.
        ///     获取玩家暂存值；必要时创建。
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby, Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return GetOrCreate(lobby, player.NetId);
        }

        /// <summary>
        ///     Attempts to get a player's staged value without creating one.
        ///     尝试获取玩家暂存值，但不创建新值。
        /// </summary>
        public bool TryGet(StartRunLobby lobby, ulong netId, out T value)
        {
            if (RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session) &&
                session.TryGetPlayer(_slot.SlotKey, netId, out var raw) &&
                raw is T typed)
            {
                value = typed;
                return true;
            }

            value = null!;
            return false;
        }

        /// <summary>
        ///     Sets a player's staged value.
        ///     设置玩家暂存值。
        /// </summary>
        public void Set(StartRunLobby lobby, ulong netId, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            RunSavedDataLobbyRuntime.GetSession(lobby).SetPlayer(_slot.SlotKey, netId, value);
            MaybeSync(lobby);
        }

        /// <summary>
        ///     Sets a player's staged value.
        ///     设置玩家暂存值。
        /// </summary>
        public void Set(StartRunLobby lobby, Player player, T value)
        {
            ArgumentNullException.ThrowIfNull(player);
            Set(lobby, player.NetId, value);
        }

        /// <summary>
        ///     Removes a player's staged value.
        ///     移除玩家暂存值。
        /// </summary>
        public bool Remove(StartRunLobby lobby, ulong netId)
        {
            var removed = RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session) &&
                          session.RemovePlayer(_slot.SlotKey, netId);
            if (removed)
                MaybeSync(lobby);
            return removed;
        }

        /// <summary>
        ///     Mutates a player's staged value.
        ///     修改玩家暂存值。
        /// </summary>
        public T Modify(StartRunLobby lobby, ulong netId, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = GetOrCreate(lobby, netId);
            mutate(value);
            Set(lobby, netId, value);
            return value;
        }

        /// <summary>
        ///     Mutates a player's staged value.
        ///     修改玩家暂存值。
        /// </summary>
        public T Modify(StartRunLobby lobby, Player player, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(player);
            return Modify(lobby, player.NetId, mutate);
        }
    }
}
