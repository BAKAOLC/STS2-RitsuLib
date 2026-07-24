using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace STS2RitsuLib.Networking.MessageExtensions
{
    internal static class RitsuNetMessageTailExtensions
    {
        private const string Magic = "ritsulib.net.tail";
        private const int ContainerVersion = 2;
        private const int LegacyStringContainerVersion = 1;

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
                        return string.IsNullOrWhiteSpace(payload) ? null : Encoding.UTF8.GetBytes(payload);
                    },
                    (payloadVersion, payload) => readPayload(payloadVersion, Encoding.UTF8.GetString(payload.Span)));
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
                return;

            var entries = new List<TailEntry>();
            foreach (var (id, registration) in registrations)
            {
                byte[]? payload;
                try
                {
                    payload = registration.WritePayload(message!);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Writer '{id}' failed for {typeof(TMessage).Name}: {ex.Message}");
                    continue;
                }

                if (payload is not { Length: > 0 })
                    continue;

                entries.Add(new(id, registration.Version, payload));
            }

            if (entries.Count == 0)
                return;

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
            if (reader.BitPosition >= reader.Buffer.Length * 8)
                return;

            if (!TryGetRegistrations<TMessage>(out var registrations))
                return;

            var registrationsById = registrations.ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);

            try
            {
                if (!reader.ReadBool())
                    return;

                var magic = reader.ReadString();
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Unknown trailer magic '{magic}' for {typeof(TMessage).Name}.");
                    return;
                }

                var containerVersion = reader.ReadInt(8);
                if (containerVersion != LegacyStringContainerVersion && containerVersion != ContainerVersion)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[NetMessageTailExtensions] Unsupported trailer version {containerVersion} for {typeof(TMessage).Name}.");
                    return;
                }

                var count = reader.ReadInt();
                for (var i = 0; i < count; i++)
                {
                    var id = reader.ReadString();
                    var version = reader.ReadInt(8);
                    var payload = containerVersion == LegacyStringContainerVersion
                        ? Encoding.UTF8.GetBytes(reader.ReadString())
                        : ReadPayloadBytes(reader);
                    if (!registrationsById.TryGetValue(id, out var registration))
                        continue;

                    try
                    {
                        registration.ReadPayload(version, payload);
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[NetMessageTailExtensions] Reader '{id}' failed for {typeof(TMessage).Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Failed to read trailer for {typeof(TMessage).Name}: {ex.Message}");
            }
        }

        private static byte[] ReadPayloadBytes(PacketReader reader)
        {
            var length = reader.ReadInt();
            if (length < 0)
                throw new InvalidDataException("Negative tail payload length.");

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
                return;

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
                return;

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
            if (reader.BitPosition >= reader.Buffer.Length * 8)
                return null;

            try
            {
                if (!reader.ReadBool())
                    return null;

                var version = reader.ReadInt(8);
                if (version == expectedVersion)
                    return reader.ReadString();

                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Unsupported legacy trailer version {version} for '{extensionId}'.");
                return null;
            }
            catch (Exception ex)
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
            if (reader.BitPosition >= reader.Buffer.Length * 8)
                return null;

            try
            {
                if (!reader.ReadBool())
                    return null;

                var version = reader.ReadInt(8);
                if (version == expectedVersion)
                    return ReadPayloadBytes(reader);

                if (version == legacyStringVersion)
                {
                    wasLegacyString = true;
                    return Encoding.UTF8.GetBytes(reader.ReadString());
                }

                RitsuLibFramework.Logger.Warn(
                    $"[NetMessageTailExtensions] Unsupported legacy trailer version {version} for '{extensionId}'.");
                return null;
            }
            catch (Exception ex)
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

        private sealed record ExtensionRegistration(
            int Version,
            Func<object, byte[]?> WritePayload,
            Action<int, ReadOnlyMemory<byte>> ReadPayload);

        private sealed record TailEntry(string ExtensionId, int Version, byte[] Payload);
    }
}
