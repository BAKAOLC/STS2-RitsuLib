using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Platform.Steam;

namespace STS2RitsuLib.Platform.Patches
{
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
            var publishedFileId = ev.GetType()
                .GetField("m_nPublishedFileId")
                ?.GetValue(ev);
            var value = publishedFileId?
                .GetType()
                .GetField("m_PublishedFileId")
                ?.GetValue(publishedFileId);
            return value == null ? null : Convert.ToUInt64(value);
        }
    }
}
