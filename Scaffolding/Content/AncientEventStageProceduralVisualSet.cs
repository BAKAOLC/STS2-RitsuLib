using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Data-only ancient encounter stage: rear layer is either a looping video (<c>VideoStreamPlayer</c>) or
    ///     <see cref="VisualCueSet" /> sprites / frame sequences; optional foreground uses cue sets only (no video).
    /// </summary>
    /// <param name="BackgroundCueSet">
    ///     When <paramref name="BackgroundVideoPath" /> is <see langword="null" />, drives the background layer (required
    ///     in that case).
    /// </param>
    /// <param name="BackgroundLoopCueName">
    ///     Primary cue for background sprite playback; when <see langword="null" />, uses <c>loop</c>. Ignored when video
    ///     is used.
    /// </param>
    /// <param name="BackgroundVideoPath">
    ///     Optional <c>res://</c> path to a <c>VideoStream</c> resource (e.g. WebM / Ogg Theora). Mutually exclusive with
    ///     <paramref name="BackgroundCueSet" />.
    /// </param>
    /// <param name="ForegroundCueSet">Optional front layer (e.g. character); textures or <see cref="VisualFrameSequence" />.</param>
    /// <param name="ForegroundLoopCueName">Primary foreground cue; when <see langword="null" />, uses <c>loop</c>.</param>
    public sealed record AncientEventStageProceduralVisualSet(
        VisualCueSet? BackgroundCueSet = null,
        string? BackgroundLoopCueName = null,
        string? BackgroundVideoPath = null,
        VisualCueSet? ForegroundCueSet = null,
        string? ForegroundLoopCueName = null);

    /// <summary>
    ///     Fluent builder for <see cref="AncientEventStageProceduralVisualSet" />.
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
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Sets the rear layer from cues (mutually exclusive with <see cref="BackgroundVideo" />).
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
    /// </summary>
    public static class ModAncientStageVisuals
    {
        /// <summary>
        ///     Begins an <see cref="AncientEventStageProceduralVisualSetBuilder" />.
        /// </summary>
        public static AncientEventStageProceduralVisualSetBuilder Stage()
        {
            return AncientEventStageProceduralVisualSetBuilder.Create();
        }
    }
}
