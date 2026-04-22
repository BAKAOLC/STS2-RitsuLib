using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.RunSidecar.Patches
{
    /// <summary>
    ///     Bumps run sidecar cache epochs when a saved run finishes loading so bindings re-read disk with the correct
    ///     fingerprint.
    /// </summary>
    public static class ModRunSidecarRunManagerPatches
    {
        /// <summary>
        ///     Harmony patch type: postfix on <see cref="RunManager.InitializeSavedRun" /> to refresh sidecar binding
        ///     epoch after a save finishes loading.
        /// </summary>
        public sealed class InitializeSavedRun : IPatchMethod
        {
            /// <summary>Unique id used by the mod patch registry.</summary>
            public static string PatchId => "ritsulib_run_sidecar_initialize_saved_run";

            /// <summary>When false, a patch failure does not abort the rest of the mod bootstrap.</summary>
            public static bool IsCritical => false;

            /// <summary>Short human-readable description for logs and diagnostics.</summary>
            public static string Description => "Run sidecar epoch after InitializeSavedRun";

            /// <summary>Returns the Harmony target methods for this patch.</summary>
            /// <returns>Targets for <c>RunManager.InitializeSavedRun(SerializableRun)</c>.</returns>
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(RunManager), "InitializeSavedRun", [typeof(SerializableRun)]),
                ];
            }

            /// <summary>Bumps the run sidecar epoch so UI bindings re-resolve disk against the loaded run.</summary>
            /// <param name="__instance">Harmony-injected run manager instance.</param>
            /// <param name="save">The serialized run that was just initialized (reserved for future use).</param>
            // ReSharper disable once InconsistentNaming
            public static void Postfix(RunManager __instance, SerializableRun save)
            {
                _ = __instance;
                _ = save;
                ModRunSidecarSession.NotifyRunLoadedFromSave(save);
            }
        }

        /// <summary>
        ///     Harmony patch type: postfix on <see cref="RunManager.InitializeNewRun" /> to refresh sidecar binding epoch
        ///     after a brand-new run is created.
        /// </summary>
        public sealed class InitializeNewRun : IPatchMethod
        {
            /// <summary>Unique id used by the mod patch registry.</summary>
            public static string PatchId => "ritsulib_run_sidecar_initialize_new_run";

            /// <summary>When false, a patch failure does not abort the rest of the mod bootstrap.</summary>
            public static bool IsCritical => false;

            /// <summary>Short human-readable description for logs and diagnostics.</summary>
            public static string Description => "Run sidecar epoch after InitializeNewRun";

            /// <summary>Returns the Harmony target methods for this patch.</summary>
            /// <returns>Targets for parameterless <c>RunManager.InitializeNewRun()</c>.</returns>
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(RunManager), "InitializeNewRun", Type.EmptyTypes),
                ];
            }

            /// <summary>Bumps the run sidecar epoch so UI bindings pick up the new run identity.</summary>
            /// <param name="__instance">Harmony-injected run manager instance.</param>
            // ReSharper disable once InconsistentNaming
            public static void Postfix(RunManager __instance)
            {
                _ = __instance;
                ModRunSidecarSession.NotifyFreshRunStarted();
            }
        }
    }
}
