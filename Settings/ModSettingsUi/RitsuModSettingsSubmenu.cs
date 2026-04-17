using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Full-screen mod settings browser: sidebar (mod list + header) and content pane.
    /// </summary>
    public partial class RitsuModSettingsSubmenu : NSubmenu
    {
        private const double AutosaveDelaySeconds = 0.35;
        private const int ScrollContentRightGutter = 12;

        private static readonly StringName PaneSidebarHotkey = MegaInput.viewDeckAndTabLeft;
        private static readonly StringName PaneContentHotkey = MegaInput.viewExhaustPileAndTabRight;

        private readonly List<Control> _contentFocusChain = [];

        private readonly HashSet<IModSettingsBinding> _dirtyBindings = [];

        private readonly List<(Control Control, Func<bool> Predicate)> _dynamicVisibilityTargets = [];
        private readonly HashSet<string> _expandedModIds = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _modButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<Action> _refreshActions = [];

        private readonly List<Control> _sidebarFocusChain = [];

        private VBoxContainer _contentList = null!;

        private bool _contentOnlyRebuildNeedsContentFocus;
        private Control _contentPanelRoot = null!;
        private bool _focusNavigationRefreshScheduled;
        private bool _focusSelectedModButtonOnNextRefresh;
        private bool _guiFocusSignalConnected;
        private Action? _hotkeyPaneContent;
        private Action? _hotkeyPaneSidebar;
        private Control? _initialFocusedControl;
        private TextureRect? _leftPaneHotkeyIcon;
        private bool _localeSubscribed;
        private VBoxContainer _modButtonList = null!;
        private Callable _modSettingsGuiFocusCallable;
        private HBoxContainer _pageTabRow = null!;
        private HBoxContainer? _paneHotkeyHintRow;
        private bool _paneHotkeySignalsConnected;
        private bool _paneHotkeysPushed;
        private AcceptDialog? _pasteErrorDialog;
        private bool _pendingRefreshFlush;
        private Timer? _refreshDebounceTimer;
        private TextureRect? _rightPaneHotkeyIcon;
        private double _saveTimer = -1;
        private ScrollContainer _scrollContainer = null!;
        private string? _selectedModId;
        private string? _selectedPageId;
        private string? _selectedSectionId;
        private Control _sidebarPanelRoot = null!;
        private ScrollContainer _sidebarScrollContainer = null!;
        private Control _sidebarModHeaderRoot = null!;
        private Panel _sidebarModPreviewFrame = null!;
        private Control _sidebarModPreviewPlaceholder = null!;
        private MegaRichTextLabel _sidebarModPreviewCaption = null!;
        private TextureRect _sidebarModIcon = null!;
        private MegaRichTextLabel _sidebarModTitleLabel = null!;
        private PanelContainer _sidebarModVersionBadgePanel = null!;
        private MegaRichTextLabel _sidebarModVersionLabel = null!;
        private MegaRichTextLabel _sidebarModMetaLabel = null!;
        private MegaRichTextLabel _sidebarModDescLabel = null!;
        private bool _suppressScrollSync;
        private Callable _updatePaneHotkeyIconsCallable;

        /// <summary>
        ///     Builds layout (header, sidebar, scrollable content) and wires initial structure.
        /// </summary>
        public RitsuModSettingsSubmenu()
        {
            AnchorRight = 1f;
            AnchorBottom = 1f;
            GrowHorizontal = GrowDirection.Both;
            GrowVertical = GrowDirection.Both;
            FocusMode = FocusModeEnum.None;

            var frame = new MarginContainer
            {
                Name = "Frame",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 160);
            frame.AddThemeConstantOverride("margin_top", 72);
            frame.AddThemeConstantOverride("margin_right", 160);
            frame.AddThemeConstantOverride("margin_bottom", 72);
            AddChild(frame);

            var root = new VBoxContainer
            {
                Name = "Root",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 18);
            frame.AddChild(root);

            root.AddChild(CreatePaneHotkeyHintRow());

            var body = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeConstantOverride("separation", 20);
            root.AddChild(body);

            body.AddChild(CreateSidebarPanel());
            body.AddChild(CreateContentPanel());
        }

        /// <inheritdoc />
        protected override Control? InitialFocusedControl => _initialFocusedControl;

        /// <inheritdoc />
        public override void _Ready()
        {
            var backButton = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/back_button"))
                .Instantiate<Control>();
            backButton.Name = "BackButton";
            AddChild(backButton);

            ConnectSignals();
            _updatePaneHotkeyIconsCallable = Callable.From(UpdatePaneHotkeyHintIcons);
            TryConnectPaneHotkeyStyleSignals();
            _scrollContainer.GetVScrollBar().ValueChanged += OnContentScrollChanged;
            SubscribeLocaleChanges();
            Rebuild();
            ProcessMode = ProcessModeEnum.Disabled;
            FocusMode = FocusModeEnum.None;
        }

        /// <inheritdoc />
        protected override void ConnectSignals()
        {
            base.ConnectSignals();
            var vp = GetViewport();
            if (vp == null)
                return;

            _modSettingsGuiFocusCallable = Callable.From<Control>(OnModSettingsGuiFocusChanged);
            vp.Connect(Viewport.SignalName.GuiFocusChanged, _modSettingsGuiFocusCallable);
            _guiFocusSignalConnected = true;
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            var vp = GetViewport();
            if (vp != null && _guiFocusSignalConnected &&
                vp.IsConnected(Viewport.SignalName.GuiFocusChanged, _modSettingsGuiFocusCallable))
            {
                vp.Disconnect(Viewport.SignalName.GuiFocusChanged, _modSettingsGuiFocusCallable);
                _guiFocusSignalConnected = false;
            }

            TryDisconnectPaneHotkeyStyleSignals();
            PopPaneHotkeys();
            base._ExitTree();
            FlushDirtyBindings();
            UnsubscribeLocaleChanges();
        }

        /// <inheritdoc />
        public override void OnSubmenuOpened()
        {
            base.OnSubmenuOpened();
            FocusMode = FocusModeEnum.None;
            FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Enabled;
            ProcessMode = ProcessModeEnum.Inherit;
            Rebuild();
        }

        /// <inheritdoc />
        public override void OnSubmenuClosed()
        {
            PopPaneHotkeys();
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            Callable.From(this.UpdateControllerNavEnabled).CallDeferred();
            base.OnSubmenuClosed();
        }

        /// <inheritdoc />
        protected override void OnSubmenuShown()
        {
            base.OnSubmenuShown();
            SetProcessInput(true);
            PushPaneHotkeys();
            UpdatePaneHotkeyHintIcons();
        }

        /// <inheritdoc />
        protected override void OnSubmenuHidden()
        {
            PopPaneHotkeys();
            FlushPendingRefreshActionsImmediate();
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            Callable.From(this.UpdateControllerNavEnabled).CallDeferred();
            base.OnSubmenuHidden();
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            base._Process(delta);
            if (_saveTimer < 0)
                return;

            _saveTimer -= delta;
            if (_saveTimer <= 0)
                FlushDirtyBindings();
        }

        internal void MarkDirty(IModSettingsBinding binding)
        {
            _dirtyBindings.Add(binding);
            _saveTimer = AutosaveDelaySeconds;
        }

        internal void RequestRefresh()
        {
            _pendingRefreshFlush = true;
            EnsureRefreshDebounceTimer();
            _refreshDebounceTimer!.Stop();
            _refreshDebounceTimer.Start();
        }

        internal void RegisterRefreshAction(Action action)
        {
            _refreshActions.Add(action);
        }

        internal void RegisterDynamicVisibility(Control control, Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(predicate);
            _dynamicVisibilityTargets.Add((control, predicate));
        }

        private void ApplyDynamicVisibilityTargets()
        {
            foreach (var (control, predicate) in _dynamicVisibilityTargets)
            {
                if (!IsInstanceValid(control))
                    continue;
                try
                {
                    control.Visible = predicate();
                }
                catch
                {
                    control.Visible = true;
                }
            }
        }

        internal void ShowPasteFailure(ModSettingsPasteFailureReason reason)
        {
            if (reason == ModSettingsPasteFailureReason.None)
                return;

            var key = reason switch
            {
                ModSettingsPasteFailureReason.ClipboardEmpty => "clipboard.pasteFailedEmpty",
                ModSettingsPasteFailureReason.PasteRuleDenied => "clipboard.pasteFailedBlocked",
                _ => "clipboard.pasteFailedIncompatible",
            };

            var fallback = reason switch
            {
                ModSettingsPasteFailureReason.ClipboardEmpty => "Clipboard is empty or unavailable.",
                ModSettingsPasteFailureReason.PasteRuleDenied => "Paste was blocked by a custom rule.",
                _ => "Clipboard contents are not compatible with this setting.",
            };

            EnsurePasteErrorDialog();
            _pasteErrorDialog!.Title =
                ModSettingsLocalization.Get("clipboard.pasteFailedTitle", "Paste failed");
            _pasteErrorDialog.OkButtonText = ModSettingsLocalization.Get("clipboard.pasteErrorOk", "OK");
            _pasteErrorDialog.DialogText = ModSettingsLocalization.Get(key, fallback);
            _pasteErrorDialog.PopupCentered();
        }

        private void EnsurePasteErrorDialog()
        {
            if (_pasteErrorDialog != null)
                return;

            _pasteErrorDialog = new() { Name = "PasteErrorDialog" };
            AddChild(_pasteErrorDialog);
        }

        private void EnsureRefreshDebounceTimer()
        {
            if (_refreshDebounceTimer != null)
                return;

            _refreshDebounceTimer = new()
            {
                Name = "ModSettingsRefreshDebounce",
                OneShot = true,
                WaitTime = 0.07,
                ProcessCallback = Timer.TimerProcessCallback.Idle,
            };
            AddChild(_refreshDebounceTimer);
            _refreshDebounceTimer.Timeout += OnRefreshDebounceTimeout;
        }

        private void OnRefreshDebounceTimeout()
        {
            if (!_pendingRefreshFlush)
                return;

            _pendingRefreshFlush = false;
            foreach (var action in _refreshActions.ToArray())
                action();
            ApplyDynamicVisibilityTargets();
        }

        private void CancelDeferredRefreshFlush()
        {
            _pendingRefreshFlush = false;
            _refreshDebounceTimer?.Stop();
        }

        private void FlushPendingRefreshActionsImmediate()
        {
            _refreshDebounceTimer?.Stop();
            if (!_pendingRefreshFlush)
                return;

            _pendingRefreshFlush = false;
            foreach (var action in _refreshActions.ToArray())
                action();
        }

        private void OnModSettingsGuiFocusChanged(Control node)
        {
            if (!Visible || !IsInstanceValid(this) || !IsInstanceValid(node))
                return;

            if (!ActiveScreenContext.Instance.IsCurrent(this))
                return;

            if (NControllerManager.Instance?.IsUsingController != true)
                return;

            if (_suppressScrollSync)
                return;

            if (_sidebarScrollContainer.IsAncestorOf(node))
                _sidebarScrollContainer.EnsureControlVisible(node);
            else if (_scrollContainer.IsAncestorOf(node))
                _scrollContainer.EnsureControlVisible(node);
        }

        /// <summary>
        ///     Selects a mod in the sidebar, optionally opening <paramref name="pageId" />, and rebuilds the UI.
        /// </summary>
        public void SelectMod(string modId, string? pageId = null)
        {
            _selectedModId = modId;
            _selectedPageId = pageId;
            _selectedSectionId = null;
            ExpandOnlyMod(modId);
            _focusSelectedModButtonOnNextRefresh = true;
            Rebuild();
        }

        /// <summary>
        ///     Switches to <paramref name="pageId" /> within the currently selected mod.
        /// </summary>
        public void NavigateToPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            _selectedPageId = pageId;
            _selectedSectionId = null;
            ModSettingsBaseLibReflectionMirror.TryRegisterMirroredPages();
            ModSettingsModConfigReflectionMirror.TryRegisterMirroredPages();
            Rebuild();
        }

        /// <summary>
        ///     Opens <paramref name="pageId" /> and scrolls/focuses <paramref name="sectionId" />.
        /// </summary>
        public void NavigateToSection(string pageId, string sectionId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            if (string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_selectedSectionId, sectionId, StringComparison.OrdinalIgnoreCase))
            {
                Callable.From(ScrollToSelectedAnchor).CallDeferred();
                RefreshFocusNavigation();
                return;
            }

            var pageChanged = !string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase);
            _selectedPageId = pageId;
            _selectedSectionId = sectionId;
            ModSettingsBaseLibReflectionMirror.TryRegisterMirroredPages();
            ModSettingsModConfigReflectionMirror.TryRegisterMirroredPages();
            if (pageChanged)
                Rebuild();
            else
                RebuildContent();
        }

        private Control CreatePaneHotkeyHintRow()
        {
            var row = new HBoxContainer
            {
                Name = "PaneHotkeyHints",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };
            _paneHotkeyHintRow = row;

            _leftPaneHotkeyIcon = new()
            {
                CustomMinimumSize = new(44f, 32f),
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(_leftPaneHotkeyIcon);

            row.AddChild(new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            });

            _rightPaneHotkeyIcon = new()
            {
                CustomMinimumSize = new(44f, 32f),
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(_rightPaneHotkeyIcon);

            return row;
        }

        private void TryConnectPaneHotkeyStyleSignals()
        {
            if (_paneHotkeySignalsConnected)
                return;

            if (NControllerManager.Instance != null)
            {
                NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected,
                    _updatePaneHotkeyIconsCallable);
                NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected,
                    _updatePaneHotkeyIconsCallable);
            }

            if (NInputManager.Instance != null)
                NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, _updatePaneHotkeyIconsCallable);

            _paneHotkeySignalsConnected = true;
        }

        private void TryDisconnectPaneHotkeyStyleSignals()
        {
            if (!_paneHotkeySignalsConnected)
                return;

            if (NControllerManager.Instance != null)
            {
                NControllerManager.Instance.Disconnect(NControllerManager.SignalName.MouseDetected,
                    _updatePaneHotkeyIconsCallable);
                NControllerManager.Instance.Disconnect(NControllerManager.SignalName.ControllerDetected,
                    _updatePaneHotkeyIconsCallable);
            }

            if (NInputManager.Instance != null)
                NInputManager.Instance.Disconnect(NInputManager.SignalName.InputRebound,
                    _updatePaneHotkeyIconsCallable);

            _paneHotkeySignalsConnected = false;
        }

        private void UpdatePaneHotkeyHintIcons()
        {
            if (_paneHotkeyHintRow == null)
                return;

            var usingController = NControllerManager.Instance?.IsUsingController ?? false;
            _paneHotkeyHintRow.Visible = usingController && Visible;
            if (!usingController)
                return;

            if (NInputManager.Instance == null)
                return;

            _leftPaneHotkeyIcon?.Texture = NInputManager.Instance.GetHotkeyIcon(PaneSidebarHotkey);
            _rightPaneHotkeyIcon?.Texture = NInputManager.Instance.GetHotkeyIcon(PaneContentHotkey);
        }

        private void PushPaneHotkeys()
        {
            if (_paneHotkeysPushed || NHotkeyManager.Instance == null)
                return;

            _hotkeyPaneSidebar = OnHotkeyPressedFocusSidebar;
            _hotkeyPaneContent = OnHotkeyPressedFocusContent;
            NHotkeyManager.Instance.PushHotkeyPressedBinding(PaneSidebarHotkey, _hotkeyPaneSidebar);
            NHotkeyManager.Instance.PushHotkeyPressedBinding(PaneContentHotkey, _hotkeyPaneContent);
            _paneHotkeysPushed = true;
        }

        private void PopPaneHotkeys()
        {
            if (!_paneHotkeysPushed || NHotkeyManager.Instance == null)
                return;

            if (_hotkeyPaneSidebar != null)
                NHotkeyManager.Instance.RemoveHotkeyPressedBinding(PaneSidebarHotkey, _hotkeyPaneSidebar);
            if (_hotkeyPaneContent != null)
                NHotkeyManager.Instance.RemoveHotkeyPressedBinding(PaneContentHotkey, _hotkeyPaneContent);

            _hotkeyPaneSidebar = null;
            _hotkeyPaneContent = null;
            _paneHotkeysPushed = false;
        }

        private void OnHotkeyPressedFocusSidebar()
        {
            if (!Visible || !IsInstanceValid(this) || !ActiveScreenContext.Instance.IsCurrent(this))
                return;

            FocusSidebarPaneFromInput();
        }

        private void OnHotkeyPressedFocusContent()
        {
            if (!Visible || !IsInstanceValid(this) || !ActiveScreenContext.Instance.IsCurrent(this))
                return;

            FocusContentPaneFromInput();
        }

        private static bool IsFocusUnderPopupOrTransientWindow(Control? c)
        {
            for (Node? n = c; n != null; n = n.GetParent())
                switch (n)
                {
                    case PopupMenu:
                    case Window { Visible: true, PopupWindow: true }:
                        return true;
                }

            return false;
        }

        private void FocusContentPaneFromInput()
        {
            if (!IsInstanceValid(this) || !Visible || !ActiveScreenContext.Instance.IsCurrent(this))
                return;

            var fo = GetViewport()?.GuiGetFocusOwner();
            if (IsFocusUnderPopupOrTransientWindow(fo))
                return;

            if (fo != null && IsInstanceValid(fo) && _contentPanelRoot.IsAncestorOf(fo))
                return;

            RebuildFocusChainsOnly();
            GrabControlDeferred(ResolveContentFocusFirstInContentPanel());
        }

        private Control? ResolveContentFocusFirstInContentPanel()
        {
            return _contentFocusChain.FirstOrDefault();
        }

        private Control? ResolveContentFocusTargetForSection()
        {
            if (_contentFocusChain.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                if (_contentList.FindChild($"Section_{_selectedSectionId}", true, false) is Control anchor)
                    foreach (var c in _contentFocusChain.Where(UnderScrollBody)
                                 .Where(c => anchor == c || anchor.IsAncestorOf(c)))
                        return c;

            foreach (var c in _contentFocusChain.Where(UnderScrollBody))
                return c;

            return _contentFocusChain.FirstOrDefault();

            bool UnderScrollBody(Control c)
            {
                return _contentList.IsAncestorOf(c);
            }
        }

        private void FocusSidebarPaneFromInput()
        {
            if (!IsInstanceValid(this) || !Visible || !ActiveScreenContext.Instance.IsCurrent(this))
                return;

            var fo = GetViewport()?.GuiGetFocusOwner();
            if (IsFocusUnderPopupOrTransientWindow(fo))
                return;

            if (fo != null && IsInstanceValid(fo) && _sidebarPanelRoot.IsAncestorOf(fo))
                return;

            RebuildFocusChainsOnly();
            GrabControlDeferred(ResolveSidebarTargetMatchingContent());
        }

        private Control? ResolveSidebarTargetMatchingContent()
        {
            if (!string.IsNullOrWhiteSpace(_selectedModId)
                && _modButtons.TryGetValue(_selectedModId, out var modBtn)
                && modBtn.IsVisibleInTree())
                return modBtn;

            return _sidebarFocusChain.FirstOrDefault();
        }

        private Control? ResolveInitialSidebarFocus()
        {
            if (_focusSelectedModButtonOnNextRefresh)
            {
                _focusSelectedModButtonOnNextRefresh = false;
                if (!string.IsNullOrWhiteSpace(_selectedModId)
                    && _modButtons.TryGetValue(_selectedModId, out var modButton)
                    && modButton.Visible)
                    return modButton;
            }

            if (!string.IsNullOrWhiteSpace(_selectedModId)
                && _modButtons.TryGetValue(_selectedModId, out var mb)
                && mb.Visible)
                return mb;

            return null;
        }

        private Control CreateSidebarPanel()
        {
            var panel = new Panel
            {
                Name = "RitsuSidebarPanel",
                CustomMinimumSize = new(ModSettingsUiMetrics.SidebarPanelMinWidth, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _sidebarPanelRoot = panel;
            panel.AddThemeStyleboxOverride("panel", CreateTransparentPanelStyle());

            var mainVBox = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            // Divider sits on its own row between the info card and the scroll list (not inside the expanding list).
            mainVBox.AddThemeConstantOverride("separation", 0);
            panel.AddChild(mainVBox);

            var modHeaderOuter = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            modHeaderOuter.AddThemeConstantOverride("margin_left", ModSettingsUiMetrics.SidebarContentMarginH);
            modHeaderOuter.AddThemeConstantOverride("margin_right", ModSettingsUiMetrics.SidebarContentMarginH);
            modHeaderOuter.AddThemeConstantOverride("margin_top", 0);
            modHeaderOuter.AddThemeConstantOverride("margin_bottom", 0);

            _sidebarModHeaderRoot = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _sidebarModHeaderRoot.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateModSidebarPreviewFrameStyle());

            var headerRow = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            headerRow.AddThemeConstantOverride("separation", 12);
            _sidebarModHeaderRoot.AddChild(headerRow);

            headerRow.AddChild(CreateSidebarModPreviewFrame());

            var textCol = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            textCol.AddThemeConstantOverride("separation", 6);
            headerRow.AddChild(textCol);

            var titleRow = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            titleRow.AddThemeConstantOverride("separation", 10);

            _sidebarModTitleLabel = CreateSidebarWrapLabel(22, HorizontalAlignment.Left);
            _sidebarModTitleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _sidebarModTitleLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            titleRow.AddChild(_sidebarModTitleLabel);

            _sidebarModVersionBadgePanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            _sidebarModVersionBadgePanel.AddThemeStyleboxOverride("panel",
                ModSettingsUiFactory.CreateSidebarModVersionBadgeStyle());

            _sidebarModVersionLabel = new MegaRichTextLabel
            {
                Theme = ModSettingsUiResources.SettingsLineTheme,
                BbcodeEnabled = false,
                AutoSizeEnabled = false,
                ScrollActive = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                FocusMode = FocusModeEnum.None,
                FitContent = true,
                Modulate = new Color(0.88f, 0.86f, 0.82f),
            };
            _sidebarModVersionLabel.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonBold);
            _sidebarModVersionLabel.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            var vs = ModSettingsUiMetrics.SidebarModVersionBadgeFontSize;
            _sidebarModVersionLabel.AddThemeFontSizeOverride("normal_font_size", vs);
            _sidebarModVersionLabel.AddThemeFontSizeOverride("bold_font_size", vs);
            _sidebarModVersionLabel.AddThemeFontSizeOverride("italics_font_size", vs);
            _sidebarModVersionLabel.AddThemeFontSizeOverride("bold_italics_font_size", vs);
            _sidebarModVersionLabel.AddThemeFontSizeOverride("mono_font_size", vs);
            _sidebarModVersionLabel.MinFontSize = vs;
            _sidebarModVersionLabel.MaxFontSize = vs;
            _sidebarModVersionBadgePanel.AddChild(_sidebarModVersionLabel);
            titleRow.AddChild(_sidebarModVersionBadgePanel);

            textCol.AddChild(titleRow);

            _sidebarModMetaLabel = CreateSidebarWrapLabel(14, HorizontalAlignment.Left);
            _sidebarModMetaLabel.Modulate = new(0.75f, 0.72f, 0.65f, 0.95f);
            _sidebarModMetaLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            textCol.AddChild(_sidebarModMetaLabel);

            _sidebarModDescLabel = CreateSidebarWrapLabel(13, HorizontalAlignment.Left);
            _sidebarModDescLabel.Modulate = new(0.65f, 0.62f, 0.58f, 0.9f);
            _sidebarModDescLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _sidebarModDescLabel.Visible = false;
            textCol.AddChild(_sidebarModDescLabel);

            modHeaderOuter.AddChild(_sidebarModHeaderRoot);
            mainVBox.AddChild(modHeaderOuter);

            var dividerPad = ModSettingsUiMetrics.SidebarListDividerPadSymmetric;
            var cardToListDivider = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            cardToListDivider.AddThemeConstantOverride("margin_left", ModSettingsUiMetrics.SidebarContentMarginH);
            cardToListDivider.AddThemeConstantOverride("margin_right", ModSettingsUiMetrics.SidebarContentMarginH);
            cardToListDivider.AddThemeConstantOverride("margin_top", dividerPad);
            cardToListDivider.AddThemeConstantOverride("margin_bottom", dividerPad);
            var dividerLine = ModSettingsUiFactory.CreateSidebarScrollTopDivider();
            dividerLine.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cardToListDivider.AddChild(dividerLine);
            mainVBox.AddChild(cardToListDivider);

            var listFrame = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            listFrame.AddThemeConstantOverride("margin_left", ModSettingsUiMetrics.SidebarContentMarginH);
            listFrame.AddThemeConstantOverride("margin_top", 0);
            listFrame.AddThemeConstantOverride("margin_right", ModSettingsUiMetrics.SidebarContentMarginH);
            listFrame.AddThemeConstantOverride("margin_bottom", 16);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                FollowFocus = false,
                FocusMode = FocusModeEnum.None,
            };
            _sidebarScrollContainer = scroll;

            var scrollInner = new VBoxContainer
            {
                Name = "SidebarScrollInner",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            scrollInner.AddThemeConstantOverride("separation", 10);

            _modButtonList = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _modButtonList.AddThemeConstantOverride("separation", 8);
            scrollInner.AddChild(_modButtonList);

            scroll.AddChild(scrollInner);
            listFrame.AddChild(scroll);
            mainVBox.AddChild(listFrame);
            return panel;
        }

        private Panel CreateSidebarModPreviewFrame()
        {
            var outer = ModSettingsUiMetrics.ModSidebarPreviewOuterSize;

            _sidebarModPreviewFrame = new Panel
            {
                Name = "ModPreviewFrame",
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new(outer, outer),
                ClipContents = true,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _sidebarModPreviewFrame.AddThemeStyleboxOverride("panel",
                ModSettingsUiFactory.CreateModSidebarPreviewFrameStyle());

            var inner = new Control
            {
                Name = "ModPreviewInner",
                MouseFilter = MouseFilterEnum.Ignore,
                ClipContents = true,
            };
            inner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _sidebarModPreviewFrame.AddChild(inner);

            _sidebarModIcon = new TextureRect
            {
                Name = "ModIcon",
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            _sidebarModIcon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            inner.AddChild(_sidebarModIcon);

            _sidebarModPreviewPlaceholder = new Control
            {
                Name = "ModPreviewPlaceholder",
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = true,
            };
            _sidebarModPreviewPlaceholder.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            inner.AddChild(_sidebarModPreviewPlaceholder);

            var bg = new ColorRect
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Color = Colors.Black,
            };
            bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _sidebarModPreviewPlaceholder.AddChild(bg);

            var captionMargin = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            captionMargin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            captionMargin.AddThemeConstantOverride("margin_left", 4);
            captionMargin.AddThemeConstantOverride("margin_right", 4);
            captionMargin.AddThemeConstantOverride("margin_top", 4);
            captionMargin.AddThemeConstantOverride("margin_bottom", 4);
            _sidebarModPreviewPlaceholder.AddChild(captionMargin);

            _sidebarModPreviewCaption = CreateSidebarWrapLabel(13, HorizontalAlignment.Center, VerticalAlignment.Center);
            _sidebarModPreviewCaption.Modulate = Colors.White;
            _sidebarModPreviewCaption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _sidebarModPreviewCaption.SizeFlagsVertical = SizeFlags.ExpandFill;
            captionMargin.AddChild(_sidebarModPreviewCaption);

            return _sidebarModPreviewFrame;
        }

        private void ApplySidebarModPreviewState(Texture2D? tex, bool noModSelected)
        {
            if (!IsInstanceValid(_sidebarModPreviewPlaceholder) || !IsInstanceValid(_sidebarModIcon))
                return;

            var hasArt = tex != null && !noModSelected;
            if (hasArt)
            {
                _sidebarModIcon.Texture = tex;
                _sidebarModIcon.Visible = true;
                _sidebarModPreviewPlaceholder.Visible = false;
                _sidebarModIcon.Modulate = Colors.White;
                return;
            }

            _sidebarModIcon.Texture = null;
            _sidebarModIcon.Visible = false;
            _sidebarModPreviewPlaceholder.Visible = true;
            var caption = noModSelected
                ? ModSettingsLocalization.Get("sidebar.modPreview.empty", "No preview")
                : ModSettingsLocalization.Get("sidebar.modPreview.noImage", "No resources");
            _sidebarModPreviewCaption.SetTextAutoSize(caption);
        }

        private Control CreateContentPanel()
        {
            var panel = new Panel
            {
                Name = "RitsuContentPanel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _contentPanelRoot = panel;
            panel.AddThemeStyleboxOverride("panel", CreateTransparentPanelStyle());

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 18);
            frame.AddThemeConstantOverride("margin_top", 18);
            frame.AddThemeConstantOverride("margin_right", 18);
            frame.AddThemeConstantOverride("margin_bottom", 18);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 10);
            frame.AddChild(root);

            _pageTabRow = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _pageTabRow.AddThemeConstantOverride("separation", 8);
            root.AddChild(_pageTabRow);

            _scrollContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                FollowFocus = true,
                FocusMode = FocusModeEnum.None,
            };
            root.AddChild(_scrollContainer);

            var contentScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            contentScrollFrame.AddThemeConstantOverride("margin_right", ScrollContentRightGutter);
            _scrollContainer.AddChild(contentScrollFrame);

            _contentList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _contentList.AddThemeConstantOverride("separation", 8);
            contentScrollFrame.AddChild(_contentList);

            return panel;
        }

        private void Rebuild()
        {
            ModSettingsBaseLibReflectionMirror.TryRegisterMirroredPages();
            ModSettingsModConfigReflectionMirror.TryRegisterMirroredPages();
            RebuildSidebar();
            RebuildContent(true);
        }

        private void RebuildSidebar()
        {
            _dynamicVisibilityTargets.Clear();
            _modButtonList.FreeChildren();
            _modButtons.Clear();

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                .GroupBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => ModSettingsRegistry.GetModSidebarOrder(group.Key))
                .ThenBy(group => ModSettingsLocalization.ResolveModName(group.Key, group.Key),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rootPages.Length == 0)
            {
                _selectedModId = null;
                RefreshSelectedModHeader();
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedModId) || rootPages.All(group =>
                    !string.Equals(group.Key, _selectedModId, StringComparison.OrdinalIgnoreCase)))
                _selectedModId = rootPages[0].Key;

            ExpandOnlyMod(_selectedModId);

            foreach (var group in rootPages)
            {
                var modId = group.Key;
                var pages = ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var button = ModSettingsUiFactory.CreateSidebarButton(
                    ResolveSidebarModTitle(group.ToArray()),
                    () =>
                    {
                        _selectedModId = modId;
                        _selectedPageId = pages.FirstOrDefault(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                            ?.Id;
                        _selectedSectionId = null;
                        ExpandOnlyMod(modId);
                        _focusSelectedModButtonOnNextRefresh = true;
                        Rebuild();
                    },
                    ModSettingsSidebarItemKind.ModGroup);
                button.Name = $"Mod_{modId}";
                button.SetSelected(string.Equals(modId, _selectedModId, StringComparison.OrdinalIgnoreCase));
                _modButtonList.AddChild(button);
                _modButtons[modId] = button;
            }

            RefreshSelectedModHeader();
        }

        private void RefreshSelectedModHeader()
        {
            if (!IsInstanceValid(_sidebarModTitleLabel))
                return;

            if (string.IsNullOrWhiteSpace(_selectedModId))
            {
                _sidebarModTitleLabel.SetTextAutoSize(
                    ModSettingsLocalization.Get("sidebar.modHeader.none", "No mod"));
                if (IsInstanceValid(_sidebarModVersionBadgePanel))
                    _sidebarModVersionBadgePanel.Visible = false;
                _sidebarModMetaLabel.SetTextAutoSize("");
                _sidebarModDescLabel.SetTextAutoSize("");
                _sidebarModDescLabel.Visible = false;
                ApplySidebarModPreviewState(null, true);
                return;
            }

            var mod = ModSettingsModInfoResolver.TryFindMod(_selectedModId);
            _sidebarModTitleLabel.SetTextAutoSize(ModSettingsModInfoResolver.ResolveTitle(mod, _selectedModId));

            var ver = ModSettingsModInfoResolver.ResolveVersion(mod);
            if (IsInstanceValid(_sidebarModVersionBadgePanel) && IsInstanceValid(_sidebarModVersionLabel))
            {
                if (string.IsNullOrWhiteSpace(ver))
                {
                    _sidebarModVersionBadgePanel.Visible = false;
                }
                else
                {
                    _sidebarModVersionBadgePanel.Visible = true;
                    _sidebarModVersionLabel.SetTextAutoSize(FormatSidebarVersionBadgeText(ver));
                }
            }

            var author = ModSettingsModInfoResolver.ResolveAuthor(mod);
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(author))
                metaParts.Add(author);
            metaParts.Add(_selectedModId);
            _sidebarModMetaLabel.SetTextAutoSize(string.Join(" · ", metaParts));

            var desc = ModSettingsModInfoResolver.ResolveDescription(mod);
            if (string.IsNullOrWhiteSpace(desc))
            {
                _sidebarModDescLabel.Visible = false;
                _sidebarModDescLabel.SetTextAutoSize("");
            }
            else
            {
                _sidebarModDescLabel.Visible = true;
                _sidebarModDescLabel.SetTextAutoSize(desc);
            }

            var tex = ModSettingsModInfoResolver.TryLoadModIcon(mod, _selectedModId);
            ApplySidebarModPreviewState(tex, false);
        }

        private void RebuildContent(bool fromFullRebuild = false)
        {
            CancelDeferredRefreshFlush();
            _contentOnlyRebuildNeedsContentFocus = !fromFullRebuild;
            _pageTabRow.FreeChildren();
            _pageTabRow.Visible = false;
            _contentList.FreeChildren();
            _refreshActions.Clear();

            foreach (var pair in _modButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedModId, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(_selectedModId))
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.none",
                    "No mod settings pages are currently registered.")));
                RefreshFocusNavigation();
                return;
            }

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                               string.IsNullOrWhiteSpace(page.ParentPageId))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rootPages.Length == 0)
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.mod",
                    "This mod does not currently expose a settings page.")));
                RefreshFocusNavigation();
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedPageId) ||
                (rootPages.All(page => !string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase)) &&
                 ModSettingsRegistry.GetPages().All(page =>
                     !string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))))
                _selectedPageId = rootPages[0].Id;

            var pageToRender = ResolveSelectedPage();
            if (pageToRender == null)
            {
                _contentList.AddChild(CreateEmptyStateLabel(ModSettingsLocalization.Get("empty.page",
                    "The selected settings page could not be found.")));
                RefreshFocusNavigation();
                return;
            }

            var context = new ModSettingsUiContext(this);
            var isChildPage = !string.IsNullOrWhiteSpace(pageToRender.ParentPageId);
            Action onBack = isChildPage
                ? () =>
                {
                    _selectedPageId = pageToRender.ParentPageId!;
                    RebuildContent();
                }
                : static () => { };

            _pageTabRow.Visible = true;
            var pageHeader = ModSettingsUiFactory.CreateModSettingsPageHeaderBar(context, pageToRender, isChildPage,
                onBack);
            pageHeader.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _pageTabRow.AddChild(pageHeader);

            _contentList.AddChild(ModSettingsUiFactory.CreatePageContent(context, pageToRender));
            ApplyDynamicVisibilityTargets();
            RefreshFocusNavigation();
            Callable.From(ScrollToSelectedAnchor).CallDeferred();
        }

        private ModSettingsPage? ResolveSelectedPage()
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolveSidebarModTitle(IReadOnlyList<ModSettingsPage> pages)
        {
            var modId = pages[0].ModId;
            return ModSettingsLocalization.ResolveModName(modId, modId);
        }

        private void ScrollToSelectedAnchor()
        {
            _suppressScrollSync = true;
            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                if (_contentList.FindChild($"Section_{_selectedSectionId}", true, false) is Control target)
                {
                    _scrollContainer.ScrollVertical = Mathf.RoundToInt(target.GlobalPosition.Y -
                        _scrollContainer.GlobalPosition.Y + _scrollContainer.ScrollVertical - 12f);
                    Callable.From(() => _suppressScrollSync = false).CallDeferred();
                    return;
                }

            _scrollContainer.ScrollVertical = 0;
            Callable.From(() => _suppressScrollSync = false).CallDeferred();
        }

        private void OnContentScrollChanged(double value)
        {
            if (_suppressScrollSync)
                return;

            var page = ResolveSelectedPage();
            if (page == null || page.Sections.Count == 0)
                return;

            var viewportTop = _scrollContainer.GlobalPosition.Y + 24f;
            var bestSectionId = page.Sections[0].Id;
            var bestDistance = float.MaxValue;

            foreach (var section in page.Sections)
            {
                if (_contentList.FindChild($"Section_{section.Id}", true, false) is not Control target)
                    continue;

                var distance = MathF.Abs(target.GlobalPosition.Y - viewportTop);
                if (!(distance < bestDistance)) continue;
                bestDistance = distance;
                bestSectionId = section.Id;
            }

            if (string.Equals(bestSectionId, _selectedSectionId, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedSectionId = bestSectionId;
        }

        private void RefreshFocusNavigation()
        {
            if (_focusNavigationRefreshScheduled)
                return;
            _focusNavigationRefreshScheduled = true;
            Callable.From(FlushFocusNavigationDeferred).CallDeferred();
        }

        private void FlushFocusNavigationDeferred()
        {
            _focusNavigationRefreshScheduled = false;
            if (!IsInstanceValid(this) || !Visible)
                return;

            ApplySplitPaneFocusNavigation();
            this.UpdateControllerNavEnabled();
        }

        private void RebuildFocusChainsOnly()
        {
            _sidebarFocusChain.Clear();
            _contentFocusChain.Clear();
            CollectSettingsFocusChainPreorder(_sidebarPanelRoot, _sidebarFocusChain);
            CollectSettingsFocusChainPreorder(_contentPanelRoot, _contentFocusChain);

            WireVerticalOnlyChain(_sidebarFocusChain);
            WireVerticalOnlyChain(_contentFocusChain);

            _initialFocusedControl = ResolveInitialSidebarFocus() ?? _sidebarFocusChain.FirstOrDefault();

            UpdatePaneHotkeyHintIcons();
        }

        private void ApplySplitPaneFocusNavigation()
        {
            RebuildFocusChainsOnly();
            var owner = GetViewport()?.GuiGetFocusOwner();
            switch (_contentOnlyRebuildNeedsContentFocus)
            {
                case false when
                    IsInstanceValid(owner) && IsAncestorOf(owner):
                    return;
                case true:
                {
                    _contentOnlyRebuildNeedsContentFocus = false;
                    var contentTarget = ResolveContentFocusTargetForSection();
                    if (contentTarget != null && contentTarget.IsVisibleInTree())
                    {
                        GrabControlDeferred(contentTarget);
                        return;
                    }

                    break;
                }
            }

            if (IsFocusUnderPopupOrTransientWindow(owner))
                return;

            var focusLost = owner == null || !IsInstanceValid(owner) || !IsAncestorOf(owner);
            if (focusLost)
                GrabControlDeferred(_initialFocusedControl);
            else
                _initialFocusedControl?.TryGrabFocus();
        }

        private static void GrabControlDeferred(Control? target)
        {
            if (target == null)
                return;

            var t = target;
            Callable.From(() =>
            {
                if (!IsInstanceValid(t) || !t.IsVisibleInTree())
                    return;

                t.GrabFocus();
            }).CallDeferred();
        }

        private static void WireVerticalOnlyChain(IReadOnlyList<Control> chain)
        {
            for (var index = 0; index < chain.Count; index++)
            {
                var current = chain[index];
                var selfPath = current.GetPath();
                current.FocusNeighborLeft = selfPath;
                current.FocusNeighborRight = selfPath;
                current.FocusNeighborTop = index > 0 ? chain[index - 1].GetPath() : null;
                current.FocusNeighborBottom =
                    index < chain.Count - 1 ? chain[index + 1].GetPath() : null;
            }
        }

        private static void CollectSettingsFocusChainPreorder(Control parent, List<Control> controls)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is not Control item || !item.IsVisibleInTree())
                    continue;

                if (IsSettingsFocusTerminal(item))
                {
                    if (item.FocusMode == FocusModeEnum.All)
                        controls.Add(item);
                    continue;
                }

                CollectSettingsFocusChainPreorder(item, controls);
            }
        }

        private static bool IsSettingsFocusTerminal(Control c)
        {
            return c switch
            {
                ModSettingsSidebarButton or ModSettingsTextButton or ModSettingsCollapsibleHeaderButton
                    or ModSettingsToggleControl or ModSettingsMiniButton or ModSettingsDragHandle
                    or ModSettingsActionsButton or NButton
                    or HSlider or OptionButton or ColorPickerButton or MenuButton => true,
                LineEdit or TextEdit => c.FocusMode == FocusModeEnum.All,
                _ => c is Button,
            };
        }

        private void ExpandOnlyMod(string? modId)
        {
            _expandedModIds.Clear();
            if (!string.IsNullOrWhiteSpace(modId))
                _expandedModIds.Add(modId);
        }

        private void FlushDirtyBindings()
        {
            if (_dirtyBindings.Count == 0)
            {
                _saveTimer = -1;
                return;
            }

            foreach (var binding in _dirtyBindings.ToArray())
                try
                {
                    binding.Save();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to save '{binding.ModId}:{binding.DataKey}': {ex.Message}");
                }

            _dirtyBindings.Clear();
            _saveTimer = -1;
        }

        private void SubscribeLocaleChanges()
        {
            if (_localeSubscribed)
                return;

            try
            {
                LocManager.Instance.SubscribeToLocaleChange(OnLocaleChanged);
                _localeSubscribed = true;
            }
            catch
            {
                // ignored
            }
        }

        private void UnsubscribeLocaleChanges()
        {
            if (!_localeSubscribed)
                return;

            try
            {
                LocManager.Instance.UnsubscribeToLocaleChange(OnLocaleChanged);
            }
            catch
            {
                // ignored
            }

            _localeSubscribed = false;
        }

        private void OnLocaleChanged()
        {
            FlushDirtyBindings();
            Callable.From(Rebuild).CallDeferred();
        }

        private static string FormatSidebarVersionBadgeText(string raw)
        {
            var t = raw.Trim();
            if (t.Length == 0)
                return string.Empty;
            if (t.StartsWith('v') || t.StartsWith('V'))
                t = t[1..].TrimStart();
            return $"V{t}".ToUpperInvariant();
        }

        /// <summary>
        ///     Sidebar mod title / meta / description: bounded width, word wrap, height grows with content.
        /// </summary>
        private static MegaRichTextLabel CreateSidebarWrapLabel(int fontSize, HorizontalAlignment alignment,
            VerticalAlignment verticalAlignment = VerticalAlignment.Top)
        {
            var label = new MegaRichTextLabel
            {
                Theme = ModSettingsUiResources.SettingsLineTheme,
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                ScrollActive = false,
                HorizontalAlignment = alignment,
                VerticalAlignment = verticalAlignment,
                MouseFilter = MouseFilterEnum.Ignore,
                FocusMode = FocusModeEnum.None,
                IsHorizontallyBound = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Min(fontSize, 16);
            label.MaxFontSize = fontSize;
            return label;
        }

        private static MegaRichTextLabel CreateTitleLabel(int fontSize, HorizontalAlignment alignment)
        {
            var label = new MegaRichTextLabel
            {
                Theme = ModSettingsUiResources.SettingsLineTheme,
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                ScrollActive = false,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                FocusMode = FocusModeEnum.None,
            };

            label.AddThemeFontOverride("normal_font", ModSettingsUiResources.KreonRegular);
            label.AddThemeFontOverride("bold_font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Min(fontSize, 16);
            label.MaxFontSize = fontSize;
            return label;
        }

        private static MegaRichTextLabel CreateEmptyStateLabel(string text)
        {
            var label = CreateTitleLabel(24, HorizontalAlignment.Center);
            label.CustomMinimumSize = new(0f, 120f);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.SetTextAutoSize(text);
            return label;
        }

        private static StyleBoxFlat CreateTransparentPanelStyle()
        {
            return new()
            {
                BgColor = Colors.Transparent,
                BorderColor = Colors.Transparent,
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 0,
                BorderWidthBottom = 0,
                ShadowSize = 0,
                ContentMarginLeft = 0,
                ContentMarginTop = 0,
                ContentMarginRight = 0,
                ContentMarginBottom = 0,
            };
        }

    }
}
