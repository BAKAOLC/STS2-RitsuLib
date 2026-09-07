using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Ui.MainMenu
{
    internal sealed class MainMenuScrollCreatePatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_create";
        public static string Description => "Keep main-menu options inside a bounded scrolling area";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
            [new(typeof(NMainMenu), nameof(NMainMenu.Create), [typeof(bool)])];

        [HarmonyPriority(Priority.First)]
        public static void Postfix(NMainMenu __result) => NMainMenuScroller.Install(__result);
    }

    internal sealed class MainMenuScrollReadyPatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_ready";
        public static string Description => "Initialize main-menu scrolling after button injection";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [new(typeof(NMainMenu), nameof(NMainMenu._Ready))];

        [HarmonyBefore(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(NMainMenu __instance) => NMainMenuScroller.Install(__instance);

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NMainMenu __instance) => NMainMenuScroller.Find(__instance)?.Initialize();
    }

    internal sealed class MainMenuScrollFocusConnectionsPatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_focus_connections";

        public static string Description =>
            "Let the scrolling menu track focus for initial and dynamically added buttons";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [new(typeof(NMainMenu), "ConnectMainMenuTextButtonFocusLogic")];

        public static bool Prefix(NMainMenu __instance) => NMainMenuScroller.Find(__instance) == null;
    }

    internal sealed class MainMenuScrollDefaultFocusPatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_default_focus";
        public static string Description => "Restore focus to valid original or mod-added menu entries";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() => [new(typeof(NMainMenu), "get_DefaultFocusedControl")];

        public static bool Prefix(NMainMenu __instance, ref Control? __result)
        {
            if (NMainMenuScroller.Find(__instance) is not { Initialized: true } host)
                return true;
            __result = host.GetDefaultFocus();
            return false;
        }
    }

    internal sealed class MainMenuScrollButtonInputPatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_button_input";
        public static string Description => "Validate menu selection gestures and refresh directional neighbors";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            new(typeof(NClickableControl), nameof(NClickableControl._GuiInput), [typeof(InputEvent)]),
        ];

        public static bool Prefix(NClickableControl __instance, InputEvent inputEvent)
        {
            return __instance is not NMainMenuTextButton button || button.GetParent() is not NMainMenuScroller host ||
                   host.FilterButtonInput(button, inputEvent);
        }
    }

    internal sealed class MainMenuScrollRunInfoPatch : IPatchMethod
    {
        public static string PatchId => "main_menu_scroll_run_info";
        public static string Description => "Keep continue-run details outside menu clipping and within the screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            new(typeof(NContinueRunInfo), nameof(NContinueRunInfo.AnimShow)),
            new(typeof(NContinueRunInfo), nameof(NContinueRunInfo.AnimHide)),
        ];

        public static bool Prefix(NContinueRunInfo __instance, MethodBase __originalMethod)
        {
            return __instance.GetParent()?.GetParent() is not NMainMenuScroller host ||
                   !host.AnimateRunInfo(__instance, __originalMethod.Name == nameof(NContinueRunInfo.AnimShow));
        }
    }
}
