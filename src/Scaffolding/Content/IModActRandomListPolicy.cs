namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Controls whether a registered mod act can appear in vanilla act-list randomization.
    ///     </para>
    ///     <para xml:lang="zh-CN">控制已注册的模组章节是否可出现在游戏本体随机生成的章节列表中。</para>
    /// </summary>
    public interface IModActRandomListPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">True when this act is safe to appear organically in generated run act lists.</para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，该章节可以自然出现在一局游戏中随机生成的章节列表里。
        ///     </para>
        /// </summary>
        bool AllowInRandomActList { get; }
    }
}
