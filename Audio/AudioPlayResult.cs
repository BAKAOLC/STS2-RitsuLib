namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Structured outcome of a playback request.
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
        /// </summary>
        public AudioPlayStatus Status { get; }

        /// <summary>
        ///     Handle created for the request when one exists.
        /// </summary>
        public IAudioHandle? Handle { get; }

        /// <summary>
        ///     Optional diagnostic message.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        ///     True when playback started successfully.
        /// </summary>
        public bool Succeeded => Status == AudioPlayStatus.Started;

        /// <summary>
        ///     Creates a successful result.
        /// </summary>
        public static AudioPlayResult Started(IAudioHandle? handle = null, string? message = null)
        {
            return new(AudioPlayStatus.Started, handle, message);
        }

        /// <summary>
        ///     Creates a failed result.
        /// </summary>
        public static AudioPlayResult Fail(AudioPlayStatus status, string? message = null)
        {
            return new(status, null, message);
        }
    }
}
