namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the semantic visual tone of a settings action button.</para>
    ///     <para xml:lang="zh-CN">指定设置操作按钮的语义视觉色调。</para>
    /// </summary>
    public enum ModSettingsButtonTone
    {
        /// <summary>
        ///     <para xml:lang="en">A neutral visual style.</para>
        ///     <para xml:lang="zh-CN">中性视觉样式。</para>
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     <para xml:lang="en">Primary or positive emphasis.</para>
        ///     <para xml:lang="zh-CN">主要或正向强调。</para>
        /// </summary>
        Accent = 1,

        /// <summary>
        ///     <para xml:lang="en">Destructive or high-attention emphasis.</para>
        ///     <para xml:lang="zh-CN">破坏性或需要高度注意的强调。</para>
        /// </summary>
        Danger = 2,
    }
}
