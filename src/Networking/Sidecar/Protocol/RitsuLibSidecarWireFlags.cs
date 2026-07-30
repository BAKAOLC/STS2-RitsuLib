namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the 32-bit, big-endian flags in a sidecar envelope. Unknown bits are cleared when read.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 sidecar 信封中采用大端序编码的 32 位标志；读取时会清除未知位。
    ///     </para>
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarWireFlags : uint
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         No flags are set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不设置任何标志。
        ///     </para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The payload uses gzip compression; handlers receive decompressed bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         载荷使用 gzip 压缩；处理器接收解压后的字节。
        ///     </para>
        /// </summary>
        PayloadGzip = 1u << 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The payload uses Brotli compression; handlers receive decompressed bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         载荷使用 Brotli 压缩；处理器接收解压后的字节。
        ///     </para>
        /// </summary>
        PayloadBrotli = 1u << 1,
    }
}
