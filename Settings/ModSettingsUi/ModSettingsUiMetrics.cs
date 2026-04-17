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

        /// <summary>
        ///     Minimum width of the left sidebar column (base 324px + one third).
        /// </summary>
        public const float SidebarPanelMinWidth = 432f;

        /// <summary>
        ///     Horizontal inset (px) for sidebar scroll content and the top mod info card so edges line up.
        /// </summary>
        public const int SidebarContentMarginH = 16;

        /// <summary>
        ///     Left accent bar width (px) on the selected mod row in the sidebar mod list.
        /// </summary>
        public const float SidebarModAccentBarWidth = 6f;

        /// <summary>
        ///     Extra gap (px) between the accent bar and the mod title text when selected.
        /// </summary>
        public const int SidebarModAccentTextGutter = 6;

        /// <summary>
        ///     Shared white alpha for mod list rows: selected strip fill and unselected bottom divider.
        /// </summary>
        public const float SidebarModListSubtleAlpha = 0.04f;

        /// <summary>
        ///     Bottom border thickness (px) for unselected mod list rows (ModGroup).
        /// </summary>
        public const int SidebarModListBottomBorderWidth = 1;

        /// <summary>
        ///     Horizontal rule above the sidebar mod list (aligned with ModGroup row weight; not heavier than 2px).
        /// </summary>
        public const float SidebarScrollTopDividerHeight = 2f;

        /// <summary>
        ///     Margin (px) above and below the card-to-list divider row (rule is its own mainVBox sibling
        ///     between the info card and the scroll; total gutter = 2×this + <see cref="SidebarScrollTopDividerHeight" />).
        /// </summary>
        public const int SidebarListDividerPadSymmetric = 8;

        /// <summary>
        ///     Font size for the mod title row version pill (caps tag).
        /// </summary>
        public const int SidebarModVersionBadgeFontSize = 11;

        /// <summary>
        ///     Sidebar mod cover / placeholder preview: square (1:1) edge length in px.
        /// </summary>
        public const float ModSidebarPreviewOuterSize = 96f;

    }
}
