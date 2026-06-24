namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     How a named channel should behave when new playback arrives.
    ///     命名通道在新播放到达时应如何处理。
    /// </summary>
    public enum AudioChannelMode
    {
        /// <summary>
        ///     Keep the existing playback and ignore the new request.
        ///     保留现有播放并忽略新请求。
        /// </summary>
        KeepExisting = 0,

        /// <summary>
        ///     Stop the existing playback and replace it with the new one.
        ///     停止现有播放并用新播放替换。
        /// </summary>
        ReplaceExisting = 1,
    }
}
