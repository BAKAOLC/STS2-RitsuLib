using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Networking.MessageExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers and dispatches bounded, versioned extension payloads appended to vanilla network messages.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册并分发追加到原版网络消息末尾的有界版本化扩展载荷。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Registrations are process-wide and case-sensitive per message type. Each extension ID may be registered
    ///         only once, with at most 64 registrations per message type. Registrations remain active for the process
    ///         lifetime. The owner of a message's serialization patches must call <see cref="Write{TMessage}" /> and
    ///         <see cref="Read{TMessage}" /> exactly once after the vanilla body; other mods extending the same message
    ///         should only register their entries. Patch a stable outer serialization boundary: small message
    ///         <c>Serialize</c> and <c>Deserialize</c> methods may be inlined before Harmony can intercept them.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册在进程内按消息类型共享，扩展 ID 区分大小写且只能注册一次，每种消息类型最多注册 64 个扩展，
    ///         并在当前进程的生命周期内持续有效。消息序列化补丁的所有者必须在原版消息体之后各调用一次
    ///         <see cref="Write{TMessage}" /> 与 <see cref="Read{TMessage}" />；扩展同一消息的其他模组只应注册
    ///         各自条目。补丁应放在稳定的外层序列化边界；较短的消息 <c>Serialize</c> 与
    ///         <c>Deserialize</c> 方法可能在 Harmony 拦截前被内联。
    ///     </para>
    /// </remarks>
    public static class RitsuNetMessageTailExtensions
    {
        private const string Magic = "ritsulib.net.tail";
        private const int ContainerVersion = 2;
        private const int LegacyStringContainerVersion = 1;
        private const int ByteBits = 8;
        private const int IntBits = sizeof(int) * ByteBits;
        private const int MaxTailEntryCount = 64;
        private const int MaxTailIdentifierBytes = 256;
        private const int MaxTailPayloadBytes = (int)RitsuLibSidecarWire.MaxPayloadBytes;
        private const int MaxTailEncodedBytes = 8 * 1024 * 1024;

        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static readonly ConcurrentDictionary<Type, SortedDictionary<string, ExtensionRegistration>>
            Registrations =
                new();

        internal static void Register<TMessage>(
            string extensionId,
            int version,
            Func<TMessage, string?> writePayload,
            Action<int, string> readPayload)
        {
            ValidateExtensionId(extensionId);
            ArgumentNullException.ThrowIfNull(writePayload);
            ArgumentNullException.ThrowIfNull(readPayload);
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");

            AddRegistration<TMessage>(extensionId, new(
                version,
                message =>
                {
                    var payload = writePayload((TMessage)message);
                    if (string.IsNullOrWhiteSpace(payload))
                        return null;

                    EnsureStringLength(payload, MaxTailPayloadBytes, "Payload", nameof(payload));
                    return StrictUtf8.GetBytes(payload);
                },
                (payloadVersion, payload) => readPayload(payloadVersion, DecodePayloadString(payload.Span))));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a bounded binary extension for <typeparamref name="TMessage" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TMessage" /> 注册有界二进制扩展。
        ///     </para>
        /// </summary>
        /// <typeparam name="TMessage">
        ///     <para xml:lang="en">The vanilla network message type being extended.</para>
        ///     <para xml:lang="zh-CN">要扩展的原版网络消息类型。</para>
        /// </typeparam>
        /// <param name="extensionId">
        ///     <para xml:lang="en">
        ///         A case-sensitive, UTF-8 ID owned by the registering mod and unique for this message type. Whitespace
        ///         and control characters are not allowed; the encoded ID is limited to 256 bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         归属于注册 Mod、使用 UTF-8 编码且在此消息类型中唯一并区分大小写的 ID；不得包含空白或控制
        ///         字符，编码后不得超过 256 字节。
        ///     </para>
        /// </param>
        /// <param name="version">
        ///     <para xml:lang="en">The payload version in the range 0 through 255.</para>
        ///     <para xml:lang="zh-CN">0 到 255 范围内的载荷版本。</para>
        /// </param>
        /// <param name="writePayload">
        ///     <para xml:lang="en">
        ///         Creates the outgoing payload; null or an empty array omits the entry. Payloads over 4 MiB are
        ///         logged and omitted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建发出载荷；返回 null 或空数组时省略此条目。超过 4 MiB 的载荷会被记录并省略。
        ///     </para>
        /// </param>
        /// <param name="readPayload">
        ///     <para xml:lang="en">
        ///         Consumes the received payload version and read-only bytes synchronously during deserialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">在反序列化期间同步消费接收到的载荷版本与只读字节。</para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The extension ID is empty, malformed, or contains a disallowed character.</para>
        ///     <para xml:lang="zh-CN">扩展 ID 为空、格式无效或包含不允许的字符。</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The extension ID or a payload callback is null.</para>
        ///     <para xml:lang="zh-CN">扩展 ID 或载荷回调为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The extension ID exceeds 256 encoded bytes, or the version does not fit in one byte.</para>
        ///     <para xml:lang="zh-CN">扩展 ID 编码后超过 256 字节，或版本无法用一个字节表示。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">
        ///         The same ID is already registered for this message type, or its registration limit was reached.
        ///     </para>
        ///     <para xml:lang="zh-CN">同一 ID 已为此消息类型注册，或此消息类型已达到注册数量上限。</para>
        /// </exception>
        public static void RegisterBytes<TMessage>(
            string extensionId,
            int version,
            Func<TMessage, byte[]?> writePayload,
            Action<int, ReadOnlyMemory<byte>> readPayload)
        {
            ValidateExtensionId(extensionId);
            ArgumentNullException.ThrowIfNull(writePayload);
            ArgumentNullException.ThrowIfNull(readPayload);
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");

            AddRegistration<TMessage>(extensionId,
                new(version, message => writePayload((TMessage)message), readPayload));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends all registered extensions for <typeparamref name="TMessage" /> after its vanilla body.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <typeparamref name="TMessage" /> 的原版消息体后追加所有已注册扩展。
        ///     </para>
        /// </summary>
        /// <typeparam name="TMessage">
        ///     <para xml:lang="en">The message type whose registered extensions are written.</para>
        ///     <para xml:lang="zh-CN">要写入已注册扩展的消息类型。</para>
        /// </typeparam>
        /// <param name="writer">
        ///     <para xml:lang="en">The writer positioned immediately after the vanilla message body.</para>
        ///     <para xml:lang="zh-CN">位于原版消息体末尾之后的写入器。</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">The message supplied to registered payload writers.</para>
        ///     <para xml:lang="zh-CN">提供给已注册载荷写入回调的消息。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The writer or message is null.</para>
        ///     <para xml:lang="zh-CN">写入器或消息为 null。</para>
        /// </exception>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Call this exactly once for each serialized message. Payload writers run synchronously in ordinal ID
        ///         order. Recoverable writer failures and payloads over 4 MiB are logged and omitted. If the complete
        ///         tail exceeds 8 MiB, it is logged and entirely omitted. Non-recoverable exceptions propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次序列化消息时只能调用一次。载荷写入回调按 ID 的序号顺序同步执行。可恢复的写入回调失败及
        ///         超过 4 MiB 的载荷会被记录并省略；完整消息尾超过 8 MiB 时会被记录并整体省略。不可恢复异常会
        ///         继续传播。
        ///     </para>
        /// </remarks>
        public static void Write<TMessage>(PacketWriter writer, TMessage message)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(message);

            if (!TryGetRegistrations<TMessage>(out var registrations))
            {
                writer.WriteBool(false);
                return;
            }

            var entries = new List<TailEntry>();
            var encodedBits = 1L;
            encodedBits += (sizeof(int) + StrictUtf8.GetByteCount(Magic)) * ByteBits;
            encodedBits += ByteBits + IntBits;
            foreach (var (id, registration) in registrations)
            {
                byte[]? payload;
                try
                {
                    payload = registration.WritePayload(message);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Writer '{id}' failed for {typeof(TMessage).Name}: {ex.Message}");
                    continue;
                }

                if (payload is not { Length: > 0 })
                    continue;

                if (payload.Length > MaxTailPayloadBytes)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Writer '{id}' payload is {payload.Length} bytes; " +
                        $"maximum is {MaxTailPayloadBytes} bytes.");
                    continue;
                }

                if (entries.Count >= MaxTailEntryCount)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Trailer for {typeof(TMessage).Name} exceeds " +
                        $"{MaxTailEntryCount} entries.");
                    writer.WriteBool(false);
                    return;
                }

                var entryBits = (long)(sizeof(int) + StrictUtf8.GetByteCount(id)) * ByteBits;
                entryBits += ByteBits;
                entryBits += (long)(sizeof(int) + payload.Length) * ByteBits;
                if (encodedBits + entryBits > (long)MaxTailEncodedBytes * ByteBits)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Trailer for {typeof(TMessage).Name} exceeds " +
                        $"the {MaxTailEncodedBytes}-byte encoded budget.");
                    writer.WriteBool(false);
                    return;
                }

                entries.Add(new(id, registration.Version, payload));
                encodedBits += entryBits;
            }

            if (entries.Count == 0)
            {
                writer.WriteBool(false);
                return;
            }

            if (writer.BitPosition + encodedBits > int.MaxValue)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Trailer for {typeof(TMessage).Name} exceeds the remaining " +
                    "PacketWriter capacity.");
                writer.WriteBool(false);
                return;
            }

            writer.WriteBool(true);
            writer.WriteString(Magic);
            writer.WriteInt(ContainerVersion, 8);
            writer.WriteInt(entries.Count);
            foreach (var entry in entries)
            {
                writer.WriteString(entry.ExtensionId);
                writer.WriteInt(entry.Version, 8);
                writer.WriteInt(entry.Payload.Length);
                writer.WriteBytes(entry.Payload, entry.Payload.Length);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads and dispatches all registered extensions following the vanilla
        ///         <typeparamref name="TMessage" /> body.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         读取并分发原版 <typeparamref name="TMessage" /> 消息体之后的所有已注册扩展。
        ///     </para>
        /// </summary>
        /// <typeparam name="TMessage">
        ///     <para xml:lang="en">The message type whose registered extensions are read.</para>
        ///     <para xml:lang="zh-CN">要读取已注册扩展的消息类型。</para>
        /// </typeparam>
        /// <param name="reader">
        ///     <para xml:lang="en">The reader positioned immediately after the vanilla message body.</para>
        ///     <para xml:lang="zh-CN">位于原版消息体末尾之后的读取器。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The reader is null.</para>
        ///     <para xml:lang="zh-CN">读取器为 null。</para>
        /// </exception>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Call this exactly once for each deserialized message. Unknown entries are skipped. Malformed data
        ///         and recoverable reader failures are logged without escaping; non-recoverable exceptions propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次反序列化消息时只能调用一次。未知条目会被跳过；格式错误的数据和可恢复的读取回调失败会被
        ///         记录而不向外抛出，不可恢复异常则继续传播。
        ///     </para>
        /// </remarks>
        public static void Read<TMessage>(PacketReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            if (!HasRemainingBits(reader, 1))
                return;

            var registrationsById = TryGetRegistrations<TMessage>(out var registrations)
                ? registrations.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                : new(StringComparer.Ordinal);

            try
            {
                if (!HasRemainingBits(reader, 1) || !reader.ReadBool())
                    return;

                var magic = ReadBoundedString(reader, MaxTailIdentifierBytes, "Tail magic");
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Unknown trailer magic '{magic}' for {typeof(TMessage).Name}.");
                    return;
                }

                if (!HasRemainingBits(reader, ByteBits))
                    throw new InvalidDataException("Tail container version is missing.");

                var containerVersion = reader.ReadInt(ByteBits);
                if (containerVersion != LegacyStringContainerVersion && containerVersion != ContainerVersion)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Unsupported trailer version {containerVersion} for {typeof(TMessage).Name}.");
                    return;
                }

                if (!HasRemainingBits(reader, IntBits))
                    throw new InvalidDataException("Tail entry count is missing.");

                var count = reader.ReadInt();
                ValidateEntryCount(reader, count);
                var remainingEncodedBytes = MaxTailEncodedBytes;
                ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(int) + StrictUtf8.GetByteCount(magic));
                ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(byte) + sizeof(int));
                for (var i = 0; i < count; i++)
                {
                    var id = ReadBoundedString(
                        reader,
                        MaxTailIdentifierBytes,
                        "Tail extension ID",
                        ref remainingEncodedBytes);
                    if (!HasRemainingBits(reader, ByteBits))
                        throw new InvalidDataException("Tail entry version is missing.");

                    var version = reader.ReadInt(ByteBits);
                    ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(byte));
                    var payload = containerVersion == LegacyStringContainerVersion
                        ? StrictUtf8.GetBytes(ReadBoundedString(
                            reader,
                            MaxTailPayloadBytes,
                            "Tail string payload",
                            ref remainingEncodedBytes))
                        : ReadPayloadBytes(reader, ref remainingEncodedBytes);
                    if (!registrationsById.TryGetValue(id, out var registration))
                        continue;

                    try
                    {
                        registration.ReadPayload(version, payload);
                    }
                    catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[NetMessageTailExtensions] Reader '{id}' failed for {typeof(TMessage).Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Failed to read trailer for {typeof(TMessage).Name}: {ex.Message}");
            }
        }

        private static byte[] ReadPayloadBytes(PacketReader reader)
        {
            var remainingEncodedBytes = MaxTailPayloadBytes + sizeof(int);
            return ReadPayloadBytes(reader, ref remainingEncodedBytes);
        }

        private static byte[] ReadPayloadBytes(PacketReader reader, ref int remainingEncodedBytes)
        {
            if (!HasRemainingBits(reader, IntBits))
                throw new InvalidDataException("Tail payload length is missing.");

            var length = reader.ReadInt();
            if (length is < 0 or > MaxTailPayloadBytes)
                throw new InvalidDataException(
                    $"Tail payload length {length} is outside the allowed range 0..{MaxTailPayloadBytes}.");
            ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(int) + length);
            if (!HasRemainingBits(reader, (long)length * ByteBits))
                throw new InvalidDataException("Tail payload exceeds the remaining packet bytes.");

            var payload = new byte[length];
            reader.ReadBytes(payload, length);
            return payload;
        }

        internal static void WriteLegacySingle(PacketWriter writer, string extensionId, int version, string? payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");
            if (string.IsNullOrWhiteSpace(payload) ||
                !TryEnsureStringLength(payload, MaxTailPayloadBytes, "Payload"))
            {
                writer.WriteBool(false);
                return;
            }

            writer.WriteBool(true);
            writer.WriteInt(version, 8);
            writer.WriteString(payload);
        }

        internal static void WriteLegacySingleBytes(PacketWriter writer, string extensionId, int version,
            byte[]? payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");
            if (payload is not { Length: > 0 })
            {
                writer.WriteBool(false);
                return;
            }

            if (payload.Length > MaxTailPayloadBytes)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Legacy payload is {payload.Length} bytes; " +
                    $"maximum is {MaxTailPayloadBytes} bytes.");
                writer.WriteBool(false);
                return;
            }

            writer.WriteBool(true);
            writer.WriteInt(version, 8);
            writer.WriteInt(payload.Length);
            writer.WriteBytes(payload, payload.Length);
        }

        internal static string? TryReadLegacySingle(PacketReader reader, string extensionId, int expectedVersion)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (expectedVersion is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion,
                    "Version must fit in 8 bits.");
            if (!HasRemainingBits(reader, 1))
                return null;

            try
            {
                if (!HasRemainingBits(reader, 1) || !reader.ReadBool())
                    return null;

                if (!HasRemainingBits(reader, ByteBits))
                    throw new InvalidDataException("Legacy tail version is missing.");

                var version = reader.ReadInt(ByteBits);
                if (version == expectedVersion)
                    return ReadBoundedString(reader, MaxTailPayloadBytes, "Legacy tail payload");

                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Unsupported legacy trailer version {version} for '{extensionId}'.");
                return null;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Failed to read legacy trailer '{extensionId}': {ex.Message}");
                return null;
            }
        }

        internal static byte[]? TryReadLegacySingleBytes(
            PacketReader reader,
            string extensionId,
            int expectedVersion,
            int legacyStringVersion,
            out bool wasLegacyString)
        {
            wasLegacyString = false;
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (expectedVersion is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion,
                    "Version must fit in 8 bits.");
            if (legacyStringVersion is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(legacyStringVersion), legacyStringVersion,
                    "Version must fit in 8 bits.");
            if (!HasRemainingBits(reader, 1))
                return null;

            try
            {
                if (!HasRemainingBits(reader, 1) || !reader.ReadBool())
                    return null;

                if (!HasRemainingBits(reader, ByteBits))
                    throw new InvalidDataException("Legacy tail version is missing.");

                var version = reader.ReadInt(ByteBits);
                if (version == expectedVersion)
                    return ReadPayloadBytes(reader);

                if (version == legacyStringVersion)
                {
                    wasLegacyString = true;
                    return StrictUtf8.GetBytes(ReadBoundedString(reader, MaxTailPayloadBytes,
                        "Legacy string payload"));
                }

                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Unsupported legacy trailer version {version} for '{extensionId}'.");
                return null;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Failed to read legacy trailer '{extensionId}': {ex.Message}");
                return null;
            }
        }

        private static void AddRegistration<TMessage>(
            string extensionId,
            ExtensionRegistration registration)
        {
            var map = Registrations.GetOrAdd(typeof(TMessage),
                _ => new(StringComparer.Ordinal));
            lock (map)
            {
                if (map.ContainsKey(extensionId))
                    throw new InvalidOperationException(
                        $"Extension '{extensionId}' is already registered for {typeof(TMessage).FullName}.");
                if (map.Count >= MaxTailEntryCount)
                    throw new InvalidOperationException(
                        $"Message type {typeof(TMessage).FullName} already has the maximum of " +
                        $"{MaxTailEntryCount} registered extensions.");

                map.Add(extensionId, registration);
            }
        }

        private static void ValidateExtensionId(string extensionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (extensionId.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
                throw new ArgumentException("Extension ID must not contain whitespace or control characters.",
                    nameof(extensionId));

            try
            {
                EnsureStringLength(extensionId, MaxTailIdentifierBytes, "Extension ID", nameof(extensionId));
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException("Extension ID must be valid UTF-16 text.", nameof(extensionId), exception);
            }
        }

        private static bool TryGetRegistrations<TMessage>(
            out IReadOnlyList<KeyValuePair<string, ExtensionRegistration>> registrations)
        {
            if (!Registrations.TryGetValue(typeof(TMessage), out var map))
            {
                registrations = [];
                return false;
            }

            lock (map)
            {
                registrations = [.. map];
            }

            return registrations.Count > 0;
        }

        private static void ValidateEntryCount(PacketReader reader, int count)
        {
            if (count is < 0 or > MaxTailEntryCount)
                throw new InvalidDataException(
                    $"Tail entry count {count} is outside the allowed range 0..{MaxTailEntryCount}.");

            const int minimumEntryBits = IntBits + ByteBits + IntBits;
            if (!HasRemainingBits(reader, (long)count * minimumEntryBits))
                throw new InvalidDataException("Tail entry count exceeds the remaining packet bytes.");
        }

        private static string ReadBoundedString(PacketReader reader, int maxBytes, string fieldName)
        {
            var remainingEncodedBytes = maxBytes + sizeof(int);
            return ReadBoundedString(reader, maxBytes, fieldName, ref remainingEncodedBytes);
        }

        private static string ReadBoundedString(
            PacketReader reader,
            int maxBytes,
            string fieldName,
            ref int remainingEncodedBytes)
        {
            if (!HasRemainingBits(reader, IntBits))
                throw new InvalidDataException($"{fieldName} length is missing.");

            var length = reader.ReadInt();
            if (length < 0 || length > maxBytes)
                throw new InvalidDataException(
                    $"{fieldName} length {length} is outside the allowed range 0..{maxBytes}.");
            ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(int) + length);
            if (!HasRemainingBits(reader, (long)length * ByteBits))
                throw new InvalidDataException($"{fieldName} exceeds the remaining packet bytes.");

            var data = new byte[length];
            reader.ReadBytes(data, length);
            return StrictUtf8.GetString(data);
        }

        private static void ConsumeEncodedBudget(ref int remainingBytes, int consumedBytes)
        {
            if (consumedBytes < 0 || consumedBytes > remainingBytes)
                throw new InvalidDataException(
                    $"Tail container exceeds the {MaxTailEncodedBytes}-byte encoded budget.");

            remainingBytes -= consumedBytes;
        }

        private static string DecodePayloadString(ReadOnlySpan<byte> payload)
        {
            if (payload.Length > MaxTailPayloadBytes)
                throw new InvalidDataException(
                    $"Tail string payload exceeds {MaxTailPayloadBytes} bytes.");

            return StrictUtf8.GetString(payload);
        }

        private static void EnsureStringLength(string value, int maxBytes, string fieldName, string parameterName)
        {
            if (StrictUtf8.GetByteCount(value) > maxBytes)
                throw new ArgumentOutOfRangeException(parameterName,
                    $"{fieldName} must not exceed {maxBytes} bytes.");
        }

        private static bool TryEnsureStringLength(string value, int maxBytes, string fieldName)
        {
            var byteCount = StrictUtf8.GetByteCount(value);
            if (byteCount <= maxBytes)
                return true;

            RitsuLibFramework.Logger.Warn(
                $"[NetMessageTailExtensions] {fieldName} is {byteCount} bytes; maximum is {maxBytes} bytes.");
            return false;
        }

        private static bool HasRemainingBits(PacketReader reader, long bitCount)
        {
            return bitCount >= 0 &&
                   reader.BitPosition >= 0 &&
                   (long)reader.Buffer.Length * ByteBits - reader.BitPosition >= bitCount;
        }

        private sealed record ExtensionRegistration(
            int Version,
            Func<object, byte[]?> WritePayload,
            Action<int, ReadOnlyMemory<byte>> ReadPayload);

        private sealed record TailEntry(string ExtensionId, int Version, byte[] Payload);
    }
}
