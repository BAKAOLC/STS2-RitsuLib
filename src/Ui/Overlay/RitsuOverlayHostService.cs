using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Ui.Windows;

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

        internal static bool TryOpenMonsterIntentWindow(
            uint combatId,
            ulong requesterNetId,
            ulong targetPlayerNetId,
            out string error)
        {
            lock (SyncRoot)
            {
                EnsureAttached(NGame.Instance);
                if (_host == null || !GodotObject.IsInstanceValid(_host))
                {
                    error = ModSettingsLocalization.Get(
                        "ritsulib.debugTools.notAvailableYet",
                        "RitsuLib developer tools are not available yet. Try again after the current screen finishes loading.");
                    return false;
                }

                _host.OpenMonsterIntentWindow(combatId, requesterNetId, targetPlayerNetId);
                error = string.Empty;
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
                    host.AttachHoverTips(owner, hoverTips);
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

    internal sealed partial class RitsuOverlayHost : Node
    {
        private readonly Dictionary<uint, RitsuFloatingWindow> _monsterIntentWindows = [];
        private bool _backgroundFocusCaptured;
        private Control _combatFloatingLayer = null!;
        private Control _combatHoverTipLayer = null!;
        private RitsuDebugToolsDock? _debugToolsDock;
        private RitsuDebugToolsPanel? _debugToolsPanel;
        private ColorRect _fixedBackdrop = null!;
        private RitsuOverlaySubmenuStack _fixedStack = null!;
        private Control _overlayHoverTipLayer = null!;
        private Control? _previousFocus;
        private bool _switchingWorkspace;
        private Control _workspaceLayer = null!;

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
            BuildLayout();
            ActiveScreenContext.Instance.Updated += RefreshCombatFloatingLayerVisibility;
            RefreshCombatFloatingLayerVisibility();
        }

        public override void _ExitTree()
        {
            ActiveScreenContext.Instance.Updated -= RefreshCombatFloatingLayerVisibility;
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
            RefreshCombatFloatingLayerVisibility();
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

        internal void OpenMonsterIntentWindow(uint combatId, ulong requesterNetId, ulong targetPlayerNetId)
        {
            if (_monsterIntentWindows.TryGetValue(combatId, out var existing))
            {
                if (IsInstanceValid(existing))
                {
                    existing.Show();
                    existing.MoveToFront();
                    _debugToolsDock?.HideForSession(false);
                    RefreshCombatFloatingLayerVisibility();
                    return;
                }

                _monsterIntentWindows.Remove(combatId);
            }

            var creatureName = RitsuDebugCombatActions.FindCreature(combatId)?.Name;
            var title = string.Format(
                ModSettingsLocalization.Get("ritsulib.debugTools.intentWindowTitle", "{0} · Intent map"),
                string.IsNullOrWhiteSpace(creatureName)
                    ? ModSettingsLocalization.Get("ritsulib.debugTools.creature", "Creature")
                    : creatureName);
            var window = new RitsuFloatingWindow(new()
            {
                Title = title,
                InitialSize = new(350f, 216f),
                MinimumSize = new(350f, 216f),
                MaximumSize = new(760f, 560f),
                Movable = true,
                Resizable = true,
                Closable = true,
                StartCentered = true,
                ConstrainToViewport = true,
                CompactChrome = true,
            })
            {
                ZIndex = 20,
            };
            window.SetContent(new RitsuDebugMonsterIntentWindow(combatId, requesterNetId, targetPlayerNetId));
            window.Closed += OnMonsterIntentWindowClosed;
            _combatFloatingLayer.AddChild(window);
            _monsterIntentWindows[combatId] = window;
            _debugToolsDock?.HideForSession(false);
            RefreshCombatFloatingLayerVisibility();
            Callable.From(() => PositionMonsterIntentWindow(window, combatId)).CallDeferred();
        }

        private void PositionMonsterIntentWindow(RitsuFloatingWindow window, uint combatId)
        {
            if (!IsInstanceValid(window) || !window.IsInsideTree() ||
                RitsuDebugCombatActions.FindCreature(combatId) is not { } creature ||
                NCombatRoom.Instance?.GetCreatureNode(creature) is not { } creatureNode ||
                !IsInstanceValid(creatureNode.Hitbox))
                return;

            var hitbox = creatureNode.Hitbox;
            var transform = hitbox.GetGlobalTransformWithCanvas();
            Vector2[] corners =
            [
                transform * Vector2.Zero,
                transform * new Vector2(hitbox.Size.X, 0f),
                transform * hitbox.Size,
                transform * new Vector2(0f, hitbox.Size.Y),
            ];
            var targetTop = new Vector2(
                (corners.Min(static point => point.X) + corners.Max(static point => point.X)) * 0.5f,
                corners.Min(static point => point.Y));
            var anchor = _combatFloatingLayer.GetGlobalTransformWithCanvas().AffineInverse() * targetTop;
            var position = new Vector2(
                anchor.X - window.Size.X * 0.5f,
                anchor.Y - window.Size.Y - 16f);
            window.ApplyGeometry(new(position, window.Size));
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
            var combatCanvasLayer = new CanvasLayer
            {
                Name = "CombatCanvasLayer",
                Layer = RitsuUiLayer.CombatOverlay,
            };
            AddChild(combatCanvasLayer);

            _combatFloatingLayer = CreateFullRectLayer("CombatFloatingLayer");
            combatCanvasLayer.AddChild(_combatFloatingLayer);
            _combatHoverTipLayer = CreateFullRectLayer("CombatHoverTipLayer", 30);
            _combatFloatingLayer.AddChild(_combatHoverTipLayer);

            var workspaceCanvasLayer = new CanvasLayer
            {
                Name = "WorkspaceCanvasLayer",
                Layer = RitsuUiLayer.Workspace,
            };
            AddChild(workspaceCanvasLayer);

            _workspaceLayer = CreateFullRectLayer("WorkspaceLayer");
            workspaceCanvasLayer.AddChild(_workspaceLayer);
            EnsureDebugToolsDock().SetAvailable(RitsuLibSettingsStore.AreDeveloperToolsEnabled());

            _fixedBackdrop = new()
            {
                Color = new(0.015f, 0.025f, 0.045f, 0.88f),
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 60,
            };
            _fixedBackdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            workspaceCanvasLayer.AddChild(_fixedBackdrop);

            _fixedStack = new()
            {
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 70,
            };
            _fixedStack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            workspaceCanvasLayer.AddChild(_fixedStack);
            _fixedStack.StackModified += OnFixedStackModified;
            _fixedBackdrop.Hide();
            _fixedStack.Hide();

            _overlayHoverTipLayer = CreateFullRectLayer("OverlayHoverTipLayer", 100);
            workspaceCanvasLayer.AddChild(_overlayHoverTipLayer);
            SetProcessUnhandledInput(true);
        }

        internal void AttachHoverTips(Control owner, NHoverTipSet hoverTips)
        {
            if (!IsInstanceValid(owner) || !IsInstanceValid(hoverTips))
                return;

            var targetLayer = _combatFloatingLayer.IsAncestorOf(owner)
                ? _combatHoverTipLayer
                : _overlayHoverTipLayer;
            if (hoverTips.GetParent() != targetLayer)
                hoverTips.Reparent(targetLayer);
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
            _workspaceLayer.AddChild(_debugToolsDock);
            return _debugToolsDock;
        }

        private static Control CreateFullRectLayer(string name, int zIndex = 0)
        {
            var layer = new Control
            {
                Name = name,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = zIndex,
            };
            layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return layer;
        }

        private void RefreshCombatFloatingLayerVisibility()
        {
            if (!IsInstanceValid(_combatFloatingLayer))
                return;

            var combatRoom = NCombatRoom.Instance;
            var activeScreen = ActiveScreenContext.Instance.GetCurrentScreen();
            _combatFloatingLayer.Visible = combatRoom != null && ReferenceEquals(activeScreen, combatRoom);
        }

        private static void ScheduleFocusRefresh()
        {
            Callable.From(ActiveScreenContext.Instance.FocusOnDefaultControl).CallDeferred();
        }

        private void OnMonsterIntentWindowClosed(object? sender, EventArgs eventArgs)
        {
            if (sender is not RitsuFloatingWindow window)
                return;
            window.Closed -= OnMonsterIntentWindowClosed;
            var combatId = _monsterIntentWindows
                .FirstOrDefault(pair => ReferenceEquals(pair.Value, window))
                .Key;
            if (_monsterIntentWindows.TryGetValue(combatId, out var registered) && ReferenceEquals(registered, window))
                _monsterIntentWindows.Remove(combatId);
            window.QueueFree();
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
            {
                RefreshCombatFloatingLayerVisibility();
                return;
            }

            _fixedStack.Hide();
            _fixedBackdrop.Hide();
            _debugToolsDock?.SetSuppressed(false);
            RefreshCombatFloatingLayerVisibility();
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
