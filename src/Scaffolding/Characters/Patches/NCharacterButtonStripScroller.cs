using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal sealed partial class NCharacterButtonStripScroller : Control
    {
        private const float ButtonWidth = 100f;
        private const float DefaultButtonSeparation = 16f;
        private const float ArrowContainerWidth = 80f;
        private const float ArrowGap = 24f;
        private const float ArrowIconSize = 64f;
        private const float ArrowButtonHeight = 64f;
        private const float ArrowIconHeight = 117f;
        private const float ArrowStepRatio = 0.72f;
        private const float SnapThreshold = 0.5f;
        private const string HsvShaderPath = "res://shaders/hsv.gdshader";

        private const string LeftArrowTexturePath =
            "res://images/atlases/ui_atlas.sprites/settings_tiny_left_arrow.tres";

        private const string RightArrowTexturePath =
            "res://images/atlases/ui_atlas.sprites/settings_tiny_right_arrow.tres";

        private float _contentHeight;
        private Control? _contents;
        private float _contentWidth;
        private Callable? _focusChangedCallable;
        private bool _isDragging;
        private NGoldArrowButton? _leftArrow;

        private CharacterButtonStripScrollOptions _options;
        private Control? _retainedForeignScroller;
        private NGoldArrowButton? _rightArrow;
        private float _targetX;

        private float ScrollLimit => Mathf.Min(0f, _options.ViewportWidth - _contentWidth);

        private bool CanScroll => _contentWidth > _options.ViewportWidth + SnapThreshold;

        private float TargetX
        {
            get => _targetX;
            set => _targetX = Mathf.Clamp(value, ScrollLimit, 0f);
        }

        public static bool Install(Control? root, CharacterButtonStripScrollOptions options)
        {
            if (root == null)
                return false;

            var contents = FindButtonContainer(root);
            if (contents == null)
                return false;

            var visibleButtonCount = CountVisibleButtons(contents);
            var replacesForeignScroller = IsForeignScroller(root);
            if (root is not NCharacterButtonStripScroller && !replacesForeignScroller &&
                visibleButtonCount <= options.VisibleButtons)
                return false;

            var scroller = root as NCharacterButtonStripScroller ??
                           ReplaceRoot(root, contents, replacesForeignScroller);
            if (scroller == null)
                return false;

            scroller.Configure(contents, options);
            return true;
        }

        public static bool InstallNested(Control? host, string scrollerName, CharacterButtonStripScrollOptions options)
        {
            if (host == null)
                return false;

            var existingScroller = host.GetNodeOrNull<Control>(scrollerName);
            if (existingScroller != null)
                return Install(existingScroller, options);

            var contents = host.GetNodeOrNull<Control>("ButtonContainer");
            if (contents == null || CountVisibleButtons(contents) <= options.VisibleButtons)
                return false;

            var index = contents.GetIndex();
            host.RemoveChild(contents);

            var scroller = new NCharacterButtonStripScroller { Name = scrollerName };
            ApplyNestedViewportLayout(scroller, options);
            host.AddChild(scroller);
            scroller.Owner = host.Owner;
            host.MoveChild(scroller, index);
            scroller.AddChild(contents);
            scroller.Configure(contents, options);
            return true;
        }

        public override void _EnterTree()
        {
            base._EnterTree();

            var viewport = GetViewport();
            if (viewport == null || _focusChangedCallable != null)
                return;

            _focusChangedCallable = Callable.From<Control>(OnGuiFocusChanged);
            viewport.Connect(Viewport.SignalName.GuiFocusChanged, _focusChangedCallable.Value);
        }

        public override void _ExitTree()
        {
            var viewport = GetViewport();
            if (viewport != null && _focusChangedCallable is { } callable &&
                viewport.IsConnected(Viewport.SignalName.GuiFocusChanged, callable))
                viewport.Disconnect(Viewport.SignalName.GuiFocusChanged, callable);

            QueueSiblingArrowFree(_leftArrow);
            QueueSiblingArrowFree(_rightArrow);
            _leftArrow = null;
            _rightArrow = null;
            _focusChangedCallable = null;
            base._ExitTree();
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (!IsVisibleInTree() || _contents == null || !CanScroll)
                return;

            switch (inputEvent)
            {
                case InputEventMouseMotion motion when _isDragging:
                    TargetX += motion.Relative.X;
                    AcceptEvent();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
                    _isDragging = mouseButton.Pressed;
                    if (_isDragging)
                        AcceptEvent();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }
                    or InputEventPanGesture:
                    var drag = ScrollHelper.GetDragForScrollEvent(inputEvent);
                    if (Mathf.Abs(drag) <= 0f)
                        return;

                    TargetX += drag;
                    AcceptEvent();
                    break;
            }
        }

        public override void _Process(double delta)
        {
            if (!IsVisibleInTree() || _contents == null)
                return;

            RefreshLayout();
            UpdateScrollPosition(delta);
        }

        private void Configure(Control contents, CharacterButtonStripScrollOptions options)
        {
            _options = options;
            _contents = contents;
            MouseFilter = MouseFilterEnum.Stop;
            ClipContents = true;

            if (!options.PreserveRootLayout)
                ApplyViewportLayout();
            PrepareContents();
            EnsureArrows();
            RefreshLayout();
            SetProcess(true);
        }

        private static Control? FindButtonContainer(Control root)
        {
            return root is NCharacterButtonStripScroller scroller
                ? scroller._contents ?? root.GetNodeOrNull<Control>("ButtonContainer")
                : root.GetNodeOrNull<Control>("ButtonContainer");
        }

        private static int CountVisibleButtons(Control contents)
        {
            return contents.GetChildren().OfType<NCharacterSelectButton>().Count(static button => button.Visible);
        }

        private static bool IsForeignScroller(Control root)
        {
            return root.GetType().FullName == "BaseLib.BaseLibScenes.NHorizontalScrollContainer";
        }

        private static NCharacterButtonStripScroller? ReplaceRoot(
            Control oldRoot,
            Control contents,
            bool retainForeignScroller)
        {
            var parent = oldRoot.GetParent();
            if (parent == null)
                return null;

            var index = oldRoot.GetIndex();
            oldRoot.RemoveChild(contents);
            parent.RemoveChild(oldRoot);

            var scroller = new NCharacterButtonStripScroller();
            CopyLayout(oldRoot, scroller);
            parent.AddChild(scroller);
            scroller.Owner = oldRoot.Owner;
            parent.MoveChild(scroller, index);
            scroller.AddChild(contents);
            if (retainForeignScroller)
                scroller.RetainForeignScroller(oldRoot);
            else
                oldRoot.QueueFree();

            return scroller;
        }

        private void RetainForeignScroller(Control oldRoot)
        {
            // BaseLib registers per-button focus lambdas that capture its scroller and cannot be disconnected reliably.
            _retainedForeignScroller = oldRoot;
            oldRoot.Name = "RetainedForeignScroller";
            oldRoot.UniqueNameInOwner = false;
            oldRoot.Visible = false;
            oldRoot.MouseFilter = MouseFilterEnum.Ignore;
            oldRoot.ProcessMode = ProcessModeEnum.Disabled;
            oldRoot.SetProcess(false);
            oldRoot.SetProcessInput(false);
            oldRoot.SetProcessUnhandledInput(false);
            oldRoot.SetPhysicsProcess(false);
            AddChild(oldRoot);
        }

        private static void CopyLayout(Control source, Control target)
        {
            target.Name = source.Name;
            target.UniqueNameInOwner = source.UniqueNameInOwner;
            target.AnchorLeft = source.AnchorLeft;
            target.AnchorTop = source.AnchorTop;
            target.AnchorRight = source.AnchorRight;
            target.AnchorBottom = source.AnchorBottom;
            target.OffsetLeft = source.OffsetLeft;
            target.OffsetTop = source.OffsetTop;
            target.OffsetRight = source.OffsetRight;
            target.OffsetBottom = source.OffsetBottom;
            target.GrowHorizontal = source.GrowHorizontal;
            target.GrowVertical = source.GrowVertical;
            target.Scale = source.Scale;
            target.PivotOffset = source.PivotOffset;
            target.SizeFlagsHorizontal = source.SizeFlagsHorizontal;
            target.SizeFlagsVertical = source.SizeFlagsVertical;
        }

        private void ApplyViewportLayout()
        {
            AnchorLeft = 0.5f;
            AnchorRight = 0.5f;
            AnchorTop = 1f;
            AnchorBottom = 1f;
            OffsetLeft = -_options.ViewportWidth / 2f;
            OffsetRight = _options.ViewportWidth / 2f;
            OffsetTop = _options.OffsetTop;
            OffsetBottom = _options.OffsetBottom;
            CustomMinimumSize = new(_options.ViewportWidth, _options.ViewportHeight);
            Size = CustomMinimumSize;
            ClipContents = true;
        }

        private static void ApplyNestedViewportLayout(Control target, CharacterButtonStripScrollOptions options)
        {
            target.AnchorLeft = 0.5f;
            target.AnchorRight = 0.5f;
            target.AnchorTop = 0.5f;
            target.AnchorBottom = 0.5f;
            target.OffsetLeft = -options.ViewportWidth / 2f;
            target.OffsetRight = options.ViewportWidth / 2f;
            target.OffsetTop = options.OffsetTop;
            target.OffsetBottom = options.OffsetBottom;
            target.GrowHorizontal = GrowDirection.Both;
            target.GrowVertical = GrowDirection.Both;
            target.ClipContents = true;
        }

        private void PrepareContents()
        {
            if (_contents == null)
                return;

            _contents.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize);
            _contents.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            foreach (var button in _contents.GetChildren().OfType<NCharacterSelectButton>())
                button.MouseFilter = MouseFilterEnum.Pass;
        }

        private void RefreshLayout()
        {
            if (_contents == null)
                return;

            _contentWidth = MeasureContentWidth(_contents);
            _contentHeight = MeasureContentHeight(_contents);
            _contents.CustomMinimumSize = new(_contentWidth, _contentHeight);
            _contents.Size = _contents.CustomMinimumSize;
            TargetX = _targetX;
            RefreshArrows();
        }

        private static float MeasureContentWidth(Control contents)
        {
            var buttons = contents.GetChildren().OfType<NCharacterSelectButton>()
                .Where(static button => button.Visible)
                .Cast<Control>()
                .ToArray();
            if (buttons.Length == 0)
                return Mathf.Max(contents.Size.X, contents.CustomMinimumSize.X);

            var separation = ResolveSeparation(contents);
            var width = buttons.Sum(static button => Mathf.Max(
                Mathf.Max(button.Size.X, button.CustomMinimumSize.X),
                ButtonWidth));

            return width + separation * (buttons.Length - 1);
        }

        private static float MeasureContentHeight(Control contents)
        {
            var measured = contents.GetChildren().OfType<NCharacterSelectButton>()
                .Where(static button => button.Visible)
                .Select(static button => Mathf.Max(button.Size.Y, button.CustomMinimumSize.Y))
                .DefaultIfEmpty(contents.Size.Y)
                .Max();

            return Mathf.Max(measured, contents.CustomMinimumSize.Y);
        }

        private static float ResolveSeparation(Control contents)
        {
            var separation = contents.GetThemeConstant("separation");
            return separation > 0 ? separation : DefaultButtonSeparation;
        }

        private void UpdateScrollPosition(double delta)
        {
            if (_contents == null)
                return;

            var centeredX = Mathf.Max(0f, (_options.ViewportWidth - _contentWidth) / 2f);
            var targetPosition = centeredX + _targetX;
            var current = _contents.Position.X;
            var next = Mathf.Lerp(current, targetPosition, (float)delta * ScrollHelper.dragLerpSpeed);
            if (Mathf.Abs(next - targetPosition) < SnapThreshold)
                next = targetPosition;

            var centeredY = Mathf.Max(0f, (_options.ViewportHeight - _contentHeight) / 2f);
            _contents.Position = new(next, centeredY);
            RefreshArrows();
        }

        private void OnGuiFocusChanged(Control focusedControl)
        {
            if (_contents == null || focusedControl == null || !IsVisibleInTree() || !CanScroll ||
                !_contents.IsAncestorOf(focusedControl))
                return;

            var item = FindDirectContentChild(focusedControl);
            if (item == null)
                return;

            var left = item.Position.X;
            var right = left + item.Size.X;
            if (left + _targetX < 0f)
                TargetX = -left;
            else if (right + _targetX > Size.X)
                TargetX = Size.X - right;
        }

        private void ScrollByArrow(int direction)
        {
            if (!CanScroll)
                return;

            TargetX -= direction * Size.X * ArrowStepRatio;
        }

        private void EnsureArrows()
        {
            _leftArrow ??= CreateArrow(-1);
            _rightArrow ??= CreateArrow(1);

            RefreshArrows();
        }

        private NGoldArrowButton CreateArrow(int direction)
        {
            var arrow = new NGoldArrowButton
            {
                Name = direction < 0 ? "RitsuScrollLeft" : "RitsuScrollRight",
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
            };
            arrow.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ =>
                ScrollByArrow(direction)));

            var icon = new TextureRect
            {
                Name = "TextureRect",
                Material = CreateArrowMaterial(),
                CustomMinimumSize = Vector2.One * ArrowIconSize,
                Size = Vector2.One * ArrowIconSize,
                Texture = ResourceLoader.Load<Texture2D>(direction < 0 ? LeftArrowTexturePath : RightArrowTexturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(direction < 0 ? 36f : 28f, 58f),
            };
            arrow.AddChild(icon);
            GetParent()?.AddChild(arrow);
            return arrow;
        }

        private static ShaderMaterial CreateArrowMaterial()
        {
            return new()
            {
                ResourceLocalToScene = true,
                Shader = ResourceLoader.Load<Shader>(HsvShaderPath),
            };
        }

        private void RefreshArrows()
        {
            if (_leftArrow == null || _rightArrow == null)
                return;

            if (!CanScroll)
            {
                _leftArrow.Visible = false;
                _rightArrow.Visible = false;
                return;
            }

            LayoutArrow(_leftArrow, OffsetLeft - ArrowGap - ArrowContainerWidth);
            var canScrollLeft = _targetX < -SnapThreshold;
            _leftArrow.Visible = canScrollLeft;
            _leftArrow.SetEnabled(canScrollLeft);

            LayoutArrow(_rightArrow, OffsetRight + ArrowGap);
            var canScrollRight = _targetX > ScrollLimit + SnapThreshold;
            _rightArrow.Visible = canScrollRight;
            _rightArrow.SetEnabled(canScrollRight);
        }

        private void LayoutArrow(NGoldArrowButton arrow, float leftOffset)
        {
            arrow.AnchorLeft = AnchorLeft;
            arrow.AnchorRight = AnchorLeft;
            arrow.AnchorTop = AnchorTop;
            arrow.AnchorBottom = AnchorTop;
            arrow.OffsetLeft = leftOffset;
            arrow.OffsetRight = leftOffset + ArrowContainerWidth;
            arrow.OffsetTop = OffsetTop + (_options.ViewportHeight - ArrowButtonHeight) / 2f;
            arrow.OffsetBottom = arrow.OffsetTop + ArrowButtonHeight;
            arrow.Size = new(ArrowContainerWidth, ArrowButtonHeight);
            arrow.CustomMinimumSize = arrow.Size;
            if (arrow.GetNodeOrNull<TextureRect>("TextureRect") is not { } icon) return;
            icon.AnchorLeft = 0.5f;
            icon.AnchorTop = 0.5f;
            icon.AnchorRight = 0.5f;
            icon.AnchorBottom = 0.5f;
            icon.OffsetLeft = -ArrowIconSize / 2f;
            icon.OffsetRight = ArrowIconSize / 2f;
            icon.OffsetTop = -50.5f;
            icon.OffsetBottom = 66.5f;
            icon.Size = new(ArrowIconSize, ArrowIconHeight);
        }

        private void QueueSiblingArrowFree(NGoldArrowButton? arrow)
        {
            if (arrow == null || !IsInstanceValid(arrow) || arrow.GetParent() == this)
                return;

            arrow.QueueFree();
        }

        private Control? FindDirectContentChild(Control focusedControl)
        {
            if (_contents == null)
                return null;

            Node current = focusedControl;
            while (current.GetParent() != null && current.GetParent() != _contents)
                current = current.GetParent();

            return current.GetParent() == _contents ? current as Control : null;
        }
    }

    internal readonly record struct CharacterButtonStripScrollOptions(
        int VisibleButtons,
        float ViewportWidth,
        float ViewportHeight,
        float OffsetTop,
        float OffsetBottom,
        bool PreserveRootLayout = false);
}
