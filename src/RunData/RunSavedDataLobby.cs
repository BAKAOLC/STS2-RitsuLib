using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates lobby-scoped run saved-data staging and commits staged values when a new run begins.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         协调大厅范围内的局内保存数据暂存，并在新一局游戏开始时提交暂存值。
    ///     </para>
    /// </summary>
    public static class RunSavedDataLobby
    {
        /// <summary>
        ///     <para xml:lang="en">Publishes a staging-change event for the current lobby session.</para>
        ///     <para xml:lang="zh-CN">为当前大厅会话发布暂存数据变更事件。</para>
        /// </summary>
        public static void NotifyStagingChanged(StartRunLobby lobby)
        {
            PublishStagingEvent(lobby, RunSavedDataLobbyStagingReason.Manual);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Pushes local staging to the host in a trailer on <see cref="LobbyPlayerChangedCharacterMessage" />, or
        ///         merges it locally when running as the host or in single-player.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="LobbyPlayerChangedCharacterMessage" /> 的尾部数据将本地暂存内容推送到主机；作为主机或进行单人游戏时则在本地合并。
        ///     </para>
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

        internal static void RemovePlayer(StartRunLobby lobby, ulong netId)
        {
            if (!RunSavedDataLobbyRuntime.TryGetSession(lobby, out var session) || !session.RemovePlayer(netId))
                return;

            PublishStagingEvent(lobby, RunSavedDataLobbyStagingReason.PlayerLeft);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides lobby staging access to a slot shared by the whole run.</para>
    ///     <para xml:lang="zh-CN">提供对整局游戏共享槽位的大厅暂存访问。</para>
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
        ///     <para xml:lang="en">Gets the staged value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取暂存值；若不存在，则创建默认值。</para>
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby)
        {
            var session = RunSavedDataLobbyRuntime.GetSession(lobby);
            if (session.TryGetRun(_slot.SlotKey, out var raw) && raw is T typed)
                return typed;

            var created = _slot.CreateDefaultValue();
            session.SetRun(_slot.SlotKey, created);
            return created;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get an existing staged value without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取现有暂存值，而不创建新值。</para>
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
        ///     <para xml:lang="en">Sets the staged value.</para>
        ///     <para xml:lang="zh-CN">设置暂存值。</para>
        /// </summary>
        public void Set(StartRunLobby lobby, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            RunSavedDataLobbyRuntime.GetSession(lobby).SetRun(_slot.SlotKey, value);
            MaybeSync(lobby);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the staged value.</para>
        ///     <para xml:lang="zh-CN">移除暂存值。</para>
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
        ///     <para xml:lang="en">Mutates and stores the staged value.</para>
        ///     <para xml:lang="zh-CN">修改并存储暂存值。</para>
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
    ///     <para xml:lang="en">Provides lobby staging access to a slot stored separately for each player.</para>
    ///     <para xml:lang="zh-CN">提供对按玩家分别存储的槽位的大厅暂存访问。</para>
    /// </summary>
    public sealed class PlayerRunSavedDataLobbyScope<T> where T : class, new()
    {
        private readonly RunSavedDataPlayerSlot<T> _slot;

        internal PlayerRunSavedDataLobbyScope(RunSavedDataPlayerSlot<T> slot)
        {
            _slot = slot;
        }

        private void MaybeSync(StartRunLobby lobby)
        {
            if (_slot.Options.SyncLobbyOnChange)
                RunSavedDataLobby.TryPushContribution(lobby);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a player's staged value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取玩家的暂存值；若不存在，则创建默认值。</para>
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby, ulong netId)
        {
            var session = RunSavedDataLobbyRuntime.GetSession(lobby);
            if (session.TryGetPlayer(_slot.SlotKey, netId, out var raw) && raw is T typed)
                return typed;

            var created = _slot.CreatePlayerDefaultValue();
            session.SetPlayer(_slot.SlotKey, netId, created);
            return created;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a player's staged value, creating a default value if none exists.</para>
        ///     <para xml:lang="zh-CN">获取玩家的暂存值；若不存在，则创建默认值。</para>
        /// </summary>
        public T GetOrCreate(StartRunLobby lobby, Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return GetOrCreate(lobby, player.NetId);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get a player's staged value without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取玩家的暂存值，而不创建新值。</para>
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
        ///     <para xml:lang="en">Sets a player's staged value.</para>
        ///     <para xml:lang="zh-CN">设置玩家的暂存值。</para>
        /// </summary>
        public void Set(StartRunLobby lobby, ulong netId, T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            RunSavedDataLobbyRuntime.GetSession(lobby).SetPlayer(_slot.SlotKey, netId, value);
            MaybeSync(lobby);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a player's staged value.</para>
        ///     <para xml:lang="zh-CN">设置玩家的暂存值。</para>
        /// </summary>
        public void Set(StartRunLobby lobby, Player player, T value)
        {
            ArgumentNullException.ThrowIfNull(player);
            Set(lobby, player.NetId, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a player's staged value.</para>
        ///     <para xml:lang="zh-CN">移除玩家的暂存值。</para>
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
        ///     <para xml:lang="en">Mutates and stores a player's staged value.</para>
        ///     <para xml:lang="zh-CN">修改并存储玩家的暂存值。</para>
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
        ///     <para xml:lang="en">Mutates and stores a player's staged value.</para>
        ///     <para xml:lang="zh-CN">修改并存储玩家的暂存值。</para>
        /// </summary>
        public T Modify(StartRunLobby lobby, Player player, Action<T> mutate)
        {
            ArgumentNullException.ThrowIfNull(player);
            return Modify(lobby, player.NetId, mutate);
        }
    }
}
