using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Nodes.Debug;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;
using GameDevConsole = MegaCrit.Sts2.Core.DevConsole.DevConsole;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Hides vanilla ghost text for localized autocomplete matches whose canonical completion does not extend the
    ///     typed input.
    /// </summary>
    internal sealed class DevConsoleAutocompleteGhostTextPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NDevConsole, GameDevConsole> DevConsoleField =
            AccessTools.FieldRefAccess<NDevConsole, GameDevConsole>("_devConsole");

        private static readonly AccessTools.FieldRef<NDevConsole, LineEdit> InputBufferField =
            AccessTools.FieldRefAccess<NDevConsole, LineEdit>("_inputBuffer");

        private static readonly AccessTools.FieldRef<NDevConsole, TabCompletionState> TabCompletionField =
            AccessTools.FieldRefAccess<NDevConsole, TabCompletionState>("_tabCompletion");

        private static readonly AccessTools.FieldRef<NDevConsole, Label> GhostTextLabelField =
            AccessTools.FieldRefAccess<NDevConsole, Label>("_ghostTextLabel");

        public static string PatchId => "dev_console_autocomplete_ghost_text";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole autocomplete: suppress invalid ghost text for localized-title matches";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "UpdateGhostText")];
        }

        public static bool Prefix(NDevConsole __instance)
        {
            if (!RitsuLibSettingsStore.IsDevConsoleAutocompleteEnhancementsEnabled())
                return true;

            if (TabCompletionField(__instance).InSelectionMode)
                return true;

            var inputBuffer = InputBufferField(__instance);
            var text = inputBuffer?.Text;
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var devConsole = DevConsoleField(__instance);
            var completionResults = devConsole?.GetCompletionResults(text);
            if (completionResults is not { Candidates.Count: 1, CommonPrefix.Length: > 0 })
                return true;

            if (completionResults.CommonPrefix.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                return true;

            HideGhostText(__instance);
            return false;
        }

        private static void HideGhostText(NDevConsole console)
        {
            if (GhostTextLabelField(console) is not { } ghostTextLabel)
                return;

            ghostTextLabel.Visible = false;
            ghostTextLabel.Text = string.Empty;
        }
    }
}
