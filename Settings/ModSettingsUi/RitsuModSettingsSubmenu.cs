using System.Text;
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
    ///     Full-screen mod settings browser: sidebar (mods, pages, sections) and content pane.
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

        private readonly List<(Control Control, Func<bool> Predicate)> _globalDynamicVisibilityTargets = [];

        private readonly List<Action> _globalRefreshActions = [];

        private readonly Dictionary<string, ModSettingsSidebarButton> _modButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, SidebarModCache> _modCaches = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _pageButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PageContentCache>
            _pageContentCaches = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PageSnapshot> _pageSnapshots = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ModSettingsSidebarButton> _sectionButtons =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<(Control Control, Func<bool> Predicate)> _sidebarDynamicVisibilityTargets = [];

        private readonly List<Control> _sidebarFocusChain = [];

        private Control _contentBuildOverlay = null!;
        private MegaRichTextLabel _contentBuildOverlayLabel = null!;
        private MegaRichTextLabel? _contentEmptyStateLabel;
        private VBoxContainer _contentList = null!;

        private bool _contentOnlyRebuildNeedsContentFocus;
        private Control _contentPanelRoot = null!;
        private bool _contentStructureDirty = true;
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
        private bool _selectionDirty = true;
        private Control _sidebarPanelRoot = null!;
        private ScrollContainer _sidebarScrollContainer = null!;
        private bool _sidebarStructureDirty = true;
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
            EnsureUiUpToDate(true, true);
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
            EnsureUiUpToDate(false, true);
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

        internal void RegisterRefreshAction(Action action, string? pageScopeId = null)
        {
            if (!string.IsNullOrWhiteSpace(pageScopeId) &&
                _pageContentCaches.TryGetValue(pageScopeId, out var pageCache))
            {
                pageCache.RefreshActions.Add(action);
                return;
            }

            _globalRefreshActions.Add(action);
        }

        internal void RegisterDynamicVisibility(Control control, Func<bool> predicate, string? pageScopeId = null)
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(predicate);
            if (!string.IsNullOrWhiteSpace(pageScopeId) &&
                _pageContentCaches.TryGetValue(pageScopeId, out var pageCache))
            {
                pageCache.VisibilityTargets.Add((control, predicate));
                return;
            }

            _globalDynamicVisibilityTargets.Add((control, predicate));
        }

        private static void ApplyDynamicVisibilityTargets(IEnumerable<(Control Control, Func<bool> Predicate)> targets)
        {
            foreach (var (control, predicate) in targets)
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
            FlushRefreshActionsImmediate();
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
            FlushRefreshActionsImmediate();
        }

        private void FlushRefreshActionsImmediate(bool includeAllPages = false)
        {
            foreach (var action in _globalRefreshActions.ToArray())
                action();

            if (includeAllPages)
                foreach (var action in _pageContentCaches.Values.SelectMany(pageCache =>
                             pageCache.RefreshActions.ToArray()))
                    action();
            else if (!string.IsNullOrWhiteSpace(_selectedPageId) && !string.IsNullOrWhiteSpace(_selectedModId) &&
                     _pageContentCaches.TryGetValue(CreatePageCacheKey(_selectedModId, _selectedPageId),
                         out var selectedPageCache))
                foreach (var action in selectedPageCache.RefreshActions.ToArray())
                    action();

            ApplyDynamicVisibilityTargets(_globalDynamicVisibilityTargets);
            ApplyDynamicVisibilityTargets(_sidebarDynamicVisibilityTargets);
            if (includeAllPages)
                foreach (var pageCache in _pageContentCaches.Values)
                    ApplyDynamicVisibilityTargets(pageCache.VisibilityTargets);
            else if (!string.IsNullOrWhiteSpace(_selectedPageId) && !string.IsNullOrWhiteSpace(_selectedModId) &&
                     _pageContentCaches.TryGetValue(CreatePageCacheKey(_selectedModId, _selectedPageId),
                         out var selectedVisibilityPage))
                ApplyDynamicVisibilityTargets(selectedVisibilityPage.VisibilityTargets);
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
            _selectionDirty = true;
            _focusSelectedPageButtonOnNextRefresh = true;
            EnsureUiUpToDate();
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
            _selectionDirty = true;
            EnsureUiUpToDate();
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
            _selectionDirty = true;
            EnsureUiUpToDate(false, pageChanged);
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

            if (_leftPaneHotkeyIcon != null)
                _leftPaneHotkeyIcon.Texture = NInputManager.Instance.GetHotkeyIcon(PaneSidebarHotkey);
            if (_rightPaneHotkeyIcon != null)
                _rightPaneHotkeyIcon.Texture = NInputManager.Instance.GetHotkeyIcon(PaneContentHotkey);
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
            var selectedSectionKey = GetSelectedSectionKey();
            if (!string.IsNullOrWhiteSpace(selectedSectionKey)
                && _sectionButtons.TryGetValue(selectedSectionKey, out var sectionBtn)
                && sectionBtn.IsVisibleInTree())
                return sectionBtn;

            var selectedPageKey = GetSelectedPageKey();
            if (!string.IsNullOrWhiteSpace(selectedPageKey)
                && _pageButtons.TryGetValue(selectedPageKey, out var pageBtn)
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
            var selectedPageKey = GetSelectedPageKey();
            var selectedSectionKey = GetSelectedSectionKey();
            if (_focusSelectedPageButtonOnNextRefresh)
            {
                _focusSelectedPageButtonOnNextRefresh = false;
                if (!string.IsNullOrWhiteSpace(selectedPageKey)
                    && _pageButtons.TryGetValue(selectedPageKey, out var pageButton)
                    && pageButton.Visible)
                    return pageButton;

                if (!string.IsNullOrWhiteSpace(_selectedModId)
                    && _modButtons.TryGetValue(_selectedModId, out var modButton)
                    && modButton.Visible)
                    return modButton;
            }

            if (!string.IsNullOrWhiteSpace(selectedSectionKey)
                && _sectionButtons.TryGetValue(selectedSectionKey, out var sectionBtn)
                && sectionBtn.IsVisibleInTree())
                return sectionBtn;

            if (!string.IsNullOrWhiteSpace(selectedPageKey)
                && _pageButtons.TryGetValue(selectedPageKey, out var pb)
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

            var contentStack = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            contentStack.AddThemeConstantOverride("separation", 0);
            _scrollContainer.AddChild(contentStack);

            var contentScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            contentScrollFrame.AddThemeConstantOverride("margin_right", ScrollContentRightGutter);
            contentStack.AddChild(contentScrollFrame);

            _contentList = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _contentList.AddThemeConstantOverride("separation", 8);
            contentScrollFrame.AddChild(_contentList);

            _contentBuildOverlay = CreateContentBuildOverlay();
            contentStack.AddChild(_contentBuildOverlay);

            return panel;
        }

        private void EnsureUiUpToDate(bool forceStructure = false, bool includeAllPagesRefresh = false)
        {
            ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
            RitsuLibModSettingsBootstrap.RefreshDynamicPages();
            ApplyStaticTexts();
            RefreshPageSnapshots();
            EnsureSelectionIsValid();

            if (forceStructure)
            {
                _sidebarStructureDirty = true;
                _contentStructureDirty = true;
            }

            if (_sidebarStructureDirty)
                RebuildSidebar();

            EnsureSelectedPageContentStructure();
            RefreshSelectionState();
            RefreshVisibleContent(includeAllPagesRefresh);
        }

        private void RefreshPageSnapshots()
        {
            var pages = ModSettingsRegistry.GetPages();
            var next = pages.ToDictionary(page => CreatePageCacheKey(page.ModId, page.Id),
                page => new PageSnapshot(page.Id, page.ModId, page.ParentPageId, CreatePageStructureSignature(page)),
                StringComparer.OrdinalIgnoreCase);
            if (_pageSnapshots.Count != next.Count || _pageSnapshots.Any(pair =>
                    !next.TryGetValue(pair.Key, out var snapshot) || snapshot != pair.Value))
            {
                _sidebarStructureDirty = true;
                _contentStructureDirty = true;
            }

            _pageSnapshots.Clear();
            foreach (var pair in next)
                _pageSnapshots[pair.Key] = pair.Value;
        }

        private void EnsureSelectionIsValid()
        {
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
                _selectedPageId = null;
                _selectedSectionId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedModId) || rootPages.All(group =>
                    !string.Equals(group.Key, _selectedModId, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedModId = rootPages[0].Key;
                _selectionDirty = true;
            }

            ExpandOnlyMod(_selectedModId);

            var modPages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var rootModPages = modPages.Where(page => string.IsNullOrWhiteSpace(page.ParentPageId)).ToArray();
            if (rootModPages.Length == 0)
            {
                _selectedPageId = null;
                _selectedSectionId = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_selectedPageId) && modPages.Any(page =>
                    string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))) return;
            _selectedPageId = rootModPages[0].Id;
            _selectedSectionId = null;
            _selectionDirty = true;
        }

        private void RebuildSidebar()
        {
            _sidebarDynamicVisibilityTargets.Clear();
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
            var liveModIds =
                new HashSet<string>(rootPages.Select(group => group.Key), StringComparer.OrdinalIgnoreCase);

            foreach (var staleModId in _modCaches.Keys.Where(modId => !liveModIds.Contains(modId)).ToArray())
            {
                if (_modCaches.TryGetValue(staleModId, out var staleCache) && IsInstanceValid(staleCache.Section))
                    staleCache.Section.QueueFree();
                _modCaches.Remove(staleModId);
                _modButtons.Remove(staleModId);
            }

            for (var index = 0; index < rootPages.Length; index++)
            {
                var group = rootPages[index];
                var modId = group.Key;
                var pages = ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (!_modCaches.TryGetValue(modId, out var cache) || !IsInstanceValid(cache.Section))
                {
                    cache = CreateSidebarModCache(modId);
                    _modCaches[modId] = cache;
                    _modButtons[modId] = cache.Button;
                }

                if (cache.Section.GetParent() != _modButtonList)
                    _modButtonList.AddChild(cache.Section);
                _modButtonList.MoveChild(cache.Section, index);

                RefreshSidebarModCache(cache, group.ToArray(), pages);
            }

            _sidebarStructureDirty = false;
            _selectionDirty = true;
        }

        private void EnsureSelectedPageContentStructure()
        {
            if (_contentStructureDirty)
            {
                var livePageKeys = new HashSet<string>(
                    ModSettingsRegistry.GetPages().Select(page => CreatePageCacheKey(page.ModId, page.Id)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var staleKey in _pageContentCaches.Keys.Where(key => !livePageKeys.Contains(key)).ToArray())
                {
                    if (_pageContentCaches.TryGetValue(staleKey, out var staleCache))
                    {
                        staleCache.BuildCancellation?.Cancel();
                        if (IsInstanceValid(staleCache.Root))
                            staleCache.Root.QueueFree();
                    }

                    _pageContentCaches.Remove(staleKey);
                }

                foreach (var cache in _pageContentCaches.Values)
                {
                    cache.BuildCancellation?.Cancel();
                    if (IsInstanceValid(cache.Root))
                        cache.Root.Visible = false;
                }

                _pageTabRow.Visible = false;
                _globalRefreshActions.Clear();
                HideTransientContentState();
                _contentStructureDirty = false;
            }

            if (string.IsNullOrWhiteSpace(_selectedPageId))
                return;

            var pageToRender = ResolveSelectedPage();
            if (pageToRender == null)
                return;

            var pageKey = CreatePageCacheKey(pageToRender.ModId, pageToRender.Id);
            if (_pageContentCaches.TryGetValue(pageKey, out var existingCache))
            {
                if (existingCache.Root.GetParent() != _contentList)
                    _contentList.AddChild(existingCache.Root);
                return;
            }

            var root = new VBoxContainer
            {
                Name = $"CachedPage_{SanitizePageNodeName(pageKey)}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };
            root.AddThemeConstantOverride("separation", 8);

            var headerHost = new VBoxContainer
            {
                Name = $"PageHeader_{SanitizePageNodeName(pageKey)}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerHost.AddThemeConstantOverride("separation", 8);

            var contentHost = ModSettingsUiFactory.CreatePageContentHost(pageToRender);
            _contentList.AddChild(root);
            root.AddChild(headerHost);
            root.AddChild(contentHost);

            _pageContentCaches[pageKey] = new()
            {
                PageId = pageToRender.Id,
                PageKey = pageKey,
                Root = root,
                HeaderHost = headerHost,
                ContentHost = contentHost,
                State = PageBuildState.NotBuilt,
                BuildVersion = 0,
            };
        }

        private void RefreshSelectionState()
        {
            var selectedPageKey = GetSelectedPageKey();
            var selectedSectionKey = GetSelectedSectionKey();

            foreach (var pair in _modButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, _selectedModId, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _pageButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, selectedPageKey, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _sectionButtons)
                pair.Value.SetSelected(string.Equals(pair.Key, selectedSectionKey, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in _modCaches)
            {
                var isSelected = string.Equals(pair.Key, _selectedModId, StringComparison.OrdinalIgnoreCase);
                var isExpanded = _expandedModIds.Contains(pair.Key);
                pair.Value.Card.AddThemeStyleboxOverride("panel", CreateSidebarGroupStyle(isSelected));
                pair.Value.MetaLabel.SetTextAutoSize(string.Format(
                    ModSettingsLocalization.Get("sidebar.modMeta", "{0} pages"),
                    ModSettingsRegistry.GetPages().Count(page =>
                        string.Equals(page.ModId, pair.Key, StringComparison.OrdinalIgnoreCase))));
                pair.Value.MetaLabel.Visible = isExpanded;
                pair.Value.NavStack.Visible = isExpanded;
            }

            _selectionDirty = false;
        }

        private void RefreshVisibleContent(bool includeAllPagesRefresh)
        {
            foreach (var cache in _pageContentCaches.Values)
                cache.Root.Visible = false;

            _pageTabRow.Visible = false;
            HideContentBuildOverlay();
            HideTransientContentState();

            if (string.IsNullOrWhiteSpace(_selectedModId))
            {
                ShowTransientContentState(ModSettingsLocalization.Get("empty.none",
                    "No mod settings pages are currently registered."));
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
                ShowTransientContentState(ModSettingsLocalization.Get("empty.mod",
                    "This mod does not currently expose a settings page."));
                RefreshFocusNavigation();
                return;
            }

            var pageToRender = ResolveSelectedPage();
            if (pageToRender == null)
            {
                ShowTransientContentState(ModSettingsLocalization.Get("empty.page",
                    "The selected settings page could not be found."));
                RefreshFocusNavigation();
                return;
            }

            var pageKey = CreatePageCacheKey(pageToRender.ModId, pageToRender.Id);
            if (!_pageContentCaches.TryGetValue(pageKey, out var selectedCache))
            {
                RefreshFocusNavigation();
                return;
            }

            selectedCache.Root.Visible = true;
            switch (selectedCache.State)
            {
                case PageBuildState.NotBuilt or PageBuildState.Failed:
                    _ = BuildPageAsync(pageToRender, selectedCache);
                    break;
                case PageBuildState.Building:
                    ShowContentBuildOverlay(ModSettingsLocalization.Get("entry.loading", "Loading settings…"));
                    break;
                default:
                    FlushRefreshActionsImmediate(includeAllPagesRefresh);
                    break;
            }

            _contentOnlyRebuildNeedsContentFocus = false;
            RefreshFocusNavigation();
            Callable.From(ScrollToSelectedAnchor).CallDeferred();
        }

        private void ShowTransientContentState(string text)
        {
            if (_contentEmptyStateLabel == null || !IsInstanceValid(_contentEmptyStateLabel))
            {
                _contentEmptyStateLabel = CreateEmptyStateLabel(text);
                _contentList.AddChild(_contentEmptyStateLabel);
            }
            else if (_contentEmptyStateLabel.GetParent() != _contentList)
            {
                _contentList.AddChild(_contentEmptyStateLabel);
            }

            _contentEmptyStateLabel.SetTextAutoSize(text);
            _contentEmptyStateLabel.Visible = true;
            foreach (var cache in _pageContentCaches.Values)
                cache.Root.Visible = false;
        }

        private void HideTransientContentState()
        {
            if (_contentEmptyStateLabel != null && IsInstanceValid(_contentEmptyStateLabel))
                _contentEmptyStateLabel.Visible = false;
        }

        private SidebarPageNodeCache CreateSidebarPageNodeCache(ModSettingsPage page, int depth)
        {
            var pageKey = CreatePageCacheKey(page.ModId, page.Id);
            var button = ModSettingsUiFactory.CreateSidebarButton(
                ResolvePageTabTitle(page), () =>
                {
                    var samePage = string.Equals(_selectedPageId, page.Id, StringComparison.OrdinalIgnoreCase);
                    _selectedModId = page.ModId;
                    _selectedPageId = page.Id;
                    if (!samePage)
                        _selectedSectionId = null;
                    ExpandOnlyMod(page.ModId);
                    _selectionDirty = true;
                    EnsureUiUpToDate();
                },
                ModSettingsSidebarItemKind.Page,
                "◦",
                Math.Max(0, depth - 1));
            button.Name = $"SidebarPage_{SanitizePageNodeName(pageKey)}";
            button.CustomMinimumSize = new(0f, 48f);

            var container = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            container.AddThemeConstantOverride("separation", 4);
            container.AddChild(button);

            var sectionRail = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };
            sectionRail.AddThemeConstantOverride("separation", 4);
            container.AddChild(sectionRail);

            var childHost = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            childHost.AddThemeConstantOverride("separation", 4);
            container.AddChild(childHost);

            return new()
            {
                PageId = page.Id,
                PageKey = pageKey,
                Depth = depth,
                Container = container,
                Button = button,
                SectionRail = sectionRail,
                ChildHost = childHost,
            };
        }

        private void ReconcileSidebarPageNode(SidebarPageNodeCache cache, IReadOnlyList<ModSettingsPage> pages,
            ModSettingsPage page, int depth)
        {
            cache.PageId = page.Id;
            cache.PageKey = CreatePageCacheKey(page.ModId, page.Id);
            cache.Depth = depth;
            cache.Button.Text = $"◦  {ResolvePageTabTitle(page)}";
            cache.Button.TooltipText = ResolvePageTabTitle(page);
            cache.Button.SetSelected(string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
            _pageButtons[cache.PageKey] = cache.Button;
            if (page.VisibleWhen != null)
                _sidebarDynamicVisibilityTargets.Add((cache.Button, page.VisibleWhen));

            var showSections = string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase);
            cache.SectionRail.Visible = showSections;
            ReconcileSidebarSectionRail(cache, page, depth, showSections);

            var childPages = pages.Where(candidate =>
                    string.Equals(candidate.ParentPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var liveChildKeys = new HashSet<string>(
                childPages.Select(child => CreatePageCacheKey(child.ModId, child.Id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var staleChildKey in cache.ChildPages.Keys.Where(key => !liveChildKeys.Contains(key)).ToArray())
            {
                if (cache.ChildPages.TryGetValue(staleChildKey, out var staleChild) &&
                    IsInstanceValid(staleChild.Container))
                    staleChild.Container.QueueFree();
                cache.ChildPages.Remove(staleChildKey);
            }

            for (var index = 0; index < childPages.Length; index++)
            {
                var child = childPages[index];
                var childKey = CreatePageCacheKey(child.ModId, child.Id);
                if (!cache.ChildPages.TryGetValue(childKey, out var childCache) ||
                    !IsInstanceValid(childCache.Container))
                {
                    childCache = CreateSidebarPageNodeCache(child, depth + 1);
                    cache.ChildPages[childKey] = childCache;
                }

                if (childCache.Container.GetParent() != cache.ChildHost)
                    cache.ChildHost.AddChild(childCache.Container);
                cache.ChildHost.MoveChild(childCache.Container, index);
                ReconcileSidebarPageNode(childCache, pages, child, depth + 1);
            }
        }

        private void ReconcileSidebarSectionRail(SidebarPageNodeCache cache, ModSettingsPage page, int depth,
            bool visible)
        {
            var liveSectionKeys = new HashSet<string>(page.Sections.Select(section =>
                CreateSectionCacheKey(page.ModId, page.Id, section.Id)), StringComparer.OrdinalIgnoreCase);

            foreach (var staleSectionKey in cache.SectionButtons.Keys.Where(key => !liveSectionKeys.Contains(key))
                         .ToArray())
            {
                if (cache.SectionButtons.TryGetValue(staleSectionKey, out var staleButton) &&
                    IsInstanceValid(staleButton))
                    staleButton.QueueFree();
                cache.SectionButtons.Remove(staleSectionKey);
            }

            for (var index = 0; index < page.Sections.Count; index++)
            {
                var section = page.Sections[index];
                var sectionKey = CreateSectionCacheKey(page.ModId, page.Id, section.Id);
                if (!cache.SectionButtons.TryGetValue(sectionKey, out var sectionButton) ||
                    !IsInstanceValid(sectionButton))
                {
                    sectionButton = ModSettingsUiFactory.CreateSidebarButton(ResolveSectionTitle(section), () =>
                        {
                            _selectedModId = page.ModId;
                            NavigateToSection(page.Id, section.Id);
                        },
                        ModSettingsSidebarItemKind.Section,
                        "·",
                        depth + 1);
                    sectionButton.Name = $"SidebarSection_{SanitizePageNodeName(sectionKey)}";
                    sectionButton.CustomMinimumSize = new(0f, 40f);
                    cache.SectionButtons[sectionKey] = sectionButton;
                }

                sectionButton.Text = $"·  {ResolveSectionTitle(section)}";
                sectionButton.TooltipText = ResolveSectionTitle(section);
                sectionButton.Visible = visible;
                sectionButton.SetSelected(string.Equals(section.Id, _selectedSectionId,
                    StringComparison.OrdinalIgnoreCase));
                _sectionButtons[sectionKey] = sectionButton;
                if (section.VisibleWhen != null)
                    _sidebarDynamicVisibilityTargets.Add((sectionButton, section.VisibleWhen));
                if (sectionButton.GetParent() != cache.SectionRail)
                    cache.SectionRail.AddChild(sectionButton);
                cache.SectionRail.MoveChild(sectionButton, index);
            }
        }

        private SidebarModCache CreateSidebarModCache(string modId)
        {
            var section = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            section.AddThemeConstantOverride("separation", 8);
            section.Name = $"SidebarModSection_{SanitizePageNodeName(modId)}";

            var card = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            section.AddChild(card);

            var cardContent = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            cardContent.AddThemeConstantOverride("separation", 8);
            card.AddChild(cardContent);

            var button = ModSettingsUiFactory.CreateSidebarButton(
                string.Empty,
                () =>
                {
                    _selectedModId = modId;
                    _selectedPageId = ModSettingsRegistry.GetPages()
                        .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase) &&
                                       string.IsNullOrWhiteSpace(page.ParentPageId))
                        .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                        .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(page => page.Id)
                        .FirstOrDefault();
                    _selectedSectionId = null;
                    ExpandOnlyMod(modId);
                    _selectionDirty = true;
                    _focusSelectedPageButtonOnNextRefresh = true;
                    EnsureUiUpToDate();
                },
                ModSettingsSidebarItemKind.ModGroup,
                "▶");
            button.Name = $"Mod_{modId}";
            cardContent.AddChild(button);

            var meta = ModSettingsUiFactory.CreateInlineDescription(string.Empty);
            cardContent.AddChild(meta);

            var navStack = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            navStack.AddThemeConstantOverride("separation", 6);
            cardContent.AddChild(navStack);

            return new()
            {
                ModId = modId,
                Section = section,
                Card = card,
                CardContent = cardContent,
                Button = button,
                MetaLabel = meta,
                NavStack = navStack,
            };
        }

        private void RefreshSidebarModCache(SidebarModCache cache, IReadOnlyList<ModSettingsPage> rootPages,
            IReadOnlyList<ModSettingsPage> pages)
        {
            var isExpanded = _expandedModIds.Contains(cache.ModId);
            cache.Button.Text = $"{(isExpanded ? "▼" : "▶")}  {ResolveSidebarModTitle(rootPages)}";
            cache.Button.TooltipText = ResolveSidebarModTitle(rootPages);
            cache.MetaLabel.SetTextAutoSize(string.Format(
                ModSettingsLocalization.Get("sidebar.modMeta", "{0} pages"),
                pages.Count));
            cache.MetaLabel.Visible = isExpanded;
            cache.NavStack.Visible = isExpanded;

            var rootChildPages = pages.Where(page => string.IsNullOrWhiteSpace(page.ParentPageId))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var livePageKeys = new HashSet<string>(
                rootChildPages.Select(page => CreatePageCacheKey(page.ModId, page.Id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var stalePageKey in cache.PageNodes.Keys.Where(key => !livePageKeys.Contains(key)).ToArray())
            {
                if (cache.PageNodes.TryGetValue(stalePageKey, out var stalePage) &&
                    IsInstanceValid(stalePage.Container))
                    stalePage.Container.QueueFree();
                cache.PageNodes.Remove(stalePageKey);
            }

            for (var index = 0; index < rootChildPages.Length; index++)
            {
                var page = rootChildPages[index];
                var pageKey = CreatePageCacheKey(page.ModId, page.Id);
                if (!cache.PageNodes.TryGetValue(pageKey, out var pageNode) || !IsInstanceValid(pageNode.Container))
                {
                    pageNode = CreateSidebarPageNodeCache(page, 1);
                    cache.PageNodes[pageKey] = pageNode;
                }

                if (pageNode.Container.GetParent() != cache.NavStack)
                    cache.NavStack.AddChild(pageNode.Container);
                cache.NavStack.MoveChild(pageNode.Container, index);
                ReconcileSidebarPageNode(pageNode, pages, page, 1);
            }
        }

        private async Task BuildPageAsync(ModSettingsPage page, PageContentCache cache)
        {
            if (cache.BuildCancellation != null)
                await cache.BuildCancellation.CancelAsync();
            cache.BuildCancellation = new();
            var ct = cache.BuildCancellation.Token;
            var buildVersion = ++cache.BuildVersion;
            cache.State = PageBuildState.Building;
            ShowContentBuildOverlay(ModSettingsLocalization.Get("entry.loading", "Loading settings…"));

            var nextHeader = new VBoxContainer
            {
                Name = $"PageHeaderBuild_{SanitizePageNodeName(cache.PageKey)}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            nextHeader.AddThemeConstantOverride("separation", 8);
            var nextContent = ModSettingsUiFactory.CreatePageContentHost(page);

            try
            {
                var context = new ModSettingsUiContext(this, cache.PageKey);
                var isChildPage = !string.IsNullOrWhiteSpace(page.ParentPageId);
                Action onBack = isChildPage
                    ? () =>
                    {
                        _selectedPageId = page.ParentPageId!;
                        _selectionDirty = true;
                        EnsureUiUpToDate();
                    }
                    : static () => { };

                try
                {
                    var pageHeader =
                        ModSettingsUiFactory.CreateModSettingsPageHeaderBar(context, page, isChildPage, onBack);
                    pageHeader.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    nextHeader.AddChild(pageHeader);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to build page header '{page.ModId}:{page.Id}': {ex.Message}");
                    nextHeader.AddChild(ModSettingsUiFactory.CreateBuildErrorPlaceholder(
                        ModSettingsLocalization.Get("page.failed.title", "Page failed to load"),
                        string.Format(ModSettingsLocalization.Get("page.failed.body", "Failed to build page '{0}'."),
                            page.Id)));
                }

                foreach (var item in ModSettingsUiFactory.CreatePageBuildItems(context, page))
                {
                    ct.ThrowIfCancellationRequested();
                    if (buildVersion != cache.BuildVersion || !IsInstanceValid(cache.Root))
                        return;

                    nextContent.AddChild(item.Control);
                    if (item.YieldAfter)
                        await this.AwaitRitsuProcessFrame(ct);
                }

                if (buildVersion != cache.BuildVersion || !IsInstanceValid(cache.Root))
                    return;

                ReplaceHostChildren(cache.HeaderHost, nextHeader);
                ReplaceHostChildren(cache.ContentHost, nextContent);
                cache.State = PageBuildState.Ready;
                if (string.Equals(_selectedPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                {
                    HideContentBuildOverlay();
                    FlushRefreshActionsImmediate();
                    RefreshFocusNavigation();
                    Callable.From(ScrollToSelectedAnchor).CallDeferred();
                }
            }
            catch (OperationCanceledException)
            {
                nextHeader.QueueFree();
                nextContent.QueueFree();
            }
            catch (Exception ex)
            {
                cache.State = PageBuildState.Failed;
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to build page '{page.ModId}:{page.Id}': {ex.Message}");
                nextContent.AddChild(ModSettingsUiFactory.CreateBuildErrorPlaceholder(
                    ModSettingsLocalization.Get("page.failed.title", "Page failed to load"),
                    string.Format(ModSettingsLocalization.Get("page.failed.body", "Failed to build page '{0}'."),
                        page.Id)));
                ReplaceHostChildren(cache.HeaderHost, nextHeader);
                ReplaceHostChildren(cache.ContentHost, nextContent);
                if (string.Equals(_selectedPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                    HideContentBuildOverlay();
            }
        }

        private ModSettingsPage? ResolveSelectedPage()
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase));
        }

        private void ReplaceHostChildren(Control host, Control stagedContent)
        {
            foreach (var child in host.GetChildren()) child?.QueueFree();

            foreach (var child in stagedContent.GetChildren().ToArray())
            {
                stagedContent.RemoveChild(child);
                host.AddChild(child);
            }

            host.ResetSize();
            _contentList.ResetSize();
            _scrollContainer.QueueSort();
            Callable.From(RefreshContentLayout).CallDeferred();
            stagedContent.QueueFree();
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

        private string? GetSelectedPageKey()
        {
            return string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId)
                ? null
                : CreatePageCacheKey(_selectedModId, _selectedPageId);
        }

        private string? GetSelectedSectionKey()
        {
            return string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId) ||
                   string.IsNullOrWhiteSpace(_selectedSectionId)
                ? null
                : CreateSectionCacheKey(_selectedModId, _selectedPageId, _selectedSectionId);
        }

        private static string CreatePageCacheKey(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }

        private static string CreateSectionCacheKey(string modId, string pageId, string sectionId)
        {
            return $"{modId}::{pageId}::{sectionId}";
        }

        private static string SanitizePageNodeName(string text)
        {
            return text.Replace(':', '_');
        }

        private Control CreateContentBuildOverlay()
        {
            var overlay = new PanelContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.None,
                ZIndex = 10,
            };
            overlay.AddThemeStyleboxOverride("panel", CreatePanelStyle(new(0.03f, 0.04f, 0.06f, 0.72f)));

            var center = new CenterContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            overlay.AddChild(center);

            var label = CreateTitleLabel(24, HorizontalAlignment.Center);
            label.CustomMinimumSize = new(320f, 64f);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            center.AddChild(label);
            _contentBuildOverlayLabel = label;
            return overlay;
        }

        private void ShowContentBuildOverlay(string text)
        {
            _contentBuildOverlayLabel.SetTextAutoSize(text);
            _contentBuildOverlay.Visible = true;
            _scrollContainer.MouseFilter = MouseFilterEnum.Ignore;
        }

        private void HideContentBuildOverlay()
        {
            _contentBuildOverlay.Visible = false;
            _scrollContainer.MouseFilter = MouseFilterEnum.Stop;
        }

        private void RefreshContentLayout()
        {
            if (!IsInstanceValid(_contentList) || !IsInstanceValid(_scrollContainer))
                return;

            _contentList.ResetSize();
            _contentList.QueueSort();
            if (_contentList.GetParent() is Control contentFrame)
            {
                contentFrame.ResetSize();
                if (contentFrame is Container contentFrameContainer)
                    contentFrameContainer.QueueSort();
                if (contentFrame.GetParent() is Control contentStack)
                {
                    contentStack.ResetSize();
                    if (contentStack is Container contentStackContainer)
                        contentStackContainer.QueueSort();
                }
            }

            _scrollContainer.ResetSize();
            _scrollContainer.QueueSort();
            _scrollContainer.ScrollVertical = Mathf.Max(0, _scrollContainer.ScrollVertical);
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
            _sidebarStructureDirty = true;
            _contentStructureDirty = true;
            _selectionDirty = true;
            Callable.From(() => EnsureUiUpToDate(true, true)).CallDeferred();
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

        private static string CreatePageStructureSignature(ModSettingsPage page)
        {
            var builder = new StringBuilder();
            builder.Append(page.Id).Append('|')
                .Append(page.ModId).Append('|')
                .Append(page.ParentPageId ?? string.Empty).Append('|')
                .Append(page.Sections.Count);

            foreach (var section in page.Sections)
            {
                builder.Append("||")
                    .Append(section.Id)
                    .Append('|')
                    .Append(section.IsCollapsible ? '1' : '0')
                    .Append('|')
                    .Append(section.StartCollapsed ? '1' : '0')
                    .Append('|')
                    .Append(section.Entries.Count);

                foreach (var entry in section.Entries)
                    builder.Append("::")
                        .Append(entry.Id)
                        .Append('@')
                        .Append(entry.GetType().FullName);
            }

            return builder.ToString();
        }

        private sealed class SidebarModCache
        {
            public required string ModId { get; init; }
            public required VBoxContainer Section { get; init; }
            public required PanelContainer Card { get; init; }
            public required VBoxContainer CardContent { get; init; }
            public required ModSettingsSidebarButton Button { get; init; }
            public required MegaRichTextLabel MetaLabel { get; init; }
            public required VBoxContainer NavStack { get; init; }
            public Dictionary<string, SidebarPageNodeCache> PageNodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class SidebarPageNodeCache
        {
            public required string PageKey { get; set; }
            public required string PageId { get; set; }
            public required int Depth { get; set; }
            public required VBoxContainer Container { get; init; }
            public required ModSettingsSidebarButton Button { get; init; }
            public required VBoxContainer SectionRail { get; init; }
            public required VBoxContainer ChildHost { get; init; }
            public Dictionary<string, SidebarPageNodeCache> ChildPages { get; } = new(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, ModSettingsSidebarButton> SectionButtons { get; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class PageContentCache
        {
            public CancellationTokenSource? BuildCancellation { get; set; }
            public required int BuildVersion { get; set; }
            public required VBoxContainer HeaderHost { get; init; }
            public required VBoxContainer ContentHost { get; init; }
            public required string PageId { get; init; }
            public required string PageKey { get; init; }
            public required VBoxContainer Root { get; init; }
            public required PageBuildState State { get; set; }
            public List<Action> RefreshActions { get; } = [];
            public List<(Control Control, Func<bool> Predicate)> VisibilityTargets { get; } = [];
        }

        private enum PageBuildState
        {
            NotBuilt,
            Building,
            Ready,
            Failed,
        }

        private sealed record PageSnapshot(string Id, string ModId, string? ParentPageId, string StructureSignature);
    }
}
