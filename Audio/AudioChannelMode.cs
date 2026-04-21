namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     How a named channel should behave when new playback arrives.
    /// </summary>
    public enum AudioChannelMode
    {
        /// <summary>
        ///     Keep the existing playback and ignore the new request.
        /// </summary>
        KeepExisting = 0,

        /// <summary>
        ///     Stop the existing playback and replace it with the new one.
        /// </summary>
        ReplaceExisting = 1,
    }
}
