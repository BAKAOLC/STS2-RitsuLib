using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a data-driven Ancient event stage. The background is either a looping video or a
    ///         <see cref="VisualCueSet" /> of textures or frame sequences; the optional foreground supports visual
    ///         cues but not video.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义由数据驱动的先古之民事件舞台。背景可以是循环视频，也可以是包含纹理或帧序列的
    ///         <see cref="VisualCueSet" />；可选前景支持形象提示，但不支持视频。
    ///     </para>
    /// </summary>
    /// <param name="BackgroundCueSet">
    ///     <para xml:lang="en">
    ///         The cues that drive the background when <paramref name="BackgroundVideoPath" /> is
    ///         <see langword="null" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <paramref name="BackgroundVideoPath" /> 为 <see langword="null" /> 时用于驱动背景的提示集合。
    ///     </para>
    /// </param>
    /// <param name="BackgroundLoopCueName">
    ///     <para xml:lang="en">
    ///         The primary background cue name. <c>loop</c> is used when this value is <see langword="null" />; the
    ///         value is ignored for video backgrounds.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         主要背景提示名称。值为 <see langword="null" /> 时使用 <c>loop</c>；视频背景会忽略此值。
    ///     </para>
    /// </param>
    /// <param name="BackgroundVideoPath">
    ///     <para xml:lang="en">
    ///         An optional <c>res://</c> path to a <c>VideoStream</c> resource. This is mutually exclusive with
    ///         <paramref name="BackgroundCueSet" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指向 <c>VideoStream</c> 资源的可选 <c>res://</c> 路径。此值与
    ///         <paramref name="BackgroundCueSet" /> 互斥。
    ///     </para>
    /// </param>
    /// <param name="ForegroundCueSet">
    ///     <para xml:lang="en">The optional foreground cues, such as those for a character.</para>
    ///     <para xml:lang="zh-CN">可选前景提示集合，例如用于显示角色的提示。</para>
    /// </param>
    /// <param name="ForegroundLoopCueName">
    ///     <para xml:lang="en">
    ///         The primary foreground cue name. <c>loop</c> is used when this value is <see langword="null" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         主要前景提示名称。值为 <see langword="null" /> 时使用 <c>loop</c>。
    ///     </para>
    /// </param>
    public sealed record AncientEventStageProceduralVisualSet(
        VisualCueSet? BackgroundCueSet = null,
        string? BackgroundLoopCueName = null,
        string? BackgroundVideoPath = null,
        VisualCueSet? ForegroundCueSet = null,
        string? ForegroundLoopCueName = null)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a stage definition with optional layer styles. The five-parameter record constructor
        ///         remains available for binary compatibility with older mods.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带可选图层样式的舞台定义。为保持与旧版模组的二进制兼容，仍保留五参数记录构造函数。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSet(
            VisualCueSet? BackgroundCueSet,
            string? BackgroundLoopCueName,
            string? BackgroundVideoPath,
            VisualCueSet? ForegroundCueSet,
            string? ForegroundLoopCueName,
            VisualNodeStyle? BackgroundLayerStyle,
            VisualNodeStyle? ForegroundLayerStyle)
            : this(BackgroundCueSet, BackgroundLoopCueName, BackgroundVideoPath, ForegroundCueSet,
                ForegroundLoopCueName)
        {
            this.BackgroundLayerStyle = BackgroundLayerStyle;
            this.ForegroundLayerStyle = ForegroundLayerStyle;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional style applied to the background layer's primary <c>Visuals</c> sprite.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取应用到背景层主要 <c>Visuals</c> 精灵的可选样式。
        ///     </para>
        /// </summary>
        public VisualNodeStyle? BackgroundLayerStyle { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional style applied to the foreground layer's primary <c>Visuals</c> sprite.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取应用到前景层主要 <c>Visuals</c> 精灵的可选样式。
        ///     </para>
        /// </summary>
        public VisualNodeStyle? ForegroundLayerStyle { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a fluent builder for <see cref="AncientEventStageProceduralVisualSet" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 <see cref="AncientEventStageProceduralVisualSet" /> 的流式构建器。
    ///     </para>
    /// </summary>
    public sealed class AncientEventStageProceduralVisualSetBuilder
    {
        private VisualCueSet? _backgroundCueSet;
        private VisualNodeStyle? _backgroundLayerStyle;
        private string? _backgroundLoopCue;
        private string? _backgroundVideoPath;
        private VisualCueSet? _foregroundCueSet;
        private VisualNodeStyle? _foregroundLayerStyle;
        private string? _foregroundLoopCue;

        private AncientEventStageProceduralVisualSetBuilder()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an empty procedural stage builder.</para>
        ///     <para xml:lang="zh-CN">创建空的程序化舞台构建器。</para>
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the background from visual cues, replacing any video background.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用形象提示配置背景，并替换已配置的视频背景。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder Background(VisualCueSet cueSet, string? loopCueName = null)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _backgroundCueSet = cueSet;
            _backgroundLoopCue = loopCueName;
            _backgroundVideoPath = null;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the background through a <see cref="VisualCueSetBuilder" /> callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="VisualCueSetBuilder" /> 回调配置背景。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder Background(Action<VisualCueSetBuilder> configure,
            string? loopCueName = null)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return Background(inner.Build(), loopCueName);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures a looping full-area background video, replacing any cue-based background. The resource
        ///         must use a <c>VideoStream</c> format supported by the target platform.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置覆盖整个区域的循环背景视频，并替换已配置的提示背景。资源必须采用目标平台支持的
        ///         <c>VideoStream</c> 格式。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder BackgroundVideo(string resourcePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
            _backgroundVideoPath = resourcePath.Trim();
            _backgroundCueSet = null;
            _backgroundLoopCue = null;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a style to the cue-based background sprite. Video backgrounds ignore this style.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将样式应用到基于提示的背景精灵。视频背景会忽略此样式。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder BackgroundStyle(VisualNodeStyle style)
        {
            ArgumentNullException.ThrowIfNull(style);
            _backgroundLayerStyle = style;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Configures an optional foreground drawn above the background.</para>
        ///     <para xml:lang="zh-CN">配置绘制在背景上方的可选前景。</para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder Foreground(VisualCueSet cueSet, string? loopCueName = null)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _foregroundCueSet = cueSet;
            _foregroundLoopCue = loopCueName;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the foreground through a <see cref="VisualCueSetBuilder" /> callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="VisualCueSetBuilder" /> 回调配置前景。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder Foreground(Action<VisualCueSetBuilder> configure,
            string? loopCueName = null)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var inner = VisualCueSetBuilder.Create();
            configure(inner);
            return Foreground(inner.Build(), loopCueName);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies a style to the foreground sprite.</para>
        ///     <para xml:lang="zh-CN">将样式应用到前景精灵。</para>
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder ForegroundStyle(VisualNodeStyle style)
        {
            ArgumentNullException.ThrowIfNull(style);
            _foregroundLayerStyle = style;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds the stage definition. Either cue-based background content or
        ///         <see cref="BackgroundVideo" /> must be configured.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建舞台定义。必须配置基于提示的背景内容或 <see cref="BackgroundVideo" />。
        ///     </para>
        /// </summary>
        public AncientEventStageProceduralVisualSet Build()
        {
            var hasVideo = !string.IsNullOrWhiteSpace(_backgroundVideoPath);
            return hasVideo switch
            {
                true when _backgroundCueSet != null => throw new InvalidOperationException(
                    "Use either Background(...) or BackgroundVideo(...), not both."),
                false when _backgroundCueSet == null => throw new InvalidOperationException(
                    "Set Background(...) or BackgroundVideo(...)."),
                _ => hasVideo
                    ? new(null, null, _backgroundVideoPath, _foregroundCueSet, _foregroundLoopCue, null,
                        _foregroundLayerStyle)
                    : new(_backgroundCueSet, _backgroundLoopCue, null, _foregroundCueSet, _foregroundLoopCue,
                        _backgroundLayerStyle, _foregroundLayerStyle),
            };
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the entry point for procedural Ancient event stage layers used by
    ///         <see cref="AncientEventPresentationAssetProfile" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 <see cref="AncientEventPresentationAssetProfile" /> 使用的程序化先古之民事件舞台图层入口。
    ///     </para>
    /// </summary>
    public static class ModAncientStageVisuals
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an <see cref="AncientEventStageProceduralVisualSetBuilder" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <see cref="AncientEventStageProceduralVisualSetBuilder" />。
        ///     </para>
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Stage()
        {
            return AncientEventStageProceduralVisualSetBuilder.Create();
        }
    }
}
