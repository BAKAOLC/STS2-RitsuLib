using Godot;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Ui.Overlay
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuDebugToolsDock : Control
    {
        private const float RailLeft = 24f;
        private const float RailWidth = 64f;
        private const float PanelRightMargin = 24f;
        private const float PanelMaximumViewportFraction = 0.9f;
        private const double CollapseGraceMilliseconds = 320d;
        private const double WorkspaceResizeDurationSeconds = 0.22d;
        private readonly Dictionary<string, Button> _pageButtons = new(StringComparer.OrdinalIgnoreCase);
        private bool _available;
        private Control _clickAway = null!;
        private Control _clipHost = null!;
        private double _collapseAfter;
        private bool _layoutBuilt;
        private Label _pageTitle = null!;
        private Button _peekTab = null!;
        private PanelContainer _rail = null!;
        private VBoxContainer _railButtons = null!;
        private bool _railShown;
        private StyleBoxFlat _railStyle = null!;
        private Tween? _railTween;
        private bool _suppressed;
        private IDisposable? _tooltipTimingScope;
        private Control _workspaceContent = null!;
        private Control _workspaceMover = null!;
        private bool _workspaceResizeAnimating;
        private double _workspaceResizeElapsed;
        private float _workspaceResizeFrom;
        private Tween? _workspaceTween;
        private float _workspaceWidth;

        internal RitsuDebugToolsDock(RitsuDebugToolsPanel panel)
        {
            ArgumentNullException.ThrowIfNull(panel);
            Panel = panel;
            Panel.PagesChanged += RebuildPageButtons;
            Panel.PageChanged += OnPageChanged;
            Panel.CreaturePickingStarted += OnCreaturePickingStarted;
            Panel.CreaturePickingFinished += OnCreaturePickingFinished;
        }

        internal bool Expanded { get; private set; }

        internal bool SessionVisible { get; private set; }

        internal RitsuDebugToolsPanel Panel { get; }

        private float HiddenRailLeft => -(RailLeft + RailWidth);

        internal event EventHandler? Expanding;

        internal event EventHandler? Collapsed;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            ZIndex = 40;
            BuildLayout();
            GetViewport().SizeChanged += OnViewportSizeChanged;
            RitsuDebugToolsInterfaceStateStore.StateRestored += OnInterfaceStateRestored;
            SetProcess(false);
            SetProcessUnhandledInput(false);
            SyncAvailability();
            RestoreSessionVisibility(RitsuDebugToolsInterfaceStateStore.IsVisible());
        }

        public override void _Process(double delta)
        {
            if (!_workspaceResizeAnimating)
                return;
            _workspaceResizeElapsed = Math.Min(
                WorkspaceResizeDurationSeconds,
                _workspaceResizeElapsed + delta);
            var progress = (float)(_workspaceResizeElapsed / WorkspaceResizeDurationSeconds);
            var easedProgress = (1f - MathF.Cos(progress * MathF.PI)) * 0.5f;
            SetWorkspaceWidth(Mathf.Lerp(_workspaceResizeFrom, _workspaceWidth, easedProgress));
            if (progress < 1f)
                return;
            SetWorkspaceWidth(_workspaceWidth);
            _workspaceResizeAnimating = false;
            SetProcess(false);
        }

        public override void _ExitTree()
        {
            var viewport = GetViewport();
            if (viewport != null)
                viewport.SizeChanged -= OnViewportSizeChanged;
            Panel.PagesChanged -= RebuildPageButtons;
            Panel.PageChanged -= OnPageChanged;
            Panel.CreaturePickingStarted -= OnCreaturePickingStarted;
            Panel.CreaturePickingFinished -= OnCreaturePickingFinished;
            RitsuDebugToolsInterfaceStateStore.StateRestored -= OnInterfaceStateRestored;
            _railTween?.Kill();
            _railTween = null;
            ReleaseQuickTooltipTiming();
            _workspaceTween?.Kill();
            _workspaceTween = null;
            StopWorkspaceResize();
            base._ExitTree();
        }

        private void OnCreaturePickingStarted()
        {
            Collapse(true);
        }

        private void OnCreaturePickingFinished()
        {
            Expand();
        }

        internal void SetAvailable(bool available)
        {
            _available = available;
            if (!_layoutBuilt)
                return;
            if (!available)
            {
                Panel.CancelCreaturePicking();
                SessionVisible = false;
                Collapse(true);
            }

            SyncAvailability();
            if (available)
                RestoreSessionVisibility(RitsuDebugToolsInterfaceStateStore.IsVisible());
        }

        internal void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;
            if (!_layoutBuilt)
                return;
            if (suppressed)
            {
                Panel.CancelCreaturePicking();
                Collapse(true);
            }

            SyncAvailability();
        }

        internal void Expand(string? pageId = null, bool remember = true)
        {
            if (!_available || _suppressed || !_layoutBuilt)
                return;
            Panel.CancelCreaturePicking();
            var wasSessionVisible = SessionVisible;
            SessionVisible = true;
            if (remember && !wasSessionVisible)
                RitsuDebugToolsInterfaceStateStore.RememberVisibility(true);
            SyncAvailability();
            if (!string.IsNullOrWhiteSpace(pageId))
                Panel.SelectPage(pageId);
            if (Expanded)
            {
                RefreshPageButtonStyles();
                return;
            }

            Expanding?.Invoke(this, EventArgs.Empty);
            Expanded = true;
            _clickAway.Show();
            _workspaceMover.Show();
            SetRailJoined(true);
            SlideRail(true);
            RefreshPageButtonStyles();
            RecalculateWorkspaceWidth();

            _workspaceTween?.Kill();
            _workspaceMover.Position = new(-_workspaceWidth, 0f);
            _workspaceMover.Modulate = new(1f, 1f, 1f, 0.82f);
            _workspaceTween = _workspaceMover.CreateTween();
            _workspaceTween.SetTrans(Tween.TransitionType.Quint);
            _workspaceTween.SetEase(Tween.EaseType.Out);
            _workspaceTween.TweenProperty(_workspaceMover, "position:x", 0f, 0.34f);
            _workspaceTween.Parallel().TweenProperty(_workspaceMover, "modulate:a", 1f, 0.2f);
            _workspaceTween.TweenCallback(Callable.From(() => _workspaceTween = null));
        }

        internal void HideForSession(bool remember = true)
        {
            Panel.CancelCreaturePicking();
            var wasSessionVisible = SessionVisible;
            SessionVisible = false;
            if (remember && wasSessionVisible)
                RitsuDebugToolsInterfaceStateStore.RememberVisibility(false);
            Collapse(true);
            SyncAvailability();
        }

        internal void Collapse(bool immediate = false)
        {
            if (!Expanded)
                return;
            Expanded = false;
            RefreshPageButtonStyles();
            if (immediate || !_workspaceMover.IsInsideTree())
            {
                FinishCollapse();
                return;
            }

            _workspaceTween?.Kill();
            _workspaceTween = _workspaceMover.CreateTween();
            _workspaceTween.SetTrans(Tween.TransitionType.Cubic);
            _workspaceTween.SetEase(Tween.EaseType.In);
            _workspaceTween.TweenProperty(_workspaceMover, "position:x", -Math.Max(1f, _workspaceWidth), 0.24f);
            _workspaceTween.Parallel().TweenProperty(_workspaceMover, "modulate:a", 0.82f, 0.18f);
            _workspaceTween.TweenCallback(Callable.From(FinishCollapse));
        }

        private void BuildLayout()
        {
            _layoutBuilt = true;
            _clickAway = new ColorRect
            {
                Color = new(0f, 0f, 0f, 0.22f),
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
            };
            _clickAway.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _clickAway.GuiInput += OnClickAwayInput;
            AddChild(_clickAway);

            BuildRail();
            BuildWorkspace();
            BuildPeekTab();

            var timer = new Timer
            {
                WaitTime = 0.1d,
                Autostart = true,
            };
            timer.Timeout += PollRailHover;
            AddChild(timer);
        }

        private void BuildRail()
        {
            _rail = new()
            {
                Name = "DebugToolsRail",
                AnchorLeft = 0f,
                AnchorRight = 0f,
                AnchorTop = 0.08f,
                AnchorBottom = 0.92f,
                OffsetLeft = HiddenRailLeft,
                OffsetRight = HiddenRailLeft + RailWidth,
                MouseFilter = MouseFilterEnum.Stop,
                Modulate = new(1f, 1f, 1f, 0f),
            };
            _railStyle = RitsuShellPanelStyles.CreateFramedSurface(
                RitsuShellTheme.Current.Surface.Sidebar,
                RitsuShellTheme.Current.Metric.Radius.Default);
            _rail.AddThemeStyleboxOverride("panel", _railStyle);
            AddChild(_rail);

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 4);
            margin.AddThemeConstantOverride("margin_top", 10);
            margin.AddThemeConstantOverride("margin_right", 4);
            margin.AddThemeConstantOverride("margin_bottom", 10);
            _rail.AddChild(margin);
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            margin.AddChild(scroll);

            var scrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            scrollFrame.AddThemeConstantOverride("margin_right", 0);
            scroll.AddChild(scrollFrame);
            _railButtons = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _railButtons.AddThemeConstantOverride("separation", 4);
            scrollFrame.AddChild(_railButtons);

            var scrollBar = scroll.GetVScrollBar();
            scrollBar.VisibilityChanged += () => SyncRailScrollGutter(scroll, scrollFrame);
            SyncRailScrollGutter(scroll, scrollFrame);
        }

        private void BuildWorkspace()
        {
            _clipHost = new()
            {
                ClipContents = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _clipHost.AnchorLeft = 0f;
            _clipHost.AnchorRight = 0f;
            _clipHost.AnchorTop = 0.08f;
            _clipHost.AnchorBottom = 0.92f;
            _clipHost.OffsetLeft = RailLeft + RailWidth;
            _clipHost.OffsetRight = RailLeft + RailWidth + 1f;
            AddChild(_clipHost);

            _workspaceMover = new()
            {
                ClipContents = true,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };
            _workspaceMover.AnchorLeft = 0f;
            _workspaceMover.AnchorRight = 1f;
            _workspaceMover.AnchorTop = 0f;
            _workspaceMover.AnchorBottom = 1f;
            _workspaceMover.OffsetLeft = 0f;
            _workspaceMover.OffsetRight = 0f;
            _clipHost.AddChild(_workspaceMover);

            var workspaceSurface = new Panel
            {
                MouseFilter = MouseFilterEnum.Stop,
            };
            workspaceSurface.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var workspaceStyle = RitsuShellPanelStyles.CreateFramedSurface(
                RitsuShellTheme.Current.Surface.Content,
                RitsuShellTheme.Current.Metric.Radius.Default);
            workspaceStyle.CornerRadiusTopLeft = 0;
            workspaceStyle.CornerRadiusBottomLeft = 0;
            workspaceStyle.BorderWidthLeft = 0;
            workspaceSurface.AddThemeStyleboxOverride("panel", workspaceStyle);
            _workspaceMover.AddChild(workspaceSurface);

            _workspaceContent = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _workspaceContent.AnchorLeft = 0f;
            _workspaceContent.AnchorRight = 0f;
            _workspaceContent.AnchorTop = 0f;
            _workspaceContent.AnchorBottom = 1f;
            _workspaceContent.OffsetLeft = 0f;
            _workspaceContent.OffsetRight = 1f;
            _workspaceMover.AddChild(_workspaceContent);

            var margin = new MarginContainer();
            margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            margin.AddThemeConstantOverride("margin_left", 14);
            margin.AddThemeConstantOverride("margin_top", 10);
            margin.AddThemeConstantOverride("margin_right", 14);
            margin.AddThemeConstantOverride("margin_bottom", 10);
            _workspaceContent.AddChild(margin);
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            column.AddThemeConstantOverride("separation", 8);
            margin.AddChild(column);

            var header = new HBoxContainer
            {
                CustomMinimumSize = new(0f, 42f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            header.AddThemeConstantOverride("separation", 10);
            _pageTitle = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _pageTitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            _pageTitle.AddThemeFontSizeOverride("font_size", 20);
            _pageTitle.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            header.AddChild(_pageTitle);
            column.AddChild(header);

            var separator = new HSeparator();
            column.AddChild(separator);
            Panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            Panel.SizeFlagsVertical = SizeFlags.ExpandFill;
            column.AddChild(Panel);
        }

        private void BuildPeekTab()
        {
            _peekTab = CreateIconButton(
                RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.ChevronRight,
                    13,
                    RitsuShellTheme.Current.Text.LabelPrimary),
                ModSettingsLocalization.Get("ritsulib.debugTools.expand", "Show developer tools"),
                () => Expand());
            _peekTab.Name = "DebugToolsPeekTab";
            _peekTab.FocusMode = FocusModeEnum.None;
            _peekTab.CustomMinimumSize = new(15f, 50f);
            _peekTab.AnchorLeft = 0f;
            _peekTab.AnchorRight = 0f;
            _peekTab.AnchorTop = 0.5f;
            _peekTab.AnchorBottom = 0.5f;
            _peekTab.OffsetLeft = 0f;
            _peekTab.OffsetRight = 15f;
            _peekTab.OffsetTop = -25f;
            _peekTab.OffsetBottom = 25f;
            _peekTab.MouseEntered += () => SlideRail(true);
            AddChild(_peekTab);
        }

        private void RebuildPageButtons(IReadOnlyList<RitsuDebugToolsPageView> pages)
        {
            foreach (var child in _railButtons.GetChildren())
            {
                _railButtons.RemoveChild(child);
                child.QueueFree();
            }

            _pageButtons.Clear();
            foreach (var page in pages)
            {
                var captured = page;
                var icon = page.Icon ?? RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Puzzle,
                    22,
                    RitsuShellTheme.Current.Text.LabelPrimary);
                var button = CreateIconButton(icon, page.Title, () => ActivatePage(captured));
                button.SetMeta("page_id", page.Id);
                _railButtons.AddChild(button);
                _pageButtons.Add(page.Id, button);
            }

            RefreshPageButtonStyles();
        }

        private void ActivatePage(RitsuDebugToolsPageView page)
        {
            var wasCurrent = _pageButtons.TryGetValue(page.Id, out var button) && IsButtonSelected(button);
            if (Expanded && wasCurrent)
            {
                Collapse();
                return;
            }

            Panel.SelectPage(page.Id);
            Expand();
        }

        private void OnPageChanged(RitsuDebugToolsPageView page)
        {
            _pageTitle.Text = page.Title;
            RecalculateWorkspaceWidth(_workspaceMover.Visible);
            RefreshPageButtonStyles();
        }

        private void OnInterfaceStateRestored(bool isVisible)
        {
            Callable.From(() =>
            {
                if (IsInstanceValid(this) && IsInsideTree())
                    RestoreSessionVisibility(isVisible);
            }).CallDeferred();
        }

        private void RestoreSessionVisibility(bool isVisible)
        {
            if (!isVisible)
            {
                HideForSession(false);
                return;
            }

            SessionVisible = true;
            Collapse(true);
            SyncAvailability();
        }

        private void RefreshPageButtonStyles()
        {
            var currentId = Panel.CurrentPageId;
            foreach (var (id, button) in _pageButtons)
            {
                var selected = id.Equals(currentId, StringComparison.OrdinalIgnoreCase);
                button.SetMeta("selected", selected);
                var normal = RitsuShellPanelStyles.CreateSidebarModCardCompact(8, selected, 4);
                var hover = RitsuShellPanelStyles.CreateSidebarModCardCompact(8, true, 4);
                button.AddThemeStyleboxOverride("normal", normal);
                button.AddThemeStyleboxOverride("hover", hover);
                button.AddThemeStyleboxOverride("pressed", hover);
                button.AddThemeStyleboxOverride("focus", hover);
            }
        }

        private static bool IsButtonSelected(Button button)
        {
            return button.HasMeta("selected") && button.GetMeta("selected").AsBool();
        }

        private static void SyncRailScrollGutter(ScrollContainer scroll, MarginContainer frame)
        {
            var gutter = scroll.GetVScrollBar().Visible
                ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll)
                : 0;
            if (frame.GetThemeConstant("margin_right") == gutter)
                return;
            frame.AddThemeConstantOverride("margin_right", gutter);
            frame.QueueSort();
        }

        private static Button CreateIconButton(Texture2D? icon, string tooltip, Action action)
        {
            var button = new ModSettingsGamepadCompatibleButton
            {
                Icon = icon,
                TooltipText = tooltip,
                CustomMinimumSize = new(42f, 42f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                IconAlignment = HorizontalAlignment.Center,
                ExpandIcon = false,
            };
            button.Pressed += action;
            return button;
        }

        private void PollRailHover()
        {
            if (!_available || _suppressed || Expanded || !Visible)
                return;
            var mouse = GetViewport().GetMousePosition();
            var railRect = _rail.GetGlobalRect();
            var overRail = _railShown && railRect.Grow(8f).HasPoint(mouse);
            var overPeek = _peekTab.Visible && _peekTab.GetGlobalRect().Grow(4f).HasPoint(mouse);
            var inSideZone = mouse.X <= RailLeft + RailWidth + 16f &&
                             mouse.Y >= railRect.Position.Y - 20f && mouse.Y <= railRect.End.Y + 20f;
            if (overRail || overPeek || inSideZone)
            {
                _collapseAfter = 0d;
                SlideRail(true);
                return;
            }

            if (!_railShown)
                return;
            var now = Time.GetTicksMsec();
            if (_collapseAfter <= 0d)
                _collapseAfter = now + CollapseGraceMilliseconds;
            else if (now >= _collapseAfter)
                SlideRail(false);
        }

        private void SlideRail(bool show)
        {
            if (Expanded)
                show = true;
            if (_railShown == show || !_layoutBuilt)
                return;
            _railShown = show;
            _collapseAfter = 0d;
            if (show)
                _tooltipTimingScope ??= RitsuShellTooltipTiming.Acquire(RitsuShellTooltipTiming.StandardDelaySeconds);
            else
                ReleaseQuickTooltipTiming();
            _peekTab.Visible = _available && !_suppressed && !show && !Expanded;
            _railTween?.Kill();
            _railTween = _rail.CreateTween();
            _railTween.SetTrans(Tween.TransitionType.Cubic);
            _railTween.SetEase(Tween.EaseType.Out);
            var left = show ? RailLeft : HiddenRailLeft;
            _railTween.TweenProperty(_rail, "offset_left", left, 0.2f);
            _railTween.Parallel().TweenProperty(_rail, "offset_right", left + RailWidth, 0.2f);
            _railTween.Parallel().TweenProperty(_rail, "modulate:a", show ? 1f : 0f, 0.15f);
            _railTween.TweenCallback(Callable.From(() => _railTween = null));
        }

        private void RecalculateWorkspaceWidth(bool animate = false)
        {
            if (!_layoutBuilt)
                return;
            var viewportWidth = GetViewport().GetVisibleRect().Size.X;
            var available = Math.Max(1f, viewportWidth - RailLeft - RailWidth - PanelRightMargin);
            var widthFraction = Math.Min(PanelMaximumViewportFraction, Panel.CurrentPageWidthFraction);
            var targetWidth = Math.Max(1f, Math.Min(available, viewportWidth * widthFraction));
            var currentWidth = Math.Max(1f, _clipHost.Size.X);
            SetWorkspaceContentWidth(targetWidth);
            if (!animate && _workspaceResizeAnimating &&
                Math.Abs(_workspaceWidth - targetWidth) < 0.5f)
                return;

            _workspaceWidth = targetWidth;
            if (!animate || Math.Abs(currentWidth - targetWidth) < 0.5f)
            {
                StopWorkspaceResize();
                SetWorkspaceWidth(targetWidth);
                return;
            }

            SetWorkspaceWidth(currentWidth);
            _workspaceResizeFrom = currentWidth;
            _workspaceResizeElapsed = 0d;
            _workspaceResizeAnimating = true;
            SetProcess(true);
        }

        private void SetWorkspaceWidth(float width)
        {
            _clipHost.OffsetRight = _clipHost.OffsetLeft + width;
        }

        private void SetWorkspaceContentWidth(float width)
        {
            _workspaceContent.OffsetRight = _workspaceContent.OffsetLeft + width;
        }

        private void StopWorkspaceResize()
        {
            _workspaceResizeAnimating = false;
            SetProcess(false);
        }

        private void SetRailJoined(bool joined)
        {
            var radius = joined ? 0 : RitsuShellTheme.Current.Metric.Radius.Default;
            _railStyle.CornerRadiusTopRight = radius;
            _railStyle.CornerRadiusBottomRight = radius;
            _railStyle.BorderWidthRight = joined ? 0 : 1;
        }

        private void FinishCollapse()
        {
            _workspaceTween?.Kill();
            _workspaceTween = null;
            _workspaceMover.Hide();
            _workspaceMover.Position = Vector2.Zero;
            _workspaceMover.Modulate = Colors.White;
            _clickAway.Hide();
            SetRailJoined(false);
            if (_available && SessionVisible && !_suppressed)
                SlideRail(true);
            Collapsed?.Invoke(this, EventArgs.Empty);
        }

        private void SyncAvailability()
        {
            Visible = _available && SessionVisible && !_suppressed;
            if (!Visible)
            {
                _railShown = false;
                _collapseAfter = 0d;
                ReleaseQuickTooltipTiming();
                _railTween?.Kill();
                _railTween = null;
                _workspaceTween?.Kill();
                _workspaceTween = null;
                StopWorkspaceResize();
                _peekTab.Hide();
                _rail.Hide();
                _clickAway.Hide();
                _workspaceMover.Hide();
                _rail.OffsetLeft = HiddenRailLeft;
                _rail.OffsetRight = HiddenRailLeft + RailWidth;
                _rail.Modulate = new(1f, 1f, 1f, 0f);
                return;
            }

            _rail.Show();
            RecalculateWorkspaceWidth();
            if (Expanded)
            {
                SlideRail(true);
                _peekTab.Hide();
                return;
            }

            if (_railShown)
            {
                _peekTab.Hide();
                return;
            }

            _rail.OffsetLeft = HiddenRailLeft;
            _rail.OffsetRight = HiddenRailLeft + RailWidth;
            _rail.Modulate = new(1f, 1f, 1f, 0f);
            _peekTab.Show();
        }

        private void ReleaseQuickTooltipTiming()
        {
            _tooltipTimingScope?.Dispose();
            _tooltipTimingScope = null;
        }

        private void OnClickAwayInput(InputEvent inputEvent)
        {
            if (inputEvent is not InputEventMouseButton { Pressed: true })
                return;
            Collapse();
            GetViewport().SetInputAsHandled();
        }

        private void OnViewportSizeChanged()
        {
            RecalculateWorkspaceWidth();
        }
    }
}
