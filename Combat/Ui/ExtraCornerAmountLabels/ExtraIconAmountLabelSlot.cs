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
    public readonly record struct ExtraIconAmountLabelSlot(
        string Text,
        ExtraIconAmountLabelCorner Corner,
        Rect2 CustomRect = default)
    {
        /// <summary>
        ///     Shorthand for a preset-corner slot: <c>new ExtraIconAmountLabelSlot(text, corner)</c>.
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     Slot at <see cref="ExtraIconAmountLabelCorner.Custom" /> with position and size (host-local space).
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
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
    }
}
