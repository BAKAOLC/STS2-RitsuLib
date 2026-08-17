namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines fixed opcodes reserved for framework control messages. Opcodes returned by
    ///         <see cref="RitsuLibSidecarOpcodes.For" /> are always outside the reserved range.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义为框架控制消息保留的固定操作码。<see cref="RitsuLibSidecarOpcodes.For" /> 返回的操作码始终位于
    ///         保留范围之外。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarControlOpcodes
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Reserved for an optional framework keepalive or latency probe.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         保留用于可选的框架保活或延迟探测。
        ///     </para>
        /// </summary>
        public const ulong FrameworkPing = RitsuLibSidecarControlOpcodeLayout.FrameworkPing;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Advertises capabilities and initiates wire-version negotiation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         声明功能并发起线路格式版本协商。
        ///     </para>
        /// </summary>
        public const ulong Handshake =
            RitsuLibSidecarControlOpcodeLayout.ControlRangeStart + RitsuLibSidecarControlOpcodeLayout.HandshakeOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Acknowledges <see cref="Handshake" /> and reports the negotiated version and responder features.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确认 <see cref="Handshake" />，并报告协商后的版本和响应方功能。
        ///     </para>
        /// </summary>
        public const ulong HandshakeAck = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                          RitsuLibSidecarControlOpcodeLayout.HandshakeAckOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Carries one segment of a chunked logical payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         承载分块逻辑载荷中的一个分段。
        ///     </para>
        /// </summary>
        public const ulong ChunkedFrame = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                          RitsuLibSidecarControlOpcodeLayout.ChunkedFrameOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reports variable-length ranges of missing segments to the original chunk-stream sender.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向原分块流发送方报告缺失分段的变长范围列表。
        ///     </para>
        /// </summary>
        public const ulong ChunkStreamSelectiveNack = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                      RitsuLibSidecarControlOpcodeLayout.ChunkStreamSelectiveNackOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Confirms reassembly to the original chunk-stream sender so it can discard the retained outbound
        ///         frames.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向原分块流发送方确认重组完成，使其可以丢弃保留的待发送帧。
        ///     </para>
        /// </summary>
        public const ulong ChunkStreamReassemblyDone = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                       RitsuLibSidecarControlOpcodeLayout
                                                           .ChunkStreamReassemblyDoneOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests that the host coordinate a combat-state diagnostic dump across all peers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求主机协调所有对等端生成战斗状态诊断转储。
        ///     </para>
        /// </summary>
        public const ulong DiagnosticRelayDumpRequest = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                        RitsuLibSidecarControlOpcodeLayout
                                                            .DiagnosticRelayDumpRequestOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Carries <see cref="RitsuLibSidecarDiagnosticPayload" /> from the host so each peer records its local
        ///         state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由主机发送 <see cref="RitsuLibSidecarDiagnosticPayload" />，使每个对等端记录本地状态。
        ///     </para>
        /// </summary>
        public const ulong DiagnosticRelayDumpFanout = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                       RitsuLibSidecarControlOpcodeLayout
                                                           .DiagnosticRelayDumpFanoutOffset;

        /// <summary>
        ///     <para xml:lang="en">Carries a peer's complete routed-endpoint capability catalog to the host.</para>
        ///     <para xml:lang="zh-CN">向主机承载对等方完整的路由端点能力目录。</para>
        /// </summary>
        public const ulong EndpointCatalog = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                             RitsuLibSidecarControlOpcodeLayout.EndpointCatalogOffset;

        /// <summary>
        ///     <para xml:lang="en">Carries the host-authoritative routed-endpoint snapshot to one client.</para>
        ///     <para xml:lang="zh-CN">向一个客户端承载由主机确定的路由端点快照。</para>
        /// </summary>
        public const ulong EndpointRouteSnapshot = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                   RitsuLibSidecarControlOpcodeLayout.EndpointRouteSnapshotOffset;

        /// <summary>
        ///     <para xml:lang="en">Acknowledges an atomically applied routed-endpoint snapshot.</para>
        ///     <para xml:lang="zh-CN">确认已原子应用一份路由端点快照。</para>
        /// </summary>
        public const ulong EndpointRouteSnapshotAck = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                                      RitsuLibSidecarControlOpcodeLayout
                                                          .EndpointRouteSnapshotAckOffset;

        /// <summary>
        ///     <para xml:lang="en">Carries client endpoint data to the host for validation and optional relay.</para>
        ///     <para xml:lang="zh-CN">将客户端端点数据送往主机，以便验证并按需中继。</para>
        /// </summary>
        public const ulong EndpointIngress = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                             RitsuLibSidecarControlOpcodeLayout.EndpointIngressOffset;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Carries host-originated or host-relayed endpoint data with a host-assigned canonical sender identity.
        ///     </para>
        ///     <para xml:lang="zh-CN">承载主机发起或主机中继的端点数据，并包含由主机确定的规范发送方身份。</para>
        /// </summary>
        public const ulong EndpointDelivery = RitsuLibSidecarControlOpcodeLayout.ControlRangeStart +
                                              RitsuLibSidecarControlOpcodeLayout.EndpointDeliveryOffset;
    }
}
