using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.Persistence.Patches
{
    internal static class ModDataCloudSyncPatches
    {
        public sealed class AfterInitProfileId : IPatchMethod
        {
            public static string PatchId => "ritsulib_mod_data_cloud_after_init_profile";

            public static bool IsCritical => false;

            public static string Description =>
                "Defer async reconcile of mod_data with Steam cloud after profile id init (non-blocking)";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(SaveManager), nameof(SaveManager.InitProfileId), [typeof(int?)])];
            }

            public static void Postfix()
            {
                ModDataCloudMirror.ScheduleReconcileModDataWithCloud();
            }
        }

        public sealed class AfterSwitchProfileId : IPatchMethod
        {
            public static string PatchId => "ritsulib_mod_data_cloud_after_switch_profile";

            public static bool IsCritical => false;

            public static string Description =>
                "Defer async reconcile of mod_data with Steam cloud after switching save profile";

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(SaveManager), nameof(SaveManager.SwitchProfileId), [typeof(int)])];
            }

            public static void Postfix()
            {
                ModDataCloudMirror.ScheduleReconcileModDataWithCloud();
            }
        }
    }
}
