using Godot;

namespace STS2RitsuLib.Ui.Windows
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures the title, size limits, placement, and optional movement or resizing of a
    ///         <see cref="RitsuFloatingWindow" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         配置 <see cref="RitsuFloatingWindow" /> 的标题、尺寸限制、初始位置，以及可选的移动和缩放能力。
    ///     </para>
    /// </summary>
    public sealed class RitsuFloatingWindowOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the title displayed in the window header.</para>
        ///     <para xml:lang="zh-CN">获取窗口标题栏中显示的标题。</para>
        /// </summary>
        public string Title { get; init; } = "Window";

        /// <summary>
        ///     <para xml:lang="en">Gets the initial unscaled window size.</para>
        ///     <para xml:lang="zh-CN">获取窗口未经缩放的初始尺寸。</para>
        /// </summary>
        public Vector2 InitialSize { get; init; } = new(960f, 720f);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the window uses its content's complete minimum size when it first opens. The result is
        ///         still constrained by <see cref="MinimumSize" />, <see cref="MaximumSize" />, and the viewport.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取窗口首次打开时是否采用内容的完整最小尺寸。最终尺寸仍受 <see cref="MinimumSize" />、
        ///         <see cref="MaximumSize" /> 及视口范围约束。
        ///     </para>
        /// </summary>
        public bool FitInitialSizeToContent { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the minimum unscaled window size.</para>
        ///     <para xml:lang="zh-CN">获取窗口未经缩放的最小尺寸。</para>
        /// </summary>
        public Vector2 MinimumSize { get; init; } = new(480f, 320f);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the maximum unscaled window size. A zero component uses the corresponding viewport dimension.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取窗口未经缩放的最大尺寸。分量为零时使用对应的视口尺寸。
        ///     </para>
        /// </summary>
        public Vector2 MaximumSize { get; init; } = Vector2.Zero;

        /// <summary>
        ///     <para xml:lang="en">Gets whether dragging the header moves the window.</para>
        ///     <para xml:lang="zh-CN">获取是否允许拖动标题栏来移动窗口。</para>
        /// </summary>
        public bool Movable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the window exposes edge and corner resize handles.</para>
        ///     <para xml:lang="zh-CN">获取窗口是否提供边缘及角落缩放区域。</para>
        /// </summary>
        public bool Resizable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the header contains a close button.</para>
        ///     <para xml:lang="zh-CN">获取标题栏是否包含关闭按钮。</para>
        /// </summary>
        public bool Closable { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the window is centered the first time it enters a viewport.</para>
        ///     <para xml:lang="zh-CN">获取窗口首次进入视口时是否居中。</para>
        /// </summary>
        public bool StartCentered { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether moving and resizing keep the complete window inside the viewport when the viewport can
        ///         accommodate <see cref="MinimumSize" />. The minimum size takes precedence in smaller viewports.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取移动和缩放时是否在视口可容纳 <see cref="MinimumSize" /> 的前提下，将完整窗口约束在视口内；
        ///         视口更小时最小尺寸优先。
        ///     </para>
        /// </summary>
        public bool ConstrainToViewport { get; init; } = true;

        internal bool CompactChrome { get; init; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Title))
                throw new ArgumentException("Window title cannot be null, empty, or whitespace.", nameof(Title));
            if (Title.Length > 256)
                throw new ArgumentOutOfRangeException(nameof(Title), "Window title cannot exceed 256 characters.");
            ValidatePositiveSize(InitialSize, nameof(InitialSize));
            ValidatePositiveSize(MinimumSize, nameof(MinimumSize));
            if (!float.IsFinite(MaximumSize.X) || !float.IsFinite(MaximumSize.Y) ||
                MaximumSize.X < 0f || MaximumSize.Y < 0f)
                throw new ArgumentOutOfRangeException(nameof(MaximumSize),
                    "Maximum size components must be finite and cannot be negative.");
            if ((MaximumSize.X > 0f && MaximumSize.X < MinimumSize.X) ||
                (MaximumSize.Y > 0f && MaximumSize.Y < MinimumSize.Y))
                throw new ArgumentException("Maximum size cannot be smaller than minimum size.", nameof(MaximumSize));
        }

        private static void ValidatePositiveSize(Vector2 value, string parameterName)
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || value.X <= 0f || value.Y <= 0f)
                throw new ArgumentOutOfRangeException(parameterName,
                    "Size components must be finite and greater than zero.");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Represents the unscaled position and size of a floating window.</para>
    ///     <para xml:lang="zh-CN">表示浮动窗口未经缩放的位置与尺寸。</para>
    /// </summary>
    /// <param name="Position">
    ///     <para xml:lang="en">The window position in its parent control's coordinate space.</para>
    ///     <para xml:lang="zh-CN">窗口在其父控件坐标空间中的位置。</para>
    /// </param>
    /// <param name="Size">
    ///     <para xml:lang="en">The unscaled window size.</para>
    ///     <para xml:lang="zh-CN">窗口未经缩放的尺寸。</para>
    /// </param>
    public readonly record struct RitsuFloatingWindowGeometry(Vector2 Position, Vector2 Size);
}
