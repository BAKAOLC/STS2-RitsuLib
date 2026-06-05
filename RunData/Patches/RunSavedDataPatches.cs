using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Test;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Networking.MessageExtensions;
using STS2RitsuLib.Patching.Models;
using GameMode = MegaCrit.Sts2.Core.Runs.GameMode;

namespace STS2RitsuLib.RunData.Patches
{
    internal static class RunSavedDataPatchHelpers
    {
        private const string TailExtensionId = "ritsulib.runSavedData";
        private const int PayloadVersion = 1;
        private static readonly AsyncLocal<Stack<RunSavedDataSaveRunCapture>?> ActiveSaveRunCaptures = new();

        public static string GetRunSavePath(RunSaveManager manager, bool isMultiplayer)
        {
            var fileName = isMultiplayer
                ? RunSaveManager.multiplayerRunSaveFileName
                : RunSaveManager.runSaveFileName;
            return RunSaveManager.GetRunSavePath(
                RunSavedDataRunSaveManagerAccess.ProfileIdProvider(manager).CurrentProfileId,
                fileName);
        }

        public static void AttachDocumentFromCurrentFile(RunSaveManager manager, SerializableRun? save,
            bool isMultiplayer)
        {
            if (save == null)
                return;

            try
            {
                var json = RunSavedDataRunSaveManagerAccess.SaveStore(manager)
                    .ReadFile(GetRunSavePath(manager, isMultiplayer));
                RunSavedDataRegistry.AttachDocumentFromJson(save, json);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to read run extension data: {ex.Message}");
            }
        }

        public static RunSavedDataSaveRunCapture BeginSaveRunCapture(
            RunSaveManager manager,
            SerializableRun? save = null,
            bool? isMultiplayer = null)
        {
            var capture = new RunSavedDataSaveRunCapture(
                manager,
                isMultiplayer ?? TryGetCurrentRunIsMultiplayerHost())
            {
                Save = save,
            };
            (ActiveSaveRunCaptures.Value ??= []).Push(capture);
            return capture;
        }

        public static void CaptureCurrentSave(SerializableRun save)
        {
            if (ActiveSaveRunCaptures.Value is { Count: > 0 } captures)
                captures.Peek().Save = save;
        }

        public static Task EndSaveRunCaptureAfter(Task originalTask, RunSavedDataSaveRunCapture capture)
        {
            return EndSaveRunCaptureAfterAsync(originalTask, capture);
        }

