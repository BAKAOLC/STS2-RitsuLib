using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Networking.MessageExtensions
{
    internal static class RitsuNetMessageTailExtensions
    {
        private const string Magic = "ritsulib.net.tail";
        private const int ContainerVersion = 2;
        private const int LegacyStringContainerVersion = 1;
        private const int ByteBits = 8;
        private const int IntBits = sizeof(int) * ByteBits;
        private const int MaxTailEntryCount = 64;
        private const int MaxTailIdentifierBytes = RitsuLibManagedNetActions.MaxPayloadBytes;
        private const int MaxTailPayloadBytes = (int)RitsuLibSidecarWire.MaxPayloadBytes;
        private const int MaxTailEncodedBytes = 8 * 1024 * 1024;

        private static readonly ConcurrentDictionary<Type, SortedDictionary<string, ExtensionRegistration>>
            Registrations =
                new();

        public static void Register<TMessage>(
            string extensionId,
            int version,
            Func<TMessage, string?> writePayload,
            Action<int, string> readPayload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            ArgumentNullException.ThrowIfNull(writePayload);
            ArgumentNullException.ThrowIfNull(readPayload);
            EnsureStringLength(extensionId, MaxTailIdentifierBytes, "Extension ID", nameof(extensionId));
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");

            var map = Registrations.GetOrAdd(typeof(TMessage),
                _ => new(StringComparer.Ordinal));

            lock (map)
            {
                map[extensionId] = new(
                    version,
                    message =>
                    {
                        var payload = writePayload((TMessage)message);
                        if (string.IsNullOrWhiteSpace(payload))
                            return null;

                        EnsureStringLength(payload, MaxTailPayloadBytes, "Payload", nameof(payload));
                        return Encoding.UTF8.GetBytes(payload);
                    },
                    (payloadVersion, payload) => readPayload(payloadVersion, DecodePayloadString(payload.Span)));
            }
        }

        public static void RegisterBytes<TMessage>(
            string extensionId,
            int version,
            Func<TMessage, byte[]?> writePayload,
            Action<int, ReadOnlyMemory<byte>> readPayload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            ArgumentNullException.ThrowIfNull(writePayload);
            ArgumentNullException.ThrowIfNull(readPayload);
            EnsureStringLength(extensionId, MaxTailIdentifierBytes, "Extension ID", nameof(extensionId));
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");

            var map = Registrations.GetOrAdd(typeof(TMessage),
                _ => new(StringComparer.Ordinal));

            lock (map)
            {
                map[extensionId] = new(version, message => writePayload((TMessage)message), readPayload);
            }
        }

        public static void Write<TMessage>(PacketWriter writer, TMessage message)
        {
            if (!TryGetRegistrations<TMessage>(out var registrations))
            {
                writer.WriteBool(false);
                return;
            }

            var entries = new List<TailEntry>();
            foreach (var (id, registration) in registrations)
            {
                byte[]? payload;
                try
                {
                    payload = registration.WritePayload(message!);
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

                entries.Add(new(id, registration.Version, payload));
            }

            if (entries.Count == 0)
            {
                writer.WriteBool(false);
                return;
            }

            var encodedBits = GetEncodedTailBitCount(entries);
            if (encodedBits > (long)MaxTailEncodedBytes * ByteBits ||
                (long)writer.BitPosition + encodedBits > int.MaxValue)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Trailer for {typeof(TMessage).Name} exceeds " +
                    $"the {MaxTailEncodedBytes}-byte encoded budget.");
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

        public static void Read<TMessage>(PacketReader reader)
        {
            if (!HasRemainingBits(reader, 1))
                return;

            if (!TryGetRegistrations<TMessage>(out var registrations))
                return;

            var registrationsById = registrations.ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);

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
                ConsumeEncodedBudget(ref remainingEncodedBytes, sizeof(int) + Encoding.UTF8.GetByteCount(magic));
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
                        ? Encoding.UTF8.GetBytes(ReadBoundedString(
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

        public static void WriteLegacySingle(PacketWriter writer, string extensionId, int version, string? payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);
            if (version is < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must fit in 8 bits.");
            if (string.IsNullOrWhiteSpace(payload))
            {
                writer.WriteBool(false);
                return;
            }

            if (!TryEnsureStringLength(payload, MaxTailPayloadBytes, "Payload"))
            {
                writer.WriteBool(false);
                return;
            }

            writer.WriteBool(true);
            writer.WriteInt(version, 8);
            writer.WriteString(payload);
        }

        public static void WriteLegacySingleBytes(PacketWriter writer, string extensionId, int version, byte[]? payload)
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

        public static string? TryReadLegacySingle(PacketReader reader, string extensionId, int expectedVersion)
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

        public static byte[]? TryReadLegacySingleBytes(
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
                    return Encoding.UTF8.GetBytes(ReadBoundedString(reader, MaxTailPayloadBytes,
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

            var minimumEntryBits = IntBits + ByteBits + IntBits;
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
            return Encoding.UTF8.GetString(data);
        }

        private static long GetEncodedTailBitCount(IReadOnlyList<TailEntry> entries)
        {
            var bits = 1L;
            bits += (sizeof(int) + Encoding.UTF8.GetByteCount(Magic)) * ByteBits;
            bits += ByteBits + IntBits;

            foreach (var entry in entries)
            {
                bits += (long)(sizeof(int) + Encoding.UTF8.GetByteCount(entry.ExtensionId)) * ByteBits;
                bits += ByteBits;
                bits += (long)(sizeof(int) + entry.Payload.Length) * ByteBits;
            }

            return bits;
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

            return Encoding.UTF8.GetString(payload);
        }

        private static void EnsureStringLength(string value, int maxBytes, string fieldName, string parameterName)
        {
            if (Encoding.UTF8.GetByteCount(value) > maxBytes)
                throw new ArgumentOutOfRangeException(parameterName,
                    $"{fieldName} must not exceed {maxBytes} bytes.");
        }

        private static bool TryEnsureStringLength(string value, int maxBytes, string fieldName)
        {
            var byteCount = Encoding.UTF8.GetByteCount(value);
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
