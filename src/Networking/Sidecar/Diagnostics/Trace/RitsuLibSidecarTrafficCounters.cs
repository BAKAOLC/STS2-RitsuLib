namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Process-wide best-effort counters for Sidecar traffic observed by RitsuLib hooks, including RitsuLib's
    ///         routed-endpoint scheduler. They do not include vanilla <c>NetMessageBus</c> traffic or queue state owned
    ///         internally by a target backend.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         RitsuLib 钩子所观察到的 Sidecar 流量的进程级尽力统计，包括 RitsuLib 路由端点调度器。
    ///         不包括游戏原版 <c>NetMessageBus</c> 流量或目标后端内部拥有的队列状态。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarTrafficCounters
    {
        private static long _incomingPackets;
        private static long _incomingWireBytes;
        private static long _incomingLogicalPayloadBytes;
        private static long _outgoingSendOperations;
        private static long _outgoingWireBytes;

        /// <summary>
        ///     <para xml:lang="en">Inbound Sidecar envelopes that passed parsing and entered the Sidecar receive pipeline.</para>
        ///     <para xml:lang="zh-CN">通过解析并进入 Sidecar 接收流程的入站信封数量。</para>
        /// </summary>
        public static long IncomingPackets => Interlocked.Read(ref _incomingPackets);

        /// <summary>
        ///     <para xml:lang="en">Sum of full on-wire packet lengths for <see cref="IncomingPackets" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="IncomingPackets" /> 的完整线上数据包长度总和。</para>
        /// </summary>
        public static long IncomingWireBytes => Interlocked.Read(ref _incomingWireBytes);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sum of logical payload lengths after decompression, when applicable, for <see cref="IncomingPackets" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="IncomingPackets" /> 的逻辑载荷长度总和；存在压缩时按解压后的长度计算。
        ///     </para>
        /// </summary>
        public static long IncomingLogicalPayloadBytes => Interlocked.Read(ref _incomingLogicalPayloadBytes);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Outbound send operations: one per client in a host broadcast, otherwise one per
        ///         <see cref="RitsuLibSidecarSend" /> call that returned <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         出站发送操作：主机广播时每个客户端一次，否则每次
        ///         <see cref="RitsuLibSidecarSend" /> 调用返回 <see langword="true" /> 时记一次。
        ///     </para>
        /// </summary>
        public static long OutgoingSendOperations => Interlocked.Read(ref _outgoingSendOperations);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sum of envelope lengths passed to the vanilla send API for
        ///         <see cref="OutgoingSendOperations" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">传给游戏原版发送 API 的 <see cref="OutgoingSendOperations" /> 信封长度总和。</para>
        /// </summary>
        public static long OutgoingWireBytes => Interlocked.Read(ref _outgoingWireBytes);

        /// <summary>
        ///     <para xml:lang="en">Routed endpoint frames currently held by the bounded outbound scheduler.</para>
        ///     <para xml:lang="zh-CN">当前由有界出站调度器持有的路由端点帧数。</para>
        /// </summary>
        public static int RoutedEndpointQueuedFrames => RitsuLibSidecarOutboundScheduler.GetQueueCounts().Messages;

        /// <summary>
        ///     <para xml:lang="en">Wire bytes currently held by the routed endpoint outbound scheduler.</para>
        ///     <para xml:lang="zh-CN">当前由路由端点出站调度器持有的线路字节数。</para>
        /// </summary>
        public static int RoutedEndpointQueuedWireBytes => RitsuLibSidecarOutboundScheduler.GetQueueCounts().Bytes;

        /// <summary>
        ///     <para xml:lang="en">Frames rejected because the bounded routed endpoint queue had no capacity.</para>
        ///     <para xml:lang="zh-CN">因路由端点有界队列容量不足而被拒绝的帧数。</para>
        /// </summary>
        public static long RoutedEndpointQueueRejectedFrames =>
            RitsuLibSidecarOutboundScheduler.QueueRejectedFrames;

        /// <summary>
        ///     <para xml:lang="en">Older realtime frames evicted to admit newer realtime data.</para>
        ///     <para xml:lang="zh-CN">为接纳较新的实时数据而被淘汰的旧实时帧数。</para>
        /// </summary>
        public static long RoutedEndpointRealtimeEvictedFrames =>
            RitsuLibSidecarOutboundScheduler.RealtimeEvictedFrames;

        /// <summary>
        ///     <para xml:lang="en">Realtime frames dropped after their configured queue lifetime elapsed.</para>
        ///     <para xml:lang="zh-CN">超过配置队列生存时间后被丢弃的实时帧数。</para>
        /// </summary>
        public static long RoutedEndpointExpiredFrames => RitsuLibSidecarOutboundScheduler.ExpiredFrames;

        /// <summary>
        ///     <para xml:lang="en">Queued frames discarded because their session epoch was no longer current.</para>
        ///     <para xml:lang="zh-CN">因所属会话纪元不再有效而被丢弃的排队帧数。</para>
        /// </summary>
        public static long RoutedEndpointStaleSessionFrames =>
            RitsuLibSidecarOutboundScheduler.StaleSessionFrames;

        /// <summary>
        ///     <para xml:lang="en">Queued frames discarded after their owning endpoint was disposed.</para>
        ///     <para xml:lang="zh-CN">所属端点释放后被丢弃的排队帧数。</para>
        /// </summary>
        public static long RoutedEndpointDisposedFrames => RitsuLibSidecarOutboundScheduler.DisposedFrames;

        /// <summary>
        ///     <para xml:lang="en">Dequeued routed frames rejected by the active transport.</para>
        ///     <para xml:lang="zh-CN">出队后被当前传输拒绝的路由帧数。</para>
        /// </summary>
        public static long RoutedEndpointTransportFailedFrames =>
            RitsuLibSidecarOutboundScheduler.TransportFailedFrames;

        /// <summary>
        ///     <para xml:lang="en">Pending or active inbound bulk streams across all endpoints.</para>
        ///     <para xml:lang="zh-CN">全部端点中等待接受或正在接收的入站批量流数量。</para>
        /// </summary>
        public static int BulkActiveInboundStreams => RitsuLibSidecarBulkTransferCoordinator.ActiveInboundStreams;

        /// <summary>
        ///     <para xml:lang="en">Pending or active outbound bulk streams across all endpoints.</para>
        ///     <para xml:lang="zh-CN">全部端点中等待协商或正在发送的出站批量流数量。</para>
        /// </summary>
        public static int BulkActiveOutboundStreams => RitsuLibSidecarBulkTransferCoordinator.ActiveOutboundStreams;

        /// <summary>
        ///     <para xml:lang="en">Local bulk transfer directions that completed with verified integrity.</para>
        ///     <para xml:lang="zh-CN">已通过完整性校验完成的本地批量传输方向数量。</para>
        /// </summary>
        public static long BulkCompletedTransfers => RitsuLibSidecarBulkTransferManager.CompletedTransfers;

        /// <summary>
        ///     <para xml:lang="en">Local bulk transfer directions that terminated without completing.</para>
        ///     <para xml:lang="zh-CN">未完成即终止的本地批量传输方向数量。</para>
        /// </summary>
        public static long BulkNonCompletedTransfers => RitsuLibSidecarBulkTransferManager.NonCompletedTransfers;

        /// <summary>
        ///     <para xml:lang="en">Bulk offer, data, or completion frames retransmitted after an acknowledgement timeout.</para>
        ///     <para xml:lang="zh-CN">确认超时后重传的批量提议、数据或完成帧数量。</para>
        /// </summary>
        public static long BulkRetransmittedFrames => RitsuLibSidecarBulkTransferManager.RetransmittedFrames;

        /// <summary>
        ///     <para xml:lang="en">Outbound bulk payload bytes cumulatively acknowledged by receivers.</para>
        ///     <para xml:lang="zh-CN">接收方累计确认的出站批量载荷字节数。</para>
        /// </summary>
        public static long BulkAcknowledgedOutboundBytes =>
            RitsuLibSidecarBulkTransferManager.AcknowledgedOutboundBytes;

        /// <summary>
        ///     <para xml:lang="en">Inbound bulk payload bytes committed to caller-provided destinations.</para>
        ///     <para xml:lang="zh-CN">已写入调用方所提供目标的入站批量载荷字节数。</para>
        /// </summary>
        public static long BulkCommittedInboundBytes => RitsuLibSidecarBulkTransferManager.CommittedInboundBytes;

        /// <summary>
        ///     <para xml:lang="en">Sets all counters to zero (e.g. diagnostics or tests).</para>
        ///     <para xml:lang="zh-CN">将所有计数器置零（例如用于诊断或测试）。</para>
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _incomingPackets, 0);
            Interlocked.Exchange(ref _incomingWireBytes, 0);
            Interlocked.Exchange(ref _incomingLogicalPayloadBytes, 0);
            Interlocked.Exchange(ref _outgoingSendOperations, 0);
            Interlocked.Exchange(ref _outgoingWireBytes, 0);
            RitsuLibSidecarOutboundScheduler.ResetStatistics();
            RitsuLibSidecarBulkTransferManager.ResetStatistics();
        }

        internal static void AddIncoming(int wireLen, int logicalPayloadLen)
        {
            Interlocked.Increment(ref _incomingPackets);
            Interlocked.Add(ref _incomingWireBytes, wireLen);
            Interlocked.Add(ref _incomingLogicalPayloadBytes, logicalPayloadLen);
        }

        internal static void AddOutgoing(int operations, long wireBytes)
        {
            Interlocked.Add(ref _outgoingSendOperations, operations);
            Interlocked.Add(ref _outgoingWireBytes, wireBytes);
        }
    }
}