        public static bool TryInjectCurrentSaveBytes(string path, byte[] bytes, out byte[] injectedBytes)
        {
            injectedBytes = bytes;
            var capture = ActiveSaveRunCaptures.Value is { Count: > 0 } captures ? captures.Peek() : null;
            if (capture is not { Save: { } save } ||
                !RunSavedDataRegistry.HasDocument(save) ||
                !string.Equals(path, capture.SavePath, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var json = Encoding.UTF8.GetString(bytes);
                var injectedJson = RunSavedDataRegistry.InjectIntoJson(json, save);
                if (ReferenceEquals(injectedJson, json))
                    return false;

                injectedBytes = Encoding.UTF8.GetBytes(injectedJson);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to inject run extension data: {ex.Message}");
                return false;
            }
        }

        private static async Task EndSaveRunCaptureAfterAsync(Task originalTask, RunSavedDataSaveRunCapture capture)
        {
            ArgumentNullException.ThrowIfNull(originalTask);

            try
            {
                await originalTask;
            }
            finally
            {
                EndSaveRunCapture(capture);
            }
        }

        private static void EndSaveRunCapture(RunSavedDataSaveRunCapture capture)
        {
            var captures = ActiveSaveRunCaptures.Value;
            if (captures is not { Count: > 0 })
                return;

            if (ReferenceEquals(captures.Peek(), capture))
                captures.Pop();

            if (captures.Count == 0)
                ActiveSaveRunCaptures.Value = null;
        }

        private static bool TryGetCurrentRunIsMultiplayerHost()
        {
            try
            {
                return RunManager.Instance.ShouldSave && RunManager.Instance.NetService.Type == NetGameType.Host;
            }
            catch
            {
                return false;
            }
        }

        public static void WritePayload(PacketWriter writer, string? payload)
        {
            RitsuNetMessageTailExtensions.WriteLegacySingle(writer, TailExtensionId, PayloadVersion, payload);
        }

        public static string? PrepareNewRunPayload(StartRunLobby lobby, string seed,
            IReadOnlyList<ModifierModel> modifiers)
        {
            try
            {
                var rng = new Rng((uint)StringHelper.GetDeterministicHashCode(seed));
                var unlockState = RunSavedDataStartRunLobbyAccess.GetUnlockState(lobby);
                var acts = ActModel.GetRandomList(rng, unlockState, lobby.NetService.Type.IsMultiplayer()).ToList();
                if (RunSavedDataStartRunLobbyAccess.GetAct(lobby.Act1) is { } forcedAct)
                    acts[0] = forcedAct;

                var players = new List<Player>();
                foreach (var lobbyPlayer in lobby.Players)
                {
                    var character = lobbyPlayer.character;
                    if (character is RandomCharacter)
                        character = rng.NextItem(ModelDb.AllCharacters);

                    players.Add(Player.CreateForNewRun(
                        character ?? throw new InvalidOperationException("Random character resolution produced null."),
                        UnlockState.FromSerializable(lobbyPlayer.unlockState),
                        lobbyPlayer.id));
                }

                RunSavedDataLobby.PublishStagingEvent(lobby, RunSavedDataLobbyStagingReason.Committing);

                var runState = RunState.CreateForNewRun(
                    players,
                    acts.Select(act => act.ToMutable()).ToList(),
                    modifiers,
                    lobby.GameMode,
                    lobby.Ascension,
                    seed);

                RunSavedDataLobby.CommitSession(lobby, runState);

                RitsuLibFramework.PublishLifecycleEvent(
                    new RunSavedDataPreparingEvent(runState, lobby.NetService.Type.IsMultiplayer(),
                        DateTimeOffset.UtcNow),
                    nameof(RunSavedDataPreparingEvent));

                return RunSavedDataRegistry.BuildPayload(runState);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[RunSavedData] Failed to prepare new-run payload: {ex.Message}");
                return null;
            }
        }

        public static string? TryReadPayload(PacketReader reader)
        {
            return RitsuNetMessageTailExtensions.TryReadLegacySingle(reader, TailExtensionId, PayloadVersion);
        }
    }

    internal sealed class RunSavedDataSaveRunCapture(RunSaveManager manager, bool isMultiplayer)
    {
        public RunSaveManager Manager { get; } = manager;

        public bool IsMultiplayer { get; } = isMultiplayer;

        public string SavePath => RunSavedDataPatchHelpers.GetRunSavePath(Manager, IsMultiplayer);

        public SerializableRun? Save { get; set; }
    }

    internal static class RunSavedDataRunSaveManagerAccess
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_saveStore")]
        internal static extern ref readonly ISaveStore SaveStore(RunSaveManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_forceSynchronous")]
        internal static extern ref readonly bool ForceSynchronous(RunSaveManager manager);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_profileIdProvider")]
        internal static extern ref readonly IProfileIdProvider ProfileIdProvider(RunSaveManager manager);
    }

    internal static class RunSavedDataStartRunLobbyAccess
    {
        private static readonly ConditionalWeakTable<INetGameService, StartRunLobby> LobbyByNetService = [];

        private static readonly Func<string, ActModel?> GetActAccessor =
            AccessTools.MethodDelegate<Func<string, ActModel?>>(
                AccessTools.DeclaredMethod(typeof(StartRunLobby), "GetAct", [typeof(string)]));

        internal static void Track(StartRunLobby lobby)
        {
            LobbyByNetService.Remove(lobby.NetService);
            LobbyByNetService.Add(lobby.NetService, lobby);
        }

        internal static void Untrack(StartRunLobby lobby)
        {
            LobbyByNetService.Remove(lobby.NetService);
            RunSavedDataLobbyRuntime.RemoveSession(lobby);
        }

        internal static StartRunLobby? TryGetCurrentLobby()
        {
            var netService = RunManager.Instance.NetService;
            return netService != null && LobbyByNetService.TryGetValue(netService, out var lobby)
                ? lobby
                : null;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetUnlockState")]
        internal static extern UnlockState GetUnlockState(StartRunLobby lobby);

        internal static ActModel? GetAct(string act1Key)
        {
            return GetActAccessor(act1Key);
        }
    }

