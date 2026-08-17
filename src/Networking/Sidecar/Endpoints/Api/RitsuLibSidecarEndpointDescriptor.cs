using System.Text;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines one process-owned routed endpoint. Registration is unique by the ordinal
    ///         <see cref="OwnerId" /> and <see cref="Name" /> pair.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义一个由当前进程拥有的路由端点。注册按区分大小写的 <see cref="OwnerId" /> 与
    ///         <see cref="Name" /> 组合保持唯一。
    ///     </para>
    /// </summary>
    public sealed class RitsuLibSidecarEndpointDescriptor
    {
        /// <summary>
        ///     <para xml:lang="en">Creates and validates an immutable endpoint descriptor.</para>
        ///     <para xml:lang="zh-CN">创建并验证一个不可变端点描述符。</para>
        /// </summary>
        /// <param name="ownerId">
        ///     <para xml:lang="en">
        ///         Stable mod or library identifier. It must contain only ASCII letters, digits, dot, underscore, or
        ///         hyphen and occupy at most 128 UTF-8 bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         稳定的模组或库标识符。只能包含 ASCII 字母、数字、点、下划线或连字符，且 UTF-8 长度最多为
        ///         128 字节。
        ///     </para>
        /// </param>
        /// <param name="name">
        ///     <para xml:lang="en">
        ///         Stable service name. It follows the owner identifier rules and additionally permits slash.
        ///     </para>
        ///     <para xml:lang="zh-CN">稳定的服务名称。规则与所有者标识符相同，并额外允许斜杠。</para>
        /// </param>
        /// <param name="protocolVersion">
        ///     <para xml:lang="en">Highest payload contract version this endpoint can send and receive.</para>
        ///     <para xml:lang="zh-CN">此端点能够发送和接收的最高载荷契约版本。</para>
        /// </param>
        /// <param name="minimumCompatibleProtocolVersion">
        ///     <para xml:lang="en">
        ///         Lowest payload contract version this endpoint can send and receive. The host selects the highest
        ///         version common to the largest compatible participant set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此端点能够发送和接收的最低载荷契约版本。主机会为最大的兼容参与方集合选择共同支持的最高版本。
        ///     </para>
        /// </param>
        /// <param name="deliveryProfile">
        ///     <para xml:lang="en">Backend-independent delivery contract.</para>
        ///     <para xml:lang="zh-CN">后端无关的投递契约。</para>
        /// </param>
        /// <param name="topology">
        ///     <para xml:lang="en">Endpoint routing and host-authority policy.</para>
        ///     <para xml:lang="zh-CN">端点路由与主机权威策略。</para>
        /// </param>
        /// <param name="maxPayloadBytes">
        ///     <para xml:lang="en">
        ///         Maximum logical payload size. Zero selects the profile default. Negotiation may select a lower value.
        ///     </para>
        ///     <para xml:lang="zh-CN">逻辑载荷大小上限。零表示使用档位默认值；协商结果可能选择更低上限。</para>
        /// </param>
        /// <param name="dispatchMode">
        ///     <para xml:lang="en">Receive callback scheduling policy.</para>
        ///     <para xml:lang="zh-CN">接收回调调度策略。</para>
        /// </param>
        /// <param name="realtimeLifetime">
        ///     <para xml:lang="en">
        ///         Maximum local queue lifetime for realtime datagrams. Null selects 250 milliseconds. It is ignored for
        ///         non-realtime endpoints.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         实时数据报在本地队列中的最长生存时间。空值使用 250 毫秒；非实时端点会忽略该值。
        ///     </para>
        /// </param>
        /// <param name="maxOutboundPacketsPerSecond">
        ///     <para xml:lang="en">Local per-endpoint packet rate; zero selects the profile default.</para>
        ///     <para xml:lang="zh-CN">本地单端点包速率；零表示使用档位默认值。</para>
        /// </param>
        /// <param name="maxOutboundBytesPerSecond">
        ///     <para xml:lang="en">Local per-endpoint byte rate; zero selects the profile default.</para>
        ///     <para xml:lang="zh-CN">本地单端点字节速率；零表示使用档位默认值。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">An identifier is empty, too long, or contains an unsupported character.</para>
        ///     <para xml:lang="zh-CN">标识符为空、过长或包含不支持的字符。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">A version, enum value, payload limit, lifetime, or rate limit is invalid.</para>
        ///     <para xml:lang="zh-CN">版本、枚举值、载荷限制、生存时间或速率限制无效。</para>
        /// </exception>
        public RitsuLibSidecarEndpointDescriptor(
            string ownerId,
            string name,
            ushort protocolVersion,
            ushort minimumCompatibleProtocolVersion,
            RitsuLibSidecarDeliveryProfile deliveryProfile,
            RitsuLibSidecarEndpointTopology topology,
            int maxPayloadBytes = 0,
            RitsuLibSidecarEndpointDispatchMode dispatchMode = RitsuLibSidecarEndpointDispatchMode.GodotMainLoop,
            TimeSpan? realtimeLifetime = null,
            int maxOutboundPacketsPerSecond = 0,
            int maxOutboundBytesPerSecond = 0)
        {
            ValidateIdentifier(ownerId, nameof(ownerId), false);
            ValidateIdentifier(name, nameof(name), true);
            ArgumentOutOfRangeException.ThrowIfZero(protocolVersion);
            ArgumentOutOfRangeException.ThrowIfZero(minimumCompatibleProtocolVersion);
            if (minimumCompatibleProtocolVersion > protocolVersion)
                throw new ArgumentOutOfRangeException(
                    nameof(minimumCompatibleProtocolVersion),
                    minimumCompatibleProtocolVersion,
                    "The minimum compatible protocol version cannot exceed the current protocol version.");
            if (!Enum.IsDefined(deliveryProfile))
                throw new ArgumentOutOfRangeException(nameof(deliveryProfile), deliveryProfile,
                    "Invalid delivery profile.");
            if (!Enum.IsDefined(topology))
                throw new ArgumentOutOfRangeException(nameof(topology), topology, "Invalid endpoint topology.");
            if (!Enum.IsDefined(dispatchMode))
                throw new ArgumentOutOfRangeException(nameof(dispatchMode), dispatchMode, "Invalid dispatch mode.");

            var profileMaximum = deliveryProfile switch
            {
                RitsuLibSidecarDeliveryProfile.Control => RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                    RitsuLibSidecarEndpointPolicy.MaxRealtimePayloadBytes,
                RitsuLibSidecarDeliveryProfile.BulkStream =>
                    RitsuLibSidecarEndpointPolicy.MaxBulkPayloadBytes,
                _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
            };
            if (maxPayloadBytes < 0 || maxPayloadBytes > profileMaximum)
                throw new ArgumentOutOfRangeException(
                    nameof(maxPayloadBytes),
                    maxPayloadBytes,
                    $"Payload size must be zero or between 1 and {profileMaximum} bytes.");
            if (deliveryProfile == RitsuLibSidecarDeliveryProfile.BulkStream &&
                maxPayloadBytes != 0 &&
                maxPayloadBytes <
                RitsuLibSidecarBulkBinary.DataHeaderSize + RitsuLibSidecarEndpointPolicy.MinBulkChunkBytes)
                throw new ArgumentOutOfRangeException(
                    nameof(maxPayloadBytes),
                    maxPayloadBytes,
                    "Bulk payload size cannot hold the minimum data frame.");

            var resolvedLifetime = realtimeLifetime ?? RitsuLibSidecarEndpointPolicy.DefaultRealtimeLifetime;
            if (deliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram &&
                (resolvedLifetime < RitsuLibSidecarEndpointPolicy.MinimumRealtimeLifetime ||
                 resolvedLifetime > RitsuLibSidecarEndpointPolicy.MaximumRealtimeLifetime))
                throw new ArgumentOutOfRangeException(
                    nameof(realtimeLifetime),
                    resolvedLifetime,
                    $"Realtime lifetime must be between {RitsuLibSidecarEndpointPolicy.MinimumRealtimeLifetime} and {RitsuLibSidecarEndpointPolicy.MaximumRealtimeLifetime}.");

            ValidateRate(maxOutboundPacketsPerSecond, RitsuLibSidecarEndpointPolicy.MaxConfiguredPacketsPerSecond,
                nameof(maxOutboundPacketsPerSecond));
            ValidateRate(maxOutboundBytesPerSecond, RitsuLibSidecarEndpointPolicy.MaxConfiguredBytesPerSecond,
                nameof(maxOutboundBytesPerSecond));

            OwnerId = ownerId;
            Name = name;
            ProtocolVersion = protocolVersion;
            MinimumCompatibleProtocolVersion = minimumCompatibleProtocolVersion;
            DeliveryProfile = deliveryProfile;
            Topology = topology;
            MaxPayloadBytes = maxPayloadBytes == 0
                ? deliveryProfile switch
                {
                    RitsuLibSidecarDeliveryProfile.Control =>
                        RitsuLibSidecarEndpointPolicy.DefaultControlPayloadBytes,
                    RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                        RitsuLibSidecarEndpointPolicy.DefaultRealtimePayloadBytes,
                    RitsuLibSidecarDeliveryProfile.BulkStream =>
                        RitsuLibSidecarEndpointPolicy.DefaultBulkPayloadBytes,
                    _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
                }
                : maxPayloadBytes;
            DispatchMode = dispatchMode;
            RealtimeLifetime = deliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram
                ? resolvedLifetime
                : TimeSpan.Zero;
            MaxOutboundPacketsPerSecond = maxOutboundPacketsPerSecond == 0
                ? deliveryProfile switch
                {
                    RitsuLibSidecarDeliveryProfile.Control =>
                        RitsuLibSidecarEndpointPolicy.DefaultControlPacketsPerSecond,
                    RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                        RitsuLibSidecarEndpointPolicy.DefaultRealtimePacketsPerSecond,
                    RitsuLibSidecarDeliveryProfile.BulkStream =>
                        RitsuLibSidecarEndpointPolicy.DefaultBulkPacketsPerSecond,
                    _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
                }
                : maxOutboundPacketsPerSecond;
            MaxOutboundBytesPerSecond = maxOutboundBytesPerSecond == 0
                ? deliveryProfile switch
                {
                    RitsuLibSidecarDeliveryProfile.Control =>
                        RitsuLibSidecarEndpointPolicy.DefaultControlBytesPerSecond,
                    RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                        RitsuLibSidecarEndpointPolicy.DefaultRealtimeBytesPerSecond,
                    RitsuLibSidecarDeliveryProfile.BulkStream =>
                        RitsuLibSidecarEndpointPolicy.DefaultBulkBytesPerSecond,
                    _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
                }
                : maxOutboundBytesPerSecond;
        }

        /// <summary>
        ///     <para xml:lang="en">Stable mod or library owner identifier.</para>
        ///     <para xml:lang="zh-CN">稳定的模组或库所有者标识符。</para>
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        ///     <para xml:lang="en">Stable endpoint service name.</para>
        ///     <para xml:lang="zh-CN">稳定的端点服务名称。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     <para xml:lang="en">Highest supported payload contract version.</para>
        ///     <para xml:lang="zh-CN">支持的最高载荷契约版本。</para>
        /// </summary>
        public ushort ProtocolVersion { get; }

        /// <summary>
        ///     <para xml:lang="en">Lowest supported payload contract version.</para>
        ///     <para xml:lang="zh-CN">支持的最低载荷契约版本。</para>
        /// </summary>
        public ushort MinimumCompatibleProtocolVersion { get; }

        /// <summary>
        ///     <para xml:lang="en">Backend-independent delivery contract.</para>
        ///     <para xml:lang="zh-CN">后端无关的投递契约。</para>
        /// </summary>
        public RitsuLibSidecarDeliveryProfile DeliveryProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Routing and host-authority policy.</para>
        ///     <para xml:lang="zh-CN">路由与主机权威策略。</para>
        /// </summary>
        public RitsuLibSidecarEndpointTopology Topology { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum local logical payload size before route negotiation lowers it.</para>
        ///     <para xml:lang="zh-CN">路由协商进一步降低之前的本地逻辑载荷大小上限。</para>
        /// </summary>
        public int MaxPayloadBytes { get; }

        /// <summary>
        ///     <para xml:lang="en">Receive callback scheduling policy.</para>
        ///     <para xml:lang="zh-CN">接收回调调度策略。</para>
        /// </summary>
        public RitsuLibSidecarEndpointDispatchMode DispatchMode { get; }

        /// <summary>
        ///     <para xml:lang="en">Local outbound queue lifetime for realtime datagrams; zero for other profiles.</para>
        ///     <para xml:lang="zh-CN">实时数据报的本地出站队列生存时间；其他档位为零。</para>
        /// </summary>
        public TimeSpan RealtimeLifetime { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum locally accepted outbound packets per second.</para>
        ///     <para xml:lang="zh-CN">本地每秒最多接受的出站包数。</para>
        /// </summary>
        public int MaxOutboundPacketsPerSecond { get; }

        /// <summary>
        ///     <para xml:lang="en">Maximum locally accepted outbound logical payload bytes per second.</para>
        ///     <para xml:lang="zh-CN">本地每秒最多接受的出站逻辑载荷字节数。</para>
        /// </summary>
        public int MaxOutboundBytesPerSecond { get; }

        private static void ValidateIdentifier(string value, string parameterName, bool allowSlash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Identifier cannot have leading or trailing whitespace.", parameterName);
            if (Encoding.UTF8.GetByteCount(value) > RitsuLibSidecarEndpointPolicy.MaxIdentifierUtf8Bytes)
                throw new ArgumentException(
                    $"Identifier cannot exceed {RitsuLibSidecarEndpointPolicy.MaxIdentifierUtf8Bytes} UTF-8 bytes.",
                    parameterName);

            if (value.Any(character =>
                    character is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_'
                        or '-') &&
                    (!allowSlash || character != '/')))
                throw new ArgumentException(
                    "Identifier contains an unsupported character.",
                    parameterName);
        }

        private static void ValidateRate(int value, int maximum, string parameterName)
        {
            if (value < 0 || value > maximum)
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"Rate must be zero or between 1 and {maximum}.");
        }
    }
}
