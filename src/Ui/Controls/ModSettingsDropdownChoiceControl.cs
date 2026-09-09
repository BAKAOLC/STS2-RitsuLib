using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Platform;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A virtualized dropdown editor for choosing among labeled values.</para>
    ///     <para xml:lang="zh-CN">用于从带标签值中选择一项的虚拟化下拉编辑器。</para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The stored option-value type.</para>
    ///     <para xml:lang="zh-CN">所存储选项值的类型。</para>
    /// </typeparam>
    public sealed partial class ModSettingsDropdownChoiceControl<TValue> : HBoxContainer,
        IModSettingsTransientPopupOwner, IModSettingsDirectionalInputClaimant
    {
        private const float DropListMinWidth = 200f;
        private const float RowHeight = 38f;
        private const int DropdownVirtualOverscanRows = 2;
        private const float DropdownViewportEdgeMargin = 16f;

        private readonly Action<TValue>? _onChanged;
        private readonly List<ModSettingsMiniButton> _rowButtons = [];
        private readonly List<ModSettingsMiniButton> _virtualRowPool = [];
        private int _activePoolCount;
        private Control? _backdrop;
        private float _cachedDropdownBodyH;
        private bool _dropOpen;
        private PanelContainer? _dropPanel;
        private ScrollContainer? _dropScroll;
        private float _dropdownListSeparation;
        private float _dropdownPanelMinWidth;
        private float _dropdownRowStride;
        private bool _dropdownScrollWired;
        private float _dropdownUniformRowLayoutWidth;
        private ModSettingsGamepadCompatibleButton? _faceButton;
        private Action? _opening;
        private (TValue Value, string Label)[] _optionsWithValues = [];
        private int _selectedIndex;
        private int[] _slotOptionIndex = [];
        private bool _suppressCallbacks;
        private Control? _virtualContent;

        /// <summary>
        ///     <para xml:lang="en">Creates a dropdown from labeled values and an initial selection.</para>
        ///     <para xml:lang="zh-CN">根据带标签值和初始选中值创建下拉选项编辑器。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The labeled values available to the editor.</para>
        ///     <para xml:lang="zh-CN">编辑器可用的带标签值。</para>
        /// </param>
        /// <param name="currentValue">
        ///     <para xml:lang="en">The initial value; the first option is used when no value matches.</para>
        ///     <para xml:lang="zh-CN">初始值；没有匹配项时使用第一个选项。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked after the user chooses a value.</para>
        ///     <para xml:lang="zh-CN">用户选择值后调用的回调。</para>
        /// </param>
        public ModSettingsDropdownChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = [.. options];
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dropdown.layout.entry.minSize",
                new(RitsuShellTheme.Current.Metric.Entry.ValueMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;

            _selectedIndex = 0;
            for (var i = 0; i < _optionsWithValues.Length; i++)
                if (EqualityComparer<TValue>.Default.Equals(_optionsWithValues[i].Value, currentValue))
                {
                    _selectedIndex = i;
                    break;
                }

            var face = new ModSettingsGamepadCompatibleButton
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.dropdown.layout.face.minSize",
                    new(RitsuShellTheme.Current.Metric.Entry.ValueMinWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
                Flat = false,
                Disabled = _optionsWithValues.Length == 0,
                Alignment = HorizontalAlignment.Left,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            face.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            face.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            face.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            face.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            face.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            face.AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            face.AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelPrimary));
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(face);
            face.AddThemeStyleboxOverride("disabled", RitsuShellChromeStyles.CreateSurfaceStyle());
            ModSettingsUiControlTheming.EnableAdaptiveButtonText(
                face,
                11,
                RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            face.Pressed += OnFacePressed;
            AddChild(face);
            _faceButton = face;

            RefreshFaceLabel();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured dropdown for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的下拉控件。</para>
        /// </summary>
        public ModSettingsDropdownChoiceControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _dropOpen;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            CloseDropdown();
        }

        /// <summary>
        ///     <para xml:lang="en">Occurs immediately before opening so callers can replace options on demand.</para>
        ///     <para xml:lang="zh-CN">在下拉列表展开前立即发生，调用方可按需替换选项。</para>
        /// </summary>
        public event Action? Opening
        {
            add
            {
                _opening += value;
                SyncFaceAvailability();
            }
            remove
            {
                _opening -= value;
                SyncFaceAvailability();
            }
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            ApplyFaceDropdownChrome();
            RefreshFaceLabel();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (_dropOpen)
                CloseDropdown();
            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what != NotificationThemeChanged)
                return;

            if (_dropScroll != null && IsInstanceValid(_dropScroll))
                ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_dropScroll);
            if (_dropPanel != null && IsInstanceValid(_dropPanel))
                _dropPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            if (!_dropOpen || _optionsWithValues.Length == 0)
                return;

            SyncDropdownVirtualContentWidthToShelf();
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho())
            {
                if (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack))
                {
                    CloseDropdown();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (@event.IsActionPressed("ui_up"))
                {
                    if (TryNavigateVirtualDropdownByDirection(-1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
                else if (@event.IsActionPressed("ui_down"))
                {
                    if (TryNavigateVirtualDropdownByDirection(1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
            }

            base._Input(@event);
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho())
            {
                if (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack))
                {
                    CloseDropdown();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (@event.IsActionPressed("ui_up"))
                {
                    if (TryNavigateVirtualDropdownByDirection(-1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
                else if (@event.IsActionPressed("ui_down"))
                {
                    if (TryNavigateVirtualDropdownByDirection(1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
            }

            base._UnhandledInput(@event);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects a matching option without opening the dropdown or invoking the change callback;
        ///         unmatched values leave the selection unchanged.
        ///     </para>
        ///     <para xml:lang="zh-CN">选择匹配项而不展开下拉列表或调用变更回调；值不匹配时保持当前选项不变。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to select.</para>
        ///     <para xml:lang="zh-CN">要选择的值。</para>
        /// </param>
        public void SetValue(TValue value)
        {
            if (_optionsWithValues.Length == 0 || _faceButton == null)
                return;

            var idx = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, value));
            if (idx < 0)
                return;

            _suppressCallbacks = true;
            try
            {
                _selectedIndex = idx;
                RefreshFaceLabel();
                // Keep the dropdown-only refresh grouped without returning from inside the try/finally.
                // ReSharper disable once InvertIf
                if (_dropOpen)
                {
                    SyncVirtualDropdownRows();
                    WireRowFocusNeighbors();
                }
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces every option and selects the matching value, or the first option, without invoking the
        ///         callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">替换全部选项，并在不调用回调的情况下选择匹配值；没有匹配项时选择第一个选项。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The replacement labeled values.</para>
        ///     <para xml:lang="zh-CN">用于替换的带标签值。</para>
        /// </param>
        /// <param name="selectedValue">
        ///     <para xml:lang="en">The value to select after replacement.</para>
        ///     <para xml:lang="zh-CN">替换后要选择的值。</para>
        /// </param>
        public void SetOptions(IReadOnlyList<(TValue Value, string Label)> options, TValue selectedValue)
        {
            _optionsWithValues = [.. options];
            _selectedIndex = 0;
            for (var i = 0; i < _optionsWithValues.Length; i++)
                if (EqualityComparer<TValue>.Default.Equals(_optionsWithValues[i].Value, selectedValue))
                {
                    _selectedIndex = i;
                    break;
                }

            SyncFaceAvailability();
            RefreshFaceLabel();
            if (_dropOpen)
                RebuildListRows();
        }

        private void OnFacePressed()
        {
            if (_faceButton == null)
                return;

            if (_dropOpen)
            {
                CloseDropdown();
                return;
            }

            _opening?.Invoke();
            if (_faceButton.Disabled || _optionsWithValues.Length == 0)
                return;

            OpenDropdown();
        }

        private void SyncFaceAvailability()
        {
            if (_faceButton != null)
                _faceButton.Disabled = _optionsWithValues.Length == 0 && _opening == null;
        }

        private void BuildDropdownShell()
        {
            _backdrop = new()
            {
                Name = "ChoiceDropdownBackdrop",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                TopLevel = true,
                ZIndex = 880,
            };
            _backdrop.SetAnchorsPreset(LayoutPreset.TopLeft);
            _backdrop.GuiInput += OnBackdropGuiInput;
            AddChild(_backdrop);

            _dropPanel = new()
            {
                Name = "ChoiceDropdownPanel",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                ClipContents = true,
                TopLevel = true,
                ZIndex = 881,
            };
            _dropPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            AddChild(_dropPanel);

            _dropScroll = new()
            {
                Name = "ChoiceDropdownScroll",
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                // Dropdown width should follow content, not the viewport.
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                ClipContents = true,
            };
            _dropPanel.AddChild(_dropScroll);

            _virtualContent = new()
            {
                Name = "ChoiceDropdownVirtualContent",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                ClipContents = true,
            };
            _dropScroll.AddChild(_virtualContent);
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_dropScroll);
        }

        private void OnBackdropGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                CloseDropdown();
        }

        private void OpenDropdown()
        {
            if (_optionsWithValues.Length == 0)
                return;

            EnsureDropdownShell();
            if (_dropPanel == null || _virtualContent == null || _backdrop == null)
                return;

            _dropPanel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListShellStyle());
            RebuildListRows();
            if (_activePoolCount == 0 || _dropScroll == null)
                return;

            TryWireDropdownScroll();
            _dropOpen = true;
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
            SyncVirtualDropdownRows();
            LayoutDropdownInViewport();
            _backdrop.Visible = true;
            _dropPanel.Visible = true;
            WireRowFocusNeighbors();
            Callable.From(GrabSelectedRowFocus).CallDeferred();
            Callable.From(TryFinalizeDropdownLayoutAfterScrollResolved).CallDeferred();
        }

        private void EnsureDropdownShell()
        {
            if (_backdrop != null && IsInstanceValid(_backdrop) &&
                _dropPanel != null && IsInstanceValid(_dropPanel) &&
                _dropScroll != null && IsInstanceValid(_dropScroll) &&
                _virtualContent != null && IsInstanceValid(_virtualContent))
                return;

            BuildDropdownShell();
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            _dropOpen = false;
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            TryUnwireDropdownScroll();
            if (_faceButton != null && IsInstanceValid(_faceButton))
                _faceButton.FocusNeighborBottom = null;

            if (_backdrop != null)
                _backdrop.Visible = false;
            if (_dropPanel != null)
                _dropPanel.Visible = false;

            if (_faceButton != null && IsInstanceValid(_faceButton) && _faceButton.IsVisibleInTree())
                _faceButton.GrabFocus();
        }

        private void RebuildListRows()
        {
            if (_virtualContent == null || _dropScroll == null)
                return;

            _rowButtons.Clear();
            _dropdownListSeparation =
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.listSeparation", 8);
            _dropdownRowStride = RowHeight + _dropdownListSeparation;

            var vr = GetViewport().GetVisibleRect();
            var shell = GetDropdownListShellHorizontalInset();
            var faceOuter = ComputeDropdownOuterMinWidthProvisional();

            var n = _optionsWithValues.Length;
            var totalContentH = GetTotalDropdownVirtualContentHeight(n);
            _cachedDropdownBodyH =
                Mathf.Min(totalContentH, EstimateMaxDropdownViewportListHeight(vr));

            if (n == 0)
            {
                _dropdownUniformRowLayoutWidth = 0f;
                _dropdownPanelMinWidth = faceOuter;
                var shelfEmpty = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
                _virtualContent.CustomMinimumSize = new(shelfEmpty, Mathf.Max(totalContentH, RowHeight));
                _activePoolCount = 0;
                _slotOptionIndex = [];
                HideExtraVirtualRows(0);
                if (_dropPanel != null)
                    _dropPanel.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.panel.minSize",
                        new(_dropdownPanelMinWidth, 0f));

                _dropScroll!.CustomMinimumSize = new(shelfEmpty, RowHeight);
                return;
            }

            _dropdownUniformRowLayoutWidth = ComputeUniformDropdownListRowLayoutWidth(vr, n);
            _dropdownPanelMinWidth = Mathf.Max(faceOuter, _dropdownUniformRowLayoutWidth + shell);
            var shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            if (_dropdownUniformRowLayoutWidth > shelfW + 0.5f)
            {
                _dropdownPanelMinWidth = _dropdownUniformRowLayoutWidth + shell;
                shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            }

            _virtualContent.CustomMinimumSize = new(shelfW, Mathf.Max(totalContentH, RowHeight));

            var poolSlots = Mathf.Min(n,
                Mathf.Max(
                    DropdownVirtualOverscanRows * 2 + 1,
                    Mathf.CeilToInt(_cachedDropdownBodyH / Mathf.Max(_dropdownRowStride, 0.001f))
                    + 1 + DropdownVirtualOverscanRows * 2));

            EnsureVirtualDropdownRows(poolSlots);
            _slotOptionIndex = new int[_virtualRowPool.Count];
            Array.Fill(_slotOptionIndex, -1);

            ApplyDropdownScrollViewportSizing(_cachedDropdownBodyH);

            if (_dropPanel != null)
                _dropPanel.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.dropdown.layout.panel.minSize",
                    new(_dropdownPanelMinWidth, 0f));

            _activePoolCount = poolSlots;

            HideExtraVirtualRows(poolSlots);
            ScrollDropdownContentToShowIndex(_selectedIndex);
        }

        private float ComputeDropdownOuterMinWidthProvisional()
        {
            return _faceButton == null
                ? DropListMinWidth
                : Mathf.Max(Mathf.Max(DropListMinWidth, _faceButton.Size.X), _faceButton.CustomMinimumSize.X);
        }

        private float GetDropdownListShellHorizontalInset()
        {
            if (_dropPanel?.GetThemeStylebox("panel") is StyleBoxFlat sb)
                return sb.ContentMarginLeft + sb.ContentMarginRight;

            var pad = RitsuShellThemeLayoutResolver.ResolveEdges("components.listShell.layout.padding", 12);
            return pad.Left + pad.Right;
        }

        private float GetDropdownInnerScrollMinWidth(float outerMinWidth)
        {
            var inset = GetDropdownListShellHorizontalInset();
            return Mathf.Max(1f, outerMinWidth - inset);
        }

        private void ApplyDropdownOuterWidthFromViewport(Rect2 vr)
        {
            if (_faceButton == null || _dropPanel == null || _dropScroll == null || _virtualContent == null)
                return;

            var gr = _faceButton.GetGlobalRect();
            var faceW = gr.Size.X > 2f ? gr.Size.X : Mathf.Max(_faceButton.Size.X, _faceButton.CustomMinimumSize.X);
            var maxOuter = Mathf.Max(DropListMinWidth, vr.Size.X - DropdownViewportEdgeMargin * 2f);
            var faceBased = Mathf.Clamp(Mathf.Max(DropListMinWidth, faceW), DropListMinWidth, maxOuter);
            var newOuter = Mathf.Min(maxOuter, Mathf.Max(faceBased, _dropdownPanelMinWidth));

            if (Mathf.IsEqualApprox(newOuter, _dropdownPanelMinWidth))
                return;

            _dropdownPanelMinWidth = newOuter;
            var shelf = GetDropdownInnerScrollMinWidth(newOuter);
            var hContent = _virtualContent.CustomMinimumSize.Y;
            _virtualContent.CustomMinimumSize = new(shelf, hContent);
            _dropScroll.CustomMinimumSize = new(shelf, _dropScroll.CustomMinimumSize.Y);
            _dropPanel.CustomMinimumSize = new(newOuter, 0f);
            if (_dropOpen)
                SyncVirtualDropdownRows();
        }

        private void SyncDropdownVirtualContentWidthToShelf()
        {
            if (_virtualContent == null)
                return;

            var shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            _virtualContent.CustomMinimumSize = new(shelfW, _virtualContent.CustomMinimumSize.Y);
        }

        private float GetDropdownRowHorizontalChromeWidth()
        {
            var pad = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.padding", 10);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.borderWidth", 1);
            return pad.Left + pad.Right + border.Left + border.Right;
        }

        private static float MeasureDropdownLabelDrawableWidth(string label, Font font, int fontSize,
            float horizontalChrome)
        {
            if (string.IsNullOrEmpty(label))
                return horizontalChrome;

            if (font is null)
                return horizontalChrome + label.Length * 8f;

            return horizontalChrome + font.GetStringSize(label, HorizontalAlignment.Left, -1f, fontSize).X;
        }

        private float ComputeMaxDropdownOptionDrawableWidth(int optionCount)
        {
            if (optionCount <= 0)
                return DropListMinWidth;

            var font = RitsuShellTheme.Current.Font.Body;
            var fontSize = RitsuShellTheme.Current.Metric.FontSize.PopupRow;
            var chrome = GetDropdownRowHorizontalChromeWidth();
            var maxW = 0f;
            for (var i = 0; i < optionCount; i++)
            {
                var label = _optionsWithValues[i].Label ?? string.Empty;
                maxW = Mathf.Max(maxW, MeasureDropdownLabelDrawableWidth(label, font, fontSize, chrome));
            }

            return Mathf.Max(maxW, DropListMinWidth);
        }

        private float ResolveDropdownListInnerMaxUniformWidth(Rect2 vr)
        {
            const float edge = DropdownViewportEdgeMargin * 2f;
            var frac = RitsuShellThemeLayoutResolver.ResolveFloat(
                "components.dropdown.layout.list.maxUniformContentWidthFraction", 0.92f);
            var shell = GetDropdownListShellHorizontalInset();
            var fromViewport = Mathf.Max(DropListMinWidth, vr.Size.X * frac - edge - shell);
            var hard = RitsuShellThemeLayoutResolver.ResolveFloat(
                "components.dropdown.layout.list.maxUniformContentWidth", 0f);
            return hard > 1f ? Mathf.Max(DropListMinWidth, Mathf.Min(fromViewport, hard)) : fromViewport;
        }

        private float ComputeUniformDropdownListRowLayoutWidth(Rect2 vr, int optionCount)
        {
            var raw = ComputeMaxDropdownOptionDrawableWidth(optionCount);
            var cap = ResolveDropdownListInnerMaxUniformWidth(vr);
            return Mathf.Max(DropListMinWidth, Mathf.Min(raw, cap));
        }

        private void TryFinalizeDropdownLayoutAfterScrollResolved()
        {
            if (!_dropOpen || _dropScroll == null || _virtualContent == null)
                return;

            _dropScroll.QueueSort();
            var inner = _dropScroll.Size.X;
            if (inner < 2f && _dropScroll.GetGlobalRect().Size.X > 2f)
                inner = _dropScroll.GetGlobalRect().Size.X;

            if (inner < 2f || _dropdownUniformRowLayoutWidth <= 0f)
                return;

            if (_dropdownUniformRowLayoutWidth <= inner + 0.5f)
                return;

            _dropdownUniformRowLayoutWidth = inner;
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
        }

        private static float RowTopOffset(int rowIndex, float rowStride)
        {
            return rowIndex * rowStride;
        }

        private float GetTotalDropdownVirtualContentHeight(int optionCount)
        {
            return optionCount == 0 ? 0f : optionCount * RowHeight + (optionCount - 1) * _dropdownListSeparation;
        }

        private static float EstimateMaxDropdownViewportListHeight(Rect2 visibleRect)
        {
            const float edge = DropdownViewportEdgeMargin * 2f;
            return Mathf.Max(RowHeight, visibleRect.Size.Y * 0.5f - edge);
        }

        private static float ClampToOrderedRange(float value, float a, float b)
        {
            var min = Mathf.Min(a, b);
            var max = Mathf.Max(a, b);
            return Mathf.Clamp(value, min, max);
        }

        private void ApplyDropdownScrollViewportSizing(float bodyH)
        {
            if (_dropScroll == null)
                return;

            var h = Mathf.Max(RowHeight, bodyH);
            var shelf = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            _dropScroll.CustomMinimumSize = new(shelf, h);
            _cachedDropdownBodyH = h;
            SyncDropdownVirtualContentWidthToShelf();
        }

        private void EnsureVirtualDropdownRows(int poolSlots)
        {
            if (_virtualContent == null)
                return;

            while (_virtualRowPool.Count < poolSlots)
            {
                var slotIndex = _virtualRowPool.Count;
                var row = new ModSettingsMiniButton(string.Empty, () => OnVirtualPoolRowActivated(slotIndex))
                {
                    Alignment = HorizontalAlignment.Left,
                };
                row.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                row.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PopupRow);
                row.SetAnchorsPreset(LayoutPreset.TopLeft);
                row.FocusEntered += () => _dropScroll?.EnsureControlVisible(row);
                _virtualContent.AddChild(row);
                _virtualRowPool.Add(row);
            }
        }

        private void HideExtraVirtualRows(int firstHiddenIndex)
        {
            for (var i = firstHiddenIndex; i < _virtualRowPool.Count; i++)
            {
                var row = _virtualRowPool[i];
                row.Visible = false;
                row.FocusMode = FocusModeEnum.None;
            }
        }

        private void OnVirtualPoolRowActivated(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotOptionIndex.Length)
                return;

            var optIndex = _slotOptionIndex[slotIndex];
            if (optIndex < 0)
                return;

            ActivateRow(optIndex);
        }

        private void TryWireDropdownScroll()
        {
            if (_dropdownScrollWired || _dropScroll == null)
                return;

            _dropScroll.GetVScrollBar().ValueChanged += OnDropdownScrollValueChanged;
            _dropdownScrollWired = true;
        }

        private void TryUnwireDropdownScroll()
        {
            if (!_dropdownScrollWired || _dropScroll == null)
                return;

            _dropScroll.GetVScrollBar().ValueChanged -= OnDropdownScrollValueChanged;
            _dropdownScrollWired = false;
        }

        private void OnDropdownScrollValueChanged(double _)
        {
            SyncVirtualDropdownRows();
        }

        private void SyncVirtualDropdownRows()
        {
            if (_virtualContent == null || _dropScroll == null || _activePoolCount == 0)
                return;

            var n = _optionsWithValues.Length;
            if (n == 0)
                return;

            var totalH = GetTotalDropdownVirtualContentHeight(n);
            var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
            var maxScroll = Mathf.Max(0f, totalH - viewH);
            var scrollFloat = (float)_dropScroll.ScrollVertical;
            var clampedScroll = ClampToOrderedRange(scrollFloat, 0f, maxScroll);
            if (!Mathf.IsEqualApprox(scrollFloat, clampedScroll))
                _dropScroll.ScrollVertical = (int)Mathf.Round(clampedScroll);

            var scrollY = (float)_dropScroll.ScrollVertical;

            var maxAnchor = Mathf.Max(0, n - _activePoolCount);
            var anchor = Mathf.Clamp(Mathf.FloorToInt(scrollY / Mathf.Max(_dropdownRowStride, 0.001f)), 0, maxAnchor);

            // Reserve scrollbar width (and separation) when visible; otherwise rows will extend under it and get clipped.
            var shelfW = _dropScroll.Size.X > 2f ? _dropScroll.Size.X : _dropScroll.GetGlobalRect().Size.X;
            var reserve = 0f;
            var bar = _dropScroll.GetVScrollBar();
            if (bar != null && IsInstanceValid(bar) && bar.Visible)
            {
                var sep = RitsuShellThemeLayoutResolver.ResolveInt(
                    "components.dropdown.layout.scroll.scrollbarVSeparation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.scrollbar.layout.scrollbarVSeparation", 0));
                reserve = Mathf.Max(0f, bar.Size.X + sep);
            }

            var usableW = Mathf.Max(0f, shelfW - reserve);

            _rowButtons.Clear();

            for (var slot = 0; slot < _activePoolCount; slot++)
            {
                var optIndex = anchor + slot;
                if (optIndex >= n)
                {
                    _slotOptionIndex[slot] = -1;
                    var hidden = _virtualRowPool[slot];
                    hidden.Visible = false;
                    hidden.FocusMode = FocusModeEnum.None;
                    continue;
                }

                _slotOptionIndex[slot] = optIndex;
                var row = _virtualRowPool[slot];
                row.Visible = true;
                row.FocusMode = FocusModeEnum.All;
                ResetDropdownVirtualRowState(row);
                ApplyDropdownVirtualRowPresentation(row, optIndex, usableW);
                var yTop = RowTopOffset(optIndex, _dropdownRowStride);
                row.Position = new(0f, yTop);
                row.TooltipText = _optionsWithValues[optIndex].Label;

                if (optIndex == _selectedIndex)
                {
                    row.TooltipText += "\n" +
                                       RitsuModuleLocalization.Get("choice.dropdown.currentRow", "Currently selected.");
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.DropdownRow);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeStyleboxOverride("normal", CreateDropdownCurrentRowNormal());
                    row.AddThemeStyleboxOverride("hover", CreateDropdownCurrentRowHover());
                    row.AddThemeStyleboxOverride("pressed", CreateDropdownCurrentRowPressed());
                    row.AddThemeStyleboxOverride("focus", CreateDropdownCurrentRowFocus());
                }
                else
                {
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeStyleboxOverride("normal", ModSettingsMiniButton.CreateStyle(false));
                    row.AddThemeStyleboxOverride("hover", ModSettingsMiniButton.CreateStyle(true));
                    row.AddThemeStyleboxOverride("pressed", ModSettingsMiniButton.CreatePressedStyle());
                    row.AddThemeStyleboxOverride("focus", ModSettingsMiniButton.CreateFocusStyle());
                }

                _rowButtons.Add(row);
            }

            HideExtraVirtualRows(_activePoolCount);
        }

        private static void ResetDropdownVirtualRowState(ModSettingsMiniButton row)
        {
            // Virtual rows are recycled; clear any leftover toggle/pressed state and theme overrides
            // so slot appearance always reflects the current bound option index.
            row.ToggleMode = false;
            row.ButtonPressed = false;

            row.RemoveThemeColorOverride("font_color");
            row.RemoveThemeColorOverride("font_hover_color");
            row.RemoveThemeColorOverride("font_pressed_color");

            row.RemoveThemeStyleboxOverride("normal");
            row.RemoveThemeStyleboxOverride("hover");
            row.RemoveThemeStyleboxOverride("pressed");
            row.RemoveThemeStyleboxOverride("focus");
        }

        private void ApplyDropdownVirtualRowPresentation(ModSettingsMiniButton row, int optIndex, float rowW)
        {
            var opt = _optionsWithValues[optIndex];
            // Make the actual selected option unambiguous vs hover/focus on recycled rows.
            row.Text = optIndex == _selectedIndex ? $"\u2713 {opt.Label}" : opt.Label;
            row.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dropdown.layout.row.minSize",
                new(rowW, RowHeight));
            row.Size = row.CustomMinimumSize;
        }

        private void ScrollDropdownContentToShowIndex(int index)
        {
            if (_dropScroll == null)
                return;

            var n = _optionsWithValues.Length;
            if (n == 0)
                return;

            index = Mathf.Clamp(index, 0, n - 1);
            var y = RowTopOffset(index, _dropdownRowStride);
            var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
            var totalH = GetTotalDropdownVirtualContentHeight(n);
            var maxScroll = Mathf.Max(0f, totalH - viewH);
            var scroll = (float)_dropScroll.ScrollVertical;
            if (y < scroll)
                scroll = y;
            else if (y + RowHeight > scroll + viewH)
                scroll = y + RowHeight - viewH;

            _dropScroll.ScrollVertical = (int)Mathf.Round(ClampToOrderedRange(scroll, 0f, maxScroll));
        }

        private void ActivateRow(int index)
        {
            if (_suppressCallbacks)
                return;

            if (index < 0 || index >= _optionsWithValues.Length)
                return;

            _selectedIndex = index;
            RefreshFaceLabel();
            InvokeOnChanged(_optionsWithValues[index].Value);
            CloseDropdown();
        }

        private void InvokeOnChanged(TValue value)
        {
            _onChanged?.Invoke(value);
        }

        private void ApplyFaceDropdownChrome()
        {
            if (_faceButton == null)
                return;

            if (SteamCompatibilityRuntime.IsProtonLaunch)
            {
                _faceButton.Icon = null;
                _faceButton.ExpandIcon = false;
                return;
            }

            var arrow = _faceButton.GetThemeIcon("arrow", "OptionButton")
                        ?? _faceButton.GetThemeIcon("select_arrow", "Tree");
            if (arrow == null)
            {
                _faceButton.Icon = null;
                _faceButton.ExpandIcon = false;
                return;
            }

            _faceButton.Icon = arrow;
            _faceButton.IconAlignment = HorizontalAlignment.Right;
            _faceButton.ExpandIcon = false;
        }

        private void RefreshFaceLabel()
        {
            if (_faceButton == null)
                return;
            if (_optionsWithValues.Length == 0)
            {
                _faceButton.Text = RitsuModuleLocalization.Get("choice.noAvailableOptions", "No available options");
                _faceButton.TooltipText = _faceButton.Text;
                return;
            }

            var i = Mathf.Clamp(_selectedIndex, 0, _optionsWithValues.Length - 1);
            var label = _optionsWithValues[i].Label;
            _faceButton.Text = _faceButton.Icon != null
                ? label
                : label + RitsuModuleLocalization.Get("choice.dropdown.chevronGap", "  ") +
                  RitsuModuleLocalization.Get("choice.dropdown.chevron", "\u25be");
            _faceButton.TooltipText = $"{RitsuModuleLocalization.Get("choice.dropdown.title", "Choose a value")}\n" +
                                      ModSettingsLocalizedFormatting.Format(
                                          RitsuModuleLocalization.Get("choice.dropdown.tooltip",
                                              "Opens a list to choose a value. Current: {0}"),
                                          label);
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(_faceButton);
        }

        private static StyleBoxFlat CreateDropdownCurrentRowNormal()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.dropdown.current.normal", BuildDropdownCurrentRowNormal);
        }

        private static StyleBoxFlat BuildDropdownCurrentRowNormal()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.borderWidth", 2);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.dropdown.layout.currentRow.cornerRadius",
                    RitsuShellTheme.Current.Metric.Radius.Default);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.bottom", 5));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Open.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateDropdownCurrentRowHover()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.dropdown.current.hover", () =>
            {
                var s = BuildDropdownCurrentRowNormal();
                s.BgColor = RitsuShellTheme.Current.Component.Dropdown.Hover.Bg;
                return s;
            });
        }

        private static StyleBoxFlat CreateDropdownCurrentRowPressed()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.dropdown.current.pressed", () =>
            {
                var s = BuildDropdownCurrentRowNormal();
                s.BgColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Bg;
                return s;
            });
        }

        private static StyleBoxFlat CreateDropdownCurrentRowFocus()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.dropdown.current.focus", BuildDropdownCurrentRowFocus);
        }

        private static StyleBoxFlat BuildDropdownCurrentRowFocus()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.borderWidthFocus", 3);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.dropdown.layout.currentRow.cornerRadius",
                    RitsuShellTheme.Current.Metric.Radius.Default);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.paddingFocus", 9);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.top", 4),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.bottom",
                    4));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private void WireRowFocusNeighbors()
        {
            if (_faceButton != null)
                _faceButton.FocusNeighborBottom =
                    _rowButtons.Count > 0 ? _rowButtons[0].GetPath() : null;

            for (var i = 0; i < _rowButtons.Count; i++)
            {
                var row = _rowButtons[i];
                row.FocusNeighborLeft = row.GetPath();
                row.FocusNeighborRight = row.GetPath();

                var optIdx = LookupVirtualPoolOptionIndex(row);
                row.FocusNeighborTop = i > 0
                    ? _rowButtons[i - 1].GetPath()
                    : optIdx == 0
                        ? _faceButton?.GetPath()
                        : null;

                row.FocusNeighborBottom =
                    i < _rowButtons.Count - 1 ? _rowButtons[i + 1].GetPath() : null;
            }
        }

        private int LookupVirtualPoolOptionIndex(ModSettingsMiniButton row)
        {
            if (_virtualRowPool.Count == 0 || _slotOptionIndex.Length == 0)
                return -1;

            var limit = Mathf.Min(Mathf.Min(_activePoolCount, _slotOptionIndex.Length), _virtualRowPool.Count);
            for (var s = 0; s < limit; s++)
                if (_virtualRowPool[s] == row)
                    return _slotOptionIndex[s];

            return -1;
        }

        private bool TryNavigateVirtualDropdownByDirection(int delta)
        {
            if (!_dropOpen || GetViewport()?.GuiGetFocusOwner() is not ModSettingsMiniButton focus)
                return false;

            var optIdx = LookupVirtualPoolOptionIndex(focus);
            if (optIdx < 0)
                return false;

            var target = optIdx + delta;
            if (target < 0 || target >= _optionsWithValues.Length)
                return false;

            // Always navigate by option index. Scroll only when needed, rather than waiting for focus to
            // reach the last visible pooled row (which makes scrolling feel "late").
            if (_dropScroll != null)
            {
                var scrollY = (float)_dropScroll.ScrollVertical;
                var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
                var y = RowTopOffset(target, _dropdownRowStride);
                var outOfView = y < scrollY || y + RowHeight > scrollY + viewH;
                if (outOfView)
                    ScrollDropdownContentToShowIndex(target);
            }

            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
            Callable.From(() => TryGrabVirtualRowForOption(target)).CallDeferred();
            return true;
        }

        private void TryGrabVirtualRowForOption(int optionIndex)
        {
            for (var s = 0; s < _activePoolCount; s++)
            {
                if (!IsInstanceValid(_virtualRowPool[s]) ||
                    !_virtualRowPool[s].Visible ||
                    s >= _slotOptionIndex.Length)
                    continue;
                if (_slotOptionIndex[s] != optionIndex)
                    continue;
                _virtualRowPool[s].GrabFocus();
                return;
            }
        }

        private void GrabSelectedRowFocus()
        {
            if (_virtualRowPool.Count == 0)
                return;

            ScrollDropdownContentToShowIndex(_selectedIndex);
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
            Callable.From(TryGrabVirtualRowForOptionDelegate).CallDeferred();
        }

        private void TryGrabVirtualRowForOptionDelegate()
        {
            TryGrabVirtualRowForOption(_selectedIndex);
        }

        private void LayoutDropdownInViewport()
        {
            if (_backdrop == null || _dropPanel == null || _faceButton == null)
                return;

            var vr = GetViewport().GetVisibleRect();
            _backdrop.GlobalPosition = vr.Position;
            _backdrop.Size = vr.Size;

            ApplyDropdownOuterWidthFromViewport(vr);

            var scrollBodyH =
                Mathf.Max(RowHeight, _dropScroll?.CustomMinimumSize.Y ?? _cachedDropdownBodyH);

            ApplyDropdownScrollViewportSizing(scrollBodyH);

            _dropPanel.ResetSize();
            _dropPanel.QueueSort();
            var measured = _dropPanel.GetCombinedMinimumSize();
            var panelW = Mathf.Max(_dropdownPanelMinWidth, measured.X);
            var panelSize = new Vector2(panelW, measured.Y);

            var gr = _faceButton.GetGlobalRect();
            var spaceBelow = vr.End.Y - gr.End.Y;
            var spaceAbove = gr.Position.Y - vr.Position.Y;
            var openAbove = spaceBelow < panelSize.Y && spaceAbove > spaceBelow;
            var desiredTopLeft = new Vector2(
                gr.Position.X,
                openAbove ? gr.Position.Y - panelSize.Y : gr.End.Y);

            var maxX = Mathf.Max(vr.Position.X, vr.End.X - panelSize.X);
            var maxY = Mathf.Max(vr.Position.Y, vr.End.Y - panelSize.Y);

            desiredTopLeft = new(
                ClampToOrderedRange(desiredTopLeft.X, vr.Position.X, maxX),
                ClampToOrderedRange(desiredTopLeft.Y, vr.Position.Y, maxY));
            _dropPanel.GlobalPosition = desiredTopLeft;
            Callable.From(TryFinalizeDropdownLayoutAfterScrollResolved).CallDeferred();
        }
    }
}
