namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Values for FMOD Studio load_bank via the Godot FMOD addon.
    ///     通过 Godot FMOD addon 调用 FMOD Studio load_bank 的取值。
    /// </summary>
    public enum FmodStudioLoadBankMode
    {
        /// <summary>
        ///     Default blocking load.
        ///     默认阻塞加载。
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     Load without blocking the caller.
        ///     加载时不阻塞调用方。
        /// </summary>
        NonBlocking = 1,

        /// <summary>
        ///     Decompress sample data into memory (Studio load flag).
        ///     将采样数据解压到内存中（Studio 加载标志）。
        /// </summary>
        DecompressSamples = 2,
    }
}
