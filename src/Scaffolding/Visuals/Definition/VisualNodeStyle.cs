using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Optional style overrides for procedural visual nodes created by RitsuLib factories and cue playback.
    ///     Unset properties leave the target node unchanged.
    ///     RitsuLib 工厂和 cue 播放创建的程序化视觉节点可选样式覆盖；未设置的属性不会修改目标节点。
    /// </summary>
    public sealed record VisualNodeStyle
    {
        /// <summary>
        ///     Empty style; applying it does not mutate the target node.
        ///     空样式；应用后不会修改目标节点。
        /// </summary>
        public static VisualNodeStyle Empty { get; } = new();

        /// <summary>
        ///     Absolute local position. When unset, the current node position is preserved unless a caller supplies a
        ///     base position.
        ///     绝对本地位置。未设置时保留节点当前位置，除非调用方提供基础位置。
        /// </summary>
        public Vector2? Position { get; init; }

        /// <summary>
        ///     Local position delta applied after <see cref="Position" /> or a caller-supplied base position.
        ///     在 <see cref="Position" /> 或调用方提供的基础位置之后应用的本地位置偏移。
        /// </summary>
        public Vector2? Offset { get; init; }

        /// <summary>
        ///     Local scale for <see cref="Node2D" /> and <see cref="Control" /> targets.
        ///     应用于 <see cref="Node2D" /> 和 <see cref="Control" /> 目标的本地缩放。
        /// </summary>
        public Vector2? Scale { get; init; }

        /// <summary>
        ///     Local rotation in radians, matching Godot's native rotation units.
        ///     以弧度表示的本地旋转，匹配 Godot 原生旋转单位。
        /// </summary>
        public float? RotationRadians { get; init; }

        /// <summary>
        ///     Local skew for <see cref="Node2D" /> targets.
        ///     应用于 <see cref="Node2D" /> 目标的本地倾斜。
        /// </summary>
        public float? Skew { get; init; }

        /// <summary>
        ///     Pivot offset for <see cref="Control" /> targets.
        ///     应用于 <see cref="Control" /> 目标的枢轴偏移。
        /// </summary>
        public Vector2? PivotOffset { get; init; }

        /// <summary>
        ///     Canvas-item modulate color.
        ///     CanvasItem 的调制颜色。
        /// </summary>
        public Color? Modulate { get; init; }

        /// <summary>
        ///     Canvas-item self-modulate color.
        ///     CanvasItem 的自身调制颜色。
        /// </summary>
        public Color? SelfModulate { get; init; }

        /// <summary>
        ///     Canvas-item z-index.
        ///     CanvasItem 的 z-index。
        /// </summary>
        public int? ZIndex { get; init; }

        /// <summary>
        ///     Canvas-item visibility.
        ///     CanvasItem 的可见性。
        /// </summary>
        public bool? Visible { get; init; }

        /// <summary>
        ///     Sprite centering flag for <see cref="Sprite2D" /> targets.
        ///     应用于 <see cref="Sprite2D" /> 目标的居中标记。
        /// </summary>
        public bool? Centered { get; init; }

        /// <summary>
        ///     Horizontal flip flag for <see cref="Sprite2D" /> targets.
        ///     应用于 <see cref="Sprite2D" /> 目标的水平翻转标记。
        /// </summary>
        public bool? FlipH { get; init; }

        /// <summary>
        ///     Vertical flip flag for <see cref="Sprite2D" /> targets.
        ///     应用于 <see cref="Sprite2D" /> 目标的垂直翻转标记。
        /// </summary>
        public bool? FlipV { get; init; }

        /// <summary>
        ///     Creates a style using degrees for rotation, which is the friendlier authoring format for mods.
        ///     使用角度创建样式，这是对 mod 作者更友好的旋转书写格式。
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
        ///     Creates a style using radians for rotation, matching Godot's native API.
        ///     使用弧度创建样式，匹配 Godot 原生 API。
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
        ///     Returns a copy with <see cref="Position" /> set.
        ///     返回设置了 <see cref="Position" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithPosition(Vector2 position)
        {
            return this with { Position = position };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Offset" /> set.
        ///     返回设置了 <see cref="Offset" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithOffset(Vector2 offset)
        {
            return this with { Offset = offset };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Scale" /> set.
        ///     返回设置了 <see cref="Scale" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithScale(Vector2 scale)
        {
            return this with { Scale = scale };
        }

        /// <summary>
        ///     Returns a copy with uniform <see cref="Scale" /> set.
        ///     返回设置了统一 <see cref="Scale" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithScale(float uniformScale)
        {
            return this with { Scale = new(uniformScale, uniformScale) };
        }

        /// <summary>
        ///     Returns a copy with <see cref="RotationRadians" /> set from degrees.
        ///     返回从角度值设置 <see cref="RotationRadians" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithRotationDegrees(float degrees)
        {
            return this with { RotationRadians = Mathf.DegToRad(degrees) };
        }

        /// <summary>
        ///     Returns a copy with <see cref="RotationRadians" /> set.
        ///     返回设置了 <see cref="RotationRadians" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithRotationRadians(float radians)
        {
            return this with { RotationRadians = radians };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Skew" /> set.
        ///     返回设置了 <see cref="Skew" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithSkew(float skew)
        {
            return this with { Skew = skew };
        }

        /// <summary>
        ///     Returns a copy with <see cref="PivotOffset" /> set.
        ///     返回设置了 <see cref="PivotOffset" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithPivotOffset(Vector2 pivotOffset)
        {
            return this with { PivotOffset = pivotOffset };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Modulate" /> set.
        ///     返回设置了 <see cref="Modulate" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithModulate(Color modulate)
        {
            return this with { Modulate = modulate };
        }

        /// <summary>
        ///     Returns a copy with <see cref="SelfModulate" /> set.
        ///     返回设置了 <see cref="SelfModulate" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithSelfModulate(Color selfModulate)
        {
            return this with { SelfModulate = selfModulate };
        }

        /// <summary>
        ///     Returns a copy with <see cref="ZIndex" /> set.
        ///     返回设置了 <see cref="ZIndex" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithZIndex(int zIndex)
        {
            return this with { ZIndex = zIndex };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Visible" /> set.
        ///     返回设置了 <see cref="Visible" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithVisible(bool visible = true)
        {
            return this with { Visible = visible };
        }

        /// <summary>
        ///     Returns a copy hidden from rendering.
        ///     返回隐藏渲染的副本。
        /// </summary>
        public VisualNodeStyle Hidden()
        {
            return this with { Visible = false };
        }

        /// <summary>
        ///     Returns a copy with <see cref="Centered" /> set.
        ///     返回设置了 <see cref="Centered" /> 的副本。
        /// </summary>
        public VisualNodeStyle WithCentered(bool centered = true)
        {
            return this with { Centered = centered };
        }

        /// <summary>
        ///     Returns a copy with sprite flip flags set.
        ///     返回设置了 sprite 翻转标记的副本。
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
