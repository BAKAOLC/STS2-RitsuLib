namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     One labeled option for choice / enum settings.
    /// </summary>
    /// <typeparam name="TValue">Stored option value.</typeparam>
    /// <param name="Value">Value written to the binding when selected.</param>
    /// <param name="Label">Visible caption.</param>
    public readonly record struct ModSettingsChoiceOption<TValue>(TValue Value, ModSettingsText Label);

    /// <summary>
    ///     How multi-option settings are rendered in the value column.
    /// </summary>
    public enum ModSettingsChoicePresentation
    {
        /// <summary>
        ///     Left/right stepper with centered label.
        /// </summary>
        Stepper = 0,

        /// <summary>
        ///     Dropdown list.
        /// </summary>
        Dropdown = 1,
    }

    /// <summary>
    ///     Semantic tone for settings action buttons.
    /// </summary>
    public enum ModSettingsButtonTone
    {
        /// <summary>
        ///     Neutral chrome.
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     Primary / positive emphasis.
        /// </summary>
        Accent = 1,

        /// <summary>
        ///     Destructive or high-attention actions.
        /// </summary>
        Danger = 2,
    }
}
