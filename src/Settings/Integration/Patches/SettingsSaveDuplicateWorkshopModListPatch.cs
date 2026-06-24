using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Settings.Patches
{
    internal sealed class SettingsSaveDuplicateWorkshopModListPatch : IPatchMethod
    {
        public static string PatchId => "settings_save_duplicate_workshop_mod_list";

        public static string Description =>
            "Remove workshop mod_list entries when the same mod id exists in the local mods directory";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.SaveSettings), Type.EmptyTypes)];
        }

        public static void Prefix(SaveManager __instance)
        {
            try
            {
                var removed =
                    ContentModLoadOrderInventory.RemoveLocalDuplicateWorkshopEntries(
                        __instance.SettingsSave.ModSettings);
                if (removed > 0)
                    RitsuLibFramework.Logger.Info(
                        $"[ContentModLoadOrder] Removed {removed} duplicate Steam Workshop mod_list entr{(removed == 1 ? "y" : "ies")} before saving settings.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ContentModLoadOrder] Failed to remove duplicate Steam Workshop mod_list entries: {ex.Message}");
            }
        }
    }
}
