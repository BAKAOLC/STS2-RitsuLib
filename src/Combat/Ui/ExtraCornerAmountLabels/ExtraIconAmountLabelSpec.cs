using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a plain-text or rich-text badge in the unified provider API.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述统一提供接口中的纯文本或富文本角标。</para>
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
        ///     <para xml:lang="en">Creates a badge description from a plain-text entry.</para>
        ///     <para xml:lang="zh-CN">根据纯文本条目创建角标描述。</para>
        /// </summary>
        public ExtraIconAmountLabelSpec(ExtraIconAmountLabelSlot slot,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(slot.Text, slot.Corner, slot.CustomRect, slot.FontColor, slot.FontOutlineColor, textMode)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a badge description from a rich-text entry.</para>
        ///     <para xml:lang="zh-CN">根据富文本条目创建角标描述。</para>
        /// </summary>
        public ExtraIconAmountLabelSpec(ExtraIconRichTextLabelSlot slot)
            : this(slot.Text, slot.Corner, slot.CustomRect, slot.FontColor, slot.FontOutlineColor,
                ExtraIconAmountLabelTextMode.RichText)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a badge at a built-in corner without color overrides.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建不带颜色覆盖的角标。</para>
        /// </summary>
        public ExtraIconAmountLabelSpec(string text, ExtraIconAmountLabelCorner corner,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(text, corner, default, null, null, textMode)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a badge with explicit bounds and no color overrides.</para>
        ///     <para xml:lang="zh-CN">创建具有显式边界且不带颜色覆盖的角标。</para>
        /// </summary>
        public ExtraIconAmountLabelSpec(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect,
            ExtraIconAmountLabelTextMode textMode = ExtraIconAmountLabelTextMode.Plain)
            : this(text, corner, customRect, null, null, textMode)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Converts a plain-text entry to a badge description.</para>
        ///     <para xml:lang="zh-CN">将纯文本条目转换为角标描述。</para>
        /// </summary>
        public static implicit operator ExtraIconAmountLabelSpec(ExtraIconAmountLabelSlot slot)
        {
            return new(slot);
        }

        /// <summary>
        ///     <para xml:lang="en">Converts a rich-text entry to a badge description.</para>
        ///     <para xml:lang="zh-CN">将富文本条目转换为角标描述。</para>
        /// </summary>
        public static implicit operator ExtraIconAmountLabelSpec(ExtraIconRichTextLabelSlot slot)
        {
            return new(slot);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a plain-text badge at a built-in corner.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建纯文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec Plain(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(new ExtraIconAmountLabelSlot(text, corner));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rich-text badge at a built-in corner.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建富文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec RichText(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(new(text, corner));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a plain-text badge with custom bounds.</para>
        ///     <para xml:lang="zh-CN">创建使用自定义边界的纯文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec PlainCustom(string text, Rect2 customRect)
        {
            return new(ExtraIconAmountLabelSlot.WithCustom(text, customRect));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rich-text badge with custom bounds.</para>
        ///     <para xml:lang="zh-CN">创建使用自定义边界的富文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec RichTextCustom(string text, Rect2 customRect)
        {
            return new(ExtraIconRichTextLabelSlot.WithCustom(text, customRect));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a plain-text badge from host-local edge offsets.</para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建纯文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec PlainCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(ExtraIconAmountLabelSlot.WithCustom(text, left, top, right, bottom));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rich-text badge from host-local edge offsets.</para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建富文本角标。</para>
        /// </summary>
        public static ExtraIconAmountLabelSpec RichTextCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(ExtraIconRichTextLabelSlot.WithCustom(text, left, top, right, bottom));
        }
    }
}
