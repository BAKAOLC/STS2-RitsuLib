using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Suppresses the rethrow after <see cref="NRunHistory.RefreshAndSelectRun" /> handles a
    ///         run-history load failure. The rethrow propagates through <c>TaskHelper.RunSafely</c> and can freeze input;
    ///         swallowing it after vanilla handling keeps the menu usable.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NRunHistory.RefreshAndSelectRun" /> 处理跑局历史加载失败后抑制重新抛出。该异常会经由
    ///         <c>TaskHelper.RunSafely</c> 传播并可能冻结输入；在原版处理后吞掉它可使菜单保持可用。
    ///     </para>
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
