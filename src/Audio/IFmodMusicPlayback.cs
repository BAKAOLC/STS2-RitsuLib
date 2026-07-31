namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Controls the single music instance owned by the game's native audio proxy.</para>
    ///     <para xml:lang="zh-CN">控制游戏原生音频代理持有的单个音乐实例。</para>
    /// </summary>
    public interface IFmodMusicPlayback
    {
        /// <summary>
        ///     <para xml:lang="en">Starts a valid path-based music event after stopping the current one.</para>
        ///     <para xml:lang="zh-CN">停止当前音乐后，启动有效的路径型音乐事件。</para>
        /// </summary>
        /// <param name="eventPath">
        ///     <para xml:lang="en">The FMOD Studio music-event path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 音乐事件路径。</para>
        /// </param>
        void PlayMusic(string eventPath);

        /// <summary>
        ///     <para xml:lang="en">Stops and releases the current native music instance, if any.</para>
        ///     <para xml:lang="zh-CN">停止并释放当前原生音乐实例（如果存在）。</para>
        /// </summary>
        void StopMusic();

        /// <summary>
        ///     <para xml:lang="en">Sets a labeled parameter on the current native music instance.</para>
        ///     <para xml:lang="zh-CN">为当前原生音乐实例设置标签型参数。</para>
        /// </summary>
        /// <param name="parameterName">
        ///     <para xml:lang="en">The FMOD parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD 参数名称。</para>
        /// </param>
        /// <param name="labelValue">
        ///     <para xml:lang="en">The FMOD parameter label.</para>
        ///     <para xml:lang="zh-CN">FMOD 参数标签。</para>
        /// </param>
        void UpdateMusicParameter(string parameterName, string labelValue);
    }
}
