using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugToolsPanel
    {
        private RelicCatalogMode _relicCatalogMode = RelicCatalogMode.Library;
        private PotionCatalogMode _potionCatalogMode = PotionCatalogMode.Library;
        private PowerCatalogMode _powerCatalogMode = PowerCatalogMode.Library;
        private OrbCatalogMode _orbCatalogMode = OrbCatalogMode.Library;

        private Control CreateRelicWorkspace(
            IReadOnlyList<RelicModel> models,
            IReadOnlyDictionary<string, RelicModel> modelsById,
            RitsuCatalogBrowser libraryBrowser)
        {
            var ownedByItemId = new Dictionary<string, RelicModel>(StringComparer.Ordinal);
            var relicIndexesByItemId = new Dictionary<string, int>(StringComparer.Ordinal);
            var rarityFilter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => ownedByItemId.TryGetValue(item.Id, out var model) && model.Rarity == value);
            var ownedBrowser = Browser(
                L("ritsulib.debugTools.search.ownedRelics", "Search owned relics"),
                item => ownedByItemId.TryGetValue(item.Id, out var relic) &&
                        relicIndexesByItemId.TryGetValue(item.Id, out var relicIndex)
                    ? CreateOwnedRelicDetail(relic, relicIndex)
                    : EmptyBrowser(L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available.")),
                [rarityFilter, CreateContentSourceFilter(models, ownedByItemId)],
                RitsuCatalogPresentation.Grid,
                detailWidth: 520f);

            var root = new RitsuDebugLiveDetailContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation", 10);
            var ownedButton = ModeButton(
                RitsuDebugToolsGlyph.Inventory,
                L("ritsulib.debugTools.relics.owned", "Owned"),
                static () => { });
            var libraryButton = ModeButton(
                RitsuDebugToolsGlyph.Library,
                L("ritsulib.debugTools.relics.library", "All relics"),
                static () => { });
            ownedButton.Pressed += () => SetMode(RelicCatalogMode.Owned);
            libraryButton.Pressed += () => SetMode(RelicCatalogMode.Library);
            root.AddChild(CreateWorkspaceToolbar(
                libraryButton,
                ownedButton,
                L("ritsulib.debugTools.relics.workspaceHint",
                    "Browse the full library by default; switch to owned relics for direct removal.")));
            root.AddChild(ownedBrowser);
            root.AddChild(libraryBrowser);
            root.RegisterRefresh(RefreshItems);
            RefreshItems();
            SetMode(_relicCatalogMode);
            return root;

            void SetMode(RelicCatalogMode mode)
            {
                _relicCatalogMode = mode;
                var owned = mode == RelicCatalogMode.Owned;
                ownedBrowser.Visible = owned;
                libraryBrowser.Visible = !owned;
                ownedButton.SetSelected(owned);
                libraryButton.SetSelected(!owned);
            }

            void RefreshItems()
            {
                var relics = TryGetTargetPlayer(out var target) ? target.Relics.ToArray() : [];
                ownedByItemId.Clear();
                relicIndexesByItemId.Clear();
                var trashIcon = RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Trash,
                    18,
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg);
                var ownedItems = relics.Select((relic, index) =>
                {
                    var itemId = $"{index}:{relic.Id}";
                    ownedByItemId[itemId] = relic;
                    relicIndexesByItemId[itemId] = index;
                    var source = ContentSourceResolver.Resolve(relic);
                    RitsuCatalogItemAction? quickAction = trashIcon == null
                        ? null
                        : new(
                            trashIcon,
                            L("ritsulib.debugTools.action.removeRelic", "Remove relic"),
                            () => SubmitInventoryAction((requester, actionTarget) =>
                                RitsuDebugInventoryActions.SubmitRemoveRelic(
                                    requester,
                                    actionTarget,
                                    relic.Id.ToString(),
                                    index)),
                            RitsuCatalogItemActionTone.Danger);
                    return new RitsuCatalogItem(
                        itemId,
                        SafeTitle(relic),
                        $"{EnumLabel(relic.Rarity)} · {ContentSourceDisplayLabel(source)}",
                        $"{relic.Id} {source.ModId} {source.DisplayName}",
                        iconFactory: () => relic.Icon,
                        badge: $"#{index + 1}",
                        tooltip: BuildCatalogTooltip(
                            SafeTitle(relic),
                            relic.Id.ToString(),
                            ContentSourceDisplayLabel(source)),
                        quickAction: quickAction);
                }).ToArray();
                ownedBrowser.UpdateItems(ownedItems);

                var counts = relics.GroupBy(static relic => relic.Id.ToString(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
                libraryBrowser.UpdateItems([
                    .. models.Select(model =>
                        CreateRelicLibraryItem(model, counts.GetValueOrDefault(model.Id.ToString()))),
                ]);
                ownedButton.Text = string.Format(
                    L("ritsulib.debugTools.relics.ownedCount", "Owned ({0})"),
                    relics.Length);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(ownedButton);
            }
        }

        private Control CreatePotionWorkspace(
            IReadOnlyList<PotionModel> models,
            IReadOnlyDictionary<string, PotionModel> modelsById,
            RitsuCatalogBrowser libraryBrowser)
        {
            var ownedByItemId = new Dictionary<string, PotionModel>(StringComparer.Ordinal);
            var slotsByItemId = new Dictionary<string, int>(StringComparer.Ordinal);
            var rarityFilter = EnumFilter(
                "rarity",
                L("ritsulib.debugTools.filter.rarity", "Rarity"),
                models.Select(static model => model.Rarity).Distinct(),
                EnumLabel,
                (item, value) => ownedByItemId.TryGetValue(item.Id, out var model) && model.Rarity == value);
            var ownedBrowser = Browser(
                L("ritsulib.debugTools.search.ownedPotions", "Search owned potions"),
                item => ownedByItemId.TryGetValue(item.Id, out var potion) &&
                        slotsByItemId.TryGetValue(item.Id, out var slot)
                    ? CreateOwnedPotionDetail(potion, slot)
                    : EmptyBrowser(L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available.")),
                [rarityFilter, CreateContentSourceFilter(models, ownedByItemId)],
                RitsuCatalogPresentation.Grid,
                detailWidth: 520f);

            var root = new RitsuDebugLiveDetailContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation", 10);
            var ownedButton = ModeButton(
                RitsuDebugToolsGlyph.Inventory,
                L("ritsulib.debugTools.potions.owned", "Owned potions"),
                static () => { });
            var libraryButton = ModeButton(
                RitsuDebugToolsGlyph.Library,
                L("ritsulib.debugTools.potions.library", "All potions"),
                static () => { });
            ownedButton.Pressed += () => SetMode(PotionCatalogMode.Owned);
            libraryButton.Pressed += () => SetMode(PotionCatalogMode.Library);
            root.AddChild(CreateWorkspaceToolbar(
                libraryButton,
                ownedButton,
                L("ritsulib.debugTools.potions.workspaceHint",
                    "Browse the full library by default; switch to owned potions for direct discarding.")));
            root.AddChild(ownedBrowser);
            root.AddChild(libraryBrowser);
            root.RegisterRefresh(RefreshItems);
            RefreshItems();
            SetMode(_potionCatalogMode);
            return root;

            void SetMode(PotionCatalogMode mode)
            {
                _potionCatalogMode = mode;
                var owned = mode == PotionCatalogMode.Owned;
                ownedBrowser.Visible = owned;
                libraryBrowser.Visible = !owned;
                ownedButton.SetSelected(owned);
                libraryButton.SetSelected(!owned);
            }

            void RefreshItems()
            {
                var occupied = TryGetTargetPlayer(out var target)
                    ? target.PotionSlots
                        .Select(static (potion, slot) => (Potion: potion, Slot: slot))
                        .Where(static entry => entry.Potion != null)
                        .Select(static entry => (Potion: entry.Potion!, entry.Slot))
                        .ToArray()
                    : [];
                ownedByItemId.Clear();
                slotsByItemId.Clear();
                var trashIcon = RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Trash,
                    18,
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg);
                var ownedItems = occupied.Select(entry =>
                {
                    var potion = entry.Potion;
                    var itemId = $"{entry.Slot}:{potion.Id}";
                    ownedByItemId[itemId] = potion;
                    slotsByItemId[itemId] = entry.Slot;
                    var source = ContentSourceResolver.Resolve(potion);
                    RitsuCatalogItemAction? quickAction = trashIcon == null
                        ? null
                        : new(
                            trashIcon,
                            L("ritsulib.debugTools.action.discardPotion", "Discard potion"),
                            () => SubmitInventoryAction((requester, actionTarget) =>
                                RitsuDebugInventoryActions.SubmitDiscardPotion(
                                    requester,
                                    actionTarget,
                                    entry.Slot,
                                    potion.Id.ToString())),
                            RitsuCatalogItemActionTone.Danger);
                    return new RitsuCatalogItem(
                        itemId,
                        SafeTitle(potion),
                        $"{EnumLabel(potion.Rarity)} · {ContentSourceDisplayLabel(source)}",
                        $"{potion.Id} {source.ModId} {source.DisplayName}",
                        iconFactory: () => potion.Image,
                        badge: $"#{entry.Slot + 1}",
                        tooltip: BuildCatalogTooltip(
                            SafeTitle(potion),
                            potion.Id.ToString(),
                            ContentSourceDisplayLabel(source)),
                        quickAction: quickAction);
                }).ToArray();
                ownedBrowser.UpdateItems(ownedItems);

                var counts = occupied
                    .GroupBy(static entry => entry.Potion.Id.ToString(), StringComparer.Ordinal)
                    .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
                libraryBrowser.UpdateItems([
                    .. models.Select(model =>
                        CreatePotionLibraryItem(model, counts.GetValueOrDefault(model.Id.ToString()))),
                ]);
                ownedButton.Text = string.Format(
                    L("ritsulib.debugTools.potions.ownedCount", "Owned ({0})"),
                    occupied.Length);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(ownedButton);
            }
        }

        private Control CreatePowerWorkspace(RitsuCatalogBrowser libraryBrowser)
        {
            var powersByItemId = new Dictionary<string, (uint CombatId, int Index, PowerModel Power)>(
                StringComparer.Ordinal);
            var currentBrowser = Browser(
                L("ritsulib.debugTools.search.currentPowers", "Search current Powers"),
                item => CreateLivePowerDetail(item.Id, powersByItemId),
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 260f,
                gridTileHeight: 132f,
                detailWidth: 540f);
            var root = new RitsuDebugLiveDetailContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation", 10);
            var currentView = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            var currentButton = ModeButton(
                RitsuDebugToolsGlyph.Powers,
                L("ritsulib.debugTools.powers.current", "Current Powers"),
                static () => { });
            var libraryButton = ModeButton(
                RitsuDebugToolsGlyph.Library,
                L("ritsulib.debugTools.powers.library", "Power library"),
                static () => { });
            currentButton.Pressed += () => SetMode(PowerCatalogMode.Current);
            libraryButton.Pressed += () => SetMode(PowerCatalogMode.Library);
            root.AddChild(CreateWorkspaceToolbar(
                libraryButton,
                currentButton,
                L("ritsulib.debugTools.powers.workspaceHint",
                    "Browse the full library by default; switch to current Powers for live adjustments.")));

            currentView.AddThemeConstantOverride("separation", 10);
            root.AddChild(currentView);
            var currentToolbar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            currentToolbar.AddThemeConstantOverride("separation", 8);
            var summary = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
            };
            summary.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            summary.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            currentToolbar.AddChild(summary);
            var clearArmed = false;
            var clearButton = IconTextButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.clearPowers", "Clear all"),
                ModSettingsButtonTone.Danger,
                static () => { });
            clearButton.CustomMinimumSize = new(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
            clearButton.Pressed += OnClearPressed;
            currentToolbar.AddChild(clearButton);
            currentView.AddChild(currentToolbar);
            currentView.AddChild(currentBrowser);

            void OnClearPressed()
            {
                var creature = SelectedCreature();
                if (creature?.CombatId is not { } combatId)
                    return;
                if (!clearArmed)
                {
                    clearArmed = true;
                    clearButton.Text = L("ritsulib.debugTools.action.confirmClearPowers", "Confirm clear");
                    clearButton.SetSelected(true);
                    ModSettingsUiControlTheming.RefreshAdaptiveButtonText(clearButton);
                    SetStatus(L("ritsulib.debugTools.confirmClearPowers",
                            "Press the highlighted button again to remove every Power from the selected creature."),
                        false);
                    return;
                }

                clearArmed = false;
                SubmitCreatureOperation(combatId, RitsuDebugCreatureOperation.ClearPowers, 0);
            }

            root.AddChild(libraryBrowser);
            root.RegisterRefresh(RefreshCurrentPowers);

            if (CurrentCreatures().Length == 0)
                _powerCatalogMode = PowerCatalogMode.Library;
            RefreshCurrentPowers();
            SetMode(_powerCatalogMode);
            return root;

            void SetMode(PowerCatalogMode mode)
            {
                _powerCatalogMode = mode;
                var current = mode == PowerCatalogMode.Current;
                currentView.Visible = current;
                libraryBrowser.Visible = !current;
                currentButton.SetSelected(current);
                libraryButton.SetSelected(!current);
            }

            void RefreshCurrentPowers()
            {
                var creature = SelectedCreature();
                var powers = creature?.Powers.ToArray() ?? [];
                powersByItemId.Clear();
                var trashIcon = RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Trash,
                    18,
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg);
                var items = creature == null
                    ? []
                    : powers.Select((power, index) =>
                    {
                        var itemId = $"{creature.CombatId}:{index}";
                        powersByItemId[itemId] = (creature.CombatId!.Value, index, power);
                        var source = ContentSourceResolver.Resolve(power);
                        RitsuCatalogItemAction? quickAction = trashIcon == null
                            ? null
                            : new(
                                trashIcon,
                                L("ritsulib.debugTools.action.removePower", "Remove Power"),
                                () => SubmitPowerInstanceRemoval(
                                    creature.CombatId.Value,
                                    index,
                                    power.Id.ToString()),
                                RitsuCatalogItemActionTone.Danger);
                        return new RitsuCatalogItem(
                            itemId,
                            SafeTitle(power),
                            $"{EnumLabel(power.Type)} · {ContentSourceDisplayLabel(source)}",
                            $"{power.Id} {source.ModId} {source.DisplayName}",
                            iconFactory: () => power.Icon,
                            badge: power.Amount.ToString(),
                            tooltip: BuildCatalogTooltip(
                                SafeTitle(power),
                                power.Id.ToString(),
                                SafeDescription(() => power.Description.GetFormattedText())),
                            quickAction: quickAction,
                            accentColor: PowerTypeAccent(power.Type));
                    }).ToArray();
                currentBrowser.UpdateItems(items);

                if (!clearArmed)
                {
                    clearButton.Text = L("ritsulib.debugTools.action.clearPowers", "Clear all");
                    clearButton.SetSelected(false);
                    ModSettingsUiControlTheming.RefreshAdaptiveButtonText(clearButton);
                }

                clearButton.Disabled = powers.Length == 0;
                summary.Text = string.Format(
                    L("ritsulib.debugTools.powers.activeCount", "Active Powers: {0}"),
                    powers.Length);
                currentButton.Text = string.Format(
                    L("ritsulib.debugTools.powers.currentCount", "Current ({0})"),
                    powers.Length);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
            }

            Creature? SelectedCreature()
            {
                var creatures = CurrentCreatures();
                if (creatures.Length == 0)
                    return null;
                var combatId = PreferredCreatureCombatId(creatures);
                _selectedCreatureCombatId = combatId;
                return creatures.FirstOrDefault(creature => creature.CombatId == combatId);
            }
        }

        private Control CreateOrbWorkspace(IReadOnlyList<OrbModel> models, RitsuCatalogBrowser libraryBrowser)
        {
            var orbsByItemId = new Dictionary<string, (int Index, OrbModel Orb)>(StringComparer.Ordinal);
            var emptySlotsByItemId = new Dictionary<string, int>(StringComparer.Ordinal);
            var currentBrowser = Browser(
                L("ritsulib.debugTools.search.currentOrbs", "Search current orbs"),
                item => CreateLiveOrbSlotDetail(item.Id, orbsByItemId, emptySlotsByItemId, models),
                presentation: RitsuCatalogPresentation.Grid,
                gridTileMinimumWidth: 260f,
                gridTileHeight: 132f,
                detailWidth: 540f);
            var root = new RitsuDebugLiveDetailContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation", 10);
            var currentView = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            currentView.AddThemeConstantOverride("separation", 10);
            var currentButton = ModeButton(
                RitsuDebugToolsGlyph.Orbs,
                L("ritsulib.debugTools.orbs.current", "Current orbs"),
                static () => { });
            var libraryButton = ModeButton(
                RitsuDebugToolsGlyph.Library,
                L("ritsulib.debugTools.orbs.library", "Orb library"),
                static () => { });
            currentButton.Pressed += () => SetMode(OrbCatalogMode.Current);
            libraryButton.Pressed += () => SetMode(OrbCatalogMode.Library);
            root.AddChild(CreateWorkspaceToolbar(
                libraryButton,
                currentButton,
                L("ritsulib.debugTools.orbs.workspaceHint",
                    "Browse all orb types by default; switch to the current queue for live changes.")));
            root.AddChild(currentView);

            var summary = new Label { VerticalAlignment = VerticalAlignment.Center };
            summary.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            summary.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            var slotEdit = CreateIntegerEdit("0");
            var slotEditChanged = false;
            var updatingSlotEdit = false;
            slotEdit.TextChanged += _ =>
            {
                if (!updatingSlotEdit)
                    slotEditChanged = true;
            };
            var slotApply = IconTextButton(
                RitsuDebugToolsGlyph.Sliders,
                L("ritsulib.debugTools.action.setOrbSlots", "Set slots"),
                ModSettingsButtonTone.Normal,
                SetSlots);
            var toolbar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddThemeConstantOverride("separation", 10);
            summary.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            toolbar.AddChild(summary);
            var slotField = ActionField(
                L("ritsulib.debugTools.field.orbSlots", "Orb slots"),
                slotEdit,
                slotApply);
            slotField.CustomMinimumSize = new(390f, 0f);
            toolbar.AddChild(slotField);
            currentView.AddChild(toolbar);
            AddHint(currentView, L("ritsulib.debugTools.orbs.slotReduction",
                "Reducing slots removes orbs from the back of the queue without evoking them."));
            currentView.AddChild(currentBrowser);
            root.AddChild(libraryBrowser);
            root.RegisterRefresh(RefreshCurrentOrbs);

            if (!TryGetTargetPlayer(out var initialPlayer) || !HasActiveCombatState(initialPlayer))
                _orbCatalogMode = OrbCatalogMode.Library;
            RefreshCurrentOrbs();
            SetMode(_orbCatalogMode);
            return root;

            void SetMode(OrbCatalogMode mode)
            {
                _orbCatalogMode = mode;
                var current = mode == OrbCatalogMode.Current;
                currentView.Visible = current;
                libraryBrowser.Visible = !current;
                currentButton.SetSelected(current);
                libraryButton.SetSelected(!current);
            }

            void SetSlots()
            {
                if (!TryReadInt(slotEdit, 0, RitsuDebugOrbActions.MaximumOrbSlots, out var capacity) ||
                    !TryGetActionContext(out var requester, out var target))
                    return;
                if (RunAction(() => RitsuDebugOrbActions.SubmitSetOrbSlots(requester, target, capacity)))
                    slotEditChanged = false;
            }

            void RefreshCurrentOrbs()
            {
                if (!TryGetTargetPlayer(out var player) || !HasActiveCombatState(player) ||
                    player.PlayerCombatState?.OrbQueue is not { } queue)
                {
                    orbsByItemId.Clear();
                    emptySlotsByItemId.Clear();
                    currentBrowser.UpdateItems([]);
                    summary.Text = L("ritsulib.debugTools.orbs.unavailable", "Orb queue unavailable");
                    RefreshSlotEditor("0");
                    slotApply.Disabled = true;
                    currentButton.Text = L("ritsulib.debugTools.orbs.current", "Current orbs");
                    ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
                    return;
                }

                var orbs = queue.Orbs.ToArray();
                orbsByItemId.Clear();
                emptySlotsByItemId.Clear();
                var trashIcon = RitsuDebugToolsIcons.Get(
                    RitsuDebugToolsGlyph.Trash,
                    18,
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg);
                var items = new List<RitsuCatalogItem>(queue.Capacity);
                for (var index = 0; index < orbs.Length; index++)
                {
                    var orbIndex = index;
                    var orb = orbs[index];
                    var itemId = $"{player.NetId}:{index}";
                    orbsByItemId[itemId] = (orbIndex, orb);
                    RitsuCatalogItemAction? quickAction = trashIcon == null
                        ? null
                        : new(
                            trashIcon,
                            L("ritsulib.debugTools.action.removeOrb", "Remove orb"),
                            () => SubmitOrbRemoval(orbIndex, orb.Id.ToString()),
                            RitsuCatalogItemActionTone.Danger);
                    items.Add(new(
                        itemId,
                        SafeTitle(orb),
                        string.Format(
                            L("ritsulib.debugTools.orbs.values", "Passive {0} · Evoke {1}"),
                            SafeOrbValue(() => orb.PassiveVal),
                            SafeOrbValue(() => orb.EvokeVal)),
                        orb.Id.ToString(),
                        iconFactory: () => orb.Icon,
                        badge: $"#{index + 1}",
                        tooltip: BuildCatalogTooltip(
                            SafeTitle(orb),
                            orb.Id.ToString(),
                            SafeDescription(() => orb.Description.GetFormattedText())),
                        quickAction: quickAction,
                        accentColor: orb.DarkenedColor));
                }

                for (var index = orbs.Length; index < queue.Capacity; index++)
                {
                    var itemId = $"{player.NetId}:{index}";
                    emptySlotsByItemId[itemId] = index;
                    items.Add(new(
                        itemId,
                        L("ritsulib.debugTools.orbs.emptySlot", "Empty orb slot"),
                        badge: $"#{index + 1}"));
                }

                currentBrowser.UpdateItems(items);

                summary.Text = string.Format(
                    L("ritsulib.debugTools.orbs.queueSummary", "{0} orbs · {1} slots"),
                    orbs.Length,
                    queue.Capacity);
                RefreshSlotEditor(queue.Capacity.ToString());
                slotApply.Disabled = false;
                currentButton.Text = string.Format(
                    L("ritsulib.debugTools.orbs.currentCount", "Current ({0}/{1})"),
                    orbs.Length,
                    queue.Capacity);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
            }

            void RefreshSlotEditor(string value)
            {
                if (slotEditChanged || slotEdit.HasFocus() || slotEdit.Text == value)
                    return;
                updatingSlotEdit = true;
                slotEdit.Text = value;
                updatingSlotEdit = false;
            }

            void SubmitOrbRemoval(int index, string orbId)
            {
                if (!TryGetActionContext(out var requester, out var target))
                    return;
                RunAction(() => RitsuDebugOrbActions.SubmitRemoveOrb(requester, target, index, orbId));
            }
        }

        private Control CreateLiveOrbSlotDetail(
            string itemId,
            IReadOnlyDictionary<string, (int Index, OrbModel Orb)> orbsByItemId,
            IReadOnlyDictionary<string, int> emptySlotsByItemId,
            IReadOnlyList<OrbModel> models)
        {
            var host = new RitsuDebugLiveDetailContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var renderedAvailable = TryResolve(out var renderedIndex, out var renderedOrb);
            RebuildContent();
            host.RegisterRefresh(RefreshState);
            return host;

            void RefreshState()
            {
                var available = TryResolve(out var index, out var orb);
                if (available == renderedAvailable && index == renderedIndex && ReferenceEquals(orb, renderedOrb))
                    return;
                renderedAvailable = available;
                renderedIndex = index;
                renderedOrb = orb;
                RebuildContent();
            }

            void RebuildContent()
            {
                foreach (var child in host.GetChildren())
                {
                    host.RemoveChild(child);
                    child.QueueFree();
                }

                if (!renderedAvailable)
                {
                    AddHint(host, L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available."));
                    return;
                }

                host.AddChild(renderedOrb == null
                    ? CreateEmptyOrbSlotDetail(renderedIndex)
                    : CreateCurrentOrbDetail(renderedIndex, renderedOrb, models));
            }

            bool TryResolve(out int index, out OrbModel? orb)
            {
                if (orbsByItemId.TryGetValue(itemId, out var entry))
                {
                    index = entry.Index;
                    orb = entry.Orb;
                    return true;
                }

                if (emptySlotsByItemId.TryGetValue(itemId, out index))
                {
                    orb = null;
                    return true;
                }

                index = -1;
                orb = null;
                return false;
            }
        }

        private Control CreateCurrentOrbDetail(int index, OrbModel orb, IReadOnlyList<OrbModel> models)
        {
            var root = DetailShell(
                orb.Id.ToString(),
                () => orb.Icon,
                OrbMetadata(),
                SafeDescription(() => orb.Description.GetFormattedText()),
                OrbMetadata,
                () => SafeDescription(() => orb.Description.GetFormattedText()));
            var editorContent = CreateAdjustmentContent();
            var replacement = orb.Id.ToString();
            editorContent.AddChild(DropdownField(
                L("ritsulib.debugTools.field.orbType", "Orb type"),
                models.Select(model => (model.Id.ToString(), SafeTitle(model))).ToArray(),
                replacement,
                value => replacement = value));
            editorContent.AddChild(ActionButton(
                L("ritsulib.debugTools.action.replaceOrb", "Replace orb"),
                ModSettingsButtonTone.Normal,
                Replace));
            AddHint(editorContent, L("ritsulib.debugTools.orbs.computedValuesHint",
                "Passive and evoke values are computed by each orb and the current combat state."));
            root.AddChild(AdjustmentSection(
                L("ritsulib.debugTools.action.adjustOrb", "Adjust orb"),
                editorContent));
            AddCapabilitySection(
                root,
                new(RitsuDebugCapabilityTargetKind.Orb, orb.Id.ToString(), index),
                orb);
            root.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.removeOrb", "Remove orb"),
                ModSettingsButtonTone.Danger,
                Remove));
            return root;

            string OrbMetadata()
            {
                return $"#{index + 1} · {string.Format(
                    L("ritsulib.debugTools.orbs.values", "Passive {0} · Evoke {1}"),
                    SafeOrbValue(() => orb.PassiveVal),
                    SafeOrbValue(() => orb.EvokeVal))}";
            }

            void Replace()
            {
                if (!TryGetActionContext(out var requester, out var target))
                    return;
                RunAction(() => RitsuDebugOrbActions.SubmitReplaceOrb(
                    requester,
                    target,
                    index,
                    orb.Id.ToString(),
                    replacement));
            }

            void Remove()
            {
                if (!TryGetActionContext(out var requester, out var target))
                    return;
                RunAction(() => RitsuDebugOrbActions.SubmitRemoveOrb(
                    requester,
                    target,
                    index,
                    orb.Id.ToString()));
            }
        }

        private static Control CreateEmptyOrbSlotDetail(int index)
        {
            var root = new RitsuDebugLiveDetailContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddHint(root, $"#{index + 1} · {L("ritsulib.debugTools.orbs.emptySlot", "Empty orb slot")}");
            return root;
        }

        private static string SafeOrbValue(Func<decimal> valueFactory)
        {
            try
            {
                return valueFactory().ToString("0.##");
            }
            catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
            {
                return "—";
            }
        }

        private RitsuCatalogItem CreateRelicLibraryItem(RelicModel relic, int ownedCount)
        {
            var source = ContentSourceResolver.Resolve(relic);
            return new(
                relic.Id.ToString(),
                SafeTitle(relic),
                $"{EnumLabel(relic.Rarity)} · {ContentSourceDisplayLabel(source)} · {relic.Id}",
                $"{relic.Id.Category} {source.ModId} {source.DisplayName}",
                iconFactory: () => relic.Icon,
                badge: ownedCount > 0
                    ? string.Format(L("ritsulib.debugTools.relics.ownedBadge", "Owned ×{0}"), ownedCount)
                    : EnumLabel(relic.Rarity));
        }

        private RitsuCatalogItem CreatePotionLibraryItem(PotionModel potion, int ownedCount)
        {
            var source = ContentSourceResolver.Resolve(potion);
            return new(
                potion.Id.ToString(),
                SafeTitle(potion),
                $"{EnumLabel(potion.Rarity)} · {ContentSourceDisplayLabel(source)} · {potion.Id}",
                $"{potion.Id.Category} {source.ModId} {source.DisplayName}",
                iconFactory: () => potion.Image,
                badge: ownedCount > 0
                    ? string.Format(L("ritsulib.debugTools.potions.ownedBadge", "Owned ×{0}"), ownedCount)
                    : EnumLabel(potion.Rarity));
        }

        private Control CreateOwnedRelicDetail(RelicModel relic, int relicIndex)
        {
            var source = ContentSourceResolver.Resolve(relic);
            var root = DetailShell(
                relic.Id.ToString(),
                () => relic.BigIcon,
                $"{EnumLabel(relic.Rarity)} · {ContentSourceDisplayLabel(source)}",
                SafeDescription(() => relic.DynamicDescription.GetFormattedText()));
            root.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.removeRelic", "Remove this relic"),
                ModSettingsButtonTone.Danger,
                () => SubmitInventoryAction((requester, target) =>
                    RitsuDebugInventoryActions.SubmitRemoveRelic(
                        requester,
                        target,
                        relic.Id.ToString(),
                        relicIndex))));
            var settings = CreateAdjustmentContent();
            var dynamicVariables = CreateDynamicVariableEditors(settings, relic.DynamicVars);
            if (dynamicVariables.HasEditors)
            {
                settings.AddChild(ActionButton(
                    L("ritsulib.debugTools.action.applyChanges", "Apply changes"),
                    ModSettingsButtonTone.Accent,
                    Apply));
                root.AddChild(AdjustmentSection(
                    L("ritsulib.debugTools.action.adjustValues", "Adjust values"),
                    settings));
            }

            AddCapabilitySection(
                root,
                new(RitsuDebugCapabilityTargetKind.Relic, relic.Id.ToString(), relicIndex),
                relic);
            return root;

            void Apply()
            {
                if (!TryReadDynamicVariableOverrides(dynamicVariables, out var overrides))
                    return;
                if (overrides is not { Count: > 0 })
                {
                    SetStatus(L("ritsulib.debugTools.changeValueRequired",
                        "Change at least one value before applying."), true);
                    return;
                }

                SubmitInventoryAction((requester, target) => RitsuDebugInventoryActions.SubmitEditRelic(
                    requester,
                    target,
                    relic.Id.ToString(),
                    overrides,
                    relicIndex));
            }
        }

        private Control CreateOwnedPotionDetail(PotionModel potion, int slot)
        {
            var source = ContentSourceResolver.Resolve(potion);
            var root = DetailShell(
                potion.Id.ToString(),
                () => PotionPreviewImage(potion),
                $"{EnumLabel(potion.Rarity)} · {ContentSourceDisplayLabel(source)}",
                SafeDescription(() => potion.Description.GetFormattedText()));
            root.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.discardPotion", "Discard potion"),
                ModSettingsButtonTone.Danger,
                () => SubmitInventoryAction((requester, target) =>
                    RitsuDebugInventoryActions.SubmitDiscardPotion(
                        requester,
                        target,
                        slot,
                        potion.Id.ToString()))));
            var settings = CreateAdjustmentContent();
            var dynamicVariables = CreateDynamicVariableEditors(settings, potion.DynamicVars);
            if (dynamicVariables.HasEditors)
            {
                settings.AddChild(ActionButton(
                    L("ritsulib.debugTools.action.applyChanges", "Apply changes"),
                    ModSettingsButtonTone.Accent,
                    Apply));
                root.AddChild(AdjustmentSection(
                    L("ritsulib.debugTools.action.adjustValues", "Adjust values"),
                    settings));
            }

            AddCapabilitySection(
                root,
                new(RitsuDebugCapabilityTargetKind.Potion, potion.Id.ToString(), slot),
                potion);
            return root;

            void Apply()
            {
                if (!TryReadDynamicVariableOverrides(dynamicVariables, out var overrides))
                    return;
                if (overrides is not { Count: > 0 })
                {
                    SetStatus(L("ritsulib.debugTools.changeValueRequired",
                        "Change at least one value before applying."), true);
                    return;
                }

                SubmitInventoryAction((requester, target) => RitsuDebugInventoryActions.SubmitEditPotion(
                    requester,
                    target,
                    slot,
                    potion.Id.ToString(),
                    overrides));
            }
        }

        private Control CreateLivePowerDetail(
            string itemId,
            IReadOnlyDictionary<string, (uint CombatId, int Index, PowerModel Power)> powersByItemId)
        {
            var host = new RitsuDebugLiveDetailContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var renderedAvailable = TryResolve(out var renderedCombatId, out var renderedIndex, out var renderedPower);
            RebuildContent();
            host.RegisterRefresh(RefreshState);
            return host;

            void RefreshState()
            {
                var available = TryResolve(out var combatId, out var index, out var power);
                if (available == renderedAvailable &&
                    combatId == renderedCombatId &&
                    index == renderedIndex &&
                    ReferenceEquals(power, renderedPower))
                    return;
                renderedAvailable = available;
                renderedCombatId = combatId;
                renderedIndex = index;
                renderedPower = power;
                RebuildContent();
            }

            void RebuildContent()
            {
                foreach (var child in host.GetChildren())
                {
                    host.RemoveChild(child);
                    child.QueueFree();
                }

                if (!renderedAvailable || renderedPower == null)
                {
                    AddHint(host, L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available."));
                    return;
                }

                host.AddChild(CreateCurrentPowerDetail(renderedCombatId, renderedIndex, renderedPower));
            }

            bool TryResolve(out uint combatId, out int index, out PowerModel? power)
            {
                if (powersByItemId.TryGetValue(itemId, out var entry))
                {
                    combatId = entry.CombatId;
                    index = entry.Index;
                    power = entry.Power;
                    return true;
                }

                combatId = 0u;
                index = -1;
                power = null;
                return false;
            }
        }

        private Control CreateCurrentPowerDetail(uint combatId, int index, PowerModel power)
        {
            var source = ContentSourceResolver.Resolve(power);
            var root = DetailShell(
                power.Id.ToString(),
                () => power.Icon,
                PowerMetadata(),
                SafeDescription(() => power.Description.GetFormattedText()),
                PowerMetadata,
                () => SafeDescription(() => power.Description.GetFormattedText()));
            var quickActions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            quickActions.AddThemeConstantOverride("separation", 8);
            quickActions.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Minus,
                L("ritsulib.debugTools.action.decreasePower", "Decrease by 1"),
                ModSettingsButtonTone.Normal,
                () => SubmitPowerInstanceAdjustment(combatId, index, power.Id.ToString(), -1)));
            quickActions.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Plus,
                L("ritsulib.debugTools.action.increasePower", "Increase by 1"),
                ModSettingsButtonTone.Normal,
                () => SubmitPowerInstanceAdjustment(combatId, index, power.Id.ToString(), 1)));
            root.AddChild(quickActions);
            var editorContent = CreateAdjustmentContent();
            var amountChanged = false;
            var amountEditor = CreateIntegerEdit(power.Amount.ToString());
            amountEditor.TextChanged += _ => amountChanged = true;
            editorContent.AddChild(Field(L("ritsulib.debugTools.field.amount", "Stack amount"), amountEditor));
            var dynamicVariables = CreateDynamicVariableEditors(editorContent, power.DynamicVars);
            editorContent.AddChild(ActionButton(
                L("ritsulib.debugTools.action.applyChanges", "Apply changes"),
                ModSettingsButtonTone.Accent,
                ApplyChanges));
            root.AddChild(AdjustmentSection(
                L("ritsulib.debugTools.action.adjustValues", "Adjust values"),
                editorContent));
            AddCapabilitySection(
                root,
                new(
                    RitsuDebugCapabilityTargetKind.Power,
                    power.Id.ToString(),
                    index,
                    CreatureCombatId: combatId),
                power);
            root.AddChild(IconTextButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.removePower", "Remove Power"),
                ModSettingsButtonTone.Danger,
                () => SubmitPowerInstanceRemoval(combatId, index, power.Id.ToString())));
            root.RegisterRefresh(RefreshEditors);
            return root;

            string PowerMetadata()
            {
                return $"#{index + 1} · {EnumLabel(power.Type)} · {ContentSourceDisplayLabel(source)} · {power.Amount}";
            }

            void RefreshEditors()
            {
                if (!amountChanged && !amountEditor.HasFocus())
                {
                    amountEditor.Text = power.Amount.ToString();
                    amountChanged = false;
                }

                RefreshDynamicVariableEditors(dynamicVariables, power.DynamicVars);
            }

            void ApplyChanges()
            {
                int? desiredAmount = null;
                if (amountChanged)
                {
                    var minimum = power.AllowNegative ? -RitsuDebugCombatActions.MaxAmount : 0;
                    if (!TryReadInt(amountEditor, minimum, RitsuDebugCombatActions.MaxAmount, out var parsedAmount))
                        return;
                    desiredAmount = parsedAmount;
                }

                if (!TryReadDynamicVariableOverrides(dynamicVariables, out var overrides))
                    return;
                if (!desiredAmount.HasValue && overrides is not { Count: > 0 })
                {
                    SetStatus(L("ritsulib.debugTools.changeValueRequired",
                        "Change at least one value before applying."), true);
                    return;
                }

                if (!TryGetActionContext(out var requester, out var target))
                    return;
                RunAction(() => RitsuDebugCombatActions.SubmitEditPowerInstance(
                    requester,
                    target,
                    combatId,
                    index,
                    power.Id.ToString(),
                    desiredAmount,
                    overrides));
            }
        }

        private void SubmitPowerInstanceAdjustment(uint combatId, int index, string powerId, int offset)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitAdjustPowerInstance(
                requester,
                target,
                combatId,
                index,
                powerId,
                offset));
        }

        private void SubmitPowerInstanceRemoval(uint combatId, int index, string powerId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitRemovePowerInstance(
                requester,
                target,
                combatId,
                index,
                powerId));
        }

        private static HBoxContainer CreateWorkspaceToolbar(
            Button firstMode,
            Button secondMode,
            string hint)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(firstMode);
            row.AddChild(secondMode);
            var label = new Label
            {
                Text = hint,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            row.AddChild(label);
            return row;
        }

        private static ModSettingsTextButton ModeButton(
            RitsuDebugToolsGlyph glyph,
            string text,
            Action action)
        {
            var button = IconTextButton(glyph, text, ModSettingsButtonTone.Normal, action);
            button.CustomMinimumSize = new(146f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
            return button;
        }

        private static ModSettingsTextButton IconTextButton(
            RitsuDebugToolsGlyph glyph,
            string text,
            ModSettingsButtonTone tone,
            Action action)
        {
            var iconColor = tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };
            var button = new ModSettingsTextButton(text, tone, action)
            {
                Icon = RitsuDebugToolsIcons.Get(glyph, 18, iconColor),
                ExpandIcon = false,
                TooltipText = text,
            };
            return button;
        }

        private enum RelicCatalogMode
        {
            Owned,
            Library,
        }

        private enum PowerCatalogMode
        {
            Current,
            Library,
        }

        private enum OrbCatalogMode
        {
            Current,
            Library,
        }

        private enum PotionCatalogMode
        {
            Owned,
            Library,
        }

        private enum PowerApplyScope
        {
            Selected,
            AllCreatures,
            AllPlayers,
            AllEnemies,
        }
    }
}
