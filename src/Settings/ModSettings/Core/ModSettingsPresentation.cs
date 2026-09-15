namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Pairs one choice or enum value with its display text.</para>
    ///     <para xml:lang="zh-CN">将一个选项或枚举值与其显示文本配对。</para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The option value type.</para>
    ///     <para xml:lang="zh-CN">选项值类型。</para>
    /// </typeparam>
    /// <param name="Value">
    ///     <para xml:lang="en">The value written to the binding when selected.</para>
    ///     <para xml:lang="zh-CN">选中时写入绑定的值。</para>
    /// </param>
    /// <param name="Label">
    ///     <para xml:lang="en">The text displayed for the option.</para>
    ///     <para xml:lang="zh-CN">为选项显示的文本。</para>
    /// </param>
    public readonly record struct ModSettingsChoiceOption<TValue>(TValue Value, ModSettingsText Label);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how a setting with multiple choices is rendered in the value column.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定具有多个候选项的设置在值列中的呈现方式。
    ///     </para>
    /// </summary>
    public enum ModSettingsChoicePresentation
    {
        /// <summary>
        ///     <para xml:lang="en">A left/right stepper with a centered label.</para>
        ///     <para xml:lang="zh-CN">标签居中的左右步进器。</para>
        /// </summary>
        Stepper = 0,

        /// <summary>
        ///     <para xml:lang="en">A drop-down list.</para>
        ///     <para xml:lang="zh-CN">下拉列表。</para>
        /// </summary>
        Dropdown = 1,
    }
}
