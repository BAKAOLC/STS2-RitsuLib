using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Diagnostics.Logging;
using STS2RitsuLib.Interop.Patches;
using STS2RitsuLib.Networking.MessageExtensions;

namespace STS2RitsuLib.Networking.StateDivergence
{
    internal sealed record StateDivergenceSupplementPayload(
        uint ChecksumId,
        uint ChecksumValue,
        int SavedPropertyNetIdBitSize,
        uint SavedPropertyMapHash,
        IReadOnlyList<string> SavedPropertyNames,
        bool? ModelDbHashUsesDeterministicCache,
        bool? SavedPropertyNetIdUsesDeterministicSort,
        string? LoadedMods,
        string? ContentMods,
        ProgressDiagnosticsSnapshot? Progress,
        StateDivergenceRecentLogSnapshot? RecentLogs);

    internal sealed record StateDivergenceRecentLogSnapshot(
        DateTimeOffset CapturedAtUtc,
        int TotalRecordCount,
        int IncludedRecordCount,
        int DroppedOldRecordCount,
        IReadOnlyList<StateDivergenceLogRecord> Records);

    internal sealed record StateDivergenceLogRecord(
        long Id,
        DateTimeOffset Timestamp,
        string SeverityText,
        int SeverityNumber,
        string Body,
        string? Source,
        string? Category,
        string? LoggerName,
        string? CodeFilePath,
        string? CodeFunctionName,
        int? CodeLineNumber);

    internal sealed record StateDivergenceSupplementPayloadV6(
        uint ChecksumId,
        uint ChecksumValue,
        int SavedPropertyNetIdBitSize,
        uint SavedPropertyMapHash,
        IReadOnlyList<string> SavedPropertyNames,
        bool? ModelDbHashUsesDeterministicCache,
        bool? SavedPropertyNetIdUsesDeterministicSort,
        IReadOnlyList<ContentModInventoryPayloadCodec.CompactEntry> LoadedMods,
        IReadOnlyList<ContentModInventoryPayloadCodec.CompactEntry> ContentMods,
        ProgressDiagnosticsSnapshot? Progress,
        StateDivergenceRecentLogSnapshot? RecentLogs);

    internal static class StateDivergenceSupplementPayloadCodec
    {
        private const string ExtensionId = "ritsulib.stateDivergence";
        private const int PayloadVersion = 6;
        private const int MaxCompressedPayloadBytes = 128 * 1024;
        private static int _registered;
        private static readonly Lock PreparedOutgoingLock = new();

        private static readonly Dictionary<(uint Id, uint Checksum), Queue<StateDivergenceSupplementPayload>>
            PreparedOutgoingPayloads = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 1)
                return;

