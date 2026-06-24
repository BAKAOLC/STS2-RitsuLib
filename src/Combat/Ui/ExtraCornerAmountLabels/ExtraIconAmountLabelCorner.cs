namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Preset corners for extra icon badges. Vanilla stack/value UI uses bottom-right; mods use the other three
    ///     corners, or <see cref="Custom" /> with explicit bounds on <see cref="ExtraIconAmountLabelSlot" />.
    ///     额外图标徽标的预设角。原版层数/数值 UI 使用右下角；mod 使用另外三个
    ///     角，或使用带 <see cref="ExtraIconAmountLabelSlot" /> 显式边界的 <see cref="Custom" />。
    /// </summary>
    public enum ExtraIconAmountLabelCorner
    {
        /// <summary>
        ///     Top-left band (built-in layout per host kind).
        ///     左上带区（按宿主类型使用内置布局）。
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Top-right band (built-in layout per host kind).
        ///     右上带区（按宿主类型使用内置布局）。
        /// </summary>
        TopRight,

        /// <summary>
        ///     Bottom-left band (built-in layout per host kind).
        ///     左下带区（按宿主类型使用内置布局）。
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Bottom-right band (built-in layout per host kind).
        ///     右下带区（按宿主类型使用内置布局）。
        /// </summary>
        BottomRight,

        /// <summary>
        ///     Use <see cref="ExtraIconAmountLabelSlot.CustomRect" />; horizontal and vertical alignment are centered.
        ///     使用 <see cref="ExtraIconAmountLabelSlot.CustomRect" />；水平和垂直对齐均居中。
        /// </summary>
        Custom,
    }
}
