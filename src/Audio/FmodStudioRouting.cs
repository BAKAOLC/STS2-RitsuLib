namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Defines the FMOD Studio bus paths used by the game's <c>AudioManagerProxy</c>; use them with <see cref="FmodStudioBusAccess" /> for direct bus operations.</para>
    ///     <para xml:lang="zh-CN">定义游戏 <c>AudioManagerProxy</c> 使用的 FMOD Studio 总线路径；可配合 <see cref="FmodStudioBusAccess" /> 执行直接总线操作。</para>
    /// </summary>
    public static class FmodStudioRouting
    {
        /// <summary>
        ///     <para xml:lang="en">The game's master bus path.</para>
        ///     <para xml:lang="zh-CN">游戏的主总线路径。</para>
        /// </summary>
        public const string MasterBus = "bus:/master";

        /// <summary>
        ///     <para xml:lang="en">The game sound-effects bus below the master bus.</para>
        ///     <para xml:lang="zh-CN">主总线下的游戏音效总线。</para>
        /// </summary>
        public const string SfxBus = "bus:/master/sfx";

        /// <summary>
        ///     <para xml:lang="en">The ambience bus below the master bus.</para>
        ///     <para xml:lang="zh-CN">主总线下的环境音总线。</para>
        /// </summary>
        public const string AmbienceBus = "bus:/master/ambience";

        /// <summary>
        ///     <para xml:lang="en">The music bus below the master bus.</para>
        ///     <para xml:lang="zh-CN">主总线下的音乐总线。</para>
        /// </summary>
        public const string MusicBus = "bus:/master/music";
    }
}
