using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Notifies mods that start-run lobby staging data can be read or changed before it is committed to the run.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通知模组可在开局大厅暂存数据提交到一局游戏前读取或修改这些数据。
    ///     </para>
    /// </summary>
    public sealed record RunSavedDataLobbyStagingEvent(
        StartRunLobby Lobby,
        bool IsMultiplayer,
        bool IsHost,
        RunSavedDataLobbyStagingReason Reason,
        DateTimeOffset OccurredAtUtc) : IFrameworkLifecycleEvent;
}
