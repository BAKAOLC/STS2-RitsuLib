using System.Globalization;
using Godot;

namespace STS2RitsuLib.RuntimeInput
{
    internal static class RuntimeHotkeyParser
    {
        private const string ActionPrefix = "action:";

        public static bool TryParse(string? text, out RuntimeHotkeyBinding binding, out string normalized)
        {
            binding = default;
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (TryParseActionBinding(text, out binding, out normalized))
                return true;

            var parts = text.Split('+', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || parts.Any(string.IsNullOrEmpty))
                return false;

            var ctrl = ModifierRequirement.NotPressed;
            var alt = ModifierRequirement.NotPressed;
            var shift = ModifierRequirement.NotPressed;
            var meta = ModifierRequirement.NotPressed;
            Key? primaryKey = null;
            var seenModifiers = new HashSet<ModifierKind>();

            foreach (var rawPart in parts)
            {
                var token = rawPart.Trim();
                if (TryParseModifierToken(token, out var kind, out var requirement))
                {
                    if (!seenModifiers.Add(kind))
                        return false;

                    switch (kind)
                    {
                        case ModifierKind.Ctrl:
                            ctrl = requirement;
                            break;
                        case ModifierKind.Alt:
                            alt = requirement;
                            break;
                        case ModifierKind.Shift:
                            shift = requirement;
                            break;
                        case ModifierKind.Meta:
                            meta = requirement;
                            break;
                    }

                    if (parts.Length == 1)
                        primaryKey = ModifierKindToPrimaryKey(kind);
                    continue;
                }

                if (primaryKey != null || !TryParseKeyToken(token, out var parsedKey))
                    return false;
                primaryKey = parsedKey;
            }

            primaryKey ??= ctrl != ModifierRequirement.NotPressed
                ? ModifierKindToPrimaryKey(ModifierKind.Ctrl)
                : alt != ModifierRequirement.NotPressed
                    ? ModifierKindToPrimaryKey(ModifierKind.Alt)
                    : shift != ModifierRequirement.NotPressed
                        ? ModifierKindToPrimaryKey(ModifierKind.Shift)
                        : meta != ModifierRequirement.NotPressed
                            ? ModifierKindToPrimaryKey(ModifierKind.Meta)
                            : null;

            if (primaryKey == null)
                return false;

            normalized = BuildCanonicalString(primaryKey.Value, ctrl, alt, shift, meta);
            binding = new(RuntimeHotkeyBindingKind.Key, primaryKey.Value, ctrl, alt, shift, meta, null, normalized);
            return true;
        }

        public static string ActionBinding(string actionName)
        {
            var normalizedAction = NormalizeActionName(actionName);
            return string.IsNullOrWhiteSpace(normalizedAction) ? string.Empty : $"{ActionPrefix}{normalizedAction}";
        }

        public static string NormalizeOrDefault(string? text, string fallback)
        {
            return TryParse(text, out _, out var normalized) ? normalized : fallback;
        }

        internal static bool IsModifierKey(Key key)
        {
            return GetModifierKind(key) != ModifierKind.None;
        }

        internal static ModifierKind GetModifierKind(Key key)
        {
            var name = key.ToString().ToLowerInvariant();
            if (name.Contains("ctrl") || name.Contains("control"))
                return ModifierKind.Ctrl;
            if (name.Contains("alt"))
                return ModifierKind.Alt;
            if (name.Contains("shift"))
                return ModifierKind.Shift;
            if (name.Contains("meta") || name.Contains("cmd") || name.Contains("command"))
                return ModifierKind.Meta;
            return ModifierKind.None;
        }

        internal static ModifierKind GetModifierKindForKeyEvent(InputEventKey keyEvent)
        {
            return GetModifierKind(keyEvent.PhysicalKeycode) is var physicalKind and not ModifierKind.None
                ? physicalKind
                : GetModifierKind(keyEvent.Keycode);
        }

        internal static bool ModifierStateMatches(
            ModifierKind kind,
            ModifierRequirement requirement,
            InputEventKey keyEvent,
            RuntimeModifierSideState modifierSides)
        {
            return requirement switch
            {
                ModifierRequirement.NotPressed => !IsModifierPressed(kind, keyEvent),
                ModifierRequirement.AnySide => IsModifierPressed(kind, keyEvent),
                ModifierRequirement.LeftOnly or ModifierRequirement.RightOnly =>
                    IsModifierPressed(kind, keyEvent) && modifierSides.IsPressed(kind, requirement),
                _ => false,
            };
        }

        private static bool IsModifierPressed(ModifierKind kind, InputEventKey keyEvent)
        {
            if (GetModifierKindForKeyEvent(keyEvent) == kind)
                return true;

            return kind switch
            {
                ModifierKind.Ctrl => keyEvent.CtrlPressed,
                ModifierKind.Alt => keyEvent.AltPressed,
                ModifierKind.Shift => keyEvent.ShiftPressed,
                ModifierKind.Meta => keyEvent.MetaPressed,
                _ => false,
            };
        }

