using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Applies registered dev-console autocomplete enhancements through <see cref="DevConsoleAutocompleteRegistry" />.
    /// </summary>
    public sealed class DevConsoleAutocompleteEnhancementPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "dev_console_autocomplete_enhancement";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "DevConsole autocomplete: registry-driven match and display enhancements per command argument";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AbstractConsoleCmd), nameof(AbstractConsoleCmd.CompleteArgument), true)];
        }

        /// <summary>
        ///     Installs registered match-predicate enhancements for the active argument slot.
        /// </summary>
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

        /// <summary>
        ///     Applies registered result enhancements such as localized labels and de-duplication.
        /// </summary>
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
