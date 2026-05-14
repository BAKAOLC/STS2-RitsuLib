namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Structured outcome of a playback request.
    ///     播放请求的结构化结果。
    /// </summary>
    public sealed class AudioPlayResult
    {
        private AudioPlayResult(AudioPlayStatus status, IAudioHandle? handle, string? message)
        {
            Status = status;
            Handle = handle;
            Message = message;
        }

        /// <summary>
        ///     Playback outcome category.
        ///     播放结果类别。
        /// </summary>
        public AudioPlayStatus Status { get; }

        /// <summary>
        ///     Handle created for the request when one exists.
        ///     请求存在句柄时为其创建的句柄。
        /// </summary>
        public IAudioHandle? Handle { get; }

        /// <summary>
        ///     Optional diagnostic message.
        ///     可选诊断消息。
        /// </summary>
        public string? Message { get; }

        /// <summary>
        ///     True when playback started successfully.
        ///     播放成功启动时为 true。
        /// </summary>
        public bool Succeeded => Status == AudioPlayStatus.Started;

        /// <summary>
        ///     Creates a successful result.
        ///     创建成功结果。
        /// </summary>
        public static AudioPlayResult Started(IAudioHandle? handle = null, string? message = null)
        {
            return new(AudioPlayStatus.Started, handle, message);
        }

        /// <summary>
        ///     Creates a failed result.
        ///     创建失败结果。
        /// </summary>
        public static AudioPlayResult Fail(AudioPlayStatus status, string? message = null)
        {
            return new(status, null, message);
        }
    }
}
