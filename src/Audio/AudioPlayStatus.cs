namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Identifies the outcome category of a high-level audio playback request.</para>
    ///     <para xml:lang="zh-CN">标识高级音频播放请求的结果类别。</para>
    /// </summary>
    public enum AudioPlayStatus
    {
        /// <summary>
        ///     <para xml:lang="en">Playback started successfully.</para>
        ///     <para xml:lang="zh-CN">播放已成功启动。</para>
        /// </summary>
        Started,

        /// <summary>
        ///     <para xml:lang="en">The supplied source type is invalid for the requested operation.</para>
        ///     <para xml:lang="zh-CN">提供的音频源类型不适用于所请求的操作。</para>
        /// </summary>
        InvalidSource,

        /// <summary>
        ///     <para xml:lang="en">The FMOD server was unavailable.</para>
        ///     <para xml:lang="zh-CN">FMOD 服务器不可用。</para>
        /// </summary>
        MissingServer,

        /// <summary>
        ///     <para xml:lang="en">The game's native audio manager was unavailable.</para>
        ///     <para xml:lang="zh-CN">游戏原生音频管理器不可用。</para>
        /// </summary>
        MissingManager,

        /// <summary>
        ///     <para xml:lang="en">A controllable backend playback instance could not be created.</para>
        ///     <para xml:lang="zh-CN">无法创建可控制的后端播放实例。</para>
        /// </summary>
        MissingInstance,

        /// <summary>
        ///     <para xml:lang="en">Playback was skipped because its cooldown group is still throttled.</para>
        ///     <para xml:lang="zh-CN">播放所属冷却分组仍处于节流期，因此已跳过播放。</para>
        /// </summary>
        SkippedCooldown,

        /// <summary>
        ///     <para xml:lang="en">Playback failed for another reason.</para>
        ///     <para xml:lang="zh-CN">播放因其他原因失败。</para>
        /// </summary>
        Failed,

        /// <summary>
        ///     <para xml:lang="en">The requested operation does not support the supplied source type.</para>
        ///     <para xml:lang="zh-CN">所请求的操作不支持提供的音频源类型。</para>
        /// </summary>
        NotSupported,
    }
}
