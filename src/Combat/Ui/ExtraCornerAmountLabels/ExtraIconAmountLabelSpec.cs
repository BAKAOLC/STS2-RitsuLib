using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Runtime-unified extra badge spec. Prefer creating it from <see cref="ExtraIconAmountLabelSlot" /> or
    ///     <see cref="ExtraIconRichTextLabelSlot" />.
    ///     运行时统一的额外徽标 spec。推荐由 <see cref="ExtraIconAmountLabelSlot" /> 或
    ///     <see cref="ExtraIconRichTextLabelSlot" /> 创建。
    /// </summary>
    public readonly record struct ExtraIconAmountLabelSpec(
        string Text,
        ExtraIconAmountLabelCorner Corner,
        Rect2 CustomRect,
        Color? FontColor,
        Color? FontOutlineColor,
        ExtraIconAmountLabelTextMode TextMode = ExtraIconAmountLabelTextMode.Plain)
    {
        /// <summary>
        ///     Converts an original plain slot to a spec.
        ///     将原始普通槽位转换为 spec。
        /// </summary>
        public ExtraIconAmountLabelSpec(ExtraIconAmountLabelSlot slot,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(slot.Text, slot.Corner, slot.CustomRect, slot.FontColor, slot.FontOutlineColor, textMode)
        {
        }

        /// <summary>
        ///     Converts a rich-text slot to a spec.
        ///     将富文本槽位转换为 spec。
        /// </summary>
        public ExtraIconAmountLabelSpec(ExtraIconRichTextLabelSlot slot)
            : this(slot.Text, slot.Corner, slot.CustomRect, slot.FontColor, slot.FontOutlineColor,
                ExtraIconAmountLabelTextMode.RichText)
        {
        }

        /// <summary>
        ///     Preset-corner spec with no color overrides.
        ///     无颜色覆盖的预设角 spec。
        /// </summary>
        public ExtraIconAmountLabelSpec(string text, ExtraIconAmountLabelCorner corner,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(text, corner, default, null, null, textMode)
        {
        }

        /// <summary>
        ///     Custom-bounds spec with no color overrides.
        ///     无颜色覆盖的自定义边界 spec。
        /// </summary>
        public ExtraIconAmountLabelSpec(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(text, corner, customRect, null, null, textMode)
        {
        }

        /// <summary>
        ///     Converts an original plain slot to a plain spec.
        ///     将原始普通槽位转换为普通 spec。
        /// </summary>
        public static implicit operator ExtraIconAmountLabelSpec(ExtraIconAmountLabelSlot slot)
        {
            return new(slot);
        }

        /// <summary>
        ///     Converts a rich-text slot to a rich-text spec.
        ///     将富文本槽位转换为富文本 spec。
        /// </summary>
        public static implicit operator ExtraIconAmountLabelSpec(ExtraIconRichTextLabelSlot slot)
        {
            return new(slot);
        }

        /// <summary>
        ///     Plain preset-corner spec.
        ///     普通预设角 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec Plain(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(new ExtraIconAmountLabelSlot(text, corner));
        }

        /// <summary>
        ///     Rich-text preset-corner spec.
        ///     富文本预设角 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec RichText(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(new(text, corner));
        }

        /// <summary>
        ///     Plain custom-bounds spec.
        ///     普通自定义边界 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec PlainCustom(string text, Rect2 customRect)
        {
            return new(ExtraIconAmountLabelSlot.WithCustom(text, customRect));
        }

        /// <summary>
        ///     Rich-text custom-bounds spec.
        ///     富文本自定义边界 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec RichTextCustom(string text, Rect2 customRect)
        {
            return new(ExtraIconRichTextLabelSlot.WithCustom(text, customRect));
        }

        /// <summary>
        ///     Plain custom edge spec.
        ///     普通自定义边缘 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec PlainCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(ExtraIconAmountLabelSlot.WithCustom(text, left, top, right, bottom));
        }

        /// <summary>
        ///     Rich-text custom edge spec.
        ///     富文本自定义边缘 spec。
        /// </summary>
        public static ExtraIconAmountLabelSpec RichTextCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(ExtraIconRichTextLabelSlot.WithCustom(text, left, top, right, bottom));
        }
    }
}
