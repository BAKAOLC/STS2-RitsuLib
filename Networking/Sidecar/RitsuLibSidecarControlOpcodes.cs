namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Reserved fixed opcodes for framework control-plane messages (0…
    ///     <see cref="RitsuLibSidecarOpcodes.FixedProtocolOpcodeMaxInclusive" />).
    ///     User <see cref="RitsuLibSidecarOpcodes.For" /> opcodes are always above that range.
    /// </summary>
    public static class RitsuLibSidecarControlOpcodes
    {
        /// <summary>Optional framework keep-alive or latency probe (reserved).</summary>
        public const ulong FrameworkPing = 1;

        /// <summary>Capability advertisement and version negotiation (payload: <see cref="RitsuLibSidecarHandshakeBinary" />).</summary>
        public const ulong Handshake = 0x10;

        /// <summary>Response to <see cref="Handshake" /> (payload: <see cref="RitsuLibSidecarHandshakeBinary" /> ack layout).</summary>
        public const ulong HandshakeAck = 0x11;

        /// <summary>One chunk of a large logical payload; see chunked stream reassembly.</summary>
        public const ulong ChunkedFrame = 0x12;

        /// <summary>
        ///     Receiver → original chunk sender: missing part ranges (SACK / selective gap report; variable-length
        ///     range list).
        /// </summary>
        public const ulong ChunkStreamSelectiveNack = 0x13;

        /// <summary>
        ///     Receiver → original chunk sender: reassembly completed; sender may drop outbound buffers (8-byte
        ///     <c>streamId</c>).
        /// </summary>
        public const ulong ChunkStreamReassemblyDone = 0x14;

        /// <summary>
        ///     Client → host: request a coordinated combat-state dump across all peers (host fans out
        ///     <see cref="DiagnosticRelayDumpFanout" />).
        /// </summary>
        public const ulong DiagnosticRelayDumpRequest = 0x15;

        /// <summary>Host → everyone: carry <see cref="RitsuLibSidecarDiagnosticPayload" /> so each peer logs local state.</summary>
        public const ulong DiagnosticRelayDumpFanout = 0x16;
    }
}
