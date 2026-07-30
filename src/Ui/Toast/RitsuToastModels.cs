using Godot;

namespace STS2RitsuLib.Ui.Toast
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies a tracked toast and provides operations for querying, updating, or closing it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标识一条已跟踪的浮动通知，并提供查询、更新及关闭操作。
    ///     </para>
    /// </summary>
    public sealed class RitsuToastHandle
    {
        internal RitsuToastHandle(Guid id)
        {
            Id = id;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable identifier assigned to the tracked toast.</para>
        ///     <para xml:lang="zh-CN">获取分配给该已跟踪浮动通知的稳定标识符。</para>
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Determines whether the toast is queued or active and has not begun closing.</para>
        ///     <para xml:lang="zh-CN">确定该浮动通知是否仍在队列中或处于活动状态，且尚未开始关闭。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast is still alive; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若该浮动通知仍有效则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool IsAlive()
        {
            return RitsuToastService.IsAlive(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a queued toast or requests that an active toast close.</para>
        ///     <para xml:lang="zh-CN">移除队列中的浮动通知，或请求关闭活动中的浮动通知。</para>
        /// </summary>
        /// <param name="immediate">
        ///     <para xml:lang="en">Whether an active toast should close without playing its exit animation.</para>
        ///     <para xml:lang="zh-CN">活动中的浮动通知是否应跳过退出动画并立即关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the service found the toast; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若服务找到了该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool Close(bool immediate = false)
        {
            return RitsuToastService.Close(this, immediate);
        }

        /// <summary>
        ///     <para xml:lang="en">Provides an alias for <see cref="Close" />.</para>
        ///     <para xml:lang="zh-CN">提供 <see cref="Close" /> 的别名。</para>
        /// </summary>
        /// <param name="immediate">
        ///     <para xml:lang="en">Whether an active toast should close without playing its exit animation.</para>
        ///     <para xml:lang="zh-CN">活动中的浮动通知是否应跳过退出动画并立即关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the service found the toast; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若服务找到了该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool Dismiss(bool immediate = false)
        {
            return Close(immediate);
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the request associated with this handle.</para>
        ///     <para xml:lang="zh-CN">替换与该句柄关联的请求。</para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">The replacement request.</para>
        ///     <para xml:lang="zh-CN">替换后的请求。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若浮动通知已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="request" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="request" /> 为 <see langword="null" />。</para>
        /// </exception>
        public bool Update(RitsuToastRequest request, bool resetDuration = true)
        {
            return RitsuToastService.Update(this, request, resetDuration);
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the body while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新正文并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若浮动通知已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public bool UpdateBody(string body, bool resetDuration = true)
        {
            return RitsuToastService.UpdateBody(this, body, resetDuration);
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the body and title while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新正文和标题，并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若浮动通知已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public bool UpdateText(string body, string? title, bool resetDuration = true)
        {
            return RitsuToastService.UpdateText(this, body, title, resetDuration);
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the title while preserving the other request values.</para>
        ///     <para xml:lang="zh-CN">更新标题并保留请求中的其他值。</para>
        /// </summary>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <param name="resetDuration">
        ///     <para xml:lang="en">Whether to restart the timer if the toast is already active.</para>
        ///     <para xml:lang="zh-CN">若浮动通知已处于活动状态，是否重新开始计时。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the toast was updated; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若已更新该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool UpdateTitle(string? title, bool resetDuration = false)
        {
            return RitsuToastService.UpdateTitle(this, title, resetDuration);
        }

        /// <summary>
        ///     <para xml:lang="en">Restarts the timer and optionally replaces the per-toast duration.</para>
        ///     <para xml:lang="zh-CN">重新开始计时，并可选择替换该浮动通知的持续时间。</para>
        /// </summary>
        /// <param name="durationSeconds">
        ///     <para xml:lang="en">
        ///         The new duration in seconds, or <see langword="null" /> to reuse the request or global duration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         新的持续时间（秒）；传入 <see langword="null" /> 可继续使用请求值或全局值。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the service found the toast; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">若服务找到了该浮动通知则为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool ResetDuration(double? durationSeconds = null)
        {
            return RitsuToastService.ResetDuration(this, durationSeconds);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the semantic level used to select default toast colors.</para>
    ///     <para xml:lang="zh-CN">指定用于选择浮动通知默认颜色的语义级别。</para>
    /// </summary>
    public enum RitsuToastLevel
    {
        /// <summary>
        ///     <para xml:lang="en">An informational message.</para>
        ///     <para xml:lang="zh-CN">信息消息。</para>
        /// </summary>
        Info,

        /// <summary>
        ///     <para xml:lang="en">A warning message.</para>
        ///     <para xml:lang="zh-CN">警告消息。</para>
        /// </summary>
        Warning,

        /// <summary>
        ///     <para xml:lang="en">An error message.</para>
        ///     <para xml:lang="zh-CN">错误消息。</para>
        /// </summary>
        Error,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies a built-in animation used when a toast enters or exits.</para>
    ///     <para xml:lang="zh-CN">指定浮动通知进入或退出时使用的内置动画。</para>
    /// </summary>
    public enum RitsuToastAnimationPreset
    {
        /// <summary>
        ///     <para xml:lang="en">Fades the toast without sliding or scaling it.</para>
        ///     <para xml:lang="zh-CN">仅使浮动通知淡入淡出，不进行滑动或缩放。</para>
        /// </summary>
        Fade,

        /// <summary>
        ///     <para xml:lang="en">Combines fading with a slide based on the toast anchor.</para>
        ///     <para xml:lang="zh-CN">将淡入淡出与基于浮动通知锚点方向的滑动结合。</para>
        /// </summary>
        FadeSlide,

        /// <summary>
        ///     <para xml:lang="en">Combines fading with a scale animation.</para>
        ///     <para xml:lang="zh-CN">将淡入淡出与缩放动画结合。</para>
        /// </summary>
        FadeScale,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the toast stack anchor on a three-by-three viewport grid.</para>
    ///     <para xml:lang="zh-CN">指定浮动通知堆栈在视口三乘三网格上的锚点。</para>
    /// </summary>
    public enum RitsuToastAnchor
    {
        /// <summary>
        ///     <para xml:lang="en">The top-left anchor.</para>
        ///     <para xml:lang="zh-CN">左上锚点。</para>
        /// </summary>
        TopLeft,

        /// <summary>
        ///     <para xml:lang="en">The top-center anchor.</para>
        ///     <para xml:lang="zh-CN">顶部居中锚点。</para>
        /// </summary>
        TopCenter,

        /// <summary>
        ///     <para xml:lang="en">The top-right anchor.</para>
        ///     <para xml:lang="zh-CN">右上锚点。</para>
        /// </summary>
        TopRight,

        /// <summary>
        ///     <para xml:lang="en">The middle-left anchor.</para>
        ///     <para xml:lang="zh-CN">中部靠左锚点。</para>
        /// </summary>
        MiddleLeft,

        /// <summary>
        ///     <para xml:lang="en">The middle-center anchor.</para>
        ///     <para xml:lang="zh-CN">中部居中锚点。</para>
        /// </summary>
        MiddleCenter,

        /// <summary>
        ///     <para xml:lang="en">The middle-right anchor.</para>
        ///     <para xml:lang="zh-CN">中部靠右锚点。</para>
        /// </summary>
        MiddleRight,

        /// <summary>
        ///     <para xml:lang="en">The bottom-left anchor.</para>
        ///     <para xml:lang="zh-CN">左下锚点。</para>
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     <para xml:lang="en">The bottom-center anchor.</para>
        ///     <para xml:lang="zh-CN">底部居中锚点。</para>
        /// </summary>
        BottomCenter,

        /// <summary>
        ///     <para xml:lang="en">The bottom-right anchor.</para>
        ///     <para xml:lang="zh-CN">右下锚点。</para>
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
    ///     <para xml:lang="en">
    ///         Describes the content, lifetime, interaction, and presentation options of a toast.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述浮动通知的内容、生命周期、交互方式及呈现选项。
    ///     </para>
    /// </summary>
    public sealed record RitsuToastRequest
    {
        private string _body = string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Initializes a toast request.</para>
        ///     <para xml:lang="zh-CN">初始化一个浮动通知请求。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title displayed above the body.</para>
        ///     <para xml:lang="zh-CN">显示在正文上方的可选标题。</para>
        /// </param>
        /// <param name="image">
        ///     <para xml:lang="en">The optional image displayed beside the text.</para>
        ///     <para xml:lang="zh-CN">显示在文本旁的可选图像。</para>
        /// </param>
        /// <param name="level">
        ///     <para xml:lang="en">The semantic level used to select default colors.</para>
        ///     <para xml:lang="zh-CN">用于选择默认颜色的语义级别。</para>
        /// </param>
        /// <param name="durationSeconds">
        ///     <para xml:lang="en">
        ///         The optional display duration in seconds. <see langword="null" /> uses the global setting.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的显示持续时间（秒）；<see langword="null" /> 表示使用全局设置。
        ///     </para>
        /// </param>
        /// <param name="onClick">
        ///     <para xml:lang="en">The optional callback invoked when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">点击浮动通知时调用的可选回调。</para>
        /// </param>
        /// <param name="animationOverride">
        ///     <para xml:lang="en">The optional animation preset used instead of the global setting.</para>
        ///     <para xml:lang="zh-CN">用于替代全局设置的可选动画预设。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
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
        ///     <para xml:lang="en">Gets or initializes the required body text.</para>
        ///     <para xml:lang="zh-CN">获取或初始化必需的正文文本。</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The assigned value is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">赋予的值为 <see langword="null" />。</para>
        /// </exception>
        public string Body
        {
            get => _body;
            init
            {
                ArgumentNullException.ThrowIfNull(value);
                _body = value;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the optional title displayed above the body.</para>
        ///     <para xml:lang="zh-CN">获取或初始化显示在正文上方的可选标题。</para>
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the optional image displayed beside the text.</para>
        ///     <para xml:lang="zh-CN">获取或初始化显示在文本旁的可选图像。</para>
        /// </summary>
        public Texture2D? Image { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the semantic level used to select default colors.</para>
        ///     <para xml:lang="zh-CN">获取或初始化用于选择默认颜色的语义级别。</para>
        /// </summary>
        public RitsuToastLevel Level { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional display duration in seconds. <see langword="null" /> or a
        ///         non-finite value uses the global setting; zero or a negative value disables automatic closing.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化可选的显示持续时间（秒）。<see langword="null" /> 或非有限值表示使用全局设置；
        ///         零或负值表示不自动关闭。
        ///     </para>
        /// </summary>
        public double? DurationSeconds { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional callback invoked when the notification is clicked. Callback
        ///         exceptions are logged and do not escape the toast input handler.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化点击浮动通知时调用的可选回调。回调异常会被记录，不会逸出浮动通知输入处理器。
        ///     </para>
        /// </summary>
        public Action? OnClick { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional animation preset used instead of the global setting.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化用于替代全局设置的可选动画预设。</para>
        /// </summary>
        public RitsuToastAnimationPreset? AnimationOverride { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether automatic closing is disabled. A persistent toast remains closable
        ///         through the service, its handle, click-to-dismiss behavior, or disabling toast settings.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化是否禁用自动关闭。持久浮动通知仍可通过服务、句柄、点击关闭行为或禁用浮动通知设置来关闭。
        ///     </para>
        /// </summary>
        public bool IsPersistent { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional explicit progress value. Finite values are clamped to the range
        ///         from zero to one, and non-finite values are treated as zero. When unset, timed toasts show their
        ///         remaining-time progress.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化可选的显式进度值。有限值会限制在零到一之间，非有限值按零处理；
        ///         未设置时，限时浮动通知会显示剩余时间进度。
        ///     </para>
        /// </summary>
        public float? ProgressFraction { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether clicking the toast requests that it close. The default is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化点击浮动通知时是否请求将其关闭。默认值为 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool DismissOnClick { get; init; } = true;

        internal RitsuToastVisualStyle? StyleOverride { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates an informational toast request.</para>
        ///     <para xml:lang="zh-CN">创建一条信息浮动通知请求。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The new informational request.</para>
        ///     <para xml:lang="zh-CN">新建的信息请求。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastRequest Info(string body, string? title = null)
        {
            return new(body, title);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a warning toast request.</para>
        ///     <para xml:lang="zh-CN">创建一条警告浮动通知请求。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The new warning request.</para>
        ///     <para xml:lang="zh-CN">新建的警告请求。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastRequest Warning(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Warning);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an error toast request.</para>
        ///     <para xml:lang="zh-CN">创建一条错误浮动通知请求。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The required body text.</para>
        ///     <para xml:lang="zh-CN">必需的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The optional title.</para>
        ///     <para xml:lang="zh-CN">可选标题。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The new error request.</para>
        ///     <para xml:lang="zh-CN">新建的错误请求。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public static RitsuToastRequest Error(string body, string? title = null)
        {
            return new(body, title, null, RitsuToastLevel.Error);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with different body text.</para>
        ///     <para xml:lang="zh-CN">创建正文文本不同的副本。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public RitsuToastRequest WithBody(string body)
        {
            return this with { Body = body };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different title.</para>
        ///     <para xml:lang="zh-CN">创建标题不同的副本。</para>
        /// </summary>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithTitle(string? title)
        {
            return this with { Title = title };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with different body and title text.</para>
        ///     <para xml:lang="zh-CN">创建正文和标题文本不同的副本。</para>
        /// </summary>
        /// <param name="body">
        ///     <para xml:lang="en">The replacement body text.</para>
        ///     <para xml:lang="zh-CN">替换后的正文文本。</para>
        /// </param>
        /// <param name="title">
        ///     <para xml:lang="en">The replacement title, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的标题；传入 <see langword="null" /> 可隐藏标题。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="body" /> is <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN"><paramref name="body" /> 为 <see langword="null" />。</para>
        /// </exception>
        public RitsuToastRequest WithText(string body, string? title)
        {
            return this with { Body = body, Title = title };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different image.</para>
        ///     <para xml:lang="zh-CN">创建图像不同的副本。</para>
        /// </summary>
        /// <param name="image">
        ///     <para xml:lang="en">The replacement image, or <see langword="null" /> to hide it.</para>
        ///     <para xml:lang="zh-CN">替换后的图像；传入 <see langword="null" /> 可隐藏图像。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithImage(Texture2D? image)
        {
            return this with { Image = image };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different semantic level.</para>
        ///     <para xml:lang="zh-CN">创建语义级别不同的副本。</para>
        /// </summary>
        /// <param name="level">
        ///     <para xml:lang="en">The replacement semantic level.</para>
        ///     <para xml:lang="zh-CN">替换后的语义级别。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithLevel(RitsuToastLevel level)
        {
            return this with { Level = level };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different per-toast duration.</para>
        ///     <para xml:lang="zh-CN">创建单条浮动通知持续时间不同的副本。</para>
        /// </summary>
        /// <param name="durationSeconds">
        ///     <para xml:lang="en">
        ///         The replacement duration in seconds, or <see langword="null" /> to use the global setting.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         替换后的持续时间（秒）；传入 <see langword="null" /> 可使用全局设置。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithDuration(double? durationSeconds)
        {
            return this with { DurationSeconds = durationSeconds };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different click callback.</para>
        ///     <para xml:lang="zh-CN">创建点击回调不同的副本。</para>
        /// </summary>
        /// <param name="onClick">
        ///     <para xml:lang="en">The replacement callback, or <see langword="null" /> to remove it.</para>
        ///     <para xml:lang="zh-CN">替换后的回调；传入 <see langword="null" /> 可移除回调。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithClick(Action? onClick)
        {
            return this with { OnClick = onClick };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different animation override.</para>
        ///     <para xml:lang="zh-CN">创建动画覆盖不同的副本。</para>
        /// </summary>
        /// <param name="animationOverride">
        ///     <para xml:lang="en">
        ///         The replacement preset, or <see langword="null" /> to use the global setting.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         替换后的预设；传入 <see langword="null" /> 可使用全局设置。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithAnimation(RitsuToastAnimationPreset? animationOverride)
        {
            return this with { AnimationOverride = animationOverride };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different persistent flag.</para>
        ///     <para xml:lang="zh-CN">创建持久标志不同的副本。</para>
        /// </summary>
        /// <param name="isPersistent">
        ///     <para xml:lang="en"><see langword="true" /> to disable automatic closing.</para>
        ///     <para xml:lang="zh-CN">传入 <see langword="true" /> 可禁用自动关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest Persistent(bool isPersistent = true)
        {
            return this with { IsPersistent = isPersistent };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with a different explicit progress value.</para>
        ///     <para xml:lang="zh-CN">创建显式进度值不同的副本。</para>
        /// </summary>
        /// <param name="progressFraction">
        ///     <para xml:lang="en">
        ///         The replacement progress value, or <see langword="null" /> to use remaining-time progress.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         替换后的进度值；传入 <see langword="null" /> 可使用剩余时间进度。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithProgress(float? progressFraction)
        {
            return this with { ProgressFraction = progressFraction };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a copy with click-to-dismiss behavior enabled or disabled.</para>
        ///     <para xml:lang="zh-CN">创建启用或禁用点击关闭行为的副本。</para>
        /// </summary>
        /// <param name="dismissOnClick">
        ///     <para xml:lang="en"><see langword="true" /> to request closing when the toast is clicked.</para>
        ///     <para xml:lang="zh-CN">传入 <see langword="true" /> 可在点击浮动通知时请求关闭。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The updated request.</para>
        ///     <para xml:lang="zh-CN">更新后的请求。</para>
        /// </returns>
        public RitsuToastRequest WithDismissOnClick(bool dismissOnClick)
        {
            return this with { DismissOnClick = dismissOnClick };
        }
    }
}
