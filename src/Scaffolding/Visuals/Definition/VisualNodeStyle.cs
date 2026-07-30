using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional style overrides for procedural visual nodes created by RitsuLib factories and cue playback.
    ///         Unset properties leave the target node unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 RitsuLib 工厂和视觉提示播放所创建程序化视觉节点的可选样式覆盖。
    ///         未设置的属性不会改变目标节点。
    ///     </para>
    /// </summary>
    public sealed record VisualNodeStyle
    {
        /// <summary>
        ///     <para xml:lang="en">Gets an empty style that does not change the target node when applied.</para>
        ///     <para xml:lang="zh-CN">获取应用后不会改变目标节点的空样式。</para>
        /// </summary>
        public static VisualNodeStyle Empty { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the absolute local position. When unset, the current position is preserved unless the caller supplies
        ///         a base position.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取绝对局部位置。未设置时会保留节点的当前位置，除非调用方提供了基准位置。
        ///     </para>
        /// </summary>
        public Vector2? Position { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the local position delta applied after <see cref="Position" /> or a caller-supplied base position.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在 <see cref="Position" /> 或调用方提供的基准位置之后叠加的局部位置偏移。
        ///     </para>
        /// </summary>
        public Vector2? Offset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local scale for <see cref="Node2D" /> and <see cref="Control" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Node2D" /> 和 <see cref="Control" /> 目标的局部缩放。</para>
        /// </summary>
        public Vector2? Scale { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local rotation in radians, matching Godot's native rotation unit.</para>
        ///     <para xml:lang="zh-CN">获取以弧度表示的局部旋转，与 Godot 的原生旋转单位一致。</para>
        /// </summary>
        public float? RotationRadians { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the local skew for <see cref="Node2D" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Node2D" /> 目标的局部倾斜。</para>
        /// </summary>
        public float? Skew { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the pivot offset for <see cref="Control" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Control" /> 目标的枢轴偏移。</para>
        /// </summary>
        public Vector2? PivotOffset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="CanvasItem.Modulate" /> color.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="CanvasItem.Modulate" /> 颜色。</para>
        /// </summary>
        public Color? Modulate { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="CanvasItem.SelfModulate" /> color.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="CanvasItem.SelfModulate" /> 颜色。</para>
        /// </summary>
        public Color? SelfModulate { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="CanvasItem.ZIndex" /> value.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="CanvasItem.ZIndex" /> 值。</para>
        /// </summary>
        public int? ZIndex { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="CanvasItem.Visible" /> value.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="CanvasItem.Visible" /> 值。</para>
        /// </summary>
        public bool? Visible { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the centering flag for <see cref="Sprite2D" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Sprite2D" /> 目标的居中标记。</para>
        /// </summary>
        public bool? Centered { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the horizontal flip flag for <see cref="Sprite2D" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Sprite2D" /> 目标的水平翻转标记。</para>
        /// </summary>
        public bool? FlipH { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the vertical flip flag for <see cref="Sprite2D" /> targets.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="Sprite2D" /> 目标的垂直翻转标记。</para>
        /// </summary>
        public bool? FlipV { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates a style using degrees as the mod-facing rotation unit.</para>
        ///     <para xml:lang="zh-CN">使用更便于模组作者配置的角度制旋转值创建样式。</para>
        /// </summary>
        public static VisualNodeStyle Create(
            Vector2? position = null,
            Vector2? offset = null,
            Vector2? scale = null,
            float? rotationDegrees = null,
            float? skew = null,
            Vector2? pivotOffset = null,
            Color? modulate = null,
            Color? selfModulate = null,
            int? zIndex = null,
            bool? visible = null,
            bool? centered = null,
            bool? flipH = null,
            bool? flipV = null)
        {
            return new()
            {
                Position = position,
                Offset = offset,
                Scale = scale,
                RotationRadians = rotationDegrees.HasValue ? Mathf.DegToRad(rotationDegrees.Value) : null,
                Skew = skew,
                PivotOffset = pivotOffset,
                Modulate = modulate,
                SelfModulate = selfModulate,
                ZIndex = zIndex,
                Visible = visible,
                Centered = centered,
                FlipH = flipH,
                FlipV = flipV,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a style using radians for rotation, matching Godot's native API.</para>
        ///     <para xml:lang="zh-CN">使用与 Godot 原生 API 一致的弧度制旋转值创建样式。</para>
        /// </summary>
        public static VisualNodeStyle CreateRadians(
            Vector2? position = null,
            Vector2? offset = null,
            Vector2? scale = null,
            float? rotationRadians = null,
            float? skew = null,
            Vector2? pivotOffset = null,
            Color? modulate = null,
            Color? selfModulate = null,
            int? zIndex = null,
            bool? visible = null,
            bool? centered = null,
            bool? flipH = null,
            bool? flipV = null)
        {
            return new()
            {
                Position = position,
                Offset = offset,
                Scale = scale,
                RotationRadians = rotationRadians,
                Skew = skew,
                PivotOffset = pivotOffset,
                Modulate = modulate,
                SelfModulate = selfModulate,
                ZIndex = zIndex,
                Visible = visible,
                Centered = centered,
                FlipH = flipH,
                FlipV = flipV,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Position" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Position" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithPosition(Vector2 position)
        {
            return this with { Position = position };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Offset" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Offset" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithOffset(Vector2 offset)
        {
            return this with { Offset = offset };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Scale" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Scale" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithScale(Vector2 scale)
        {
            return this with { Scale = scale };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with a uniform <see cref="Scale" /> set on both axes.</para>
        ///     <para xml:lang="zh-CN">返回在两个轴上设置了相同 <see cref="Scale" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithScale(float uniformScale)
        {
            return this with { Scale = new(uniformScale, uniformScale) };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="RotationRadians" /> converted from degrees.</para>
        ///     <para xml:lang="zh-CN">返回将角度制值转换并设置到 <see cref="RotationRadians" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithRotationDegrees(float degrees)
        {
            return this with { RotationRadians = Mathf.DegToRad(degrees) };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="RotationRadians" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="RotationRadians" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithRotationRadians(float radians)
        {
            return this with { RotationRadians = radians };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Skew" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Skew" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithSkew(float skew)
        {
            return this with { Skew = skew };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="PivotOffset" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="PivotOffset" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithPivotOffset(Vector2 pivotOffset)
        {
            return this with { PivotOffset = pivotOffset };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Modulate" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Modulate" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithModulate(Color modulate)
        {
            return this with { Modulate = modulate };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="SelfModulate" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="SelfModulate" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithSelfModulate(Color selfModulate)
        {
            return this with { SelfModulate = selfModulate };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="ZIndex" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="ZIndex" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithZIndex(int zIndex)
        {
            return this with { ZIndex = zIndex };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Visible" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Visible" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithVisible(bool visible = true)
        {
            return this with { Visible = visible };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Visible" /> set to <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">
        ///         返回将 <see cref="Visible" /> 设为 <see langword="false" /> 的副本。
        ///     </para>
        /// </summary>
        public VisualNodeStyle Hidden()
        {
            return this with { Visible = false };
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy with <see cref="Centered" /> set.</para>
        ///     <para xml:lang="zh-CN">返回已设置 <see cref="Centered" /> 的副本。</para>
        /// </summary>
        public VisualNodeStyle WithCentered(bool centered = true)
        {
            return this with { Centered = centered };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a copy with the specified sprite flip flags set. A <see langword="null" /> argument preserves the
        ///         corresponding current value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回已设置指定精灵翻转标记的副本；参数为 <see langword="null" /> 时保留对应的当前值。
        ///     </para>
        /// </summary>
        public VisualNodeStyle WithFlip(bool? horizontal = null, bool? vertical = null)
        {
            return this with
            {
                FlipH = horizontal ?? FlipH,
                FlipV = vertical ?? FlipV,
            };
        }
    }

    internal static class VisualNodeStyleApplicator
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        // ReSharper disable once InvertIf
        public static void ApplyTo(this VisualNodeStyle? style, Node? target, Vector2? positionBase = null)
        {
            if (style == null || !GodotObject.IsInstanceValid(target))
                return;

            if (target is CanvasItem canvasItem)
            {
                if (style.Visible.HasValue)
                    canvasItem.Visible = style.Visible.Value;
                if (style.Modulate.HasValue)
                    canvasItem.Modulate = style.Modulate.Value;
                if (style.SelfModulate.HasValue)
                    canvasItem.SelfModulate = style.SelfModulate.Value;
                if (style.ZIndex.HasValue)
                    canvasItem.ZIndex = style.ZIndex.Value;
            }

            if (target is Node2D node2D)
            {
                if (style.Position.HasValue)
                    node2D.Position = style.Position.Value;
                else if (positionBase.HasValue)
                    node2D.Position = positionBase.Value;

                if (style.Offset.HasValue)
                    node2D.Position += style.Offset.Value;
                if (style.Scale.HasValue)
                    node2D.Scale = style.Scale.Value;
                if (style.RotationRadians.HasValue)
                    node2D.Rotation = style.RotationRadians.Value;
                if (style.Skew.HasValue)
                    node2D.Skew = style.Skew.Value;
            }

            if (target is Control control)
            {
                if (style.Position.HasValue)
                    control.Position = style.Position.Value;
                else if (positionBase.HasValue)
                    control.Position = positionBase.Value;

                if (style.Offset.HasValue)
                    control.Position += style.Offset.Value;
                if (style.Scale.HasValue)
                    control.Scale = style.Scale.Value;
                if (style.RotationRadians.HasValue)
                    control.Rotation = style.RotationRadians.Value;
                if (style.PivotOffset.HasValue)
                    control.PivotOffset = style.PivotOffset.Value;
            }

            if (target is Sprite2D sprite)
            {
                if (style.Centered.HasValue)
                    sprite.Centered = style.Centered.Value;
                if (style.FlipH.HasValue)
                    sprite.FlipH = style.FlipH.Value;
                if (style.FlipV.HasValue)
                    sprite.FlipV = style.FlipV.Value;
            }
        }
    }
}
