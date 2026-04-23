namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Outcome category for a playback request.
    /// </summary>
    public enum AudioPlayStatus
    {
        /// <summary>
        ///     Playback started successfully.
        /// </summary>
        Started,

        /// <summary>
        ///     The requested source shape is invalid for the chosen operation.
        /// </summary>
        InvalidSource,

        /// <summary>
        ///     The FMOD server was unavailable.
        /// </summary>
        MissingServer,

        /// <summary>
        ///     The vanilla audio manager was unavailable.
        /// </summary>
        MissingManager,

        /// <summary>
        ///     A controllable playback instance could not be created.
        /// </summary>
        MissingInstance,

        /// <summary>
        ///     Playback was skipped due to cooldown throttling.
        /// </summary>
        SkippedCooldown,

        /// <summary>
        ///     Playback failed for another reason.
        /// </summary>
        Failed,

        /// <summary>
        ///     The requested operation is not supported for that source type.
        /// </summary>
        NotSupported,
    }
}
