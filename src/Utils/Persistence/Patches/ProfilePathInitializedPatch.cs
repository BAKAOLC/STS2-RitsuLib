using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.Persistence.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Notifies <see cref="DataReadyLifecycle" /> after <see cref="SaveManager" /> initializes or switches the profile path, establishing the trigger point for safe data operations.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="SaveManager" /> 初始化或切换档案路径后通知 <see cref="DataReadyLifecycle" />，作为安全执行数据操作的触发点。</para>
    /// </summary>
    internal class ProfilePathInitializedPatch : IPatchMethod
    {
        public static string PatchId => "profile_path_initialized";
        public static string Description => "Notify safe data-ready lifecycle after profile path initialization";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), "InitProfileId", [typeof(int?)]),
                new(typeof(SaveManager), "SwitchProfileId", [typeof(int)]),
            ];
        }

        public static void Postfix()
        {
            try
            {
                DataReadyLifecycle.NotifyPotentialReady("SaveManager.InitProfileId/SwitchProfileId");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Persistence] Failed to process profile path initialized hook: {ex.Message}");
            }
        }
    }
}
