using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal static partial class ModSettingsUiFactory
    {
        internal sealed partial class FastVerticalStack : Container
        {
            private static readonly HashSet<FastVerticalStack> DeferredLayoutStacks = [];
            private static int _layoutDeferDepth;
            private float _layoutWidthOverride = -1f;
            private int _separation;

            public FastVerticalStack(int separation = 0)
            {
                _separation = separation;
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
            }

            public FastVerticalStack()
            {
            }

            public int Separation
            {
                get => _separation;
                set
                {
                    if (_separation == value)
                        return;
                    _separation = value;
                    RequestLayout();
                }
            }

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

            public override Vector2 _GetMinimumSize()
            {
                var layoutWidth = ResolveLayoutWidth();
                var minHeight = 0f;
                var visibleCount = 0;
                foreach (var child in GetChildren())
                {
                    if (child is not Control control || !IsInstanceValid(control) || !control.Visible)
                        continue;

                    if (layoutWidth > 1f)
                        PrepareChildWidth(control, layoutWidth);
                    var childMin = control.GetCombinedMinimumSize();
                    minHeight += childMin.Y;
                    visibleCount++;
                }

                if (visibleCount > 1)
                    minHeight += _separation * (visibleCount - 1);
                if (layoutWidth > 1f)
                    return new(1f, minHeight);

                var minWidth = 0f;
                foreach (var child in GetChildren())
                {
                    if (child is not Control control || !IsInstanceValid(control) || !control.Visible)
                        continue;

                    minWidth = Math.Max(minWidth, control.GetCombinedMinimumSize().X);
                }

                return new(minWidth, minHeight);
            }

            internal void SetLayoutWidth(float width)
            {
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

            internal void RequestLayout()
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

            internal static void RequestAncestorLayouts(Control node)
            {
                for (var current = node; current != null; current = current.GetParent() as Control)
                    if (current is FastVerticalStack stack)
                        stack.RequestLayout();
            }

            internal static IDisposable DeferLayoutRequests()
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

                if (control is FastVerticalStack stack)
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

        internal partial class FastSettingLine : Control
        {
            private readonly BoxEdges _lineMargins;
            private Control? _actionControl;
            private MegaRichTextLabel? _descriptionLabel;
            private MegaRichTextLabel? _label;
            private int _layoutDeferDepth;
            private bool _layoutDirty;
            private StyleBoxFlat _surfaceStyle;
            private Control? _valueControl;

            public FastSettingLine(Control? valueControl)
            {
                Name = "FastSettingLine";
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
                ClipContents = false;

                var lineMargins = RitsuShellThemeLayoutResolver.ResolveEdges("components.entryLine.layout.margin", 8);
                _lineMargins = new(
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.left",
                        lineMargins.Left),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.top", 4),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.right",
                        lineMargins.Right),
                    RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.margin.bottom", 4));
                _surfaceStyle = CreateEntrySurfaceStyle();

                _label = CreateHeaderLabel(string.Empty, RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle,
                    HorizontalAlignment.Left, null, RitsuShellTheme.Current.Text.RichTitle);
                _label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                AddChild(_label);

                _valueControl = valueControl;
                if (_valueControl == null) return;
                PrepareValueControl(_valueControl);
                AddChild(_valueControl);
            }

            public FastSettingLine()
            {
                _surfaceStyle = CreateEntrySurfaceStyle();
            }

            internal int ReuseVersion { get; private set; }

            internal T GetValueControl<T>() where T : Control
            {
                return (T)_valueControl!;
            }

            internal int Bind(ModSettingsUiContext context, Func<string> labelProvider,
                Func<string> descriptionProvider, Control? actionControl,
                ModSettingsText? labelRefreshSource = null, ModSettingsText? descriptionRefreshSource = null)
            {
                ReuseVersion++;
                Visible = true;
                MouseFilter = MouseFilterEnum.Ignore;
                ProcessMode = ProcessModeEnum.Inherit;
                Modulate = Colors.White;

                using (DeferLineLayoutRequests())
                {
                    SetLabel(labelProvider());
                    SetDescription(descriptionProvider());
                    ReplaceActionControl(actionControl);

                    if (_valueControl != null)
                    {
                        PrepareValueControl(_valueControl);
                        if (actionControl is ModSettingsActionsButton actionsButton)
                            AttachContextMenuTargets(this, _valueControl, actionsButton);
                    }
                }

                var version = ReuseVersion;
                var labelSpec = labelRefreshSource?.GetUiRefreshSpec() ?? ModSettingsUiRefreshSpec.StaticDisplay;
                RegisterRefreshWhenAlive(context, this, () =>
                {
                    if (ReuseVersion != version)
                        return;
                    SetLabel(labelProvider());
                }, labelSpec);
                var descriptionSpec = descriptionRefreshSource?.GetUiRefreshSpec() ??
                                      ModSettingsUiRefreshSpec.StaticDisplay;
                RegisterRefreshWhenAlive(context, this, () =>
                {
                    if (ReuseVersion != version)
                        return;
                    SetDescription(descriptionProvider());
                }, descriptionSpec);
                if (_valueControl != null)
                    AttachHostSurfaceReadOnlySync(context, _valueControl, actionControl,
                        () => ReuseVersion == version);
                return version;
            }

            internal void ReleaseForPool()
            {
                ReuseVersion++;
                this.ReleaseFocusIfInsideTree();
                Visible = false;
                ProcessMode = ProcessModeEnum.Inherit;
                Modulate = Colors.White;
                SetLabel(string.Empty);
                SetDescription(string.Empty);

                switch (_valueControl)
                {
                    case ModSettingsToggleControl toggle:
                        toggle.ClearBinding();
                        break;
                    case ModSettingsTextButton textButton:
                        textButton.ClearAction();
                        break;
                }

                ReplaceActionControl(null);
                if (GetParent() is { } parent)
                    parent.RemoveChild(this);
            }

            public override void _Notification(int what)
            {
                base._Notification(what);
                if (what == (int)NotificationResized)
                {
                    LayoutChildren();
                    return;
                }

                if (what != (int)NotificationThemeChanged)
                    return;
                _surfaceStyle = CreateEntrySurfaceStyle();
                RequestLayout();
            }

            public override Vector2 _GetMinimumSize()
            {
                var surfaceMargins = GetSurfaceMargins();
                var textMin = ComputeTextColumnMinSize();
                var valueMin = GetVisibleMinSize(_valueControl);
                var actionMin = GetVisibleMinSize(_actionControl);
                var rowSeparation = ResolveRowSeparation();
                var availableContentWidth = ResolveCurrentContentWidth();
                if (availableContentWidth > 1f)
                {
                    actionMin.X = Math.Min(actionMin.X, availableContentWidth);
                    var remaining = Math.Max(0f, availableContentWidth - actionMin.X -
                                                 (actionMin.X > 0f ? rowSeparation : 0f));
                    valueMin.X = Math.Min(valueMin.X, remaining);
                }

                var controlWidth = valueMin.X + actionMin.X;
                if (valueMin.X > 0f && actionMin.X > 0f)
                    controlWidth += rowSeparation;
                var rowWidth = textMin.X + controlWidth;
                if (textMin.X > 0f && controlWidth > 0f)
                    rowWidth += rowSeparation;
                var rowHeight = Math.Max(textMin.Y, Math.Max(valueMin.Y, actionMin.Y));

                return new(
                    _lineMargins.Left + _lineMargins.Right + surfaceMargins.Left + surfaceMargins.Right + rowWidth,
                    _lineMargins.Top + _lineMargins.Bottom + surfaceMargins.Top + surfaceMargins.Bottom + rowHeight);
            }

            public override void _Draw()
            {
                DrawStyleBox(_surfaceStyle, GetSurfaceRect());
            }

            protected void ReplaceValueControl(Control? next)
            {
                if (_valueControl != null && IsInstanceValid(_valueControl) && _valueControl.GetParent() == this)
                    RemoveChild(_valueControl);
                _valueControl = next;
                if (_valueControl != null)
                {
                    PrepareValueControl(_valueControl);
                    if (_valueControl.GetParent() != this)
                        AddChild(_valueControl);
                }

                RequestLayout();
            }

            private void ReplaceActionControl(Control? next)
            {
                if (_actionControl != null && IsInstanceValid(_actionControl))
                {
                    if (_actionControl.GetParent() == this)
                        RemoveChild(_actionControl);
                    _actionControl.QueueFree();
                }

                _actionControl = next;
                if (_actionControl == null)
                {
                    RequestLayout();
                    return;
                }

                _actionControl.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                AddChild(_actionControl);
                RequestLayout();
            }

            private void PrepareValueControl(Control valueControl)
            {
                valueControl.CustomMinimumSize = new(
                    Math.Max(EntryControlWidth, valueControl.CustomMinimumSize.X),
                    Mathf.Max(valueControl.CustomMinimumSize.Y,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
                valueControl.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
                valueControl.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                valueControl.ProcessMode = ProcessModeEnum.Inherit;
                valueControl.Modulate = Colors.White;
            }

            private void SetLabel(string text)
            {
                var displayText = string.IsNullOrWhiteSpace(text)
                    ? ModSettingsLocalization.Get("entry.label.empty", "-")
                    : text;
                var label = EnsureRichLabel();
                label.SetTextAutoSize(displayText);
                label.Visible = true;

                RequestLayout();
            }

            private void SetDescription(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (_descriptionLabel != null)
                    {
                        _descriptionLabel.SetTextAutoSize(string.Empty);
                        _descriptionLabel.Visible = false;
                    }

                    RequestLayout();
                    return;
                }

                var descriptionLabel = EnsureRichDescriptionLabel();
                descriptionLabel.SetTextAutoSize(text);
                descriptionLabel.Visible = true;

                RequestLayout();
            }

            private MegaRichTextLabel EnsureRichLabel()
            {
                if (_label != null)
                    return _label;

                _label = CreateHeaderLabel(string.Empty, RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle,
                    HorizontalAlignment.Left, null, RitsuShellTheme.Current.Text.RichTitle);
                _label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                AddChild(_label);
                return _label;
            }

            private MegaRichTextLabel EnsureRichDescriptionLabel()
            {
                if (_descriptionLabel != null)
                    return _descriptionLabel;

                _descriptionLabel = CreateDescriptionLabel(string.Empty);
                AddChild(_descriptionLabel);
                return _descriptionLabel;
            }

            private void RequestLayout()
            {
                if (_layoutDeferDepth > 0)
                {
                    _layoutDirty = true;
                    return;
                }

                UpdateMinimumSize();
                LayoutChildren();
                QueueRedraw();
                FastVerticalStack.RequestAncestorLayouts(this);
            }

            private IDisposable DeferLineLayoutRequests()
            {
                _layoutDeferDepth++;
                return new LineLayoutScope(this);
            }

            private void LayoutChildren()
            {
                if (!IsInsideTree())
                    return;

                var content = GetContentRect();
                var rowSeparation = ResolveRowSeparation();
                var columnSeparation = ResolveLeftColumnSeparation();
                var actionMin = GetVisibleMinSize(_actionControl);
                var valueMin = GetVisibleMinSize(_valueControl);
                var contentLeft = content.Position.X;
                var right = content.Position.X + content.Size.X;

                if (_actionControl is { Visible: true })
                {
                    var actionWidth = Math.Min(actionMin.X, Math.Max(0f, right - contentLeft));
                    _actionControl.Position = new(right - actionWidth,
                        content.Position.Y + Math.Max(0f, (content.Size.Y - actionMin.Y) * 0.5f));
                    _actionControl.Size = new(actionWidth, actionMin.Y);
                    right -= actionWidth + rowSeparation;
                }

                if (_valueControl is { Visible: true })
                {
                    var valueWidth = Math.Min(valueMin.X, Math.Max(0f, right - contentLeft));
                    _valueControl.Position = new(right - valueWidth,
                        content.Position.Y + Math.Max(0f, (content.Size.Y - valueMin.Y) * 0.5f));
                    _valueControl.Size = new(valueWidth, valueMin.Y);
                    right -= valueWidth + rowSeparation;
                }

                var textWidth = Math.Max(0f, right - content.Position.X);
                PrepareTextLabelWidth(_label, textWidth);
                PrepareTextLabelWidth(_descriptionLabel, textWidth);
                var labelMin = GetLabelMinSize();
                var descriptionMin = GetDescriptionMinSize();
                var textHeight = labelMin.Y;
                if (descriptionMin.Y > 0f)
                    textHeight += columnSeparation + descriptionMin.Y;
                var y = content.Position.Y + Math.Max(0f, (content.Size.Y - textHeight) * 0.5f);

                if (_label is { Visible: true })
                {
                    _label.Position = new(content.Position.X, y);
                    _label.Size = new(textWidth, labelMin.Y);
                    y += labelMin.Y;
                }

                y += columnSeparation;
                if (_descriptionLabel is not { Visible: true }) return;
                _descriptionLabel.Position = new(content.Position.X, y);
                _descriptionLabel.Size = new(textWidth, descriptionMin.Y);
            }

            private static void PrepareTextLabelWidth(MegaRichTextLabel? label, float width)
            {
                if (label is not { Visible: true } || width <= 1f)
                    return;

                if (Math.Abs(label.Size.X - width) < 0.5f)
                    return;

                label.Size = new(width, Math.Max(label.Size.Y, label.CustomMinimumSize.Y));
                label.UpdateMinimumSize();
            }

            private Vector2 ComputeTextColumnMinSize()
            {
                var currentTextWidth = ResolveCurrentTextColumnWidth();
                PrepareTextLabelWidth(_label, currentTextWidth);
                PrepareTextLabelWidth(_descriptionLabel, currentTextWidth);
                var labelMin = GetLabelMinSize();
                var descriptionMin = GetDescriptionMinSize();
                var height = labelMin.Y;
                if (descriptionMin.Y > 0f)
                    height += ResolveLeftColumnSeparation() + descriptionMin.Y;
                return new(Math.Min(Math.Max(labelMin.X, descriptionMin.X), ResolveTextColumnMaxMinimumWidth()),
                    height);
            }

            private float ResolveCurrentTextColumnWidth()
            {
                var contentWidth = ResolveCurrentContentWidth();
                if (contentWidth <= 1f)
                    return 0f;

                var actionMin = GetVisibleMinSize(_actionControl);
                var valueMin = GetVisibleMinSize(_valueControl);
                var rowSeparation = ResolveRowSeparation();
                var controlWidth = valueMin.X + actionMin.X;
                if (valueMin.X > 0f && actionMin.X > 0f)
                    controlWidth += rowSeparation;
                if (controlWidth > 0f)
                    controlWidth += rowSeparation;
                return Math.Max(0f, contentWidth - controlWidth);
            }

            private float ResolveCurrentContentWidth()
            {
                if (Size.X <= 1f)
                    return 0f;

                var surfaceMargins = GetSurfaceMargins();
                return Math.Max(0f,
                    Size.X - _lineMargins.Left - _lineMargins.Right -
                    surfaceMargins.Left - surfaceMargins.Right);
            }

            private Rect2 GetSurfaceRect()
            {
                return new(
                    _lineMargins.Left,
                    _lineMargins.Top,
                    Math.Max(0f, Size.X - _lineMargins.Left - _lineMargins.Right),
                    Math.Max(0f, Size.Y - _lineMargins.Top - _lineMargins.Bottom));
            }

            private Rect2 GetContentRect()
            {
                var surface = GetSurfaceRect();
                var margins = GetSurfaceMargins();
                return new(
                    surface.Position.X + margins.Left,
                    surface.Position.Y + margins.Top,
                    Math.Max(0f, surface.Size.X - margins.Left - margins.Right),
                    Math.Max(0f, surface.Size.Y - margins.Top - margins.Bottom));
            }

            private BoxEdges GetSurfaceMargins()
            {
                return new(
                    Mathf.RoundToInt(_surfaceStyle.ContentMarginLeft),
                    Mathf.RoundToInt(_surfaceStyle.ContentMarginTop),
                    Mathf.RoundToInt(_surfaceStyle.ContentMarginRight),
                    Mathf.RoundToInt(_surfaceStyle.ContentMarginBottom));
            }

            private Vector2 GetLabelMinSize()
            {
                return GetVisibleMinSize(_label);
            }

            private Vector2 GetDescriptionMinSize()
            {
                return GetVisibleMinSize(_descriptionLabel);
            }

            private static Vector2 GetVisibleMinSize(Control? control)
            {
                return control is { Visible: true } ? control.GetCombinedMinimumSize() : Vector2.Zero;
            }

            private static int ResolveRowSeparation()
            {
                return RitsuShellThemeLayoutResolver.ResolveInt("components.entryLine.layout.rowSeparation", 20);
            }

            private static int ResolveLeftColumnSeparation()
            {
                return RitsuShellThemeLayoutResolver.ResolveInt(
                    "components.entryLine.layout.leftColumnSeparation", 5);
            }

            private static int ResolveTextColumnMaxMinimumWidth()
            {
                return RitsuShellThemeLayoutResolver.ResolveInt(
                    "components.entryLine.layout.textColumnMaxMinimumWidth", 360);
            }

            private sealed class LineLayoutScope(FastSettingLine line) : IDisposable
            {
                private bool _disposed;

                public void Dispose()
                {
                    if (_disposed)
                        return;

                    _disposed = true;
                    line._layoutDeferDepth = Math.Max(0, line._layoutDeferDepth - 1);
                    if (line._layoutDeferDepth != 0 || !line._layoutDirty)
                        return;

                    line._layoutDirty = false;
                    line.RequestLayout();
                }
            }
        }

        internal sealed partial class FixedWidthScrollContent : Control
        {
            private Control? _content;
            private int _rightGutter;
            private float _viewportHeight;
            private float _viewportWidth = -1f;

            public FixedWidthScrollContent()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
                ClipContents = true;
            }

            internal void Configure(Control content, int rightGutter)
            {
                _content = content;
                _rightGutter = Math.Max(0, rightGutter);
                if (content.GetParent() != this)
                    AddChild(content);
                RequestLayout();
            }

            internal void SetViewportSize(Vector2 size)
            {
                var width = size.X > 1f ? size.X : -1f;
                var height = Math.Max(0f, size.Y);
                if (Math.Abs(_viewportWidth - width) < 0.5f &&
                    Math.Abs(_viewportHeight - height) < 0.5f)
                    return;

                _viewportWidth = width;
                _viewportHeight = height;
                RequestLayout();
            }

            public override Vector2 _GetMinimumSize()
            {
                var contentHeight = _content is { Visible: true }
                    ? _content.GetCombinedMinimumSize().Y
                    : 0f;
                return new(1f, contentHeight);
            }

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

            internal void RequestLayout()
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

                if (_content is FastVerticalStack stack)
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
                return Math.Max(1f, viewportWidth - _rightGutter);
            }
        }

        private sealed partial class FastPageHeaderBar : Control
        {
            private readonly Control? _backButton;
            private readonly Control? _description;
            private readonly Control? _title;
            private readonly Control? _trailingMenu;
            private StyleBoxFlat? _trayStyle;

            public FastPageHeaderBar(Control title, Control description, Control? backButton, Control? trailingMenu)
            {
                _title = title;
                _description = description;
                _backButton = backButton;
                _trailingMenu = trailingMenu;
                _trayStyle = CreatePageToolbarTrayStyle();
                SizeFlagsHorizontal = SizeFlags.ExpandFill;
                MouseFilter = MouseFilterEnum.Ignore;
                ClipContents = false;

                if (_backButton != null)
                    AddChild(_backButton);
                AddChild(_title);
                AddChild(_description);
                if (_trailingMenu != null)
                    AddChild(_trailingMenu);
            }

            public FastPageHeaderBar()
            {
            }

            public override void _Notification(int what)
            {
                base._Notification(what);
                switch (what)
                {
                    case (int)NotificationResized:
                        LayoutChildren();
                        break;
                    case (int)NotificationThemeChanged:
                        _trayStyle = CreatePageToolbarTrayStyle();
                        RequestLayout();
                        break;
                }
            }

            public override Vector2 _GetMinimumSize()
            {
                var margins = GetTrayMargins();
                var centerMin = GetCenterMinSize();
                var sideMin = ResolveSideSlotMinSize();
                var rowSeparation = ResolveRowSeparation();
                var backMin = GetVisibleMinSize(_backButton);
                var trailingMin = GetVisibleMinSize(_trailingMenu);
                var sideWidth = Math.Max(sideMin.X, Math.Max(backMin.X, trailingMin.X));
                return new(
                    margins.Left + margins.Right + sideWidth * 2f + rowSeparation * 2f + centerMin.X,
                    margins.Top + margins.Bottom + Math.Max(sideMin.Y,
                        Math.Max(centerMin.Y, Math.Max(backMin.Y, trailingMin.Y))));
            }

            public override void _Draw()
            {
                DrawStyleBox(_trayStyle, new(Vector2.Zero, Size));
            }

            private void RequestLayout()
            {
                UpdateMinimumSize();
                LayoutChildren();
                QueueRedraw();
                FastVerticalStack.RequestAncestorLayouts(this);
            }

            private void LayoutChildren()
            {
                if (!IsInsideTree())
                    return;

                var margins = GetTrayMargins();
                var rowSeparation = ResolveRowSeparation();
                var centerSeparation = ResolveCenterSeparation();
                var sideMin = ResolveSideSlotMinSize();
                var contentX = (float)margins.Left;
                var contentY = (float)margins.Top;
                var contentWidth = Math.Max(0f, Size.X - margins.Left - margins.Right);
                var contentHeight = Math.Max(0f, Size.Y - margins.Top - margins.Bottom);
                var sideWidth = sideMin.X;
                var titleMin = GetVisibleMinSize(_title);
                var descriptionMin = GetVisibleMinSize(_description);
                var centerHeight = titleMin.Y;
                if (descriptionMin.Y > 0f)
                    centerHeight += centerSeparation + descriptionMin.Y;

                LayoutSideControl(_backButton, new(contentX, contentY, sideWidth, contentHeight), false);
                LayoutSideControl(_trailingMenu,
                    new(contentX + contentWidth - sideWidth, contentY, sideWidth, contentHeight), true);

                var centerX = contentX + sideWidth + rowSeparation;
                var centerWidth = Math.Max(0f, contentWidth - sideWidth * 2f - rowSeparation * 2f);
                var y = contentY + Math.Max(0f, (contentHeight - centerHeight) * 0.5f);
                if (_title is { Visible: true })
                {
                    _title.Position = new(centerX, y);
                    _title.Size = new(centerWidth, titleMin.Y);
                    y += titleMin.Y;
                }

                if (_description is { Visible: false } or null)
                    return;
                y += centerSeparation;
                _description.Position = new(centerX, y);
                _description.Size = new(centerWidth, descriptionMin.Y);
            }

            private static void LayoutSideControl(Control? control, Rect2 slot, bool alignEnd)
            {
                if (control is not { Visible: true })
                    return;

                var min = control.GetCombinedMinimumSize();
                control.Position = new(
                    alignEnd ? slot.Position.X + Math.Max(0f, slot.Size.X - min.X) : slot.Position.X,
                    slot.Position.Y + Math.Max(0f, (slot.Size.Y - min.Y) * 0.5f));
                control.Size = min;
            }

            private Vector2 GetCenterMinSize()
            {
                var titleMin = GetVisibleMinSize(_title);
                var descriptionMin = GetVisibleMinSize(_description);
                var height = titleMin.Y;
                if (descriptionMin.Y > 0f)
                    height += ResolveCenterSeparation() + descriptionMin.Y;
                return new(Math.Max(titleMin.X, descriptionMin.X), height);
            }

            private BoxEdges GetTrayMargins()
            {
                if (_trayStyle != null)
                    return new(
                        Mathf.RoundToInt(_trayStyle.ContentMarginLeft),
                        Mathf.RoundToInt(_trayStyle.ContentMarginTop),
                        Mathf.RoundToInt(_trayStyle.ContentMarginRight),
                        Mathf.RoundToInt(_trayStyle.ContentMarginBottom));
                return default;
            }

            private static Vector2 GetVisibleMinSize(Control? control)
            {
                return control is { Visible: true } ? control.GetCombinedMinimumSize() : Vector2.Zero;
            }

            private static Vector2 ResolveSideSlotMinSize()
            {
                return RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.pageToolbar.layout.sideSlot.minSize",
                    new(104f, 44f));
            }

            private static int ResolveRowSeparation()
            {
                return RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbar.layout.rowSeparation", 10);
            }

            private static int ResolveCenterSeparation()
            {
                return RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbar.layout.centerSeparation", 5);
            }
        }
    }
}
