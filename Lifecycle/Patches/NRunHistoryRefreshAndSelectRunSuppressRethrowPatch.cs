using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <see cref="NRunHistory.RefreshAndSelectRun" /> logs run-history load failures and shows the out-of-date visual,
    ///     then rethrows. That propagates through <c>TaskHelper.RunSafely</c> and can freeze input. Swallow after vanilla
    ///     handling so the menu stays usable (same spirit as continue-run missing-character patches).
    /// </summary>
    public class NRunHistoryRefreshAndSelectRunSuppressRethrowPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nrun_history_refresh_and_select_run_suppress_rethrow";

        /// <inheritdoc />
        public static string Description =>
            "Run history: after failed load UI state, do not rethrow (avoids TaskHelper stall)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRunHistory), "RefreshAndSelectRun", [typeof(int)])];
        }

        /// <summary>
        ///     Harmony finalizer: swallow exceptions so arrow navigation and menu remain responsive.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static Exception? Finalizer(Exception? __exception)
            // ReSharper restore InconsistentNaming
        {
            if (__exception == null)
                return null;

            RitsuLibFramework.Logger.Warn(
                "[Saves] Run history load exception suppressed after vanilla error UI: " + __exception.Message);
            return null;
        }
    }
}
