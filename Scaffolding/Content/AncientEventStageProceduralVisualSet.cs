using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Data-only ancient encounter stage: rear layer is either a looping video (<c>VideoStreamPlayer</c>) or
    ///     <see cref="VisualCueSet" /> sprites / frame sequences; optional foreground uses cue sets only (no video).
    ///     仅数据的远古事件舞台定义：后层可以是循环视频（<c>VideoStreamPlayer</c>），也可以是
    ///     <c>VisualCueSet</c> sprite / 帧序列；可选前景层仅支持 cue set（不支持视频）。
    /// </summary>
    /// <param name="BackgroundCueSet">
    ///     When <paramref name="BackgroundVideoPath" /> is <see langword="null" />, drives the background layer (required
    ///     in that case).
    ///     当 <c>BackgroundVideoPath</c> 为 <see langword="null" /> 时，驱动背景层（这种情况下必填）。
    /// </param>
    /// <param name="BackgroundLoopCueName">
    ///     Primary cue for background sprite playback; when <see langword="null" />, uses <c>loop</c>. Ignored when video
    ///     is used.
    ///     背景 sprite 播放的主 cue；为 <see langword="null" /> 时使用 <c>loop</c>。使用视频时忽略。
    /// </param>
    /// <param name="BackgroundVideoPath">
    ///     Optional <c>res://</c> path to a <c>VideoStream</c> resource (e.g. WebM / Ogg Theora). Mutually exclusive with
    ///     <paramref name="BackgroundCueSet" />.
    ///     指向 <c>VideoStream</c> 资源的可选 <c>res://</c> 路径（例如 WebM / Ogg Theora）。与
    ///     <c>BackgroundCueSet</c> 互斥。
    /// </param>
    /// <param name="ForegroundCueSet">
    ///     Optional front layer (e.g. character); textures or <see cref="VisualFrameSequence" />.
    ///     可选前层（例如角色）；可使用贴图或 <c>VisualFrameSequence</c>。
    /// </param>
    /// <param name="ForegroundLoopCueName">
    ///     Primary foreground cue; when <see langword="null" />, uses <c>loop</c>.
    ///     前景主 cue；为 <see langword="null" /> 时使用 <c>loop</c>。
    /// </param>
    public sealed record AncientEventStageProceduralVisualSet(
        VisualCueSet? BackgroundCueSet = null,
        string? BackgroundLoopCueName = null,
        string? BackgroundVideoPath = null,
        VisualCueSet? ForegroundCueSet = null,
        string? ForegroundLoopCueName = null);

    /// <summary>
    ///     Fluent builder for <see cref="AncientEventStageProceduralVisualSet" />.
    ///     <c>AncientEventStageProceduralVisualSet</c> 的流式 builder。
    /// </summary>
    public sealed class AncientEventStageProceduralVisualSetBuilder
    {
        private VisualCueSet? _backgroundCueSet;
        private string? _backgroundLoopCue;
        private string? _backgroundVideoPath;
        private VisualCueSet? _foregroundCueSet;
        private string? _foregroundLoopCue;

        private AncientEventStageProceduralVisualSetBuilder()
        {
        }

        /// <summary>
        ///     Starts a stage procedural definition.
        ///     开始一个程序化舞台定义。
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Sets the rear layer from cues (mutually exclusive with <see cref="BackgroundVideo" />).
        ///     用 cue 设置后层（与 <c>BackgroundVideo</c> 互斥）。
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
        ///     Configures the rear layer via <see cref="VisualCueSetBuilder" />.
        ///     通过 <c>VisualCueSetBuilder</c> 配置后层。
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
        ///     Sets a looping full-rect background video (mutually exclusive with cue-based <c>Background</c>).
        ///     Use <c>VideoStream</c> formats Godot supports on your export target.
        ///     设置一个全区域循环背景视频（与基于 cue 的 <c>Background</c> 互斥）。请使用目标导出平台上 Godot
        ///     支持的 <c>VideoStream</c> 格式。
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
        ///     Sets an optional front layer (e.g. character) drawn above the background.
        ///     设置绘制在背景之上的可选前层（例如角色）。
        /// </summary>
        public AncientEventStageProceduralVisualSetBuilder Foreground(VisualCueSet cueSet, string? loopCueName = null)
        {
            ArgumentNullException.ThrowIfNull(cueSet);
            _foregroundCueSet = cueSet;
            _foregroundLoopCue = loopCueName;
            return this;
        }

        /// <summary>
        ///     Configures the front layer via <see cref="VisualCueSetBuilder" />.
        ///     通过 <c>VisualCueSetBuilder</c> 配置前层。
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
        ///     Materializes the set. Requires either background cues or <see cref="BackgroundVideo" />.
        ///     实体化该集合。必须提供背景 cue 或 <c>BackgroundVideo</c>。
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
                    ? new(null, null, _backgroundVideoPath, _foregroundCueSet, _foregroundLoopCue)
                    : new(_backgroundCueSet, _backgroundLoopCue, null, _foregroundCueSet, _foregroundLoopCue),
            };
        }
    }

    /// <summary>
    ///     Entry point for ancient stage procedural layers on <see cref="AncientEventPresentationAssetProfile" />.
    ///     <c>AncientEventPresentationAssetProfile</c> 上远古事件程序化舞台图层的入口点。
    /// </summary>
    public static class ModAncientStageVisuals
    {
        /// <summary>
        ///     Begins an <see cref="AncientEventStageProceduralVisualSetBuilder" />.
        ///     开始一个 <c>AncientEventStageProceduralVisualSetBuilder</c>。
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Stage()
        {
            return AncientEventStageProceduralVisualSetBuilder.Create();
        }
    }
}
