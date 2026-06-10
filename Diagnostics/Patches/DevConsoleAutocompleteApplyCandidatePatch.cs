using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Nodes.Debug;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Strips localized autocomplete suffix labels before applying a selected candidate to the input line.
    /// </summary>
    internal sealed class DevConsoleAutocompleteApplyCandidatePatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NDevConsole, TabCompletionState> TabCompletionField =
            AccessTools.FieldRefAccess<NDevConsole, TabCompletionState>("_tabCompletion");

        public static string PatchId => "dev_console_autocomplete_apply_candidate";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole autocomplete: apply canonical model entry id when candidate has a localized suffix";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "AcceptSelection")];
        }

        public static void Prefix(NDevConsole __instance)
        {
            if (!RitsuLibSettingsStore.IsDevConsoleAutocompleteEnhancementsEnabled())
                return;

            var tabCompletion = TabCompletionField(__instance);
            if (!tabCompletion.InSelectionMode)
                return;

            var index = tabCompletion.SelectionIndex;
            if (index < 0 || index >= tabCompletion.CompletionCandidates.Count)
                return;

            tabCompletion.CompletionCandidates[index] =
                DevConsoleAutocompleteDisplay.StripLocalizedSuffix(tabCompletion.CompletionCandidates[index]);
        }
    }
}
