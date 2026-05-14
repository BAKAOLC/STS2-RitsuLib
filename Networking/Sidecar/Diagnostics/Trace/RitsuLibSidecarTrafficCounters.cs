namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Process-wide best-effort counters for sidecar traffic observed in RitsuLib hooks. Does not include vanilla
    ///     <c>NetMessageBus</c> traffic or transport queue depth (not exposed here).
    ///     RitsuLib hook 中观察到的 sidecar 流量的进程级 best-effort 计数器。不包括原版
    ///     <c>NetMessageBus</c> 流量或传输队列深度（此处未暴露）。
    /// </summary>
    public static class RitsuLibSidecarTrafficCounters
    {
        private static long _incomingPackets;
        private static long _incomingWireBytes;
        private static long _incomingLogicalPayloadBytes;
        private static long _outgoingSendOperations;
        private static long _outgoingWireBytes;

        /// <summary>
        ///     Inbound sidecar envelopes that passed parse and reached <see cref="RitsuLibSidecarBus.Dispatch" />.
        ///     通过解析并到达 <see cref="RitsuLibSidecarBus.Dispatch" /> 的入站 sidecar envelope。
        /// </summary>
        public static long IncomingPackets => Interlocked.Read(ref _incomingPackets);

        /// <summary>
        ///     Sum of full on-wire packet lengths for <see cref="IncomingPackets" />.
        ///     <see cref="IncomingPackets" /> 的完整线上数据包长度总和。
        /// </summary>
        public static long IncomingWireBytes => Interlocked.Read(ref _incomingWireBytes);

        /// <summary>
        ///     Sum of logical payload lengths (after gzip when applicable) for <see cref="IncomingPackets" />.
        ///     <see cref="IncomingPackets" /> 的逻辑载荷长度总和（适用时为 gzip 之后）。
        /// </summary>
        public static long IncomingLogicalPayloadBytes => Interlocked.Read(ref _incomingLogicalPayloadBytes);

        /// <summary>
        ///     Outbound send operations: one per client in a host broadcast, otherwise one per
        ///     <see cref="RitsuLibSidecarSend" /> call that returned <c>true</c>.
        ///     出站发送操作：主机广播时每个客户端一次，否则每次
        ///     <see cref="RitsuLibSidecarSend" /> 调用返回 <c>true</c> 时记一次。
        /// </summary>
        public static long OutgoingSendOperations => Interlocked.Read(ref _outgoingSendOperations);

        /// <summary>
        ///     Sum of envelope lengths passed to the vanilla send API for <see cref="OutgoingSendOperations" />.
        ///     传给原版发送 API 的 <see cref="OutgoingSendOperations" /> envelope 长度总和。
        /// </summary>
        public static long OutgoingWireBytes => Interlocked.Read(ref _outgoingWireBytes);

        /// <summary>
        ///     Sets all counters to zero (e.g. diagnostics or tests).
        ///     将所有计数器置零（例如用于诊断或测试）。
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _incomingPackets, 0);
            Interlocked.Exchange(ref _incomingWireBytes, 0);
            Interlocked.Exchange(ref _incomingLogicalPayloadBytes, 0);
            Interlocked.Exchange(ref _outgoingSendOperations, 0);
            Interlocked.Exchange(ref _outgoingWireBytes, 0);
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
