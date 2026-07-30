namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Provides factory methods for common adaptive music-plan patterns.</para>
    ///     <para xml:lang="zh-CN">提供常见自适应音乐方案模式的工厂方法。</para>
    /// </summary>
    public static class AudioAdaptivePlans
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a combat override with optional room and victory sources and caller-supplied playback
        ///         options.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建战斗音乐覆盖，并可指定房间与胜利音乐源以及调用方提供的播放选项。</para>
        /// </summary>
        /// <param name="combatSource">
        ///     <para xml:lang="en">The music source applied when combat starts.</para>
        ///     <para xml:lang="zh-CN">战斗开始时应用的音乐源。</para>
        /// </param>
        /// <param name="roomSource">
        ///     <para xml:lang="en">The optional source applied outside combat; when absent, normal room music is refreshed.</para>
        ///     <para xml:lang="zh-CN">在战斗外应用的可选音乐源；未提供时会刷新正常房间音乐。</para>
        /// </param>
        /// <param name="victorySource">
        ///     <para xml:lang="en">The optional source applied after combat victory.</para>
        ///     <para xml:lang="zh-CN">战斗胜利后应用的可选音乐源。</para>
        /// </param>
        /// <param name="combatOptions">
        ///     <para xml:lang="en">The combat playback options, or null to use combat-scoped defaults.</para>
        ///     <para xml:lang="zh-CN">战斗播放选项；为 <see langword="null" /> 时使用战斗作用域默认值。</para>
        /// </param>
        /// <param name="roomOptions">
        ///     <para xml:lang="en">The room playback options, or null to use room-scoped defaults.</para>
        ///     <para xml:lang="zh-CN">房间播放选项；为 <see langword="null" /> 时使用房间作用域默认值。</para>
        /// </param>
        /// <param name="victoryOptions">
        ///     <para xml:lang="en">The victory playback options, or null to use combat-scoped defaults.</para>
        ///     <para xml:lang="zh-CN">胜利播放选项；为 <see langword="null" /> 时使用战斗作用域默认值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured adaptive music plan.</para>
        ///     <para xml:lang="zh-CN">配置完成的自适应音乐方案。</para>
        /// </returns>
        public static AudioAdaptiveMusicPlan CombatOverride(
            AudioSource combatSource,
            AudioSource? roomSource = null,
            AudioSource? victorySource = null,
            AudioPlaybackOptions? combatOptions = null,
            AudioPlaybackOptions? roomOptions = null,
            AudioPlaybackOptions? victoryOptions = null)
        {
            return new()
            {
                RoomSource = roomSource,
                CombatSource = combatSource,
                VictorySource = victorySource,
                RoomOptions = roomOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Room },
                CombatOptions = combatOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat },
                VictoryOptions = victoryOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat },
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a run-wide room and combat override that returns to its room source after combat.</para>
        ///     <para xml:lang="zh-CN">创建覆盖整场跑局房间与战斗音乐，并在战斗后返回其房间音乐源的方案。</para>
        /// </summary>
        /// <param name="roomSource">
        ///     <para xml:lang="en">The music source applied outside combat.</para>
        ///     <para xml:lang="zh-CN">在战斗外应用的音乐源。</para>
        /// </param>
        /// <param name="combatSource">
        ///     <para xml:lang="en">The music source applied when combat starts.</para>
        ///     <para xml:lang="zh-CN">战斗开始时应用的音乐源。</para>
        /// </param>
        /// <param name="victorySource">
        ///     <para xml:lang="en">The optional source applied after combat victory.</para>
        ///     <para xml:lang="zh-CN">战斗胜利后应用的可选音乐源。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured adaptive music plan.</para>
        ///     <para xml:lang="zh-CN">配置完成的自适应音乐方案。</para>
        /// </returns>
        public static AudioAdaptiveMusicPlan FullRunOverride(
            AudioSource roomSource,
            AudioSource combatSource,
            AudioSource? victorySource = null)
        {
            return new()
            {
                RoomSource = roomSource,
                CombatSource = combatSource,
                VictorySource = victorySource,
                RestoreVanillaMusicOnCombatEnd = false,
                RoomOptions = new() { Scope = AudioLifecycleScope.Room },
                CombatOptions = new() { Scope = AudioLifecycleScope.Combat },
                VictoryOptions = new() { Scope = AudioLifecycleScope.Combat },
            };
        }
    }
}
