namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Owns one routed endpoint registration. Dispose it to withdraw the endpoint from the current and future
    ///         sessions. Disposal clears queued callbacks but does not abort a callback already executing.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         拥有一个路由端点注册。释放后会从当前及未来会话撤回该端点；释放会清除排队中的回调，但不会中止已经开始执行的回调。
    ///     </para>
    /// </summary>
    public sealed class RitsuLibSidecarEndpointHandle : IDisposable
    {
        private readonly RitsuLibSidecarEndpointRegistration _registration;

        internal RitsuLibSidecarEndpointHandle(RitsuLibSidecarEndpointRegistration registration)
        {
            _registration = registration;
        }

        /// <summary>
        ///     <para xml:lang="en">Immutable descriptor supplied when the endpoint was registered.</para>
        ///     <para xml:lang="zh-CN">注册端点时提供的不可变描述符。</para>
        /// </summary>
        public RitsuLibSidecarEndpointDescriptor Descriptor => _registration.Descriptor;

        /// <summary>
        ///     <para xml:lang="en">Whether this registration has been disposed.</para>
        ///     <para xml:lang="zh-CN">此注册是否已经释放。</para>
        /// </summary>
        public bool IsDisposed => _registration.IsDisposed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised after the negotiated protocol version or compatible participant set changes. Handlers follow
        ///         the endpoint's dispatch mode and exceptions are isolated.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在协商协议版本或兼容参与方集合变化后引发。处理器遵循端点的调度模式，且异常会被隔离。
        ///     </para>
        /// </summary>
        public event Action<RitsuLibSidecarEndpointParticipantsChangedEvent> ParticipantsChanged
        {
            add => _registration.ParticipantsChanged += value;
            remove => _registration.ParticipantsChanged -= value;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns an immutable snapshot of compatible route participants. An empty result means no route is
        ///         currently available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回兼容路由参与方的不可变快照。空结果表示当前没有可用路由。
        ///     </para>
        /// </summary>
        public IReadOnlyList<ulong> GetParticipantsSnapshot()
        {
            return _registration.GetRoute() is { } route
                ? Array.AsReadOnly([.. route.ParticipantNetIds])
                : [];
        }

        /// <summary>
        ///     <para xml:lang="en">Sends one message to the host when the host participates in this route.</para>
        ///     <para xml:lang="zh-CN">当主机参与此路由时，向主机发送一条消息。</para>
        /// </summary>
        /// <param name="payload">
        ///     <para xml:lang="en">Logical payload copied before this method returns.</para>
        ///     <para xml:lang="zh-CN">会在此方法返回前复制的逻辑载荷。</para>
        /// </param>
        public RitsuLibSidecarSendResult SendToHost(ReadOnlySpan<byte> payload)
        {
            return RitsuLibSidecarEndpointProtocol.Send(
                _registration,
                RitsuLibSidecarEndpointDestination.Host,
                0,
                payload);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends one message to every other active participant. Relay-group participants may call it; for a
        ///         host-authority endpoint only the host may call it. It never echoes to the sender.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向其他所有活动参与方发送一条消息。中继组参与方可以调用；主机权威端点中仅主机可以调用。
        ///         此操作绝不会回送给发送者。
        ///     </para>
        /// </summary>
        /// <param name="payload">
        ///     <para xml:lang="en">Logical payload copied before this method returns.</para>
        ///     <para xml:lang="zh-CN">会在此方法返回前复制的逻辑载荷。</para>
        /// </param>
        public RitsuLibSidecarSendResult Broadcast(ReadOnlySpan<byte> payload)
        {
            return RitsuLibSidecarEndpointProtocol.Send(
                _registration,
                RitsuLibSidecarEndpointDestination.Broadcast,
                0,
                payload);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends one message to another active participant through the host. Relay-group participants may call it;
        ///         for a host-authority endpoint only the host may call it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过主机向另一个活动参与方发送一条消息。中继组参与方可以调用；主机权威端点中仅主机可以调用。
        ///     </para>
        /// </summary>
        /// <param name="peerNetId">
        ///     <para xml:lang="en">Nonzero destination session peer ID; it cannot be the local sender.</para>
        ///     <para xml:lang="zh-CN">非零的目标会话对等方 ID；不能是本地发送者自身。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">Logical payload copied before this method returns.</para>
        ///     <para xml:lang="zh-CN">会在此方法返回前复制的逻辑载荷。</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en"><paramref name="peerNetId" /> is zero.</para>
        ///     <para xml:lang="zh-CN"><paramref name="peerNetId" /> 为零。</para>
        /// </exception>
        public RitsuLibSidecarSendResult SendToPeer(ulong peerNetId, ReadOnlySpan<byte> payload)
        {
            ArgumentOutOfRangeException.ThrowIfZero(peerNetId);
            return RitsuLibSidecarEndpointProtocol.Send(
                _registration,
                RitsuLibSidecarEndpointDestination.Peer,
                peerNetId,
                payload);
        }

        /// <summary>
        ///     <para xml:lang="en">Withdraws this endpoint registration. Repeated calls are safe.</para>
        ///     <para xml:lang="zh-CN">撤回此端点注册；重复调用是安全的。</para>
        /// </summary>
        public void Dispose()
        {
            _registration.Dispose();
        }
    }
}
