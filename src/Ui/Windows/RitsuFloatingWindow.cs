using Godot;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Windows
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a themed content window that can remain fixed or allow dragging and eight-direction resizing.
    ///         It also supports replacing content and saving or restoring window geometry.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供带主题样式的内容窗口，可保持固定，也可启用拖动和八方向缩放；同时支持替换内容以及保存、恢复窗口位置和尺寸。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Create and mutate this Godot control only on the main thread. Content returned by
    ///         <see cref="TakeContent" /> remains owned by the caller.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         仅可在 Godot 主线程创建及修改此控件。通过 <see cref="TakeContent" /> 取回的内容仍由调用方持有。
    ///     </para>
    /// </remarks>
    public sealed partial class RitsuFloatingWindow : PanelContainer
    {
        private const float HeaderHeight = 48f;
        private const float EdgeHandleThickness = 8f;
        private const float CornerHandleSize = 16f;
        private readonly List<Control> _resizeHandles = [];
        private Control? _content;
        private MarginContainer? _contentHost;
        private Vector2 _dragOffset;
        private bool _dragging;
        private HBoxContainer? _header;
        private bool _interactionLocked;
        private bool _layoutInitialized;
        private ResizeEdge _resizeEdge;
        private Control? _resizeLayer;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartPosition;
        private Vector2 _resizeStartSize;

        /// <summary>
        ///     <para xml:lang="en">Creates a window with default fixed-window options.</para>
        ///     <para xml:lang="zh-CN">使用默认的固定窗口选项创建窗口。</para>
        /// </summary>
        public RitsuFloatingWindow()
        {
            MouseFilter = MouseFilterEnum.Stop;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a window using the supplied immutable configuration.</para>
        ///     <para xml:lang="zh-CN">使用给定的不可变配置创建窗口。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The interaction and size configuration to validate and apply.</para>
        ///     <para xml:lang="zh-CN">要验证并应用的交互与尺寸配置。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="options" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="options" /> 为 null 时引发。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when an option value is invalid or exceeds a supported limit.</para>
        ///     <para xml:lang="zh-CN">当选项值无效或超过支持的限制时引发。</para>
        /// </exception>
        public RitsuFloatingWindow(RitsuFloatingWindowOptions options) : this()
        {
            Configure(options);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the validated configuration currently used by the window.</para>
        ///     <para xml:lang="zh-CN">获取窗口当前使用的已验证配置。</para>
        /// </summary>
        public RitsuFloatingWindowOptions Options { get; private set; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether movement and resizing are temporarily locked. This does not alter the window's
        ///         configured capabilities.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置是否临时锁定移动与缩放；此属性不会改变窗口已配置的能力。
        ///     </para>
        /// </summary>
        public bool InteractionLocked
        {
            get => _interactionLocked;
            set
            {
                _interactionLocked = value;
                RefreshInteractionState();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised synchronously after <see cref="Close" /> hides the window. Recoverable subscriber failures
        ///         are isolated and logged.
        ///     </para>
        ///     <para xml:lang="zh-CN"><see cref="Close" /> 隐藏窗口后同步触发；可恢复的订阅者异常会被隔离并记录。</para>
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised synchronously after a pointer-driven move or resize completes. Programmatic geometry changes
        ///         do not raise this event. Recoverable subscriber failures are isolated and logged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         指针驱动的移动或缩放完成后同步触发；以代码修改几何范围不会触发此事件。可恢复的订阅者异常会被隔离并记录。
        ///     </para>
        /// </summary>
        public event EventHandler? GeometryChanged;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the window before it enters the scene tree. Reconfiguration after <see cref="_Ready" />
        ///         is rejected so existing content and pointer state cannot be invalidated implicitly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在窗口进入场景树前配置窗口。<see cref="_Ready" /> 之后禁止重新配置，以免隐式破坏已有内容及
        ///         指针交互状态。
        ///     </para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The interaction and size configuration to validate and apply.</para>
        ///     <para xml:lang="zh-CN">要验证并应用的交互与尺寸配置。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="options" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="options" /> 为 null 时引发。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Thrown when the window has already entered the scene tree.</para>
        ///     <para xml:lang="zh-CN">当窗口已进入场景树时引发。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when an option value is invalid or exceeds a supported limit.</para>
        ///     <para xml:lang="zh-CN">当选项值无效或超过支持的限制时引发。</para>
        /// </exception>
        public void Configure(RitsuFloatingWindowOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (IsInsideTree() || _layoutInitialized)
                throw new InvalidOperationException(
                    "A floating window can only be configured before it enters the scene tree.");
            options.Validate();
            Options = options;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces the window content and returns the previous content. The new content must be a valid
        ///         unattached control. Replaced content is detached but not freed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         替换窗口内容并返回原内容。新内容必须是有效且尚未挂载的控件；被替换的内容会被分离，
        ///         但不会被释放。
        ///     </para>
        /// </summary>
        /// <param name="content">
        ///     <para xml:lang="en">The unattached control to display.</para>
        ///     <para xml:lang="zh-CN">要显示的未挂载控件。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="content" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="content" /> 为 null 时引发。</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     <para xml:lang="en">Thrown when <paramref name="content" /> is no longer a valid Godot object.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="content" /> 不再是有效的 Godot 对象时引发。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Thrown when <paramref name="content" /> already has a parent.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="content" /> 已有父节点时引发。</para>
        /// </exception>
        /// <returns>
        ///     <para xml:lang="en">The detached previous content, or null when the window was empty.</para>
        ///     <para xml:lang="zh-CN">已分离的原内容；窗口原本为空时返回 null。</para>
        /// </returns>
        public Control? SetContent(Control content)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (!IsInstanceValid(content))
                throw new ObjectDisposedException(nameof(content));
            if (content.GetParent() != null)
                throw new InvalidOperationException("Floating window content must not already have a parent.");

            var previousContent = TakeContent();
            _content = content;
            _contentHost?.AddChild(content);
            return previousContent;
        }

        /// <summary>
        ///     <para xml:lang="en">Detaches and returns the current content without freeing it.</para>
        ///     <para xml:lang="zh-CN">分离并返回当前内容，但不释放该内容。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The detached content, or null when the window is empty.</para>
        ///     <para xml:lang="zh-CN">已分离的内容；窗口为空时返回 null。</para>
        /// </returns>
        public Control? TakeContent()
        {
            var content = _content;
            _content = null;
            if (content == null || !IsInstanceValid(content))
                return null;
            if (_contentHost != null && content.GetParent() == _contentHost)
                _contentHost.RemoveChild(content);
            return content;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a snapshot of the current unscaled geometry.</para>
        ///     <para xml:lang="zh-CN">获取当前未经缩放的几何快照。</para>
        /// </summary>
        public RitsuFloatingWindowGeometry CaptureGeometry()
        {
            return new(Position, Size);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a validated geometry snapshot, clamps its size to the configured limits, and constrains its
        ///         position when <see cref="RitsuFloatingWindowOptions.ConstrainToViewport" /> is enabled. The window
        ///         must be inside the scene tree so a viewport is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用经过验证的几何快照，将尺寸限制在配置范围内，并在启用
        ///         <see cref="RitsuFloatingWindowOptions.ConstrainToViewport" /> 时约束窗口位置。窗口必须已进入场景树，
        ///         以便取得视口。
        ///     </para>
        /// </summary>
        /// <param name="geometry">
        ///     <para xml:lang="en">The finite position and positive size to apply.</para>
        ///     <para xml:lang="zh-CN">要应用的有限位置及正数尺寸。</para>
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">Thrown when the position or size is not finite, or the size is not positive.</para>
        ///     <para xml:lang="zh-CN">当位置或尺寸不是有限值，或尺寸不是正数时引发。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">Thrown when the window is outside the scene tree.</para>
        ///     <para xml:lang="zh-CN">当窗口尚未进入场景树时引发。</para>
        /// </exception>
        public void ApplyGeometry(RitsuFloatingWindowGeometry geometry)
        {
            if (!geometry.Position.IsFinite() || !geometry.Size.IsFinite() ||
                geometry.Size.X <= 0f || geometry.Size.Y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(geometry),
                    "Window geometry must be finite and have a positive size.");
            if (!IsInsideTree())
                throw new InvalidOperationException(
                    "Window geometry can only be applied while the window is inside the scene tree.");
            Size = ClampSize(geometry.Size);
            Position = ClampPosition(geometry.Position, Size);
        }

        /// <summary>
        ///     <para xml:lang="en">Hides the window and raises <see cref="Closed" />.</para>
        ///     <para xml:lang="zh-CN">隐藏窗口并触发 <see cref="Closed" />。</para>
        /// </summary>
        public void Close()
        {
            if (!Visible)
                return;
            FinishPointerInteraction(false);
            Hide();
            InvokeHandlers(Closed, nameof(Closed));
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            Options.Validate();
            var firstReady = !_layoutInitialized;
            if (firstReady)
                BuildLayout();
            Size = ClampSize(firstReady ? Options.InitialSize : Size);
            GetViewport().SizeChanged += OnViewportSizeChanged;
            SetProcessInput(true);
            Callable.From(() => InitializeGeometry(firstReady)).CallDeferred();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            var viewport = GetViewport();
            if (viewport != null)
                viewport.SizeChanged -= OnViewportSizeChanged;
            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            if (!_dragging && _resizeEdge == ResizeEdge.None)
                return;
            switch (@event)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                    FinishPointerInteraction();
                    break;
                case InputEventMouseMotion motion when _dragging:
                    Position = ClampPosition(ToParentCoordinates(motion.GlobalPosition) - _dragOffset, Size);
                    break;
                case InputEventMouseMotion motion when _resizeEdge != ResizeEdge.None:
                    ResizeTo(motion.GlobalPosition);
                    break;
            }
        }

        private void BuildLayout()
        {
            _layoutInitialized = true;
            AddThemeStyleboxOverride("panel", RitsuShellPanelStyles.CreateFramedSurface(
                RitsuShellTheme.Current.Surface.Content, RitsuShellTheme.Current.Metric.Radius.Default));

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation", 0);
            AddChild(root);

            _header = new()
            {
                CustomMinimumSize = new(0f, HeaderHeight),
                MouseFilter = MouseFilterEnum.Stop,
            };
            _header.AddThemeConstantOverride("separation", 10);
            _header.GuiInput += OnHeaderInput;
            root.AddChild(_header);

            var titleMargin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            titleMargin.AddThemeConstantOverride("margin_left", 16);
            titleMargin.AddThemeConstantOverride("margin_top", 8);
            titleMargin.AddThemeConstantOverride("margin_bottom", 8);
            _header.AddChild(titleMargin);
            var title = new Label
            {
                Text = Options.Title,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.OverlayTitle);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            titleMargin.AddChild(title);

            if (Options.Closable)
            {
                var close = new ModSettingsTextButton("×", ModSettingsButtonTone.Normal, Close)
                {
                    CustomMinimumSize = new(44f, 40f),
                };
                _header.AddChild(close);
            }

            _contentHost = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            _contentHost.AddThemeConstantOverride("margin_left", 14);
            _contentHost.AddThemeConstantOverride("margin_top", 10);
            _contentHost.AddThemeConstantOverride("margin_right", 14);
            _contentHost.AddThemeConstantOverride("margin_bottom", 14);
            root.AddChild(_contentHost);
            if (_content != null)
                _contentHost.AddChild(_content);

            _resizeLayer = new()
            {
                LayoutMode = 1,
                AnchorsPreset = (int)LayoutPreset.FullRect,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(_resizeLayer);
            AddResizeHandles();
            RefreshInteractionState();
        }

        private void AddResizeHandles()
        {
            AddResizeHandle(ResizeEdge.Top, 0f, 0f, 1f, 0f, EdgeHandleThickness, EdgeHandleThickness,
                CursorShape.Vsize);
            AddResizeHandle(ResizeEdge.Bottom, 0f, 1f, 1f, 1f, EdgeHandleThickness, EdgeHandleThickness,
                CursorShape.Vsize);
            AddResizeHandle(ResizeEdge.Left, 0f, 0f, 0f, 1f, EdgeHandleThickness, EdgeHandleThickness,
                CursorShape.Hsize);
            AddResizeHandle(ResizeEdge.Right, 1f, 0f, 1f, 1f, EdgeHandleThickness, EdgeHandleThickness,
                CursorShape.Hsize);
            AddResizeHandle(ResizeEdge.Top | ResizeEdge.Left, 0f, 0f, 0f, 0f, CornerHandleSize,
                CornerHandleSize, CursorShape.Fdiagsize);
            AddResizeHandle(ResizeEdge.Top | ResizeEdge.Right, 1f, 0f, 1f, 0f, CornerHandleSize,
                CornerHandleSize, CursorShape.Bdiagsize);
            AddResizeHandle(ResizeEdge.Bottom | ResizeEdge.Left, 0f, 1f, 0f, 1f, CornerHandleSize,
                CornerHandleSize, CursorShape.Bdiagsize);
            AddResizeHandle(ResizeEdge.Bottom | ResizeEdge.Right, 1f, 1f, 1f, 1f, CornerHandleSize,
                CornerHandleSize, CursorShape.Fdiagsize);
        }

        private void AddResizeHandle(ResizeEdge edge, float left, float top, float right, float bottom,
            float width, float height, CursorShape cursor)
        {
            var handle = new Control
            {
                LayoutMode = 0,
                AnchorLeft = left,
                AnchorTop = top,
                AnchorRight = right,
                AnchorBottom = bottom,
                MouseDefaultCursorShape = cursor,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            var isLeft = edge.HasFlag(ResizeEdge.Left);
            var isTop = edge.HasFlag(ResizeEdge.Top);
            handle.OffsetLeft = isLeft ? -width / 2f : edge.HasFlag(ResizeEdge.Right) ? -width / 2f : 0f;
            handle.OffsetRight = isLeft ? width / 2f : edge.HasFlag(ResizeEdge.Right) ? width / 2f : 0f;
            handle.OffsetTop = isTop ? -height / 2f : edge.HasFlag(ResizeEdge.Bottom) ? -height / 2f : 0f;
            handle.OffsetBottom = isTop ? height / 2f : edge.HasFlag(ResizeEdge.Bottom) ? height / 2f : 0f;
            handle.GuiInput += input => OnResizeInput(input, edge);
            _resizeLayer!.AddChild(handle);
            _resizeHandles.Add(handle);
        }

        private void RefreshInteractionState()
        {
            if (_header != null)
                _header.MouseDefaultCursorShape = Options.Movable && !_interactionLocked
                    ? CursorShape.Move
                    : CursorShape.Arrow;
            foreach (var handle in _resizeHandles)
                handle.MouseFilter = Options.Resizable && !_interactionLocked
                    ? MouseFilterEnum.Stop
                    : MouseFilterEnum.Ignore;
            if (_interactionLocked)
                FinishPointerInteraction(false);
        }

        private void OnHeaderInput(InputEvent input)
        {
            if (!Options.Movable || _interactionLocked)
                return;
            switch (input)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse:
                    _dragging = true;
                    _dragOffset = ToParentCoordinates(mouse.GlobalPosition) - Position;
                    MoveToFront();
                    AcceptEvent();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                    FinishPointerInteraction();
                    break;
                case InputEventMouseMotion motion when _dragging:
                    Position = ClampPosition(ToParentCoordinates(motion.GlobalPosition) - _dragOffset, Size);
                    break;
            }
        }

        private void OnResizeInput(InputEvent input, ResizeEdge edge)
        {
            if (!Options.Resizable || _interactionLocked)
                return;
            switch (input)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse:
                    _resizeEdge = edge;
                    _resizeStartMouse = ToParentCoordinates(mouse.GlobalPosition);
                    _resizeStartPosition = Position;
                    _resizeStartSize = Size;
                    MoveToFront();
                    AcceptEvent();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                    FinishPointerInteraction();
                    break;
                case InputEventMouseMotion motion when _resizeEdge != ResizeEdge.None:
                    ResizeTo(motion.GlobalPosition);
                    break;
            }
        }

        private void ResizeTo(Vector2 mousePosition)
        {
            var effectiveScale = GetEffectiveScale();
            var delta = (ToParentCoordinates(mousePosition) - _resizeStartMouse) / effectiveScale;
            var requestedSize = _resizeStartSize;
            var requestedPosition = _resizeStartPosition;
            if (_resizeEdge.HasFlag(ResizeEdge.Right))
                requestedSize.X += delta.X;
            if (_resizeEdge.HasFlag(ResizeEdge.Bottom))
                requestedSize.Y += delta.Y;
            if (_resizeEdge.HasFlag(ResizeEdge.Left))
            {
                requestedSize.X -= delta.X;
                requestedPosition.X += delta.X * effectiveScale.X;
            }

            if (_resizeEdge.HasFlag(ResizeEdge.Top))
            {
                requestedSize.Y -= delta.Y;
                requestedPosition.Y += delta.Y * effectiveScale.Y;
            }

            var clampedSize = ClampSize(requestedSize);
            if (_resizeEdge.HasFlag(ResizeEdge.Left))
                requestedPosition.X += (requestedSize.X - clampedSize.X) * effectiveScale.X;
            if (_resizeEdge.HasFlag(ResizeEdge.Top))
                requestedPosition.Y += (requestedSize.Y - clampedSize.Y) * effectiveScale.Y;
            Size = clampedSize;
            Position = ClampPosition(requestedPosition, clampedSize);
        }

        private Vector2 ClampSize(Vector2 requested)
        {
            var viewport = GetViewportRectInParentCoordinates().Size / GetEffectiveScale();
            var maximum = new Vector2(
                Options.MaximumSize.X > 0f ? Options.MaximumSize.X : viewport.X,
                Options.MaximumSize.Y > 0f ? Options.MaximumSize.Y : viewport.Y);
            if (Options.ConstrainToViewport)
                maximum = maximum.Min(viewport);
            maximum = maximum.Max(Options.MinimumSize);
            return requested.Clamp(Options.MinimumSize, maximum);
        }

        private Vector2 ClampPosition(Vector2 requested, Vector2 windowSize)
        {
            if (!Options.ConstrainToViewport)
                return requested;
            var viewport = GetViewportRectInParentCoordinates();
            var available = (viewport.Size - windowSize * GetEffectiveScale()).Max(Vector2.Zero);
            return requested.Clamp(viewport.Position, viewport.Position + available);
        }

        private void FinishPointerInteraction(bool notify = true)
        {
            var changed = _dragging || _resizeEdge != ResizeEdge.None;
            _dragging = false;
            _resizeEdge = ResizeEdge.None;
            if (changed && notify)
                InvokeHandlers(GeometryChanged, nameof(GeometryChanged));
        }

        private void InitializeGeometry(bool center)
        {
            if (!IsInsideTree())
                return;
            Size = ClampSize(Size);
            var viewport = GetViewportRectInParentCoordinates();
            Position = center && Options.StartCentered
                ? ClampPosition(viewport.Position + (viewport.Size - Size * GetEffectiveScale()) / 2f, Size)
                : ClampPosition(Position, Size);
        }

        private Vector2 ToParentCoordinates(Vector2 viewportPosition)
        {
            return GetParent() is CanvasItem parent
                ? parent.GetGlobalTransformWithCanvas().AffineInverse() * viewportPosition
                : viewportPosition;
        }

        private Rect2 GetViewportRectInParentCoordinates()
        {
            var viewport = GetViewportRect();
            if (GetParent() is not CanvasItem parent)
                return viewport;

            var inverse = parent.GetGlobalTransformWithCanvas().AffineInverse();
            var topLeft = inverse * viewport.Position;
            var topRight = inverse * new Vector2(viewport.End.X, viewport.Position.Y);
            var bottomLeft = inverse * new Vector2(viewport.Position.X, viewport.End.Y);
            var bottomRight = inverse * viewport.End;
            var minimum = topLeft.Min(topRight).Min(bottomLeft).Min(bottomRight);
            var maximum = topLeft.Max(topRight).Max(bottomLeft).Max(bottomRight);
            return new(minimum, maximum - minimum);
        }

        private Vector2 GetEffectiveScale()
        {
            return new(
                float.IsFinite(Scale.X) && !Mathf.IsZeroApprox(Scale.X) ? Mathf.Abs(Scale.X) : 1f,
                float.IsFinite(Scale.Y) && !Mathf.IsZeroApprox(Scale.Y) ? Mathf.Abs(Scale.Y) : 1f);
        }

        private void OnViewportSizeChanged()
        {
            if (!IsInsideTree() || _dragging || _resizeEdge != ResizeEdge.None)
                return;
            Size = ClampSize(Size);
            Position = ClampPosition(Position, Size);
        }

        private void InvokeHandlers(EventHandler? handlers, string eventName)
        {
            if (handlers == null)
                return;
            foreach (var handler in handlers.GetInvocationList().OfType<EventHandler>())
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn($"[FloatingWindow] {eventName} subscriber failed: {ex}");
                }
        }

        [Flags]
        private enum ResizeEdge
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8,
        }
    }
}
