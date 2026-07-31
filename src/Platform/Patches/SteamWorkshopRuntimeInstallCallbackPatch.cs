using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Platform.Steam;

namespace STS2RitsuLib.Platform.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Suppresses the base game's runtime mod-detection callback for Workshop downloads that RitsuLib itself
    ///         triggered during an update check.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         对 RitsuLib 在更新检查中自行触发的创意工坊下载，阻止原版游戏的运行时模组检测回调。
    ///     </para>
    /// </summary>
    internal sealed class SteamWorkshopRuntimeInstallCallbackPatch : IPatchMethod
    {
        public static string PatchId => "steam_workshop_runtime_install_callback_ritsulib_update_guard";

        public static string Description =>
            "Suppress vanilla runtime mod detection for Workshop downloads triggered by RitsuLib update checks";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                PatchTarget.OptionalMethod(typeof(ModManager), "OnSteamWorkshopItemInstalled"),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Skips the original callback only when its event identifies a recorded RitsuLib-triggered
        ///         Workshop download.
        ///     </para>
        ///     <para xml:lang="zh-CN">仅当事件标识出已记录的 RitsuLib 触发创意工坊下载时，才跳过原始回调。</para>
        /// </summary>
        public static bool Prefix(object[] __args)
        {
            if (__args.Length == 0)
                return true;

            var itemId = TryReadItemId(__args[0]);
            if (itemId == null)
                return true;

            if (!RitsuSteamWorkshopUpdates.IsTriggeredDownloadItem(itemId.Value))
                return true;

            RitsuLibFramework.Logger.Info(
                $"[SteamWorkshopUpdate] Suppressed vanilla runtime Workshop install handling for recorded RitsuLib-triggered item {itemId}.");
            return false;
        }

        private static ulong? TryReadItemId(object ev)
        {
            try
            {
                var publishedFileId = ev.GetType()
                    .GetField("m_nPublishedFileId")
                    ?.GetValue(ev);
                var value = publishedFileId?
                    .GetType()
                    .GetField("m_PublishedFileId")
                    ?.GetValue(publishedFileId);
                return value == null ? null : Convert.ToUInt64(value);
            }
            catch
            {
                return null;
            }
        }
    }
}
