namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Rendering mode for <see cref="ExtraIconAmountLabelSlot.Text" />.
    ///     <see cref="ExtraIconAmountLabelSlot.Text" /> 的渲染模式。
    /// </summary>
    public enum ExtraIconAmountLabelTextMode
    {
        /// <summary>
        ///     Render with <c>MegaLabel</c>; text is shown literally.
        ///     使用 <c>MegaLabel</c> 渲染；文本按字面显示。
        /// </summary>
        Plain,

        /// <summary>
        ///     Render with <c>MegaRichTextLabel</c>; text is parsed as Godot/Mega rich text.
        ///     使用 <c>MegaRichTextLabel</c> 渲染；文本按 Godot/Mega 富文本解析。
        /// </summary>
        RichText,
    }
}