        private static bool TryParseModifierToken(string token, out ModifierKind kind,
            out ModifierRequirement requirement)
        {
            var normalized = token.Replace("_", string.Empty, true, CultureInfo.InvariantCulture)
                .Replace("-", string.Empty, true, CultureInfo.InvariantCulture)
                .ToLowerInvariant();

            switch (normalized)
            {
                case "ctrl":
                case "control":
                    kind = ModifierKind.Ctrl;
                    requirement = ModifierRequirement.AnySide;
                    return true;
                case "leftctrl":
                case "leftcontrol":
                case "lctrl":
                case "lcontrol":
                    kind = ModifierKind.Ctrl;
                    requirement = ModifierRequirement.LeftOnly;
                    return true;
                case "rightctrl":
                case "rightcontrol":
                case "rctrl":
                case "rcontrol":
                    kind = ModifierKind.Ctrl;
                    requirement = ModifierRequirement.RightOnly;
                    return true;
                case "alt":
                    kind = ModifierKind.Alt;
                    requirement = ModifierRequirement.AnySide;
                    return true;
                case "leftalt":
                case "lalt":
                    kind = ModifierKind.Alt;
                    requirement = ModifierRequirement.LeftOnly;
                    return true;
                case "rightalt":
                case "ralt":
                    kind = ModifierKind.Alt;
                    requirement = ModifierRequirement.RightOnly;
                    return true;
                case "shift":
                    kind = ModifierKind.Shift;
                    requirement = ModifierRequirement.AnySide;
                    return true;
                case "leftshift":
                case "lshift":
                    kind = ModifierKind.Shift;
                    requirement = ModifierRequirement.LeftOnly;
                    return true;
                case "rightshift":
                case "rshift":
                    kind = ModifierKind.Shift;
                    requirement = ModifierRequirement.RightOnly;
                    return true;
                case "meta":
                case "cmd":
                case "command":
                    kind = ModifierKind.Meta;
                    requirement = ModifierRequirement.AnySide;
                    return true;
                case "leftmeta":
                case "leftcmd":
                case "leftcommand":
                case "lmeta":
                case "lcmd":
                    kind = ModifierKind.Meta;
                    requirement = ModifierRequirement.LeftOnly;
                    return true;
                case "rightmeta":
                case "rightcmd":
                case "rightcommand":
                case "rmeta":
                case "rcmd":
                    kind = ModifierKind.Meta;
                    requirement = ModifierRequirement.RightOnly;
                    return true;
                default:
                    kind = ModifierKind.None;
                    requirement = ModifierRequirement.NotPressed;
                    return false;
            }
        }

        private static bool TryParseKeyToken(string token, out Key key)
        {
            if (Enum.TryParse(token, true, out key))
                return true;

            foreach (var candidate in Enum.GetValues<Key>())
                if (string.Equals(OS.GetKeycodeString(candidate), token, StringComparison.OrdinalIgnoreCase))
                {
                    key = candidate;
                    return true;
                }

            return false;
        }

        private static bool TryParseActionBinding(string text, out RuntimeHotkeyBinding binding, out string normalized)
        {
            binding = default;
            normalized = string.Empty;

            var trimmed = text.Trim();
            if (!trimmed.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var actionName = NormalizeActionName(trimmed[ActionPrefix.Length..]);
            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            normalized = $"{ActionPrefix}{actionName}";
            binding = new(
                RuntimeHotkeyBindingKind.Action,
                Key.None,
                ModifierRequirement.NotPressed,
                ModifierRequirement.NotPressed,
                ModifierRequirement.NotPressed,
                ModifierRequirement.NotPressed,
                actionName,
                normalized);
            return true;
        }

        private static string NormalizeActionName(string? actionName)
        {
            return actionName?.Trim() ?? string.Empty;
        }

        private static string BuildCanonicalString(Key primaryKey, ModifierRequirement ctrl, ModifierRequirement alt,
            ModifierRequirement shift, ModifierRequirement meta)
        {
            var parts = new List<string>();
            if (ctrl != ModifierRequirement.NotPressed && !SameModifierAsPrimary(primaryKey, ModifierKind.Ctrl))
                parts.Add(RequirementToken(ModifierKind.Ctrl, ctrl));
            if (alt != ModifierRequirement.NotPressed && !SameModifierAsPrimary(primaryKey, ModifierKind.Alt))
                parts.Add(RequirementToken(ModifierKind.Alt, alt));
            if (shift != ModifierRequirement.NotPressed && !SameModifierAsPrimary(primaryKey, ModifierKind.Shift))
                parts.Add(RequirementToken(ModifierKind.Shift, shift));
            if (meta != ModifierRequirement.NotPressed && !SameModifierAsPrimary(primaryKey, ModifierKind.Meta))
                parts.Add(RequirementToken(ModifierKind.Meta, meta));
            parts.Add(PrimaryKeyToken(primaryKey, ctrl, alt, shift, meta));
            return string.Join('+', parts);
        }

        private static bool SameModifierAsPrimary(Key primaryKey, ModifierKind kind)
        {
            return GetModifierKind(primaryKey) == kind;
        }

        private static string RequirementToken(ModifierKind kind, ModifierRequirement requirement)
        {
            return requirement switch
            {
                ModifierRequirement.AnySide => kind.ToString(),
                ModifierRequirement.LeftOnly => $"Left{kind}",
                ModifierRequirement.RightOnly => $"Right{kind}",
                _ => string.Empty,
            };
        }

        private static string PrimaryKeyToken(
            Key key,
            ModifierRequirement ctrl,
            ModifierRequirement alt,
            ModifierRequirement shift,
            ModifierRequirement meta)
        {
            return GetModifierKind(key) switch
            {
                ModifierKind.Ctrl => RequirementToken(ModifierKind.Ctrl, ctrl),
                ModifierKind.Alt => RequirementToken(ModifierKind.Alt, alt),
                ModifierKind.Shift => RequirementToken(ModifierKind.Shift, shift),
                ModifierKind.Meta => RequirementToken(ModifierKind.Meta, meta),
                _ => key.ToString(),
            };
        }

        private static Key ModifierKindToPrimaryKey(ModifierKind kind)
        {
            return kind switch
            {
                ModifierKind.Ctrl => Key.Ctrl,
                ModifierKind.Alt => Key.Alt,
                ModifierKind.Shift => Key.Shift,
                ModifierKind.Meta => Key.Meta,
                _ => Key.None,
            };
        }
    }
}
