using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal sealed class CharacterSelectButtonScrollPatch : IPatchMethod
    {
        private static readonly CharacterButtonStripScrollOptions Options = new(
            8,
            928f,
            200f,
            -200f,
            0f);

        /// <inheritdoc />
        public static string PatchId => "character_select_button_scroll";

        /// <inheritdoc />
        public static string Description => "Replace character-select button strip with a measured horizontal scroller";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
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

    internal sealed class CustomRunCharacterSelectButtonScrollPatch : IPatchMethod
    {
        private static readonly CharacterButtonStripScrollOptions Options = new(
            5,
            660f,
            167f,
            -177f,
            -10f);

        /// <inheritdoc />
        public static string PatchId => "custom_run_character_select_button_scroll";

        /// <inheritdoc />
        public static string Description =>
            "Replace custom-run character button strip with a measured horizontal scroller";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCustomRunScreen), "InitCharacterButtons")];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NCustomRunScreen __instance)
        {
            var root = __instance.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons/ButtonScrollContainer") ??
                       __instance.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons");
            NCharacterButtonStripScroller.Install(root, Options);
        }
    }
}
