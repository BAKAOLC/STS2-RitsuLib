using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Before <see cref="RunState.FromSerializable" />, reject saves that reference unregistered characters so the
    ///     vanilla <c>catch { DisplayLoadSaveError(); throw; }</c> path never runs (avoids TaskHelper rethrow freeze).
    ///     Run files are not deleted.
    /// </summary>
    public class NMainMenuContinueRunMissingCharacterPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NMainMenu, ReadSaveResult<SerializableRun>?> ReadRunSaveResultRef =
            AccessTools.FieldRefAccess<NMainMenu, ReadSaveResult<SerializableRun>?>("_readRunSaveResult");

        private static readonly AccessTools.FieldRef<NMainMenu, NMainMenuTextButton> ContinueButtonRef =
            AccessTools.FieldRefAccess<NMainMenu, NMainMenuTextButton>("_continueButton");

        private static readonly Action<NMainMenu> DisplayLoadSaveError =
            AccessTools.MethodDelegate<Action<NMainMenu>>(
                AccessTools.DeclaredMethod(typeof(NMainMenu), "DisplayLoadSaveError"));

        /// <inheritdoc />
        public static string PatchId => "nmain_menu_continue_run_missing_character";

        /// <inheritdoc />
        public static string Description =>
            "Main menu Continue: block resume when CharacterModel is missing; no save deletion; avoid throw after invalid-save UI";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenu), "OnContinueButtonPressed", [typeof(NButton)])];
        }

        /// <summary>
        ///     Harmony prefix: replaces Continue handling with safe read validation and async continue; returns false to
        ///     skip vanilla.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NMainMenu __instance, NButton _)
        {
            var read = ReadRunSaveResultRef(__instance);
            if (read is not { Success: true } || read.SaveData == null)
                DisplayLoadSaveError(__instance);
            else
                TaskHelper.RunSafely(ContinueRunAsync(__instance, read.SaveData));

            return false;
        }

        private static async Task ContinueRunAsync(NMainMenu menu, SerializableRun serializableRun)
        {
            _ = 2;
            var continueButton = ContinueButtonRef(menu);
            try
            {
                continueButton.Disable();
                NAudioManager.Instance?.StopMusic();

                if (RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(serializableRun))
                {
                    RitsuLibFramework.Logger.Warn(
                        "[Saves] Continue run blocked: save references character(s) not in ModelDb (e.g. mod not loaded). " +
                        "Run save was left on disk.");
                    DisplayLoadSaveError(menu);
                    return;
                }

                var runState = RunState.FromSerializable(serializableRun);
                var game = NGame.Instance;
                if (game == null)
                    throw new InvalidOperationException("NGame.Instance is null during continue run.");

                #if STS2_V_0_103_2
                RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
                #else
                await RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
                #endif
                Log.Info($"Continuing run with character: {serializableRun.Players[0].CharacterId}");
                SfxCmd.Play(runState.Players[0].Character.CharacterTransitionSfx);
                await game.Transition.FadeOut(0.8f,
                    runState.Players[0].Character.CharacterSelectTransitionPath);
                game.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
                await game.LoadRun(runState, serializableRun.PreFinishedRoom);
                await game.Transition.FadeIn();
            }
            catch (Exception)
            {
                DisplayLoadSaveError(menu);
                throw;
            }
        }
    }
}
