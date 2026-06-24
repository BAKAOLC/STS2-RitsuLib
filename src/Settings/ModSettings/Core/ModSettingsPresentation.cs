namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     One labeled option for choice / enum settings.
    ///     选择/枚举设置中的一个带标签选项。
    /// </summary>
    /// <typeparam name="TValue">
    ///     Stored option value.
    ///     存储的选项值。
    /// </typeparam>
    /// <param name="Value">
    ///     Value written to the binding when selected.
    ///     被选中时写入绑定的值。
    /// </param>
    /// <param name="Label">
    ///     Visible caption.
    ///     可见标题。
    /// </param>
    public readonly record struct ModSettingsChoiceOption<TValue>(TValue Value, ModSettingsText Label);

    /// <summary>
    ///     How multi-option settings are rendered in the value column.
    ///     多选项设置在值列中的渲染方式。
    /// </summary>
    public enum ModSettingsChoicePresentation
    {
        /// <summary>
        ///     Left/right stepper with centered label.
        ///     左/右步进器，中间显示标签。
        /// </summary>
        Stepper = 0,

        /// <summary>
        ///     Dropdown list.
        ///     下拉列表。
        /// </summary>
        Dropdown = 1,
    }

    /// <summary>
    ///     Semantic tone for settings action buttons.
    ///     设置操作按钮的语义色调。
    /// </summary>
    public enum ModSettingsButtonTone
    {
        /// <summary>
        ///     Neutral chrome.
        ///     中性 chrome。
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     Primary / positive emphasis.
        ///     主要/正向强调。
        /// </summary>
        Accent = 1,

        /// <summary>
        ///     Destructive or high-attention actions.
        ///     破坏性或需要高注意力的操作。
        /// </summary>
        Danger = 2,
    }
}
