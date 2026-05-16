using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Public entry point for registering and applying dev-console autocomplete enhancements.
    /// </summary>
    public static class DevConsoleAutocomplete
    {
        /// <summary>
        ///     Registers a command-argument autocomplete binding.
        /// </summary>
        public static void Register(DevConsoleAutocompleteBinding binding)
        {
            DevConsoleAutocompleteRegistry.Register(binding);
        }

        /// <summary>
        ///     Registers enhancements for a fixed argument index on a command.
        /// </summary>
        public static void Register(
            string commandName,
            int argumentIndex,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool>? appliesWhen = null)
        {
            DevConsoleAutocompleteRegistry.Register(commandName, argumentIndex, enhancements, appliesWhen);
        }

        /// <summary>
        ///     Registers enhancements selected by <paramref name="appliesWhen" />.
        /// </summary>
        public static void Register(
            string commandName,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool> appliesWhen)
        {
            DevConsoleAutocompleteRegistry.Register(commandName, enhancements, appliesWhen);
        }

        /// <summary>
        ///     Resolves enhancements for a completion call.
        /// </summary>
        public static DevConsoleAutocompleteEnhancements Resolve(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            return DevConsoleAutocompleteRegistry.Resolve(command, completedArgs, argumentIndex);
        }

        /// <summary>
        ///     Builds the match predicate chain for manual <c>CompleteArgument</c> usage in mod commands.
        /// </summary>
        public static Func<string, string, bool>? BuildMatchPredicate(
            AbstractConsoleCmd command,
            string[] completedArgs,
            Func<string, string, bool>? inner = null)
        {
            return DevConsoleAutocompleteEnhancer.BuildMatchPredicate(
                Resolve(command, completedArgs, completedArgs.Length),
                inner);
        }

        /// <summary>
        ///     Applies result enhancements for manual <c>CompleteArgument</c> usage in mod commands.
        /// </summary>
        public static void ApplyToResult(
            AbstractConsoleCmd command,
            string[] completedArgs,
            ref CompletionResult result)
        {
            DevConsoleAutocompleteEnhancer.ApplyToResult(
                ref result,
                Resolve(command, completedArgs, result.ArgumentIndex));
        }
    }
}
