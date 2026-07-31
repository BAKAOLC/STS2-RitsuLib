using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">Describes one independently positioned plain-text badge on a combat icon.</para>
    ///     <para xml:lang="zh-CN">描述战斗图标上一个独立定位的纯文本角标。</para>
    /// </summary>
    /// <param name="Text">
    ///     <para xml:lang="en">The badge text. Whitespace-only entries are ignored.</para>
    ///     <para xml:lang="zh-CN">角标文本；仅含空白的条目会被忽略。</para>
    /// </param>
    /// <param name="Corner">
    ///     <para xml:lang="en">
    ///         The built-in corner to use, or <see cref="ExtraIconAmountLabelCorner.Custom" /> to use
    ///         <paramref name="CustomRect" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         要使用的内置角落；使用 <see cref="ExtraIconAmountLabelCorner.Custom" /> 时改用
    ///         <paramref name="CustomRect" />。
    ///     </para>
    /// </param>
    /// <param name="CustomRect">
    ///     <para xml:lang="en">
    ///         The host-local bounds used for <see cref="ExtraIconAmountLabelCorner.Custom" />. Preset corners ignore
    ///         this value; custom entries with non-positive width or height are ignored.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ExtraIconAmountLabelCorner.Custom" /> 使用的宿主局部边界。内置角落会忽略此值；
    ///         宽度或高度非正的自定义条目也会被忽略。
    ///     </para>
    /// </param>
    /// <param name="FontColor">
    ///     <para xml:lang="en">
    ///         The optional foreground-color override, or <see langword="null" /> to use the host's color.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的前景色覆盖；为 <see langword="null" /> 时使用宿主颜色。
    ///     </para>
    /// </param>
    /// <param name="FontOutlineColor">
    ///     <para xml:lang="en">
    ///         The optional outline-color override, or <see langword="null" /> to use the host's color.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的描边颜色覆盖；为 <see langword="null" /> 时使用宿主颜色。
    ///     </para>
    /// </param>
    public readonly record struct ExtraIconAmountLabelSlot(
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
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner)
            : this(text, corner, default, null, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry with explicit bounds and no color overrides.</para>
        ///     <para xml:lang="zh-CN">创建具有显式边界且不带颜色覆盖的条目。</para>
        /// </summary>
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect)
            : this(text, corner, customRect, null, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner with a foreground-color override.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建带前景色覆盖的条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor)
        {
            return new(text, corner, default, fontColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an entry at a built-in corner with optional color overrides.</para>
        ///     <para xml:lang="zh-CN">在内置角落创建带可选颜色覆盖的条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, corner, default, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry.</para>
        ///     <para xml:lang="zh-CN">创建使用自定义边界的条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry with a foreground-color override.</para>
        ///     <para xml:lang="zh-CN">创建带前景色覆盖的自定义边界条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom-bounds entry with optional color overrides.</para>
        ///     <para xml:lang="zh-CN">创建带可选颜色覆盖的自定义边界条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor,
                fontOutlineColor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a custom entry from host-local edge offsets.</para>
        ///     <para xml:lang="zh-CN">根据宿主局部边缘偏移创建自定义条目。</para>
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom)
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
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor)
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
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor, Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, fontOutlineColor);
        }
    }
}
