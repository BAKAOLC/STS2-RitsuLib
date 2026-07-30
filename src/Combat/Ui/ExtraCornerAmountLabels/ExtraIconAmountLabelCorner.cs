namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">Defines built-in and custom positions for an extra icon badge.</para>
    ///     <para xml:lang="zh-CN">定义额外图标角标的内置位置和自定义位置。</para>
    /// </summary>
    public enum ExtraIconAmountLabelCorner
    {
        /// <summary>
        ///     <para xml:lang="en">Uses the host's built-in top-left area.</para>
        ///     <para xml:lang="zh-CN">使用宿主的内置左上区域。</para>
        /// </summary>
        TopLeft,

        /// <summary>
        ///     <para xml:lang="en">Uses the host's built-in top-right area.</para>
        ///     <para xml:lang="zh-CN">使用宿主的内置右上区域。</para>
        /// </summary>
        TopRight,

        /// <summary>
        ///     <para xml:lang="en">Uses the host's built-in bottom-left area.</para>
        ///     <para xml:lang="zh-CN">使用宿主的内置左下区域。</para>
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     <para xml:lang="en">Uses the host's built-in bottom-right area.</para>
        ///     <para xml:lang="zh-CN">使用宿主的内置右下区域。</para>
        /// </summary>
        BottomRight,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses the entry's custom bounds with centered horizontal and vertical alignment.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用条目的自定义边界，并采用水平和垂直居中对齐。</para>
        /// </summary>
        Custom,
    }
}
