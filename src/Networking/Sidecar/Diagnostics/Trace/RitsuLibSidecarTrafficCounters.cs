namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Process-wide best-effort counters for Sidecar traffic observed by RitsuLib hooks. They do not include vanilla
    ///         <c>NetMessageBus</c> traffic or transport queue depth (not exposed here).
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         RitsuLib 钩子所观察到的 Sidecar 流量的进程级尽力统计。不包括游戏原版
    ///         <c>NetMessageBus</c> 流量或此处未公开的传输队列深度。
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
        ///     <para xml:lang="en">Sum of envelope lengths passed to the vanilla send API for <see cref="OutgoingSendOperations" />.</para>
        ///     <para xml:lang="zh-CN">传给游戏原版发送 API 的 <see cref="OutgoingSendOperations" /> 信封长度总和。</para>
        /// </summary>
        public static long OutgoingWireBytes => Interlocked.Read(ref _outgoingWireBytes);

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
