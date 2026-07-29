using Godot;

namespace STS2RitsuLib.RuntimeInput
{
    internal enum RuntimeHotkeyBindingKind
    {
        Key = 0,
        Action = 1,
    }

    internal enum ModifierRequirement
    {
        NotPressed = 0,
        AnySide = 1,
        LeftOnly = 2,
        RightOnly = 3,
    }

    internal enum ModifierKind
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 3,
        Meta = 4,
    }

    internal readonly record struct RuntimeHotkeyBinding(
        RuntimeHotkeyBindingKind Kind,
        Key PrimaryKey,
        ModifierRequirement Ctrl,
        ModifierRequirement Alt,
        ModifierRequirement Shift,
        ModifierRequirement Meta,
        string? ActionName,
        string CanonicalString)
    {
        public bool IsModifierOnly => Kind == RuntimeHotkeyBindingKind.Key &&
                                      RuntimeHotkeyParser.IsModifierKey(PrimaryKey);

        public bool Matches(InputEventKey keyEvent)
        {
            if (Kind != RuntimeHotkeyBindingKind.Key)
                return false;

            if (!ModifiersMatch(keyEvent))
                return false;

            return IsModifierOnly
                ? IsRequiredModifierEvent(keyEvent)
                : PrimaryKeyMatches(keyEvent);
        }

        public bool Matches(InputEventAction actionEvent)
        {
            return Kind == RuntimeHotkeyBindingKind.Action &&
                   actionEvent.Pressed &&
                   !string.IsNullOrWhiteSpace(ActionName) &&
                   string.Equals(actionEvent.Action.ToString(), ActionName, StringComparison.Ordinal);
        }

        private bool ModifiersMatch(InputEventKey keyEvent)
        {
            return RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Ctrl, Ctrl, keyEvent)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Alt, Alt, keyEvent)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Shift, Shift, keyEvent)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Meta, Meta, keyEvent);
        }

        private bool PrimaryKeyMatches(InputEventKey keyEvent)
        {
            return keyEvent.Keycode == PrimaryKey || keyEvent.PhysicalKeycode == PrimaryKey;
        }

        private bool IsRequiredModifierEvent(InputEventKey keyEvent)
        {
            return RuntimeHotkeyParser.GetModifierKindForKeyEvent(keyEvent) switch
            {
                ModifierKind.Ctrl => Ctrl != ModifierRequirement.NotPressed,
                ModifierKind.Alt => Alt != ModifierRequirement.NotPressed,
                ModifierKind.Shift => Shift != ModifierRequirement.NotPressed,
                ModifierKind.Meta => Meta != ModifierRequirement.NotPressed,
                _ => false,
            };
        }
    }
}
