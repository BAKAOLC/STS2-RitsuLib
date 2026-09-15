using Godot;

namespace STS2RitsuLib.Ui.Layout
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Constrains one owned content control to a scroll viewport's width while reserving a right
    ///         gutter. Use on the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">将一个自有内容控件约束到滚动视口宽度，并预留右侧空白。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class RitsuFixedWidthScrollContent : Control
    {
        private Control? _content;
        private float _viewportHeight;
        private float _viewportWidth = -1f;

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public RitsuFixedWidthScrollContent()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Ignore;
            ClipContents = true;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the reserved nonnegative right-side width in pixels.</para>
        ///     <para xml:lang="zh-CN">获取右侧预留的非负像素宽度。</para>
        /// </summary>
        public int RightGutter { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Assigns the owned content control and gutter. Repeating with the same control is allowed;
        ///         replacing it requires a new wrapper. Parented or freed content is rejected.
        ///     </para>
        ///     <para xml:lang="zh-CN">指定自有内容控件及右侧空白。允许重复指定同一控件；替换内容须使用新的包装控件。不接受已挂载到其他父节点或已释放的内容。</para>
        /// </summary>
        /// <param name="content">
        ///     <para xml:lang="en">Valid parentless content, or the content already owned by this wrapper.</para>
        ///     <para xml:lang="zh-CN">有效的无父节点内容，或已由此包装控件持有的内容。</para>
        /// </param>
        /// <param name="rightGutter">
        ///     <para xml:lang="en">Reserved pixels on the right; negative values become zero.</para>
        ///     <para xml:lang="zh-CN">右侧预留像素数；负值按零处理。</para>
        /// </param>
        public void Configure(Control content, int rightGutter)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (!IsInstanceValid(content) || ReferenceEquals(content, this) ||
                (content.GetParent() != null && content.GetParent() != this))
                throw new ArgumentException("Content must be valid and parentless, or already owned by this wrapper.",
                    nameof(content));
            if (_content != null && !ReferenceEquals(_content, content))
                throw new InvalidOperationException("This wrapper already owns different content.");
            _content = content;
            RightGutter = Math.Max(0, rightGutter);
            if (content.GetParent() != this)
                AddChild(content);
            RequestLayout();
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the right gutter; negative values are treated as zero.</para>
        ///     <para xml:lang="zh-CN">更新右侧空白；负值按零处理。</para>
        /// </summary>
        /// <param name="rightGutter">
        ///     <para xml:lang="en">Reserved pixels on the right; negative values become zero.</para>
        ///     <para xml:lang="zh-CN">右侧预留像素数；负值按零处理。</para>
        /// </param>
        public void SetRightGutter(int rightGutter)
        {
            rightGutter = Math.Max(0, rightGutter);
            if (RightGutter == rightGutter)
                return;

            RightGutter = rightGutter;
            RequestLayout();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the finite viewport dimensions. Widths at most one restore automatic width; negative
        ///         heights are treated as zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">更新有限的视口尺寸。不大于一的宽度恢复自动宽度；负高度按零处理。</para>
        /// </summary>
        /// <param name="size">
        ///     <para xml:lang="en">Finite viewport dimensions.</para>
        ///     <para xml:lang="zh-CN">有限的视口尺寸。</para>
        /// </param>
        public void SetViewportSize(Vector2 size)
        {
            if (!float.IsFinite(size.X) || !float.IsFinite(size.Y))
                throw new ArgumentOutOfRangeException(nameof(size));
            var width = size.X > 1f ? size.X : -1f;
            var height = Math.Max(0f, size.Y);
            if (Math.Abs(_viewportWidth - width) < 0.5f &&
                Math.Abs(_viewportHeight - height) < 0.5f)
                return;

            _viewportWidth = width;
            _viewportHeight = height;
            RequestLayout();
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            var contentHeight = _content is { Visible: true }
                ? _content.GetCombinedMinimumSize().Y
                : 0f;
            return new(1f, contentHeight);
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            switch (what)
            {
                case (int)NotificationResized:
                case (int)NotificationChildOrderChanged:
                    LayoutChildren();
                    break;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Requests a layout and minimum-size refresh for this control.</para>
        ///     <para xml:lang="zh-CN">请求刷新此控件的布局及最小尺寸。</para>
        /// </summary>
        public void RequestLayout()
        {
            UpdateMinimumSize();
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            var width = ResolveViewportWidth();
            var contentWidth = ResolveContentWidth(width);
            PrepareContentWidth(contentWidth);

            if (_content is not { Visible: true }) return;
            var contentHeight = _content.GetCombinedMinimumSize().Y;
            _content.Position = Vector2.Zero;
            _content.Size = new(contentWidth, contentHeight);
        }

        private void PrepareContentWidth(float width)
        {
            if (_content == null || width <= 1f)
                return;

            if (_content is RitsuVerticalStack stack)
                stack.SetLayoutWidth(width);
            if (Math.Abs(_content.Size.X - width) >= 0.5f)
                _content.Size = new(width, _content.Size.Y);
        }

        private float ResolveViewportWidth()
        {
            if (_viewportWidth > 1f)
                return _viewportWidth;
            return Size.X > 1f ? Size.X : 1f;
        }

        private float ResolveContentWidth(float viewportWidth)
        {
            return Math.Max(1f, viewportWidth - RightGutter);
        }
    }
}
