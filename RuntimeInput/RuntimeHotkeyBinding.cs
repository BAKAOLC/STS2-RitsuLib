using Godot;

namespace STS2RitsuLib.RuntimeInput
{
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
        Key PrimaryKey,
        ModifierRequirement Ctrl,
        ModifierRequirement Alt,
        ModifierRequirement Shift,
        ModifierRequirement Meta,
        string CanonicalString)
    {
        public bool IsModifierOnly => RuntimeHotkeyParser.IsModifierKey(PrimaryKey);

        public bool Matches(InputEventKey keyEvent)
        {
            if (!ModifiersMatch(keyEvent))
                return false;

            if (!PrimaryKeyMatches(keyEvent))
                return false;

            if (!IsModifierOnly)
                return true;

            return RuntimeHotkeyParser.GetModifierKindForKeyEvent(keyEvent) ==
                   RuntimeHotkeyParser.GetModifierKind(PrimaryKey);
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
            if (!IsModifierOnly)
                return keyEvent.Keycode == PrimaryKey;

            return RuntimeHotkeyParser.ModifierKeyMatches(PrimaryKey, keyEvent);
        }
    }
}
