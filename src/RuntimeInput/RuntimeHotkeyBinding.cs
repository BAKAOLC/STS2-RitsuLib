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

    internal struct RuntimeModifierSideState
    {
        private int _pressedSides;

        public void Update(InputEventKey keyEvent)
        {
            ClearReleasedModifiers(keyEvent);

            var kind = RuntimeHotkeyParser.GetModifierKindForKeyEvent(keyEvent);
            var sideMask = GetSideMask(kind, keyEvent.Location);
            if (sideMask == 0)
                return;

            if (keyEvent.Pressed)
                _pressedSides |= sideMask;
            else
                _pressedSides &= ~sideMask;
        }

        public readonly bool IsPressed(ModifierKind kind, ModifierRequirement requirement)
        {
            var location = requirement switch
            {
                ModifierRequirement.LeftOnly => KeyLocation.Left,
                ModifierRequirement.RightOnly => KeyLocation.Right,
                _ => KeyLocation.Unspecified,
            };
            var sideMask = GetSideMask(kind, location);
            return sideMask != 0 && (_pressedSides & sideMask) != 0;
        }

        private void ClearReleasedModifiers(InputEventKey keyEvent)
        {
            if (!keyEvent.CtrlPressed)
                Clear(ModifierKind.Ctrl);
            if (!keyEvent.AltPressed)
                Clear(ModifierKind.Alt);
            if (!keyEvent.ShiftPressed)
                Clear(ModifierKind.Shift);
            if (!keyEvent.MetaPressed)
                Clear(ModifierKind.Meta);
        }

        private void Clear(ModifierKind kind)
        {
            _pressedSides &= ~GetKindMask(kind);
        }

        private static int GetKindMask(ModifierKind kind)
        {
            return kind == ModifierKind.None ? 0 : 3 << (((int)kind - 1) * 2);
        }

        private static int GetSideMask(ModifierKind kind, KeyLocation location)
        {
            if (kind == ModifierKind.None || location is not (KeyLocation.Left or KeyLocation.Right))
                return 0;

            var sideOffset = location == KeyLocation.Right ? 1 : 0;
            return 1 << (((int)kind - 1) * 2 + sideOffset);
        }
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

        public bool Matches(InputEventKey keyEvent, RuntimeModifierSideState modifierSides)
        {
            if (Kind != RuntimeHotkeyBindingKind.Key)
                return false;

            if (!ModifiersMatch(keyEvent, modifierSides))
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

        private bool ModifiersMatch(InputEventKey keyEvent, RuntimeModifierSideState modifierSides)
        {
            return RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Ctrl, Ctrl, keyEvent, modifierSides)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Alt, Alt, keyEvent, modifierSides)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Shift, Shift, keyEvent, modifierSides)
                   && RuntimeHotkeyParser.ModifierStateMatches(ModifierKind.Meta, Meta, keyEvent, modifierSides);
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
