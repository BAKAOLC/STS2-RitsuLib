namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies the compression mode for a sidecar envelope payload.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定 sidecar 信封载荷的压缩模式。
    ///     </para>
    /// </summary>
    public enum RitsuLibSidecarPayloadCompression
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes the payload without compression.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不压缩载荷。
        ///     </para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Compresses the payload with gzip.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 gzip 压缩载荷。
        ///     </para>
        /// </summary>
        Gzip = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Compresses the payload with Brotli.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 Brotli 压缩载荷。
        ///     </para>
        /// </summary>
        Brotli = 2,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses Brotli only when the payload meets the size threshold and compression saves enough bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅在载荷达到大小阈值且压缩可节省足够字节时使用 Brotli。
        ///     </para>
        /// </summary>
        Auto = 3,
    }
}
