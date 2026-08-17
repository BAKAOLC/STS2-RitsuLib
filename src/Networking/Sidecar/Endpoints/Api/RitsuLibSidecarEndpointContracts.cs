namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a backend-independent delivery contract for routed endpoint messages.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义路由端点消息使用的后端无关投递契约。
    ///     </para>
    /// </summary>
    public enum RitsuLibSidecarDeliveryProfile : byte
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Reliable, ordered delivery for bounded control messages. Accepted sends remain subject to disconnects
        ///         and do not constitute an application-level acknowledgement.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为有界控制消息提供可靠且有序的投递。发送被接受并不构成应用层确认，断线仍可能使消息无法到达。
        ///     </para>
        /// </summary>
        Control = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Unreliable, sequenced delivery for latency-sensitive datagrams. RitsuLib drops expired queued frames
        ///         and stale received sequence numbers; messages are never fragmented or retransmitted by the data plane.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为延迟敏感的数据报提供不可靠、带序列的投递。RitsuLib 会丢弃队列中过期的帧和收到的旧序列号；
        ///         数据平面绝不会分片或重传此类消息。
        ///     </para>
        /// </summary>
        RealtimeDatagram = 2,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reliable, windowed delivery for bounded byte streams such as images, files, and large snapshots.
        ///         Streams use application acknowledgements, bounded retransmission, cancellation, and end-to-end
        ///         SHA-256 verification. Register this profile through
        ///         <see cref="RitsuLibSidecarEndpoints.RegisterBulk" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为图片、文件和大型快照等有界字节流提供可靠的窗口式投递。流使用应用层确认、有界重传、取消和
        ///         端到端 SHA-256 校验。此档位应通过 <see cref="RitsuLibSidecarEndpoints.RegisterBulk" /> 注册。
        ///     </para>
        /// </summary>
        BulkStream = 3,
    }

    /// <summary>
    ///     <para xml:lang="en">Defines who may participate in and route messages for an endpoint.</para>
    ///     <para xml:lang="zh-CN">定义哪些参与方可以加入端点并为其路由消息。</para>
    /// </summary>
    public enum RitsuLibSidecarEndpointTopology : byte
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The host must register the endpoint. Clients may send only to the host; the host may send to compatible
        ///         clients individually or by broadcast. RitsuLib never relays a client-originated opaque payload to
        ///         another client.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         主机必须注册该端点。客户端只能向主机发送；主机可以单独或广播发送给兼容客户端。RitsuLib
        ///         绝不会把源自客户端的不透明载荷中继给其他客户端。
        ///     </para>
        /// </summary>
        HostAuthority = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The host may route opaque payloads between compatible participants without registering or
        ///         understanding the endpoint itself.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         主机可以在兼容参与方之间路由不透明载荷，而无需自行注册或理解该端点。
        ///     </para>
        /// </summary>
        RelayGroup = 2,
    }

    /// <summary>
    ///     <para xml:lang="en">Defines where an endpoint receive callback executes.</para>
    ///     <para xml:lang="zh-CN">定义端点接收回调的执行位置。</para>
    /// </summary>
    public enum RitsuLibSidecarEndpointDispatchMode
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Queue callbacks on the Godot main loop. This is the default and required for callbacks that access
        ///         scene-tree or game state. The per-endpoint callback queue is bounded; excess deliveries are dropped and
        ///         logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将回调排入 Godot 主循环。这是默认选项；访问场景树或游戏状态的回调必须使用此模式。
        ///         每个端点的回调队列有界；超出容量的投递会被丢弃并记录日志。
        ///     </para>
        /// </summary>
        GodotMainLoop = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Execute synchronously on the transport receive callback. The callback must not block or access
        ///         thread-affine game state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在传输接收回调上同步执行。回调不得阻塞，也不得访问具有线程关联的游戏状态。
        ///     </para>
        /// </summary>
        ReceiveThread = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Execute on a bounded, endpoint-owned serial background queue that preserves callback order. Excess
        ///         deliveries are dropped and logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在端点独占的有界串行后台队列中执行，并保持回调顺序；超出容量的投递会被丢弃并记录日志。
        ///     </para>
        /// </summary>
        BackgroundSerial = 2,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes why a routed endpoint send was accepted or rejected locally.</para>
    ///     <para xml:lang="zh-CN">描述路由端点发送在本地被接受或拒绝的原因。</para>
    /// </summary>
    public enum RitsuLibSidecarSendStatus
    {
        /// <summary>
        ///     <para xml:lang="en">The frame was accepted into RitsuLib's bounded outbound queue.</para>
        ///     <para xml:lang="zh-CN">帧已进入 RitsuLib 的有界出站队列。</para>
        /// </summary>
        Accepted = 0,

        /// <summary>
        ///     <para xml:lang="en">No active multiplayer session is available.</para>
        ///     <para xml:lang="zh-CN">当前没有可用的多人会话。</para>
        /// </summary>
        NoSession,

        /// <summary>
        ///     <para xml:lang="en">The endpoint registration has been disposed.</para>
        ///     <para xml:lang="zh-CN">端点注册已被释放。</para>
        /// </summary>
        EndpointDisposed,

        /// <summary>
        ///     <para xml:lang="en">No compatible negotiated route is currently available.</para>
        ///     <para xml:lang="zh-CN">当前没有可用的兼容协商路由。</para>
        /// </summary>
        RouteUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The requested destination is not an active route participant.</para>
        ///     <para xml:lang="zh-CN">请求的目标不是活动路由参与方。</para>
        /// </summary>
        DestinationUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The current transport cannot satisfy the endpoint's delivery profile.</para>
        ///     <para xml:lang="zh-CN">当前传输无法满足端点的投递档位。</para>
        /// </summary>
        ProfileUnsupported,

        /// <summary>
        ///     <para xml:lang="en">The payload exceeds the negotiated route limit.</para>
        ///     <para xml:lang="zh-CN">载荷超过协商后的路由上限。</para>
        /// </summary>
        PayloadTooLarge,

        /// <summary>
        ///     <para xml:lang="en">The endpoint's local outbound rate limit rejected the frame.</para>
        ///     <para xml:lang="zh-CN">端点的本地出站速率限制拒绝了该帧。</para>
        ///     </summary>
        RateLimited,

        /// <summary>
        ///     <para xml:lang="en">The bounded outbound queue has no capacity for the frame.</para>
        ///     <para xml:lang="zh-CN">有界出站队列没有足够容量容纳该帧。</para>
        /// </summary>
        QueueFull,

        /// <summary>
        ///     <para xml:lang="en">The target Sidecar connection is unavailable.</para>
        ///     <para xml:lang="zh-CN">目标 Sidecar 连接不可用。</para>
        /// </summary>
        TransportUnavailable,

        /// <summary>
        ///     <para xml:lang="en">The requested operation is not valid for the endpoint topology or local role.</para>
        ///     <para xml:lang="zh-CN">请求的操作不适用于该端点拓扑或本地角色。</para>
        /// </summary>
        InvalidOperation,
    }

    /// <summary>
    ///     <para xml:lang="en">Reports the local outcome of a routed endpoint send request.</para>
    ///     <para xml:lang="zh-CN">报告路由端点发送请求的本地结果。</para>
    /// </summary>
    /// <param name="Status">
    ///     <para xml:lang="en">The acceptance or rejection status.</para>
    ///     <para xml:lang="zh-CN">接受或拒绝状态。</para>
    /// </param>
    /// <param name="QueuedRecipientCount">
    ///     <para xml:lang="en">
    ///         Number of physical next-hop frames accepted locally. This is not a delivery acknowledgement.
    ///     </para>
    ///     <para xml:lang="zh-CN">本地接受的物理下一跳帧数量；这不是投递确认。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarSendResult(
        RitsuLibSidecarSendStatus Status,
        int QueuedRecipientCount)
    {
        /// <summary>
        ///     <para xml:lang="en">Whether the request was accepted into the outbound queue.</para>
        ///     <para xml:lang="zh-CN">请求是否已被出站队列接受。</para>
        /// </summary>
        public bool IsAccepted => Status == RitsuLibSidecarSendStatus.Accepted;
    }

    /// <summary>
    ///     <para xml:lang="en">Owns one payload delivered to a registered routed endpoint.</para>
    ///     <para xml:lang="zh-CN">承载投递给已注册路由端点的一份载荷。</para>
    /// </summary>
    /// <param name="OriginalSenderNetId">
    ///     <para xml:lang="en">
    ///         Canonical session peer ID assigned from the host's transport context. Clients cannot choose this value.
    ///     </para>
    ///     <para xml:lang="zh-CN">由主机依据传输上下文确定的规范会话对等方 ID；客户端无法自行指定该值。</para>
    /// </param>
    /// <param name="NegotiatedProtocolVersion">
    ///     <para xml:lang="en">The protocol version selected for this route.</para>
    ///     <para xml:lang="zh-CN">为此路由选择的协议版本。</para>
    /// </param>
    /// <param name="Payload">
    ///     <para xml:lang="en">
    ///         Owned message bytes that remain valid for the duration of the callback. Copy them before retaining.
    ///     </para>
    ///     <para xml:lang="zh-CN">在回调期间保持有效的独占消息字节；如需长期保留，请先复制。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarEndpointMessage(
        ulong OriginalSenderNetId,
        ushort NegotiatedProtocolVersion,
        ReadOnlyMemory<byte> Payload);

    /// <summary>
    ///     <para xml:lang="en">Reports a negotiated endpoint route or participant-set change.</para>
    ///     <para xml:lang="zh-CN">报告协商后的端点路由或参与方集合发生变化。</para>
    /// </summary>
    /// <param name="ProtocolVersion">
    ///     <para xml:lang="en">Selected protocol version, or zero when no route is available.</para>
    ///     <para xml:lang="zh-CN">选定的协议版本；没有可用路由时为零。</para>
    /// </param>
    /// <param name="ParticipantNetIds">
    ///     <para xml:lang="en">An immutable snapshot of compatible participant IDs.</para>
    ///     <para xml:lang="zh-CN">兼容参与方 ID 的不可变快照。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarEndpointParticipantsChangedEvent(
        ushort ProtocolVersion,
        IReadOnlyList<ulong> ParticipantNetIds);
}
