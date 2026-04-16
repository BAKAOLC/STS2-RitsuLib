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
        string? DebugName);
}
