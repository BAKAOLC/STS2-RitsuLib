using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.Persistence.Patches
{
    /// <summary>
    ///     Cleans up mod persistence when the game deletes a save profile.
    ///     游戏删除存档档案时清理 mod 持久化数据。
    /// </summary>
    internal class ProfileDeletePatch : IPatchMethod
    {
        public static string PatchId => "profile_delete";
        public static string Description => "Delete mod data when game profile is deleted";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), "DeleteProfile", [typeof(int)])];
        }

        public static void Prefix(int profileId)
        {
            try
            {
                ModDataCloudMirror.ScheduleDeleteCloudModDataForProfile(profileId);
                ModDataStore.DeleteAllProfileData(profileId);
                ProfileManager.Instance.OnProfileDeleted(profileId);
                DataReadyLifecycle.NotifyProfileInvalidated(profileId, "SaveManager.DeleteProfile");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Persistence] Failed to delete mod data for profile {profileId}: {ex.Message}");
            }
        }
    }
}
