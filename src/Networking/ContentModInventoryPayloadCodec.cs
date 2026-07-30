using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Networking
{
    internal static class ContentModInventoryPayloadCodec
    {
        private const int MaxPayloadBytes = (int)RitsuLibSidecarWire.MaxPayloadBytes;
        private const int MaxEncodedPayloadChars = 4 * ((MaxPayloadBytes + 2) / 3);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        internal static string Encode(IReadOnlyList<ContentModInventoryEntry> entries)
        {
            var compact = Compact(entries);
            var json = JsonSerializer.Serialize(compact, JsonOptions);
            return Convert.ToBase64String(Gzip(Encoding.UTF8.GetBytes(json)));
        }

        internal static IReadOnlyList<CompactEntry> Compact(IReadOnlyList<ContentModInventoryEntry> entries)
        {
            return
            [
                .. entries.Select(entry => new CompactEntry(
                    entry.Id,
                    entry.Version,
                    entry.Name,
                    entry.Source,
                    entry.WorkshopItemId,
                    BuildFlags(entry))),
            ];
        }

        internal static IReadOnlyList<ContentModInventoryEntry> Expand(IReadOnlyList<CompactEntry> compact)
        {
            return
            [
                .. compact
                    .Select((entry, index) => new ContentModInventoryEntry(
                        index,
                        entry.Id,
                        entry.Version,
                        entry.Name,
                        entry.Source,
                        entry.WorkshopItemId,
                        (entry.Flags & 1) != 0,
                        (entry.Flags & 2) != 0,
                        (entry.Flags & 4) != 0,
                        (entry.Flags & 8) != 0)),
            ];
        }

        internal static bool TryDecode(string? encoded, out IReadOnlyList<ContentModInventoryEntry> entries)
        {
            entries = [];
            if (string.IsNullOrWhiteSpace(encoded))
                return false;

            try
            {
                var encodedPayloadChars = 0;
                // This bounded validation exits before allocating or decoding an oversized payload.
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var character in encoded)
                {
                    if (char.IsWhiteSpace(character))
                        continue;
                    if (++encodedPayloadChars > MaxEncodedPayloadChars)
                        throw new InvalidDataException(
                            $"Encoded content mod inventory exceeds {MaxPayloadBytes} compressed bytes.");
                }

                var json = Encoding.UTF8.GetString(Gunzip(Convert.FromBase64String(encoded)));
                var compact = JsonSerializer.Deserialize<CompactEntry[]>(json, JsonOptions);
                if (compact == null)
                    return false;

                entries = Expand(compact);
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[ContentModInventoryPayload] Failed to decode payload: {ex.Message}");
                return false;
            }
        }

        private static int BuildFlags(ContentModInventoryEntry entry)
        {
            var flags = 0;
            if (entry.IsEnabled)
                flags |= 1;
            if (entry.AffectsGameplay)
                flags |= 2;
            if (entry.IsDependency)
                flags |= 4;
            if (entry.IsCommonIncompatibleMod)
                flags |= 8;

            return flags;
        }

        private static byte[] Gzip(byte[] data)
        {
            EnsurePayloadLength(data.Length, "uncompressed");
            var compressed = RitsuLibSidecarCompression.GzipCompress(data);
            EnsurePayloadLength(compressed.Length, "compressed");
            return compressed;
        }

        private static byte[] Gunzip(ReadOnlySpan<byte> data)
        {
            if (RitsuLibSidecarCompression.TryGunzip(data, out var decompressed))
                return decompressed;

            throw new InvalidDataException(
                $"Content mod inventory gzip data is invalid or exceeds {MaxPayloadBytes} decompressed bytes.");
        }

        private static void EnsurePayloadLength(int length, string kind)
        {
            if (length > MaxPayloadBytes)
                throw new InvalidDataException(
                    $"Content mod inventory {kind} payload exceeds {MaxPayloadBytes} bytes.");
        }

        internal sealed record CompactEntry(
            string Id,
            string Version,
            string Name,
            string Source,
            ulong? WorkshopItemId,
            int Flags);
    }
}
