using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using STS2RitsuLib.Networking.MessageExtensions;

namespace STS2RitsuLib.Networking.JoinDiagnostics
{
    internal sealed record JoinDiagnosticsPayload(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods);

    internal sealed record JoinDiagnosticsModEntry(
        int Index,
        string Key,
        string Id,
        string Version,
        string Name,
        string Source);

    internal sealed record JoinPeerSnapshot(
        string GameVersion,
        uint ModelDbHash,
        string GameMode,
        string SessionState,
        IReadOnlyList<JoinDiagnosticsModEntry> GameplayMods);

    internal static class JoinDiagnosticsPayloadCodec
    {
        private const string ExtensionId = "ritsulib.joinDiagnostics";
        private const int PayloadVersion = 1;
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
                var payload = new JoinDiagnosticsPayload(
                    message.version,
                    message.idDatabaseHash,
                    message.gameMode.ToString(),
                    message.sessionState.ToString(),
                    CreateLocalModEntries());
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
                if (version != PayloadVersion)
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
            return new(
                ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? GitHelper.ShortCommitId ?? "UNKNOWN",
                ModelIdSerializationCache.Hash,
                string.Empty,
                string.Empty,
                CreateLocalModEntries());
        }

        public static JoinPeerSnapshot CreateHostSnapshot(
            InitialGameInfoMessage message,
            JoinDiagnosticsPayload? payload)
        {
            return new(
                message.version,
                message.idDatabaseHash,
                message.gameMode.ToString(),
                message.sessionState.ToString(),
                payload?.GameplayMods.Count > 0
                    ? payload.GameplayMods
                    : CreateFallbackModEntries(message.mods));
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
                        m.modSource.ToString());
                })
                .ToArray();
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
                return new JoinDiagnosticsModEntry(i, key, id, version, id, string.Empty);
            }).ToArray();
        }

        private static string BuildKey(string id, string version)
        {
            return id + "-" + version;
        }
    }
}
