namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Declares room/combat/victory music sources that should follow the game's lifecycle transitions.
    ///     声明应跟随游戏生命周期转换的房间/战斗/胜利音乐源。
    /// </summary>
    public sealed class AudioAdaptiveMusicPlan
    {
        /// <summary>
        ///     Music source to use while the player is in a room outside combat.
        ///     玩家处于非战斗房间时使用的音乐源。
        /// </summary>
        public AudioSource? RoomSource { get; init; }

        /// <summary>
        ///     Music source to use while combat is active.
        ///     战斗进行时使用的音乐源。
        /// </summary>
        public AudioSource? CombatSource { get; init; }

        /// <summary>
        ///     Music source to use after combat victory, when provided.
        ///     提供后，在战斗胜利后使用的音乐源。
        /// </summary>
        public AudioSource? VictorySource { get; init; }

        /// <summary>
        ///     Restores vanilla run music when the adaptive handle is stopped.
        ///     自适应句柄停止时恢复原版跑局音乐。
        /// </summary>
        public bool RestoreVanillaMusicOnStop { get; init; } = true;

        /// <summary>
        ///     Restores vanilla run music after combat ends instead of returning to the room override.
        ///     战斗结束后恢复原版跑局音乐，而不是返回房间覆盖。
        /// </summary>
        public bool RestoreVanillaMusicOnCombatEnd { get; init; } = true;

        /// <summary>
        ///     Refreshes vanilla room track and ambience when entering a room with no room override.
        ///     进入没有房间覆盖的房间时刷新原版房间曲目和环境音。
        /// </summary>
        public bool RefreshVanillaRoomStateOnRoomEnter { get; init; } = true;

        /// <summary>
        ///     Playback options applied when starting room music.
        ///     启动房间音乐时应用的播放选项。
        /// </summary>
        public AudioPlaybackOptions RoomOptions { get; init; } = new() { Scope = AudioLifecycleScope.Room };

        /// <summary>
        ///     Playback options applied when starting combat music.
        ///     启动战斗音乐时应用的播放选项。
        /// </summary>
        public AudioPlaybackOptions CombatOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };

        /// <summary>
        ///     Playback options applied when starting victory music.
        ///     启动胜利音乐时应用的播放选项。
        /// </summary>
        public AudioPlaybackOptions VictoryOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };
    }
}
