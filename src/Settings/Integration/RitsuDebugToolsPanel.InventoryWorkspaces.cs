using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
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

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(scroll);
            currentView.AddChild(scroll);
            var cards = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cards.AddThemeConstantOverride("h_separation", 10);
            cards.AddThemeConstantOverride("v_separation", 10);
            scroll.AddChild(cards);
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
                foreach (var child in cards.GetChildren())
                {
                    cards.RemoveChild(child);
                    child.QueueFree();
                }

                var creature = SelectedCreature();
                var powers = creature?.Powers.ToArray() ?? [];
                clearButton.Disabled = powers.Length == 0;
                if (powers.Length == 0)
                {
                    cards.AddChild(CreatePowerListHint(creature == null
                        ? L("ritsulib.debugTools.noCombat", "Start combat to manage current Powers.")
                        : L("ritsulib.debugTools.noActivePowers", "No active Powers.")));
                }
                else
                {
                    for (var index = 0; index < powers.Length; index++)
                        cards.AddChild(CreateCurrentPowerCard(creature!.CombatId!.Value, index, powers[index]));
                }

                summary.Text = string.Format(
                    L("ritsulib.debugTools.powers.activeCount", "Active Powers: {0}"),
                    powers.Length);
                currentButton.Text = string.Format(
                    L("ritsulib.debugTools.powers.currentCount", "Current ({0})"),
                    powers.Length);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
                if (clearArmed)
                {
                    clearArmed = false;
                    clearButton.Text = L("ritsulib.debugTools.action.clearPowers", "Clear all");
                    clearButton.SetSelected(false);
                    ModSettingsUiControlTheming.RefreshAdaptiveButtonText(clearButton);
                }
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

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(scroll);
            currentView.AddChild(scroll);
            var cards = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cards.AddThemeConstantOverride("h_separation", 10);
            cards.AddThemeConstantOverride("v_separation", 10);
            scroll.AddChild(cards);
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
                RunAction(() => RitsuDebugOrbActions.SubmitSetOrbSlots(requester, target, capacity));
            }

            void RefreshCurrentOrbs()
            {
                foreach (var child in cards.GetChildren())
                {
                    cards.RemoveChild(child);
                    child.QueueFree();
                }

                if (!TryGetTargetPlayer(out var player) || !HasActiveCombatState(player) ||
                    player.PlayerCombatState?.OrbQueue is not { } queue)
                {
                    cards.AddChild(CreatePowerListHint(L(
                        "ritsulib.debugTools.orbs.combatRequired",
                        "Start combat to channel or manage orbs.")));
                    summary.Text = L("ritsulib.debugTools.orbs.unavailable", "Orb queue unavailable");
                    slotEdit.Text = "0";
                    slotApply.Disabled = true;
                    currentButton.Text = L("ritsulib.debugTools.orbs.current", "Current orbs");
                    ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
                    return;
                }

                var orbs = queue.Orbs.ToArray();
                for (var index = 0; index < orbs.Length; index++)
                    cards.AddChild(CreateCurrentOrbCard(index, orbs[index], models));
                for (var index = orbs.Length; index < queue.Capacity; index++)
                    cards.AddChild(CreateEmptyOrbSlotCard(index));
                if (queue.Capacity == 0)
                    cards.AddChild(CreatePowerListHint(L(
                        "ritsulib.debugTools.orbs.noSlots",
                        "This player has no orb slots. Set a slot count to begin.")));

                summary.Text = string.Format(
                    L("ritsulib.debugTools.orbs.queueSummary", "{0} orbs · {1} slots"),
                    orbs.Length,
                    queue.Capacity);
                slotEdit.Text = queue.Capacity.ToString();
                slotApply.Disabled = false;
                currentButton.Text = string.Format(
                    L("ritsulib.debugTools.orbs.currentCount", "Current ({0}/{1})"),
                    orbs.Length,
                    queue.Capacity);
                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(currentButton);
            }
        }

        private Control CreateCurrentOrbCard(int index, OrbModel orb, IReadOnlyList<OrbModel> models)
        {
            var panel = new PanelContainer { CustomMinimumSize = new(278f, 150f) };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var panelContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panelContent.AddThemeConstantOverride("separation", 0);
            panel.AddChild(panelContent);
            panelContent.AddChild(new ColorRect
            {
                Color = orb.DarkenedColor,
                CustomMinimumSize = new(0f, 4f),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 10);
            margin.AddThemeConstantOverride("margin_top", 9);
            margin.AddThemeConstantOverride("margin_right", 10);
            margin.AddThemeConstantOverride("margin_bottom", 9);
            panelContent.AddChild(margin);
            var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 8);
            margin.AddChild(column);

            var identityRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identityRow.AddThemeConstantOverride("separation", 9);
            column.AddChild(identityRow);
            identityRow.AddChild(new TextureRect
            {
                Texture = orb.Icon,
                CustomMinimumSize = new(42f, 42f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identity.AddThemeConstantOverride("separation", 1);
            identityRow.AddChild(identity);
            var title = new Label
            {
                Text = SafeTitle(orb),
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            identity.AddChild(title);
            var metadata = new Label
            {
                Text = string.Format(
                    L("ritsulib.debugTools.orbs.values", "Passive {0} · Evoke {1}"),
                    SafeOrbValue(() => orb.PassiveVal),
                    SafeOrbValue(() => orb.EvokeVal)),
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            metadata.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            metadata.AddThemeFontSizeOverride("font_size", DetailIdentifierFontSize);
            metadata.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            identity.AddChild(metadata);
            var position = new Label
            {
                Text = $"#{index + 1}",
                CustomMinimumSize = new(42f, 0f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            position.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            position.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            identityRow.AddChild(position);

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
            var editorPanel = CreateAdjustmentPanel(editorContent);
            editorPanel.Visible = false;

            var actions = new HBoxContainer
            {
                Alignment = AlignmentMode.End,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            actions.AddThemeConstantOverride("separation", 6);
            column.AddChild(actions);
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Sliders,
                L("ritsulib.debugTools.action.adjustOrb", "Adjust orb"),
                ModSettingsButtonTone.Normal,
                () => editorPanel.Visible = !editorPanel.Visible));
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.removeOrb", "Remove orb"),
                ModSettingsButtonTone.Danger,
                Remove));
            column.AddChild(editorPanel);
            panel.TooltipText = BuildCatalogTooltip(
                SafeTitle(orb),
                orb.Id.ToString(),
                SafeDescription(() => orb.Description.GetFormattedText()));
            return panel;

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

        private static Control CreateEmptyOrbSlotCard(int index)
        {
            var panel = new PanelContainer { CustomMinimumSize = new(278f, 76f) };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 12);
            margin.AddThemeConstantOverride("margin_top", 10);
            margin.AddThemeConstantOverride("margin_right", 12);
            margin.AddThemeConstantOverride("margin_bottom", 10);
            panel.AddChild(margin);
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 10);
            margin.AddChild(row);
            var label = new Label
            {
                Text = $"#{index + 1}",
                CustomMinimumSize = new(42f, 0f),
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            row.AddChild(label);
            var empty = new Label
            {
                Text = L("ritsulib.debugTools.orbs.emptySlot", "Empty orb slot"),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
            };
            empty.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            empty.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            row.AddChild(empty);
            return panel;
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

        private Control CreateCurrentPowerCard(uint combatId, int index, PowerModel power)
        {
            var panel = new PanelContainer { CustomMinimumSize = new(278f, 132f) };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var panelContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panelContent.AddThemeConstantOverride("separation", 0);
            panel.AddChild(panelContent);
            panelContent.AddChild(new ColorRect
            {
                Color = PowerTypeAccent(power.Type),
                CustomMinimumSize = new(0f, 4f),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 10);
            margin.AddThemeConstantOverride("margin_top", 9);
            margin.AddThemeConstantOverride("margin_right", 10);
            margin.AddThemeConstantOverride("margin_bottom", 9);
            panelContent.AddChild(margin);
            var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 8);
            margin.AddChild(column);
            var identityRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identityRow.AddThemeConstantOverride("separation", 9);
            column.AddChild(identityRow);
            Texture2D? icon = null;
            try
            {
                icon = power.Icon;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Could not load active Power icon for '{power.Id}': {ex.Message}");
            }

            identityRow.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new(42f, 42f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Visible = icon != null,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identity.AddThemeConstantOverride("separation", 1);
            identityRow.AddChild(identity);
            var title = new Label
            {
                Text = SafeTitle(power),
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            identity.AddChild(title);
            var source = ContentSourceResolver.Resolve(power);
            var metadata = new Label
            {
                Text = $"{EnumLabel(power.Type)} · {ContentSourceDisplayLabel(source)}",
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            metadata.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            metadata.AddThemeFontSizeOverride("font_size", DetailIdentifierFontSize);
            metadata.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            identity.AddChild(metadata);
            var amount = new Label
            {
                Text = power.Amount.ToString(),
                CustomMinimumSize = new(44f, 0f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            amount.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            amount.AddThemeFontSizeOverride("font_size", 20);
            amount.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            identityRow.AddChild(amount);

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
            var editorPanel = CreateAdjustmentPanel(editorContent);
            editorPanel.Visible = false;

            var actions = new HBoxContainer
            {
                Alignment = AlignmentMode.End,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            actions.AddThemeConstantOverride("separation", 6);
            column.AddChild(actions);
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Minus,
                L("ritsulib.debugTools.action.decreasePower", "Decrease by 1"),
                ModSettingsButtonTone.Normal,
                () => SubmitPowerInstanceAdjustment(combatId, index, power.Id.ToString(), -1)));
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Plus,
                L("ritsulib.debugTools.action.increasePower", "Increase by 1"),
                ModSettingsButtonTone.Normal,
                () => SubmitPowerInstanceAdjustment(combatId, index, power.Id.ToString(), 1)));
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Sliders,
                L("ritsulib.debugTools.action.adjustValues", "Adjust values"),
                ModSettingsButtonTone.Normal,
                () => editorPanel.Visible = !editorPanel.Visible));
            actions.AddChild(IconButton(
                RitsuDebugToolsGlyph.Trash,
                L("ritsulib.debugTools.action.removePower", "Remove Power"),
                ModSettingsButtonTone.Danger,
                () => SubmitPowerInstanceRemoval(combatId, index, power.Id.ToString())));
            column.AddChild(editorPanel);
            panel.TooltipText = BuildCatalogTooltip(
                SafeTitle(power),
                power.Id.ToString(),
                SafeDescription(() => power.Description.GetFormattedText()));
            return panel;

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

        private static Button IconButton(
            RitsuDebugToolsGlyph glyph,
            string tooltip,
            ModSettingsButtonTone tone,
            Action action)
        {
            var iconColor = tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };
            var button = new RitsuDebugToolsIconButton(38f, 36f);
            button.Configure(RitsuDebugToolsIcons.Get(glyph, 17, iconColor), tooltip, tone);
            button.Pressed += action;
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
