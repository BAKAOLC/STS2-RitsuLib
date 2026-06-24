using Godot;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     One extra amount/text badge on a combat UI icon. Each slot is laid out independently.
    ///     战斗 UI 图标上的一个额外数量/文本徽标。每个槽位独立布局。
    /// </summary>
    /// <param name="Text">
    ///     Badge text. Whitespace-only entries are skipped.
    ///     徽标文本。仅包含空白的条目会被跳过。
    /// </param>
    /// <param name="Corner">
    ///     <see cref="ExtraIconAmountLabelCorner.TopLeft" />, <see cref="ExtraIconAmountLabelCorner.TopRight" />,
    ///     <see cref="ExtraIconAmountLabelCorner.BottomLeft" />, or <see cref="ExtraIconAmountLabelCorner.BottomRight" />
    ///     for built-in layout, or
    ///     <see cref="ExtraIconAmountLabelCorner.Custom" /> with <paramref name="CustomRect" />.
    ///     <see cref="ExtraIconAmountLabelCorner.TopLeft" />、<see cref="ExtraIconAmountLabelCorner.TopRight" />、
    ///     <see cref="ExtraIconAmountLabelCorner.BottomLeft" /> 或 <see cref="ExtraIconAmountLabelCorner.BottomRight" />
    ///     用于内置布局，或
    ///     将 <see cref="ExtraIconAmountLabelCorner.Custom" /> 与 <paramref name="CustomRect" /> 搭配使用。
    /// </param>
    /// <param name="CustomRect">
    ///     When <paramref name="Corner" /> is <see cref="ExtraIconAmountLabelCorner.Custom" />, local rectangle
    ///     (position and size in host control space, same convention as <c>offset_*</c> for a top-left-anchored child).
    ///     Ignored for presets. Entries with non-positive width or height are skipped at runtime.
    ///     当 <paramref name="Corner" /> 为 <see cref="ExtraIconAmountLabelCorner.Custom" /> 时，表示局部矩形
    ///     （宿主 control 空间中的位置和大小，与左上锚定子节点的 <c>offset_*</c> 约定相同）。
    ///     预设角会忽略它。宽度或高度非正的条目会在运行时跳过。
    /// </param>
    /// <param name="FontColor">
    ///     When set, overrides the foreground color for this badge after base typography is copied from the host
    ///     reference label. When <see langword="null" />, keeps the inherited color from that reference (default).
    ///     设置后，在从宿主参考标签复制基础排版后覆盖此徽标的前景色。为 <see langword="null" /> 时，保留
    ///     从该参考继承的颜色（默认）。
    /// </param>
    /// <param name="FontOutlineColor">
    ///     When set, overrides the outline color after base typography is copied. When <see langword="null" />, keeps
    ///     the inherited outline from that reference (default).
    ///     设置后，在复制基础排版后覆盖描边颜色。为 <see langword="null" /> 时，保留
    ///     从该参考继承的描边（默认）。
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
        ///     预设角槽位：默认 <c>CustomRect</c>，无颜色覆盖。
        /// </summary>
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner)
            : this(text, corner, default, null, null)
        {
        }

        /// <summary>
        ///     Custom-corner slot from bounds, no color overrides.
        ///     由边界创建的自定义角槽位，无颜色覆盖。
        /// </summary>
        public ExtraIconAmountLabelSlot(string text, ExtraIconAmountLabelCorner corner, Rect2 customRect)
            : this(text, corner, customRect, null, null)
        {
        }

        /// <summary>
        ///     Shorthand for a preset-corner slot: <c>new ExtraIconAmountLabelSlot(text, corner)</c>.
        ///     预设角槽位的简写：<c>new ExtraIconAmountLabelSlot(text, corner)</c>。
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text)
        {
            return new(text, corner);
        }

        /// <summary>
        ///     Preset-corner slot; forwards to the primary constructor with <c>CustomRect</c> default and no outline
        ///     override.
        ///     预设角槽位；转发到主构造函数，使用默认 <c>CustomRect</c> 且无描边
        ///     覆盖。
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor)
        {
            return new(text, corner, default, fontColor, null);
        }

        /// <summary>
        ///     Preset-corner slot with foreground and outline overrides (use <see langword="null" /> to keep inherited
        ///     reference colors for that channel).
        ///     带有前景色和描边覆盖的预设角落槽位（使用 <see langword="null" /> 可保留该通道继承的
        ///     引用颜色）。
        /// </summary>
        public static ExtraIconAmountLabelSlot At(ExtraIconAmountLabelCorner corner, string text, Color? fontColor,
            Color? fontOutlineColor)
        {
            return new(text, corner, default, fontColor, fontOutlineColor);
        }

        /// <summary>
        ///     Slot at <see cref="ExtraIconAmountLabelCorner.Custom" /> with position and size (host-local space).
        ///     位于 <see cref="ExtraIconAmountLabelCorner.Custom" /> 的槽位，包含位置和尺寸（宿主本地空间）。
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect);
        }

        /// <summary>
        ///     Custom-bounds slot; foreground override only (no outline override).
        ///     自定义边界槽位；仅覆盖前景色（不覆盖描边）。
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, Rect2 customRect, Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom, customRect, fontColor, null);
        }

        /// <summary>
        ///     Custom-bounds slot with optional foreground and outline overrides (use <see langword="null" /> to keep
        ///     inherited reference colors).
        ///     带有可选前景色和描边覆盖的自定义边界槽位（使用 <see langword="null" /> 可保留
        ///     继承的引用颜色）。
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
        ///     位于 <see cref="ExtraIconAmountLabelCorner.Custom" /> 的槽位，边缘为
        ///     <paramref name="left" />、<paramref name="top" />、<paramref name="right" />、
        ///     <paramref name="bottom" />（宿主本地空间，约定与从左上锚点起算的控件偏移相同）。
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top));
        }

        /// <summary>
        ///     Custom edge slot; foreground override only (no outline override).
        ///     自定义边缘槽位；仅覆盖前景色（不覆盖描边）。
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, null);
        }

        /// <summary>
        ///     Custom edge slot with optional foreground and outline overrides.
        ///     带有可选前景色和描边覆盖的自定义边缘槽位。
        /// </summary>
        public static ExtraIconAmountLabelSlot WithCustom(string text, float left, float top, float right, float bottom,
            Color? fontColor, Color? fontOutlineColor)
        {
            return new(text, ExtraIconAmountLabelCorner.Custom,
                new(left, top, right - left, bottom - top), fontColor, fontOutlineColor);
        }
    }
}
