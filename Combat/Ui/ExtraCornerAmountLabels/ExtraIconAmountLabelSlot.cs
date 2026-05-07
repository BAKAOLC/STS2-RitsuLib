using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     One extra amount/text badge on a combat UI icon. Each slot is laid out independently.
    /// </summary>
    /// <param name="Text">Badge text. Whitespace-only entries are skipped.</param>
    /// <param name="Corner">
    ///     <see cref="ExtraIconAmountLabelCorner.TopLeft" />, <see cref="ExtraIconAmountLabelCorner.TopRight" />, or
    ///     <see cref="ExtraIconAmountLabelCorner.BottomLeft" /> for built-in layout, or
    ///     <see cref="ExtraIconAmountLabelCorner.Custom" /> with <paramref name="CustomRect" />.
    /// </param>
    /// <param name="CustomRect">
    ///     When <paramref name="Corner" /> is <see cref="ExtraIconAmountLabelCorner.Custom" />, local rectangle
    ///     (position and size in host control space, same convention as <c>offset_*</c> for a top-left-anchored child).
    ///     Ignored for presets. Entries with non-positive width or height are skipped at runtime.
    /// </param>
    /// <param name="FontColor">
    ///     When set, overrides the foreground color for this badge after base typography is copied from the host
    ///     reference label. When <see langword="null" />, keeps the inherited color from that reference (default).
    /// </param>
    /// <param name="FontOutlineColor">
    ///     When set, overrides the outline color after base typography is copied. When <see langword="null" />, keeps
    ///     the inherited outline from that reference (default).
    /// </param>
    public readonly record struct ExtraIconAmountLabelSlot(
        string Text,
        ExtraIconAmountLabelCorner Corner,
        Rect2 CustomRect,
        Color? FontColor,
        Color? FontOutlineColor)
    {
        /// <summary>
        ///     Preset-corner slot: default <c>CustomRect</c>, no color overrides.
        /// </summary>
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner)
            : this(text, corner, default, null, null)
        {
        }

        /// <summary>
        ///     Custom-corner slot from bounds, no color overrides.
        /// </summary>
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect)
            : this(text, corner, customRect, null, null)
        {
        }

        /// <summary>
        ///     Shorthand for a preset-corner slot: <c>new ExtraIconAmountLabelSlot(text, corner)</c>.
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     Preset-corner slot; forwards to the primary constructor with <c>CustomRect</c> default and no outline
        ///     override.
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor)
        {
            return new(text, corner, default, fontColor, null);
        }

        /// <summary>
        ///     Preset-corner slot with foreground and outline overrides (use <see langword="null" /> to keep inherited
        ///     reference colors for that channel).
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, corner, default, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     Slot at <see cref="ExtraIconAmountLabelCorner.Custom" /> with position and size (host-local space).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
        }

        /// <summary>
        ///     Custom-bounds slot; foreground override only (no outline override).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, null);
        }

        /// <summary>
        ///     Custom-bounds slot with optional foreground and outline overrides (use <see langword="null" /> to keep
        ///     inherited reference colors).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor,
                fontOutlineColor);
        }

        /// <summary>
        ///     Slot at <see cref="ExtraIconAmountLabelCorner.Custom" /> with edges
        ///     <paramref name="left" />, <paramref name="top" />, <paramref name="right" />,
        ///     <paramref name="bottom" /> (host-local, same convention as control offsets from top-left anchor).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top));
        }

        /// <summary>
        ///     Custom edge slot; foreground override only (no outline override).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, null);
        }

        /// <summary>
        ///     Custom edge slot with optional foreground and outline overrides.
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor, Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, fontOutlineColor);
        }
    }
}
