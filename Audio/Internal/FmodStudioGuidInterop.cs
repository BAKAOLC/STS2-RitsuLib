namespace STS2RitsuLib.Audio.Internal
{
    internal static class FmodStudioGuidInterop
    {
        /// <summary>
        ///     Godot FMOD GDExtension parses GUIDs with <c>sscanf("{%8x-...}")</c> (see
        ///     <c>sts-2-source/fmod-gdextension/src/helpers/common.h</c>); the string must include braces.
        /// </summary>
        internal static bool TryNormalizeForAddon(string raw, out string bracedLowercase)
        {
            bracedLowercase = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var trimmed = raw.Trim();
            if (trimmed is ['{', _, ..] && trimmed[^1] == '}')
                trimmed = trimmed[1..^1].Trim();

            if (!Guid.TryParse(trimmed, out var guid))
                return false;

            bracedLowercase = guid.ToString("B");
            return true;
        }
    }
}
