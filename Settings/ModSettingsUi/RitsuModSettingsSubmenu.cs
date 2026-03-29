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
    ///     Full-screen mod settings browser: sidebar (mods, pages, sections), content pane, deferred save, and locale
    ///     refresh.
    /// </summary>
    public partial class RitsuModSettingsSubmenu : NSubmenu
    {
        private const float SidebarWidth = 324f;
        private const double AutosaveDelaySeconds = 0.35;
        private const int ScrollContentRightGutter = 12;

        private static readonly StringName PaneSidebarHotkey = MegaInput.viewDeckAndTabLeft;
        private static readonly StringName PaneContentHotkey = MegaInput.viewExhaustPileAndTabRight;

        private readonly List<Control> _contentFocusChain = [];

        private readonly HashSet<IModSettingsBinding> _dirtyBindings = [];
        private readonly HashSet<string> _expandedModIds = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _modButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _pageButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<Action> _refreshActions = [];

        private readonly Dictionary<string, ModSettingsSidebarButton> _sectionButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<Control> _sidebarFocusChain = [];

        private VBoxContainer _contentList = null!;

        private bool _contentOnlyRebuildNeedsContentFocus;
        private Control _contentPanelRoot = null!;
        private bool _focusNavigationRefreshScheduled;
        private bool _focusSelectedPageButtonOnNextRefresh;
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
        private MegaRichTextLabel _subtitleLabel;
        private bool _suppressScrollSync;
        private MegaRichTextLabel _titleLabel;
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

            var header = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            header.AddThemeConstantOverride("separation", 6);
            root.AddChild(header);

            _titleLabel = CreateTitleLabel(32, HorizontalAlignment.Left);
            _titleLabel.CustomMinimumSize = new(0f, 42f);
            _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(_titleLabel);

            _subtitleLabel = CreateTitleLabel(16, HorizontalAlignment.Left);
            _subtitleLabel.CustomMinimumSize = new(0f, 24f);
            _subtitleLabel.Modulate = new(0.82f, 0.79f, 0.72f, 0.92f);
            _subtitleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(_subtitleLabel);

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
            _focusSelectedPageButtonOnNextRefresh = true;
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
                Callable.From(() =>
                {
                    if (_sectionButtons.TryGetValue(sectionId, out var btn) && btn.IsVisibleInTree())
                        btn.GrabFocus();
                }).CallDeferred();
                return;
            }

            var pageChanged = !string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase);
            _selectedPageId = pageId;
            _selectedSectionId = sectionId;
            ModSettingsBaseLibReflectionMirror.TryRegisterMirroredPages();
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
            if (!string.IsNullOrWhiteSpace(_selectedSectionId)
                && _sectionButtons.TryGetValue(_selectedSectionId, out var sectionBtn)
                && sectionBtn.IsVisibleInTree())
                return sectionBtn;

            if (!string.IsNullOrWhiteSpace(_selectedPageId)
                && _pageButtons.TryGetValue(_selectedPageId, out var pageBtn)
                && pageBtn.IsVisibleInTree())
                return pageBtn;

            if (!string.IsNullOrWhiteSpace(_selectedModId)
                && _modButtons.TryGetValue(_selectedModId, out var modBtn)
                && modBtn.IsVisibleInTree())
                return modBtn;

            return _sidebarFocusChain.FirstOrDefault();
        }

        private Control? ResolveInitialSidebarFocus()
        {
            if (_focusSelectedPageButtonOnNextRefresh)
            {
                _focusSelectedPageButtonOnNextRefresh = false;
                if (!string.IsNullOrWhiteSpace(_selectedPageId)
                    && _pageButtons.TryGetValue(_selectedPageId, out var pageButton)
                    && pageButton.Visible)
                    return pageButton;

                if (!string.IsNullOrWhiteSpace(_selectedModId)
                    && _modButtons.TryGetValue(_selectedModId, out var modButton)
                    && modButton.Visible)
                    return modButton;
            }

            if (!string.IsNullOrWhiteSpace(_selectedSectionId)
                && _sectionButtons.TryGetValue(_selectedSectionId, out var sectionBtn)
                && sectionBtn.IsVisibleInTree())
                return sectionBtn;

            if (!string.IsNullOrWhiteSpace(_selectedPageId)
                && _pageButtons.TryGetValue(_selectedPageId, out var pb)
                && pb.Visible)
                return pb;

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
                CustomMinimumSize = new(SidebarWidth, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _sidebarPanelRoot = panel;
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new(0.10f, 0.115f, 0.145f, 0.96f)));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 16);
            frame.AddThemeConstantOverride("margin_top", 16);
            frame.AddThemeConstantOverride("margin_right", 16);
            frame.AddThemeConstantOverride("margin_bottom", 16);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 14);
            frame.AddChild(root);

            var headerCard = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            headerCard.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            root.AddChild(headerCard);

            var headerBox = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerBox.AddThemeConstantOverride("separation", 4);
            headerCard.AddChild(headerBox);

            var headerTitle =
                ModSettingsUiFactory.CreateSectionTitle(ModSettingsLocalization.Get("sidebar.title", "Mods"));
            headerTitle.CustomMinimumSize = new(0f, 30f);
            headerBox.AddChild(headerTitle);

            headerBox.AddChild(ModSettingsUiFactory.CreateInlineDescription(
                ModSettingsLocalization.Get("sidebar.subtitle", "Browse mods, pages, and sections.")));

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                FollowFocus = false,
                FocusMode = FocusModeEnum.None,
            };
            _sidebarScrollContainer = scroll;
            root.AddChild(scroll);

            var sidebarScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sidebarScrollFrame.AddThemeConstantOverride("margin_right", ScrollContentRightGutter);
            scroll.AddChild(sidebarScrollFrame);

            _modButtonList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _modButtonList.AddThemeConstantOverride("separation", 12);
            sidebarScrollFrame.AddChild(_modButtonList);
            return panel;
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
            panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new(0.08f, 0.095f, 0.125f, 0.98f)));

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
            ApplyStaticTexts();
            RebuildSidebar();
            RebuildContent(true);
        }

        private void RebuildSidebar()
        {
            _modButtonList.FreeChildren();
            _modButtons.Clear();
            _pageButtons.Clear();
            _sectionButtons.Clear();

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

                var section = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                section.AddThemeConstantOverride("separation", 8);

                var card = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                card.AddThemeStyleboxOverride("panel", CreateSidebarGroupStyle(
                    string.Equals(modId, _selectedModId, StringComparison.OrdinalIgnoreCase)));
                section.AddChild(card);

                var cardContent = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                cardContent.AddThemeConstantOverride("separation", 8);
                card.AddChild(cardContent);

                var button = ModSettingsUiFactory.CreateSidebarButton(
                    ResolveSidebarModTitle(group.ToArray()),
                    () =>
                    {
                        _selectedModId = modId;
                        _selectedPageId = pages.FirstOrDefault(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                            ?.Id;
                        _selectedSectionId = null;
                        ExpandOnlyMod(modId);
                        _focusSelectedPageButtonOnNextRefresh = true;
                        Rebuild();
                    },
                    ModSettingsSidebarItemKind.ModGroup,
                    _expandedModIds.Contains(modId) ? "▼" : "▶");
                button.Name = $"Mod_{modId}";
                cardContent.AddChild(button);

                var isExpanded = _expandedModIds.Contains(modId);
                if (isExpanded)
                {
                    var meta = ModSettingsUiFactory.CreateInlineDescription(string.Format(
                        ModSettingsLocalization.Get("sidebar.modMeta", "{0} pages"),
                        pages.Length));
                    cardContent.AddChild(meta);

                    var navStack = new VBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    };
                    navStack.AddThemeConstantOverride("separation", 6);
                    cardContent.AddChild(navStack);

                    foreach (var page in pages.Where(page => string.IsNullOrWhiteSpace(page.ParentPageId)))
                        navStack.AddChild(CreateSidebarPageTreeButton(pages, page, 1));
                }

                _modButtonList.AddChild(section);
                _modButtons[modId] = button;
            }
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

            foreach (var pair in _pageButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedPageId, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _sectionButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedSectionId, StringComparison.OrdinalIgnoreCase));

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
            RefreshFocusNavigation();
            Callable.From(ScrollToSelectedAnchor).CallDeferred();
        }

        private Control CreateSidebarPageTreeButton(IReadOnlyList<ModSettingsPage> pages, ModSettingsPage page,
            int depth)
        {
            var button = ModSettingsUiFactory.CreateSidebarButton(
                ResolvePageTabTitle(page), () =>
                {
                    var samePage = string.Equals(_selectedPageId, page.Id, StringComparison.OrdinalIgnoreCase);
                    _selectedModId = page.ModId;
                    _selectedPageId = page.Id;
                    if (!samePage)
                        _selectedSectionId = null;
                    ExpandOnlyMod(page.ModId);
                    Rebuild();
                },
                ModSettingsSidebarItemKind.Page,
                "◦",
                Math.Max(0, depth - 1));
            button.CustomMinimumSize = new(0f, 48f);
            button.SetSelected(string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
            _pageButtons[page.Id] = button;

            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 4);
            container.AddChild(button);

            if (string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))
            {
                var sectionRail = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                sectionRail.AddThemeConstantOverride("separation", 4);
                foreach (var section in page.Sections)
                {
                    var sectionButton = ModSettingsUiFactory.CreateSidebarButton(ResolveSectionTitle(section), () =>
                        {
                            _selectedModId = page.ModId;
                            NavigateToSection(page.Id, section.Id);
                        },
                        ModSettingsSidebarItemKind.Section,
                        "·",
                        depth + 1);
                    sectionButton.CustomMinimumSize = new(0f, 40f);
                    sectionButton.SetSelected(string.Equals(section.Id, _selectedSectionId,
                        StringComparison.OrdinalIgnoreCase));
                    _sectionButtons[section.Id] = sectionButton;
                    sectionRail.AddChild(sectionButton);
                }

                container.AddChild(sectionRail);
            }

            foreach (var child in pages.Where(candidate =>
                             string.Equals(candidate.ParentPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                         .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase))
                container.AddChild(CreateSidebarPageTreeButton(pages, child, depth + 1));

            return container;
        }

        private ModSettingsPage? ResolveSelectedPage()
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolvePageTabTitle(ModSettingsPage page)
        {
            return ModSettingsLocalization.ResolvePageDisplayName(page);
        }

        private static string ResolveSidebarModTitle(IReadOnlyList<ModSettingsPage> pages)
        {
            var modId = pages[0].ModId;
            return ModSettingsLocalization.ResolveModName(modId, modId);
        }

        private static string ResolveSectionTitle(ModSettingsSection section)
        {
            return section.Title?.Resolve() ?? ModSettingsLocalization.Get("section.default", "Section");
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
            foreach (var pair in _sectionButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedSectionId, StringComparison.OrdinalIgnoreCase));
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

        private void ApplyStaticTexts()
        {
            _titleLabel.SetTextAutoSize(ModSettingsLocalization.Get("entry.title", "Mod Settings (RitsuLib)"));
            _subtitleLabel.SetTextAutoSize(ModSettingsLocalization.Get("entry.subtitle",
                "Edit player-facing mod options here."));
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

        private static StyleBoxFlat CreatePanelStyle(Color bg)
        {
            return new()
            {
                BgColor = bg,
                BorderColor = new(0.44f, 0.68f, 0.80f, 0.36f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.32f),
                ShadowSize = 12,
                ContentMarginLeft = 0,
                ContentMarginTop = 0,
                ContentMarginRight = 0,
                ContentMarginBottom = 0,
            };
        }

        private static StyleBoxFlat CreateSidebarGroupStyle(bool selected)
        {
            return new()
            {
                BgColor = selected
                    ? new(0.085f, 0.125f, 0.165f, 0.97f)
                    : new Color(0.07f, 0.095f, 0.13f, 0.94f),
                BorderColor = selected
                    ? new(0.58f, 0.80f, 0.90f, 0.58f)
                    : new Color(0.30f, 0.44f, 0.54f, 0.36f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.16f),
                ShadowSize = 4,
                ContentMarginLeft = 10,
                ContentMarginTop = 10,
                ContentMarginRight = 10,
                ContentMarginBottom = 10,
            };
        }
    }
}
