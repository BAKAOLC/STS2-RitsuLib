namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Compression mode for sidecar envelope payload bytes.
    ///     Sidecar envelope payload 字节的压缩模式。
    /// </summary>
    public enum RitsuLibSidecarPayloadCompression
    {
        /// <summary>
        ///     Write payload bytes unchanged.
        ///     原样写入 payload 字节。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Compress payload bytes with gzip.
        ///     使用 gzip 压缩 payload 字节。
        /// </summary>
        Gzip = 1,

        /// <summary>
        ///     Compress payload bytes with Brotli.
        ///     使用 Brotli 压缩 payload 字节。
        /// </summary>
        Brotli = 2,

        /// <summary>
        ///     Use Brotli only when the payload is large enough and compression reduces the wire size.
        ///     仅当 payload 足够大且压缩可减小线上大小时使用 Brotli。
        /// </summary>
        Auto = 3,
    }
}
