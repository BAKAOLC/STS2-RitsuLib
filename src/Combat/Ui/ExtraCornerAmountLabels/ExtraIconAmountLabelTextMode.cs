namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">Defines how extra badge text is rendered.</para>
    ///     <para xml:lang="zh-CN">定义额外角标文本的渲染方式。</para>
    /// </summary>
    public enum ExtraIconAmountLabelTextMode
    {
        /// <summary>
        ///     <para xml:lang="en">Renders the text literally with <c>MegaLabel</c>.</para>
        ///     <para xml:lang="zh-CN">使用 <c>MegaLabel</c> 按字面渲染文本。</para>
        /// </summary>
        Plain,

        /// <summary>
        ///     <para xml:lang="en">Parses and renders the text as Godot/Mega rich text.</para>
        ///     <para xml:lang="zh-CN">将文本作为 Godot/Mega 富文本解析并渲染。</para>
        /// </summary>
        RichText,
    }
}
