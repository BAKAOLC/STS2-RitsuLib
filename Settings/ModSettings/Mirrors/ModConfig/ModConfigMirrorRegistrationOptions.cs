namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Optional behavior for ModConfig mirror registration.
    ///     可选 behavior 用于 ModConfig mirror 注册.
    /// </summary>
    public sealed class ModConfigMirrorRegistrationOptions
    {
        /// <summary>
        ///     Default options used when none are passed.
        ///     未传入选项时使用的默认选项。
        /// </summary>
        public static ModConfigMirrorRegistrationOptions Default { get; } = new();

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        ///     转发给镜像的按键绑定条目。
        /// </summary>
        public bool KeyBindAllowModifierCombos { get; init; } = false;

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        ///     转发给镜像的按键绑定条目。
        /// </summary>
        public bool KeyBindAllowModifierOnly { get; init; } = false;

        /// <summary>
        ///     Forwarded to mirrored key binding entries.
        ///     转发给镜像的按键绑定条目。
        /// </summary>
        public bool KeyBindDistinguishModifierSides { get; init; } = false;
    }
}
