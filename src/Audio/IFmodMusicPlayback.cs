namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Single active music instance (same as vanilla proxy).
    ///     单个活动音乐实例（与原版 proxy 相同）。
    /// </summary>
    public interface IFmodMusicPlayback
    {
        /// <summary>
        ///     Switches the active music event to <paramref name="eventPath" />.
        ///     将活动音乐事件切换到 <paramref name="eventPath" />。
        /// </summary>
        void PlayMusic(string eventPath);

        /// <summary>
        ///     Stops the current music instance.
        ///     停止当前音乐实例。
        /// </summary>
        void StopMusic();

        /// <summary>
        ///     Updates a global music parameter using a label value.
        ///     使用 label 值更新全局音乐参数。
        /// </summary>
        void UpdateMusicParameter(string parameterName, string labelValue);
    }
}
