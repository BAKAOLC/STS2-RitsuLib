using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace STS2RitsuLib.Settings;

/// <summary>
///     Custom scroll area (clipper + fade mask + game <c>ui/scrollbar</c>) without Harmony-patched scroll types.
/// </summary>
public sealed partial class ModSettingsScrollContainer : Control
{
    /// <summary>
    ///     When true, <see cref="ScrollTo" /> prints logical vs actual Y to the Godot output (debug chapter navigation).
    /// </summary>
    public static bool LogScrollNav;

    /// <summary>
    ///     Width reserved on the right for the scrollbar gutter.
    /// </summary>
    public const float ScrollbarGutterWidth = 60f;

    /// <summary>
    ///     Height of the bottom fade mask region in pixels.
    /// </summary>
    public const float BottomFade = 70f;

    /// <summary>
    ///     Height of the top fade mask region in pixels.
    /// </summary>
    public const float TopFade = 24f;

    private readonly bool _disableScrollingIfContentFits;

    private float _controllerScrollAmount = 400f;
    private Control? _content;
    private Control _clipper = null!;
    private TextureRect _fadeMask = null!;
    private Gradient _maskGradient = null!;
    private GradientTexture2D _maskGradientTexture = null!;
    private NScrollbar _scrollbar = null!;

    private bool _isDragging;
    private float _paddingBottom;
    private float _paddingTop;
    private bool _scrollbarPressed;
    private float _startDragPosY;
    private float _targetDragPosY;

    private bool _scrollToDeferredRetry;
    private readonly Callable _contentRectChangedCallable;

    /// <summary>
    ///     Creates a scroll container with inner top/bottom padding inside the clipper.
    /// </summary>
    public ModSettingsScrollContainer(float topPadding = 0f, float bottomPadding = 0f, bool disableScrollingIfContentFits = false)
    {
        _contentRectChangedCallable = Callable.From(OnContentItemRectChanged);
        _paddingTop = topPadding;
        _paddingBottom = bottomPadding;
        _disableScrollingIfContentFits = disableScrollingIfContentFits;

        AnchorRight = 1f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Stop;
        ClipChildren = CanvasItem.ClipChildrenMode.Only;

        BuildUi();
    }

    /// <summary>Game scrollbar instance (0–100).</summary>
    public NScrollbar Scrollbar => _scrollbar;

    private float ContentHeight => _content?.GetCombinedMinimumSize().Y ?? 0f;

    private float ScrollViewportSize => _clipper.Size.Y;

    private float ScrollLimitBottom =>
        _content == null
            ? 0f
            : Mathf.Min(0f, _clipper.Size.Y - _paddingTop - _paddingBottom - ContentHeight);

    /// <summary>
    ///     Subscribes resize handling and enables per-frame scroll interpolation.
    /// </summary>
    public override void _Ready()
    {
        Resized += OnContainerResized;
        SetProcess(true);
    }

    /// <summary>
    ///     Unhooks resize and content signals before the node leaves the tree.
    /// </summary>
    public override void _ExitTree()
    {
        Resized -= OnContainerResized;

        DetachContentSignals();
        base._ExitTree();
    }

    /// <summary>
    ///     Replaces scroll content; forces vertical shrink so height is not inflated by ExpandFill parents.
    /// </summary>
    public void AttachContent(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);

        DetachContentSignals();

        _content = content;
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.SizeFlagsVertical = SizeFlags.ShrinkBegin;

