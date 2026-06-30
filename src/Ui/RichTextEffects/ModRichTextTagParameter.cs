namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     BBCode parameter used by <see cref="ModRichTextTag" />.
    ///     <see cref="ModRichTextTag" /> 使用的 BBCode 参数。
    /// </summary>
    /// <param name="Name">
    ///     Parameter name, such as <c>seed</c> in <c>[glitch seed=123]</c>.
    ///     参数名，例如 <c>[glitch seed=123]</c> 中的 <c>seed</c>。
    /// </param>
    /// <param name="Value">
    ///     Parameter value. Null values are omitted.
    ///     参数值。null 值会被省略。
    /// </param>
    public readonly record struct ModRichTextTagParameter(string Name, object? Value);
}
