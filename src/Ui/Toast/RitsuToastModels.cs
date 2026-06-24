using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     Handle returned by tracked toast requests for later updates or manual closing.
    ///     可跟踪 toast 请求返回的句柄，用于后续更新或主动关闭。
    /// </summary>
    public sealed class RitsuToastHandle
    {
        internal RitsuToastHandle(Guid id)
        {
            Id = id;
        }

        /// <summary>
        ///     Stable identifier for this toast request.
        ///     此 toast 请求的稳定标识。
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     Returns whether the toast is still pending or visible.
        ///     返回 toast 是否仍在等待显示或已经可见。
        /// </summary>
        public bool IsAlive()
        {
            return RitsuToastService.IsAlive(this);
        }

        /// <summary>
        ///     Closes the toast if it is still pending or visible.
        ///     如果 toast 仍在等待显示或已经可见，则关闭它。
        /// </summary>
        public bool Close(bool immediate = false)
        {
            return RitsuToastService.Close(this, immediate);
        }

        /// <summary>
        ///     Alias for <see cref="Close" />.
        ///     <see cref="Close" /> 的别名。
        /// </summary>
        public bool Dismiss(bool immediate = false)
        {
            return Close(immediate);
        }

        /// <summary>
        ///     Replaces the toast request while preserving the same handle.
        ///     在保留同一句柄的同时替换 toast 请求。
        /// </summary>
        public bool Update(RitsuToastRequest request, bool resetDuration = true)
        {
            return RitsuToastService.Update(this, request, resetDuration);
        }

        /// <summary>
        ///     Updates only the body text.
        ///     仅更新正文文本。
        /// </summary>
        public bool UpdateBody(string body, bool resetDuration = true)
        {
            return RitsuToastService.UpdateBody(this, body, resetDuration);
        }

        /// <summary>
        ///     Updates the body and title text.
        ///     更新正文和标题文本。
        /// </summary>
        public bool UpdateText(string body, string? title, bool resetDuration = true)
        {
            return RitsuToastService.UpdateText(this, body, title, resetDuration);
        }

        /// <summary>
        ///     Updates the title while preserving the body text.
        ///     更新标题并保留正文文本。
        /// </summary>
        public bool UpdateTitle(string? title, bool resetDuration = false)
        {
            return RitsuToastService.UpdateTitle(this, title, resetDuration);
        }

        /// <summary>
        ///     Restarts the remaining display time, optionally overriding the toast duration.
        ///     重新开始剩余显示时间，并可选覆盖 toast 持续时间。
        /// </summary>
        public bool ResetDuration(double? durationSeconds = null)
        {
            return RitsuToastService.ResetDuration(this, durationSeconds);
        }
    }

    /// <summary>
    ///     Semantic toast category used for default styling.
    ///     用于默认样式的语义 toast 类别。
    /// </summary>
    public enum RitsuToastLevel
    {
        /// <summary>
        ///     Informational message.
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
        internal const double DefaultDurationSeconds = 6d;

        public static readonly RitsuToastSettings Default = new(
            true,
            RitsuToastPlacement.Default,
            RitsuToastQueuePolicy.Default,
            DefaultDurationSeconds,
            RitsuToastAnimationPreset.FadeSlide);
    }

    internal sealed record RitsuToastVisualStyle(
        Color Background,
        Color Border,
        Color TitleColor,
        Color BodyColor,
        Color AccentColor,
        Color ProgressTrackColor,
        Color ProgressFillColor,
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
        float ProgressHeight,
        float ProgressSpacing,
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

        /// <summary>
        ///     Keeps the toast visible until the user clicks it or toast settings are disabled.
        ///     让 toast 保持显示，直到用户点击或 toast 设置被禁用。
        /// </summary>
        public bool IsPersistent { get; init; }

        /// <summary>
        ///     Optional explicit progress value rendered in the toast progress bar. When unset, timed toasts use the
        ///     progress bar as a remaining-duration indicator.
        ///     可选显式进度值，用 toast 进度条渲染。未设置时，限时 toast 使用进度条表示剩余显示时间。
        /// </summary>
        public float? ProgressFraction { get; init; }

        /// <summary>
        ///     Whether clicking the toast dismisses it. Defaults to true to preserve normal toast behavior.
        ///     点击 toast 是否关闭它。默认为 true，以保持普通 toast 的既有行为。
        /// </summary>
        public bool DismissOnClick { get; init; } = true;

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

        /// <summary>
        ///     Returns a copy with updated body text.
        ///     返回更新正文文本后的副本。
        /// </summary>
        public RitsuToastRequest WithBody(string body)
        {
            return this with { Body = body };
        }

        /// <summary>
        ///     Returns a copy with an updated title.
        ///     返回更新标题后的副本。
        /// </summary>
        public RitsuToastRequest WithTitle(string? title)
        {
            return this with { Title = title };
        }

        /// <summary>
        ///     Returns a copy with updated body and title text.
        ///     返回更新正文和标题文本后的副本。
        /// </summary>
        public RitsuToastRequest WithText(string body, string? title)
        {
            return this with { Body = body, Title = title };
        }

        /// <summary>
        ///     Returns a copy with an updated leading image.
        ///     返回更新起始侧图像后的副本。
        /// </summary>
        public RitsuToastRequest WithImage(Texture2D? image)
        {
            return this with { Image = image };
        }

        /// <summary>
        ///     Returns a copy with an updated semantic level.
        ///     返回更新语义级别后的副本。
        /// </summary>
        public RitsuToastRequest WithLevel(RitsuToastLevel level)
        {
            return this with { Level = level };
        }

        /// <summary>
        ///     Returns a copy with an updated per-toast duration override.
        ///     返回更新单个 toast 持续时间覆盖值后的副本。
        /// </summary>
        public RitsuToastRequest WithDuration(double? durationSeconds)
        {
            return this with { DurationSeconds = durationSeconds };
        }

        /// <summary>
        ///     Returns a copy with an updated click callback.
        ///     返回更新点击回调后的副本。
        /// </summary>
        public RitsuToastRequest WithClick(Action? onClick)
        {
            return this with { OnClick = onClick };
        }

        /// <summary>
        ///     Returns a copy with an updated animation override.
        ///     返回更新动画覆盖后的副本。
        /// </summary>
        public RitsuToastRequest WithAnimation(RitsuToastAnimationPreset? animationOverride)
        {
            return this with { AnimationOverride = animationOverride };
        }

        /// <summary>
        ///     Returns a copy that is kept visible until clicked, closed, or disabled by settings.
        ///     返回会保持显示直到被点击、关闭或被设置禁用的副本。
        /// </summary>
        public RitsuToastRequest Persistent(bool isPersistent = true)
        {
            return this with { IsPersistent = isPersistent };
        }

        /// <summary>
        ///     Returns a copy with an explicit progress value. Pass <see langword="null" /> to return to duration-based
        ///     progress.
        ///     返回设置显式进度值后的副本。传入 <see langword="null" /> 可恢复为基于持续时间的进度。
        /// </summary>
        public RitsuToastRequest WithProgress(float? progressFraction)
        {
            return this with { ProgressFraction = progressFraction };
        }

        /// <summary>
        ///     Returns a copy with click-to-dismiss behavior configured.
        ///     返回设置点击关闭行为后的副本。
        /// </summary>
        public RitsuToastRequest WithDismissOnClick(bool dismissOnClick)
        {
            return this with { DismissOnClick = dismissOnClick };
        }
    }
}
