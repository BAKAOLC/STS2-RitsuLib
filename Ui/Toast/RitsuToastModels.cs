using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     Semantic toast category used for default styling.
    ///     用于默认样式的语义 toast 类别。
    /// </summary>
    public enum RitsuToastLevel
    {
        /// <summary>
        ///     Informational message.
        ///     Informational message.
        ///     信息消息。
        ///     信息消息。
        /// </summary>
        Info,

        /// <summary>
        ///     Warning message.
        ///     警告消息。
        /// </summary>
        Warning,

        /// <summary>
        ///     Error message.
        ///     错误消息。
        /// </summary>
        Error,
    }

    /// <summary>
    ///     Built-in toast enter/exit animation presets.
    ///     内置 toast 进入 / 退出动画预设。
    /// </summary>
    public enum RitsuToastAnimationPreset
    {
        /// <summary>
        ///     Fade only.
        ///     仅淡入淡出。
        /// </summary>
        Fade,

        /// <summary>
        ///     Fade combined with directional slide.
        ///     淡入淡出并带方向滑动。
        /// </summary>
        FadeSlide,

        /// <summary>
        ///     Fade combined with scale.
        ///     淡入淡出并带缩放。
        /// </summary>
        FadeScale,
    }

    /// <summary>
    ///     Toast anchor point on a 3x3 screen grid.
    ///     3x3 屏幕网格上的 toast 锚点。
    /// </summary>
    public enum RitsuToastAnchor
    {
        /// <summary>
        ///     Top-left corner.
        ///     左上角。
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Top-center anchor.
        ///     顶部居中锚点。
        /// </summary>
        TopCenter,

        /// <summary>
        ///     Top-right corner.
        ///     右上角。
        /// </summary>
        TopRight,

        /// <summary>
        ///     Middle-left anchor.
        ///     中部靠左锚点。
        /// </summary>
        MiddleLeft,

        /// <summary>
        ///     Screen center.
        ///     屏幕中心。
        /// </summary>
        MiddleCenter,

        /// <summary>
        ///     Middle-right anchor.
        ///     中部靠右锚点。
        /// </summary>
        MiddleRight,

        /// <summary>
        ///     Bottom-left corner.
        ///     左下角。
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Bottom-center anchor.
        ///     底部居中锚点。
        /// </summary>
        BottomCenter,

        /// <summary>
        ///     Bottom-right corner.
        ///     右下角。
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
    ///     <see cref="RitsuToastService.Show" /> 使用的 toast 载荷。
    /// </summary>
    public sealed record RitsuToastRequest
    {
        /// <summary>
        ///     Creates a toast request.
        ///     创建 toast 请求。
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
        ///     toast 中渲染的正文文本。
        /// </summary>
        public string Body { get; init; }

        /// <summary>
        ///     Optional title shown above the body.
        ///     显示在正文上方的可选标题。
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     Optional image shown on the leading side.
        ///     显示在起始侧的可选图像。
        /// </summary>
        public Texture2D? Image { get; init; }

        /// <summary>
        ///     Semantic level used for default visuals.
        ///     用于默认视觉效果的语义级别。
        /// </summary>
        public RitsuToastLevel Level { get; init; }

        /// <summary>
        ///     Optional per-toast duration override in seconds.
        ///     单个 toast 的可选持续时间覆盖值，单位为秒。
        /// </summary>
        public double? DurationSeconds { get; init; }

        /// <summary>
        ///     Optional click callback for the toast body.
        ///     toast 正文的可选点击回调。
        /// </summary>
        public Action? OnClick { get; init; }

        /// <summary>
        ///     Optional animation override for this request.
        ///     此请求的可选动画覆盖。
        /// </summary>
        public RitsuToastAnimationPreset? AnimationOverride { get; init; }

        internal RitsuToastVisualStyle? StyleOverride { get; init; }

        /// <summary>
        ///     Creates an informational toast payload.
        ///     创建信息 toast 载荷。
        /// </summary>
        public static RitsuToastRequest Info(string body, string? title = null)
        {
            return new(body, title);
        }

        /// <summary>
        ///     Creates a warning toast payload.
        ///     创建警告 toast 载荷。
        /// </summary>
        public static RitsuToastRequest Warning(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Warning);
        }

        /// <summary>
        ///     Creates an error toast payload.
        ///     创建错误 toast 载荷。
        /// </summary>
        public static RitsuToastRequest Error(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Error);
        }
    }
}
