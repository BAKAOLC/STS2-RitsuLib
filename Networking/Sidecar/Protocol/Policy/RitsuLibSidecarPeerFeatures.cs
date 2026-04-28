namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Feature bits advertised in <see cref="RitsuLibSidecarHandshakeBinary" />.
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarPeerFeatures : uint
    {
        /// <summary>
        ///     No optional features advertised.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Chunked large-payload reassembly (opcode <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" />).
        /// </summary>
        ChunkedStreams = 1 << 0,
    }
}
