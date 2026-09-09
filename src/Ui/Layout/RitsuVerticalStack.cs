using Godot;

namespace STS2RitsuLib.Ui.Layout
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Lays out visible child controls vertically at the available width, recalculating their minimum
    ///         heights. Use on the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">按可用宽度纵向排列可见子控件，并重新计算它们的最小高度。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class RitsuVerticalStack : Container
    {
        private static readonly HashSet<RitsuVerticalStack> DeferredLayoutStacks = [];
        private static int _layoutDeferDepth;
        private float _layoutWidthOverride = -1f;
        private int _separation;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="separation">
        ///     <para xml:lang="en">Nonnegative pixel spacing between visible children.</para>
        ///     <para xml:lang="zh-CN">可见子控件间的非负像素间距。</para>
        /// </param>
        public RitsuVerticalStack(int separation)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(separation);
            _separation = separation;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public RitsuVerticalStack() : this(0)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the nonnegative pixel spacing between visible children. Negative values throw
        ///         ArgumentOutOfRangeException.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置可见子控件间的非负像素间距。负值抛出 ArgumentOutOfRangeException。</para>
        /// </summary>
        public int Separation
        {
            get => _separation;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                if (_separation == value)
                    return;
                _separation = value;
                RequestLayout();
            }
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            switch (what)
            {
                case (int)NotificationSortChildren:
                    if (_layoutDeferDepth > 0)
                    {
                        DeferredLayoutStacks.Add(this);
                        return;
                    }

                    LayoutChildren();
                    return;
                case (int)NotificationResized:
                case (int)NotificationChildOrderChanged:
                    if (_layoutDeferDepth > 0)
                        DeferredLayoutStacks.Add(this);
                    else
                        QueueSort();
                    break;
            }
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            var layoutWidth = ResolveLayoutWidth();
            var minHeight = 0f;
            var minWidth = 0f;
            var visibleCount = 0;
            foreach (var child in GetChildren())
            {
                if (child is not Control control || !IsInstanceValid(control) || !control.Visible)
                    continue;

                if (layoutWidth > 1f)
                    PrepareChildWidth(control, layoutWidth);
                var childMin = control.GetCombinedMinimumSize();
                minHeight += childMin.Y;
                if (layoutWidth <= 1f)
                    minWidth = Math.Max(minWidth, childMin.X);
                visibleCount++;
            }

            if (visibleCount > 1)
                minHeight += (float)_separation * (visibleCount - 1);
            if (layoutWidth > 1f)
                return new(1f, minHeight);

            return new(minWidth, minHeight);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Overrides the layout width; finite values at most one restore automatic width. Requests a fresh
        ///         layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">覆盖布局宽度；不大于一的有限值恢复自动宽度，并请求重新布局。</para>
        /// </summary>
        /// <param name="width">
        ///     <para xml:lang="en">Finite width override; values at most one restore automatic sizing.</para>
        ///     <para xml:lang="zh-CN">有限的宽度覆盖值；不大于一时恢复自动尺寸。</para>
        /// </param>
        public void SetLayoutWidth(float width)
        {
            if (!float.IsFinite(width))
                throw new ArgumentOutOfRangeException(nameof(width));
            SetLayoutWidth(width, true);
        }

        internal void SetLayoutWidthFromParent(float width)
        {
            SetLayoutWidth(width, false);
        }

        private void SetLayoutWidth(float width, bool requestLayout)
        {
            var normalized = width > 1f ? width : -1f;
            if (Math.Abs(_layoutWidthOverride - normalized) < 0.5f)
                return;

            _layoutWidthOverride = normalized;
            if (requestLayout)
                RequestLayout();
        }

        /// <summary>
        ///     <para xml:lang="en">Requests a layout and minimum-size refresh for this control.</para>
        ///     <para xml:lang="zh-CN">请求刷新此控件的布局及最小尺寸。</para>
        /// </summary>
        public void RequestLayout()
        {
            if (_layoutDeferDepth > 0)
            {
                DeferredLayoutStacks.Add(this);
                return;
            }

            UpdateMinimumSize();
            if (IsInsideTree())
                QueueSort();
        }

        /// <summary>
        ///     <para xml:lang="en">Requests layouts for this control and its relevant ancestors.</para>
        ///     <para xml:lang="zh-CN">为此控件及相关祖先节点请求重新布局。</para>
        /// </summary>
        /// <param name="node">
        ///     <para xml:lang="en">Non-null starting control.</para>
        ///     <para xml:lang="zh-CN">非 null 的起始控件。</para>
        /// </param>
        public static void RequestAncestorLayouts(Control node)
        {
            ArgumentNullException.ThrowIfNull(node);
            for (var current = node; current != null; current = current.GetParent() as Control)
                switch (current)
                {
                    case RitsuVerticalStack stack:
                        stack.RequestLayout();
                        continue;
                    case RitsuFixedWidthScrollContent scrollContent:
                        scrollContent.RequestLayout();
                        break;
                }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Defers vertical-stack layout requests until the outermost scope is disposed. Scopes affect all
        ///         stacks and must be disposed on the Godot main thread; disposal is idempotent.
        ///     </para>
        ///     <para xml:lang="zh-CN">将纵向堆叠布局请求推迟到最外层作用域释放。作用域影响所有堆叠容器，必须在 Godot 主线程释放；重复释放无副作用。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">An idempotent scope that must be disposed on the main thread.</para>
        ///     <para xml:lang="zh-CN">必须在主线程释放的幂等作用域。</para>
        /// </returns>
        public static IDisposable DeferLayoutRequests()
        {
            _layoutDeferDepth++;
            return new DeferredLayoutScope();
        }

        private static void FlushDeferredLayouts()
        {
            if (DeferredLayoutStacks.Count == 0)
                return;

            var stacks = DeferredLayoutStacks
                .Where(IsInstanceValid)
                .OrderByDescending(GetTreeDepth)
                .ToArray();
            DeferredLayoutStacks.Clear();
            foreach (var stack in stacks)
            {
                stack.UpdateMinimumSize();
                if (stack.IsInsideTree())
                    stack.QueueSort();
            }

            foreach (var stack in stacks.Reverse())
            {
                if (!IsInstanceValid(stack))
                    continue;

                stack.UpdateMinimumSize();
                if (stack.IsInsideTree())
                    stack.QueueSort();
            }
        }

        private static int GetTreeDepth(Node node)
        {
            var depth = 0;
            for (var current = node.GetParent(); current != null; current = current.GetParent())
                depth++;
            return depth;
        }

        private void LayoutChildren()
        {
            var width = ResolveLayoutWidth();
            if (width <= 1f)
                width = Size.X;
            var y = 0f;
            var placedAny = false;
            foreach (var child in GetChildren())
            {
                if (child is not Control control || !IsInstanceValid(control) || !control.Visible)
                    continue;

                if (placedAny)
                    y += _separation;

                if (width > 1f)
                    PrepareChildWidth(control, width);
                var childMin = control.GetCombinedMinimumSize();
                control.Position = new(0f, y);
                control.Size = new(width > 1f ? width : childMin.X, childMin.Y);
                y += childMin.Y;
                placedAny = true;
            }
        }

        private float ResolveLayoutWidth()
        {
            if (_layoutWidthOverride > 1f)
                return _layoutWidthOverride;
            return Size.X > 1f ? Size.X : 0f;
        }

        private static void PrepareChildWidth(Control control, float width)
        {
            if (width <= 1f)
                return;

            if (control is RitsuVerticalStack stack)
                stack.SetLayoutWidthFromParent(width);
            if (Math.Abs(control.Size.X - width) >= 0.5f)
                control.Size = new(width, control.Size.Y);
        }

        private sealed class DeferredLayoutScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _layoutDeferDepth = Math.Max(0, _layoutDeferDepth - 1);
                if (_layoutDeferDepth == 0)
                    FlushDeferredLayouts();
            }
        }
    }
}
