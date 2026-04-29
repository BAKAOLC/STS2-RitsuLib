namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Central layout and chrome numbers for mod settings UI. Tune here instead of scattering literals.
    /// </summary>
    public static class ModSettingsUiMetrics
    {
        /// <summary>
        ///     Shared StyleBox corner radius (0 = square).
        /// </summary>
        public const int CornerRadius = 0;

        /// <summary>
        ///     Default minimum width for compact value widgets (toggle, dropdown, buttons).
        /// </summary>
        public const float EntryValueMinWidth = 200f;

        /// <summary>
        ///     Fixed vertical size for value column widgets (no vertical expand).
        /// </summary>
        public const float EntryValueMinHeight = 44f;

        /// <summary>
        ///     Slider row: wide enough for a usable track plus value field.
        /// </summary>
        public const float SliderRowMinWidth = 348f;

        /// <summary>
        ///     Minimum horizontal space reserved for the HSlider track (within slider row).
        /// </summary>
        public const float SliderTrackMinWidth = 220f;

        /// <summary>
        ///     Stepper row total width; center label area.
        /// </summary>
        public const float ChoiceRowMinWidth = 292f;

        /// <summary>
        ///     Minimum width reserved for the center label/value area of a choice row.
        /// </summary>
        public const float ChoiceCenterMinWidth = 180f;

        /// <summary>
        ///     Width of the inline numeric field shown next to sliders.
        /// </summary>
        public const float SliderValueFieldWidth = 72f;

        /// <summary>
        ///     Height of the inline numeric field shown next to sliders.
        /// </summary>
        public const float SliderValueFieldHeight = 40f;

        /// <summary>
        ///     Minimum width reserved for color picker rows.
        /// </summary>
        public const float ColorRowMinWidth = 300f;

        /// <summary>
        ///     Size of the color swatch shown by color picker controls.
        /// </summary>
        public const float ColorSwatchSize = 40f;

        /// <summary>
        ///     Single-line string entry (LineEdit) minimum width in the value column.
        /// </summary>
        public const float StringEntryMinWidth = 320f;

        /// <summary>
        ///     Multiline string entry (Godot TextEdit) minimum height.
        /// </summary>
        public const float StringEntryMultilineMinHeight = 104f;

        /// <summary>
        ///     Keybinding block; wide enough for several modifiers plus key name.
        /// </summary>
        public const float KeybindingBlockWidth = 400f;

        /// <summary>
        ///     Minimum width of the capture button; grows with value column via ExpandFill.
        /// </summary>
        public const float KeybindingCaptureMinWidth = 300f;

        /// <summary>
        ///     Font size used by keybinding helper text.
        /// </summary>
        public const int KeybindingHintFontSize = 16;

        /// <summary>
        ///     Square size used by compact stepper buttons.
        /// </summary>
        public const int MiniStepperButtonSize = 40;
    }
}
