namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Studio bus paths used by the game's AudioManagerProxy (FMOD Studio). Use with <see cref="FmodStudioServer" /> for
    ///     direct bus access.
    /// </summary>
    public static class FmodStudioRouting
    {
        /// <summary>
        ///     Root master bus path.
        /// </summary>
        public const string MasterBus = "bus:/master";

        /// <summary>
        ///     Game SFX bus under master.
        /// </summary>
        public const string SfxBus = "bus:/master/sfx";

        /// <summary>
        ///     Ambience bus under master.
        /// </summary>
        public const string AmbienceBus = "bus:/master/ambience";

        /// <summary>
        ///     Music / BGM bus under master.
        /// </summary>
        public const string MusicBus = "bus:/master/music";
    }
}
