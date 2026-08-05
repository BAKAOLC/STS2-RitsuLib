using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    internal sealed record RitsuDebugToolsPageView(
        string Id,
        string Title,
        int SortOrder,
        float PreferredWidthFraction,
        Texture2D? Icon,
        Func<Control> ContentFactory);

    internal sealed partial class RitsuDebugToolsPanel : VBoxContainer, IScreenContext
    {
        private const int DetailBodyFontSize = 16;
        private const int DetailMetadataFontSize = 15;
        private const int DetailIdentifierFontSize = 14;
        private const int DetailSectionFontSize = 16;
        private const float HeaderTargetWidth = 320f;
        private readonly Dictionary<Control, GuiInputEventHandler> _creaturePickHandlers = [];
        private readonly HashSet<string> _pageFailures = new(StringComparer.Ordinal);
        private Control? _browserHost;
        private Button? _creaturePickButton;
        private bool _creaturePicking;
        private RitsuToastHandle? _creaturePickingToast;
        private ModSettingsDropdownChoiceControl<uint>? _creatureTargetDropdown;
        private bool _contextualPageSelection;
        private Control? _currentBrowser;
        private IDisposable? _modelRegistryInitializedSubscription;
        private RitsuDebugToolsPageView[] _pages = [];
        private bool _refreshScheduled;
        private uint? _selectedCreatureCombatId;
        private string? _selectedCreaturePresetId;
        private bool _stateRefreshScheduled;
        private Label? _status;
        private ModSettingsDropdownChoiceControl<ulong>? _targetDropdown;
        private Label? _targetLabel;
        private ulong[] _targetPlayerIds = [];
        private ulong? _targetPlayerNetId;

        internal string CurrentPageId { get; private set; } = $"{Const.ModId}:cards";

        internal float CurrentPageWidthFraction => GetCurrentPage()?.PreferredWidthFraction ?? 0.62f;

        public Control? DefaultFocusedControl => _creatureTargetDropdown is { Visible: true }
            ? _creatureTargetDropdown
            : _targetDropdown is { Visible: true }
                ? _targetDropdown
                : _creaturePickButton;

        internal event Action<IReadOnlyList<RitsuDebugToolsPageView>>? PagesChanged;

        internal event Action<RitsuDebugToolsPageView>? PageChanged;

        internal event Action? CreaturePickingStarted;

        internal event Action? CreaturePickingFinished;

        public override void _Ready()
        {
            RitsuDebugActionProtocol.ActionExecuted += OnDebugActionExecuted;
            RitsuDebugToolsPageRegistry.Changed += OnPageRegistryChanged;
            _modelRegistryInitializedSubscription =
                RitsuLibFramework.SubscribeLifecycle<ModelRegistryInitializedEvent>(_ => ScheduleRefresh());
            CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
            CombatManager.Instance.CombatEnded += OnCombatEnded;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            CustomMinimumSize = new(0f, 540f);
            AddThemeConstantOverride("separation", 10);
            BuildHeader();
            BuildWorkspace();
            _status = new()
            {
                Text = L("ritsulib.debugTools.status.ready", "Ready"),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _status.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            _status.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            _status.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(_status);
            RefreshPages();
            RebuildBrowser();
        }

        public override void _ExitTree()
        {
            FinishCreaturePicking(false);
            RitsuDebugActionProtocol.ActionExecuted -= OnDebugActionExecuted;
            RitsuDebugToolsPageRegistry.Changed -= OnPageRegistryChanged;
            _modelRegistryInitializedSubscription?.Dispose();
            _modelRegistryInitializedSubscription = null;
            CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
            CombatManager.Instance.CombatEnded -= OnCombatEnded;
            base._ExitTree();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!_creaturePicking || @event.IsEcho() ||
                !(@event.IsActionPressed(MegaInput.cancel) ||
                  @event.IsActionPressed(MegaInput.pauseAndBack) ||
                  @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }))
                return;

            FinishCreaturePicking(true);
            GetViewport().SetInputAsHandled();
        }

        internal void Refresh()
        {
            if (!IsInsideTree())
                return;
            RefreshAll();
        }

        internal void CancelCreaturePicking()
        {
            FinishCreaturePicking(false);
        }

        private void BuildHeader()
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreatePageToolbarTrayStyle());
            AddChild(panel);
            var toolbar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddThemeConstantOverride("separation", 10);
            panel.AddChild(toolbar);

            _targetLabel = new()
            {
                Text = L("ritsulib.debugTools.targetPlayer", "Target player"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            _targetLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            _targetLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            _targetLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            toolbar.AddChild(_targetLabel);

            var players = GetPlayers();
            if (players.Length > 0)
            {
                var localNetId = RunManager.Instance?.NetService?.NetId;
                _targetPlayerNetId = localNetId.HasValue &&
                                     players.Any(player => player.NetId == localNetId.Value)
                    ? localNetId.Value
                    : players[0].NetId;
                _selectedCreatureCombatId = players
                    .First(player => player.NetId == _targetPlayerNetId)
                    .Creature.CombatId;
            }

            var targetOptions = players
                .Select((player, index) => (player.NetId, PlayerLabel(player, index)))
                .ToArray();
            _targetPlayerIds = [.. players.Select(static player => player.NetId)];
            _targetDropdown = new(
                targetOptions,
                _targetPlayerNetId ?? 0,
                selected =>
                {
                    _targetPlayerNetId = selected;
                    _selectedCreatureCombatId = GetPlayers()
                        .FirstOrDefault(player => player.NetId == selected)
                        ?.Creature.CombatId;
                    RefreshPages();
                    RebuildBrowser();
                })
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize = new(HeaderTargetWidth, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            toolbar.AddChild(_targetDropdown);

            var creatures = CurrentCreatures();
            var creatureTarget = creatures.Length == 0 ? 0 : PreferredCreatureCombatId(creatures);
            _creatureTargetDropdown = new(
                CreateCreatureTargetOptions(creatures),
                creatureTarget,
                selected =>
                {
                    _selectedCreatureCombatId = selected;
                    if (_currentBrowser != null && IsInstanceValid(_currentBrowser))
                        RefreshLiveDetails(_currentBrowser);
                })
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize = new(HeaderTargetWidth, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            toolbar.AddChild(_creatureTargetDropdown);

            var pickCreatureLabel = L("ritsulib.debugTools.action.pickCreature", "Pick creature");
            _creaturePickButton = new ModSettingsTextButton(
                pickCreatureLabel,
                ModSettingsButtonTone.Accent,
                ToggleCreaturePicking)
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                CustomMinimumSize = new(136f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                TooltipText = $"{pickCreatureLabel}\n" +
                              L(
                                  "ritsulib.debugTools.pickCreature.description",
                                  "Close the workspace for one click, then reopen the selected creature's quick editor."),
            };
            toolbar.AddChild(_creaturePickButton);
            RefreshCreaturePickButton();
            RefreshHeaderTarget();
        }

        private void BuildWorkspace()
        {
            _browserHost = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 460f),
            };
            AddChild(_browserHost);
        }

        internal IReadOnlyList<RitsuDebugToolsPageView> GetPages()
        {
            return Array.AsReadOnly(_pages);
        }

        internal bool SelectPage(string pageId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            var selected = _pages.FirstOrDefault(page =>
                page.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase));
            if (selected == null)
                return false;
            var resetCatalogMode = !_contextualPageSelection && ResetBuiltInCatalogMode(selected.Id);
            if (CurrentPageId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (resetCatalogMode)
                    RebuildBrowser();
                return true;
            }

            CurrentPageId = selected.Id;
            RefreshHeaderTarget();
            RebuildBrowser();
            PageChanged?.Invoke(selected);
            return true;
        }

        private bool ResetBuiltInCatalogMode(string pageId)
        {
            if (pageId.Equals($"{Const.ModId}:relics", StringComparison.OrdinalIgnoreCase))
            {
                var changed = _relicCatalogMode != RelicCatalogMode.Library;
                _relicCatalogMode = RelicCatalogMode.Library;
                return changed;
            }

            if (pageId.Equals($"{Const.ModId}:potions", StringComparison.OrdinalIgnoreCase))
            {
                var changed = _potionCatalogMode != PotionCatalogMode.Library;
                _potionCatalogMode = PotionCatalogMode.Library;
                return changed;
            }

            if (!pageId.Equals($"{Const.ModId}:powers", StringComparison.OrdinalIgnoreCase))
                return false;
            var powerChanged = _powerCatalogMode != PowerCatalogMode.Library;
            _powerCatalogMode = PowerCatalogMode.Library;
            return powerChanged;
        }

        private void RefreshAll()
        {
            var players = GetPlayers();
            if (players.All(player => player.NetId != _targetPlayerNetId))
                _targetPlayerNetId = players.FirstOrDefault()?.NetId;
            UpdateTargetDropdown(players, true);
            RefreshPages();
            RebuildBrowser();
        }

        private void RefreshPages()
        {
            var context = CreatePageContext();
            var pages = new List<RitsuDebugToolsPageView>(16);
            AddBuiltInPages(pages);
            foreach (var definition in RitsuDebugToolsPageRegistry.GetPages())
            {
                if (!IsExternalPageVisible(definition, context))
                    continue;
                var title = ResolveExternalPageTitle(definition);
                var icon = ResolveExternalPageIcon(definition);
                pages.Add(new(
                    definition.QualifiedId,
                    title,
                    definition.SortOrder,
                    definition.PreferredWidthFraction,
                    icon,
                    () => CreateExternalPage(definition)));
            }

            _pages =
            [
                .. pages
                    .OrderBy(static page => page.SortOrder)
                    .ThenBy(static page => page.Id, StringComparer.OrdinalIgnoreCase),
            ];
            if (_pages.All(page => !page.Id.Equals(CurrentPageId, StringComparison.OrdinalIgnoreCase)))
                CurrentPageId = _pages.FirstOrDefault()?.Id ?? string.Empty;
            RefreshHeaderTarget();
            PagesChanged?.Invoke(Array.AsReadOnly(_pages));
            var current = GetCurrentPage();
            if (current != null)
                PageChanged?.Invoke(current);
        }

        private void AddBuiltInPages(List<RitsuDebugToolsPageView> pages)
        {
            var tint = RitsuShellTheme.Current.Text.LabelPrimary;
            Add("state-presets", "ritsulib.debugTools.category.statePresets", "State presets", -10, 0.78f,
                RitsuDebugToolsGlyph.PileCards, CreateStatePresetEditor);
            Add("cards", "ritsulib.debugTools.category.cards", "Card library", 0, 0.74f,
                RitsuDebugToolsGlyph.Cards, CreateCardCatalog);
            Add("pile-cards", "ritsulib.debugTools.category.pileCards", "Player cards", 10, 0.74f,
                RitsuDebugToolsGlyph.PileCards, CreatePileCardCatalog);
            Add("relics", "ritsulib.debugTools.category.relics", "Relics", 20, 0.68f,
                RitsuDebugToolsGlyph.Relics, CreateRelicCatalog);
            Add("potions", "ritsulib.debugTools.category.potions", "Potions", 30, 0.68f,
                RitsuDebugToolsGlyph.Potions, CreatePotionCatalog);
            Add("powers", "ritsulib.debugTools.category.powers", "Powers", 40, 0.72f,
                RitsuDebugToolsGlyph.Powers, CreatePowerCatalog);
            Add("orbs", "ritsulib.debugTools.category.orbs", "Orbs", 45, 0.72f,
                RitsuDebugToolsGlyph.Orbs, CreateOrbCatalog);
            Add("creatures", "ritsulib.debugTools.category.combatants", "Players and combat creatures", 50, 0.72f,
                RitsuDebugToolsGlyph.Creatures, CreateCombatantCatalog);
            Add("monsters", "ritsulib.debugTools.category.monsters", "Add monster", 70, 0.62f,
                RitsuDebugToolsGlyph.Monsters, CreateMonsterCatalog);
            Add("rooms", "ritsulib.debugTools.category.rooms", "Rooms", 80, 0.48f,
                RitsuDebugToolsGlyph.Rooms, CreateRoomCatalog);
            Add("encounters", "ritsulib.debugTools.category.encounters", "Encounters", 90, 0.62f,
                RitsuDebugToolsGlyph.Encounters, CreateEncounterCatalog);
            Add("events", "ritsulib.debugTools.category.events", "Events", 100, 0.56f,
                RitsuDebugToolsGlyph.Events, CreateEventCatalog);
            return;

            void Add(
                string id,
                string localizationKey,
                string fallback,
                int order,
                float preferredWidthFraction,
                RitsuDebugToolsGlyph glyph,
                Func<Control> factory)
            {
                pages.Add(new(
                    $"{Const.ModId}:{id}",
                    L(localizationKey, fallback),
                    order,
                    preferredWidthFraction,
                    RitsuDebugToolsIcons.Get(glyph, 22, tint),
                    factory));
            }
        }

        private RitsuDebugToolsPageContext CreatePageContext()
        {
            var players = GetPlayers();
            var target = _targetPlayerNetId.HasValue
                ? players.FirstOrDefault(player => player.NetId == _targetPlayerNetId.Value)
                : null;
            return new(target, Array.AsReadOnly(players), ScheduleRefresh, SelectPage);
        }

        private bool IsExternalPageVisible(
            RitsuDebugToolsPageDefinition definition,
            RitsuDebugToolsPageContext context)
        {
            if (definition.VisibleWhen == null)
                return true;
            try
            {
                return definition.VisibleWhen(context);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogPageFailureOnce(definition, "visibility", ex);
                return false;
            }
        }

        private string ResolveExternalPageTitle(RitsuDebugToolsPageDefinition definition)
        {
            try
            {
                var title = definition.Title.Resolve()?.Trim();
                return string.IsNullOrWhiteSpace(title) ? definition.Id : title;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogPageFailureOnce(definition, "title", ex);
                return definition.Id;
            }
        }

        private Texture2D? ResolveExternalPageIcon(RitsuDebugToolsPageDefinition definition)
        {
            if (definition.IconFactory == null)
                return null;
            try
            {
                var icon = definition.IconFactory();
                if (icon == null || !IsInstanceValid(icon))
                    return null;
                return icon.GetWidth() is > 0 and <= 2048 && icon.GetHeight() is > 0 and <= 2048
                    ? icon
                    : null;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogPageFailureOnce(definition, "icon", ex);
                return null;
            }
        }

        private Control CreateExternalPage(RitsuDebugToolsPageDefinition definition)
        {
            try
            {
                var control = definition.ContentFactory(CreatePageContext());
                if (control == null || !IsInstanceValid(control) || control.GetParent() != null)
                    throw new InvalidOperationException(
                        "The developer-tools page factory must return a valid, unattached control.");
                return control;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogPageFailureOnce(definition, "content", ex);
                return EmptyBrowser(L("ritsulib.debugTools.detailsUnavailable",
                    "Details are unavailable for this item."));
            }
        }

        private void LogPageFailureOnce(
            RitsuDebugToolsPageDefinition definition,
            string phase,
            Exception exception)
        {
            if (!_pageFailures.Add($"{definition.QualifiedId}\0{phase}"))
                return;
            RitsuLibFramework.Logger.Warn(
                $"[DebugToolsUi] Registered page '{definition.QualifiedId}' failed during {phase}: {exception}");
        }

        private void OnPageRegistryChanged()
        {
            _ = RitsuMainThread.InvokeAsync(() =>
            {
                if (!IsInsideTree())
                    return;
                RefreshPages();
                RebuildBrowser();
            });
        }

        private void RebuildBrowser()
        {
            if (_browserHost == null)
                return;
            if (_currentBrowser != null && IsInstanceValid(_currentBrowser))
            {
                if (ReferenceEquals(_currentBrowser.GetParent(), _browserHost))
                    _browserHost.RemoveChild(_currentBrowser);
                _currentBrowser.QueueFree();
            }

            _currentBrowser = CreateBrowser();
            _browserHost.AddChild(_currentBrowser);
            _currentBrowser.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }

        private Control CreateBrowser()
        {
            if (!RitsuLibSettingsStore.AreDeveloperToolsEnabled())
                return EmptyBrowser(
                    L("ritsulib.debugTools.disabled",
                        "Enable developer tools in RitsuLib settings before opening this workspace."));
            if (ResolvedModelCache.Phase < ContentRegistryPhase.Resolved)
                return EmptyBrowser(
                    L("ritsulib.debugTools.notAvailableYet",
                        "RitsuLib developer tools are not available yet. Try again after the current screen finishes loading."));

            return GetCurrentPage()?.ContentFactory()
                   ?? EmptyBrowser(L("ritsulib.debugTools.noMatches", "No matching items"));
        }

        private RitsuDebugToolsPageView? GetCurrentPage()
        {
            return _pages.FirstOrDefault(page =>
                page.Id.Equals(CurrentPageId, StringComparison.OrdinalIgnoreCase));
        }

        private static RitsuCatalogBrowser EmptyBrowser(string message)
        {
            var browser = new RitsuCatalogBrowser(new()
            {
                SearchPlaceholder = L("ritsulib.debugTools.search", "Search"),
                EmptyText = message,
                DetailPlaceholderText = message,
                MinimumHeight = 460f,
                CatalogWidth = 330f,
                DetailMinimumWidth = 300f,
            });
            browser.SetItems([]);
            return browser;
        }

        private bool RunAction(Func<RitsuDebugActionSubmission> submit)
        {
            try
            {
                var submission = submit();
                if (submission.Accepted)
                    SetStatus(L("ritsulib.debugTools.requestAccepted", "The requested change was accepted."), false);
                else
                    ShowActionWarning(submission.Message);
                return submission.Accepted;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugToolsUi] Action submission failed: {ex}");
                var message = L("ritsulib.debugTools.requestFailed",
                    "The requested change could not be submitted. See the game log for details.");
                SetStatus(message, true);
                RitsuToastService.ShowError(message, L("ritsulib.debugTools.toastTitle", "Developer tools"));
                return false;
            }
        }

        private Control CreateStatePresetEditor()
        {
            return new RitsuDebugStatePresetEditor(
                preset =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return false;
                    return RunAction(() => RitsuDebugStatePresetActions.SubmitApplyPreset(
                        requester,
                        target,
                        preset));
                },
                () => TryGetTargetPlayer(out var target) ? target : null,
                SetStatus);
        }

        private void ShowActionWarning(string message)
        {
            SetStatus(message, true);
            RitsuToastService.ShowWarning(message, L("ritsulib.debugTools.toastTitle", "Developer tools"));
        }

        private void SetStatus(string text, bool error)
        {
            if (_status == null)
                return;
            _status.Text = text;
            _status.AddThemeColorOverride("font_color", error
                ? RitsuShellTheme.Current.Component.TextButton.Danger.Fg
                : RitsuShellTheme.Current.Text.LabelSecondary);
        }

        private void OnDebugActionExecuted(RitsuDebugActionExecutionResult result)
        {
            if (!IsInsideTree())
                return;

            SetStatus(
                result.Success
                    ? L("ritsulib.debugTools.changeApplied", "The requested change was applied.")
                    : result.Message,
                !result.Success);
            if (result.Success && result.TargetPlayerNetId == _targetPlayerNetId)
                ScheduleStateRefresh();
        }

        private void OnCombatStateChanged(CombatState _)
        {
            ScheduleStateRefresh();
        }

        private void OnCombatEnded(CombatRoom _)
        {
            FinishCreaturePicking(false);
            ScheduleStateRefresh();
        }

        private void ScheduleStateRefresh()
        {
            if (_stateRefreshScheduled || !IsInsideTree())
                return;
            _stateRefreshScheduled = true;
            Callable.From(() =>
            {
                _stateRefreshScheduled = false;
                if (IsInsideTree())
                    RefreshCurrentState();
            }).CallDeferred();
        }

        private void RefreshCurrentState()
        {
            var players = GetPlayers();
            RefreshCreaturePickButton();
            var previousTarget = _targetPlayerNetId;
            if (players.All(player => player.NetId != _targetPlayerNetId))
                _targetPlayerNetId = players.FirstOrDefault()?.NetId;
            UpdateTargetDropdown(players);
            RefreshHeaderTarget();
            if (previousTarget != _targetPlayerNetId)
            {
                RefreshPages();
                RebuildBrowser();
                return;
            }

            if (_currentBrowser == null || !IsInstanceValid(_currentBrowser))
                return;

            switch (CurrentPageId)
            {
                case $"{Const.ModId}:pile-cards":
                    if (_currentBrowser is RitsuDebugCardCatalog pileCatalog &&
                        TryGetTargetPlayer(out var target))
                        pileCatalog.UpdateEntries(CreatePileCardCatalogEntries(GetPileCardEntries(target)));
                    else
                        RebuildBrowser();
                    break;
                case $"{Const.ModId}:creatures":
                    var creatures = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                        .Where(static creature => creature.CombatId.HasValue)
                        .OrderBy(static creature => creature.CombatId)
                        .ToArray() ?? [];
                    if (_selectedCreatureCombatId.HasValue &&
                        creatures.All(creature => creature.CombatId != _selectedCreatureCombatId))
                        _selectedCreatureCombatId = null;
                    RefreshCatalogItems(CreateCombatantCatalogItems(players, creatures));
                    break;
                default:
                    RefreshLiveDetails(_currentBrowser);
                    break;
            }
        }

        private void UpdateTargetDropdown(IReadOnlyList<Player> players, bool force = false)
        {
            var playerIds = players.Select(static player => player.NetId).ToArray();
            if (!force && _targetPlayerIds.SequenceEqual(playerIds))
                return;
            _targetPlayerIds = playerIds;
            _targetDropdown?.SetOptions(
                [.. players.Select((player, index) => (player.NetId, PlayerLabel(player, index)))],
                _targetPlayerNetId ?? 0);
        }

        private void RefreshHeaderTarget()
        {
            if (_targetLabel == null || _targetDropdown == null || _creatureTargetDropdown == null)
                return;

            var mode = HeaderTargetModeForPage();
            _targetLabel.Visible = mode != HeaderTargetMode.None;
            _targetDropdown.Visible = mode == HeaderTargetMode.Player;
            _creatureTargetDropdown.Visible = mode == HeaderTargetMode.Creature;
            switch (mode)
            {
                case HeaderTargetMode.Player:
                    _targetLabel.Text = L("ritsulib.debugTools.targetPlayer", "Target player");
                    break;
                case HeaderTargetMode.Creature:
                    _targetLabel.Text = L("ritsulib.debugTools.targetCreature", "Target creature");
                    var creatures = CurrentCreatures();
                    if (creatures.Length == 0)
                    {
                        _selectedCreatureCombatId = null;
                        _creatureTargetDropdown.SetOptions([], 0);
                        break;
                    }

                    var selected = PreferredCreatureCombatId(creatures);
                    _selectedCreatureCombatId = selected;
                    _creatureTargetDropdown.SetOptions(CreateCreatureTargetOptions(creatures), selected);
                    break;
                case HeaderTargetMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private HeaderTargetMode HeaderTargetModeForPage()
        {
            return CurrentPageId switch
            {
                $"{Const.ModId}:powers" => HeaderTargetMode.Creature,
                $"{Const.ModId}:creatures" or
                    $"{Const.ModId}:monsters" or
                    $"{Const.ModId}:rooms" or
                    $"{Const.ModId}:encounters" => HeaderTargetMode.None,
                _ => HeaderTargetMode.Player,
            };
        }

        private static (uint Value, string Label)[] CreateCreatureTargetOptions(IEnumerable<Creature> creatures)
        {
            return
            [
                .. creatures.Select(creature =>
                    (creature.CombatId!.Value, string.Format(
                        L("ritsulib.debugTools.creatureChoice", "#{0} · {1}"),
                        creature.CombatId.Value,
                        creature.Name))),
            ];
        }

        private void RefreshCatalogItems(IReadOnlyList<RitsuCatalogItem> items)
        {
            if (_currentBrowser is not RitsuCatalogBrowser browser ||
                !browser.Items.Select(static item => item.Id).SequenceEqual(
                    items.Select(static item => item.Id),
                    StringComparer.Ordinal))
            {
                RebuildBrowser();
                return;
            }

            browser.UpdateItems(items);
            RefreshLiveDetails(browser);
        }

        private static void RefreshLiveDetails(Node node)
        {
            if (node is RitsuDebugLiveDetailContainer detail)
                detail.RefreshState();
            foreach (var child in node.GetChildren())
                RefreshLiveDetails(child);
        }

        private void ScheduleRefresh()
        {
            if (_refreshScheduled || !IsInsideTree())
                return;
            _refreshScheduled = true;
            Callable.From(() =>
            {
                _refreshScheduled = false;
                if (IsInsideTree())
                    RefreshAll();
            }).CallDeferred();
        }

        private bool TryGetActionContext(out Player requester, out Player target)
        {
            requester = null!;
            target = null!;
            var players = GetPlayers();
            if (players.Length == 0)
            {
                SetStatus(L("ritsulib.debugTools.noRun", "Start a run to use state tools."), true);
                return false;
            }

            var localNetId = RunManager.Instance.NetService?.NetId;
            requester = localNetId.HasValue
                ? players.FirstOrDefault(player => player.NetId == localNetId.Value)!
                : null!;
            if (requester == null && players.Length == 1)
                requester = players[0];
            target = _targetPlayerNetId.HasValue
                ? players.FirstOrDefault(player => player.NetId == _targetPlayerNetId.Value)!
                : players[0];
            if (requester != null && target != null)
                return true;

            SetStatus(L("ritsulib.debugTools.localPlayerMissing", "The local or target player is unavailable."), true);
            return false;
        }

        private bool TryGetTargetPlayer(out Player target)
        {
            target = null!;
            var players = GetPlayers();
            if (players.Length == 0)
                return false;
            target = _targetPlayerNetId.HasValue
                ? players.FirstOrDefault(player => player.NetId == _targetPlayerNetId.Value)!
                : players[0];
            return target != null;
        }

        private static Player[] GetPlayers()
        {
            return RunManager.Instance?.DebugOnlyGetState()?.Players.ToArray() ?? [];
        }

        private static string PlayerLabel(Player player, int index)
        {
            var name = player.Character?.Title.GetFormattedText();
            return string.Format(
                L("ritsulib.debugTools.playerLabel", "Player {0}: {1}"),
                index + 1,
                string.IsNullOrWhiteSpace(name) ? player.NetId.ToString() : name);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        internal void ToggleCreaturePicking()
        {
            if (_creaturePicking)
            {
                FinishCreaturePicking(true);
                return;
            }

            BeginCreaturePicking();
        }

        private void BeginCreaturePicking()
        {
            var room = NCombatRoom.Instance;
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                room == null || NTargetManager.Instance.IsInSelection || room.Ui.Hand.InCardPlay)
            {
                ShowActionWarning(L(
                    "ritsulib.debugTools.pickCreature.unavailable",
                    "Creature picking is available during combat while no card or target selection is active."));
                return;
            }

            foreach (var creature in CurrentCreatures())
            {
                if (!creature.CombatId.HasValue || room.GetCreatureNode(creature) is not { } node ||
                    !IsInstanceValid(node.Hitbox) || node.Hitbox.MouseFilter == MouseFilterEnum.Ignore)
                    continue;

                var combatId = creature.CombatId.Value;
                var hitbox = node.Hitbox;
                GuiInputEventHandler handler = input => OnCreaturePickInput(combatId, hitbox, input);
                _creaturePickHandlers.Add(hitbox, handler);
                hitbox.GuiInput += handler;
            }

            if (_creaturePickHandlers.Count == 0)
            {
                ShowActionWarning(L(
                    "ritsulib.debugTools.pickCreature.none",
                    "No selectable combat creature is currently available."));
                return;
            }

            _creaturePicking = true;
            SetProcessUnhandledInput(true);
            RefreshCreaturePickButton();
            CreaturePickingStarted?.Invoke();
            _creaturePickingToast = RitsuToastService.ShowTracked(
                new RitsuToastRequest(
                        L("ritsulib.debugTools.pickCreature.prompt",
                            "Click a creature to edit it. Press the picker shortcut again or Esc to cancel."),
                        L("ritsulib.debugTools.toastTitle", "Developer tools"))
                    .Persistent()
                    .WithDismissOnClick(false));
        }

        private void OnCreaturePickInput(uint combatId, Control hitbox, InputEvent input)
        {
            if (!_creaturePicking ||
                input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } ||
                NTargetManager.Instance.IsInSelection ||
                NCombatRoom.Instance is not { } room || room.Ui.Hand.InCardPlay)
                return;

            hitbox.AcceptEvent();
            hitbox.GetViewport()?.SetInputAsHandled();
            FinishCreaturePicking(false);
            OpenCreatureDetail(combatId);
            CreaturePickingFinished?.Invoke();
        }

        private void OpenCreatureDetail(uint combatId)
        {
            _selectedCreatureCombatId = combatId;
            var creature = RitsuDebugCombatActions.FindCreature(combatId);
            var itemId = creature?.Player is { } player
                ? $"player:{player.NetId}"
                : $"creature:{combatId}";
            SelectPage($"{Const.ModId}:creatures");
            if (_currentBrowser is RitsuCatalogBrowser browser &&
                browser.SelectItem(itemId))
                return;

            SetStatus(L("ritsulib.debugTools.targetChanged", "The selected target is no longer available."), true);
        }

        private void FinishCreaturePicking(bool reopenWorkspace)
        {
            _creaturePickingToast?.Dismiss(true);
            _creaturePickingToast = null;
            if (!_creaturePicking && _creaturePickHandlers.Count == 0)
                return;

            foreach (var (hitbox, handler) in _creaturePickHandlers)
                if (IsInstanceValid(hitbox))
                    hitbox.GuiInput -= handler;

            _creaturePickHandlers.Clear();
            _creaturePicking = false;
            SetProcessUnhandledInput(false);
            RefreshCreaturePickButton();
            if (reopenWorkspace)
                CreaturePickingFinished?.Invoke();
        }

        private void RefreshCreaturePickButton()
        {
            if (_creaturePickButton == null)
                return;

            _creaturePickButton.Text = _creaturePicking
                ? L("ritsulib.debugTools.action.cancelPickCreature", "Cancel picking")
                : L("ritsulib.debugTools.action.pickCreature", "Pick creature");
            _creaturePickButton.Disabled = !_creaturePicking &&
                                           (!CombatManager.Instance.IsInProgress ||
                                            CombatManager.Instance.IsOverOrEnding);
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(_creaturePickButton);
        }

        private static void SyncScrollGutter(ScrollContainer scroll, MarginContainer frame)
        {
            if (!IsInstanceValid(scroll) || !IsInstanceValid(frame))
                return;
            var gutter = scroll.GetVScrollBar().Visible
                ? ModSettingsUiControlTheming.ResolveSettingsScrollContentRightGutter(scroll)
                : 0;
            if (frame.GetThemeConstant("margin_right") == gutter)
                return;
            frame.AddThemeConstantOverride("margin_right", gutter);
            frame.QueueSort();
        }

        private enum HeaderTargetMode
        {
            None,
            Player,
            Creature,
        }
    }
}
