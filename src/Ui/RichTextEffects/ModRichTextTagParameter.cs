namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     <para xml:lang="en">BBCode parameter used by <see cref="ModRichTextTag" />.</para>
    ///     <para xml:lang="zh-CN"><see cref="ModRichTextTag" /> 使用的 BBCode 参数。</para>
    /// </summary>
    /// <param name="Name">
    ///     <para xml:lang="en">Parameter name, such as <c>seed</c> in <c>[glitch seed=123]</c>.</para>
    ///     <para xml:lang="zh-CN">参数名，例如 <c>[glitch seed=123]</c> 中的 <c>seed</c>。</para>
    /// </param>
    /// <param name="Value">
    ///     <para xml:lang="en">Parameter value. <see langword="null" /> values are omitted.</para>
    ///     <para xml:lang="zh-CN">参数值。值为 <see langword="null" /> 时会被省略。</para>
    /// </param>
    public readonly record struct ModRichTextTagParameter(string Name, object? Value);
}
