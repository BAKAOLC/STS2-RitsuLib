using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Interop.Patches;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.Networking.MessageExtensions;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed record JoinDiagnosticsPayload(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods,
        string? ContentMods,
        bool ModelDbHashUsesDeterministicCache,
        string? ModelDbHashModeDetail,
        bool? SavedPropertyNetIdUsesDeterministicSort);

    internal sealed record JoinDiagnosticsModEntry(
        int Index,
        string Key,
        string Id,
        string Version,
        string Name,
        string Source,
        ulong? WorkshopItemId);

    internal sealed record JoinDiagnosticsPayloadV5(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods,
        IReadOnlyList<ContentModInventoryPayloadCodec.CompactEntry> ContentMods,
        bool ModelDbHashUsesDeterministicCache,
        string? ModelDbHashModeDetail,
        bool? SavedPropertyNetIdUsesDeterministicSort);

    internal sealed record JoinPeerSnapshot(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods,
        IReadOnlyList<ContentModInventoryEntry> ContentMods,
        bool HasProcessedContentMods,
        bool ModelDbHashUsesDeterministicCache,
        string? ModelDbHashModeDetail,
        bool? SavedPropertyNetIdUsesDeterministicSort);

    internal static class JoinDiagnosticsPayloadCodec
    {
        private const string ExtensionId = "ritsulib.joinDiagnostics";
        private const int PayloadVersion = 5;
        private const int MaxCompressedPayloadBytes = RitsuLibManagedNetActions.MaxPayloadBytes;
        private const int MaxDecompressedPayloadBytes = (int)RitsuLibSidecarWire.MaxPayloadBytes;
        private static readonly Lock RegistrationLock = new();
        private static int _registered;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static void EnsureRegistered()
        {
            if (Volatile.Read(ref _registered) != 0)
                return;

            lock (RegistrationLock)
            {
                if (_registered != 0)
                    return;

                RitsuNetMessageTailExtensions.RegisterBytes<InitialGameInfoMessage>(
                    ExtensionId,
                    PayloadVersion,
                    SerializePayload,
                    ReadPayload);
                Volatile.Write(ref _registered, 1);
            }
        }

        private static byte[]? SerializePayload(InitialGameInfoMessage message)
        {
            try
            {
                var cacheStatus = ModelIdSerializationCacheDynamicContentPatch.GetDeterministicCacheStatus();
                var payload = new JoinDiagnosticsPayloadV5(
                    GetGameVersion(message),
                    GetModelDbHash(message),
                    message.gameMode.ToString(),
                    message.sessionState.ToString(),
                    CreateLocalModEntries(),
                    ContentModInventoryPayloadCodec.Compact(CreateLocalContentModEntries()),
                    cacheStatus.IsActive,
                    cacheStatus.Detail,
                    SavedPropertiesTypeCacheInjectionPatch.UsesDeterministicNetIdTable);
                return EncodeCompressed(payload);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Failed to create payload: {ex.Message}");
                return null;
            }
        }

        private static void ReadPayload(int version, ReadOnlyMemory<byte> payload)
        {
            try
            {
                if (version == 1)
                {
                    var legacyJson = Encoding.UTF8.GetString(payload.Span);
                    JoinFailureDiagnosticsService.ObserveHostPayload(ConvertLegacyPayload(
                        JsonSerializer.Deserialize<JoinDiagnosticsPayloadV1>(legacyJson, JsonOptions)));
                    return;
                }

                if (version != 2 && version != 3 && version != 4 && version != PayloadVersion)
                {
                    RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Unsupported payload version: {version}");
                    return;
                }

                var json = version == PayloadVersion
                    ? DecodeCompressed(payload.Span)
                    : Encoding.UTF8.GetString(payload.Span);
                var parsed = version == PayloadVersion
                    ? FromWirePayload(JsonSerializer.Deserialize<JoinDiagnosticsPayloadV5>(json, JsonOptions))
                    : JsonSerializer.Deserialize<JoinDiagnosticsPayload>(json, JsonOptions);
                JoinFailureDiagnosticsService.ObserveHostPayload(parsed);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Failed to read payload: {ex.Message}");
            }
        }

        private static byte[] EncodeCompressed(JoinDiagnosticsPayloadV5 payload)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var data = Encoding.UTF8.GetBytes(json);
            if (data.Length > MaxDecompressedPayloadBytes)
                throw new InvalidDataException(
                    $"Join diagnostics payload exceeds {MaxDecompressedPayloadBytes} uncompressed bytes.");

            var compressed = RitsuLibSidecarCompression.BrotliCompress(data);
            if (compressed.Length > MaxCompressedPayloadBytes)
                throw new InvalidDataException(
                    $"Join diagnostics payload exceeds {MaxCompressedPayloadBytes} compressed bytes.");

            return compressed;
        }

        private static JoinDiagnosticsPayload? FromWirePayload(JoinDiagnosticsPayloadV5? payload)
        {
            if (payload == null)
                return null;

            return new(
                payload.GameVersion,
                payload.ModelDbHash,
                payload.GameMode,
                payload.SessionState,
                payload.GameplayMods,
                ContentModInventoryPayloadCodec.Encode(ContentModInventoryPayloadCodec.Expand(payload.ContentMods)),
                payload.ModelDbHashUsesDeterministicCache,
                payload.ModelDbHashModeDetail,
                payload.SavedPropertyNetIdUsesDeterministicSort);
        }

        private static string DecodeCompressed(ReadOnlySpan<byte> compressed)
        {
            return Encoding.UTF8.GetString(Unbrotli(compressed));
        }

        private static byte[] Unbrotli(ReadOnlySpan<byte> data)
        {
            if (!RitsuLibSidecarCompression.TryBrotliDecompress(data, out var decompressed))
                throw new InvalidDataException(
                    $"Join diagnostics Brotli data is invalid or exceeds {MaxDecompressedPayloadBytes} decompressed bytes.");

            return decompressed;
        }

        public static JoinPeerSnapshot CreateLocalSnapshot()
        {
            var cacheStatus = ModelIdSerializationCacheDynamicContentPatch.GetDeterministicCacheStatus();
            return new(
                ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? GitHelper.ShortCommitId ?? "UNKNOWN",
                ModelIdSerializationCache.Hash,
                string.Empty,
                string.Empty,
                CreateLocalModEntries(),
                CreateLocalContentModEntries(),
                true,
                cacheStatus.IsActive,
                cacheStatus.Detail,
                SavedPropertiesTypeCacheInjectionPatch.UsesDeterministicNetIdTable);
        }

        public static JoinPeerSnapshot CreateHostSnapshot(
            InitialGameInfoMessage message,
            JoinDiagnosticsPayload? payload)
        {
            var contentMods = CreateHostContentModEntries(message, payload, out var hasProcessedContentMods);
            return new(
                GetGameVersion(message),
                GetModelDbHash(message),
                message.gameMode.ToString(),
                message.sessionState.ToString(),
                payload?.GameplayMods.Count > 0
                    ? payload.GameplayMods
                    : CreateFallbackModEntries(GetFallbackGameplayModKeys(message)),
                contentMods,
                hasProcessedContentMods,
                payload?.ModelDbHashUsesDeterministicCache ?? false,
                payload?.ModelDbHashModeDetail,
                payload?.SavedPropertyNetIdUsesDeterministicSort);
        }

        private static IReadOnlyList<JoinDiagnosticsModEntry> CreateLocalModEntries()
        {
            if (!ModManager.IsRunningModded())
                return [];

            return
            [
                .. ModManager.GetLoadedMods()
                    .Where(m => m.manifest?.affectsGameplay ?? true)
                    .Select((m, i) =>
                    {
                        var manifest = m.manifest;
                        var id = manifest?.id ?? string.Empty;
                        var version = manifest?.version ?? string.Empty;
                        return new JoinDiagnosticsModEntry(
                            i,
                            BuildKey(id, version),
                            id,
                            version,
                            manifest?.name ?? id,
                            m.modSource.ToString(),
                            Sts2ModManagerCompat.TryGetWorkshopItemId(m));
                    }),
            ];
        }

        private static IReadOnlyList<ContentModInventoryEntry> CreateLocalContentModEntries()
        {
            return ContentModLoadOrderInventory.BuildRuntimeRelevantInventory();
        }

        private static IReadOnlyList<ContentModInventoryEntry> CreateHostContentModEntries(
            InitialGameInfoMessage message,
            JoinDiagnosticsPayload? payload,
            out bool hasProcessedContentMods)
        {
            hasProcessedContentMods = ContentModInventoryPayloadCodec.TryDecode(payload?.ContentMods, out var entries);
            return hasProcessedContentMods
                ? entries
                : CreateFallbackContentModEntries(GetFallbackContentModKeys(message));
        }

        private static IReadOnlyList<string>? GetFallbackGameplayModKeys(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_107_1
            return GetGameplayAffectingMods(message);
#else
            return message.mods;
#endif
        }

        private static IReadOnlyList<string>? GetFallbackContentModKeys(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_107_1
            return MergeModKeys(GetGameplayAffectingMods(message), GetOtherMods(message));
#else
            return message.mods;
#endif
        }

        private static string GetGameVersion(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_110_0
            return message.versionInfo.version;
#else
            return message.version;
#endif
        }

        private static uint GetModelDbHash(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_110_0
            return message.versionInfo.idDatabaseHash;
#else
            return message.idDatabaseHash;
#endif
        }

        private static IReadOnlyList<string>? GetGameplayAffectingMods(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_110_0
            return message.versionInfo.gameplayAffectingMods;
#else
            return message.gameplayAffectingMods;
#endif
        }

        private static IReadOnlyList<string>? GetOtherMods(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_110_0
            return message.versionInfo.otherMods;
#else
            return message.otherMods;
#endif
        }

        private static IReadOnlyList<string>? MergeModKeys(
            IReadOnlyList<string>? gameplayAffectingMods,
            IReadOnlyList<string>? otherMods)
        {
            if (gameplayAffectingMods is not { Count: > 0 })
                return otherMods;

            return otherMods is not { Count: > 0 } ? gameplayAffectingMods : [.. gameplayAffectingMods, .. otherMods];
        }

        private static IReadOnlyList<JoinDiagnosticsModEntry> CreateFallbackModEntries(IReadOnlyList<string>? keys)
        {
            if (keys == null || keys.Count == 0)
                return [];

            return
            [
                .. keys.Select((key, i) =>
                {
                    var split = key.LastIndexOf('-');
                    var id = split > 0 ? key[..split] : key;
                    var version = split > 0 && split < key.Length - 1 ? key[(split + 1)..] : string.Empty;
                    return new JoinDiagnosticsModEntry(i, key, id, version, id, string.Empty, null);
                }),
            ];
        }

        private static IReadOnlyList<ContentModInventoryEntry> CreateFallbackContentModEntries(
            IReadOnlyList<string>? keys)
        {
            if (keys == null || keys.Count == 0)
                return [];

            return
            [
                .. keys.Select((key, i) =>
                {
                    var split = key.LastIndexOf('-');
                    var id = split > 0 ? key[..split] : key;
                    var version = split > 0 && split < key.Length - 1 ? key[(split + 1)..] : string.Empty;
                    return new ContentModInventoryEntry(
                        i,
                        id,
                        version,
                        id,
                        string.Empty,
                        null,
                        true,
                        true,
                        ContentModLoadOrderInventory.IsDependencyLibraryId(id),
                        false);
                }),
            ];
        }

        private static string BuildKey(string id, string version)
        {
            return id + "-" + version;
        }

        private static JoinDiagnosticsPayload? ConvertLegacyPayload(JoinDiagnosticsPayloadV1? payload)
        {
            if (payload == null)
                return null;

            return new(
                payload.GameVersion,
                payload.ModelDbHash,
                payload.GameMode,
                payload.SessionState,
                payload.GameplayMods,
                payload.ContentMods == null ? null : ContentModInventoryPayloadCodec.Encode(payload.ContentMods),
                false,
                null,
                null);
        }

        private sealed record JoinDiagnosticsPayloadV1(
            string GameVersion,
            uint ModelDbHash,
            string GameMode,
            string SessionState,
            IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods,
            IReadOnlyList<ContentModInventoryEntry>? ContentMods);
    }
}
