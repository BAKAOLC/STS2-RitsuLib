using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     One extra rich-text badge on a combat UI icon. Each slot is laid out independently.
    ///     战斗 UI 图标上的一个额外富文本徽标。每个槽位独立布局。
    /// </summary>
    public readonly record struct ExtraIconRichTextLabelSlot(
        string Text,
        ExtraIconAmountLabelCorner Corner,
        Rect2 CustomRect,
        Color? FontColor,
        Color? FontOutlineColor)
    {
        /// <summary>
        ///     Preset-corner rich-text slot: default <c>CustomRect</c>, no color overrides.
        ///     预设角富文本槽位：默认 <c>CustomRect</c>，无颜色覆盖。
        /// </summary>
        public ExtraIconRichTextLabelSlot(string text, ExtraIconAmountLabelCorner corner)
            : this(text, corner, default, null, null)
        {
        }

        /// <summary>
        ///     Custom-corner rich-text slot from bounds, no color overrides.
        ///     由边界创建的自定义角富文本槽位，无颜色覆盖。
        /// </summary>
        public ExtraIconRichTextLabelSlot(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect)
            : this(text, corner, customRect, null, null)
        {
        }

        /// <summary>
        ///     Preset-corner rich-text slot.
        ///     预设角富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     Preset-corner rich-text slot with foreground override.
        ///     带有前景色覆盖的预设角富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor)
        {
            return new(text, corner, default, fontColor, null);
        }

        /// <summary>
        ///     Preset-corner rich-text slot with foreground and outline overrides.
        ///     带有前景色和描边覆盖的预设角富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, corner, default, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     Custom-bounds rich-text slot.
        ///     自定义边界富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
        }

        /// <summary>
        ///     Custom-bounds rich-text slot with foreground override.
        ///     带有前景色覆盖的自定义边界富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, null);
        }

        /// <summary>
        ///     Custom-bounds rich-text slot with foreground and outline overrides.
        ///     带有前景色和描边覆盖的自定义边界富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     Custom edge rich-text slot.
        ///     自定义边缘富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top));
        }

        /// <summary>
        ///     Custom edge rich-text slot with foreground override.
        ///     带有前景色覆盖的自定义边缘富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, null);
        }

        /// <summary>
        ///     Custom edge rich-text slot with foreground and outline overrides.
        ///     带有前景色和描边覆盖的自定义边缘富文本槽位。
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom, Color? fontColor, Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, fontOutlineColor);
        }
    }
}
