using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Networking.MessageExtensions;

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
        string? ModelDbHashModeDetail);

    internal sealed record JoinDiagnosticsModEntry(
        int Index,
        string Key,
        string Id,
        string Version,
        string Name,
        string Source,
        ulong? WorkshopItemId);

    internal sealed record JoinPeerSnapshot(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods,
        IReadOnlyList<ContentModInventoryEntry> ContentMods,
        bool HasProcessedContentMods,
        bool ModelDbHashUsesDeterministicCache,
        string? ModelDbHashModeDetail);

    internal static class JoinDiagnosticsPayloadCodec
    {
        private const string ExtensionId = "ritsulib.joinDiagnostics";
        private const int PayloadVersion = 3;
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

            RitsuNetMessageTailExtensions.Register<InitialGameInfoMessage>(
                ExtensionId,
                PayloadVersion,
                SerializePayload,
                ReadPayload);
        }

        public static void Write(PacketWriter writer, InitialGameInfoMessage message)
        {
            EnsureRegistered();
            RitsuNetMessageTailExtensions.Write(writer, message);
        }

        public static void Read(PacketReader reader)
        {
            EnsureRegistered();
            RitsuNetMessageTailExtensions.Read<InitialGameInfoMessage>(reader);
        }

        private static string? SerializePayload(InitialGameInfoMessage message)
        {
            try
            {
                var cacheStatus = ModelIdSerializationCacheDynamicContentPatch.GetDeterministicCacheStatus();
                var payload = new JoinDiagnosticsPayload(
                    message.version,
                    message.idDatabaseHash,
                    message.gameMode.ToString(),
                    message.sessionState.ToString(),
                    CreateLocalModEntries(),
                    ContentModInventoryPayloadCodec.Encode(CreateLocalContentModEntries()),
                    cacheStatus.IsActive,
                    cacheStatus.Detail);
                return JsonSerializer.Serialize(payload, JsonOptions);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Failed to create payload: {ex.Message}");
                return null;
            }
        }

        private static void ReadPayload(int version, string json)
        {
            try
            {
                if (version == 1)
                {
                    JoinFailureDiagnosticsService.ObserveHostPayload(ConvertLegacyPayload(
                        JsonSerializer.Deserialize<JoinDiagnosticsPayloadV1>(json, JsonOptions)));
                    return;
                }

                if (version != 2 && version != PayloadVersion)
                {
                    RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Unsupported payload version: {version}");
                    return;
                }

                JoinFailureDiagnosticsService.ObserveHostPayload(
                    JsonSerializer.Deserialize<JoinDiagnosticsPayload>(json, JsonOptions));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[JoinDiagnostics] Failed to read payload: {ex.Message}");
            }
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
                cacheStatus.Detail);
        }

        public static JoinPeerSnapshot CreateHostSnapshot(
            InitialGameInfoMessage message,
            JoinDiagnosticsPayload? payload)
        {
            var contentMods = CreateHostContentModEntries(message, payload, out var hasProcessedContentMods);
            return new(
                message.version,
                message.idDatabaseHash,
                message.gameMode.ToString(),
                message.sessionState.ToString(),
                payload?.GameplayMods.Count > 0
                    ? payload.GameplayMods
                    : CreateFallbackModEntries(GetFallbackGameplayModKeys(message)),
                contentMods,
                hasProcessedContentMods,
                payload?.ModelDbHashUsesDeterministicCache ?? false,
                payload?.ModelDbHashModeDetail);
        }

        private static IReadOnlyList<JoinDiagnosticsModEntry> CreateLocalModEntries()
        {
            if (!ModManager.IsRunningModded())
                return [];

            return ModManager.GetLoadedMods()
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
                })
                .ToArray();
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
            return message.gameplayAffectingMods;
#else
            return message.mods;
#endif
        }

        private static IReadOnlyList<string>? GetFallbackContentModKeys(InitialGameInfoMessage message)
        {
#if STS2_AT_LEAST_0_107_1
            return MergeModKeys(message.gameplayAffectingMods, message.otherMods);
#else
            return message.mods;
#endif
        }

        private static IReadOnlyList<string>? MergeModKeys(
            IReadOnlyList<string>? gameplayAffectingMods,
            IReadOnlyList<string>? otherMods)
        {
            if (gameplayAffectingMods is not { Count: > 0 })
                return otherMods;

            if (otherMods is not { Count: > 0 })
                return gameplayAffectingMods;

            return gameplayAffectingMods.Concat(otherMods).ToArray();
        }

        private static IReadOnlyList<JoinDiagnosticsModEntry> CreateFallbackModEntries(IReadOnlyList<string>? keys)
        {
            if (keys == null || keys.Count == 0)
                return [];

            return keys.Select((key, i) =>
            {
                var split = key.LastIndexOf('-');
                var id = split > 0 ? key[..split] : key;
                var version = split > 0 && split < key.Length - 1 ? key[(split + 1)..] : string.Empty;
                return new JoinDiagnosticsModEntry(i, key, id, version, id, string.Empty, null);
            }).ToArray();
        }

        private static IReadOnlyList<ContentModInventoryEntry> CreateFallbackContentModEntries(
            IReadOnlyList<string>? keys)
        {
            if (keys == null || keys.Count == 0)
                return [];

            return keys.Select((key, i) =>
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
                    ContentModLoadOrderInventory.IsDependencyLibraryId(id));
            }).ToArray();
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