        _clipper.AddChild(content);
        content.Connect(CanvasItem.SignalName.ItemRectChanged, _contentRectChangedCallable);
        OnContainerResized();
        UpdateScrollLayout();
    }

    /// <summary>
    ///     Scrolls so <paramref name="target" /> aligns to the top padding of the clipper (animated unless skipped).
    /// </summary>
    public void ScrollTo(Control? target, bool skipAnimation = false)
    {
        if (_content == null || target == null)
            return;

        if (!_content.IsAncestorOf(target))
            return;

        _content.UpdateMinimumSize();

        if (target.Size.Y < 1e-4f && !_scrollToDeferredRetry)
        {
            _scrollToDeferredRetry = true;
            Callable.From(() =>
            {
                _scrollToDeferredRetry = false;
                ScrollTo(target, skipAnimation);
            }).CallDeferred();
            return;
        }

        var raw = -ComputeOffsetInContent(target);
        _targetDragPosY = Mathf.Clamp(raw, ScrollLimitBottom, 0f);

        if (LogScrollNav)
            GD.Print(
                $"[RitsuScroll] ScrollTo name={target.Name} raw={raw:F1} targetY={_paddingTop + _targetDragPosY:F1} " +
                $"posY={_content.Position.Y:F1} limit={ScrollLimitBottom:F1} skipAnim={skipAnimation}");

        if (!skipAnimation)
            return;

        _content.Position = _content.Position with { Y = _paddingTop + _targetDragPosY };
        if (ScrollLimitBottom < -1e-4f)
            _scrollbar.SetValueWithoutAnimation(_targetDragPosY / ScrollLimitBottom * 100.0);
        else
            _scrollbar.SetValueWithoutAnimation(0.0);
        UpdateScrollLayout();
    }

    /// <summary>
    ///     Scrolls the currently focused control into view if it is under this content.
    /// </summary>
    public void ScrollToFocusedControl(bool skipAnimation = false)
    {
        var vp = GetViewport();
        if (vp?.GuiGetFocusOwner() is not Control c || _content == null || !_content.IsAncestorOf(c))
            return;

        EnsureControlVisible(c);
        if (!skipAnimation)
            return;

        if (_content != null)
            _content.Position = _content.Position with { Y = _paddingTop + _targetDragPosY };
        if (ScrollLimitBottom < -1e-4f)
            _scrollbar.SetValueWithoutAnimation(_targetDragPosY / ScrollLimitBottom * 100.0);
        else
            _scrollbar.SetValueWithoutAnimation(0.0);
        UpdateScrollLayout();
    }

    /// <summary>
    ///     Moves the scroll range minimally so <paramref name="node" /> is inside the padded viewport.
    /// </summary>
    public void EnsureControlVisible(Control node)
    {
        if (_content == null || !IsInstanceValid(node) || !_content.IsAncestorOf(node))
            return;

        _content.UpdateMinimumSize();

        var off = ComputeOffsetInContent(node);
        var top = _content.Position.Y + off;
        var bottom = top + node.Size.Y;
        var vMin = _paddingTop;
        var vMax = _clipper.Size.Y - _paddingBottom;
        var dy = 0f;
        if (top < vMin)
            dy = vMin - top;
        else if (bottom > vMax)
            dy = vMax - bottom;

        if (Mathf.Abs(dy) > 0.5f)
            _targetDragPosY = Mathf.Clamp(_targetDragPosY + dy, ScrollLimitBottom, 0f);
    }

    /// <summary>
    ///     Snaps scroll to the top without animation.
    /// </summary>
    public void InstantlyScrollToTop()
    {
        if (_content == null)
            return;

        _targetDragPosY = 0f;
        _content.Position = _content.Position with { Y = _paddingTop };
        _scrollbar.SetValueWithoutAnimation(0.0);
        UpdateScrollLayout();
    }

    /// <summary>
    ///     Recomputes content sizing and scrollbar visibility after a child layout change inside the attached content.
    /// </summary>
    public void RefreshContentMetrics()
    {
        if (_content == null)
            return;

        _content.UpdateMinimumSize();
        SyncScrollbarAfterContentResize();
        UpdateScrollLayout();
    }

    /// <summary>
    ///     Routes mouse drag and wheel input to the custom scroll logic while visible.
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (IsVisibleInTree())
        {
            ProcessMouseEvent(@event);
            ProcessScrollEvent(@event);
        }
    }

    /// <summary>
    ///     Routes controller scrolling input when no GUI control currently owns focus.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (IsVisibleInTree())
        {
            var vp = GetViewport();
            if (vp == null || vp.GuiGetFocusOwner() == null)
                ProcessControllerEvent(@event);
        }
    }

    /// <summary>
    ///     Advances smooth scrolling toward the current target position each frame.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || _content == null)
            return;

        if (_disableScrollingIfContentFits && !_scrollbar.Visible)
            return;

        UpdateScrollPosition(delta);
    }

    private void BuildUi()
    {
        // Match NNativeScrollableContainer: vertical gradient (bottom→top in UV) and 5 color stops.
        _maskGradient = new Gradient
        {
            Colors =
            [
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0.4f),
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f),
            ],
        };

        _maskGradientTexture = new GradientTexture2D
        {
            Width = 8,
            Height = 256,
            FillFrom = new Vector2(0f, 1f),
            FillTo = Vector2.Zero,
            Gradient = _maskGradient,
        };

        _fadeMask = new TextureRect
        {
            Name = "Mask",
            MouseFilter = MouseFilterEnum.Ignore,
            ClipChildren = CanvasItem.ClipChildrenMode.Only,
            Texture = _maskGradientTexture,
        };
        _fadeMask.SetAnchorsPreset(LayoutPreset.FullRect);
        _fadeMask.GrowHorizontal = GrowDirection.Both;
        _fadeMask.GrowVertical = GrowDirection.Both;
        _fadeMask.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        _fadeMask.StretchMode = TextureRect.StretchModeEnum.Scale;

        _clipper = new Control
        {
            Name = "Clipper",
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = true,
        };
        _clipper.SetAnchorsPreset(LayoutPreset.FullRect);
        _clipper.GrowHorizontal = GrowDirection.Both;
        _clipper.GrowVertical = GrowDirection.Both;
        ApplyClipperOffsets();

        _fadeMask.AddChild(_clipper);

        _scrollbar = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/scrollbar")).Instantiate<NScrollbar>();
        _scrollbar.Name = "Scrollbar";
        _scrollbar.MouseFilter = MouseFilterEnum.Stop;
        _scrollbar.SetAnchorsPreset(LayoutPreset.RightWide);
        _scrollbar.OffsetLeft = -48f;
        _scrollbar.OffsetRight = 0f;
        ApplyScrollbarOffsets();

        _scrollbar.MousePressed += _ => _scrollbarPressed = true;
        _scrollbar.MouseReleased += _ => _scrollbarPressed = false;

        AddChild(_fadeMask);
        AddChild(_scrollbar);

        _scrollbar.Visible = false;
    }

    private void ApplyClipperOffsets()
    {
        _clipper.OffsetLeft = 0f;
        _clipper.OffsetTop = _paddingTop;
        _clipper.OffsetRight = -ScrollbarGutterWidth;
        _clipper.OffsetBottom = -_paddingBottom;
    }

    private void ApplyScrollbarOffsets()
    {
        _scrollbar.OffsetTop = _paddingTop + 64f;
        _scrollbar.OffsetBottom = -_paddingBottom - 64f;
    }

    private void OnContainerResized()
    {
        ApplyClipperOffsets();
        ApplyScrollbarOffsets();
        SyncContentWidth();
        RebuildMaskGradientOffsets();
        UpdateScrollLayout();
    }

    private void OnContentItemRectChanged()
    {
        SyncScrollbarAfterContentResize();
        UpdateScrollLayout();
    }

    private void DetachContentSignals()
    {
        if (_content == null)
            return;

        if (_content.IsConnected(CanvasItem.SignalName.ItemRectChanged, _contentRectChangedCallable))
            _content.Disconnect(CanvasItem.SignalName.ItemRectChanged, _contentRectChangedCallable);

        if (_content.GetParent() == _clipper)
            _clipper.RemoveChild(_content);

        _content = null;
    }

    private void SyncContentWidth()
    {
        if (_content == null || !IsInstanceValid(_content))
            return;

        var w = _clipper.Size.X;
        if (w < 1e-4f)
            return;

        _content.CustomMinimumSize = new Vector2(w, 0f);
        _content.Size = _content.Size with { X = w };
    }

    private void UpdateScrollLayout()
    {
        if (_content == null)
            return;

        const float epsilon = 1f;

        var contentFits = ContentHeight + _paddingTop + _paddingBottom - epsilon <= _clipper.Size.Y;

        var logicalContentY = _paddingTop + _targetDragPosY;
        var scrollIsAtTop = -logicalContentY <= _paddingTop + epsilon;
        var wasVisible = _scrollbar.Visible;
        var showChrome = !contentFits || !scrollIsAtTop;

        _scrollbar.Visible = showChrome;
        _scrollbar.MouseFilter = showChrome ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        if (!wasVisible && _scrollbar.Visible)
            _targetDragPosY = _content.Position.Y - _paddingTop;

        _fadeMask.ClipChildren = showChrome ? CanvasItem.ClipChildrenMode.Only : CanvasItem.ClipChildrenMode.Disabled;
        _fadeMask.SelfModulate = new Color(1f, 1f, 1f, showChrome ? 1f : 0f);

        if (!showChrome)
            return;

        var scrollDistanceFromTop = Mathf.Max(0f, _paddingTop - logicalContentY);
        var topAlpha = 1f - Mathf.Clamp(scrollDistanceFromTop / TopFade, 0f, 1f);
        var colors = _maskGradient.Colors;
        colors[4] = new Color(1f, 1f, 1f, topAlpha);
        _maskGradient.Colors = colors;
    }

    private void RebuildMaskGradientOffsets()
    {
        var actualHeight = Size.Y;
        if (actualHeight <= 0f)
            return;

        float FromTop(float px) => 1f - px / actualHeight;

        _maskGradient.Offsets =
        [
            0f,
            BottomFade * 0.4f / actualHeight,
            BottomFade / actualHeight,
            FromTop(_paddingTop + TopFade),
            FromTop(_paddingTop),
        ];
    }

    private void SyncScrollbarAfterContentResize()
    {
        if (_content == null || ScrollLimitBottom >= 0f)
            return;

        _scrollbar.SetValueNoSignal(
            Mathf.Clamp((_content.Position.Y - _paddingTop) / ScrollLimitBottom, 0f, 1f) * 100f);
    }

    private static float ComputeOffsetInContent(Control target, Control contentRoot)
    {
        var off = 0f;
        for (Node? n = target; n != null && n != contentRoot; n = n.GetParent())
        {
            if (n is Control c)
                off += c.Position.Y;
        }

        return off;
    }

    private float ComputeOffsetInContent(Control target) =>
        _content == null ? 0f : ComputeOffsetInContent(target, _content);

    private void ProcessControllerEvent(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed(MegaInput.up))
            _targetDragPosY += _controllerScrollAmount;
        else if (inputEvent.IsActionPressed(MegaInput.down))
            _targetDragPosY -= _controllerScrollAmount;
    }

    private void ProcessMouseEvent(InputEvent inputEvent)
    {
        if (_content == null)
            return;

        switch (inputEvent)
        {
            case InputEventMouseMotion motion when _isDragging:
                _targetDragPosY += motion.Relative.Y;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } btn:
                _isDragging = btn.Pressed;
                if (btn.Pressed)
                {
                    _startDragPosY = _content.Position.Y - _paddingTop;
                    _targetDragPosY = _startDragPosY;
                }
                else
                {
                    _isDragging = false;
                }

                break;
        }
    }

    private void ProcessScrollEvent(InputEvent inputEvent)
    {
        _targetDragPosY += ScrollHelper.GetDragForScrollEvent(inputEvent);
    }

    private void UpdateScrollPosition(double delta)
    {
        if (_content == null)
            return;

        // Scrollbar thumb: read Value first so targetY matches the handle; do not Lerp content (feels laggy vs wheel).
        if (_scrollbarPressed && ScrollLimitBottom < 0f)
            _targetDragPosY = Mathf.Lerp(0f, ScrollLimitBottom, (float)_scrollbar.Value * 0.01f);

        _targetDragPosY = Mathf.Clamp(_targetDragPosY, ScrollLimitBottom, 0f);

        var targetY = _paddingTop + _targetDragPosY;

        if (_scrollbarPressed)
        {
            _content.Position = _content.Position with { Y = targetY };
        }
        else if (!Mathf.IsEqualApprox(_content.Position.Y, targetY))
        {
            var y = Mathf.Lerp(_content.Position.Y, targetY, (float)delta * 15f);
            _content.Position = _content.Position with { Y = y };
            if (Mathf.Abs(_content.Position.Y - targetY) < 0.5f)
                _content.Position = _content.Position with { Y = targetY };

            if (ScrollLimitBottom < 0f)
                _scrollbar.SetValueWithoutAnimation(
                    Mathf.Clamp((_content.Position.Y - _paddingTop) / ScrollLimitBottom, 0f, 1f) * 100f);
        }

        if (!_isDragging && !_scrollbarPressed)
        {
            if (_targetDragPosY < Mathf.Min(ScrollLimitBottom, 0f))
                _targetDragPosY = Mathf.Lerp(_targetDragPosY, ScrollLimitBottom, (float)delta * 12f);
            else if (_targetDragPosY > Mathf.Max(ScrollLimitBottom, 0f))
                _targetDragPosY = Mathf.Lerp(_targetDragPosY, 0f, (float)delta * 12f);
        }

        UpdateScrollLayout();
    }
}
