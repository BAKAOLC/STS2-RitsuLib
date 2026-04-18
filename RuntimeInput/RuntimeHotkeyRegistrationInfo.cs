namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Immutable snapshot describing one active runtime hotkey registration.
    /// </summary>
    public sealed record RuntimeHotkeyRegistrationInfo(
        string CurrentBinding,
        bool IsModifierOnly,
        string? Id,
        string? DisplayName,
        string? Description,
        string? Purpose,
        string? Category,
        bool MarkInputHandled,
        bool SuppressWhenTextInputFocused,
        bool SuppressWhenDevConsoleVisible,
        string? DebugName)
    {
        /// <summary>
        ///     All currently active bindings for this hotkey, in display order.
        /// </summary>
        public IReadOnlyList<string> CurrentBindings { get; init; } =
            string.IsNullOrWhiteSpace(CurrentBinding) ? [] : [CurrentBinding];

        /// <summary>
        ///     Per-binding modifier-only flags aligned with <see cref="CurrentBindings" />.
        /// </summary>
        public IReadOnlyList<bool> BindingModifierOnlyStates { get; init; } = [IsModifierOnly];
    }

    /// <summary>
    ///     Detailed immutable snapshot describing one active runtime hotkey registration, including all bindings.
    /// </summary>
    public sealed record RuntimeHotkeyRegistrationDetails(
        IReadOnlyList<string> CurrentBindings,
        IReadOnlyList<bool> BindingModifierOnlyStates,
        string? Id,
        string? DisplayName,
        string? Description,
        string? Purpose,
        string? Category,
        bool MarkInputHandled,
        bool SuppressWhenTextInputFocused,
        bool SuppressWhenDevConsoleVisible,
        string? DebugName)
    {
        /// <summary>
        ///     First active binding, kept for compatibility with single-binding consumers.
        /// </summary>
        public string CurrentBinding => CurrentBindings.FirstOrDefault() ?? string.Empty;

        /// <summary>
        ///     Whether the first active binding is modifier-only.
        /// </summary>
        public bool IsModifierOnly => BindingModifierOnlyStates.FirstOrDefault();

        /// <summary>
        ///     Down-converts this detailed snapshot to the legacy single-binding view.
        /// </summary>
        public RuntimeHotkeyRegistrationInfo ToRegistrationInfo()
        {
            return new(
                CurrentBinding,
                IsModifierOnly,
                Id,
                DisplayName,
                Description,
                Purpose,
                Category,
                MarkInputHandled,
                SuppressWhenTextInputFocused,
                SuppressWhenDevConsoleVisible,
                DebugName)
            {
                CurrentBindings = CurrentBindings,
                BindingModifierOnlyStates = BindingModifierOnlyStates,
            };
        }
    }
}
