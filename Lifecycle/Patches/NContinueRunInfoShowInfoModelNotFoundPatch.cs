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
    ///     <see cref="NContinueRunInfo.ShowInfo" /> 会对 act 和 character 使用 <c>ModelDb.GetById</c>；缺失 mod
    ///     内容会在玩家按 Continue 之前，于 <see cref="NMainMenu._Ready" /> / <c>RefreshButtons</c> 期间抛出。
    ///     回退到与读取结果错误相同的错误 UI。
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
        ///     Harmony finalizer：吞掉预览中的 <see cref="ModelNotFoundException" /> 并显示面板错误
        ///     状态。
        /// </summary>
        public static Exception? Finalizer(Exception? __exception, NContinueRunInfo __instance)
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