    internal sealed class RunSavedDataStartRunLobbyCtorPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_start_run_lobby_ctor";
        public static string Description => "Track active start-run lobby sessions for RunSavedData staging";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(StartRunLobby),
                    ".ctor",
                    [typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int)],
                    MethodType.Constructor),
                new(
                    typeof(StartRunLobby),
                    ".ctor",
                    [
                        typeof(GameMode),
                        typeof(INetGameService),
                        typeof(IStartRunLobbyListener),
                        typeof(TimeServerResult),
                        typeof(int),
                    ],
                    MethodType.Constructor),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(object __instance)
        {
            if (__instance is StartRunLobby lobby)
                RunSavedDataStartRunLobbyAccess.Track(lobby);
        }
    }

    internal sealed class RunSavedDataStartRunLobbyCleanUpPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_start_run_lobby_cleanup";
        public static string Description => "Release start-run lobby RunSavedData staging sessions";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
#if !STS2_AT_LEAST_0_107_0
            return [new(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp), [typeof(bool)])];
#else
            return [new(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp), [typeof(bool), typeof(NetError)])];
#endif
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(StartRunLobby __instance)
        {
            RunSavedDataStartRunLobbyAccess.Untrack(__instance);
        }
    }

    internal sealed class RunSavedDataLoadRunSavePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_load_run_save";
        public static string Description => "Attach RunSavedData document after loading single-player run saves";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave), Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RunSaveManager __instance, ReadSaveResult<SerializableRun> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result.Success)
                RunSavedDataPatchHelpers.AttachDocumentFromCurrentFile(__instance, __result.SaveData, false);
        }
    }

    internal sealed class RunSavedDataLoadMultiplayerRunSavePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_load_multiplayer_run_save";
        public static string Description => "Attach RunSavedData document after loading multiplayer run saves";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunSaveManager), "LoadMultiplayerRunSave", Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RunSaveManager __instance, ReadSaveResult<SerializableRun> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result.Success)
                RunSavedDataPatchHelpers.AttachDocumentFromCurrentFile(__instance, __result.SaveData, true);
        }
    }

    internal sealed class RunSavedDataCanonicalizeSavePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_canonicalize_save";
        public static string Description => "Preserve RunSavedData document across multiplayer save canonicalization";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.CanonicalizeSave), [typeof(SerializableRun), typeof(ulong)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(SerializableRun save, SerializableRun __result)
        {
            RunSavedDataRegistry.MergeDocuments(__result, save);
        }
    }

    internal sealed class RunSavedDataFromSerializablePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_from_serializable";
        public static string Description => "Import RunSavedData after RunState deserialization";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunState), nameof(RunState.FromSerializable), [typeof(SerializableRun)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(SerializableRun save, RunState __result)
        {
            RunSavedDataRegistry.Import(save, __result);
        }
    }

    internal sealed class RunSavedDataToSavePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_to_save";
        public static string Description => "Export RunSavedData after RunManager builds SerializableRun";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), nameof(RunManager.ToSave), [typeof(AbstractRoom)])];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RunManager __instance, SerializableRun __result)
            // ReSharper restore InconsistentNaming
        {
            RunSavedDataRegistry.AttachDocument(
                __result,
                RunSavedDataRegistry.BuildDocumentFromRun(__instance.State));
            RunSavedDataPatchHelpers.CaptureCurrentSave(__result);
        }
    }

    internal sealed class RunSavedDataSaveStoreWritePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_save_store_write";
        public static string Description => "Inject RunSavedData into run save bytes before save store writes";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(GodotFileIo), nameof(GodotFileIo.WriteFile), [typeof(string), typeof(byte[])]),
                new(typeof(GodotFileIo), nameof(GodotFileIo.WriteFileAsync), [typeof(string), typeof(byte[])]),
                new(typeof(CloudSaveStore), nameof(CloudSaveStore.WriteFile), [typeof(string), typeof(byte[])]),
                new(typeof(CloudSaveStore), nameof(CloudSaveStore.WriteFileAsync), [typeof(string), typeof(byte[])]),
                new(typeof(MockGodotFileIo), nameof(MockGodotFileIo.WriteFile), [typeof(string), typeof(byte[])]),
                new(typeof(MockGodotFileIo), nameof(MockGodotFileIo.WriteFileAsync), [typeof(string), typeof(byte[])]),
            ];
        }

        public static void Prefix(string path, ref byte[] bytes)
        {
            if (RunSavedDataPatchHelpers.TryInjectCurrentSaveBytes(path, bytes, out var injectedBytes))
                bytes = injectedBytes;
        }
    }

