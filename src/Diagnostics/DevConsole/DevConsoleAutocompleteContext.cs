using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Identifies a dev-console <see cref="AbstractConsoleCmd.CompleteArgument" /> invocation.
    /// </summary>
    public sealed class DevConsoleAutocompleteContext
    {
        /// <summary>
        ///     Creates a context for the active completion call.
        /// </summary>
        public DevConsoleAutocompleteContext(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CompletedArgs = completedArgs ?? throw new ArgumentNullException(nameof(completedArgs));
            ArgumentIndex = argumentIndex;
        }

        /// <summary>
        ///     Console command producing completions.
        /// </summary>
        public AbstractConsoleCmd Command { get; }

        /// <summary>
        ///     Arguments already present before the token being completed.
        /// </summary>
        public IReadOnlyList<string> CompletedArgs { get; }

        /// <summary>
        ///     Zero-based index of the argument being completed.
        /// </summary>
        public int ArgumentIndex { get; }

        /// <summary>
        ///     Dev-console command name.
        /// </summary>
        public string CommandName => Command.CmdName;
    }
}
