using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     After the main menu node is ready, optionally dumps Harmony patch info once per session (deferred).
    /// </summary>
    public class NMainMenuHarmonyPatchDumpPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "nmain_menu_harmony_patch_dump";

        /// <inheritdoc />
        public static string Description =>
            "Main menu: deferred optional Harmony patch dump when enabled in RitsuLib settings";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenu), "_Ready")];
        }

        /// <summary>
        ///     Harmony postfix: schedule auto-dump after the menu finishes its ready work.
        /// </summary>
        public static void Postfix()
        {
            Callable.From(HarmonyPatchDumpCoordinator.TryAutoDumpOnFirstMainMenu).CallDeferred();
        }
    }
}
