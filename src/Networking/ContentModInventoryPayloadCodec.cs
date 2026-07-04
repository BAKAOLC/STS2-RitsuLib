using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Networking
{
    internal static class ContentModInventoryPayloadCodec
    {
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
            return entries.Select(entry => new CompactEntry(
                    entry.Id,
                    entry.Version,
                    entry.Name,
                    entry.Source,
                    entry.WorkshopItemId,
                    BuildFlags(entry)))
                .ToArray();
        }

        internal static IReadOnlyList<ContentModInventoryEntry> Expand(IReadOnlyList<CompactEntry> compact)
        {
            return compact
                .Select((entry, index) => new ContentModInventoryEntry(
                    index,
                    entry.Id,
                    entry.Version,
                    entry.Name,
                    entry.Source,
                    entry.WorkshopItemId,
                    (entry.Flags & 1) != 0,
                    (entry.Flags & 2) != 0,
                    (entry.Flags & 4) != 0))
                .ToArray();
        }

        internal static bool TryDecode(string? encoded, out IReadOnlyList<ContentModInventoryEntry> entries)
        {
            entries = [];
            if (string.IsNullOrWhiteSpace(encoded))
                return false;

            try
            {
                var json = Encoding.UTF8.GetString(Gunzip(Convert.FromBase64String(encoded)));
                var compact = JsonSerializer.Deserialize<CompactEntry[]>(json, JsonOptions);
                if (compact == null)
                    return false;

                entries = Expand(compact);
                return true;
            }
            catch (Exception ex)
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
            return flags;
        }

        private static byte[] Gzip(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
            {
                gzip.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        private static byte[] Gunzip(byte[] data)
        {
            using var input = new MemoryStream(data, false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
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
