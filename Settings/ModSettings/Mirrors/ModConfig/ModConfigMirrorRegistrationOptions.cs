namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Optional behavior for ModConfig mirror registration.
    /// </summary>
    public sealed class ModConfigMirrorRegistrationOptions
    {
        /// <summary>
        ///     Default options used when none are passed.
        /// </summary>
        public static ModConfigMirrorRegistrationOptions Default { get; } = new();

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        /// </summary>
        public bool KeyBindAllowModifierCombos { get; init; } = true;

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        /// </summary>
        public bool KeyBindAllowModifierOnly { get; init; } = true;

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        /// </summary>
        public bool KeyBindDistinguishModifierSides { get; init; } = false;
    }
}
