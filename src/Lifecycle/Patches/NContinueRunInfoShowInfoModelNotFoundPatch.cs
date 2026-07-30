using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts missing Act or character models encountered by <see cref="NContinueRunInfo.ShowInfo" /> into the
    ///         same error state used for an unreadable save.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="NContinueRunInfo.ShowInfo" /> 遇到的章节或角色模型缺失错误转换为与存档无法读取时相同的错误状态。
    ///     </para>
    /// </summary>
    internal class NContinueRunInfoShowInfoModelNotFoundPatch : IPatchMethod
    {
        private static readonly Action<NContinueRunInfo> ShowError =
            AccessTools.MethodDelegate<Action<NContinueRunInfo>>(
                AccessTools.DeclaredMethod(typeof(NContinueRunInfo), "ShowError"));

        public static string PatchId => "ncontinue_run_info_show_info_model_not_found";

        public static string Description =>
            "When continue-run preview hits ModelNotFoundException, show NContinueRunInfo error state instead of crashing";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NContinueRunInfo), "ShowInfo", [typeof(SerializableRun)])];
        }

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
