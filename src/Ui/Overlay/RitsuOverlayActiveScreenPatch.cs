using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Ui.Overlay
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Keeps keyboard and controller navigation in the visible RitsuLib settings or developer-tools window.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 RitsuLib 设置或开发者工具窗口可见时，使键盘与控制器导航保持在该窗口内。
    ///     </para>
    /// </summary>
    [HarmonyPriority(Priority.Last)]
    internal sealed class RitsuOverlayActiveScreenPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_overlay_active_screen";
        public static string Description => "Keep keyboard and controller focus inside visible RitsuLib windows";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActiveScreenContext), nameof(ActiveScreenContext.GetCurrentScreen))];
        }

        public static void Postfix(ref IScreenContext? __result)
        {
            if (RitsuOverlayHostService.TryGetActiveScreen(out var overlayScreen))
                __result = overlayScreen;
        }
    }
}
