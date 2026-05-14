namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar envelope flags (uint32, big-endian on wire). Unknown bits are cleared on read for forward
    ///     compatibility.
    ///     Sidecar envelope 标志（uint32，线上 big-endian）。读取时清除未知位以保持向前
    ///     兼容。
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarWireFlags : uint
    {
        /// <summary>
        ///     No flags.
        ///     无标志。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Payload bytes are gzip-compressed (RFC 1952); handlers see decompressed bytes.
        ///     载荷字节经过 gzip 压缩（RFC 1952）；处理器看到解压后的字节。
        /// </summary>
        PayloadGzip = 1u << 0,
    }
}
