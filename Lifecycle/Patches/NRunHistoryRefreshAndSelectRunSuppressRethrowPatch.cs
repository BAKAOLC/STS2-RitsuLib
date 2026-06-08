using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <see cref="NRunHistory.RefreshAndSelectRun" /> logs run-history load failures and shows the out-of-date visual,
    ///     then rethrows. That propagates through <c>TaskHelper.RunSafely</c> and can freeze input. Swallow after vanilla
    ///     handling so the menu stays usable (same spirit as continue-run missing-character patches).
    ///     <see cref="NRunHistory.RefreshAndSelectRun" /> 会记录跑局历史加载失败并显示过期视觉状态，
    ///     随后重新抛出。该异常会经由 <c>TaskHelper.RunSafely</c> 传播并可能冻结输入。在原版
    ///     处理后吞掉异常，使菜单保持可用（与继续跑局缺失角色补丁思路一致）。
    /// </summary>
    internal class NRunHistoryRefreshAndSelectRunSuppressRethrowPatch : IPatchMethod
    {
        public static string PatchId => "nrun_history_refresh_and_select_run_suppress_rethrow";

        public static string Description =>
            "Run history: after failed load UI state, do not rethrow (avoids TaskHelper stall)";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRunHistory), "RefreshAndSelectRun", [typeof(int)])];
        }

        public static Exception? Finalizer(Exception? __exception)
        {
            if (__exception == null)
                return null;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Run history load exception suppressed after vanilla error UI: " + __exception.Message);
            return null;
        }
    }
}
