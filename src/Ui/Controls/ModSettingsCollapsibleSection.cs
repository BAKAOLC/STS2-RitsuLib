using Godot;
using STS2RitsuLib.Ui.Layout;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A reusable collapsible content section with optional header actions and lazy content. Use on
    ///         the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">支持可选标题操作和延迟内容创建的可复用折叠栏。请在 Godot 主线程使用。</para>
    /// </summary>
    public sealed partial class ModSettingsCollapsibleSection : Control
    {
        private readonly Control[]? _contentControls;
        private readonly string? _description;
        private readonly ModSettingsActionsButton? _headerActions;
        private readonly string? _sectionId;
        private readonly string? _title;
        private Control? _content;
        private bool _contentEnabled = true;
        private Control? _header;
        private bool _layoutRefreshDeferred;
        private Action? _lazyContentBuilder;
        private bool _lazyContentBuilt;
        private int _separation;
        private StyleBoxFlat _surfaceStyle = RitsuShellChromeStyles.CreateSurfaceStyle();
        private ModSettingsCollapsibleHeaderButton? _toggle;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="title">
        ///     <para xml:lang="en">Non-null title text; an empty title is allowed.</para>
        ///     <para xml:lang="zh-CN">非 null 的标题文本；允许为空字符串。</para>
        /// </param>
        /// <param name="sectionId">
        ///     <para xml:lang="en">Optional local node-name suffix; it is not a global registration ID.</para>
        ///     <para xml:lang="zh-CN">可选的局部节点名称后缀，不是全局注册 ID。</para>
        /// </param>
        /// <param name="description">
        ///     <para xml:lang="en">Optional description below the header title.</para>
        ///     <para xml:lang="zh-CN">标题下方的可选描述。</para>
        /// </param>
        /// <param name="startCollapsed">
        ///     <para xml:lang="en">Whether content starts hidden.</para>
        ///     <para xml:lang="zh-CN">内容是否初始隐藏。</para>
        /// </param>
        /// <param name="contentControls">
        ///     <para xml:lang="en">
        ///         Distinct, valid, parentless controls. The array is copied; keep the controls parentless until
        ///         this section enters the tree and adopts them.
        ///     </para>
        ///     <para xml:lang="zh-CN">互不重复、有效且无父节点的控件。会复制数组；折叠栏进入场景树并接管这些控件前，请保持它们无父节点。</para>
        /// </param>
        /// <param name="headerActions">
        ///     <para xml:lang="en">Optional parentless action button adopted by the section on entering the tree.</para>
        ///     <para xml:lang="zh-CN">可选的无父节点操作按钮；折叠栏进入场景树时会将其接管。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsCollapsibleSection(string title, string? sectionId, string? description, bool startCollapsed,
            Control[] contentControls, ModSettingsActionsButton? headerActions = null)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(contentControls);
            var controls = new HashSet<Control>(ReferenceEqualityComparer.Instance);
            foreach (var control in contentControls)
            {
                ArgumentNullException.ThrowIfNull(control);
                if (!IsInstanceValid(control) || control.GetParent() != null || !controls.Add(control))
                    throw new ArgumentException("Content controls must be valid, distinct, and parentless.",
                        nameof(contentControls));
            }

            if (headerActions != null && (!IsInstanceValid(headerActions) || headerActions.GetParent() != null ||
                                          controls.Contains(headerActions)))
                throw new ArgumentException("Header actions must be valid, parentless, and separate from content.",
                    nameof(headerActions));
            _title = title;
            _sectionId = sectionId;
            _description = description;
            IsCollapsed = startCollapsed;
            _contentControls = [.. contentControls];
            _headerActions = headerActions;
            MouseFilter = MouseFilterEnum.Ignore;
            ClipContents = true;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _separation = RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.cardSeparation", 8);
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public ModSettingsCollapsibleSection()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the borrowed content container. Add content here; do not free or reparent the container.
        ///         Added controls become owned children.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取借用的内容容器。可向其中添加内容；不得释放或重新挂载此容器。添加的控件成为其自有子节点。</para>
        /// </summary>
        public Control ContentHost => _content ??= CreateContentHost();

        /// <summary>
        ///     <para xml:lang="en">Gets whether content is collapsed, including before the section enters the scene tree.</para>
        ///     <para xml:lang="zh-CN">获取内容是否已折叠；在折叠栏进入场景树前同样有效。</para>
        /// </summary>
        public bool IsCollapsed { get; private set; }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (!string.IsNullOrWhiteSpace(_sectionId))
                Name = $"Section_{_sectionId}";

            if (_title != null)
                _toggle = new(_title, _description, ToggleCollapsed)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };

            if (_toggle != null || _headerActions != null)
            {
                if (_toggle != null && _headerActions == null)
                {
                    _header = _toggle;
                    AddChild(_toggle);
                }
                else
                {
                    var headerRow = new HBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                        Alignment = BoxContainer.AlignmentMode.Center,
                    };
                    headerRow.AddThemeConstantOverride("separation",
                        RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.headerRowSeparation",
                            10));
                    if (_toggle != null)
                    {
                        _toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                        headerRow.AddChild(_toggle);
                    }
                    else
                    {
                        headerRow.AddChild(new Control
                        {
                            SizeFlagsHorizontal = SizeFlags.ExpandFill,
                            MouseFilter = MouseFilterEnum.Ignore,
                        });
                    }

                    if (_headerActions != null)
                    {
                        _headerActions.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                        headerRow.AddChild(_headerActions);
                    }

                    _header = headerRow;
                    AddChild(headerRow);
                }
            }

            _content ??= CreateContentHost();
            if (_contentControls != null)
                foreach (var control in _contentControls)
                {
                    if (control.GetParent() == _content)
                        continue;
                    if (control.GetParent() != null)
                        throw new InvalidOperationException(
                            "Section content was adopted by another parent before initialization.");
                    _content.AddChild(control);
                }

            AddChild(_content);

            ApplyCollapsedState();
            ApplyContentEnabledState();
            RequestLayout();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            switch (what)
            {
                case (int)NotificationResized:
                    LayoutChildren();
                    break;
                case (int)NotificationThemeChanged:
                    _surfaceStyle = RitsuShellChromeStyles.CreateSurfaceStyle();
                    _separation = RitsuShellThemeLayoutResolver.ResolveInt(
                        "components.collapsible.layout.cardSeparation", 8);
                    RequestLayout();
                    break;
            }
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            var margins = GetSurfaceMargins();
            var contentWidth = ResolveInnerWidth(margins);
            PrepareChildWidth(_header, contentWidth, false);
            PrepareChildWidth(_content, contentWidth, false);

            var minWidth = 0f;
            var minHeight = 0f;
            var headerMin = GetVisibleMinSize(_header);
            var contentMin = GetVisibleMinSize(_content);
            if (headerMin.Y > 0f)
            {
                minWidth = Math.Max(minWidth, headerMin.X);
                minHeight += headerMin.Y;
            }

            if (!(contentMin.Y > 0f))
                return new(minWidth + margins.Left + margins.Right, minHeight + margins.Top + margins.Bottom);
            if (minHeight > 0f)
                minHeight += _separation;
            minWidth = Math.Max(minWidth, contentMin.X);
            minHeight += contentMin.Y;

            return new(minWidth + margins.Left + margins.Right, minHeight + margins.Top + margins.Bottom);
        }

        /// <inheritdoc />
        public override void _Draw()
        {
            DrawStyleBox(_surfaceStyle, new(Vector2.Zero, Size));
        }

        private void ToggleCollapsed()
        {
            SetCollapsed(!IsCollapsed);
            if (!IsCollapsed)
                Callable.From(EnsureExpandedSectionVisible).CallDeferred();
        }

        /// <summary>
        ///     <para xml:lang="en">Expands the section and builds lazy content once, if registered.</para>
        ///     <para xml:lang="zh-CN">展开折叠栏，并在已注册时创建一次延迟内容。</para>
        /// </summary>
        public void Expand()
        {
            SetCollapsed(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the collapsed state. Expanding attempts a registered lazy builder once; callback
        ///         exceptions propagate.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置折叠状态。展开时会尝试运行一次已注册的延迟创建器；回调异常会继续传播。</para>
        /// </summary>
        /// <param name="collapsed">
        ///     <para xml:lang="en">Whether to hide the content.</para>
        ///     <para xml:lang="zh-CN">是否隐藏内容。</para>
        /// </param>
        public void SetCollapsed(bool collapsed)
        {
            IsCollapsed = collapsed;
            if (!collapsed)
                EnsureLazyContentBuilt();
            ApplyCollapsedState();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs a registered lazy builder once without changing the collapsed state. Callback exceptions
        ///         propagate; a failed builder is not retried.
        ///     </para>
        ///     <para xml:lang="zh-CN">在不改变折叠状态的情况下运行一次已注册的延迟创建器。回调异常会继续传播；失败的创建器不会重试。</para>
        /// </summary>
        public void EnsureContentBuilt()
        {
            EnsureLazyContentBuilt();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers one content builder. It runs immediately for an expanded section, or on expansion or
        ///         explicit content preparation. Duplicate registration throws InvalidOperationException; the builder is attempted
        ///         once even if it throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册一个内容创建器。折叠栏已展开时立即运行，否则在展开或显式准备内容时运行。重复注册抛出 InvalidOperationException；即使抛出异常也只尝试一次。</para>
        /// </summary>
        /// <param name="builder">
        ///     <para xml:lang="en">Non-null callback that appends owned controls to ContentHost. Called on the main thread.</para>
        ///     <para xml:lang="zh-CN">向 ContentHost 添加自有控件的非 null 回调。在主线程调用。</para>
        /// </param>
        public void SetLazyContentBuilder(Action builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (_lazyContentBuilder != null || _lazyContentBuilt)
                throw new InvalidOperationException("A lazy content builder has already been registered or invoked.");
            _lazyContentBuilder = builder;
            if (!IsCollapsed)
                EnsureLazyContentBuilt();
        }

        private void EnsureLazyContentBuilt()
        {
            if (_lazyContentBuilt || _lazyContentBuilder == null)
                return;

            var builder = _lazyContentBuilder;
            _lazyContentBuilt = true;
            _lazyContentBuilder = null;
            builder();
            ApplyContentEnabledState();
        }

        private void ApplyCollapsedState()
        {
            if (_content != null)
            {
                _content.Visible = !IsCollapsed;
                RitsuVerticalStack.RequestAncestorLayouts(_content);
            }

            _toggle?.SetSelected(!IsCollapsed);
            RequestLayout();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the visual treatment of the section header. It does not disable, free, or alter child
        ///         controls.
        ///     </para>
        ///     <para xml:lang="zh-CN">更新折叠栏标题的视觉状态，不禁用、释放或修改子控件。</para>
        /// </summary>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether to use the enabled visual state.</para>
        ///     <para xml:lang="zh-CN">是否使用启用时的视觉状态。</para>
        /// </param>
        public void SetContentEnabled(bool enabled)
        {
            _contentEnabled = enabled;
            ApplyContentEnabledState();
        }

        private void ApplyContentEnabledState()
        {
            _toggle?.SetContentEnabled(_contentEnabled);
        }

        private static Control CreateContentHost()
        {
            return new RitsuVerticalStack(
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.contentSeparation", 8))
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }

        private void RequestLayout(bool defer = true)
        {
            UpdateMinimumSize();
            LayoutChildren();
            QueueRedraw();
            RequestAncestorAndScrollLayouts();
            if (defer)
                ScheduleDeferredLayoutRefresh();
        }

        private void ScheduleDeferredLayoutRefresh()
        {
            if (_layoutRefreshDeferred || !IsInsideTree())
                return;

            _layoutRefreshDeferred = true;
            Callable.From(DeferredRequestLayout).CallDeferred();
        }

        private void DeferredRequestLayout()
        {
            _layoutRefreshDeferred = false;
            if (!IsInsideTree())
                return;

            RequestLayout(false);
        }

        private void RequestAncestorAndScrollLayouts()
        {
            RitsuVerticalStack.RequestAncestorLayouts(this);
            for (var current = GetParent() as Control; current != null; current = current.GetParent() as Control)
            {
                current.UpdateMinimumSize();
                if (current is Container container && container.IsInsideTree())
                    container.QueueSort();

                if (current is ScrollContainer scroll)
                    scroll.QueueSort();
            }
        }

        private void LayoutChildren()
        {
            if (!IsInsideTree())
                return;

            var margins = GetSurfaceMargins();
            var x = (float)margins.Left;
            var y = (float)margins.Top;
            var width = ResolveInnerWidth(margins);
            PrepareChildWidth(_header, width, true);
            PrepareChildWidth(_content, width, true);

            if (_header is { Visible: true })
            {
                var headerMin = _header.GetCombinedMinimumSize();
                _header.Position = new(x, y);
                _header.Size = new(width, headerMin.Y);
                y += headerMin.Y;
            }

            if (_content is not { Visible: true })
                return;

            if (y > margins.Top)
                y += _separation;
            var contentMin = _content.GetCombinedMinimumSize();
            _content.Position = new(x, y);
            _content.Size = new(width, contentMin.Y);
        }

        private float ResolveInnerWidth(BoxEdges margins)
        {
            return Math.Max(0f, Size.X - margins.Left - margins.Right);
        }

        private static void PrepareChildWidth(Control? control, float width, bool requestLayout)
        {
            if (control is not { Visible: true } || width <= 1f)
                return;

            if (control is RitsuVerticalStack stack)
            {
                if (requestLayout)
                    stack.SetLayoutWidth(width);
                else
                    stack.SetLayoutWidthFromParent(width);
            }

            if (Math.Abs(control.Size.X - width) >= 0.5f)
                control.Size = new(width, control.Size.Y);
        }

        private BoxEdges GetSurfaceMargins()
        {
            return new(
                Mathf.RoundToInt(_surfaceStyle.ContentMarginLeft),
                Mathf.RoundToInt(_surfaceStyle.ContentMarginTop),
                Mathf.RoundToInt(_surfaceStyle.ContentMarginRight),
                Mathf.RoundToInt(_surfaceStyle.ContentMarginBottom));
        }

        private static Vector2 GetVisibleMinSize(Control? control)
        {
            return control is { Visible: true } ? control.GetCombinedMinimumSize() : Vector2.Zero;
        }

        private void EnsureExpandedSectionVisible()
        {
            if (!IsVisibleInTree())
                return;

            var scroll = FindAncestorScrollContainer(this);
            scroll?.EnsureControlVisible(this);
        }

        private static ScrollContainer? FindAncestorScrollContainer(Node node)
        {
            for (var current = node.GetParent(); current != null; current = current.GetParent())
                if (current is ScrollContainer scroll)
                    return scroll;

            return null;
        }
    }
}
