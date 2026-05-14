namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Feature bits advertised in <see cref="RitsuLibSidecarHandshakeBinary" />.
    ///     <see cref="RitsuLibSidecarHandshakeBinary" /> 中宣告的 feature bit。
    /// </summary>
    [Flags]
    public enum RitsuLibSidecarPeerFeatures : uint
    {
        /// <summary>
        ///     No optional features advertised.
        ///     未宣告可选 feature。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Chunked large-payload reassembly (opcode <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" />).
        ///     分块大型载荷重组（opcode <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" />）。
        /// </summary>
        ChunkedStreams = 1 << 0,
    }
}
