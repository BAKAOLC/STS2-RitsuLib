using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using STS2RitsuLib.Patching.Models;
#if STS2_AT_LEAST_0_113_0
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
#endif

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal sealed class CharacterSelectButtonScrollPatch : IPatchMethod
    {
        internal static readonly CharacterButtonStripScrollOptions Options = new(
            8,
            928f,
            200f,
            -200f,
            0f);

        public static string PatchId => "character_select_button_scroll";
        public static string Description => "Replace character-select button strip with a measured horizontal scroller";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCharacterSelectScreen __instance)
        {
            var root = __instance.GetNodeOrNull<Control>("CharSelectButtons");
            NCharacterButtonStripScroller.Install(root, Options);
        }
    }

#if STS2_AT_LEAST_0_113_0
    internal sealed class SpecialModeCharacterSelectButtonScrollPatch : IPatchMethod
    {
        public static string PatchId => "special_mode_character_select_button_scroll";
        public static string Description => "Enable character button scrolling in special run modes";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NBlitzScreen), nameof(NBlitzScreen._Ready)),
                new(typeof(NDreadScreen), nameof(NDreadScreen._Ready)),
            ];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Control __instance)
        {
            var root = __instance.GetNodeOrNull<Control>("CharSelectButtons");
            NCharacterButtonStripScroller.Install(root, CharacterSelectButtonScrollPatch.Options);
        }
    }
#endif

    internal sealed class CustomRunCharacterSelectButtonScrollPatch : IPatchMethod
    {
        private static readonly CharacterButtonStripScrollOptions Options = new(
            5,
            660f,
            167f,
            -177f,
            -10f,
            true);

        public static string PatchId => "custom_run_character_select_button_scroll";

        public static string Description =>
            "Replace custom-run character button strip with a measured horizontal scroller";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCustomRunScreen), "InitCharacterButtons")];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCustomRunScreen __instance)
        {
            var root = __instance.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons/ButtonScrollContainer");
            if (root != null)
            {
                NCharacterButtonStripScroller.Install(root, Options);
                return;
            }

            var host = __instance.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons");
            NCharacterButtonStripScroller.InstallNested(host, "ButtonScrollContainer", Options);
        }
    }
}
