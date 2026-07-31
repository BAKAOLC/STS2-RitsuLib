namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the room, combat, and victory music states driven by
    ///         <see cref="AudioAdaptiveMusicDirector" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义由 <see cref="AudioAdaptiveMusicDirector" /> 驱动的房间、战斗和胜利音乐状态。</para>
    /// </summary>
    public sealed class AudioAdaptiveMusicPlan
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the music source applied when room state is refreshed outside combat.</para>
        ///     <para xml:lang="zh-CN">获取或初始化在非战斗房间状态刷新时应用的音乐源。</para>
        /// </summary>
        public AudioSource? RoomSource { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the music source applied when combat starts.</para>
        ///     <para xml:lang="zh-CN">获取或初始化战斗开始时应用的音乐源。</para>
        /// </summary>
        public AudioSource? CombatSource { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the optional music source applied when combat victory is reported.</para>
        ///     <para xml:lang="zh-CN">获取或初始化战斗胜利事件发生时应用的可选音乐源。</para>
        /// </summary>
        public AudioSource? VictorySource { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes whether a stop request may refresh the game's normal run music.</para>
        ///     <para xml:lang="zh-CN">获取或初始化停止请求是否可以刷新游戏的正常跑局音乐。</para>
        /// </summary>
        public bool RestoreVanillaMusicOnStop { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether combat end stops the adaptive override instead of returning to
        ///         <see cref="RoomSource" />. Normal run music is refreshed only when
        ///         <see cref="RestoreVanillaMusicOnStop" /> is also enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化战斗结束时是否停止自适应覆盖，而不是返回 <see cref="RoomSource" />。
        ///         仅当 <see cref="RestoreVanillaMusicOnStop" /> 也启用时才会刷新正常跑局音乐。
        ///     </para>
        /// </summary>
        public bool RestoreVanillaMusicOnCombatEnd { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether room-state refreshes update the game's normal track and ambience
        ///         when <see cref="RoomSource" /> is absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化在缺少 <see cref="RoomSource" /> 时，房间状态刷新是否更新游戏的正常曲目和环境音。</para>
        /// </summary>
        public bool RefreshVanillaRoomStateOnRoomEnter { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the playback options used to start <see cref="RoomSource" />.</para>
        ///     <para xml:lang="zh-CN">获取或初始化启动 <see cref="RoomSource" /> 时使用的播放选项。</para>
        /// </summary>
        public AudioPlaybackOptions RoomOptions { get; init; } = new() { Scope = AudioLifecycleScope.Room };

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the playback options used to start <see cref="CombatSource" />.</para>
        ///     <para xml:lang="zh-CN">获取或初始化启动 <see cref="CombatSource" /> 时使用的播放选项。</para>
        /// </summary>
        public AudioPlaybackOptions CombatOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the playback options used to start <see cref="VictorySource" />.</para>
        ///     <para xml:lang="zh-CN">获取或初始化启动 <see cref="VictorySource" /> 时使用的播放选项。</para>
        /// </summary>
        public AudioPlaybackOptions VictoryOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };
    }
}
