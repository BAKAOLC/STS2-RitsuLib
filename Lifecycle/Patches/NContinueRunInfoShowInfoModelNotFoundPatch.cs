using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <see cref="NContinueRunInfo.ShowInfo" /> uses <c>ModelDb.GetById</c> for act and character; missing mod
    ///     content throws during <see cref="NMainMenu._Ready" /> / <c>RefreshButtons</c> before the player presses Continue.
    ///     Fall back to the same error UI as a bad read result.
    /// </summary>
    public class NContinueRunInfoShowInfoModelNotFoundPatch : IPatchMethod
    {
        private static readonly Action<NContinueRunInfo> ShowError =
            AccessTools.MethodDelegate<Action<NContinueRunInfo>>(
                AccessTools.DeclaredMethod(typeof(NContinueRunInfo), "ShowError"));

        /// <inheritdoc />
        public static string PatchId => "ncontinue_run_info_show_info_model_not_found";

        /// <inheritdoc />
        public static string Description =>
            "When continue-run preview hits ModelNotFoundException, show NContinueRunInfo error state instead of crashing";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NContinueRunInfo), "ShowInfo", [typeof(SerializableRun)])];
        }

        /// <summary>
        ///     Harmony finalizer: swallows <see cref="ModelNotFoundException" /> from preview and shows the panel error
        ///     state.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static Exception? Finalizer(Exception? __exception, NContinueRunInfo __instance)
            // ReSharper restore InconsistentNaming
        {
            if (__exception is not ModelNotFoundException modelNotFoundException)
                return __exception;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Continue-run preview failed (model missing from ModelDb); showing error panel. Run save not modified. " +
                modelNotFoundException.Message);
            ShowError(__instance);
            return null;
        }
    }
}
