using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Data;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Ui.Overlay
{
    internal static class RitsuOverlayHostService
    {
        private static readonly Lock SyncRoot = new();
        private static IRuntimeHotkeyHandle? _creaturePickerHotkey;
        private static RitsuOverlayHost? _host;
        private static IRuntimeHotkeyHandle? _debugToolsHotkey;
        private static IRuntimeHotkeyHandle? _settingsHotkey;
        private static IDisposable? _lifecycleSubscription;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                _lifecycleSubscription ??= RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
                {
                    lock (SyncRoot)
                    {
                        EnsureAttached(evt.Game);
                        EnsureHotkeysRegistered();
                    }
                });
                EnsureAttached(NGame.Instance);
                EnsureHotkeysRegistered();
            }
        }

        internal static bool TryOpenSettings(out RitsuModSettingsSubmenu submenu, out string error)
        {
            lock (SyncRoot)
            {
                EnsureAttached(NGame.Instance);
                if (_host == null || !GodotObject.IsInstanceValid(_host))
                {
                    submenu = null!;
                    error = ModSettingsLocalization.Get(
                        "ritsulib.settings.notAvailableYet",
                        "RitsuLib settings are not available yet. Try again after the current screen finishes loading.");
                    return false;
                }

                submenu = _host.OpenSettings();
                error = string.Empty;
                return true;
            }
        }

        internal static bool TryOpenDebugTools(out string message)
        {
            if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
            {
                message = ModSettingsLocalization.Get(
                    "ritsulib.debugTools.disabled",
                    "Enable developer tools in RitsuLib settings before opening this workspace.");
                return false;
            }

            lock (SyncRoot)
            {
                EnsureAttached(NGame.Instance);
                if (_host == null || !GodotObject.IsInstanceValid(_host))
                {
                    message = ModSettingsLocalization.Get(
                        "ritsulib.debugTools.notAvailableYet",
                        "RitsuLib developer tools are not available yet. Try again after the current screen finishes loading.");
                    return false;
                }

                _host.OpenDebugTools();
                message = ModSettingsLocalization.Get(
                    "ritsulib.debugTools.opened",
                    "Opened RitsuLib developer tools.");
                return true;
            }
        }

        internal static bool TryGetActiveScreen(out IScreenContext screen)
        {
            lock (SyncRoot)
            {
                if (_host is { } host && GodotObject.IsInstanceValid(host) && host.ActiveScreen is { } activeScreen)
                {
                    screen = activeScreen;
                    return true;
                }

                screen = null!;
                return false;
            }
        }

        internal static void TryAttachHoverTips(Control owner, NHoverTipSet? hoverTips)
        {
            if (hoverTips == null || !GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(hoverTips))
                return;

            lock (SyncRoot)
            {
                if (_host is { } host && GodotObject.IsInstanceValid(host) && host.IsAncestorOf(owner))
                    host.AttachHoverTips(hoverTips);
            }
        }

        internal static void TryRebindDebugToolsHotkey(string binding)
        {
            lock (SyncRoot)
            {
                if (_debugToolsHotkey == null)
                    return;
                if (!_debugToolsHotkey.TryRebind(binding, out _))
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not apply runtime hotkey binding '{binding}'.");
            }
        }

        internal static void TryRebindCreaturePickerHotkey(string binding)
        {
            lock (SyncRoot)
            {
                if (_creaturePickerHotkey == null)
                    return;
                if (!_creaturePickerHotkey.TryRebind(binding, out _))
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not apply creature-picker hotkey binding '{binding}'.");
            }
        }

        internal static void TryRebindSettingsHotkey(string binding)
        {
            lock (SyncRoot)
            {
                if (_settingsHotkey == null)
                    return;
                if (!_settingsHotkey.TryRebind(binding, out _))
                    RitsuLibFramework.Logger.Warn($"[Settings] Could not apply runtime hotkey binding '{binding}'.");
            }
        }

        internal static void NotifyDeveloperToolsAvailabilityChanged(bool enabled)
        {
            lock (SyncRoot)
            {
                if (_host is { } host && GodotObject.IsInstanceValid(host))
                    host.SetDeveloperToolsAvailable(enabled);
            }
        }

        private static void EnsureAttached(Node? game)
        {
            if (_host != null && GodotObject.IsInstanceValid(_host))
                return;
            if (game == null || !GodotObject.IsInstanceValid(game))
                return;

            _host = new() { Name = "RitsuOverlayHost" };
            game.AddChild(_host);
        }

        internal static void NotifyHostExited(RitsuOverlayHost host)
        {
            lock (SyncRoot)
            {
                if (ReferenceEquals(_host, host))
                    _host = null;
            }
        }

        private static void EnsureHotkeysRegistered()
        {
            if (_settingsHotkey == null)
                try
                {
                    _settingsHotkey = RuntimeHotkeyService.Register(RitsuLibSettingsStore.GetSettingsOpenHotkey(),
                        () => { _ = ModSettingsNavigator.RequestOpenByIds(Const.ModId, null, null, null); }, new()
                        {
                            Id = "ritsulib.settings.open",
                            DisplayName = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get("ritsulib.settings.hotkey.label", "Open mod settings")),
                            Description = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get("ritsulib.settings.hotkey.description",
                                    "Open the independent mod settings center.")),
                            Category = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get("ritsulib.category.core.label", "Core settings")),
                            Purpose = "settings",
                            SuppressWhenTextInputFocused = true,
                            SuppressWhenDevConsoleVisible = true,
                            MarkInputHandled = true,
                            DebugName = "RitsuLib mod settings",
                        });
                }
                catch (InvalidOperationException)
                {
                    // GameReady will retry after the runtime input router has a game root.
                }

            if (_debugToolsHotkey == null)
                try
                {
                    _debugToolsHotkey = RuntimeHotkeyService.Register(
                        RitsuLibSettingsStore.GetDebugToolsOpenHotkey(),
                        () =>
                        {
                            lock (SyncRoot)
                            {
                                EnsureAttached(NGame.Instance);
                                if (_host is not { } host || !GodotObject.IsInstanceValid(host))
                                    return;
                                if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                                {
                                    RitsuToastService.ShowWarning(
                                        ModSettingsLocalization.Get(
                                            "ritsulib.debugTools.disabled",
                                            "Enable developer tools in RitsuLib settings before opening this workspace."),
                                        ModSettingsLocalization.Get(
                                            "ritsulib.debugTools.toastTitle",
                                            "Developer tools"));
                                    return;
                                }

                                host.ToggleDebugTools();
                            }
                        },
                        new()
                        {
                            Id = "ritsulib.debug-tools.open",
                            DisplayName = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get(
                                    "ritsulib.debugTools.hotkey.label",
                                    "Toggle developer tools")),
                            Description = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get(
                                    "ritsulib.debugTools.hotkey.description",
                                    "Show or close the developer tools workspace.")),
                            Category = RuntimeHotkeyText.Dynamic(() =>
                                ModSettingsLocalization.Get("ritsulib.category.developerTools.label",
                                    "Developer tools")),
                            Purpose = "debug-tools",
                            SuppressWhenTextInputFocused = false,
                            SuppressWhenDevConsoleVisible = true,
                            MarkInputHandled = true,
                            DebugName = "RitsuLib developer tools",
                        });
                }
                catch (InvalidOperationException)
                {
                    // GameReady will retry after the runtime input router has a game root.
                }

            if (_creaturePickerHotkey != null)
                return;
            try
            {
                _creaturePickerHotkey = RuntimeHotkeyService.Register(
                    RitsuLibSettingsStore.GetCreaturePickerHotkey(),
                    () =>
                    {
                        lock (SyncRoot)
                        {
                            EnsureAttached(NGame.Instance);
                            if (_host is not { } host || !GodotObject.IsInstanceValid(host))
                                return;
                            if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                            {
                                RitsuToastService.ShowWarning(
                                    ModSettingsLocalization.Get(
                                        "ritsulib.debugTools.disabled",
                                        "Enable developer tools in RitsuLib settings before using this shortcut."),
                                    ModSettingsLocalization.Get(
                                        "ritsulib.debugTools.toastTitle",
                                        "Developer tools"));
                                return;
                            }

                            host.ToggleCreaturePicking();
                        }
                    },
                    new()
                    {
                        Id = "ritsulib.debug-tools.pick-creature",
                        DisplayName = RuntimeHotkeyText.Dynamic(() =>
                            ModSettingsLocalization.Get(
                                "ritsulib.debugTools.creaturePickerHotkey.label",
                                "Pick a combat creature")),
                        Description = RuntimeHotkeyText.Dynamic(() =>
                            ModSettingsLocalization.Get(
                                "ritsulib.debugTools.creaturePickerHotkey.description",
                                "Start creature picking directly during combat.")),
                        Category = RuntimeHotkeyText.Dynamic(() =>
                            ModSettingsLocalization.Get("ritsulib.category.developerTools.label", "Developer tools")),
                        Purpose = "debug-tools",
                        SuppressWhenTextInputFocused = false,
                        SuppressWhenDevConsoleVisible = true,
                        MarkInputHandled = true,
                        DebugName = "RitsuLib combat creature picker",
                    });
            }
            catch (InvalidOperationException)
            {
                // GameReady will retry after the runtime input router has a game root.
            }
        }
    }

    internal sealed partial class RitsuOverlayHost : CanvasLayer
    {
        private const int OverlayLayer = 180;
        private bool _backgroundFocusCaptured;
        private RitsuDebugToolsDock? _debugToolsDock;
        private RitsuDebugToolsPanel? _debugToolsPanel;
        private ColorRect _fixedBackdrop = null!;
        private RitsuOverlaySubmenuStack _fixedStack = null!;
        private Control _floatingWindows = null!;
        private Control _hoverTips = null!;
        private Control? _previousFocus;
        private bool _switchingWorkspace;

        internal IScreenContext? ActiveScreen
        {
            get
            {
                if (_fixedStack is { SubmenusOpen: true })
                    return _fixedStack.Peek();
                return _debugToolsDock is { Expanded: true } ? _debugToolsPanel : null;
            }
        }

        public override void _Ready()
        {
            Layer = OverlayLayer;
            BuildLayout();
        }

        public override void _ExitTree()
        {
            if (_fixedStack != null)
                _fixedStack.StackModified -= OnFixedStackModified;
            if (_debugToolsDock != null && IsInstanceValid(_debugToolsDock))
            {
                _debugToolsDock.Expanding -= OnDebugToolsExpanding;
                _debugToolsDock.Collapsed -= OnDebugToolsCollapsed;
            }

            RitsuOverlayHostService.NotifyHostExited(this);
            base._ExitTree();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsEcho() ||
                !(@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
                return;

            if (_fixedStack.SubmenusOpen)
            {
                _fixedStack.Pop();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_debugToolsDock is not { Expanded: true })
                return;
            _debugToolsDock.Collapse();
            GetViewport().SetInputAsHandled();
        }

        internal RitsuModSettingsSubmenu OpenSettings()
        {
            CaptureBackgroundFocus();
            _switchingWorkspace = true;
            try
            {
                _debugToolsDock?.SetSuppressed(true);
                _fixedBackdrop.Show();
                _fixedStack.Show();
                var submenu = _fixedStack.PushSubmenuType<RitsuModSettingsSubmenu>();
                ScheduleFocusRefresh();
                return submenu;
            }
            finally
            {
                _switchingWorkspace = false;
            }
        }

        internal void OpenDebugTools(string? pageId = null)
        {
            var dock = EnsureDebugToolsDock();
            dock.SetSuppressed(false);
            _debugToolsPanel?.Refresh();
            dock.Expand(pageId);
        }

        internal void ToggleDebugTools()
        {
            var dock = EnsureDebugToolsDock();
            if (dock.SessionVisible)
            {
                dock.HideForSession();
                return;
            }

            OpenDebugTools();
        }

        internal void ToggleCreaturePicking()
        {
            var dock = EnsureDebugToolsDock();
            dock.SetSuppressed(false);
            if (_debugToolsPanel == null)
                return;

            _switchingWorkspace = true;
            try
            {
                CloseFixedWorkspace();
                _debugToolsPanel.ToggleCreaturePicking();
            }
            finally
            {
                _switchingWorkspace = false;
            }
        }

        internal void SetDeveloperToolsAvailable(bool available)
        {
            var dock = EnsureDebugToolsDock();
            dock.SetAvailable(available);
            if (available)
                _debugToolsPanel?.Refresh();
        }

        private void BuildLayout()
        {
            var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(root);

            _floatingWindows = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
            _floatingWindows.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(_floatingWindows);
            EnsureDebugToolsDock().SetAvailable(RitsuLibSettingsStore.AreDeveloperToolsEnabled());

            _fixedBackdrop = new()
            {
                Color = new(0.015f, 0.025f, 0.045f, 0.88f),
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            _fixedBackdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(_fixedBackdrop);

            _fixedStack = new() { MouseFilter = Control.MouseFilterEnum.Stop };
            _fixedStack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(_fixedStack);
            _fixedStack.StackModified += OnFixedStackModified;
            _fixedBackdrop.Hide();
            _fixedStack.Hide();

            _hoverTips = new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 100,
            };
            _hoverTips.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddChild(_hoverTips);
            SetProcessUnhandledInput(true);
        }

        internal void AttachHoverTips(NHoverTipSet hoverTips)
        {
            if (!IsInstanceValid(_hoverTips) || !IsInstanceValid(hoverTips) || hoverTips.GetParent() == _hoverTips)
                return;

            hoverTips.Reparent(_hoverTips);
        }

        private RitsuDebugToolsDock EnsureDebugToolsDock()
        {
            if (_debugToolsDock != null && IsInstanceValid(_debugToolsDock))
                return _debugToolsDock;

            _debugToolsPanel = new();
            _debugToolsDock = new(_debugToolsPanel)
            {
                Name = "RitsuDebugToolsDock",
            };
            _debugToolsDock.Expanding += OnDebugToolsExpanding;
            _debugToolsDock.Collapsed += OnDebugToolsCollapsed;
            _floatingWindows.AddChild(_debugToolsDock);
            return _debugToolsDock;
        }

        private static void ScheduleFocusRefresh()
        {
            Callable.From(ActiveScreenContext.Instance.FocusOnDefaultControl).CallDeferred();
        }

        private void OnDebugToolsExpanding(object? sender, EventArgs eventArgs)
        {
            CaptureBackgroundFocus();
            _switchingWorkspace = true;
            try
            {
                CloseFixedWorkspace();
                ActiveScreenContext.Instance.Update();
                ScheduleFocusRefresh();
            }
            finally
            {
                _switchingWorkspace = false;
            }
        }

        private void OnDebugToolsCollapsed(object? sender, EventArgs eventArgs)
        {
            ActiveScreenContext.Instance.Update();
            if (!_switchingWorkspace)
                RestoreBackgroundFocus();
        }

        private void OnFixedStackModified()
        {
            if (_fixedStack.SubmenusOpen)
                return;
            _fixedStack.Hide();
            _fixedBackdrop.Hide();
            _debugToolsDock?.SetSuppressed(false);
            if (!_switchingWorkspace)
                RestoreBackgroundFocus();
        }

        private void CloseFixedWorkspace()
        {
            while (_fixedStack.SubmenusOpen)
                _fixedStack.Pop();
        }

        private void CaptureBackgroundFocus()
        {
            if (_backgroundFocusCaptured || _fixedStack.SubmenusOpen || _debugToolsDock is { Expanded: true })
                return;

            _previousFocus = GetViewport()?.GuiGetFocusOwner();
            _backgroundFocusCaptured = true;
        }

        private void RestoreBackgroundFocus()
        {
            ActiveScreenContext.Instance.Update();
            if (!_backgroundFocusCaptured)
                return;

            var target = _previousFocus;
            _previousFocus = null;
            _backgroundFocusCaptured = false;
            Callable.From(() =>
            {
                if (target != null && IsInstanceValid(target) && target.IsInsideTree() &&
                    target.IsVisibleInTree() && target.GetFocusModeWithOverride() != Control.FocusModeEnum.None)
                {
                    target.TryGrabFocus();
                    return;
                }

                ActiveScreenContext.Instance.FocusOnDefaultControl();
            }).CallDeferred();
        }
    }
}
