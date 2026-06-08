using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Applies registered dev-console autocomplete enhancements through <see cref="DevConsoleAutocompleteRegistry" />.
    /// </summary>
    internal sealed class DevConsoleAutocompleteEnhancementPatch : IPatchMethod
    {
        public static string PatchId => "dev_console_autocomplete_enhancement";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole autocomplete: registry-driven match and display enhancements per command argument";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AbstractConsoleCmd), nameof(AbstractConsoleCmd.CompleteArgument), true)];
        }

        public static void Prefix(
            AbstractConsoleCmd __instance,
            string[] completedArgs,
            ref Func<string, string, bool>? matchPredicate)
        {
            var enhancements = DevConsoleAutocompleteRegistry.Resolve(
                __instance,
                completedArgs,
                completedArgs.Length);

            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return;

            matchPredicate = DevConsoleAutocompleteEnhancer.BuildMatchPredicate(
                enhancements,
                matchPredicate,
                completedArgs);
        }

        public static void Postfix(
            AbstractConsoleCmd __instance,
            string[] completedArgs,
            ref CompletionResult __result)
        {
            var enhancements = DevConsoleAutocompleteRegistry.Resolve(
                __instance,
                completedArgs,
                __result.ArgumentIndex);

            DevConsoleAutocompleteEnhancer.ApplyToResult(ref __result, enhancements, completedArgs);
        }
    }
}
