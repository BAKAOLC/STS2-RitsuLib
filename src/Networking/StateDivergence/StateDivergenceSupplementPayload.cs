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
        ProgressDiagnosticsSnapshot? Progress);

    internal static class StateDivergenceSupplementPayloadCodec
    {
        private const string ExtensionId = "ritsulib.stateDivergence";
        private const int PayloadVersion = 4;
        private static int _registered;

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

            RitsuNetMessageTailExtensions.Register<StateDivergenceMessage>(
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
                ProgressDiagnosticsSnapshot.CreateLocal());
        }

        private static string? SerializePayload(StateDivergenceMessage message)
        {
            try
            {
                var payload = CreateLocalSnapshot(message.senderChecksum);
                var json = JsonSerializer.Serialize(payload, JsonOptions);
                return Convert.ToBase64String(Gzip(Encoding.UTF8.GetBytes(json)));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] Failed to create supplement payload: {ex.Message}");
                return null;
            }
        }

        private static void ReadPayload(int version, string encoded)
        {
            try
            {
                if (version != 1 && version != 2 && version != 3 && version != PayloadVersion)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[State divergence diagnostics] Unsupported supplement payload version: {version}");
                    return;
                }

                var bytes = Gunzip(Convert.FromBase64String(encoded));
                var json = Encoding.UTF8.GetString(bytes);
                var payload = version == 1
                    ? ConvertLegacyPayload(JsonSerializer.Deserialize<StateDivergenceSupplementPayloadV1>(
                        json,
                        JsonOptions))
                    : JsonSerializer.Deserialize<StateDivergenceSupplementPayload>(json, JsonOptions);
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
                payload.Progress);
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
