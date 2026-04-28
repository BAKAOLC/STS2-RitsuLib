using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.RunSidecar.Patches
{
    /// <summary>
    ///     Aligns run sidecar disk cleanup with vanilla removal of the current run save file (singleplayer and
    ///     multiplayer), including main-menu abandon where <see cref="RunManager.OnEnded" /> is not invoked.
    /// </summary>
    public static class ModRunSidecarSaveDeletionPatches
    {
        private static void TryPruneSidecarForCurrentSpRunSave(SaveManager saveManager)
        {
            try
            {
                var read = saveManager.LoadRunSave();
                if (!read.Success || read.SaveData is not { } run)
                    return;

                var netId = RunManager.Instance?.NetService?.NetId ?? 0UL;
                var fp = ModRunSidecarFingerprint.FromSerializableRun(run, netId);
                ModRunSidecarStore.TryDeleteRunDirectoryForFingerprint(fp);
            }
            catch
            {
                // best-effort
            }
        }

        private static void TryPruneSidecarForCurrentMpRunSave(SaveManager saveManager)
        {
            try
            {
                var localId = PlatformUtil.GetLocalPlayerId(PlatformUtil.PrimaryPlatform);
                var read = saveManager.LoadAndCanonicalizeMultiplayerRunSave(localId);
                if (!read.Success || read.SaveData is not { } run)
                    return;

                var fp = ModRunSidecarFingerprint.FromSerializableRun(run, localId);
                ModRunSidecarStore.TryDeleteRunDirectoryForFingerprint(fp);
            }
            catch
            {
                // best-effort
            }
        }

        /// <summary>
        ///     Harmony prefix on <see cref="SaveManager.DeleteCurrentRun" />: deletes the matching sidecar folder while
        ///     the run save file still exists on disk.
        /// </summary>
        public sealed class DeleteCurrentRun : IPatchMethod
        {
            /// <summary>
            ///     Unique id used by the mod patch registry.
            /// </summary>
            public static string PatchId => "ritsulib_run_sidecar_delete_current_run_prefix";

            /// <summary>
            ///     When false, a patch failure does not abort the rest of the mod bootstrap.
            /// </summary>
            public static bool IsCritical => false;

            /// <summary>
            ///     Short human-readable description for logs and diagnostics.
            /// </summary>
            public static string Description =>
                "Delete run sidecar folder before SaveManager.DeleteCurrentRun removes the run save file";

            /// <summary>
            ///     Returns the Harmony target methods for this patch.
            /// </summary>
            /// <returns>Targets for parameterless <c>SaveManager.DeleteCurrentRun()</c>.</returns>
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(SaveManager), nameof(SaveManager.DeleteCurrentRun), Type.EmptyTypes)];
            }

            /// <summary>
            ///     Loads the current run save, then removes its sidecar directory before vanilla deletes the file.
            /// </summary>
            /// <param name="__instance">The save manager instance.</param>
            // ReSharper disable once InconsistentNaming
            public static void Prefix(SaveManager __instance)
            {
                TryPruneSidecarForCurrentSpRunSave(__instance);
            }
        }

        /// <summary>
        ///     Harmony prefix on <see cref="SaveManager.DeleteCurrentMultiplayerRun" />: deletes the matching sidecar
        ///     folder while the multiplayer run save still exists on disk.
        /// </summary>
        public sealed class DeleteCurrentMultiplayerRun : IPatchMethod
        {
            /// <summary>
            ///     Unique id used by the mod patch registry.
            /// </summary>
            public static string PatchId => "ritsulib_run_sidecar_delete_current_mp_run_prefix";

            /// <summary>
            ///     When false, a patch failure does not abort the rest of the mod bootstrap.
            /// </summary>
            public static bool IsCritical => false;

            /// <summary>
            ///     Short human-readable description for logs and diagnostics.
            /// </summary>
            public static string Description =>
                "Delete run sidecar folder before SaveManager.DeleteCurrentMultiplayerRun removes the mp run save file";

            /// <summary>
            ///     Returns the Harmony target methods for this patch.
            /// </summary>
            /// <returns>Targets for parameterless <c>SaveManager.DeleteCurrentMultiplayerRun()</c>.</returns>
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SaveManager), nameof(SaveManager.DeleteCurrentMultiplayerRun), Type.EmptyTypes),
                ];
            }

            /// <summary>
            ///     Loads the canonical mp run save for the local player, then removes its sidecar directory.
            /// </summary>
            /// <param name="__instance">The save manager instance.</param>
            // ReSharper disable once InconsistentNaming
            public static void Prefix(SaveManager __instance)
            {
                TryPruneSidecarForCurrentMpRunSave(__instance);
            }
        }
    }
}
