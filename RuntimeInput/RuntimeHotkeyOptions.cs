namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Controls optional runtime hotkey router behavior for a single registration.
    /// </summary>
    public sealed class RuntimeHotkeyOptions
    {
        /// <summary>
        ///     Stable identifier for this hotkey registration.
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        ///     Optional human-readable display name for UI or help surfaces.
        /// </summary>
        public RuntimeHotkeyText? DisplayName { get; init; }

        /// <summary>
        ///     Optional human-readable description explaining what the hotkey does.
        /// </summary>
        public RuntimeHotkeyText? Description { get; init; }

        /// <summary>
        ///     Optional short semantic purpose string used for grouping or formatting.
        /// </summary>
        public string? Purpose { get; init; }

        /// <summary>
        ///     Optional UI-facing category used to group related hotkeys.
        /// </summary>
        public RuntimeHotkeyText? Category { get; init; }

        /// <summary>
        ///     When true, marks the input event as handled after the hotkey callback runs.
        /// </summary>
        public bool MarkInputHandled { get; init; }

        /// <summary>
        ///     When true, suppresses the hotkey while a text input control is actively editing.
        /// </summary>
        public bool SuppressWhenTextInputFocused { get; init; } = true;

        /// <summary>
        ///     When true, suppresses the hotkey while the developer console is visible.
        /// </summary>
        public bool SuppressWhenDevConsoleVisible { get; init; } = true;

        /// <summary>
        ///     Optional debug name included in registration logs.
        /// </summary>
        public string? DebugName { get; init; }
    }
}
