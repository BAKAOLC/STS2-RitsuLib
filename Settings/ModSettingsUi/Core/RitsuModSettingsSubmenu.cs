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
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Utils;
using Timer = Godot.Timer;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Full-screen mod settings browser: sidebar (mods, pages, sections) and content pane.
    ///     全屏 mod 设置浏览器：侧边栏（mod、页面、section）和内容窗格。
    /// </summary>
    public partial class RitsuModSettingsSubmenu : NSubmenu
    {
        /// <summary>
        ///     Deferred <see cref="FlushDirtyBindings" /> interval after the last binding write.
        ///     最后一次 binding 写入后的延迟 <see cref="FlushDirtyBindings" /> 间隔。
        /// </summary>
        private const double AutosaveDelaySeconds = 0.35;

        /// <summary>
        ///     Debounced mirror paragraph / static refresh. Must be greater than <see cref="AutosaveDelaySeconds" /> so the
        ///     first flush sees persisted and callback <c>Save()</c> effects without an extra refresh pass.
        ///     防抖的镜像段落 / 静态刷新。必须大于 <see cref="AutosaveDelaySeconds" />，使第一次 flush 能看到持久化和回调 <c>Save()</c> 效果，而不需要额外刷新遍历。
        /// </summary>
        private const double RefreshDebounceSeconds = AutosaveDelaySeconds + 0.04;

        private const string ScrollbarContentRightGutterTokenPath = "components.scrollbar.layout.contentRightGutter";
        private const ulong PageBuildFrameBudgetMsec = 6;
        private const int RetainedPageContentCacheLimit = 2;
        private const ulong RetainedPageContentIdleReleaseMsec = 5_000;

        private static readonly StringName PaneSidebarHotkey = MegaInput.viewDeckAndTabLeft;
        private static readonly StringName PaneContentHotkey = MegaInput.viewExhaustPileAndTabRight;
        private static readonly ModSettingsReusableEntryNodePool SharedReusableEntryNodePool = new();

        private readonly Action<IModSettingsBinding> _bindingWriteListener;

        private readonly List<Control> _contentFocusChain = [];

        private readonly HashSet<IModSettingsBinding> _dirtyBindings = [];
        private readonly HashSet<string> _expandedModIds = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<(Control Control, Func<bool> Predicate)> _globalDynamicVisibilityTargets = [];

        private readonly List<ModSettingsRefreshRegistration> _globalRefreshRegistrations = [];

        private readonly Dictionary<string, PageContentCache>
            _pageContentCaches = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PageSnapshot> _pageSnapshots = new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<IModSettingsBinding> _refreshBindingTriggers = [];
        private readonly ModSettingsReusableEntryNodePool _reusableEntryNodePool = SharedReusableEntryNodePool;

        private readonly List<Control> _sidebarFocusChain = [];
        private Control? _contentBuildOverlay;

        private MegaRichTextLabel? _contentEmptyStateLabel;
        private bool _contentLayoutRefreshQueued;
        private ModSettingsUiFactory.FastVerticalStack _contentList = null!;

        private bool _contentOnlyRebuildNeedsContentFocus;
        private Control _contentPanelRoot = null!;
        private ModSettingsUiFactory.FixedWidthScrollContent _contentScrollContent = null!;
        private bool _contentStructureDirty = true;
        private bool _focusNavigationRefreshScheduled;
        private bool _focusSelectedPageButtonOnNextRefresh;
        private bool _guiFocusSignalConnected;
        private Action? _hotkeyPaneContent;
        private Action? _hotkeyPaneSidebar;
        private Control? _initialFocusedControl;
        private string? _lastVisibleContentPageKey;
        private string? _lastVisibleMirrorRefreshPageKey;
        private TextureRect? _leftPaneHotkeyIcon;
        private bool _localeSubscribed;
        private ModSettingsSidebarList _modButtonList = null!;
        private Callable _modSettingsGuiFocusCallable;
        private HBoxContainer? _paneHotkeyHintRow;
        private bool _paneHotkeySignalsConnected;
        private bool _paneHotkeysPushed;
        private AcceptDialog? _pasteErrorDialog;
        private bool _pendingRefreshFlush;
        private bool _pendingScrollResetToTop;
        private Timer? _refreshDebounceTimer;
        private bool _refreshNextFlushAsFullPass;
        private bool _resizeLayoutRefreshQueued;
        private TextureRect? _rightPaneHotkeyIcon;
        private double _saveTimer = -1;
        private ScrollContainer _scrollContainer = null!;
        private string? _selectedModId;
        private string? _selectedPageId;
        private string? _selectedSectionId;
        private bool _selectionDirty = true;
        private Action? _shellThemeChangedHandler;

        private FileSystemWatcher? _shellThemeWatcher;
        private bool _shellThemeWatcherQueued;
        private PanelContainer? _sidebarHeaderCard;
        private Label? _sidebarHeaderSubtitleLabel;
        private Label? _sidebarHeaderTitleLabel;
        private Control _sidebarPanelRoot = null!;
        private ScrollContainer _sidebarScrollContainer = null!;
        private bool _sidebarStructureDirty = true;
        private bool _suppressScrollSync;
        private Callable _updatePaneHotkeyIconsCallable;

        /// <summary>
        ///     Builds layout (header, sidebar, scrollable content) and wires initial structure.
        ///     构建布局（标题、侧边栏、可滚动内容）并连接初始结构。
        /// </summary>
        public RitsuModSettingsSubmenu()
        {
            _bindingWriteListener = OnBindingValueWrittenForSettingsUi;
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
            frame.AddThemeConstantOverride("margin_left", 144);
            frame.AddThemeConstantOverride("margin_top", 64);
            frame.AddThemeConstantOverride("margin_right", 144);
            frame.AddThemeConstantOverride("margin_bottom", 64);
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
            root.AddThemeConstantOverride("separation", 12);
            frame.AddChild(root);

            root.AddChild(CreatePaneHotkeyHintRow());

            var body = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeConstantOverride("separation", 14);
            root.AddChild(body);

            body.AddChild(CreateSidebarPanel());
            body.AddChild(CreateContentPanel());
        }

        /// <inheritdoc />
        protected override Control? InitialFocusedControl => _initialFocusedControl;

        internal bool IsInitialUiReady { get; private set; }

        internal Task WaitForInitialUiReadyAsync()
        {
            EnsureUiUpToDate(true);
            return Task.CompletedTask;
        }

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
            _shellThemeChangedHandler = OnShellThemeChanged;
            RitsuShellThemeRuntime.ThemeChanged += _shellThemeChangedHandler;
            TryStartShellThemeWatcher();
            RitsuShellTooltipTheme.ApplyToTreeRoot(this);
            ProcessMode = ProcessModeEnum.Disabled;
            FocusMode = FocusModeEnum.None;
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what == NotificationResized)
                QueueResizeLayoutRefresh();
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
            CancelPendingUiWork();

            var vp = GetViewport();
            if (vp != null && _guiFocusSignalConnected &&
                vp.IsConnected(Viewport.SignalName.GuiFocusChanged, _modSettingsGuiFocusCallable))
            {
                vp.Disconnect(Viewport.SignalName.GuiFocusChanged, _modSettingsGuiFocusCallable);
                _guiFocusSignalConnected = false;
            }

            TryDisconnectPaneHotkeyStyleSignals();
            PopPaneHotkeys();
            if (_shellThemeChangedHandler != null)
                RitsuShellThemeRuntime.ThemeChanged -= _shellThemeChangedHandler;
            StopShellThemeWatcher();
            ModSettingsBindingWriteEvents.ValueWritten -= _bindingWriteListener;
            base._ExitTree();
            FlushDirtyBindings();
            UnsubscribeLocaleChanges();
        }

        /// <inheritdoc />
        public override void OnSubmenuOpened()
        {
            ModSettingsBindingWriteEvents.ValueWritten += _bindingWriteListener;
            base.OnSubmenuOpened();
            FocusMode = FocusModeEnum.None;
            ApplySettingsFocusBehavior();
            ProcessMode = ProcessModeEnum.Inherit;
            _lastVisibleMirrorRefreshPageKey = null;
            TryStartShellThemeWatcher();
            ShowContentBuildOverlay();
            if (!IsInitialUiReady)
            {
                ObserveBackgroundUiTask(EnsureOpenContentReadyAsync(), "open_content_ready");
                return;
            }

            EnsureUiUpToDate(false, true);
            QueueResizeLayoutRefresh();
        }

        /// <inheritdoc />
        public override void OnSubmenuClosed()
        {
            ModSettingsBindingWriteEvents.ValueWritten -= _bindingWriteListener;
            PopPaneHotkeys();
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            _lastVisibleMirrorRefreshPageKey = null;
            HideContentBuildOverlay();
            StopShellThemeWatcher();
            CallDeferredIfAlive(ApplySettingsFocusBehavior);
            base.OnSubmenuClosed();
        }

        /// <inheritdoc />
        protected override void OnSubmenuShown()
        {
            base.OnSubmenuShown();
            SetProcessInput(true);
            ApplySettingsFocusBehavior();
            PushPaneHotkeys();
            UpdatePaneHotkeyHintIcons();
            RequestMirrorVisibilitySyncRefreshIfNeeded();
            QueueResizeLayoutRefresh();
        }

        /// <inheritdoc />
        protected override void OnSubmenuHidden()
        {
            ModSettingsBindingWriteEvents.ValueWritten -= _bindingWriteListener;
            PopPaneHotkeys();
            FlushPendingRefreshActionsImmediate();
            HideContentBuildOverlay();
            FlushDirtyBindings();
            ProcessMode = ProcessModeEnum.Disabled;
            _lastVisibleMirrorRefreshPageKey = null;
            StopShellThemeWatcher();
            CallDeferredIfAlive(ApplySettingsFocusBehavior);
            base.OnSubmenuHidden();
        }

        private void TryStartShellThemeWatcher()
        {
            if (_shellThemeWatcher != null)
                return;

            if (!RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                return;

            try
            {
                var watcher = new FileSystemWatcher(themesAbs, "*.theme.json")
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                watcher.Changed += OnShellThemeFileChanged;
                watcher.Created += OnShellThemeFileChanged;
                watcher.Deleted += OnShellThemeFileChanged;
                watcher.Renamed += OnShellThemeFileRenamed;
                _shellThemeWatcher = watcher;
            }
            catch
            {
                // Best-effort: live theme reload is optional.
                _shellThemeWatcher = null;
            }
        }

        private void StopShellThemeWatcher()
        {
            if (_shellThemeWatcher == null)
                return;

            try
            {
                _shellThemeWatcher.EnableRaisingEvents = false;
                _shellThemeWatcher.Changed -= OnShellThemeFileChanged;
                _shellThemeWatcher.Created -= OnShellThemeFileChanged;
                _shellThemeWatcher.Deleted -= OnShellThemeFileChanged;
                _shellThemeWatcher.Renamed -= OnShellThemeFileRenamed;
                _shellThemeWatcher.Dispose();
            }
            catch
            {
                // ignored
            }

            _shellThemeWatcher = null;
            _shellThemeWatcherQueued = false;
        }

        private void OnShellThemeFileRenamed(object sender, RenamedEventArgs e)
        {
            QueueShellThemeReapplyDeferred();
        }

        private void OnShellThemeFileChanged(object sender, FileSystemEventArgs e)
        {
            QueueShellThemeReapplyDeferred();
        }

        private void QueueShellThemeReapplyDeferred()
        {
            if (_shellThemeWatcherQueued)
                return;

            _shellThemeWatcherQueued = true;
            CallDeferredIfAlive(FlushShellThemeWatcherReapply);
        }

        private void FlushShellThemeWatcherReapply()
        {
            _shellThemeWatcherQueued = false;
            RitsuShellThemeRuntime.ReapplyActiveTheme(true);
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

        private void OnBindingValueWrittenForSettingsUi(IModSettingsBinding binding)
        {
            MarkDirty(binding);
            if (ShouldRunDeterministicMirrorFullRefresh(binding))
                RequestRefreshAfterDataModelBatchChange();
            else
                RequestRefresh();
        }

        private bool ShouldRunDeterministicMirrorFullRefresh(IModSettingsBinding binding)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId))
                return false;
            if (!string.Equals(binding.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!TryGetSelectedMirrorSyncPolicy(out var policy))
                return false;

            return !policy.HasStableExternalSync;
        }

        internal void MarkDirty(IModSettingsBinding binding)
        {
            MarkDirtyRecursive(binding, []);
            _saveTimer = AutosaveDelaySeconds;
        }

        private void MarkDirtyRecursive(IModSettingsBinding binding, HashSet<IModSettingsBinding> visited)
        {
            if (!visited.Add(binding))
                return;

            _dirtyBindings.Add(binding);
            _refreshBindingTriggers.Add(binding);
            if (binding is not IModSettingsUiRefreshPropagation propagation)
                return;

            foreach (var extra in propagation.ExtraBindingsToMarkDirtyForUi)
                MarkDirtyRecursive(extra, visited);
        }

        internal void RequestRefresh()
        {
            _pendingRefreshFlush = true;
            EnsureRefreshDebounceTimer();
            _refreshDebounceTimer!.Stop();
            _refreshDebounceTimer.Start();
        }

        internal void RequestRefreshAfterDataModelBatchChange()
        {
            _refreshNextFlushAsFullPass = true;
            RequestRefresh();
        }

        internal void RegisterRefreshAction(Action action, ModSettingsUiRefreshSpec spec, string? pageScopeId = null)
        {
            if (spec.IsStaticDisplay)
                return;

            var registration = new ModSettingsRefreshRegistration(action, spec);
            if (!string.IsNullOrWhiteSpace(pageScopeId) &&
                _pageContentCaches.TryGetValue(pageScopeId, out var pageCache))
            {
                pageCache.RefreshRegistrations.Add(registration);
                return;
            }

            _globalRefreshRegistrations.Add(registration);
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

        private static bool ApplyDynamicVisibilityTargets(IEnumerable<(Control Control, Func<bool> Predicate)> targets)
        {
            var changed = false;
            foreach (var (control, predicate) in targets)
            {
                if (!IsInstanceValid(control))
                    continue;
                try
                {
                    var visible = predicate();
                    if (control.Visible == visible)
                        continue;
                    control.Visible = visible;
                    ModSettingsUiFactory.FastVerticalStack.RequestAncestorLayouts(control);
                    changed = true;
                }
                catch
                {
                    if (!control.Visible)
                    {
                        control.Visible = true;
                        changed = true;
                    }

                    ModSettingsUiFactory.FastVerticalStack.RequestAncestorLayouts(control);
                }
            }

            return changed;
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
                WaitTime = RefreshDebounceSeconds,
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

        private void CancelPendingUiWork()
        {
            CancelDeferredRefreshFlush();
            _focusNavigationRefreshScheduled = false;
            _shellThemeWatcherQueued = false;
            _suppressScrollSync = false;
            _pendingScrollResetToTop = false;

            foreach (var cache in _pageContentCaches.Values)
                CancelPageBuild(cache);
        }

        private void ObserveBackgroundUiTask(Task task, string operation)
        {
            _ = ObserveBackgroundUiTaskAsync(task, operation);
        }

        private static async Task ObserveBackgroundUiTaskAsync(Task task, string operation)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Normal when the submenu is closed or freed between deferred frame waits.
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Background UI task '{operation}' failed: {ex.Message}");
            }
        }

        private void CallDeferredIfAlive(Action action)
        {
            var owner = this;
            Callable.From(() =>
            {
                if (!IsInstanceValid(owner))
                    return;

                action();
            }).CallDeferred();
        }

        private void RefreshPageRegistryForUi()
        {
            RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
            ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
            RitsuLibModSettingsBootstrap.RefreshDynamicPages();
        }

        private void FlushPendingRefreshActionsImmediate()
        {
            _refreshDebounceTimer?.Stop();
            if (!_pendingRefreshFlush)
                return;

            _pendingRefreshFlush = false;
            FlushRefreshActionsImmediate();
        }

        private void FlushRefreshActionsImmediate(bool includeAllPages = false, bool emptyDirtyIsFullPass = true)
        {
            var forceFullPass = _refreshNextFlushAsFullPass;
            _refreshNextFlushAsFullPass = false;
            var treatAsFullPass = includeAllPages ||
                                  forceFullPass ||
                                  (emptyDirtyIsFullPass && _refreshBindingTriggers.Count == 0);
            var dirtySnapshot = _refreshBindingTriggers.ToHashSet();
            var selectedLayoutPage = TryGetSelectedPageContentCache(out var selectedBefore) ? selectedBefore : null;
            var hadSelectedHeight = TryMeasurePageContentHeight(selectedLayoutPage, out var selectedHeightBefore);

            RunRegistrations(_globalRefreshRegistrations.ToArray());

            if (includeAllPages)
                foreach (var pageCache in _pageContentCaches.Values)
                    RunRegistrations(pageCache.RefreshRegistrations.ToArray());
            else if (!string.IsNullOrWhiteSpace(_selectedPageId) && !string.IsNullOrWhiteSpace(_selectedModId) &&
                     _pageContentCaches.TryGetValue(CreatePageCacheKey(_selectedModId, _selectedPageId),
                         out var selectedPageCache))
                RunRegistrations(selectedPageCache.RefreshRegistrations.ToArray());

            _refreshBindingTriggers.Clear();

            var contentVisibilityChanged = ApplyDynamicVisibilityTargets(_globalDynamicVisibilityTargets);
            if (_modButtonList != null && IsInstanceValid(_modButtonList))
                _modButtonList.RefreshRows();
            if (includeAllPages)
                contentVisibilityChanged = _pageContentCaches.Values.Aggregate(contentVisibilityChanged,
                    (current, pageCache) => current | ApplyDynamicVisibilityTargets(pageCache.VisibilityTargets));
            else if (!string.IsNullOrWhiteSpace(_selectedPageId) && !string.IsNullOrWhiteSpace(_selectedModId) &&
                     _pageContentCaches.TryGetValue(CreatePageCacheKey(_selectedModId, _selectedPageId),
                         out var selectedVisibilityPage))
                contentVisibilityChanged |= ApplyDynamicVisibilityTargets(selectedVisibilityPage.VisibilityTargets);

            if (contentVisibilityChanged &&
                !TryGetSelectedPageContentCache(out selectedLayoutPage))
                selectedLayoutPage = null;

            var heightChanged = selectedLayoutPage != null &&
                                hadSelectedHeight &&
                                TryMeasurePageContentHeight(selectedLayoutPage, out var selectedHeightAfter) &&
                                Math.Abs(selectedHeightAfter - selectedHeightBefore) >= 0.5f;
            if (selectedLayoutPage != null && (contentVisibilityChanged || heightChanged))
                RefreshPageHostLayout(selectedLayoutPage);
            return;

            void RunRegistrations(ModSettingsRefreshRegistration[] registrations)
            {
                foreach (var registration in registrations)
                {
                    if (!ModSettingsUiRefreshSpec.ShouldRun(registration.Spec, treatAsFullPass, dirtySnapshot))
                        continue;
                    registration.Action();
                }
            }
        }

        private static bool TryMeasurePageContentHeight(PageContentCache? cache, out float height)
        {
            height = 0f;
            if (cache is not { State: PageBuildState.Ready } || !IsInstanceValid(cache.Root))
                return false;

            height = cache.Root.GetCombinedMinimumSize().Y;
            return true;
        }

        private void OnModSettingsGuiFocusChanged(Control node)
        {
            if (!Visible || !IsInstanceValid(this) || !IsInstanceValid(node))
                return;

            if (!ActiveScreenContext.Instance.IsCurrent(this))
                return;

            if ((!ReferenceEquals(node, this) && !IsAncestorOf(node)) ||
                NControllerManager.Instance?.IsUsingController != true)
                return;

            if (_suppressScrollSync)
                return;

            if (node is ModSettingsSidebarList focusedSidebarList)
                focusedSidebarList.EnsureActiveRowVisible();
            else if (_sidebarScrollContainer.IsAncestorOf(node))
                _sidebarScrollContainer.EnsureControlVisible(node);
            else if (_scrollContainer.IsAncestorOf(node))
                _scrollContainer.EnsureControlVisible(node);
        }

        /// <summary>
        ///     Selects a mod in the sidebar, optionally opening <paramref name="pageId" />, and rebuilds the UI.
        ///     在侧边栏中选择一个 mod，可选打开 <paramref name="pageId" />，并重建 UI。
        /// </summary>
        public void SelectMod(string modId, string? pageId = null)
        {
            _selectedModId = modId;
            _selectedPageId = pageId;
            _selectedSectionId = null;
            ExpandOnlyMod(modId);
            _sidebarStructureDirty = true;
            _selectionDirty = true;
            _focusSelectedPageButtonOnNextRefresh = true;
            EnsureUiUpToDate();
        }

        /// <summary>
        ///     Switches to <paramref name="pageId" /> within the currently selected mod.
        ///     在当前选中的 mod 内切换到 <paramref name="pageId" />。
        /// </summary>
        public void NavigateToPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            _selectedPageId = pageId;
            _selectedSectionId = null;
            _sidebarStructureDirty = true;
            _selectionDirty = true;
            _contentOnlyRebuildNeedsContentFocus = false;
            EnsureUiUpToDate();
        }

        /// <summary>
        ///     Opens <paramref name="pageId" /> and scrolls/focuses <paramref name="sectionId" />.
        ///     打开 <paramref name="pageId" /> 并滚动/聚焦 <paramref name="sectionId" />。
        /// </summary>
        public void NavigateToSection(string pageId, string sectionId)
        {
            if (string.IsNullOrWhiteSpace(_selectedModId))
                return;

            CancelPendingContentScrollReset();
            if (string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_selectedSectionId, sectionId, StringComparison.OrdinalIgnoreCase))
            {
                CallDeferredIfAlive(ScrollToSelectedAnchor);
                RefreshFocusNavigation();
                CallDeferredIfAlive(() =>
                {
                    if (IsInstanceValid(_modButtonList) && _modButtonList.IsVisibleInTree())
                        _modButtonList.GrabFocus();
                });
                return;
            }

            var pageChanged = !string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase);
            if (!pageChanged)
            {
                _selectedSectionId = sectionId;
                RefreshSelectionState();
                CallDeferredIfAlive(ScrollToSelectedAnchor);
                RefreshFocusNavigation();
                return;
            }

            _selectedPageId = pageId;
            _selectedSectionId = sectionId;
            _sidebarStructureDirty = true;
            _selectionDirty = true;
            _contentOnlyRebuildNeedsContentFocus = true;
            EnsureUiUpToDate(false, pageChanged);
        }

        internal async Task<ModSettingsOpenResult> OpenToAsync(ModSettingsLocation location,
            ModSettingsOpenOptions options)
        {
            if (!IsInstanceValid(this))
                return ModSettingsOpenResult.Error("ui-not-available", "The mod settings UI is not available.",
                    location);

            var resolved = ModSettingsNavigator.ResolveLocation(location);
            if (!resolved.Success)
                return resolved;

            var target = new ModSettingsLocation(resolved.ModId, resolved.PageId, resolved.SectionId,
                resolved.EntryId);
            SelectMod(target.ModId, target.PageId);
            await WaitForInitialUiReadyAsync();
            if (!IsInstanceValid(this))
                return ModSettingsOpenResult.Error("ui-not-available", "The mod settings UI was closed.", target);

            await WaitForSelectedPageContentReadyAsync();
            if (!IsInstanceValid(this))
                return ModSettingsOpenResult.Error("ui-not-available", "The mod settings UI was closed.", target);

            Control? scrollTarget = null;
            if (!string.IsNullOrWhiteSpace(target.SectionId))
            {
                _selectedSectionId = target.SectionId;
                RefreshSelectionState();
                var sectionAnchor = await ResolveSectionAnchorForOpenAsync(target.SectionId);
                if (sectionAnchor == null)
                    return ModSettingsOpenResult.Error("section-not-found",
                        $"Settings section '{target.SectionId}' was not found after the page loaded.", target);

                if (options.ExpandCollapsedSection)
                    ExpandSectionAnchor(sectionAnchor);
                else if (!string.IsNullOrWhiteSpace(target.EntryId))
                    EnsureSectionContentBuilt(sectionAnchor);
                scrollTarget = sectionAnchor;
            }

            if (!string.IsNullOrWhiteSpace(target.EntryId))
            {
                if (string.IsNullOrWhiteSpace(target.SectionId))
                    return ModSettingsOpenResult.Error("section-not-found",
                        "A resolved section id is required before opening an entry.", target);

                var entryAnchor = await ResolveEntryAnchorForOpenAsync(target.SectionId, target.EntryId);
                if (entryAnchor == null)
                    return ModSettingsOpenResult.Error("entry-not-found",
                        $"Settings entry '{target.EntryId}' was not found after the page loaded.", target);
                if (!entryAnchor.IsVisibleInTree())
                    return ModSettingsOpenResult.Error("entry-hidden",
                        $"Settings entry '{target.EntryId}' is currently hidden.", target);
                scrollTarget = entryAnchor;
            }

            if (scrollTarget != null)
            {
                CancelPendingContentScrollReset();
                AlignScrollToAnchor(scrollTarget);
                if (options.Focus)
                    FocusTarget(scrollTarget);
                if (options.Highlight)
                    PulseTarget(scrollTarget);
            }
            else
            {
                _scrollContainer.ScrollVertical = 0;
            }

            return ModSettingsOpenResult.Ok("opened",
                $"Opened settings location '{ModSettingsNavigator.FormatLocation(target)}'.",
                target);
        }

        private async Task<Control?> ResolveSectionAnchorForOpenAsync(string sectionId)
        {
            if (TryFindSectionAnchorOnSelectedPage(sectionId, out var anchor))
                return anchor;

            await WaitForSelectedPageContentReadyAsync();
            return TryFindSectionAnchorOnSelectedPage(sectionId, out anchor) ? anchor : null;
        }

        private async Task<Control?> ResolveEntryAnchorForOpenAsync(string sectionId, string entryId)
        {
            if (TryFindEntryAnchorOnSelectedPage(sectionId, entryId, out var anchor))
                return anchor;

            if (TryFindSectionAnchorOnSelectedPage(sectionId, out var sectionAnchor))
                EnsureSectionContentBuilt(sectionAnchor);

            await WaitForSelectedPageContentReadyAsync();
            return TryFindEntryAnchorOnSelectedPage(sectionId, entryId, out anchor) ? anchor : null;
        }

        internal void RegisterEntryAnchor(ModSettingsPage page, ModSettingsSection section,
            ModSettingsEntryDefinition entry, Control control)
        {
            var pageKey = CreatePageCacheKey(page.ModId, page.Id);
            if (!_pageContentCaches.TryGetValue(pageKey, out var cache))
                return;

            cache.EntryAnchors[CreateEntryCacheKey(page.ModId, page.Id, section.Id, entry.Id)] = control;
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
            if (IsFocusNavigationBlocked())
                return;

            FocusSidebarPaneFromInput();
        }

        private void OnHotkeyPressedFocusContent()
        {
            if (!Visible || !IsInstanceValid(this) || !ActiveScreenContext.Instance.IsCurrent(this))
                return;
            if (IsContentBuildOverlayVisible())
                return;
            if (IsFocusNavigationBlocked())
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
            if (IsContentBuildOverlayVisible())
                return;

            var fo = GetViewport()?.GuiGetFocusOwner();
            if (IsFocusUnderPopupOrTransientWindow(fo))
                return;

            if (fo != null && IsInstanceValid(fo) && _contentPanelRoot.IsAncestorOf(fo))
                return;

            RebuildFocusChainsOnly();
            GrabControlDeferred(ResolveContentFocusTargetForSection());
        }

        private Control? ResolveContentFocusTargetForSection()
        {
            if (_contentFocusChain.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                if (TryFindSectionAnchorOnSelectedPage(_selectedSectionId, out var anchor))
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
            return _modButtonList is { Visible: true } ? _modButtonList : _sidebarFocusChain.FirstOrDefault();
        }

        private Control? ResolveInitialSidebarFocus()
        {
            _focusSelectedPageButtonOnNextRefresh = false;
            return _modButtonList is { Visible: true } ? _modButtonList : null;
        }

        private Control CreateSidebarPanel()
        {
            var panel = new Panel
            {
                Name = "RitsuSidebarPanel",
                CustomMinimumSize = new(RitsuShellTheme.Current.Metric.Sidebar.Width, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _sidebarPanelRoot = panel;
            panel.AddThemeStyleboxOverride("panel",
                RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Sidebar,
                    RitsuShellTheme.Current.Metric.Radius.Default));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 12);
            frame.AddThemeConstantOverride("margin_top", 12);
            frame.AddThemeConstantOverride("margin_right", 12);
            frame.AddThemeConstantOverride("margin_bottom", 12);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 10);
            frame.AddChild(root);

            var headerCard = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            headerCard.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            _sidebarHeaderCard = headerCard;
            root.AddChild(headerCard);

            var headerBox = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            headerBox.AddThemeConstantOverride("separation", 2);
            headerCard.AddChild(headerBox);

            var headerTitle = new Label
            {
                Text = ModSettingsLocalization.Get("sidebar.title", "Mods"),
                CustomMinimumSize = new(0f, 26f),
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.Off,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                Modulate = RitsuShellTheme.Current.Text.SidebarSection,
            };
            headerTitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            headerTitle.AddThemeFontSizeOverride("font_size", 22);
            _sidebarHeaderTitleLabel = headerTitle;
            headerBox.AddChild(headerTitle);

            var subtitleLabel = new Label
            {
                Text = ModSettingsLocalization.Get("sidebar.subtitle", "Browse mods, pages, and sections."),
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = RitsuShellTheme.Current.Text.RichSecondary,
            };
            subtitleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            subtitleLabel.AddThemeFontSizeOverride("font_size", 16);
            _sidebarHeaderSubtitleLabel = subtitleLabel;
            headerBox.AddChild(subtitleLabel);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                FollowFocus = false,
                FocusMode = FocusModeEnum.None,
            };
            _sidebarScrollContainer = scroll;
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(scroll);
            root.AddChild(scroll);

            var sidebarScrollFrame = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sidebarScrollFrame.AddThemeConstantOverride("margin_right", ResolveScrollbarContentRightGutter());
            scroll.AddChild(sidebarScrollFrame);

            _modButtonList = new(this)
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Stop,
            };
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
                ClipContents = true,
            };
            _contentPanelRoot = panel;
            panel.AddThemeStyleboxOverride("panel",
                RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Content,
                    RitsuShellTheme.Current.Metric.Radius.Default));

            var frame = new MarginContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.AddThemeConstantOverride("margin_left", 14);
            frame.AddThemeConstantOverride("margin_top", 14);
            frame.AddThemeConstantOverride("margin_right", 14);
            frame.AddThemeConstantOverride("margin_bottom", 14);
            panel.AddChild(frame);

            var root = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 8);
            frame.AddChild(root);

            _scrollContainer = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                FollowFocus = true,
                FocusMode = FocusModeEnum.None,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(_scrollContainer);
            root.AddChild(_scrollContainer);

            var scrollContent = new ModSettingsUiFactory.FixedWidthScrollContent
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _contentScrollContent = scrollContent;
            _scrollContainer.AddChild(scrollContent);

            _contentList = new(RitsuShellThemeLayoutResolver.ResolveInt("components.page.layout.sectionSeparation", 8))
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };

            scrollContent.Configure(_contentList, ResolveScrollbarContentRightGutter());
            CreateContentBuildOverlay(panel);

            return panel;
        }

        private void CreateContentBuildOverlay(Control parent)
        {
            var overlay = new ColorRect
            {
                Name = "RitsuContentBuildOverlay",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All,
                Visible = false,
                Color = new(0f, 0f, 0f, 0.22f),
            };
            parent.AddChild(overlay);
            _contentBuildOverlay = overlay;
        }

        private void EnsureUiUpToDate(bool forceStructure = false, bool includeAllPagesRefresh = false)
        {
            RefreshPageRegistryForUi();
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
            RefreshContentBuildOverlayVisibility();
            IsInitialUiReady = true;
            QueueDeferredContentLayoutRefresh();
        }

        private async Task EnsureOpenContentReadyAsync()
        {
            await RitsuGodotAwaitSafety.AwaitProcessFrameAsync(GetTree(), this);
            if (!IsInstanceValid(this))
                return;

            EnsureUiUpToDate(true, true);
            EnsureSelectedPageContentStructure();
            RefreshSelectionState();
            RefreshVisibleContent(true);
            RefreshContentBuildOverlayVisibility();
            QueueResizeLayoutRefresh();
        }

        private async Task WaitForSelectedPageContentReadyAsync()
        {
            EnsureSelectedPageContentStructure();
            RefreshVisibleContent(true);
            if (ResolveSelectedPage() is not { } page)
                return;

            var cache = EnsurePageContentCache(page);
            if (cache.State is not (PageBuildState.Ready or PageBuildState.Failed))
                StartBuildPage(page, cache);

            if (cache.BuildTask != null)
                await cache.BuildTask;
            if (!IsInstanceValid(this) || !IsPageCurrentlySelected(cache))
                return;

            RefreshPageHostLayout(cache);
            await RitsuGodotAwaitSafety.AwaitProcessFrameAsync(GetTree(), this);
            if (IsInstanceValid(this) && IsPageCurrentlySelected(cache))
                RefreshPageHostLayout(cache);
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
                .Where(page => string.IsNullOrWhiteSpace(page.ParentPageId) &&
                               IsPageVisibleOnCurrentHost(page))
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
                ExpandOnlyMod(_selectedModId);
            }

            var modPages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                               IsPageVisibleOnCurrentHost(page))
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
            _modButtonList.ClearRows();

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.IsNullOrWhiteSpace(page.ParentPageId) &&
                               IsPageVisibleOnCurrentHost(page))
                .GroupBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => ModSettingsRegistry.GetModSidebarOrder(group.Key))
                .ThenBy(group => ModSettingsLocalization.ResolveModName(group.Key, group.Key),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _modButtonList.SetRows(BuildSidebarRows(rootPages));
            _sidebarStructureDirty = false;
            _selectionDirty = true;
        }

        private List<ModSettingsSidebarRow> BuildSidebarRows(
            IReadOnlyList<IGrouping<string, ModSettingsPage>> rootPages)
        {
            var rows = new List<ModSettingsSidebarRow>();
            foreach (var group in rootPages)
            {
                var modId = group.Key;
                var pages = ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase) &&
                                   IsPageVisibleOnCurrentHost(page))
                    .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var rootModPages = pages.Where(page => string.IsNullOrWhiteSpace(page.ParentPageId)).ToArray();
                var navVisible = ShouldShowExpandedModNav(modId);
                var title = ResolveSidebarModTitle(rootModPages.Length > 0 ? rootModPages : pages);
                var pageCountText = string.Format(
                    ModSettingsLocalization.Get("sidebar.modMeta", "{0} pages"),
                    pages.Length);

                rows.Add(new(
                    ModSettingsSidebarItemKind.ModGroup,
                    modId,
                    null,
                    null,
                    modId,
                    title,
                    navVisible ? "▼" : "▶",
                    0,
                    pageCountText,
                    () => ActivateSidebarMod(modId),
                    null));

                if (!navVisible)
                    continue;

                foreach (var page in rootModPages)
                    AddSidebarPageRows(rows, pages, page, 1);
            }

            return rows;
        }

        private void AddSidebarPageRows(List<ModSettingsSidebarRow> rows, IReadOnlyList<ModSettingsPage> pages,
            ModSettingsPage page, int depth)
        {
            var pageKey = CreatePageCacheKey(page.ModId, page.Id);
            rows.Add(new(
                ModSettingsSidebarItemKind.Page,
                page.ModId,
                page.Id,
                null,
                pageKey,
                ResolvePageTabTitle(page),
                "◦",
                Math.Max(0, depth - 1),
                null,
                () => ActivateSidebarPage(page.ModId, page.Id),
                CreateSidebarPageVisibilityPredicate(page, pages)));

            if (string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase))
                rows.AddRange(from section in page.Sections
                    let sectionKey = CreateSectionCacheKey(page.ModId, page.Id, section.Id)
                    select new ModSettingsSidebarRow(ModSettingsSidebarItemKind.Section, page.ModId, page.Id,
                        section.Id, sectionKey, ResolveSectionTitle(section), "·", depth + 1, null,
                        () => ActivateSidebarSection(page.ModId, page.Id, section.Id),
                        CreateSidebarSectionVisibilityPredicate(section)));

            if (!IsSelectedPageInNavSubtree(_selectedPageId, page, pages))
                return;

            var childPages = pages.Where(candidate =>
                    string.Equals(candidate.ParentPageId, page.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var child in childPages)
                AddSidebarPageRows(rows, pages, child, depth + 1);
        }

        private void ActivateSidebarMod(string modId)
        {
            if (_expandedModIds.Remove(modId))
            {
                _sidebarStructureDirty = true;
                _selectionDirty = true;
                EnsureUiUpToDate();
                return;
            }

            if (!string.Equals(_selectedModId, modId, StringComparison.OrdinalIgnoreCase))
            {
                _selectedModId = modId;
                _selectedPageId = ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase) &&
                                   string.IsNullOrWhiteSpace(page.ParentPageId) &&
                                   IsPageVisibleOnCurrentHost(page))
                    .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(page => page.Id)
                    .FirstOrDefault();
                _selectedSectionId = null;
                _focusSelectedPageButtonOnNextRefresh = true;
            }

            ExpandOnlyMod(modId);
            _sidebarStructureDirty = true;
            _selectionDirty = true;
            EnsureUiUpToDate();
        }

        private void ActivateSidebarPage(string modId, string pageId)
        {
            var samePage = string.Equals(_selectedPageId, pageId, StringComparison.OrdinalIgnoreCase);
            _selectedModId = modId;
            _selectedPageId = pageId;
            if (!samePage)
                _selectedSectionId = null;
            ExpandOnlyMod(modId);
            _sidebarStructureDirty = true;
            _selectionDirty = true;
            EnsureUiUpToDate();
        }

        private void ActivateSidebarSection(string modId, string pageId, string sectionId)
        {
            _selectedModId = modId;
            NavigateToSection(pageId, sectionId);
        }

        private void EnsureSelectedPageContentStructure()
        {
            EnsurePageContentCacheStructure();

            if (ResolveSelectedPage() is { } pageToRender)
                EnsurePageContentCache(pageToRender);
        }

        private void EnsureAllPageContentCacheStructure()
        {
            EnsurePageContentCacheStructure();
            foreach (var page in ModSettingsRegistry.GetPages())
                EnsurePageContentCache(page);
        }

        private void EnsurePageContentCacheStructure()
        {
            if (!_contentStructureDirty) return;
            var livePageKeys = new HashSet<string>(
                ModSettingsRegistry.GetPages().Select(page => CreatePageCacheKey(page.ModId, page.Id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var staleKey in _pageContentCaches.Keys.Where(key => !livePageKeys.Contains(key)).ToArray())
                if (_pageContentCaches.TryGetValue(staleKey, out var staleCache))
                    ReleasePageContentCache(staleKey, staleCache);

            foreach (var cache in _pageContentCaches.Values)
            {
                CancelPageBuild(cache);
                if (IsInstanceValid(cache.Root))
                    cache.Root.Visible = false;
            }

            _globalRefreshRegistrations.Clear();
            HideTransientContentState();
            _contentStructureDirty = false;
        }

        private PageContentCache EnsurePageContentCache(ModSettingsPage pageToRender)
        {
            var pageKey = CreatePageCacheKey(pageToRender.ModId, pageToRender.Id);
            if (_pageContentCaches.TryGetValue(pageKey, out var existingCache))
            {
                if (existingCache.Root.GetParent() != _contentList)
                    _contentList.AddChild(existingCache.Root);
                return existingCache;
            }

            var root = new ModSettingsUiFactory.FastVerticalStack(8)
            {
                Name = $"CachedPage_{SanitizePageNodeName(pageKey)}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };

            var headerHost = new ModSettingsUiFactory.FastVerticalStack(8)
            {
                Name = $"PageHeader_{SanitizePageNodeName(pageKey)}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };

            var contentHost = ModSettingsUiFactory.CreatePageContentHost(pageToRender);
            _contentList.AddChild(root);
            root.AddChild(headerHost);
            root.AddChild(contentHost);

            var cache = new PageContentCache
            {
                PageId = pageToRender.Id,
                PageKey = pageKey,
                Root = root,
                HeaderHost = headerHost,
                ContentHost = contentHost,
                State = PageBuildState.NotBuilt,
                BuildVersion = 0,
                LastUsedMsec = Time.GetTicksMsec(),
            };
            _pageContentCaches[pageKey] = cache;
            return cache;
        }

        private void RefreshSelectionState()
        {
            var selectedPageKey = GetSelectedPageKey();
            var selectedSectionKey = GetSelectedSectionKey();

            _modButtonList.SyncSelection(_selectedModId, selectedPageKey, selectedSectionKey);
            _selectionDirty = false;
        }

        private void RefreshVisibleContent(bool includeAllPagesRefresh)
        {
            foreach (var cache in _pageContentCaches.Values)
                cache.Root.Visible = false;

            HideTransientContentState();

            if (string.IsNullOrWhiteSpace(_selectedModId))
            {
                _lastVisibleContentPageKey = null;
                ShowTransientContentState(ModSettingsLocalization.Get("empty.none",
                    "No mod settings pages are currently registered."));
                RefreshFocusNavigation();
                return;
            }

            var rootPages = ModSettingsRegistry.GetPages()
                .Where(page => string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                               string.IsNullOrWhiteSpace(page.ParentPageId) &&
                               IsPageVisibleOnCurrentHost(page))
                .OrderBy(ModSettingsRegistry.GetEffectivePageSortOrder)
                .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rootPages.Length == 0)
            {
                _lastVisibleContentPageKey = null;
                ShowTransientContentState(ModSettingsLocalization.Get("empty.mod",
                    "This mod does not currently expose a settings page."));
                RefreshFocusNavigation();
                return;
            }

            var pageToRender = ResolveSelectedPage();
            if (pageToRender == null)
            {
                _lastVisibleContentPageKey = null;
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

            selectedCache.LastUsedMsec = Time.GetTicksMsec();
            RequestMirrorVisibilitySyncRefreshIfNeeded(pageKey);

            var visiblePageChanged = !string.Equals(_lastVisibleContentPageKey, pageKey,
                StringComparison.OrdinalIgnoreCase);
            _lastVisibleContentPageKey = pageKey;
            if (visiblePageChanged && string.IsNullOrWhiteSpace(_selectedSectionId))
                ScheduleContentScrollResetToTop();

            selectedCache.Root.Visible = true;
            switch (selectedCache.State)
            {
                case PageBuildState.NotBuilt or PageBuildState.Failed:
                    StartBuildPage(pageToRender, selectedCache);
                    break;
                case PageBuildState.Building:
                    ShowContentBuildOverlay();
                    RefreshPageHostLayout(selectedCache);
                    break;
                case PageBuildState.Ready:
                    RefreshPageHostLayout(selectedCache);
                    FlushRefreshActionsImmediate(includeAllPagesRefresh);
                    break;
            }

            RefreshFocusNavigation();
            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                CallDeferredIfAlive(ScrollToSelectedAnchor);
            SweepPageContentCachePool(pageKey);
        }

        private void SweepPageContentCachePool(string selectedPageKey)
        {
            var now = Time.GetTicksMsec();
            var inactive = _pageContentCaches
                .Where(pair => !string.Equals(pair.Key, selectedPageKey, StringComparison.OrdinalIgnoreCase))
                .OrderBy(pair => pair.Value.LastUsedMsec)
                .ToArray();

            var warmInactiveLimit = Math.Max(0, RetainedPageContentCacheLimit - 1);
            var idleOverflowCount = Math.Max(0, inactive.Length - warmInactiveLimit);
            for (var i = 0; i < inactive.Length; i++)
            {
                var (pageKey, cache) = inactive[i];
                var idleOverflow = i < idleOverflowCount;
                var idleExpired = now >= cache.LastUsedMsec &&
                                  now - cache.LastUsedMsec >= RetainedPageContentIdleReleaseMsec;
                if (!idleOverflow || !idleExpired)
                    continue;

                ReleasePageContentCache(pageKey, cache);
            }
        }

        private void ReleasePageContentCache(string pageKey, PageContentCache cache)
        {
            CancelPageBuild(cache);
            cache.RefreshRegistrations.Clear();
            cache.VisibilityTargets.Clear();
            if (IsInstanceValid(cache.Root))
            {
                RecycleReusableEntryNodes(cache.Root);
                cache.Root.QueueFree();
            }

            _pageContentCaches.Remove(pageKey);
            _reusableEntryNodePool.Sweep();
        }

        private void RequestMirrorVisibilitySyncRefreshIfNeeded(string? selectedPageKey = null)
        {
            if (!TryGetSelectedMirrorSyncPolicy(out var policy) || policy.HasStableExternalSync)
                return;

            var pageKey = selectedPageKey;
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                if (string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId))
                    return;
                pageKey = CreatePageCacheKey(_selectedModId, _selectedPageId);
            }

            if (string.Equals(_lastVisibleMirrorRefreshPageKey, pageKey, StringComparison.OrdinalIgnoreCase))
                return;

            _lastVisibleMirrorRefreshPageKey = pageKey;
            RequestRefreshAfterDataModelBatchChange();
        }

        private bool TryGetSelectedMirrorSyncPolicy(out ModSettingsMirrorSyncPolicy policy)
        {
            policy = default;
            return !string.IsNullOrWhiteSpace(_selectedModId) &&
                   !string.IsNullOrWhiteSpace(_selectedPageId) &&
                   ModSettingsMirrorSyncPolicyRegistry.TryGetPolicy(_selectedModId, _selectedPageId, out policy);
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

        private void ShowContentBuildOverlay()
        {
            if (_contentBuildOverlay == null || !IsInstanceValid(_contentBuildOverlay))
                return;

            _contentBuildOverlay.Visible = true;
            _contentBuildOverlay.MoveToFront();
            var focusOwner = GetViewport()?.GuiGetFocusOwner();
            if (focusOwner != null && IsInstanceValid(focusOwner) && _contentPanelRoot.IsAncestorOf(focusOwner))
                GrabControlDeferred(ResolveSidebarTargetMatchingContent());
        }

        private void HideContentBuildOverlay()
        {
            if (_contentBuildOverlay == null || !IsInstanceValid(_contentBuildOverlay))
                return;

            _contentBuildOverlay.Visible = false;
        }

        private void RefreshContentBuildOverlayVisibility()
        {
            if (!Visible || !IsInsideTree())
            {
                HideContentBuildOverlay();
                return;
            }

            if (TryGetSelectedPageContentCache(out var cache) && cache.State == PageBuildState.Building)
                ShowContentBuildOverlay();
            else
                HideContentBuildOverlay();
        }

        private bool IsContentBuildOverlayVisible()
        {
            return _contentBuildOverlay != null &&
                   IsInstanceValid(_contentBuildOverlay) &&
                   _contentBuildOverlay.Visible;
        }

        private static bool IsSelectedPageInNavSubtree(string? selectedPageId, ModSettingsPage subtreeRoot,
            IReadOnlyList<ModSettingsPage> pages)
        {
            if (string.IsNullOrWhiteSpace(selectedPageId))
                return false;

            var map = new Dictionary<string, ModSettingsPage>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in pages)
                map[p.Id] = p;

            if (!map.TryGetValue(selectedPageId, out _))
                return false;

            if (string.Equals(selectedPageId, subtreeRoot.Id, StringComparison.OrdinalIgnoreCase))
                return true;

            var cur = map[selectedPageId];
            while (!string.IsNullOrWhiteSpace(cur.ParentPageId))
            {
                if (string.Equals(cur.ParentPageId, subtreeRoot.Id, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!map.TryGetValue(cur.ParentPageId, out cur!))
                    break;
            }

            return false;
        }

        private Func<bool>? CreateSidebarPageVisibilityPredicate(ModSettingsPage page,
            IReadOnlyList<ModSettingsPage> pages)
        {
            if (page.VisibleWhen == null &&
                page is { VisibleOnHostSurfaces: ModSettingsHostSurface.All, SidebarVisibleOnlyWhenActive: false })
                return null;

            return () => (page.VisibleWhen?.Invoke() ?? true) &&
                         ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces) &&
                         (!page.SidebarVisibleOnlyWhenActive ||
                          IsSelectedPageInNavSubtree(_selectedPageId, page, pages));
        }

        private static Func<bool>? CreateSidebarSectionVisibilityPredicate(ModSettingsSection section)
        {
            if (section.VisibleWhen == null && section.VisibleOnHostSurfaces == ModSettingsHostSurface.All)
                return null;

            return () => (section.VisibleWhen?.Invoke() ?? true) &&
                         ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(section.VisibleOnHostSurfaces);
        }

        private void StartBuildPage(ModSettingsPage page, PageContentCache cache)
        {
            if (cache.State is PageBuildState.Ready or PageBuildState.Building)
                return;

            CancelPageBuild(cache);
            cache.State = PageBuildState.Building;
            if (IsPageCurrentlySelected(cache))
                ShowContentBuildOverlay();

            var buildVersion = cache.BuildVersion;
            var cts = new CancellationTokenSource();
            cache.BuildCancellation = cts;
            cache.BuildTask = BuildPageAsync(page, cache, buildVersion, cts);
            ObserveBackgroundUiTask(cache.BuildTask, $"build_page:{page.ModId}:{page.Id}");
        }

        private async Task BuildPageAsync(ModSettingsPage page, PageContentCache cache, int buildVersion,
            CancellationTokenSource cts)
        {
            var ct = cts.Token;
            IDisposable? layoutDefer = null;

            try
            {
                await YieldPageBuildAsync(cache, buildVersion, ct);
                cache.RefreshRegistrations.Clear();
                cache.VisibilityTargets.Clear();
                cache.EntryAnchors.Clear();

                ClearHostChildren(cache.HeaderHost);
                ClearHostChildren(cache.ContentHost);

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
                    cache.HeaderHost.AddChild(pageHeader);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Settings] Failed to build page header '{page.ModId}:{page.Id}': {ex.Message}");
                    cache.HeaderHost.AddChild(ModSettingsUiFactory.CreateBuildErrorPlaceholder(
                        ModSettingsLocalization.Get("page.failed.title", "Page failed to load"),
                        string.Format(ModSettingsLocalization.Get("page.failed.body", "Failed to build page '{0}'."),
                            page.Id)));
                }

                layoutDefer = ModSettingsUiFactory.FastVerticalStack.DeferLayoutRequests();
                var frameStartedAt = Time.GetTicksMsec();
                foreach (var item in ModSettingsUiFactory.CreatePageBuildItems(context, page,
                             _reusableEntryNodePool))
                {
                    ThrowIfPageBuildCanceled(cache, buildVersion, ct);

                    (item.Parent ?? cache.ContentHost).AddChild(item.Control);
                    item.AfterAdded?.Invoke(item.Control);

                    if (Time.GetTicksMsec() - frameStartedAt < PageBuildFrameBudgetMsec)
                        continue;

                    layoutDefer.Dispose();
                    layoutDefer = null;
                    RefreshPageHostLayout(cache);
                    await YieldPageBuildAsync(cache, buildVersion, ct);
                    layoutDefer = ModSettingsUiFactory.FastVerticalStack.DeferLayoutRequests();
                    frameStartedAt = Time.GetTicksMsec();
                }

                layoutDefer.Dispose();
                layoutDefer = null;
                ThrowIfPageBuildCanceled(cache, buildVersion, ct);

                if (IsPageCurrentlySelected(cache))
                {
                    cache.Root.Visible = true;
                    cache.Root.Modulate = Colors.White;
                }
                else
                {
                    cache.Root.Visible = false;
                }

                RefreshPageHostLayout(cache);
                CompletePageBuild(page, cache);
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(cache.BuildCancellation, cts) && cache.State == PageBuildState.Building)
                    cache.State = PageBuildState.NotBuilt;
            }
            catch (Exception ex)
            {
                if (!ReferenceEquals(cache.BuildCancellation, cts))
                    return;

                cache.State = PageBuildState.Failed;
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] Failed to build page '{page.ModId}:{page.Id}': {ex.Message}");
                ClearHostChildren(cache.ContentHost);
                cache.ContentHost.AddChild(ModSettingsUiFactory.CreateBuildErrorPlaceholder(
                    ModSettingsLocalization.Get("page.failed.title", "Page failed to load"),
                    string.Format(ModSettingsLocalization.Get("page.failed.body", "Failed to build page '{0}'."),
                        page.Id)));
                if (IsPageCurrentlySelected(cache))
                {
                    cache.Root.Visible = true;
                    cache.Root.Modulate = Colors.White;
                    RefreshPageHostLayout(cache);
                    FlushRefreshActionsImmediate();
                    RefreshSelectionState();
                }
                else
                {
                    if (IsInstanceValid(cache.Root))
                        cache.Root.Visible = false;
                    RefreshPageHostLayout(cache);
                }
            }
            finally
            {
                layoutDefer?.Dispose();
                if (ReferenceEquals(cache.BuildCancellation, cts))
                {
                    cache.BuildCancellation = null;
                    cache.BuildTask = null;
                }

                cts.Dispose();
                RefreshContentBuildOverlayVisibility();
            }
        }

        private void ClearHostChildren(Node host)
        {
            foreach (var child in host.GetChildren().ToArray())
            {
                if (child is ModSettingsUiFactory.ReusableSettingLine line)
                {
                    _reusableEntryNodePool.Return(line);
                    continue;
                }

                if (child != null)
                    RecycleReusableEntryNodes(child);
                if (child?.GetParent() == host)
                    host.RemoveChild(child);
                child?.QueueFree();
            }
        }

        private static void CancelPageBuild(PageContentCache cache)
        {
            cache.BuildVersion++;
            cache.BuildCancellation?.Cancel();
            if (cache.State == PageBuildState.Building)
                cache.State = PageBuildState.NotBuilt;
        }

        private async Task YieldPageBuildAsync(PageContentCache cache, int buildVersion,
            CancellationToken ct)
        {
            ThrowIfPageBuildCanceled(cache, buildVersion, ct);
            await RitsuGodotAwaitSafety.AwaitProcessFrameAsync(GetTree(), this, ct);
            ThrowIfPageBuildCanceled(cache, buildVersion, ct);
        }

        private static void ThrowIfPageBuildCanceled(PageContentCache cache, int buildVersion,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (buildVersion != cache.BuildVersion || !IsInstanceValid(cache.Root))
                throw new OperationCanceledException(ct);
        }

        private void CompletePageBuild(ModSettingsPage page, PageContentCache cache)
        {
            cache.State = PageBuildState.Ready;

            if (page.EnabledWhen != null)
            {
                ModSettingsUiFactory.ApplyEnabledRecursive(cache.HeaderHost, page.EnabledWhen());
                ModSettingsUiFactory.ApplyEnabledRecursive(cache.ContentHost, page.EnabledWhen());
                bool enabled;
                RegisterRefreshAction(() =>
                {
                    try
                    {
                        enabled = page.EnabledWhen();
                    }
                    catch
                    {
                        enabled = true;
                    }

                    ModSettingsUiFactory.ApplyEnabledRecursive(cache.HeaderHost, enabled);
                    ModSettingsUiFactory.ApplyEnabledRecursive(cache.ContentHost, enabled);
                }, ModSettingsUiRefreshSpec.Always, cache.PageKey);
            }

            if (!IsPageCurrentlySelected(cache))
                return;

            FlushRefreshActionsImmediate(emptyDirtyIsFullPass: false);
            RefreshSelectionState();
            RefreshFocusNavigation();
            CallDeferredIfAlive(ScrollToSelectedAnchor);
        }

        private ModSettingsPage? ResolveSelectedPage()
        {
            return ModSettingsRegistry.GetPages().FirstOrDefault(page =>
                string.Equals(page.ModId, _selectedModId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(page.Id, _selectedPageId, StringComparison.OrdinalIgnoreCase) &&
                IsPageVisibleOnCurrentHost(page));
        }

        private static bool IsPageVisibleOnCurrentHost(ModSettingsPage page)
        {
            return ModSettingsHostSurfaceResolver.IsVisibleOnCurrentHost(page.VisibleOnHostSurfaces);
        }

        private void RefreshPageHostLayout(PageContentCache cache)
        {
            if (!IsInstanceValid(cache.Root))
                return;

            cache.HeaderHost.UpdateMinimumSize();
            cache.ContentHost.UpdateMinimumSize();
            if (cache.HeaderHost is ModSettingsUiFactory.FastVerticalStack headerStack)
                headerStack.RequestLayout();
            if (cache.ContentHost is ModSettingsUiFactory.FastVerticalStack contentStack)
                contentStack.RequestLayout();
            if (cache.Root is ModSettingsUiFactory.FastVerticalStack rootStack)
                rootStack.RequestLayout();

            ApplyContentViewportWidth();
            _contentList.RequestLayout();
            _scrollContainer.QueueSort();
            RefreshContentLayout();
            QueueDeferredContentLayoutRefresh();
        }

        private void RecycleReusableEntryNodes(Node root)
        {
            foreach (var child in root.GetChildren().ToArray())
            {
                if (child is ModSettingsUiFactory.ReusableSettingLine line)
                {
                    _reusableEntryNodePool.Return(line);
                    continue;
                }

                if (child != null)
                    RecycleReusableEntryNodes(child);
            }
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

        private bool IsPageCurrentlySelected(PageContentCache cache)
        {
            var selectedPageKey = GetSelectedPageKey();
            return !string.IsNullOrWhiteSpace(selectedPageKey) &&
                   string.Equals(cache.PageKey, selectedPageKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreatePageCacheKey(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }

        private static string CreateSectionCacheKey(string modId, string pageId, string sectionId)
        {
            return $"{modId}::{pageId}::{sectionId}";
        }

        private static string CreateEntryCacheKey(string modId, string pageId, string sectionId, string entryId)
        {
            return $"{modId}::{pageId}::{sectionId}::{entryId}";
        }

        private static string SanitizePageNodeName(string text)
        {
            return text.Replace(':', '_');
        }

        private void QueueResizeLayoutRefresh()
        {
            if (!IsInsideTree())
                return;

            RefreshLayoutAfterResize();
            if (_resizeLayoutRefreshQueued)
                return;

            _resizeLayoutRefreshQueued = true;
            CallDeferredIfAlive(FlushResizeLayoutRefresh);
        }

        private void FlushResizeLayoutRefresh()
        {
            _resizeLayoutRefreshQueued = false;
            RefreshLayoutAfterResize();
        }

        private void RefreshLayoutAfterResize()
        {
            if (!IsInsideTree())
                return;

            if (_sidebarPanelRoot != null && IsInstanceValid(_sidebarPanelRoot))
                _sidebarPanelRoot.UpdateMinimumSize();
            if (_sidebarScrollContainer != null && IsInstanceValid(_sidebarScrollContainer))
                _sidebarScrollContainer.QueueSort();
            if (_modButtonList != null && IsInstanceValid(_modButtonList))
            {
                _modButtonList.UpdateMinimumSize();
                _modButtonList.QueueRedraw();
            }

            if (!IsInstanceValid(_contentList) || !IsInstanceValid(_scrollContainer))
                return;

            if (TryGetSelectedPageContentCache(out var cache))
                RefreshPageHostLayout(cache);
            else
                RefreshContentLayout();
        }

        private void RefreshContentLayout()
        {
            if (!IsInstanceValid(_contentList) || !IsInstanceValid(_scrollContainer))
                return;

            ApplyContentViewportWidth();
            _contentList.RequestLayout();
            _contentScrollContent.RequestLayout();

            _scrollContainer.QueueSort();
            if (_pendingScrollResetToTop)
                ResetContentScrollToTop();
            else
                _scrollContainer.ScrollVertical = Mathf.Max(0, _scrollContainer.ScrollVertical);
        }

        private void QueueDeferredContentLayoutRefresh()
        {
            if (_contentLayoutRefreshQueued)
                return;

            _contentLayoutRefreshQueued = true;
            CallDeferredIfAlive(FlushDeferredContentLayoutRefresh);
        }

        private void FlushDeferredContentLayoutRefresh()
        {
            _contentLayoutRefreshQueued = false;
            RefreshContentLayout();
        }

        private void ScheduleContentScrollResetToTop()
        {
            _pendingScrollResetToTop = true;
            _suppressScrollSync = true;
            ResetContentScrollToTop();
            _pendingScrollResetToTop = false;
            _suppressScrollSync = false;
        }

        private void CancelPendingContentScrollReset()
        {
            if (!_pendingScrollResetToTop)
                return;

            _pendingScrollResetToTop = false;
            _suppressScrollSync = false;
        }

        private void ResetContentScrollToTop()
        {
            if (!IsInstanceValid(_scrollContainer))
                return;

            _scrollContainer.ScrollHorizontal = 0;
            _scrollContainer.ScrollVertical = 0;
        }

        private void ApplyContentViewportWidth()
        {
            var viewportWidth = ResolveStableContentViewportWidth();
            if (viewportWidth <= 1f)
                return;

            var contentWidth = Mathf.Max(0f, viewportWidth - ResolveScrollbarContentRightGutter());
            _contentScrollContent.SetViewportSize(new(viewportWidth, ResolveStableContentViewportHeight()));
            _contentList.SetLayoutWidth(contentWidth);
        }

        private float ResolveStableContentViewportWidth()
        {
            if (_scrollContainer is { } scroll && IsInstanceValid(scroll) && scroll.Size.X > 1f)
                return scroll.Size.X;

            if (_scrollContainer.GetParent() is Control scrollParent && IsInstanceValid(scrollParent) &&
                scrollParent.Size.X > 1f)
                return scrollParent.Size.X;

            if (_contentPanelRoot is { } panel && IsInstanceValid(panel) && panel.Size.X > 28f)
                return Math.Max(0f, panel.Size.X - 28f);

            return 0f;
        }

        private float ResolveStableContentViewportHeight()
        {
            if (_scrollContainer is { } scroll && IsInstanceValid(scroll) && scroll.Size.Y > 1f)
                return scroll.Size.Y;

            if (_scrollContainer.GetParent() is Control scrollParent && IsInstanceValid(scrollParent) &&
                scrollParent.Size.Y > 1f)
                return scrollParent.Size.Y;

            if (_contentPanelRoot is { } panel && IsInstanceValid(panel) && panel.Size.Y > 28f)
                return Math.Max(0f, panel.Size.Y - 28f);

            return 0f;
        }

        private void ScrollToSelectedAnchor()
        {
            CancelPendingContentScrollReset();
            RefreshContentLayout();
            _suppressScrollSync = true;
            if (!string.IsNullOrWhiteSpace(_selectedSectionId))
                if (TryFindSectionAnchorOnSelectedPage(_selectedSectionId, out var target))
                {
                    AlignScrollToAnchor(target);
                    CallDeferredIfAlive(() => _suppressScrollSync = false);
                    return;
                }

            _scrollContainer.ScrollVertical = 0;
            CallDeferredIfAlive(() => _suppressScrollSync = false);
        }

        private void AlignScrollToAnchor(Control target)
        {
            CancelPendingContentScrollReset();
            RefreshContentLayout();
            var desired = TryComputeTargetOffsetInSelectedContent(target, out var targetTopInContent)
                ? Mathf.RoundToInt(targetTopInContent - 12f)
                : Mathf.RoundToInt(target.GlobalPosition.Y -
                    _scrollContainer.GlobalPosition.Y + _scrollContainer.ScrollVertical - 12f);
            _scrollContainer.ScrollVertical = Math.Max(0, desired);
        }

        private bool TryComputeTargetOffsetInSelectedContent(Control target, out float y)
        {
            y = 0f;
            if (!TryGetSelectedPageContentCache(out var cache))
                return false;

            var cursor = target;
            while (true)
            {
                y += cursor.Position.Y;
                var parent = cursor.GetParent();
                if (parent == cache.ContentHost)
                    return true;
                if (parent is not Control parentControl || !IsInstanceValid(parentControl))
                    return false;
                cursor = parentControl;
            }
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
                if (!TryFindSectionAnchorOnSelectedPage(section.Id, out var target))
                    continue;

                var distance = MathF.Abs(target.GlobalPosition.Y - viewportTop);
                if (!(distance < bestDistance)) continue;
                bestDistance = distance;
                bestSectionId = section.Id;
            }

            if (string.Equals(bestSectionId, _selectedSectionId, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedSectionId = bestSectionId;
            _modButtonList.SyncSelection(_selectedModId, GetSelectedPageKey(), GetSelectedSectionKey());
        }

        private bool TryFindSectionAnchorOnSelectedPage(string sectionId, out Control anchor)
        {
            anchor = null!;
            if (!TryGetSelectedPageContentCache(out var cache))
                return false;

            var node = cache.ContentHost.FindChild($"Section_{sectionId}", true, false);
            if (node is not Control control || !IsInstanceValid(control))
                return false;

            anchor = control;
            return true;
        }

        private bool TryFindEntryAnchorOnSelectedPage(string sectionId, string entryId, out Control anchor)
        {
            anchor = null!;
            if (string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId))
                return false;
            if (!TryGetSelectedPageContentCache(out var cache))
                return false;

            var key = CreateEntryCacheKey(_selectedModId, _selectedPageId, sectionId, entryId);
            if (!cache.EntryAnchors.TryGetValue(key, out var control) || !IsInstanceValid(control))
                return false;

            anchor = control;
            return true;
        }

        private bool TryGetSelectedPageContentCache(out PageContentCache cache)
        {
            cache = null!;
            var modId = _selectedModId;
            var pageId = _selectedPageId;
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(pageId))
                return false;

            if (!_pageContentCaches.TryGetValue(CreatePageCacheKey(modId, pageId), out var resolved) ||
                resolved == null)
                return false;

            cache = resolved;
            return true;
        }

        private static void ExpandSectionAnchor(Control anchor)
        {
            if (anchor is ModSettingsCollapsibleSection collapsible)
            {
                collapsible.Expand();
                return;
            }

            foreach (var child in anchor.GetChildren())
                if (child is Control control)
                    ExpandSectionAnchor(control);
        }

        private static void EnsureSectionContentBuilt(Control anchor)
        {
            if (anchor is ModSettingsCollapsibleSection collapsible)
            {
                collapsible.EnsureContentBuilt();
                return;
            }

            foreach (var child in anchor.GetChildren())
                if (child is Control control)
                    EnsureSectionContentBuilt(control);
        }

        private void FocusTarget(Control target)
        {
            var focusTarget = FindFirstFocusable(target);
            if (focusTarget != null)
                GrabControlDeferred(focusTarget);
        }

        private static Control? FindFirstFocusable(Control root)
        {
            if (root.IsVisibleInTree() && root.FocusMode == FocusModeEnum.All && IsSettingsFocusTerminal(root))
                return root;

            foreach (var child in root.GetChildren())
            {
                if (child is not Control control || !control.IsVisibleInTree())
                    continue;

                var candidate = FindFirstFocusable(control);
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private static void PulseTarget(Control target)
        {
            if (!IsInstanceValid(target))
                return;

            var original = target.Modulate;
            var highlight = new Color(1.35f, 1.18f, 0.65f, original.A);
            var tween = target.CreateTween();
            tween.TweenProperty(target, "modulate", highlight, 0.22d);
            tween.TweenInterval(0.18d);
            tween.TweenProperty(target, "modulate", original, 0.35d);
            tween.TweenInterval(0.12d);
            tween.TweenProperty(target, "modulate", highlight, 0.22d);
            tween.TweenInterval(0.18d);
            tween.TweenProperty(target, "modulate", original, 0.45d);
        }

        private void RefreshFocusNavigation()
        {
            if (_focusNavigationRefreshScheduled)
                return;
            _focusNavigationRefreshScheduled = true;
            CallDeferredIfAlive(FlushFocusNavigationDeferred);
        }

        private void FlushFocusNavigationDeferred()
        {
            _focusNavigationRefreshScheduled = false;
            if (!IsInstanceValid(this) || !Visible)
                return;

            ApplySplitPaneFocusNavigation();
            ApplySettingsFocusBehavior();
        }

        private void ApplySettingsFocusBehavior()
        {
            FocusBehaviorRecursive = Visible && IsInsideTree()
                ? FocusBehaviorRecursiveEnum.Enabled
                : FocusBehaviorRecursiveEnum.Disabled;
            if (_contentPanelRoot != null && IsInstanceValid(_contentPanelRoot))
                _contentPanelRoot.FocusBehaviorRecursive = Visible && IsInsideTree()
                    ? FocusBehaviorRecursiveEnum.Enabled
                    : FocusBehaviorRecursiveEnum.Disabled;
        }

        private void RebuildFocusChainsOnly()
        {
            _sidebarFocusChain.Clear();
            _contentFocusChain.Clear();
            CollectSettingsFocusChainPreorder(_sidebarPanelRoot, _sidebarFocusChain);
            if (!IsContentBuildOverlayVisible())
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
            foreach (var current in chain)
            {
                var selfPath = current.GetPath();
                current.FocusNeighborLeft = selfPath;
                current.FocusNeighborRight = selfPath;
                current.FocusNeighborTop = selfPath;
                current.FocusNeighborBottom = selfPath;
            }
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            var focusOwner = GetViewport()?.GuiGetFocusOwner();
            if (IsFocusNavigationBlocked() && !IsFocusUnderBlockingOverlay(focusOwner) && IsBlockedFocusInput(@event))
            {
                GetViewport()?.SetInputAsHandled();
                return;
            }

            if (TryHandleDirectionalFocusInput(@event))
                return;
            base._Input(@event);
        }

        private bool TryHandleDirectionalFocusInput(InputEvent @event)
        {
            if (!Visible || !IsInstanceValid(this))
                return false;
            if (!ActiveScreenContext.Instance.IsCurrent(this))
                return false;
            if (IsFocusNavigationBlocked())
                return false;

            int delta;
            if (@event.IsActionPressed("ui_up"))
                delta = -1;
            else if (@event.IsActionPressed("ui_down"))
                delta = 1;
            else
                return false;

            var owner = GetViewport()?.GuiGetFocusOwner();
            if (owner == null || !IsInstanceValid(owner))
                return false;

            if (IsFocusUnderPopupOrTransientWindow(owner))
                return false;

            if (owner is ModSettingsSidebarList sidebarList)
            {
                sidebarList.MoveActiveBy(delta);
                GetViewport()?.SetInputAsHandled();
                return true;
            }

            for (Node? n = owner; n != null && !ReferenceEquals(n, this); n = n.GetParent())
                if (n is IModSettingsDirectionalInputClaimant { ClaimsDirectionalInput: true })
                    return false;

            Control paneRoot;
            ScrollContainer paneScroll;
            if (_contentPanelRoot.IsAncestorOf(owner))
            {
                paneRoot = _contentPanelRoot;
                paneScroll = _scrollContainer;
            }
            else if (_sidebarPanelRoot.IsAncestorOf(owner))
            {
                paneRoot = _sidebarPanelRoot;
                paneScroll = _sidebarScrollContainer;
            }
            else
            {
                return false;
            }

            var focusables = new List<Control>();
            CollectSettingsFocusChainPreorder(paneRoot, focusables);
            if (focusables.Count == 0)
                return false;

            var currentIndex = focusables.IndexOf(owner);
            if (currentIndex < 0)
                currentIndex = ResolveNearestFocusIndex(focusables, owner, delta);
            if (currentIndex < 0)
                return false;

            var nextIndex = currentIndex + delta;
            if (nextIndex >= 0 && nextIndex < focusables.Count)
            {
                var target = focusables[nextIndex];
                target.GrabFocus();
                if (!IsInstanceValid(paneScroll))
                {
                    GetViewport()?.SetInputAsHandled();
                    return true;
                }

                if (ReferenceEquals(paneScroll, _scrollContainer))
                    Callable.From(() =>
                    {
                        if (!IsInstanceValid(paneScroll) || !IsInstanceValid(target))
                            return;

                        paneScroll.EnsureControlVisible(target);
                    }).CallDeferred();
                else
                    paneScroll.EnsureControlVisible(target);
            }

            GetViewport()?.SetInputAsHandled();
            return true;
        }

        private static int ResolveNearestFocusIndex(IReadOnlyList<Control> focusables, Control owner, int delta)
        {
            for (var i = 0; i < focusables.Count; i++)
            {
                var candidate = focusables[i];
                if (candidate == owner || candidate.IsAncestorOf(owner) || owner.IsAncestorOf(candidate))
                    return i;
            }

            var ownerCenterY = owner.GetGlobalRect().GetCenter().Y;
            if (delta > 0)
            {
                for (var i = focusables.Count - 1; i >= 0; i--)
                    if (focusables[i].GetGlobalRect().GetCenter().Y <= ownerCenterY)
                        return i;
                return -1;
            }

            for (var i = 0; i < focusables.Count; i++)
                if (focusables[i].GetGlobalRect().GetCenter().Y >= ownerCenterY)
                    return i;

            return -1;
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
                ModSettingsSidebarList => true,
                ModSettingsSidebarButton or ModSettingsTextButton or ModSettingsCollapsibleHeaderButton
                    or ModSettingsToggleControl or ModSettingsMiniButton or ModSettingsDragHandle
                    or ModSettingsActionsButton or NButton
                    or HSlider or OptionButton or ColorPickerButton or MenuButton => true,
                LineEdit or TextEdit => false,
                _ => c is Button,
            };
        }

        private bool IsFocusNavigationBlocked()
        {
            if (IsBlockingOverlayVisible(GetTree()?.Root))
                return true;

            var focusOwner = GetViewport()?.GuiGetFocusOwner();
            return IsFocusUnderPopupOrTransientWindow(focusOwner);
        }

        private static bool IsFocusUnderBlockingOverlay(Control? c)
        {
            for (Node? n = c; n != null; n = n.GetParent())
                if (n is CanvasLayer layer && IsBlockingOverlayLayer(layer))
                    return true;

            return false;
        }

        private static bool IsBlockingOverlayVisible(Node? root)
        {
            if (root == null)
                return false;

            foreach (var child in root.GetChildren())
            {
                if (child is CanvasLayer layer && IsBlockingOverlayLayer(layer) && layer.Visible)
                    return true;
                if (IsBlockingOverlayVisible(child))
                    return true;
            }

            return false;
        }

        private static bool IsBlockingOverlayLayer(CanvasLayer layer)
        {
            var name = layer.Name.ToString();
            return name.StartsWith("RitsuModSettings", StringComparison.Ordinal) ||
                   (name.StartsWith("Ritsu", StringComparison.Ordinal) &&
                    (name.Contains("Progress", StringComparison.Ordinal) ||
                     name.Contains("Modal", StringComparison.Ordinal)));
        }

        private static bool IsBlockedFocusInput(InputEvent @event)
        {
            if (@event.IsEcho())
                return false;

            return @event.IsActionPressed("ui_up") ||
                   @event.IsActionPressed("ui_down") ||
                   @event.IsActionPressed("ui_left") ||
                   @event.IsActionPressed("ui_right") ||
                   @event.IsActionPressed("ui_accept") ||
                   @event.IsActionPressed("ui_cancel") ||
                   @event.IsActionPressed(MegaInput.left) ||
                   @event.IsActionPressed(MegaInput.right) ||
                   @event.IsActionPressed(MegaInput.select) ||
                   @event.IsActionPressed(MegaInput.accept) ||
                   @event.IsActionPressed(MegaInput.cancel) ||
                   @event.IsActionPressed(MegaInput.pauseAndBack) ||
                   @event.IsActionPressed(PaneSidebarHotkey) ||
                   @event.IsActionPressed(PaneContentHotkey);
        }

        private void ApplyStaticTexts()
        {
        }

        private void ExpandOnlyMod(string? modId)
        {
            _expandedModIds.Clear();
            if (!string.IsNullOrWhiteSpace(modId))
                _expandedModIds.Add(modId);
        }

        private bool SelectedPageContentReady()
        {
            if (string.IsNullOrWhiteSpace(_selectedModId) || string.IsNullOrWhiteSpace(_selectedPageId))
                return true;

            var key = CreatePageCacheKey(_selectedModId, _selectedPageId);
            if (!_pageContentCaches.TryGetValue(key, out var cache))
                return true;

            return cache.State is PageBuildState.Ready or PageBuildState.Failed;
        }

        private bool ShouldShowExpandedModNav(string modId)
        {
            return _expandedModIds.Contains(modId) && SelectedPageContentReady();
        }

        private void FlushDirtyBindings()
        {
            if (_dirtyBindings.Count == 0)
            {
                _saveTimer = -1;
                return;
            }

            var roots = ModSettingsBindingFlushPlanner.SelectEffectiveSaveRoots(_dirtyBindings);
            foreach (var binding in roots)
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
            ModSettingsRegistry.InvalidateOrderingCache();
            _sidebarStructureDirty = true;
            _contentStructureDirty = true;
            _selectionDirty = true;
            CallDeferredIfAlive(() => EnsureUiUpToDate(true, true));
        }

        private void OnShellThemeChanged()
        {
            CallDeferredIfAlive(() =>
            {
                ResetUiCachesForShellThemeChange();
                ApplyShellThemeToExistingChrome();
                EnsureUiUpToDate(true, true);
            });
        }

        private void ApplyShellThemeToExistingChrome()
        {
            if (!IsInsideTree())
                return;

            if (_sidebarPanelRoot != null && IsInstanceValid(_sidebarPanelRoot))
                _sidebarPanelRoot.AddThemeStyleboxOverride("panel",
                    RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Sidebar,
                        RitsuShellTheme.Current.Metric.Radius.Default));

            if (_contentPanelRoot != null && IsInstanceValid(_contentPanelRoot))
                _contentPanelRoot.AddThemeStyleboxOverride("panel",
                    RitsuShellPanelStyles.CreateFramedSurface(RitsuShellTheme.Current.Surface.Content,
                        RitsuShellTheme.Current.Metric.Radius.Default));

            if (_sidebarHeaderCard != null && IsInstanceValid(_sidebarHeaderCard))
                _sidebarHeaderCard.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());

            if (_sidebarHeaderTitleLabel != null && IsInstanceValid(_sidebarHeaderTitleLabel))
                _sidebarHeaderTitleLabel.Modulate = RitsuShellTheme.Current.Text.SidebarSection;

            if (_sidebarHeaderSubtitleLabel != null && IsInstanceValid(_sidebarHeaderSubtitleLabel))
                _sidebarHeaderSubtitleLabel.Modulate = RitsuShellTheme.Current.Text.RichSecondary;

            if (_sidebarScrollContainer != null && IsInstanceValid(_sidebarScrollContainer))
                ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(_sidebarScrollContainer);

            if (_scrollContainer != null && IsInstanceValid(_scrollContainer))
                ModSettingsUiControlTheming.ApplySettingsScrollContainerTheme(_scrollContainer);

            RitsuShellTooltipTheme.ApplyToTreeRoot(this);
        }

        private static int ResolveScrollbarContentRightGutter()
        {
            return RitsuShellThemeLayoutResolver.ResolveInt(ScrollbarContentRightGutterTokenPath, 12);
        }

        private void ResetUiCachesForShellThemeChange()
        {
            _sidebarStructureDirty = true;
            _contentStructureDirty = true;
            _selectionDirty = true;

            foreach (var cache in _pageContentCaches.Values)
            {
                CancelPageBuild(cache);
                if (IsInstanceValid(cache.Root))
                    cache.Root.QueueFree();
            }

            _pageContentCaches.Clear();

            _globalDynamicVisibilityTargets.Clear();
            _globalRefreshRegistrations.Clear();
            HideTransientContentState();
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

            label.AddThemeFontOverride("normal_font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontOverride("bold_font", RitsuShellTheme.Current.Font.BodyBold);
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

        private static string CreatePageStructureSignature(ModSettingsPage page)
        {
            var builder = new StringBuilder();
            builder.Append(page.Id).Append('|')
                .Append(page.ModId).Append('|')
                .Append(page.ParentPageId ?? string.Empty).Append('|')
                .Append(page.SidebarVisibleOnlyWhenActive ? '1' : '0').Append('|')
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

        private sealed record ModSettingsSidebarRow(
            ModSettingsSidebarItemKind Kind,
            string ModId,
            string? PageId,
            string? SectionId,
            string Key,
            string Label,
            string? Prefix,
            int Depth,
            string? Meta,
            Action Activate,
            Func<bool>? VisibleWhen)
        {
            public bool Selected { get; set; }

            public bool Visible { get; set; } = true;
        }

        private sealed partial class ModSettingsSidebarList : Control
        {
            private readonly RitsuModSettingsSubmenu _owner = null!;
            private readonly List<ModSettingsSidebarRow> _rows = [];
            private int _activeVisibleIndex;
            private bool _hovered;

            public ModSettingsSidebarList(RitsuModSettingsSubmenu owner)
            {
                _owner = owner;
                FocusMode = FocusModeEnum.All;
                MouseFilter = MouseFilterEnum.Stop;
                ClipContents = false;
            }

            public ModSettingsSidebarList()
            {
            }

            public void ClearRows()
            {
                _rows.Clear();
                _activeVisibleIndex = 0;
                TooltipText = string.Empty;
                UpdateMinimumSize();
                RequestScrollContainerLayout();
                QueueRedraw();
            }

            public void SetRows(IEnumerable<ModSettingsSidebarRow> rows)
            {
                _rows.Clear();
                _rows.AddRange(rows);
                RefreshRows(false);
                SyncSelection(_owner._selectedModId, _owner.GetSelectedPageKey(), _owner.GetSelectedSectionKey());
                RequestScrollContainerLayout();
            }

            public void SyncSelection(string? selectedModId, string? selectedPageKey, string? selectedSectionKey)
            {
                var selectedVisibleIndex = -1;
                var visibleIndex = 0;
                foreach (var row in _rows)
                {
                    row.Selected = row.Kind switch
                    {
                        ModSettingsSidebarItemKind.ModGroup =>
                            string.Equals(row.ModId, selectedModId, StringComparison.OrdinalIgnoreCase),
                        ModSettingsSidebarItemKind.Page =>
                            string.Equals(row.Key, selectedPageKey, StringComparison.OrdinalIgnoreCase),
                        ModSettingsSidebarItemKind.Section =>
                            string.Equals(row.Key, selectedSectionKey, StringComparison.OrdinalIgnoreCase),
                        _ => false,
                    };
                    if (!row.Visible)
                        continue;
                    if (row.Selected && selectedVisibleIndex < 0)
                        selectedVisibleIndex = visibleIndex;
                    visibleIndex++;
                }

                if (selectedVisibleIndex >= 0)
                    _activeVisibleIndex = selectedVisibleIndex;
                ClampActiveIndex();
                UpdateTooltip();
                UpdateMinimumSize();
                RequestScrollContainerLayout();
                QueueRedraw();
            }

            public void RefreshRows(bool redraw = true)
            {
                foreach (var row in _rows)
                {
                    bool visible;
                    try
                    {
                        visible = row.VisibleWhen?.Invoke() ?? true;
                    }
                    catch
                    {
                        visible = true;
                    }

                    row.Visible = visible;
                }

                ClampActiveIndex();
                if (!redraw)
                    return;
                UpdateTooltip();
                UpdateMinimumSize();
                RequestScrollContainerLayout();
                QueueRedraw();
            }

            public void MoveActiveBy(int delta)
            {
                RefreshRows(false);
                var count = VisibleRowCount();
                if (count == 0)
                    return;

                _activeVisibleIndex = Mathf.Clamp(_activeVisibleIndex + delta, 0, count - 1);
                EnsureActiveVisible();
                UpdateTooltip();
                QueueRedraw();
            }

            public void EnsureActiveRowVisible()
            {
                EnsureActiveVisible();
            }

            public override Vector2 _GetMinimumSize()
            {
                RefreshRows(false);
                var height = 0f;
                var visible = 0;
                foreach (var row in _rows.Where(row => row.Visible))
                {
                    if (visible > 0)
                        height += ResolveSeparation(row.Kind);
                    height += ResolveRowHeight(row);
                    visible++;
                }

                return new(1f, height);
            }

            public override void _Notification(int what)
            {
                base._Notification(what);
                switch (what)
                {
                    case (int)NotificationMouseEnter:
                        _hovered = true;
                        QueueRedraw();
                        break;
                    case (int)NotificationMouseExit:
                        _hovered = false;
                        QueueRedraw();
                        break;
                    case (int)NotificationFocusEnter:
                        EnsureActiveVisible();
                        QueueRedraw();
                        break;
                    case (int)NotificationFocusExit:
                    case (int)NotificationThemeChanged:
                        QueueRedraw();
                        break;
                }
            }

            public override void _GuiInput(InputEvent @event)
            {
                switch (@event)
                {
                    case InputEventMouseMotion motion:
                        SetActiveFromY(motion.Position.Y, false);
                        return;
                    case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse:
                        if (SetActiveFromY(mouse.Position.Y, true))
                            ActivateCurrent();
                        AcceptEvent();
                        return;
                }

                if (!@event.IsEcho() &&
                    (@event.IsActionPressed(MegaInput.select) || @event.IsActionPressed(MegaInput.accept) ||
                     @event.IsActionPressed("ui_accept")))
                {
                    ActivateCurrent();
                    AcceptEvent();
                    return;
                }

                base._GuiInput(@event);
            }

            public override void _Draw()
            {
                RefreshRows(false);
                var y = 0f;
                var visibleIndex = 0;
                foreach (var row in _rows.Where(row => row.Visible))
                {
                    if (visibleIndex > 0)
                        y += ResolveSeparation(row.Kind);
                    var h = ResolveRowHeight(row);
                    DrawRow(row, visibleIndex, new(0f, y, Size.X, h));
                    y += h;
                    visibleIndex++;
                }
            }

            private void DrawRow(ModSettingsSidebarRow row, int visibleIndex, Rect2 rect)
            {
                var active = HasFocus() && visibleIndex == _activeVisibleIndex;
                var highlighted = active || row.Selected;
                DrawStyleBox(active
                        ? ModSettingsSidebarButton.CreateFocusStyle(row.Selected, row.Kind, row.Depth)
                        : ModSettingsSidebarButton.CreateStyle(row.Selected, _hovered && active, row.Kind, row.Depth),
                    rect);

                var font = row.Kind == ModSettingsSidebarItemKind.ModGroup
                    ? RitsuShellTheme.Current.Font.BodyBold
                    : RitsuShellTheme.Current.Font.Body;
                var fontSize = row.Kind switch
                {
                    ModSettingsSidebarItemKind.ModGroup => 22,
                    ModSettingsSidebarItemKind.Page => 19,
                    ModSettingsSidebarItemKind.Section => 16,
                    _ => 17,
                };
                var style = ModSettingsSidebarButton.CreateStyle(row.Selected, active, row.Kind, row.Depth);
                var left = style.ContentMarginLeft + ResolveTextLeftInset(row.Kind);
                var right = style.ContentMarginRight;
                var textX = rect.Position.X + left;
                var textWidth = Math.Max(1f, rect.Size.X - left - right);
                var label = string.IsNullOrWhiteSpace(row.Prefix) ? row.Label : $"{row.Prefix}  {row.Label}";
                var color = row.Kind == ModSettingsSidebarItemKind.Section
                    ? RitsuShellTheme.Current.Text.SidebarSection
                    : highlighted
                        ? RitsuShellTheme.Current.Text.HoverHighlight
                        : RitsuShellTheme.Current.Text.LabelPrimary;

                const int metaSize = 14;
                const float metaGap = 1f;
                var metaFont = RitsuShellTheme.Current.Font.Body;
                var fontHeight = font.GetHeight(fontSize);
                var baseline = ResolveCenteredTextBaseline(font, fontSize, rect.Position.Y, rect.Size.Y);
                if (row.Kind == ModSettingsSidebarItemKind.ModGroup &&
                    !string.IsNullOrWhiteSpace(row.Meta) &&
                    RitsuShellTheme.Current.Metric.Sidebar.ShowInlinePageCount)
                {
                    var metaHeight = metaFont.GetHeight(metaSize);
                    var totalHeight = fontHeight + metaGap + metaHeight;
                    var textTop = rect.Position.Y + Math.Max(0f, (rect.Size.Y - totalHeight) * 0.5f);
                    baseline = textTop + font.GetAscent(fontSize);
                }

                DrawString(font, new(textX, baseline), TrimToWidth(font, label, fontSize, textWidth),
                    HorizontalAlignment.Left, textWidth, fontSize, color);

                if (row.Kind != ModSettingsSidebarItemKind.ModGroup ||
                    string.IsNullOrWhiteSpace(row.Meta) ||
                    !RitsuShellTheme.Current.Metric.Sidebar.ShowInlinePageCount)
                    return;

                var metaColor = highlighted
                    ? RitsuShellTheme.Current.Text.HoverHighlight
                    : RitsuShellTheme.Current.Text.RichSecondary;
                DrawString(RitsuShellTheme.Current.Font.Body,
                    new(textX, baseline + (fontHeight - font.GetAscent(fontSize)) + metaGap +
                               metaFont.GetAscent(metaSize)),
                    TrimToWidth(metaFont, row.Meta, metaSize, textWidth),
                    HorizontalAlignment.Left, textWidth, metaSize, metaColor);
            }

            private bool SetActiveFromY(float y, bool requireHit)
            {
                var range = ResolveVisibleRowIndexAt(y);
                if (range < 0)
                    return !requireHit;

                _activeVisibleIndex = range;
                UpdateTooltip();
                QueueRedraw();
                return true;
            }

            private int ResolveVisibleRowIndexAt(float localY)
            {
                var y = 0f;
                var visibleIndex = 0;
                foreach (var row in _rows.Where(row => row.Visible))
                {
                    if (visibleIndex > 0)
                        y += ResolveSeparation(row.Kind);
                    var h = ResolveRowHeight(row);
                    if (localY >= y && localY <= y + h)
                        return visibleIndex;
                    y += h;
                    visibleIndex++;
                }

                return -1;
            }

            private void ActivateCurrent()
            {
                var row = GetActiveRow();
                if (row == null)
                    return;

                try
                {
                    row.Activate();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[ModSettingsSidebarList] action failed: {ex.Message}");
                }
            }

            private ModSettingsSidebarRow? GetActiveRow()
            {
                var visibleIndex = 0;
                foreach (var row in _rows.Where(row => row.Visible))
                {
                    if (visibleIndex == _activeVisibleIndex)
                        return row;
                    visibleIndex++;
                }

                return null;
            }

            private int VisibleRowCount()
            {
                return _rows.Count(row => row.Visible);
            }

            private void ClampActiveIndex()
            {
                var count = VisibleRowCount();
                _activeVisibleIndex = count <= 0 ? 0 : Mathf.Clamp(_activeVisibleIndex, 0, count - 1);
            }

            private void EnsureActiveVisible()
            {
                if (FindAncestorScrollContainer(this) is not { } scroll)
                    return;

                var top = ResolveVisibleRowTop(_activeVisibleIndex);
                var height = ResolveActiveRowHeight();
                var bottom = top + height;
                var viewTop = scroll.ScrollVertical;
                var viewBottom = viewTop + scroll.Size.Y;
                if (top < viewTop)
                    scroll.ScrollVertical = Mathf.RoundToInt(top);
                else if (bottom > viewBottom)
                    scroll.ScrollVertical = Mathf.RoundToInt(Mathf.Max(0f, bottom - scroll.Size.Y));
            }

            private float ResolveVisibleRowTop(int visibleIndex)
            {
                var y = 0f;
                var index = 0;
                foreach (var row in _rows.Where(row => row.Visible))
                {
                    if (index > 0)
                        y += ResolveSeparation(row.Kind);
                    if (index == visibleIndex)
                        return y;
                    y += ResolveRowHeight(row);
                    index++;
                }

                return 0f;
            }

            private float ResolveActiveRowHeight()
            {
                return GetActiveRow() is { } row ? ResolveRowHeight(row) : 0f;
            }

            private void UpdateTooltip()
            {
                TooltipText = GetActiveRow() is { } row && !string.IsNullOrWhiteSpace(row.Meta) &&
                              row.Kind == ModSettingsSidebarItemKind.ModGroup &&
                              !RitsuShellTheme.Current.Metric.Sidebar.ShowInlinePageCount
                    ? $"{row.Label}\n{row.Meta}"
                    : GetActiveRow()?.Label ?? string.Empty;
            }

            private void RequestScrollContainerLayout(bool defer = true)
            {
                UpdateMinimumSize();
                if (GetParent() is Control parent)
                {
                    parent.UpdateMinimumSize();
                    if (parent is Container container)
                        container.QueueSort();
                }

                if (FindAncestorScrollContainer(this) is { } scroll)
                    scroll.QueueSort();

                if (defer)
                    Callable.From(DeferredRequestScrollContainerLayout).CallDeferred();
            }

            private void DeferredRequestScrollContainerLayout()
            {
                if (!IsInsideTree())
                    return;

                RequestScrollContainerLayout(false);
            }

            private static float ResolveRowHeight(ModSettingsSidebarRow row)
            {
                return row.Kind switch
                {
                    ModSettingsSidebarItemKind.ModGroup =>
                        RitsuShellTheme.Current.Metric.Sidebar.ShowInlinePageCount ? 64f : 52f,
                    ModSettingsSidebarItemKind.Page => RitsuShellTheme.Current.Metric.Sidebar.PageRowMinHeight,
                    ModSettingsSidebarItemKind.Section => RitsuShellTheme.Current.Metric.Sidebar.SectionRowMinHeight,
                    _ => 44f,
                };
            }

            private static float ResolveTextLeftInset(ModSettingsSidebarItemKind kind)
            {
                return kind switch
                {
                    ModSettingsSidebarItemKind.Page => 10f,
                    ModSettingsSidebarItemKind.Section => 14f,
                    _ => 0f,
                };
            }

            private static float ResolveCenteredTextBaseline(Font font, int fontSize, float y, float height)
            {
                return y + Math.Max(0f, (height - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize);
            }

            private static float ResolveSeparation(ModSettingsSidebarItemKind kind)
            {
                return kind switch
                {
                    ModSettingsSidebarItemKind.ModGroup => RitsuShellTheme.Current.Metric.Sidebar.ModListSeparation,
                    ModSettingsSidebarItemKind.Section => RitsuShellTheme.Current.Metric.Sidebar.SectionRailSeparation,
                    _ => RitsuShellTheme.Current.Metric.Sidebar.PageTreeSeparation,
                };
            }

            private static string TrimToWidth(Font font, string text, int fontSize, float maxWidth)
            {
                if (string.IsNullOrEmpty(text) ||
                    font.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize).X <= maxWidth)
                    return text;

                const string ellipsis = "...";
                var lo = 0;
                var hi = text.Length;
                while (lo < hi)
                {
                    var mid = (lo + hi + 1) / 2;
                    var candidate = text[..mid] + ellipsis;
                    if (font.GetStringSize(candidate, HorizontalAlignment.Left, -1f, fontSize).X <= maxWidth)
                        lo = mid;
                    else
                        hi = mid - 1;
                }

                return text[..Math.Max(0, lo)] + ellipsis;
            }

            private static ScrollContainer? FindAncestorScrollContainer(Node node)
            {
                for (var current = node.GetParent(); current != null; current = current.GetParent())
                    if (current is ScrollContainer scroll)
                        return scroll;

                return null;
            }
        }

        private sealed class PageContentCache
        {
            public required int BuildVersion { get; set; }
            public CancellationTokenSource? BuildCancellation { get; set; }
            public Task? BuildTask { get; set; }
            public required Control HeaderHost { get; set; }
            public required Control ContentHost { get; set; }
            public required string PageId { get; init; }
            public required string PageKey { get; init; }
            public required Control Root { get; init; }
            public required PageBuildState State { get; set; }
            public ulong LastUsedMsec { get; set; }
            public List<ModSettingsRefreshRegistration> RefreshRegistrations { get; } = [];
            public List<(Control Control, Func<bool> Predicate)> VisibilityTargets { get; } = [];
            public Dictionary<string, Control> EntryAnchors { get; } = new(StringComparer.OrdinalIgnoreCase);
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
