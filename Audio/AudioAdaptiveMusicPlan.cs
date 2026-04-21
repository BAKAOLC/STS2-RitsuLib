namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Declares room/combat/victory music sources that should follow the game's lifecycle transitions.
    /// </summary>
    public sealed class AudioAdaptiveMusicPlan
    {
        /// <summary>
        ///     Music source to use while the player is in a room outside combat.
        /// </summary>
        public AudioSource? RoomSource { get; init; }

        /// <summary>
        ///     Music source to use while combat is active.
        /// </summary>
        public AudioSource? CombatSource { get; init; }

        /// <summary>
        ///     Music source to use after combat victory, when provided.
        /// </summary>
        public AudioSource? VictorySource { get; init; }

        /// <summary>
        ///     Restores vanilla run music when the adaptive handle is stopped.
        /// </summary>
        public bool RestoreVanillaMusicOnStop { get; init; } = true;

        /// <summary>
        ///     Restores vanilla run music after combat ends instead of returning to the room override.
        /// </summary>
        public bool RestoreVanillaMusicOnCombatEnd { get; init; } = true;

        /// <summary>
        ///     Refreshes vanilla room track and ambience when entering a room with no room override.
        /// </summary>
        public bool RefreshVanillaRoomStateOnRoomEnter { get; init; } = true;

        /// <summary>
        ///     Playback options applied when starting room music.
        /// </summary>
        public AudioPlaybackOptions RoomOptions { get; init; } = new() { Scope = AudioLifecycleScope.Room };

        /// <summary>
        ///     Playback options applied when starting combat music.
        /// </summary>
        public AudioPlaybackOptions CombatOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };

        /// <summary>
        ///     Playback options applied when starting victory music.
        /// </summary>
        public AudioPlaybackOptions VictoryOptions { get; init; } = new() { Scope = AudioLifecycleScope.Combat };
    }
}
