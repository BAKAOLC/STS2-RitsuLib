namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Preset corners for extra icon badges. Vanilla stack/value UI uses bottom-right; mods use the other three
    ///     corners, or <see cref="Custom" /> with explicit bounds on <see cref="ExtraIconAmountLabelSlot" />.
    /// </summary>
    public enum ExtraIconAmountLabelCorner
    {
        /// <summary>
        ///     Top-left band (built-in layout per host kind).
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Top-right band (built-in layout per host kind).
        /// </summary>
        TopRight,

        /// <summary>
        ///     Bottom-left band (built-in layout per host kind).
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Use <see cref="ExtraIconAmountLabelSlot.CustomRect" />; horizontal and vertical alignment are centered.
        /// </summary>
        Custom,
    }
}
