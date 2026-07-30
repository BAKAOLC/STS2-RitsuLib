namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the FMOD Studio bank-loading mode passed to the Godot FMOD addon's <c>load_bank</c> method.</para>
    ///     <para xml:lang="zh-CN">指定传递给 Godot FMOD 插件 <c>load_bank</c> 方法的 FMOD Studio 音频库加载模式。</para>
    /// </summary>
    public enum FmodStudioLoadBankMode
    {
        /// <summary>
        ///     <para xml:lang="en">Loads the bank synchronously with the default FMOD Studio behavior.</para>
        ///     <para xml:lang="zh-CN">使用 FMOD Studio 默认行为同步加载音频库。</para>
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     <para xml:lang="en">Starts loading without blocking the caller.</para>
        ///     <para xml:lang="zh-CN">开始加载，但不阻塞调用方。</para>
        /// </summary>
        NonBlocking = 1,

        /// <summary>
        ///     <para xml:lang="en">Loads the bank while decompressing its sample data into memory.</para>
        ///     <para xml:lang="zh-CN">加载音频库，并将其中的采样数据解压到内存。</para>
        /// </summary>
        DecompressSamples = 2,
    }
}
