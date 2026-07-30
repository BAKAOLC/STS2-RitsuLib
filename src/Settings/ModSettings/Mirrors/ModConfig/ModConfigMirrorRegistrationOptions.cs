namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Configures how ModConfig key binding entries are mirrored into RitsuLib settings.</para>
    ///     <para xml:lang="zh-CN">配置如何将 ModConfig 的按键绑定条目镜像到 RitsuLib 设置。</para>
    /// </summary>
    public sealed class ModConfigMirrorRegistrationOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the shared default option set.</para>
        ///     <para xml:lang="zh-CN">获取共享的默认选项集。</para>
        /// </summary>
        public static ModConfigMirrorRegistrationOptions Default { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">Gets whether mirrored key bindings allow modifier-and-key combinations.</para>
        ///     <para xml:lang="zh-CN">获取镜像按键绑定是否允许修饰键与普通按键的组合。</para>
        /// </summary>
        public bool KeyBindAllowModifierCombos { get; init; } = false;

        /// <summary>
        ///     <para xml:lang="en">Gets whether mirrored key bindings allow a modifier key by itself.</para>
        ///     <para xml:lang="zh-CN">获取镜像按键绑定是否允许单独使用修饰键。</para>
        /// </summary>
        public bool KeyBindAllowModifierOnly { get; init; } = false;

        /// <summary>
        ///     <para xml:lang="en">Gets whether mirrored key bindings distinguish left and right modifier keys.</para>
        ///     <para xml:lang="zh-CN">获取镜像按键绑定是否区分左侧与右侧修饰键。</para>
        /// </summary>
        public bool KeyBindDistinguishModifierSides { get; init; } = false;
    }
}
