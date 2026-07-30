using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Defers publication of the shared main-menu lifecycle event until the main menu node is ready.</para>
    ///     <para xml:lang="zh-CN">主菜单节点就绪后，延迟发布共享的主菜单生命周期事件。</para>
    /// </summary>
    internal class NMainMenuReadyLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "nmain_menu_ready_lifecycle";

        public static string Description =>
            "Main menu: publish deferred RitsuLib main-menu-ready lifecycle event";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenu), "_Ready")];
        }

        public static void Postfix()
        {
            Callable.From(PublishMainMenuReady).CallDeferred();
        }

        private static void PublishMainMenuReady()
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new MainMenuReadyEvent(DateTimeOffset.UtcNow),
                nameof(MainMenuReadyEvent));
        }
    }
}