#if STS2_AT_LEAST_0_104_0
    internal sealed class RunSavedDataSaveRunPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_save_run";
        public static string Description => "Write RunSavedData into current run JSON saves";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunSaveManager), nameof(RunSaveManager.SaveRun), [typeof(SerializableRun), typeof(bool)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Prefix(RunSaveManager __instance, SerializableRun save, bool isMultiplayer,
                out RunSavedDataSaveRunCapture __state)
            // ReSharper restore InconsistentNaming
        {
            __state = RunSavedDataPatchHelpers.BeginSaveRunCapture(__instance, save, isMultiplayer);
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RunSavedDataSaveRunCapture __state, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            __result = RunSavedDataPatchHelpers.EndSaveRunCaptureAfter(__result, __state);
        }
    }
#else
    internal sealed class RunSavedDataLegacySaveRunPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_save_run_legacy";
        public static string Description => "Write RunSavedData into legacy current run JSON saves";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunSaveManager), nameof(RunSaveManager.SaveRun), [typeof(AbstractRoom)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(RunSaveManager __instance, out RunSavedDataSaveRunCapture __state)
        {
            __state = RunSavedDataPatchHelpers.BeginSaveRunCapture(__instance);
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(RunSavedDataSaveRunCapture __state, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            __result = RunSavedDataPatchHelpers.EndSaveRunCaptureAfter(__result, __state);
        }
    }
#endif

    internal static class RunSavedDataLobbyContributionState
    {
        internal static string? PendingPayload { get; private set; }

        internal static void SetPending(string? payload)
        {
            PendingPayload = payload;
        }

        internal static bool TryConsume(out string? payload)
        {
            payload = PendingPayload;
            PendingPayload = null;
            return !string.IsNullOrWhiteSpace(payload);
        }
    }

    internal static class RunSavedDataLobbyBeginRunMessageState
    {
        internal static string? PendingNewRunPayload { get; private set; }
        internal static string? PreparedNewRunPayload { get; set; }

        internal static void SetPendingPayload(string? payload)
        {
            PendingNewRunPayload = payload;
        }

        internal static string? ConsumePendingPayload()
        {
            var payload = PendingNewRunPayload;
            PendingNewRunPayload = null;
            return payload;
        }
    }

    internal sealed class RunSavedDataLobbyBeginRunMessageSerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_begin_run_message_serialize";
        public static string Description => "Synchronize RunSavedData in new-run lobby begin messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Serialize), [typeof(PacketWriter)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(LobbyBeginRunMessage __instance, PacketWriter writer)
        {
            RunSavedDataPatchHelpers.WritePayload(writer, RunSavedDataLobbyBeginRunMessageState.PreparedNewRunPayload);
        }
    }

    internal sealed class RunSavedDataLobbyBeginRunMessageDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_begin_run_message_deserialize";
        public static string Description => "Synchronize RunSavedData in new-run lobby begin messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
                [new(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Deserialize), [typeof(PacketReader)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PacketReader reader)
        {
            RunSavedDataLobbyBeginRunMessageState.SetPendingPayload(RunSavedDataPatchHelpers.TryReadPayload(reader));
        }
    }

    internal sealed class RunSavedDataLobbyPlayerSetReadySerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_set_ready_serialize";
        public static string Description => "Attach lobby RunSavedData contributions to ready messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LobbyPlayerSetReadyMessage), nameof(LobbyPlayerSetReadyMessage.Serialize),
                    [typeof(PacketWriter)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PacketWriter writer)
        {
            RunSavedDataLobbySync.AppendVanillaTrailer(RunSavedDataStartRunLobbyAccess.TryGetCurrentLobby(), writer);
        }
    }

    internal sealed class RunSavedDataLobbyPlayerSetReadyDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_set_ready_deserialize";
        public static string Description => "Read lobby RunSavedData contributions from ready messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LobbyPlayerSetReadyMessage), nameof(LobbyPlayerSetReadyMessage.Deserialize),
                    [typeof(PacketReader)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PacketReader reader)
        {
            RunSavedDataLobbyContributionState.SetPending(RunSavedDataPatchHelpers.TryReadPayload(reader));
        }
    }

    internal sealed class RunSavedDataLobbyPlayerSetReadyHandlerPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_set_ready_handler";
        public static string Description => "Merge lobby RunSavedData contributions on the host";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(StartRunLobby), "HandlePlayerReadyMessage",
                    [typeof(LobbyPlayerSetReadyMessage), typeof(ulong)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(StartRunLobby __instance, ulong senderId)
        {
            RunSavedDataLobbySync.TryMergeVanillaTrailer(__instance, senderId);
        }
    }

    internal sealed class RunSavedDataLobbyPlayerChangedCharacterSerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_changed_character_serialize";
        public static string Description => "Attach lobby RunSavedData contributions to character change messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LobbyPlayerChangedCharacterMessage), nameof(LobbyPlayerChangedCharacterMessage.Serialize),
                    [typeof(PacketWriter)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PacketWriter writer)
        {
            RunSavedDataLobbySync.AppendVanillaTrailer(RunSavedDataStartRunLobbyAccess.TryGetCurrentLobby(), writer);
        }
    }

    internal sealed class RunSavedDataLobbyPlayerChangedCharacterDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_changed_character_deserialize";
        public static string Description => "Read lobby RunSavedData contributions from character change messages";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LobbyPlayerChangedCharacterMessage), nameof(LobbyPlayerChangedCharacterMessage.Deserialize),
                    [typeof(PacketReader)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(PacketReader reader)
        {
            RunSavedDataLobbyContributionState.SetPending(RunSavedDataPatchHelpers.TryReadPayload(reader));
        }
    }

    internal sealed class RunSavedDataLobbyPlayerChangedCharacterHandlerPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_changed_character_handler";
        public static string Description => "Merge lobby RunSavedData contributions after character changes";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(StartRunLobby), "HandleLobbyPlayerChangedCharacterMessage",
                    [typeof(LobbyPlayerChangedCharacterMessage), typeof(ulong)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(StartRunLobby __instance, ulong senderId)
        {
            RunSavedDataLobbySync.TryMergeVanillaTrailer(__instance, senderId);
        }
    }

    internal sealed class RunSavedDataLobbyPlayerJoinedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_lobby_player_joined";
        public static string Description => "Publish lobby staging events when players join";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(StartRunLobby), "TryAddPlayerInFirstAvailableSlot",
                    [typeof(SerializableUnlockState), typeof(int), typeof(ulong)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(StartRunLobby __instance, LobbyPlayer? __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result == null || __instance.NetService.Type != NetGameType.Host)
                return;

            RunSavedDataLobby.PublishStagingEvent(__instance, RunSavedDataLobbyStagingReason.PlayerJoined);
        }
    }

    internal sealed class RunSavedDataPrepareNewRunPayloadPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_prepare_new_run_payload";
        public static string Description => "Prepare RunSavedData payload before new multiplayer runs begin";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "BeginRunForAllPlayers", [typeof(string), typeof(List<ModifierModel>)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(StartRunLobby __instance, string seed, List<ModifierModel> modifiers)
        {
            RunSavedDataLobbyBeginRunMessageState.PreparedNewRunPayload =
                RunSavedDataPatchHelpers.PrepareNewRunPayload(__instance, seed, modifiers);
        }
    }

    internal sealed class RunSavedDataInitializeNewRunPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_initialize_new_run";
        public static string Description => "Import new-run RunSavedData payload before run initialization";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), "InitializeNewRun")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(RunManager __instance)
        {
            var payload = RunSavedDataLobbyBeginRunMessageState.ConsumePendingPayload() ??
                          RunSavedDataLobbyBeginRunMessageState.PreparedNewRunPayload;
            if (!string.IsNullOrWhiteSpace(payload) && __instance.State != null)
            {
                RunSavedDataRegistry.ImportPayloadIntoRun(__instance.State, payload);
                if (__instance.NetService?.Type.IsMultiplayer() == true)
                    RitsuLibFramework.PublishLifecycleEvent(
                        new RunSavedDataPreparingEvent(__instance.State, true, DateTimeOffset.UtcNow),
                        nameof(RunSavedDataPreparingEvent));

                return;
            }

            if (__instance.State == null || __instance.NetService?.Type != NetGameType.Singleplayer)
                return;

            RitsuLibFramework.PublishLifecycleEvent(
                new RunSavedDataPreparingEvent(__instance.State, false, DateTimeOffset.UtcNow),
                nameof(RunSavedDataPreparingEvent));
        }
    }

    internal sealed class RunSavedDataClientLoadJoinResponseMessageSerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_client_load_join_response_serialize";
        public static string Description => "Synchronize RunSavedData in loaded-run lobby responses";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ClientLoadJoinResponseMessage), nameof(ClientLoadJoinResponseMessage.Serialize),
                    [typeof(PacketWriter)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ClientLoadJoinResponseMessage __instance, PacketWriter writer)
        {
            var payload = RunSavedDataRegistry.BuildPayloadFromSerializable(__instance.serializableRun);
            RunSavedDataPatchHelpers.WritePayload(writer, payload);
        }
    }

    internal sealed class RunSavedDataClientLoadJoinResponseMessageDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_client_load_join_response_deserialize";
        public static string Description => "Synchronize RunSavedData in loaded-run lobby responses";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ClientLoadJoinResponseMessage), nameof(ClientLoadJoinResponseMessage.Deserialize),
                    [typeof(PacketReader)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ClientLoadJoinResponseMessage __instance, PacketReader reader)
        {
            var payload = RunSavedDataPatchHelpers.TryReadPayload(reader);
            if (!string.IsNullOrWhiteSpace(payload))
                RunSavedDataRegistry.AttachDocumentFromJson(__instance.serializableRun, payload);
        }
    }

    internal sealed class RunSavedDataClientRejoinResponseMessageSerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_client_rejoin_response_serialize";
        public static string Description => "Synchronize RunSavedData in rejoin responses";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ClientRejoinResponseMessage), nameof(ClientRejoinResponseMessage.Serialize),
                    [typeof(PacketWriter)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ClientRejoinResponseMessage __instance, PacketWriter writer)
        {
            var payload = RunSavedDataRegistry.BuildPayloadFromSerializable(__instance.serializableRun);
            RunSavedDataPatchHelpers.WritePayload(writer, payload);
        }
    }

    internal sealed class RunSavedDataClientRejoinResponseMessageDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_client_rejoin_response_deserialize";
        public static string Description => "Synchronize RunSavedData in rejoin responses";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ClientRejoinResponseMessage), nameof(ClientRejoinResponseMessage.Deserialize),
                    [typeof(PacketReader)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(ClientRejoinResponseMessage __instance, PacketReader reader)
        {
            var payload = RunSavedDataPatchHelpers.TryReadPayload(reader);
            if (!string.IsNullOrWhiteSpace(payload))
                RunSavedDataRegistry.AttachDocumentFromJson(__instance.serializableRun, payload);
        }
    }

    internal sealed class RunSavedDataCombatReplaySerializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_combat_replay_serialize";
        public static string Description => "Preserve RunSavedData in combat replay initial state";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatReplay), nameof(CombatReplay.Serialize), [typeof(PacketWriter)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(CombatReplay __instance, PacketWriter writer)
        {
            var payload = RunSavedDataRegistry.BuildPayloadFromSerializable(__instance.serializableRun);
            RunSavedDataPatchHelpers.WritePayload(writer, payload);
        }
    }

    internal sealed class RunSavedDataCombatReplayDeserializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_run_saved_data_combat_replay_deserialize";
        public static string Description => "Preserve RunSavedData in combat replay initial state";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatReplay), nameof(CombatReplay.Deserialize), [typeof(PacketReader)])];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(CombatReplay __instance, PacketReader reader)
        {
            var payload = RunSavedDataPatchHelpers.TryReadPayload(reader);
            if (!string.IsNullOrWhiteSpace(payload))
                RunSavedDataRegistry.AttachDocumentFromJson(__instance.serializableRun, payload);
        }
    }
}
