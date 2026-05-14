namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Reserved fixed opcodes for framework control-plane messages (0…
    ///     <see cref="RitsuLibSidecarOpcodes.FixedProtocolOpcodeMaxInclusive" />).
    ///     User <see cref="RitsuLibSidecarOpcodes.For" /> opcodes are always above that range.
    ///     框架控制平面消息的保留固定 opcode（0…
    ///     <see cref="RitsuLibSidecarOpcodes.FixedProtocolOpcodeMaxInclusive" />）。
    ///     用户 <see cref="RitsuLibSidecarOpcodes.For" /> opcode 始终高于该范围。
    /// </summary>
    public static class RitsuLibSidecarControlOpcodes
    {
        /// <summary>
        ///     Optional framework keep-alive or latency probe (reserved).
        ///     可选的框架 keep-alive 或延迟探测（保留）。
        /// </summary>
        public const ulong FrameworkPing = RitsuLibSidecarControlOpcodeLayout.FrameworkPing;

        /// <summary>
        ///     Capability advertisement and version negotiation (payload: <see cref="RitsuLibSidecarHandshakeBinary" />).
        ///     能力宣告和版本协商（载荷：<see cref="RitsuLibSidecarHandshakeBinary" />）。
        /// </summary>
        public const ulong Handshake =
            RitsuLibSidecarControlOpcodeLayout.ControlRangeStart + RitsuLibSidecarControlOpcodeLayout.HandshakeOffset;

        /// <summary>
        ///     Response to <see cref="Handshake" /> (payload: <see cref="RitsuLibSidecarHandshakeBinary" /> ack layout).
        ///     对 <see cref="Handshake" /> 的响应（载荷：<see cref="RitsuLibSidecarHandshakeBinary" /> ack 布局）。
        /// </summary>
        public const ulong HandshakeAck = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                          RitsuLibSidecarControlOpcodeLayout.HandshakeAckOffset;

        /// <summary>
        ///     One chunk of a large logical payload; see chunked stream reassembly.
        ///     大型逻辑载荷的一个 chunk；见分块流重组。
        ///     大型逻辑载荷的一个 chunk；见分块流重组。
        /// </summary>
        public const ulong ChunkedFrame = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                          RitsuLibSidecarControlOpcodeLayout.ChunkedFrameOffset;

        /// <summary>
        ///     Receiver → original chunk sender: missing part ranges (SACK / selective gap report; variable-length
        ///     range list).
        ///     接收方 → 原始 chunk 发送方：缺失部分范围（SACK / 选择性缺口报告；变长
        ///     range 列表）。
        /// </summary>
        public const ulong ChunkStreamSelectiveNack = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                      RitsuLibSidecarControlOpcodeLayout.ChunkStreamSelectiveNackOffset;

        /// <summary>
        ///     Receiver → original chunk sender: reassembly completed; sender may drop outbound buffers (8-byte
        ///     <c>streamId</c>).
        ///     接收方 → 原始 chunk 发送方：重组完成；发送方可丢弃出站缓冲区（8 字节
        ///     <c>streamId</c>）。
        /// </summary>
        public const ulong ChunkStreamReassemblyDone = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                       RitsuLibSidecarControlOpcodeLayout
                                                           .ChunkStreamReassemblyDoneOffset;

        /// <summary>
        ///     Client → host: request a coordinated combat-state dump across all peers (host fans out
        ///     <see cref="DiagnosticRelayDumpFanout" />).
        ///     客户端 → 主机：请求在所有 peer 间执行一次协调的战斗状态 dump（主机会 fan out
        ///     <see cref="DiagnosticRelayDumpFanout" />）。
        /// </summary>
        public const ulong DiagnosticRelayDumpRequest = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                        RitsuLibSidecarControlOpcodeLayout
                                                            .DiagnosticRelayDumpRequestOffset;

        /// <summary>
        ///     Host → everyone: carry <see cref="RitsuLibSidecarDiagnosticPayload" /> so each peer logs local state.
        ///     主机到所有人：携带 <see cref="RitsuLibSidecarDiagnosticPayload" />，使每个 peer 记录本地状态。
        /// </summary>
        public const ulong DiagnosticRelayDumpFanout = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                       RitsuLibSidecarControlOpcodeLayout
                                                           .DiagnosticRelayDumpFanoutOffset;
    }
}
