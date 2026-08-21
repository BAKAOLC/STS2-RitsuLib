namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Owns one bulk-stream endpoint registration. Streams are unicast, bounded, windowed, cancelable, and
    ///         end-to-end verified. Disposing the handle terminates all active transfers and withdraws the endpoint.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         拥有一个批量流端点注册。流为单播、有界、窗口式、可取消且经过端到端校验。释放句柄会终止全部活动
    ///         传输并撤回端点。
    ///     </para>
    /// </summary>
    public sealed class RitsuLibSidecarBulkEndpointHandle : IDisposable
    {
        private readonly RitsuLibSidecarEndpointRegistration _registration;

        internal RitsuLibSidecarBulkEndpointHandle(RitsuLibSidecarEndpointRegistration registration)
        {
            _registration = registration;
        }

        /// <summary>
        ///     <para xml:lang="en">Immutable routed-endpoint descriptor.</para>
        ///     <para xml:lang="zh-CN">不可变的路由端点描述符。</para>
        /// </summary>
        public RitsuLibSidecarEndpointDescriptor Descriptor => _registration.Descriptor;

        /// <summary>
        ///     <para xml:lang="en">Immutable local bulk-stream policy.</para>
        ///     <para xml:lang="zh-CN">不可变的本地批量流策略。</para>
        /// </summary>
        public RitsuLibSidecarBulkStreamOptions Options => _registration.BulkOptions!;

        /// <summary>
        ///     <para xml:lang="en">Whether the registration has been disposed.</para>
        ///     <para xml:lang="zh-CN">注册是否已经释放。</para>
        /// </summary>
        public bool IsDisposed => _registration.IsDisposed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised after the negotiated protocol version or compatible participant set changes. Handlers follow
        ///         the descriptor dispatch mode and exceptions are isolated.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在协商协议版本或兼容参与方集合变化后引发。处理器遵循描述符的调度模式，且异常会被隔离。
        ///     </para>
        /// </summary>
        public event Action<RitsuLibSidecarEndpointParticipantsChangedEvent> ParticipantsChanged
        {
            add => _registration.ParticipantsChanged += value;
            remove => _registration.ParticipantsChanged -= value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns an immutable snapshot of compatible route participants.</para>
        ///     <para xml:lang="zh-CN">返回兼容路由参与方的不可变快照。</para>
        /// </summary>
        public IReadOnlyList<ulong> GetParticipantsSnapshot()
        {
            return _registration.GetRoute() is { } route
                ? Array.AsReadOnly([.. route.ParticipantNetIds])
                : [];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends exactly <paramref name="length" /> bytes from <paramref name="source" /> to the host. The source
        ///         remains caller-owned and must remain readable until the returned task completes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 <paramref name="source" /> 向主机发送恰好 <paramref name="length" /> 个字节。源流仍由调用方
        ///         拥有，并且在返回任务完成前必须保持可读。
        ///     </para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">Readable source stream; seekability is not required.</para>
        ///     <para xml:lang="zh-CN">可读源流；不要求支持定位。</para>
        /// </param>
        /// <param name="length">
        ///     <para xml:lang="en">Exact number of bytes to consume from the current source position.</para>
        ///     <para xml:lang="zh-CN">从源流当前位置开始消耗的确切字节数。</para>
        /// </param>
        /// <param name="metadata">
        ///     <para xml:lang="en">Optional validated opaque metadata.</para>
        ///     <para xml:lang="zh-CN">可选的经过验证的不透明元数据。</para>
        /// </param>
        /// <param name="progress">
        ///     <para xml:lang="en">Optional cumulative acknowledgement progress observer.</para>
        ///     <para xml:lang="zh-CN">可选的累计确认进度观察器。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Token used to cancel this transfer.</para>
        ///     <para xml:lang="zh-CN">用于取消此传输的令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task that always resolves to a terminal transfer result.</para>
        ///     <para xml:lang="zh-CN">始终解析为最终传输结果的任务。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="source" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" /> 为空。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="source" /> is not readable.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" /> 不可读。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="length" /> is negative or exceeds the endpoint limit.</para>
        ///     <para xml:lang="zh-CN"><paramref name="length" /> 为负数或超过端点限制。</para>
        /// </exception>
        public Task<RitsuLibSidecarBulkTransferResult> SendToHostAsync(
            Stream source,
            long length,
            RitsuLibSidecarBulkStreamMetadata? metadata = null,
            IProgress<RitsuLibSidecarBulkStreamProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return _registration.BulkTransfers!.SendAsync(
                RitsuLibSidecarEndpointDestination.Host,
                0,
                source,
                length,
                metadata,
                progress,
                cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends exactly <paramref name="length" /> bytes from <paramref name="source" /> to one compatible peer
        ///         through the host. Relay-group participants may call it; for a host-authority endpoint only the host may
        ///         call it. The source remains caller-owned until completion.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过主机从 <paramref name="source" /> 向一个兼容对等方发送恰好 <paramref name="length" /> 个
        ///         字节。中继组参与方可以调用；主机权威端点中仅主机可以调用。源流在完成前仍由调用方拥有。
        ///     </para>
        /// </summary>
        /// <param name="peerNetId">
        ///     <para xml:lang="en">Nonzero destination session peer ID.</para>
        ///     <para xml:lang="zh-CN">非零的目标会话对等 ID。</para>
        /// </param>
        /// <param name="source">
        ///     <para xml:lang="en">Readable source stream; seekability is not required.</para>
        ///     <para xml:lang="zh-CN">可读源流；不要求支持定位。</para>
        /// </param>
        /// <param name="length">
        ///     <para xml:lang="en">Exact number of bytes to consume from the current source position.</para>
        ///     <para xml:lang="zh-CN">从源流当前位置开始消耗的确切字节数。</para>
        /// </param>
        /// <param name="metadata">
        ///     <para xml:lang="en">Optional validated opaque metadata.</para>
        ///     <para xml:lang="zh-CN">可选的经过验证的不透明元数据。</para>
        /// </param>
        /// <param name="progress">
        ///     <para xml:lang="en">Optional cumulative acknowledgement progress observer.</para>
        ///     <para xml:lang="zh-CN">可选的累计确认进度观察器。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Token used to cancel this transfer.</para>
        ///     <para xml:lang="zh-CN">用于取消此传输的令牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A task that always resolves to a terminal transfer result.</para>
        ///     <para xml:lang="zh-CN">始终解析为最终传输结果的任务。</para>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">
        ///         <paramref name="peerNetId" /> is zero, or <paramref name="length" /> is negative or exceeds the endpoint
        ///         limit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="peerNetId" /> 为零，或 <paramref name="length" /> 为负数或超过端点限制。
        ///     </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="source" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" /> 为空。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="source" /> is not readable.</para>
        ///     <para xml:lang="zh-CN"><paramref name="source" /> 不可读。</para>
        /// </exception>
        public Task<RitsuLibSidecarBulkTransferResult> SendToPeerAsync(
            ulong peerNetId,
            Stream source,
            long length,
            RitsuLibSidecarBulkStreamMetadata? metadata = null,
            IProgress<RitsuLibSidecarBulkStreamProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfZero(peerNetId);
            return _registration.BulkTransfers!.SendAsync(
                RitsuLibSidecarEndpointDestination.Peer,
                peerNetId,
                source,
                length,
                metadata,
                progress,
                cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">Cancels active transfers and withdraws this registration. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">取消活动传输并撤回此注册；重复调用是安全的。</para>
        /// </summary>
        public void Dispose()
        {
            _registration.Dispose();
        }
    }
}
