namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Controls whether a registered mod act can appear in vanilla act-list randomization.
    ///     </para>
    ///     <para xml:lang="zh-CN">控制已注册 mod 章节是否可出现在原版章节列表随机逻辑中。</para>
    /// </summary>
    public interface IModActRandomListPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">True when this act is safe to appear organically in generated run act lists.</para>
        ///     <para xml:lang="zh-CN">为 true 时，该章节可自然出现在生成的 run 章节列表中。</para>
        /// </summary>
        bool AllowInRandomActList { get; }
    }
}
