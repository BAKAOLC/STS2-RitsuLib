using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Compatibility: replaces multiplayer load-screen <c>BeginRun</c> so starting a run is blocked when any player
    ///     references an unregistered character, without deleting saves; otherwise mirrors vanilla load flow.
    /// </summary>
    public class NMultiplayerLoadGameScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, LoadRunLobby> RunLobbyRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, LoadRunLobby>("_runLobby");

        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, NConfirmButton> ConfirmRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, NConfirmButton>("_confirmButton");

        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, NBackButton> UnreadyRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, NBackButton>("_unreadyButton");

        /// <inheritdoc />
        public static string PatchId => "nmultiplayer_load_game_begin_run_missing_character";

        /// <inheritdoc />
        public static string Description =>
            "Multiplayer load screen: block StartRun when a saved player character is not registered; no save deletion";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerLoadGameScreen), "BeginRun")];
        }

        /// <summary>
        ///     Harmony prefix: skips vanilla <c>BeginRun</c>, disables confirm/back, and runs async validation and load.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NMultiplayerLoadGameScreen __instance)
        {
            NAudioManager.Instance?.StopMusic();
            ConfirmRef(__instance).Disable();
            UnreadyRef(__instance).Disable();
            TaskHelper.RunSafely(StartRunAsync(__instance));
            return false;
        }

        private static async Task StartRunAsync(NMultiplayerLoadGameScreen screen)
        {
            var lobby = RunLobbyRef(screen);
            Log.Info("Loading a multiplayer run. Players: " + string.Join(",", lobby.ConnectedPlayerIds) + ".");
            if (RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
            {
                RitsuLibFramework.Logger.Warn(
                    "[Saves] Multiplayer run load blocked: missing CharacterModel; run save not deleted.");
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                ConfirmRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var serializablePlayer = lobby.Run.Players.First(p => p.NetId == lobby.NetService.NetId);
            var cid = serializablePlayer.CharacterId;
            if (cid == null)
            {
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                ConfirmRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var character = ModelDb.GetById<CharacterModel>(cid);
            var game = NGame.Instance;
            if (game == null)
                throw new InvalidOperationException("NGame.Instance is null during multiplayer run load.");

            SfxCmd.Play(character.CharacterTransitionSfx);
            await game.Transition.FadeOut(0.8f, character.CharacterSelectTransitionPath);
            var runState = RunState.FromSerializable(lobby.Run);
#if STS2_V_0_103_2
            RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#else
            await RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#endif
            await game.LoadRun(runState, lobby.Run.PreFinishedRoom);
            CleanUpLobby(screen, false);
            await game.Transition.FadeIn();
        }

        private static void CleanUpLobby(NMultiplayerLoadGameScreen screen, bool disconnectSession)
        {
            var m = AccessTools.DeclaredMethod(typeof(NMultiplayerLoadGameScreen), "CleanUpLobby");
            m.Invoke(screen, [disconnectSession]);
        }
    }

    /// <summary>
    ///     Compatibility: replaces custom-run load-screen <c>BeginRun</c> so starting a run is blocked when any player
    ///     references an unregistered character, without deleting saves; otherwise mirrors vanilla load flow.
    /// </summary>
    public class NCustomRunLoadScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, LoadRunLobby> LobbyRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, LoadRunLobby>("_lobby");

        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, NConfirmButton> ConfirmRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, NConfirmButton>("_confirmButton");

        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, NBackButton> UnreadyRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, NBackButton>("_unreadyButton");

        /// <inheritdoc />
        public static string PatchId => "ncustom_run_load_begin_run_missing_character";

        /// <inheritdoc />
        public static string Description =>
            "Custom run load screen: block StartRun when a saved player character is not registered; no save deletion";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCustomRunLoadScreen), "BeginRun")];
        }

        /// <summary>
        ///     Harmony prefix: skips vanilla <c>BeginRun</c>, disables confirm/back, and runs async validation and load.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NCustomRunLoadScreen __instance)
        {
            NAudioManager.Instance?.StopMusic();
            ConfirmRef(__instance).Disable();
            UnreadyRef(__instance).Disable();
            TaskHelper.RunSafely(StartRunAsync(__instance));
            return false;
        }

        private static async Task StartRunAsync(NCustomRunLoadScreen screen)
        {
            var lobby = LobbyRef(screen);
            Log.Info("Loading a custom multiplayer run. Players: " + string.Join(",", lobby.ConnectedPlayerIds) + ".");
            if (RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
            {
                RitsuLibFramework.Logger.Warn(
                    "[Saves] Custom run load blocked: missing CharacterModel; run save not deleted.");
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                ConfirmRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var serializablePlayer = lobby.Run.Players.First(p => p.NetId == lobby.NetService.NetId);
            var cid = serializablePlayer.CharacterId;
            if (cid == null)
            {
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                ConfirmRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var character = ModelDb.GetById<CharacterModel>(cid);
            var game = NGame.Instance;
            if (game == null)
                throw new InvalidOperationException("NGame.Instance is null during custom run load.");

            SfxCmd.Play(character.CharacterTransitionSfx);
            await game.Transition.FadeOut(0.8f, character.CharacterSelectTransitionPath);
            var runState = RunState.FromSerializable(lobby.Run);
#if STS2_V_0_103_2
            RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#else
            await RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#endif
            await game.LoadRun(runState, lobby.Run.PreFinishedRoom);
            CleanUpLobby(screen, false);
            await game.Transition.FadeIn();
        }

        private static void CleanUpLobby(NCustomRunLoadScreen screen, bool disconnectSession)
        {
            AccessTools.DeclaredMethod(typeof(NCustomRunLoadScreen), "CleanUpLobby")
                .Invoke(screen, [disconnectSession]);
        }
    }

    /// <summary>
    ///     Compatibility: replaces daily-run load-screen <c>BeginRun</c> so starting a run is blocked when any player
    ///     references an unregistered character, without deleting saves; otherwise mirrors vanilla load flow.
    /// </summary>
    public class NDailyRunLoadScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NDailyRunLoadScreen, LoadRunLobby?> LobbyRef =
            AccessTools.FieldRefAccess<NDailyRunLoadScreen, LoadRunLobby?>("_lobby");

        private static readonly AccessTools.FieldRef<NDailyRunLoadScreen, NConfirmButton> EmbarkRef =
            AccessTools.FieldRefAccess<NDailyRunLoadScreen, NConfirmButton>("_embarkButton");

        private static readonly AccessTools.FieldRef<NDailyRunLoadScreen, NBackButton> UnreadyRef =
            AccessTools.FieldRefAccess<NDailyRunLoadScreen, NBackButton>("_unreadyButton");

        /// <inheritdoc />
        public static string PatchId => "ndaily_run_load_begin_run_missing_character";

        /// <inheritdoc />
        public static string Description =>
            "Daily run load screen: block StartRun when a saved player character is not registered; no save deletion";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDailyRunLoadScreen), "BeginRun")];
        }

        /// <summary>
        ///     Harmony prefix: skips vanilla <c>BeginRun</c>, disables embark/back, and runs async validation and load.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NDailyRunLoadScreen __instance)
        {
            NAudioManager.Instance?.StopMusic();
            EmbarkRef(__instance).Disable();
            UnreadyRef(__instance).Disable();
            TaskHelper.RunSafely(StartRunAsync(__instance));
            return false;
        }

        private static async Task StartRunAsync(NDailyRunLoadScreen screen)
        {
            var lobby = LobbyRef(screen);
            if (lobby == null)
                return;

            Log.Info("Loading a multiplayer run. Players: " + string.Join(",", lobby.ConnectedPlayerIds) + ".");
            if (RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
            {
                RitsuLibFramework.Logger.Warn(
                    "[Saves] Daily run load blocked: missing CharacterModel; run save not deleted.");
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                EmbarkRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var serializablePlayer = lobby.Run.Players.First(p => p.NetId == lobby.NetService.NetId);
            var cid = serializablePlayer.CharacterId;
            if (cid == null)
            {
                RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
                EmbarkRef(screen).Enable();
                UnreadyRef(screen).Enable();
                return;
            }

            var character = ModelDb.GetById<CharacterModel>(cid);
            var game = NGame.Instance;
            if (game == null)
                throw new InvalidOperationException("NGame.Instance is null during daily run load.");

            SfxCmd.Play(character.CharacterTransitionSfx);
            await game.Transition.FadeOut(0.8f, character.CharacterSelectTransitionPath);
            var runState = RunState.FromSerializable(lobby.Run);
#if STS2_V_0_103_2
            RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#else
            await RunManager.Instance.SetUpSavedMultiPlayer(runState, lobby);
#endif
            await game.LoadRun(runState, lobby.Run.PreFinishedRoom);
            CleanUpLobby(screen, false);
            await game.Transition.FadeIn();
        }

        private static void CleanUpLobby(NDailyRunLoadScreen screen, bool disconnectSession)
        {
            AccessTools.DeclaredMethod(typeof(NDailyRunLoadScreen), "CleanUpLobby").Invoke(screen, [disconnectSession]);
        }
    }
}
