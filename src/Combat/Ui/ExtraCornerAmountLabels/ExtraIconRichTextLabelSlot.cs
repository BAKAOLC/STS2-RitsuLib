using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">Describes one independently positioned rich-text badge on a combat icon.</para>
    ///     <para xml:lang="zh-CN">描述战斗图标上一个独立定位的富文本角标。</para>
    /// </summary>
    public readonly record struct ExtraIconRichTextLabelSlot(
        string Text,
        ExtraIconAmountLabelCorner Corner,
        Rect2 CustomRect,
        Color? FontColor,
        Color? FontOutlineColor)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner without color overrides.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建不带颜色覆盖的条目。</para>
        /// </summary>
        public ExtraIconRichTextLabelSlot(string text, ExtraIconAmountLabelCorner corner)
            : this(text, corner, default, null, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry with explicit bounds and no color overrides.</para>
        ///     <para xml:lang="zh-CN">创建具有显式边界且不带颜色覆盖的条目。</para>
        /// </summary>
        public ExtraIconRichTextLabelSlot(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect)
            : this(text, corner, customRect, null, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner with a foreground-color override.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建带前景色覆盖的条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor)
        {
            return new(text, corner, default, fontColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner with optional color overrides.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建带可选颜色覆盖的条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, corner, default, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry.</para>
        ///     <para xml:lang="zh-CN">创建使用自定义边界的条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry with a foreground-color override.</para>
        ///     <para xml:lang="zh-CN">创建带前景色覆盖的自定义边界条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry with optional color overrides.</para>
        ///     <para xml:lang="zh-CN">创建带可选颜色覆盖的自定义边界条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom entry from host-local edge offsets.</para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建自定义条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a custom entry from host-local edge offsets with a foreground-color override.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建带前景色覆盖的自定义条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a custom entry from host-local edge offsets with optional color overrides.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建带可选颜色覆盖的自定义条目。</para>
        /// </summary>
        public static ExtraIconRichTextLabelSlot WithCustom(string text, float left, float top, float right,
            float bottom, Color? fontColor, Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, fontOutlineColor);
        }
    }
}
