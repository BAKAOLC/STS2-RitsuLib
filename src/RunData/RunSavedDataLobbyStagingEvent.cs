using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Published while a start-run lobby is active so mods can read or write lobby staging data
    ///     before the run snapshot is committed.
    ///     开局大厅仍活跃时发布，供 mod 在跑局快照提交前读写大厅暂存数据。
    /// </summary>
    public sealed record RunSavedDataLobbyStagingEvent(
        StartRunLobby Lobby,
        bool IsMultiplayer,
        bool IsHost,
        RunSavedDataLobbyStagingReason Reason,
        DateTimeOffset OccurredAtUtc) : IFrameworkLifecycleEvent;
}
