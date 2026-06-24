namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Outcome category for a playback request.
    ///     播放请求的结果类别。
    /// </summary>
    public enum AudioPlayStatus
    {
        /// <summary>
        ///     Playback started successfully.
        ///     播放已成功启动。
        /// </summary>
        Started,

        /// <summary>
        ///     The requested source shape is invalid for the chosen operation.
        ///     请求的源形状对所选操作无效。
        /// </summary>
        InvalidSource,

        /// <summary>
        ///     The FMOD server was unavailable.
        ///     FMOD server 不可用。
        /// </summary>
        MissingServer,

        /// <summary>
        ///     The vanilla audio manager was unavailable.
        ///     原版 audio manager 不可用。
        /// </summary>
        MissingManager,

        /// <summary>
        ///     A controllable playback instance could not be created.
        ///     无法创建可控制的播放实例。
        /// </summary>
        MissingInstance,

        /// <summary>
        ///     Playback was skipped due to cooldown throttling.
        ///     由于 cooldown 节流，已跳过播放。
        /// </summary>
        SkippedCooldown,

        /// <summary>
        ///     Playback failed for another reason.
        ///     播放因其他原因失败。
        /// </summary>
        Failed,

        /// <summary>
        ///     The requested operation is not supported for that source type.
        ///     请求的操作不支持该源类型。
        /// </summary>
        NotSupported,
    }
}
