namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar envelope flags (uint32, big-endian on wire). Unknown bits are cleared on read for forward
    ///     compatibility.
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarWireFlags : uint
    {
        /// <summary>
        ///     No flags.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Payload bytes are gzip-compressed (RFC 1952); handlers see decompressed bytes.
        /// </summary>
        PayloadGzip = 1u << 0,
    }
}
