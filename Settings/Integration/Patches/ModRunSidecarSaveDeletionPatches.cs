using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.RunSidecar.Patches
{
    /// <summary>
    ///     Aligns run sidecar disk cleanup with vanilla removal of the current run save file (singleplayer and
    ///     multiplayer), including main-menu abandon where <see cref="RunManager.OnEnded" /> is not invoked.
    ///     使跑局 sidecar 的磁盘清理与原版删除当前跑局存档文件的行为对齐（单人和
    ///     多人），包括不会调用 <see cref="RunManager.OnEnded" /> 的主菜单放弃跑局场景。
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
        ///     <see cref="SaveManager.DeleteCurrentRun" /> 上的 Harmony prefix：在跑局存档文件
        ///     仍存在于磁盘上时删除匹配的 sidecar 文件夹。
        /// </summary>
        public sealed class DeleteCurrentRun : IPatchMethod
        {
            /// <summary>
            ///     Unique id used by the mod patch registry.
            ///     mod patch 注册表使用的唯一 id。
            /// </summary>
            public static string PatchId => "ritsulib_run_sidecar_delete_current_run_prefix";

            /// <summary>
            ///     When false, a patch failure does not abort the rest of the mod bootstrap.
            ///     为 false 时，patch 失败不会中止 mod 引导流程的其余部分。
            /// </summary>
            public static bool IsCritical => false;

            /// <summary>
            ///     Short human-readable description for logs and diagnostics.
            ///     供日志和诊断使用的简短可读描述。
            /// </summary>
            public static string Description =>
                "Delete run sidecar folder before SaveManager.DeleteCurrentRun removes the run save file";

            /// <summary>
            ///     Returns the Harmony target methods for this patch.
            ///     返回此 patch 的 Harmony 目标方法。
            /// </summary>
            /// <returns>
            ///     Targets for parameterless <c>SaveManager.DeleteCurrentRun()</c>.
            ///     无参数 <c>SaveManager.DeleteCurrentRun()</c> 的目标。
            /// </returns>
            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(SaveManager), nameof(SaveManager.DeleteCurrentRun), Type.EmptyTypes)];
            }

            /// <summary>
            ///     Loads the current run save, then removes its sidecar directory before vanilla deletes the file.
            ///     加载当前跑局存档，然后在原版删除该文件前移除它的 sidecar 目录。
            /// </summary>
            /// <param name="__instance">
            ///     The save manager instance.
            ///     save manager 实例。
            /// </param>
            // ReSharper disable once InconsistentNaming
            public static void Prefix(SaveManager __instance)
            {
                TryPruneSidecarForCurrentSpRunSave(__instance);
            }
        }

        /// <summary>
        ///     Harmony prefix on <see cref="SaveManager.DeleteCurrentMultiplayerRun" />: deletes the matching sidecar
        ///     folder while the multiplayer run save still exists on disk.
        ///     <see cref="SaveManager.DeleteCurrentMultiplayerRun" /> 上的 Harmony prefix：在多人跑局存档
        ///     仍存在于磁盘上时删除匹配的 sidecar 文件夹。
        /// </summary>
        public sealed class DeleteCurrentMultiplayerRun : IPatchMethod
        {
            /// <summary>
            ///     Unique id used by the mod patch registry.
            ///     mod patch 注册表使用的唯一 id。
            /// </summary>
            public static string PatchId => "ritsulib_run_sidecar_delete_current_mp_run_prefix";

            /// <summary>
            ///     When false, a patch failure does not abort the rest of the mod bootstrap.
            ///     为 false 时，patch 失败不会中止 mod 引导流程的其余部分。
            /// </summary>
            public static bool IsCritical => false;

            /// <summary>
            ///     Short human-readable description for logs and diagnostics.
            ///     供日志和诊断使用的简短可读描述。
            /// </summary>
            public static string Description =>
                "Delete run sidecar folder before SaveManager.DeleteCurrentMultiplayerRun removes the mp run save file";

            /// <summary>
            ///     Returns the Harmony target methods for this patch.
            ///     返回此 patch 的 Harmony 目标方法。
            /// </summary>
            /// <returns>
            ///     Targets for parameterless <c>SaveManager.DeleteCurrentMultiplayerRun()</c>.
            ///     无参数 <c>SaveManager.DeleteCurrentMultiplayerRun()</c> 的目标。
            /// </returns>
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SaveManager), nameof(SaveManager.DeleteCurrentMultiplayerRun), Type.EmptyTypes),
                ];
            }

            /// <summary>
            ///     Loads the canonical mp run save for the local player, then removes its sidecar directory.
            ///     加载本地玩家的规范 mp 跑局存档，然后移除它的 sidecar 目录。
            /// </summary>
            /// <param name="__instance">
            ///     The save manager instance.
            ///     save manager 实例。
            /// </param>
            // ReSharper disable once InconsistentNaming
            public static void Prefix(SaveManager __instance)
            {
                TryPruneSidecarForCurrentMpRunSave(__instance);
            }
        }
    }
}
