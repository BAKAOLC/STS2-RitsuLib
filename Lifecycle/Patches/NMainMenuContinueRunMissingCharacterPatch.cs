using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Before <c>OnContinueButtonPressedAsync</c> reaches <c>RunState.FromSerializable</c>, rejects saves
    ///     that reference unregistered characters. The normal vanilla continue flow is left untouched.
    ///     在 <c>OnContinueButtonPressedAsync</c> 进入 <c>RunState.FromSerializable</c> 前拒绝引用未注册角色的存档。
    ///     正常原版继续跑局流程保持不变。
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
            return [new(typeof(NMainMenu), "OnContinueButtonPressedAsync")];
        }

        /// <summary>
        ///     Harmony prefix: only replaces the continue task for invalid mod-character saves.
        ///     Harmony prefix：仅在 mod 角色存档无效时替换继续跑局任务。
        /// </summary>
        public static bool Prefix(NMainMenu __instance, ref Task __result)
        {
            var read = ReadRunSaveResultRef(__instance);
            if (read is not { Success: true, SaveData: not null } ||
                !RunResumeMissingCharacterSupport.AnyPlayerMissingRegisteredCharacter(read.SaveData))
                return true;

            __result = BlockContinueRunAsync(__instance);
            return false;
        }

        private static Task BlockContinueRunAsync(NMainMenu menu)
        {
            var continueButton = ContinueButtonRef(menu);
            continueButton.Disable();
            NAudioManager.Instance?.StopMusic();
            RitsuLibFramework.Logger.Warn(
                "[Saves] Continue run blocked: save references character(s) not in ModelDb (e.g. mod not loaded). " +
                "Run save was left on disk.");
            DisplayLoadSaveError(menu);
            return Task.CompletedTask;
        }
    }
}
