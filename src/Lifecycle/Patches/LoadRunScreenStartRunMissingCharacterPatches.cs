using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Compatibility: blocks multiplayer load-screen <c>StartRun</c> when any player references an unregistered
    ///     character, without deleting saves; otherwise leaves the vanilla load flow untouched.
    ///     兼容性：当任意玩家引用未注册角色时阻止多人加载界面的 <c>StartRun</c> 且不删除存档；
    ///     其他情况下保持原版加载流程不变。
    /// </summary>
    internal class NMultiplayerLoadGameScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, LoadRunLobby> RunLobbyRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, LoadRunLobby>("_runLobby");

        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, NConfirmButton> ConfirmRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, NConfirmButton>("_confirmButton");

        private static readonly AccessTools.FieldRef<NMultiplayerLoadGameScreen, NBackButton> UnreadyRef =
            AccessTools.FieldRefAccess<NMultiplayerLoadGameScreen, NBackButton>("_unreadyButton");

        public static string PatchId => "nmultiplayer_load_game_begin_run_missing_character";

        public static string Description =>
            "Multiplayer load screen: block StartRun when a saved player character is not registered; no save deletion";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMultiplayerLoadGameScreen), "StartRun")];
        }

        public static bool Prefix(NMultiplayerLoadGameScreen __instance, ref Task __result)
        {
            var lobby = RunLobbyRef(__instance);
            if (!RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
                return true;

            __result = LoadRunMissingCharacterBlocker.BlockStartRunAsync(
                "Multiplayer",
                () => ConfirmRef(__instance).Enable(),
                () => UnreadyRef(__instance).Enable());
            return false;
        }
    }

    /// <summary>
    ///     Compatibility: blocks custom-run load-screen <c>StartRun</c> when any player references an unregistered
    ///     character, without deleting saves; otherwise leaves the vanilla load flow untouched.
    ///     兼容性：当任意玩家引用未注册角色时阻止自定义跑局加载界面的 <c>StartRun</c> 且不删除存档；
    ///     其他情况下保持原版加载流程不变。
    /// </summary>
    internal class NCustomRunLoadScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, LoadRunLobby> LobbyRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, LoadRunLobby>("_lobby");

        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, NConfirmButton> ConfirmRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, NConfirmButton>("_confirmButton");

        private static readonly AccessTools.FieldRef<NCustomRunLoadScreen, NBackButton> UnreadyRef =
            AccessTools.FieldRefAccess<NCustomRunLoadScreen, NBackButton>("_unreadyButton");

        public static string PatchId => "ncustom_run_load_begin_run_missing_character";

        public static string Description =>
            "Custom run load screen: block StartRun when a saved player character is not registered; no save deletion";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCustomRunLoadScreen), "StartRun")];
        }

        public static bool Prefix(NCustomRunLoadScreen __instance, ref Task __result)
        {
            var lobby = LobbyRef(__instance);
            if (!RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
                return true;

            __result = LoadRunMissingCharacterBlocker.BlockStartRunAsync(
                "Custom run",
                () => ConfirmRef(__instance).Enable(),
                () => UnreadyRef(__instance).Enable());
            return false;
        }
    }

    /// <summary>
    ///     Compatibility: blocks daily-run load-screen <c>StartRun</c> when any player references an unregistered
    ///     character, without deleting saves; otherwise leaves the vanilla load flow untouched.
    ///     兼容性：当任意玩家引用未注册角色时阻止每日跑局加载界面的 <c>StartRun</c> 且不删除存档；
    ///     其他情况下保持原版加载流程不变。
    /// </summary>
    internal class NDailyRunLoadScreenBeginRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly Lazy<AccessTools.FieldRef<NDailyRunLoadScreen, LoadRunLobby?>> LobbyRefLazy =
            new(() => AccessTools.FieldRefAccess<NDailyRunLoadScreen, LoadRunLobby?>("_lobby"));

        private static readonly Lazy<AccessTools.FieldRef<NDailyRunLoadScreen, NConfirmButton>> EmbarkRefLazy =
            new(() => AccessTools.FieldRefAccess<NDailyRunLoadScreen, NConfirmButton>("_embarkButton"));

        private static readonly Lazy<AccessTools.FieldRef<NDailyRunLoadScreen, NBackButton>> UnreadyRefLazy =
            new(() => AccessTools.FieldRefAccess<NDailyRunLoadScreen, NBackButton>("_unreadyButton"));

        public static string PatchId => "ndaily_run_load_begin_run_missing_character";

        public static string Description =>
            "Daily run load screen: block StartRun when a saved player character is not registered; no save deletion";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDailyRunLoadScreen), "StartRun")];
        }

        public static bool Prefix(NDailyRunLoadScreen __instance, ref Task __result)
        {
            var lobby = LobbyRefLazy.Value(__instance);
            if (lobby == null)
                return true;

            if (!RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(lobby.Run))
                return true;

            __result = LoadRunMissingCharacterBlocker.BlockStartRunAsync(
                "Daily run",
                () => EmbarkRefLazy.Value(__instance).Enable(),
                () => UnreadyRefLazy.Value(__instance).Enable());
            return false;
        }
    }

    internal static class LoadRunMissingCharacterBlocker
    {
        internal static Task BlockStartRunAsync(
            string source,
            Action enablePrimaryButton,
            Action enableBackButton)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Saves] {source} load blocked: missing CharacterModel; run save not deleted.");
            RunResumeMissingCharacterSupport.TryShowInvalidRunSaveModal();
            enablePrimaryButton();
            enableBackButton();
            return Task.CompletedTask;
        }
    }
}
