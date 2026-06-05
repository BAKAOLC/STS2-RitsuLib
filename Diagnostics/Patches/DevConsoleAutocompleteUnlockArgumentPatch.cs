using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Supplies <c>unlock &lt;type&gt; &lt;id&gt;...</c> autocomplete via
    ///     <see cref="AbstractConsoleCmd.CompleteArgument" />.
    /// </summary>
    public sealed class DevConsoleAutocompleteUnlockArgumentPatch : IPatchMethod
    {
        private static readonly MethodInfo CompleteArgumentMethod = AccessTools.Method(
            typeof(AbstractConsoleCmd),
            nameof(AbstractConsoleCmd.CompleteArgument),
            [
                typeof(IEnumerable<string>), typeof(string[]), typeof(string), typeof(CompletionType),
                typeof(Func<string, string, bool>),
            ]);

        /// <inheritdoc />
        public static string PatchId => "dev_console_autocomplete_unlock_argument";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "DevConsole unlock: autocomplete discovery ids after the discovery type token";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(UnlockConsoleCmd), nameof(UnlockConsoleCmd.GetArgumentCompletions), true)];
        }

        /// <summary>
        ///     Routes <c>unlock cards ID ...</c> completion through <see cref="AbstractConsoleCmd.CompleteArgument" />.
        /// </summary>
        public static bool Prefix(UnlockConsoleCmd __instance, string[] args, ref CompletionResult __result)
        {
            if (args.Length < 2 || CompleteArgumentMethod == null)
                return true;

            if (!DevConsoleUnlockAutocompleteSources.TryGetCandidates(args[0], out var candidates))
                return true;

            var completedArgs = args[..^1];
            var partial = args[^1];
            __result = (CompletionResult)CompleteArgumentMethod.Invoke(
                __instance,
                [candidates, completedArgs, partial, CompletionType.Argument, null])!;

            return false;
        }
    }
}
