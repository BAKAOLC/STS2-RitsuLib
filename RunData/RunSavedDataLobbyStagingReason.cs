namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Reason a <see cref="RunSavedDataLobbyStagingEvent" /> was published.
    ///     <see cref="RunSavedDataLobbyStagingEvent" /> 的发布原因。
    /// </summary>
    public enum RunSavedDataLobbyStagingReason
    {
        /// <summary>
        ///     A remote or local player contribution was merged on the host lobby session.
        ///     远程或本地玩家贡献已合并进主机大厅会话。
        /// </summary>
        ContributionMerged,

        /// <summary>
        ///     A player slot was added to the lobby.
        ///     大厅新增了一名玩家。
        /// </summary>
        PlayerJoined,

        /// <summary>
        ///     <see cref="RunSavedDataLobby.NotifyStagingChanged" /> was called explicitly.
        ///     显式调用了 <see cref="RunSavedDataLobby.NotifyStagingChanged" />。
        /// </summary>
        Manual,

        /// <summary>
        ///     The host is about to build the new-run snapshot.
        ///     主机即将构建新开局快照。
        /// </summary>
        Committing,
    }
}
