namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">Identifies why a <see cref="RunSavedDataLobbyStagingEvent" /> was published.</para>
    ///     <para xml:lang="zh-CN">标识发布 <see cref="RunSavedDataLobbyStagingEvent" /> 的原因。</para>
    /// </summary>
    public enum RunSavedDataLobbyStagingReason
    {
        /// <summary>
        ///     <para xml:lang="en">A local or remote player's contribution was merged into the host's lobby session.</para>
        ///     <para xml:lang="zh-CN">本地或远程玩家的贡献已合并到主机的大厅会话中。</para>
        /// </summary>
        ContributionMerged = 0,

        /// <summary>
        ///     <para xml:lang="en">A player slot was added to the lobby.</para>
        ///     <para xml:lang="zh-CN">大厅中新增了一个玩家槽位。</para>
        /// </summary>
        PlayerJoined = 1,

        /// <summary>
        ///     <para xml:lang="en"><see cref="RunSavedDataLobby.NotifyStagingChanged" /> was called explicitly.</para>
        ///     <para xml:lang="zh-CN">显式调用了 <see cref="RunSavedDataLobby.NotifyStagingChanged" />。</para>
        /// </summary>
        Manual = 2,

        /// <summary>
        ///     <para xml:lang="en">The host is about to construct the new-run snapshot.</para>
        ///     <para xml:lang="zh-CN">主机即将构建新一局游戏快照。</para>
        /// </summary>
        Committing = 3,

        /// <summary>
        ///     <para xml:lang="en">A player slot was removed from the lobby.</para>
        ///     <para xml:lang="zh-CN">大厅中移除了一个玩家槽位。</para>
        /// </summary>
        PlayerLeft = 4,
    }
}
