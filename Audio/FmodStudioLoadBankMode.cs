namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Values for FMOD Studio load_bank via the Godot FMOD addon.
    /// </summary>
    public enum FmodStudioLoadBankMode
    {
        /// <summary>
        ///     Default blocking load.
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     Load without blocking the caller.
        /// </summary>
        NonBlocking = 1,

        /// <summary>
        ///     Decompress sample data into memory (Studio load flag).
        /// </summary>
        DecompressSamples = 2,
    }
}
