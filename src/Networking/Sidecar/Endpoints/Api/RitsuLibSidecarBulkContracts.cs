using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Describes the terminal outcome of a bulk-stream transfer.</para>
    ///     <para xml:lang="zh-CN">描述批量流传输的最终结果。</para>
    /// </summary>
    public enum RitsuLibSidecarBulkTransferStatus
    {
        /// <summary>
        ///     <para xml:lang="en">The complete byte stream passed end-to-end integrity verification.</para>
        ///     <para xml:lang="zh-CN">完整字节流已通过端到端完整性校验。</para>
        /// </summary>
        Completed = 0,

        /// <summary>
        ///     <para xml:lang="en">The remote endpoint declined the offered stream.</para>
        ///     <para xml:lang="zh-CN">远端端点拒绝了所提供的流。</para>
        /// </summary>
        Rejected,

        /// <summary>
        ///     <para xml:lang="en">The caller or remote participant canceled the transfer.</para>
        ///     <para xml:lang="zh-CN">调用方或远端参与方取消了传输。</para>
        /// </summary>
        Canceled,

        /// <summary>
        ///     <para xml:lang="en">The transfer exceeded its acknowledgement, retry, or idle-time limit.</para>
        ///     <para xml:lang="zh-CN">传输超过了确认、重试或空闲时间限制。</para>
        /// </summary>
        TimedOut,

        /// <summary>
        ///     <para xml:lang="en">The endpoint route or multiplayer session became unavailable.</para>
        ///     <para xml:lang="zh-CN">端点路由或多人会话变得不可用。</para>
        /// </summary>
        Disconnected,

        /// <summary>
        ///     <para xml:lang="en">The endpoint registration was disposed during the transfer.</para>
        ///     <para xml:lang="zh-CN">传输期间端点注册被释放。</para>
        /// </summary>
        EndpointDisposed,

        /// <summary>
        ///     <para xml:lang="en">The source stream could not supply the declared byte count.</para>
        ///     <para xml:lang="zh-CN">源流无法提供声明的字节数。</para>
        /// </summary>
        SourceFailed,

        /// <summary>
        ///     <para xml:lang="en">The destination stream failed while accepting data.</para>
        ///     <para xml:lang="zh-CN">目标流在接收数据时失败。</para>
        /// </summary>
        DestinationFailed,

        /// <summary>
        ///     <para xml:lang="en">The final SHA-256 digest did not match the received bytes.</para>
        ///     <para xml:lang="zh-CN">最终 SHA-256 摘要与收到的字节不匹配。</para>
        /// </summary>
        IntegrityFailed,

        /// <summary>
        ///     <para xml:lang="en">A local or remote bounded resource limit rejected the transfer.</para>
        ///     <para xml:lang="zh-CN">本地或远端的有界资源限制拒绝了传输。</para>
        /// </summary>
        ResourceLimit,

        /// <summary>
        ///     <para xml:lang="en">Malformed or contradictory stream frames terminated the transfer.</para>
        ///     <para xml:lang="zh-CN">格式错误或相互矛盾的流帧终止了传输。</para>
        /// </summary>
        ProtocolError,
    }

    /// <summary>
    ///     <para xml:lang="en">Identifies the local direction reported by bulk-stream progress.</para>
    ///     <para xml:lang="zh-CN">标识批量流进度所报告的本地方向。</para>
    /// </summary>
    public enum RitsuLibSidecarBulkTransferDirection
    {
        /// <summary>
        ///     <para xml:lang="en">Bytes are being sent from the local source.</para>
        ///     <para xml:lang="zh-CN">字节正从本地源发送。</para>
        /// </summary>
        Sending = 1,

        /// <summary>
        ///     <para xml:lang="en">Bytes are being committed to the local destination.</para>
        ///     <para xml:lang="zh-CN">字节正写入本地目标。</para>
        /// </summary>
        Receiving = 2,
    }

    /// <summary>
    ///     <para xml:lang="en">Immutable optional metadata carried by a bulk-stream offer.</para>
    ///     <para xml:lang="zh-CN">批量流提议所携带的不可变可选元数据。</para>
    /// </summary>
    public sealed class RitsuLibSidecarBulkStreamMetadata
    {
        /// <summary>
        ///     <para xml:lang="en">Creates validated, opaque stream metadata.</para>
        ///     <para xml:lang="zh-CN">创建经过验证的不透明流元数据。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">
        ///         Optional display name occupying at most 255 UTF-8 bytes. It is never interpreted as a filesystem path;
        ///         receivers must choose and validate their own destination path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选显示名称，UTF-8 长度最多为 255 字节。它绝不会被解释为文件系统路径；接收方必须自行选择并
        ///         验证目标路径。
        ///     </para>
        /// </param>
        /// <param name="contentType">
        ///     <para xml:lang="en">Optional visible-ASCII media type occupying at most 127 UTF-8 bytes.</para>
        ///     <para xml:lang="zh-CN">可选的可见 ASCII 媒体类型，UTF-8 长度最多为 127 字节。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">A value exceeds its encoded limit or contains a forbidden control character.</para>
        ///     <para xml:lang="zh-CN">值超过编码限制，或包含禁止的控制字符。</para>
        /// </exception>
        public RitsuLibSidecarBulkStreamMetadata(string? name = null, string? contentType = null)
        {
            ValidateName(name, nameof(name));
            ValidateContentType(contentType, nameof(contentType));
            Name = string.IsNullOrEmpty(name) ? null : name;
            ContentType = string.IsNullOrEmpty(contentType) ? null : contentType;
        }

        /// <summary>
        ///     <para xml:lang="en">Optional opaque display name.</para>
        ///     <para xml:lang="zh-CN">可选的不透明显示名称。</para>
        /// </summary>
        public string? Name { get; }

        /// <summary>
        ///     <para xml:lang="en">Optional media type such as <c>image/png</c>.</para>
        ///     <para xml:lang="zh-CN">可选媒体类型，例如 <c>image/png</c>。</para>
        /// </summary>
        public string? ContentType { get; }

        private static void ValidateName(string? value, string parameterName)
        {
            if (value == null)
                return;
            if (Encoding.UTF8.GetByteCount(value) > RitsuLibSidecarEndpointPolicy.MaxBulkNameUtf8Bytes ||
                value.Any(char.IsControl))
                throw new ArgumentException("Bulk stream name is too long or contains a control character.",
                    parameterName);
        }

        private static void ValidateContentType(string? value, string parameterName)
        {
            if (value == null)
                return;
            if (Encoding.UTF8.GetByteCount(value) > RitsuLibSidecarEndpointPolicy.MaxBulkContentTypeUtf8Bytes ||
                value.Any(character => character is < (char)0x21 or > (char)0x7e))
                throw new ArgumentException(
                    "Bulk stream content type is too long or contains a non-visible ASCII character.",
                    parameterName);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines bounded local resource and timeout policy for one bulk endpoint.</para>
    ///     <para xml:lang="zh-CN">定义一个批量端点的本地有界资源与超时策略。</para>
    /// </summary>
    public sealed class RitsuLibSidecarBulkStreamOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Creates and validates bulk-stream policy.</para>
        ///     <para xml:lang="zh-CN">创建并验证批量流策略。</para>
        /// </summary>
        /// <param name="maxStreamBytes">
        ///     <para xml:lang="en">Maximum declared stream length; zero selects 64 MiB. The absolute limit is 1 GiB.</para>
        ///     <para xml:lang="zh-CN">声明流长度上限；零表示 64 MiB，绝对上限为 1 GiB。</para>
        /// </param>
        /// <param name="preferredChunkBytes">
        ///     <para xml:lang="en">Preferred data bytes per frame; zero selects 16 KiB.</para>
        ///     <para xml:lang="zh-CN">每帧首选数据字节数；零表示 16 KiB。</para>
        /// </param>
        /// <param name="receiveWindowBytes">
        ///     <para xml:lang="en">Maximum unacknowledged receive window; zero selects 256 KiB.</para>
        ///     <para xml:lang="zh-CN">未确认接收窗口上限；零表示 256 KiB。</para>
        /// </param>
        /// <param name="maxConcurrentInboundStreams">
        ///     <para xml:lang="en">Concurrent accepted inbound streams; zero selects two.</para>
        ///     <para xml:lang="zh-CN">并发接受的入站流数量；零表示两个。</para>
        /// </param>
        /// <param name="maxConcurrentOutboundStreams">
        ///     <para xml:lang="en">Concurrent local outbound streams; zero selects two.</para>
        ///     <para xml:lang="zh-CN">本地并发出站流数量；零表示两个。</para>
        /// </param>
        /// <param name="acknowledgementTimeout">
        ///     <para xml:lang="en">Wait before retransmitting an unacknowledged window; null selects two seconds.</para>
        ///     <para xml:lang="zh-CN">重传未确认窗口前的等待时间；空值表示两秒。</para>
        /// </param>
        /// <param name="idleTimeout">
        ///     <para xml:lang="en">Maximum period without valid stream progress; null selects thirty seconds.</para>
        ///     <para xml:lang="zh-CN">没有有效流进度的最长时间；空值表示三十秒。</para>
        /// </param>
        /// <param name="maxRetransmissions">
        ///     <para xml:lang="en">Maximum retransmissions for an offer, window, or completion frame.</para>
        ///     <para xml:lang="zh-CN">提议、窗口或完成帧的最大重传次数。</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">A size, concurrency limit, timeout, or retry count is outside its documented range.</para>
        ///     <para xml:lang="zh-CN">大小、并发限制、超时或重试次数超出文档范围。</para>
        /// </exception>
        public RitsuLibSidecarBulkStreamOptions(
            long maxStreamBytes = 0,
            int preferredChunkBytes = 0,
            int receiveWindowBytes = 0,
            int maxConcurrentInboundStreams = 0,
            int maxConcurrentOutboundStreams = 0,
            TimeSpan? acknowledgementTimeout = null,
            TimeSpan? idleTimeout = null,
            int maxRetransmissions = 5)
        {
            MaxStreamBytes = maxStreamBytes == 0
                ? RitsuLibSidecarEndpointPolicy.DefaultBulkStreamBytes
                : maxStreamBytes;
            PreferredChunkBytes = preferredChunkBytes == 0
                ? RitsuLibSidecarEndpointPolicy.DefaultBulkChunkBytes
                : preferredChunkBytes;
            ReceiveWindowBytes = receiveWindowBytes == 0
                ? RitsuLibSidecarEndpointPolicy.DefaultBulkWindowBytes
                : receiveWindowBytes;
            MaxConcurrentInboundStreams = maxConcurrentInboundStreams == 0
                ? RitsuLibSidecarEndpointPolicy.DefaultBulkConcurrentInboundStreams
                : maxConcurrentInboundStreams;
            MaxConcurrentOutboundStreams = maxConcurrentOutboundStreams == 0
                ? RitsuLibSidecarEndpointPolicy.DefaultBulkConcurrentOutboundStreams
                : maxConcurrentOutboundStreams;
            AcknowledgementTimeout =
                acknowledgementTimeout ?? RitsuLibSidecarEndpointPolicy.DefaultBulkAcknowledgementTimeout;
            IdleTimeout = idleTimeout ?? RitsuLibSidecarEndpointPolicy.DefaultBulkIdleTimeout;
            MaxRetransmissions = maxRetransmissions;

            ArgumentOutOfRangeException.ThrowIfLessThan(MaxStreamBytes, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                MaxStreamBytes,
                RitsuLibSidecarEndpointPolicy.MaxBulkStreamBytes);
            ValidateRange(
                PreferredChunkBytes,
                RitsuLibSidecarEndpointPolicy.MinBulkChunkBytes,
                RitsuLibSidecarEndpointPolicy.MaxBulkChunkBytes,
                nameof(preferredChunkBytes));
            ValidateRange(
                ReceiveWindowBytes,
                RitsuLibSidecarEndpointPolicy.MinBulkWindowBytes,
                RitsuLibSidecarEndpointPolicy.MaxBulkWindowBytes,
                nameof(receiveWindowBytes));
            if (ReceiveWindowBytes < PreferredChunkBytes)
                throw new ArgumentOutOfRangeException(
                    nameof(receiveWindowBytes),
                    ReceiveWindowBytes,
                    "Receive window cannot be smaller than the preferred chunk size.");
            ValidateRange(
                MaxConcurrentInboundStreams,
                1,
                RitsuLibSidecarEndpointPolicy.MaxBulkConcurrentStreamsPerEndpoint,
                nameof(maxConcurrentInboundStreams));
            ValidateRange(
                MaxConcurrentOutboundStreams,
                1,
                RitsuLibSidecarEndpointPolicy.MaxBulkConcurrentStreamsPerEndpoint,
                nameof(maxConcurrentOutboundStreams));
            ValidateRange(
                AcknowledgementTimeout,
                RitsuLibSidecarEndpointPolicy.MinimumBulkAcknowledgementTimeout,
                RitsuLibSidecarEndpointPolicy.MaximumBulkAcknowledgementTimeout,
                nameof(acknowledgementTimeout));
            ValidateRange(
                IdleTimeout,
                RitsuLibSidecarEndpointPolicy.MinimumBulkIdleTimeout,
                RitsuLibSidecarEndpointPolicy.MaximumBulkIdleTimeout,
                nameof(idleTimeout));
            ValidateRange(
                MaxRetransmissions,
                0,
                RitsuLibSidecarEndpointPolicy.MaxBulkRetryCount,
                nameof(maxRetransmissions));
        }

        /// <summary>
        ///     <para xml:lang="en">Maximum declared length accepted or sent by this endpoint.</para>
        ///     <para xml:lang="zh-CN">此端点接受或发送的声明长度上限。</para>
        /// </summary>
        public long MaxStreamBytes { get; }

        /// <summary>
        ///     <para xml:lang="en">Preferred logical data bytes in each bulk data frame.</para>
        ///     <para xml:lang="zh-CN">每个批量数据帧的首选逻辑数据字节数。</para>
        /// </summary>
        public int PreferredChunkBytes { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum bytes accepted before cumulative acknowledgement.</para>
        ///     <para xml:lang="zh-CN">发送累计确认前最多接受的字节数。</para>
        /// </summary>
        public int ReceiveWindowBytes { get; }

        /// <summary>
        ///     <para xml:lang="en">Per-endpoint concurrent inbound stream limit.</para>
        ///     <para xml:lang="zh-CN">单端点并发入站流上限。</para>
        /// </summary>
        public int MaxConcurrentInboundStreams { get; }

        /// <summary>
        ///     <para xml:lang="en">Per-endpoint concurrent outbound stream limit.</para>
        ///     <para xml:lang="zh-CN">单端点并发出站流上限。</para>
        /// </summary>
        public int MaxConcurrentOutboundStreams { get; }

        /// <summary>
        ///     <para xml:lang="en">Time before retransmitting unacknowledged state.</para>
        ///     <para xml:lang="zh-CN">重传未确认状态前的等待时间。</para>
        /// </summary>
        public TimeSpan AcknowledgementTimeout { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum duration without valid transfer progress.</para>
        ///     <para xml:lang="zh-CN">没有有效传输进度的最长持续时间。</para>
        /// </summary>
        public TimeSpan IdleTimeout { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum retransmissions before timing out.</para>
        ///     <para xml:lang="zh-CN">超时前的最大重传次数。</para>
        /// </summary>
        public int MaxRetransmissions { get; }

        private static void ValidateRange(int value, int minimum, int maximum, string parameterName)
        {
            if (value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(parameterName, value,
                    $"Value must be between {minimum} and {maximum}.");
        }

        private static void ValidateRange(TimeSpan value, TimeSpan minimum, TimeSpan maximum, string parameterName)
        {
            if (value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(parameterName, value,
                    $"Value must be between {minimum} and {maximum}.");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Describes one inbound bulk stream before the receiver accepts it.</para>
    ///     <para xml:lang="zh-CN">描述接收方接受前的一条入站批量流。</para>
    /// </summary>
    /// <param name="OriginalSenderNetId">
    ///     <para xml:lang="en">Host-canonicalized session peer ID of the stream sender.</para>
    ///     <para xml:lang="zh-CN">由主机规范化的流发送方会话对等 ID。</para>
    /// </param>
    /// <param name="TransferId">
    ///     <para xml:lang="en">Nonzero sender-scoped transfer identifier.</para>
    ///     <para xml:lang="zh-CN">非零、发送方作用域内的传输标识符。</para>
    /// </param>
    /// <param name="NegotiatedProtocolVersion">
    ///     <para xml:lang="en">Application payload contract version selected for the endpoint route.</para>
    ///     <para xml:lang="zh-CN">为端点路由选定的应用载荷契约版本。</para>
    /// </param>
    /// <param name="Length">
    ///     <para xml:lang="en">Declared total stream length in bytes.</para>
    ///     <para xml:lang="zh-CN">声明的流总字节长度。</para>
    /// </param>
    /// <param name="Metadata">
    ///     <para xml:lang="en">Validated opaque metadata supplied by the sender.</para>
    ///     <para xml:lang="zh-CN">发送方提供的经过验证的不透明元数据。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarBulkStreamOffer(
        ulong OriginalSenderNetId,
        ulong TransferId,
        ushort NegotiatedProtocolVersion,
        long Length,
        RitsuLibSidecarBulkStreamMetadata Metadata);

    /// <summary>
    ///     <para xml:lang="en">Reports acknowledged send progress or committed receive progress.</para>
    ///     <para xml:lang="zh-CN">报告已确认的发送进度或已写入的接收进度。</para>
    /// </summary>
    /// <param name="TransferId">
    ///     <para xml:lang="en">Transfer identifier.</para>
    ///     <para xml:lang="zh-CN">传输标识符。</para>
    /// </param>
    /// <param name="Direction">
    ///     <para xml:lang="en">Local transfer direction.</para>
    ///     <para xml:lang="zh-CN">本地传输方向。</para>
    /// </param>
    /// <param name="ConfirmedBytes">
    ///     <para xml:lang="en">Bytes acknowledged by the receiver or committed to the destination.</para>
    ///     <para xml:lang="zh-CN">接收方已确认或已写入目标的字节数。</para>
    /// </param>
    /// <param name="TotalBytes">
    ///     <para xml:lang="en">Declared total byte count.</para>
    ///     <para xml:lang="zh-CN">声明的总字节数。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarBulkStreamProgress(
        ulong TransferId,
        RitsuLibSidecarBulkTransferDirection Direction,
        long ConfirmedBytes,
        long TotalBytes);

    /// <summary>
    ///     <para xml:lang="en">Reports the terminal state and confirmed byte count of one bulk transfer.</para>
    ///     <para xml:lang="zh-CN">报告一条批量传输的最终状态和已确认字节数。</para>
    /// </summary>
    /// <param name="TransferId">
    ///     <para xml:lang="en">Transfer identifier.</para>
    ///     <para xml:lang="zh-CN">传输标识符。</para>
    /// </param>
    /// <param name="PeerNetId">
    ///     <para xml:lang="en">Remote session peer ID.</para>
    ///     <para xml:lang="zh-CN">远端会话对等 ID。</para>
    /// </param>
    /// <param name="Status">
    ///     <para xml:lang="en">Terminal transfer status.</para>
    ///     <para xml:lang="zh-CN">最终传输状态。</para>
    /// </param>
    /// <param name="ConfirmedBytes">
    ///     <para xml:lang="en">Bytes confirmed before termination.</para>
    ///     <para xml:lang="zh-CN">终止前已确认的字节数。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarBulkTransferResult(
        ulong TransferId,
        ulong PeerNetId,
        RitsuLibSidecarBulkTransferStatus Status,
        long ConfirmedBytes)
    {
        /// <summary>
        ///     <para xml:lang="en">Whether the stream completed with verified integrity.</para>
        ///     <para xml:lang="zh-CN">流是否已通过完整性校验并完成。</para>
        /// </summary>
        public bool IsCompleted => Status == RitsuLibSidecarBulkTransferStatus.Completed;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies a caller-owned writable destination for one accepted inbound stream. RitsuLib exclusively writes
    ///         to the destination until <see cref="Completion" /> finishes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为一条已接受的入站流提供调用方拥有的可写目标。在 <see cref="Completion" /> 完成前，RitsuLib
    ///         独占地写入该目标。
    ///     </para>
    /// </summary>
    public sealed class RitsuLibSidecarBulkReceiveTarget
    {
        private readonly TaskCompletionSource<RitsuLibSidecarBulkTransferResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _attached;

        /// <summary>
        ///     <para xml:lang="en">Creates a receive target for exactly one accepted stream.</para>
        ///     <para xml:lang="zh-CN">为恰好一条已接受的流创建接收目标。</para>
        /// </summary>
        /// <param name="destination">
        ///     <para xml:lang="en">Writable stream positioned where incoming bytes should begin.</para>
        ///     <para xml:lang="zh-CN">定位在传入字节起始写入位置的可写流。</para>
        /// </param>
        /// <param name="leaveOpen">
        ///     <para xml:lang="en">Whether RitsuLib leaves the destination open after every terminal outcome.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 是否在任意最终结果后保持目标流打开。</para>
        /// </param>
        /// <param name="progress">
        ///     <para xml:lang="en">Optional committed-byte progress observer, invoked from the stream worker.</para>
        ///     <para xml:lang="zh-CN">可选的已写入字节进度观察器，由流工作线程调用。</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Optional token that cancels the accepted transfer.</para>
        ///     <para xml:lang="zh-CN">可选的令牌，用于取消已接受的传输。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="destination" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="destination" /> 为空。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="destination" /> is not writable.</para>
        ///     <para xml:lang="zh-CN"><paramref name="destination" /> 不可写。</para>
        /// </exception>
        public RitsuLibSidecarBulkReceiveTarget(
            Stream destination,
            bool leaveOpen = false,
            IProgress<RitsuLibSidecarBulkStreamProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(destination);
            if (!destination.CanWrite)
                throw new ArgumentException("Bulk receive destination must be writable.", nameof(destination));
            Destination = destination;
            LeaveOpen = leaveOpen;
            Progress = progress;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Completes exactly once after verification succeeds or the transfer terminates. Callback exceptions are
        ///         not stored in this task.
        ///     </para>
        ///     <para xml:lang="zh-CN">在校验成功或传输终止后恰好完成一次；回调异常不会存入此任务。</para>
        /// </summary>
        public Task<RitsuLibSidecarBulkTransferResult> Completion => _completion.Task;

        internal Stream Destination { get; }
        internal bool LeaveOpen { get; }
        internal IProgress<RitsuLibSidecarBulkStreamProgress>? Progress { get; }
        internal CancellationToken CancellationToken { get; }

        internal bool TryAttach()
        {
            return Interlocked.Exchange(ref _attached, 1) == 0;
        }

        internal void Complete(RitsuLibSidecarBulkTransferResult result)
        {
            _completion.TrySetResult(result);
        }
    }
}
