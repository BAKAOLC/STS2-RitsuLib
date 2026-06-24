namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Studio bus paths used by the game's AudioManagerProxy (FMOD Studio). Use with <see cref="FmodStudioServer" /> for
    ///     direct bus access.
    ///     游戏 AudioManagerProxy（FMOD Studio）使用的 Studio bus 路径。配合 <see cref="FmodStudioServer" /> 进行
    ///     直接 bus 访问。
    /// </summary>
    public static class FmodStudioRouting
    {
        /// <summary>
        ///     Root master bus path.
        ///     根 master bus 路径。
        /// </summary>
        public const string MasterBus = "bus:/master";

        /// <summary>
        ///     Game SFX bus under master.
        ///     master 下的游戏 SFX bus。
        /// </summary>
        public const string SfxBus = "bus:/master/sfx";

        /// <summary>
        ///     Ambience bus under master.
        ///     master 下的环境音 bus。
        /// </summary>
        public const string AmbienceBus = "bus:/master/ambience";

        /// <summary>
        ///     Music / BGM bus under master.
        ///     master 下的音乐/BGM bus。
        /// </summary>
        public const string MusicBus = "bus:/master/music";
    }
}
