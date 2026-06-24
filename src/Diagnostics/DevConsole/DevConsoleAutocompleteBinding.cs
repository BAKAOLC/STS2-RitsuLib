namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Binds autocomplete enhancements to a command argument slot.
    /// </summary>
    public sealed class DevConsoleAutocompleteBinding
    {
        /// <summary>
        ///     Dev-console command name (for example <c>card</c>).
        /// </summary>
        public required string CommandName { get; init; }

        /// <summary>
        ///     When set, only this argument index receives the enhancements.
        /// </summary>
        public int? ArgumentIndex { get; init; }

        /// <summary>
        ///     Optional extra guard based on completed arguments and command state.
        /// </summary>
        public Func<DevConsoleAutocompleteContext, bool>? AppliesWhen { get; init; }

        /// <summary>
        ///     Enhancements applied when this binding matches.
        /// </summary>
        public DevConsoleAutocompleteEnhancements Enhancements { get; init; }
    }
}
