using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Utils.Persistence.Patches
{
    /// <summary>
    ///     Framework trigger point for safe data operations.
    ///     Fires after SaveManager has initialized/switched profile path, then forwards to DataReadyLifecycle.
    ///     安全数据操作的框架触发点。
    ///     在 SaveManager 初始化 / 切换档案路径后触发，然后转发给 DataReadyLifecycle。
    /// </summary>
    public class ProfilePathInitializedPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "profile_path_initialized";

        /// <inheritdoc />
        public static string Description => "Notify safe data-ready lifecycle after profile path initialization";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), "InitProfileId", [typeof(int?)]),
                new(typeof(SaveManager), "SwitchProfileId", [typeof(int)]),
            ];
        }

        /// <summary>
        ///     Publishes data-ready lifecycle notifications after profile path initialization completes.
        ///     档案路径初始化完成后发布数据 ready 生命周期通知。
        /// </summary>
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
