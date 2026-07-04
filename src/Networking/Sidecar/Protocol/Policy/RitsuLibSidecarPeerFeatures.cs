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

        /// <summary>
        ///     RitsuLib-managed actions can be carried inside vanilla action enqueue messages.
        ///     RitsuLib 管理的 action 可承载在原版 action 入队消息中。
        /// </summary>
        ManagedNetActions = 1 << 1,

        /// <summary>
        ///     Sidecar envelope payloads may use the Brotli payload compression flag.
        ///     Sidecar envelope payload 可使用 Brotli payload 压缩标志。
        /// </summary>
        BrotliPayloadCompression = 1 << 2,
    }
}