            RitsuNetMessageTailExtensions.RegisterBytes<StateDivergenceMessage>(
                ExtensionId,
                PayloadVersion,
                SerializePayload,
                ReadPayload);
        }

        public static void Write(PacketWriter writer, StateDivergenceMessage message)
        {
            EnsureRegistered();
            RitsuNetMessageTailExtensions.Write(writer, message);
        }

        public static void Read(PacketReader reader)
        {
            EnsureRegistered();
            RitsuNetMessageTailExtensions.Read<StateDivergenceMessage>(reader);
        }

        public static StateDivergenceSupplementPayload CreateLocalSnapshot(NetChecksumData checksum)
        {
            var propertyNames = GetSavedPropertyNames();
            var modelDbCacheStatus = ModelIdSerializationCacheDynamicContentPatch.GetDeterministicCacheStatus();
            return new(
                checksum.id,
                checksum.checksum,
                SavedPropertiesTypeCache.NetIdBitSize,
                StableHash(propertyNames),
                propertyNames,
                modelDbCacheStatus.IsActive,
                SavedPropertiesTypeCacheInjectionPatch.UsesDeterministicNetIdTable,
                ContentModInventoryPayloadCodec.Encode(ContentModLoadOrderInventory.BuildRuntimeLoadedInventory()),
                ContentModInventoryPayloadCodec.Encode(ContentModLoadOrderInventory.BuildRuntimeRelevantInventory()),
                ProgressDiagnosticsSnapshot.CreateLocal(),
                CreateRecentLogSnapshot(RitsuDebugLogPipeline.Snapshot(0), 0));
        }

        public static void PrepareOutgoingSnapshot(StateDivergenceSupplementPayload payload)
        {
            lock (PreparedOutgoingLock)
            {
                var key = (payload.ChecksumId, payload.ChecksumValue);
                if (!PreparedOutgoingPayloads.TryGetValue(key, out var queue))
                {
                    queue = new();
                    PreparedOutgoingPayloads[key] = queue;
                }

                queue.Enqueue(payload);
            }
        }

        private static byte[]? SerializePayload(StateDivergenceMessage message)
        {
            try
            {
                var payload = TryTakePreparedOutgoingSnapshot(message.senderChecksum, out var prepared)
                    ? prepared
                    : CreateLocalSnapshot(message.senderChecksum);
                return EncodeWithinBudget(payload);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] Failed to create supplement payload: {ex.Message}");
                return null;
            }
        }

        private static bool TryTakePreparedOutgoingSnapshot(
            NetChecksumData checksum,
            out StateDivergenceSupplementPayload payload)
        {
            lock (PreparedOutgoingLock)
            {
                var key = (checksum.id, checksum.checksum);
                if (!PreparedOutgoingPayloads.TryGetValue(key, out var queue) || queue.Count == 0)
                {
                    payload = null!;
                    return false;
                }

                payload = queue.Dequeue();
                if (queue.Count == 0)
                    PreparedOutgoingPayloads.Remove(key);
                return true;
            }
        }

        private static void ReadPayload(int version, ReadOnlyMemory<byte> encoded)
        {
            try
            {
                if (version != 1 && version != 2 && version != 3 && version != 4 && version != 5 &&
                    version != PayloadVersion)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[State divergence diagnostics] Unsupported supplement payload version: {version}");
                    return;
                }

                var json = version == PayloadVersion
                    ? Encoding.UTF8.GetString(Unbrotli(encoded.Span))
                    : Encoding.UTF8.GetString(Gunzip(Convert.FromBase64String(Encoding.UTF8.GetString(encoded.Span))));
                var payload = version switch
                {
                    1 => ConvertLegacyPayload(JsonSerializer.Deserialize<StateDivergenceSupplementPayloadV1>(
                        json,
                        JsonOptions)),
                    PayloadVersion => FromWirePayload(
                        JsonSerializer.Deserialize<StateDivergenceSupplementPayloadV6>(json, JsonOptions)),
                    _ => JsonSerializer.Deserialize<StateDivergenceSupplementPayload>(json, JsonOptions),
                };
                if (payload != null)
                    StateDivergenceSupplementStore.Store(payload);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] Failed to read supplement payload: {ex.Message}");
            }
        }

        private static IReadOnlyList<string> GetSavedPropertyNames()
        {
            return AccessTools.DeclaredField(typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap")
                ?.GetValue(null) is List<string> names
                ? names.ToArray()
                : [];
        }

        private static byte[] Brotli(byte[] data)
        {
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, true))
            {
                brotli.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        private static byte[]? EncodeWithinBudget(StateDivergenceSupplementPayload payload)
        {
            if (TryEncode(payload, out var encoded, out _))
                return encoded;

            var logs = payload.RecentLogs;
            if (logs == null || logs.Records.Count == 0)
                return null;

            byte[]? best = null;
            var low = 0;
            var high = logs.Records.Count;
            while (low <= high)
            {
                var count = low + (high - low) / 2;
                var candidate = payload with
                {
                    RecentLogs = CreateRecentLogSnapshot(logs.Records.TakeLast(count).ToArray(),
                        logs.TotalRecordCount - count),
                };

                if (TryEncode(candidate, out var candidateEncoded, out _))
                {
                    best = candidateEncoded;
                    low = count + 1;
                }
                else
                {
                    high = count - 1;
                }
            }

            return best;
        }

        private static bool TryEncode(
            StateDivergenceSupplementPayload payload,
            out byte[] encoded,
            out int compressedBytes)
        {
            var json = JsonSerializer.Serialize(ToWirePayload(payload), JsonOptions);
            var compressed = Brotli(Encoding.UTF8.GetBytes(json));
            compressedBytes = compressed.Length;
            if (compressed.Length > MaxCompressedPayloadBytes)
            {
                encoded = [];
                return false;
            }

            encoded = compressed;
            return true;
        }

        private static StateDivergenceRecentLogSnapshot CreateRecentLogSnapshot(
            IReadOnlyList<RitsuDebugLogRecord> records,
            int droppedOldRecords)
        {
            return CreateRecentLogSnapshot(records.Select(CreateLogRecord).ToArray(), droppedOldRecords);
        }

        private static StateDivergenceRecentLogSnapshot CreateRecentLogSnapshot(
            IReadOnlyList<StateDivergenceLogRecord> records,
            int droppedOldRecords)
        {
            return new(
                DateTimeOffset.UtcNow,
                records.Count + Math.Max(0, droppedOldRecords),
                records.Count,
                Math.Max(0, droppedOldRecords),
                records);
        }

        private static StateDivergenceLogRecord CreateLogRecord(RitsuDebugLogRecord record)
        {
            return new(
                record.Id,
                record.Timestamp,
                record.SeverityText,
                record.SeverityNumber,
                record.Body,
                EmptyToNull(record.Source),
                EmptyToNull(record.Category),
                EmptyToNull(record.LoggerName),
                EmptyToNull(record.CodeFilePath),
                EmptyToNull(record.CodeFunctionName),
                record.CodeLineNumber);
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static byte[] Gunzip(byte[] data)
        {
            using var input = new MemoryStream(data, false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] Unbrotli(ReadOnlySpan<byte> data)
        {
            using var input = new MemoryStream(data.ToArray(), false);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            brotli.CopyTo(output);
            return output.ToArray();
        }

        private static StateDivergenceSupplementPayloadV6 ToWirePayload(StateDivergenceSupplementPayload payload)
        {
            ContentModInventoryPayloadCodec.TryDecode(payload.LoadedMods, out var loadedMods);
            ContentModInventoryPayloadCodec.TryDecode(payload.ContentMods, out var contentMods);
            return new(
                payload.ChecksumId,
                payload.ChecksumValue,
                payload.SavedPropertyNetIdBitSize,
                payload.SavedPropertyMapHash,
                payload.SavedPropertyNames,
                payload.ModelDbHashUsesDeterministicCache,
                payload.SavedPropertyNetIdUsesDeterministicSort,
                ContentModInventoryPayloadCodec.Compact(loadedMods),
                ContentModInventoryPayloadCodec.Compact(contentMods),
                payload.Progress,
                payload.RecentLogs);
        }

        private static StateDivergenceSupplementPayload? FromWirePayload(StateDivergenceSupplementPayloadV6? payload)
        {
            if (payload == null)
                return null;

            return new(
                payload.ChecksumId,
                payload.ChecksumValue,
                payload.SavedPropertyNetIdBitSize,
                payload.SavedPropertyMapHash,
                payload.SavedPropertyNames,
                payload.ModelDbHashUsesDeterministicCache,
                payload.SavedPropertyNetIdUsesDeterministicSort,
                ContentModInventoryPayloadCodec.Encode(ContentModInventoryPayloadCodec.Expand(payload.LoadedMods)),
                ContentModInventoryPayloadCodec.Encode(ContentModInventoryPayloadCodec.Expand(payload.ContentMods)),
                payload.Progress,
                payload.RecentLogs);
        }

        private static uint StableHash(IEnumerable<string> values)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var value in values)
                {
                    foreach (var ch in value)
                    {
                        hash ^= ch;
                        hash *= 16777619u;
                    }

                    hash ^= 0xffu;
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static StateDivergenceSupplementPayload? ConvertLegacyPayload(
            StateDivergenceSupplementPayloadV1? payload)
        {
            if (payload == null)
                return null;

            return new(
                payload.ChecksumId,
                payload.ChecksumValue,
                payload.SavedPropertyNetIdBitSize,
                payload.SavedPropertyMapHash,
                payload.SavedPropertyNames,
                null,
                null,
                null,
                payload.ContentMods == null ? null : ContentModInventoryPayloadCodec.Encode(payload.ContentMods),
                payload.Progress,
                null);
        }

        private sealed record StateDivergenceSupplementPayloadV1(
            uint ChecksumId,
            uint ChecksumValue,
            int SavedPropertyNetIdBitSize,
            uint SavedPropertyMapHash,
            IReadOnlyList<string> SavedPropertyNames,
            IReadOnlyList<ContentModInventoryEntry>? ContentMods,
            ProgressDiagnosticsSnapshot? Progress);
    }

    internal static class StateDivergenceSupplementStore
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<(uint Id, uint Checksum), Queue<StateDivergenceSupplementPayload>> Payloads =
            new();

        public static void Store(StateDivergenceSupplementPayload payload)
        {
            lock (SyncRoot)
            {
                var key = (payload.ChecksumId, payload.ChecksumValue);
                if (!Payloads.TryGetValue(key, out var queue))
                {
                    queue = new();
                    Payloads[key] = queue;
                }

                queue.Enqueue(payload);
            }
        }

        public static bool TryTake(NetChecksumData checksum, out StateDivergenceSupplementPayload payload)
        {
            lock (SyncRoot)
            {
                var key = (checksum.id, checksum.checksum);
                if (!Payloads.TryGetValue(key, out var queue) || queue.Count == 0)
                {
                    payload = null!;
                    return false;
                }

                payload = queue.Dequeue();
                if (queue.Count == 0)
                    Payloads.Remove(key);
                return true;
            }
        }
    }
}
