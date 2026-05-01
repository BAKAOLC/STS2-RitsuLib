using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     Semantic toast category used for default styling.
    /// </summary>
    public enum RitsuToastLevel
    {
        /// <summary>
        ///     Informational message.
        /// </summary>
        Info,

        /// <summary>
        ///     Warning message.
        /// </summary>
        Warning,

        /// <summary>
        ///     Error message.
        /// </summary>
        Error,
    }

    /// <summary>
    ///     Built-in toast enter/exit animation presets.
    /// </summary>
    public enum RitsuToastAnimationPreset
    {
        /// <summary>
        ///     Fade only.
        /// </summary>
        Fade,

        /// <summary>
        ///     Fade combined with directional slide.
        /// </summary>
        FadeSlide,

        /// <summary>
        ///     Fade combined with scale.
        /// </summary>
        FadeScale,
    }

    /// <summary>
    ///     Toast anchor point on a 3x3 screen grid.
    /// </summary>
    public enum RitsuToastAnchor
    {
        /// <summary>
        ///     Top-left corner.
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Top-center anchor.
        /// </summary>
        TopCenter,

        /// <summary>
        ///     Top-right corner.
        /// </summary>
        TopRight,

        /// <summary>
        ///     Middle-left anchor.
        /// </summary>
        MiddleLeft,

        /// <summary>
        ///     Screen center.
        /// </summary>
        MiddleCenter,

        /// <summary>
        ///     Middle-right anchor.
        /// </summary>
        MiddleRight,

        /// <summary>
        ///     Bottom-left corner.
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Bottom-center anchor.
        /// </summary>
        BottomCenter,

        /// <summary>
        ///     Bottom-right corner.
        /// </summary>
        BottomRight,
    }

    internal sealed record RitsuToastPlacement(RitsuToastAnchor Anchor, Vector2 Offset)
    {
        public static readonly RitsuToastPlacement Default = new(RitsuToastAnchor.TopRight, new(-24f, 24f));
    }

    internal sealed record RitsuToastQueuePolicy(int MaxVisible, float Gap)
    {
        public static readonly RitsuToastQueuePolicy Default = new(3, 12f);
    }

    internal sealed record RitsuToastSettings(
        bool Enabled,
        RitsuToastPlacement Placement,
        RitsuToastQueuePolicy QueuePolicy,
        double DurationSeconds,
        RitsuToastAnimationPreset AnimationPreset)
    {
        public static readonly RitsuToastSettings Default = new(
            true,
            RitsuToastPlacement.Default,
            RitsuToastQueuePolicy.Default,
            3.5d,
            RitsuToastAnimationPreset.FadeSlide);
    }

    internal sealed record RitsuToastVisualStyle(
        Color Background,
        Color Border,
        Color TitleColor,
        Color BodyColor,
        Color AccentColor,
        Color ShadowColor,
        Color InteractiveBadgeBackground,
        Color InteractiveBadgeForeground,
        Color CloseButtonBackground,
        Color CloseButtonBackgroundHover,
        Color CloseButtonBorder,
        Color CloseButtonBorderHover,
        int BorderWidth,
        int CornerRadius,
        int TitleFontSize,
        int BodyFontSize,
        int BadgeFontSize,
        int InteractiveBorderWidth,
        int CloseButtonBorderWidth,
        float ShadowSize,
        float Width,
        float MinHeight,
        float PaddingHorizontal,
        float PaddingVertical,
        float TextSpacing,
        float RowSpacing,
        float ImageSize,
        float CloseButtonSize,
        float CloseButtonPaddingHorizontal,
        float CloseButtonPaddingVertical,
        float InteractiveBadgeHeight,
        float ScreenMargin,
        float EnterDuration,
        float MoveDuration,
        float ExitDuration,
        float EnterSlideDistance,
        float ExitSlideDistance,
        float EnterScale);

    /// <summary>
    ///     Toast payload used by <see cref="RitsuToastService.Show" />.
    /// </summary>
    public sealed record RitsuToastRequest
    {
        /// <summary>
        ///     Creates a toast request.
        /// </summary>
        public RitsuToastRequest(string body, string? title = null, Texture2D? image = null,
            RitsuToastLevel level = RitsuToastLevel.Info, double? durationSeconds = null, Action? onClick = null,
            RitsuToastAnimationPreset? animationOverride = null)
        {
            Body = body;
            Title = title;
            Image = image;
            Level = level;
            DurationSeconds = durationSeconds;
            OnClick = onClick;
            AnimationOverride = animationOverride;
        }

        /// <summary>
        ///     Body text rendered in the toast.
        /// </summary>
        public string Body { get; init; }

        /// <summary>
        ///     Optional title shown above the body.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     Optional image shown on the leading side.
        /// </summary>
        public Texture2D? Image { get; init; }

        /// <summary>
        ///     Semantic level used for default visuals.
        /// </summary>
        public RitsuToastLevel Level { get; init; }

        /// <summary>
        ///     Optional per-toast duration override in seconds.
        /// </summary>
        public double? DurationSeconds { get; init; }

        /// <summary>
        ///     Optional click callback for the toast body.
        /// </summary>
        public Action? OnClick { get; init; }

        /// <summary>
        ///     Optional animation override for this request.
        /// </summary>
        public RitsuToastAnimationPreset? AnimationOverride { get; init; }

        internal RitsuToastVisualStyle? StyleOverride { get; init; }

        /// <summary>
        ///     Creates an informational toast payload.
        /// </summary>
        public static RitsuToastRequest Info(string body, string? title = null)
        {
            return new(body, title);
        }

        /// <summary>
        ///     Creates a warning toast payload.
        /// </summary>
        public static RitsuToastRequest Warning(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Warning);
        }

        /// <summary>
        ///     Creates an error toast payload.
        /// </summary>
        public static RitsuToastRequest Error(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Error);
        }
    }
}
