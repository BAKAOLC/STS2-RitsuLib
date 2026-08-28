using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Search;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugStatePresetEditor
    {
        private void ShowCardPicker(RitsuDebugStatePresetCardPile pile)
        {
            ShowModelPicker(
                L("ritsulib.debugTools.statePresets.addCard", "Add card"),
                ModelDb.AllCards,
                static card => card.Portrait,
                card =>
                {
                    if (pile.Cards.Sum(static saved => saved.Count) >=
                        RitsuDebugStatePresetStore.MaximumCardsPerPile)
                    {
                        _setStatus(string.Format(
                            L("ritsulib.debugTools.statePresets.cardLimit",
                                "A pile can contain at most {0} saved cards."),
                            RitsuDebugStatePresetStore.MaximumCardsPerPile), true);
                        return false;
                    }

                    pile.Cards.Add(new() { CardId = card.Id.ToString() });
                    MarkDirty(true);
                    return true;
                },
                subtitle: card =>
                    $"{EnumLabel(card.Type)} · {EnumLabel(card.Rarity)} · {card.Id}");
        }

        private void ShowRelicPicker(RitsuDebugStatePresetInventory relics)
        {
            var existing = relics.ModelIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            ShowModelPicker(
                L("ritsulib.debugTools.statePresets.addRelic", "Add relic"),
                ModelDb.AllRelics,
                static model => model.Icon,
                model =>
                {
                    if (relics.ModelIds.Count >= RitsuDebugStatePresetStore.MaximumRelics)
                    {
                        ShowCollectionLimit(RitsuDebugStatePresetStore.MaximumRelics);
                        return false;
                    }

                    var id = model.Id.ToString();
                    if (!existing.Add(id))
                        return false;
                    relics.ModelIds.Add(id);
                    MarkDirty();
                    return true;
                },
                model => !existing.Contains(model.Id.ToString()));
        }

        private void ShowPotionPicker(RitsuDebugStatePresetPotions potions)
        {
            ShowModelPicker(
                L("ritsulib.debugTools.statePresets.addPotion", "Add potion"),
                ModelDb.AllPotions,
                static model => model.Image,
                model =>
                {
                    if (potions.Items.Count >= RitsuDebugPlayerActions.MaxPotionSlots)
                    {
                        ShowCollectionLimit(RitsuDebugPlayerActions.MaxPotionSlots);
                        return false;
                    }

                    var slot = potions.ApplyMode == RitsuDebugStatePresetApplyMode.Replace
                        ? Enumerable.Range(0, RitsuDebugPlayerActions.MaxPotionSlots)
                            .First(index => potions.Items.All(item => item.SlotIndex != index))
                        : (int?)null;
                    potions.Items.Add(new() { PotionId = model.Id.ToString(), SlotIndex = slot });
                    MarkDirty();
                    return true;
                });
        }

        private void ShowPowerPicker(RitsuDebugStatePresetPowers powers)
        {
            var existing = powers.Items.Select(static power => power.PowerId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            ShowModelPicker(
                L("ritsulib.debugTools.statePresets.addPower", "Add power"),
                ModelDb.AllPowers,
                static model => model.Icon,
                model =>
                {
                    if (powers.Items.Count >= RitsuDebugStatePresetStore.MaximumPowers)
                    {
                        ShowCollectionLimit(RitsuDebugStatePresetStore.MaximumPowers);
                        return false;
                    }

                    var id = model.Id.ToString();
                    if (!existing.Add(id))
                        return false;
                    powers.Items.Add(new() { PowerId = id, Amount = 1 });
                    MarkDirty();
                    return true;
                },
                model => !existing.Contains(model.Id.ToString()));
        }

        private void ShowModelPicker<TModel>(
            string title,
            IEnumerable<TModel> source,
            Func<TModel, Texture2D?> icon,
            Func<TModel, bool> selected,
            Func<TModel, bool>? available = null,
            Func<TModel, string>? subtitle = null)
            where TModel : AbstractModel
        {
            _drawerTitle.Text = title;
            ClearChildren(_drawerBody);
            var models = source.OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase).ToArray();
            var searchIndexes = models.ToDictionary(
                static model => model,
                model => new RitsuSearchPreparedText($"{SafeTitle(model)} {model.Id}"));
            var search = new LineEdit
            {
                PlaceholderText = L("ritsulib.debugTools.statePresets.search", "Search by name or ID"),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, 34f),
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(
                search,
                RitsuShellTheme.Current.Font.Body,
                RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            _drawerBody.AddChild(search);
            var result = SecondaryLabel(string.Empty);
            _drawerBody.AddChild(result);
            var flow = CollectionFlow();
            _drawerBody.AddChild(flow);
            search.TextChanged += Rebuild;
            Rebuild(string.Empty);
            OpenDrawer();
            search.GrabFocus();
            return;

            void Rebuild(string query)
            {
                ClearChildren(flow);
                var normalized = query.Trim();
                var allMatches = models.Where(model => (available == null || available(model)) &&
                                                       (normalized.Length == 0 ||
                                                        SafeTitle(model).Contains(normalized,
                                                            StringComparison.CurrentCultureIgnoreCase) ||
                                                        model.Id.ToString().Contains(normalized,
                                                            StringComparison.OrdinalIgnoreCase) ||
                                                        searchIndexes[model].ScoreExpansion(normalized) >= 0))
                    .ToArray();
                var matches = allMatches.Take(120).ToArray();
                result.Text = allMatches.Length == matches.Length
                    ? string.Format(
                        L("ritsulib.debugTools.statePresets.resultCount", "{0} results"),
                        matches.Length)
                    : string.Format(
                        L("ritsulib.debugTools.statePresets.resultCountLimited",
                            "Showing {0} of {1} results"),
                        matches.Length,
                        allMatches.Length);
                foreach (var model in matches)
                {
                    var captured = model;
                    flow.AddChild(CreateCollectionTile(
                        SafeTitle(captured),
                        subtitle?.Invoke(captured) ?? captured.Id.ToString(),
                        SafeTexture(() => icon(captured)),
                        () =>
                        {
                            if (!selected(captured))
                                return;
                            _setStatus(string.Format(
                                L("ritsulib.debugTools.statePresets.itemAdded", "Added {0}."),
                                SafeTitle(captured)), false);
                            if (available != null)
                                Rebuild(search.Text);
                        }));
                }
            }
        }

        private void ShowCardEditor(RitsuDebugStatePresetCardPile pile, int index, bool animate = true)
        {
            if (index < 0 || index >= pile.Cards.Count)
                return;
            var card = pile.Cards[index];
            _drawerTitle.Text = ModelLabel(card.CardId);
            ClearChildren(_drawerBody);

            var actions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            actions.AddThemeConstantOverride("separation", 6);
            actions.AddChild(OrderButton(
                RitsuDebugToolsGlyph.ChevronUp,
                L("ritsulib.debugTools.action.moveEarlier", "Move earlier"),
                () => MoveCard(-1),
                index == 0));
            actions.AddChild(OrderButton(
                RitsuDebugToolsGlyph.ChevronDown,
                L("ritsulib.debugTools.action.moveLater", "Move later"),
                () => MoveCard(1),
                index == pile.Cards.Count - 1));
            actions.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.duplicateCard", "Duplicate"),
                ModSettingsButtonTone.Normal,
                () =>
                {
                    if (pile.Cards.Sum(static saved => saved.Count) + card.Count >
                        RitsuDebugStatePresetStore.MaximumCardsPerPile)
                    {
                        _setStatus(string.Format(
                            L("ritsulib.debugTools.statePresets.cardLimit",
                                "A pile can contain at most {0} saved cards."),
                            RitsuDebugStatePresetStore.MaximumCardsPerPile), true);
                        return;
                    }

                    pile.Cards.Insert(index + 1, card.Clone());
                    MarkDirty();
                    CloseDrawer();
                },
                88f));
            actions.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.removeCard", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    pile.Cards.RemoveAt(index);
                    MarkDirty();
                    CloseDrawer();
                },
                82f));
            _drawerBody.AddChild(actions);
            _drawerBody.AddChild(Divider());

            _drawerBody.AddChild(IntegerField(
                L("ritsulib.debugTools.field.count", "Count"),
                card.Count,
                1,
                RitsuDebugCardActions.MaxCreateCount,
                value =>
                {
                    var otherCards = pile.Cards.Where((_, candidateIndex) => candidateIndex != index)
                        .Sum(static saved => saved.Count);
                    card.Count = Math.Min(value,
                        RitsuDebugStatePresetStore.MaximumCardsPerPile - otherCards);
                    CardChanged();
                }));
            var maxUpgrade = RitsuDebugCardActions.TryResolveCanonicalCard(card.CardId, out var canonical, out _)
                ? canonical.MaxUpgradeLevel
                : 0;
            _drawerBody.AddChild(IntegerField(
                L("ritsulib.debugTools.field.upgrades", "Upgrade levels"),
                card.UpgradeLevels,
                0,
                maxUpgrade,
                value =>
                {
                    card.UpgradeLevels = value;
                    InternalCardChanged();
                }));
            _drawerBody.AddChild(OptionalIntegerField(
                L("ritsulib.debugTools.field.baseCost", "Base cost"),
                card.BaseCost,
                0,
                RitsuDebugCardActions.MaxCardEditValue,
                value =>
                {
                    card.BaseCost = value;
                    InternalCardChanged();
                }));
            _drawerBody.AddChild(OptionalIntegerField(
                L("ritsulib.debugTools.field.replayCount", "Replay count"),
                card.ReplayCount,
                0,
                RitsuDebugCardActions.MaxReplayCount,
                value =>
                {
                    card.ReplayCount = value;
                    InternalCardChanged();
                }));

            _drawerBody.AddChild(SectionTitle(L("ritsulib.debugTools.action.cardFlags", "Card flags")));
            _drawerBody.AddChild(NullableBoolField(
                L("ritsulib.debugTools.field.exhaust", "Exhaust"),
                card.Exhaust,
                value =>
                {
                    card.Exhaust = value;
                    InternalCardChanged();
                }));
            _drawerBody.AddChild(NullableBoolField(
                L("ritsulib.debugTools.field.ethereal", "Ethereal"),
                card.Ethereal,
                value =>
                {
                    card.Ethereal = value;
                    InternalCardChanged();
                }));
            _drawerBody.AddChild(NullableBoolField(
                L("ritsulib.debugTools.field.unplayable", "Unplayable"),
                card.Unplayable,
                value =>
                {
                    card.Unplayable = value;
                    InternalCardChanged();
                }));

            if (canonical != null)
            {
                var keys = canonical.DynamicVars.Keys
                    .Concat(card.DynamicVars?.Keys ?? Enumerable.Empty<string>())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static key => key, StringComparer.Ordinal)
                    .ToArray();
                if (keys.Length > 0)
                {
                    _drawerBody.AddChild(SectionTitle(L(
                        "ritsulib.debugTools.action.dynamicValues",
                        "Dynamic values")));
                    foreach (var key in keys)
                    {
                        var capturedKey = key;
                        _drawerBody.AddChild(OptionalIntegerField(
                            key,
                            card.DynamicVars?.GetValueOrDefault(key),
                            0,
                            RitsuDebugCardActions.MaxCardEditValue,
                            value =>
                            {
                                if (value.HasValue)
                                {
                                    card.DynamicVars ??= new(StringComparer.Ordinal);
                                    card.DynamicVars[capturedKey] = value.Value;
                                }
                                else if (card.DynamicVars != null)
                                {
                                    card.DynamicVars.Remove(capturedKey);
                                    if (card.DynamicVars.Count == 0)
                                        card.DynamicVars = null;
                                }

                                InternalCardChanged();
                            }));
                    }
                }
            }

            var enchantments = ModelDb.DebugEnchantments
                .OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase)
                .Select(enchantment => new RitsuDebugEnchantmentOption(
                    enchantment.Id.ToString(),
                    SafeTitle(enchantment),
                    () => enchantment.Icon))
                .ToArray();
            if (enchantments.Length > 0)
            {
                _drawerBody.AddChild(SectionTitle(L(
                    "ritsulib.debugTools.field.enchantment",
                    "Enchantment")));
                var picker = new RitsuDebugEnchantmentPicker(
                    L("ritsulib.debugTools.field.enchantment", "Enchantment"),
                    enchantments,
                    card.EnchantmentId);
                picker.SelectionChanged += id =>
                {
                    card.EnchantmentId = id;
                    card.EnchantmentAmount = id == null ? null : card.EnchantmentAmount ?? 1;
                    InternalCardChanged();
                };
                picker.AddExpandedControl(OptionalIntegerField(
                    L("ritsulib.debugTools.field.enchantmentAmount", "Enchantment amount"),
                    card.EnchantmentAmount,
                    1,
                    RitsuDebugCardActions.MaxCardEditValue,
                    value =>
                    {
                        card.EnchantmentAmount = card.EnchantmentId == null ? null : value ?? 1;
                        InternalCardChanged();
                    }));
                _drawerBody.AddChild(picker);
            }

            _cardGrid?.SetSelectedIndex(index);
            if (animate)
                OpenDrawer();
            return;

            void CardChanged()
            {
                MarkDirty();
                _cardGrid?.RefreshCard(index);
            }

            void InternalCardChanged()
            {
                MarkInternalValuesDirty();
                _cardGrid?.RefreshCard(index);
            }

            void MoveCard(int offset)
            {
                var destination = index + offset;
                if (!MovePresetCard(pile, index, destination))
                    return;
                ShowCardEditor(pile, destination, false);
            }

            static RitsuDebugToolsIconButton OrderButton(
                RitsuDebugToolsGlyph glyph,
                string tooltip,
                Action action,
                bool disabled)
            {
                var button = new RitsuDebugToolsIconButton(38f, 38f);
                button.Configure(
                    RitsuDebugToolsIcons.Get(
                        glyph,
                        18,
                        RitsuShellTheme.Current.Text.LabelPrimary),
                    tooltip,
                    ModSettingsButtonTone.Normal);
                button.Disabled = disabled;
                button.Pressed += action;
                return button;
            }
        }

        private void ShowPotionEditor(
            RitsuDebugStatePresetPotions potions,
            RitsuDebugStatePresetPotion potion)
        {
            _drawerTitle.Text = ModelLabel(potion.PotionId);
            ClearChildren(_drawerBody);
            if (potions.ApplyMode == RitsuDebugStatePresetApplyMode.Replace)
                _drawerBody.AddChild(IntegerField(
                    L("ritsulib.debugTools.statePresets.slot", "Slot"),
                    potion.SlotIndex.GetValueOrDefault(),
                    0,
                    RitsuDebugPlayerActions.MaxPotionSlots - 1,
                    value =>
                    {
                        if (potions.Items.Any(item => !ReferenceEquals(item, potion) && item.SlotIndex == value))
                            return;
                        potion.SlotIndex = value;
                        MarkDirty();
                    }));
            var canonical = ModelDb.AllPotions.FirstOrDefault(model =>
                model.Id.ToString().Equals(potion.PotionId, StringComparison.Ordinal));
            if (canonical != null)
                AddInternalValueEditors(
                    canonical.DynamicVars,
                    () => potion.DynamicVars,
                    values =>
                    {
                        potion.DynamicVars = values;
                        MarkInternalValuesDirty();
                    });
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    potions.Items.Remove(potion);
                    MarkDirty();
                    CloseDrawer();
                },
                88f));
            OpenDrawer();
        }

        private void ShowPowerEditor(
            RitsuDebugStatePresetPowers powers,
            RitsuDebugStatePresetPower power)
        {
            _drawerTitle.Text = ModelLabel(power.PowerId);
            ClearChildren(_drawerBody);
            _drawerBody.AddChild(IntegerField(
                L("ritsulib.debugTools.field.amount", "Amount"),
                power.Amount,
                1,
                RitsuDebugCombatActions.MaxAmount,
                value =>
                {
                    power.Amount = value;
                    MarkInternalValuesDirty();
                }));
            var canonical = ModelDb.AllPowers.FirstOrDefault(model =>
                model.Id.ToString().Equals(power.PowerId, StringComparison.Ordinal));
            if (canonical != null)
                AddInternalValueEditors(
                    canonical.DynamicVars,
                    () => power.DynamicVars,
                    values =>
                    {
                        power.DynamicVars = values;
                        MarkInternalValuesDirty();
                    });
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    powers.Items.Remove(power);
                    MarkDirty();
                    CloseDrawer();
                },
                88f));
            OpenDrawer();
        }

        private void ShowRelicEditor(RitsuDebugStatePresetInventory relics, string relicId)
        {
            _drawerTitle.Text = ModelLabel(relicId);
            ClearChildren(_drawerBody);
            var state = relics.InternalValues?.GetValueOrDefault(relicId) ?? new();
            _drawerBody.AddChild(IntegerField(
                L("ritsulib.debugTools.field.stackCount", "Stack count"),
                state.StackCount,
                1,
                RitsuDebugInventoryActions.MaxRelicStackCount,
                value =>
                {
                    state.StackCount = value;
                    StoreValues();
                }));
            var canonical = ModelDb.AllRelics.FirstOrDefault(model =>
                model.Id.ToString().Equals(relicId, StringComparison.Ordinal));
            if (canonical != null)
                AddInternalValueEditors(
                    canonical.DynamicVars,
                    () => state.DynamicVars,
                    values =>
                    {
                        state.DynamicVars = values;
                        StoreValues();
                    });
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    relics.ModelIds.Remove(relicId);
                    relics.InternalValues?.Remove(relicId);
                    MarkDirty();
                    CloseDrawer();
                },
                88f));
            OpenDrawer();
            return;

            void StoreValues()
            {
                relics.InternalValues ??= new(StringComparer.Ordinal);
                relics.InternalValues[relicId] = state;
                MarkInternalValuesDirty();
            }
        }

        private void AddInternalValueEditors(
            DynamicVarSet dynamicVars,
            Func<Dictionary<string, int>?> getValues,
            Action<Dictionary<string, int>?> setValues)
        {
            var keys = dynamicVars
                .Where(static pair => RitsuDebugModelValueOverrides.IsEditable(pair.Value))
                .Select(static pair => pair.Key)
                .OrderBy(static key => key, StringComparer.Ordinal)
                .ToArray();
            if (keys.Length == 0)
                return;
            _drawerBody.AddChild(SectionTitle(L(
                "ritsulib.debugTools.action.dynamicValues",
                "Dynamic values")));
            foreach (var key in keys)
            {
                var capturedKey = key;
                _drawerBody.AddChild(OptionalIntegerField(
                    key,
                    getValues()?.GetValueOrDefault(key),
                    RitsuDebugModelValueOverrides.MinimumValue,
                    RitsuDebugModelValueOverrides.MaximumValue,
                    value =>
                    {
                        var values = getValues();
                        if (value.HasValue)
                        {
                            values ??= new(StringComparer.Ordinal);
                            values[capturedKey] = value.Value;
                        }
                        else if (values != null)
                        {
                            values.Remove(capturedKey);
                            if (values.Count == 0)
                                values = null;
                        }

                        setValues(values);
                    }));
            }
        }

        private void ShowRemoveItemDrawer(string title, Action remove)
        {
            _drawerTitle.Text = title;
            ClearChildren(_drawerBody);
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    remove();
                    CloseDrawer();
                },
                88f));
            OpenDrawer();
        }

        private void ShowCaptureDrawer()
        {
            var target = _getTarget();
            if (target == null)
            {
                _setStatus(L("ritsulib.debugTools.statePresets.noTarget",
                    "No target player is available."), true);
                return;
            }

            var combat = RitsuDebugStatePresetCapture.HasActiveCombat(target);
            var customPiles = ModCardPileRegistry.GetDefinitionsSnapshot();
            var canCaptureCustomPiles = customPiles.Length > 0 &&
                                        (combat || customPiles.All(static definition =>
                                            definition.Scope == ModCardPileScope.RunPersistent));
            var scope = RitsuDebugStatePresetCaptureScope.Deck |
                        RitsuDebugStatePresetCaptureScope.Relics |
                        RitsuDebugStatePresetCaptureScope.Potions |
                        RitsuDebugStatePresetCaptureScope.Player;
            _drawerTitle.Text = L("ritsulib.debugTools.statePresets.fill", "Fill from current state");
            ClearChildren(_drawerBody);
            _drawerBody.AddChild(Hint(L(
                "ritsulib.debugTools.statePresets.fillHint",
                "Choose the pages to replace with the selected player's current state.")));
            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 8);
            AddScope(RitsuDebugStatePresetCaptureScope.Deck,
                L("ritsulib.debugTools.statePresets.deck", "Deck"), true, true);
            AddScope(RitsuDebugStatePresetCaptureScope.CombatPiles,
                L("ritsulib.debugTools.statePresets.combatPiles", "Combat piles"), false, combat);
            AddScope(RitsuDebugStatePresetCaptureScope.CustomPiles,
                L("ritsulib.debugTools.statePresets.customPiles", "Custom piles"), false,
                canCaptureCustomPiles);
            AddScope(RitsuDebugStatePresetCaptureScope.Relics,
                L("ritsulib.debugTools.category.relics", "Relics"), true, true);
            AddScope(RitsuDebugStatePresetCaptureScope.Potions,
                L("ritsulib.debugTools.category.potions", "Potions"), true, true);
            AddScope(RitsuDebugStatePresetCaptureScope.Powers,
                L("ritsulib.debugTools.category.powers", "Powers"), false, combat);
            AddScope(RitsuDebugStatePresetCaptureScope.Player,
                L("ritsulib.debugTools.statePresets.playerValues", "Player values"), true, true);
            AddScope(RitsuDebugStatePresetCaptureScope.CombatValues,
                L("ritsulib.debugTools.statePresets.combatValues", "Combat values"), false, combat);
            AddScope(RitsuDebugStatePresetCaptureScope.SecondaryResources,
                L("ritsulib.debugTools.category.secondaryResources", "Secondary resources"), false, combat);
            AddScope(RitsuDebugStatePresetCaptureScope.Capabilities,
                L("ritsulib.debugTools.category.capabilities", "Capabilities"), false, true);
            _drawerBody.AddChild(grid);
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillSelected", "Fill selected pages"),
                ModSettingsButtonTone.Accent,
                () => CaptureScopes(scope),
                170f));
            OpenDrawer();
            return;

            void AddScope(
                RitsuDebugStatePresetCaptureScope value,
                string text,
                bool selected,
                bool enabled)
            {
                var button = ModSettingsUiControlTheming.CreateCompactSettingsToggleButton(text, selected);
                button.Disabled = !enabled;
                button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                button.Toggled += on =>
                {
                    if (on)
                        scope |= value;
                    else
                        scope &= ~value;
                };
                grid.AddChild(button);
            }
        }

        private void CaptureScopes(RitsuDebugStatePresetCaptureScope scope)
        {
            var target = _getTarget();
            if (_draft == null || target == null)
            {
                _setStatus(L("ritsulib.debugTools.statePresets.noTarget",
                    "No target player is available."), true);
                return;
            }

            if (!RitsuDebugStatePresetCapture.TryCapture(
                    target,
                    _draft,
                    scope,
                    out var result,
                    out var feedback))
            {
                _setStatus(feedback.GetLocalizedText(), true);
                return;
            }

            _draft = result.Preset;
            _dirty = true;
            _setStatus(result.SkippedValueCount == 0
                ? L("ritsulib.debugTools.statePresets.filled", "Preset pages filled from the current state.")
                : string.Format(
                    L("ritsulib.debugTools.statePresets.filledSkipped",
                        "Preset pages filled; {0} unsupported values were omitted."),
                    result.SkippedValueCount), false);
            CloseDrawer();
            if (!_drawerLayer.Visible)
                RebuildMain();
        }

        private void CapturePile(PileType pileType)
        {
            var target = _getTarget();
            if (_draft == null || target == null)
            {
                _setStatus(L("ritsulib.debugTools.statePresets.noTarget",
                    "No target player is available."), true);
                return;
            }

            if (!RitsuDebugStatePresetCapture.TryCapturePileOnly(
                    target,
                    _draft,
                    pileType,
                    out var result,
                    out var feedback))
            {
                _setStatus(feedback.GetLocalizedText(), true);
                return;
            }

            _draft = result.Preset;
            _dirty = true;
            _setStatus(result.SkippedValueCount == 0
                ? L("ritsulib.debugTools.statePresets.filled", "Preset pages filled from the current state.")
                : string.Format(
                    L("ritsulib.debugTools.statePresets.filledSkipped",
                        "Preset pages filled; {0} unsupported values were omitted."),
                    result.SkippedValueCount), false);
            CloseDrawer();
            RebuildMain();
        }

        private void ShowManagementDrawer()
        {
            _drawerTitle.Text = L("ritsulib.debugTools.statePresets.manage", "Preset actions");
            ClearChildren(_drawerBody);
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.duplicate", "Duplicate preset"),
                ModSettingsButtonTone.Normal,
                DuplicateDraft));
            _drawerBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.delete", "Delete preset"),
                ModSettingsButtonTone.Danger,
                DeleteDraft));
            OpenDrawer();
        }

        private void SaveDraft()
        {
            if (_draft == null)
                return;
            if (!RitsuDebugStatePresetStore.TrySave(_draft, out var feedback))
            {
                _setStatus(feedback.GetLocalizedText(), true);
                return;
            }

            _dirty = false;
            _setStatus(L("ritsulib.debugTools.statePresets.saved", "Preset saved."), false);
            RebuildPresetList();
        }

        private void ApplyDraft()
        {
            if (_draft == null)
                return;
            var check = RitsuDebugStatePresetActions.ValidateStoredPreset(_draft);
            if (!check.Success)
            {
                _setStatus(check.Feedback.GetLocalizedText(), true);
                return;
            }

            _apply(_draft.Clone());
        }

        private void ExportToClipboard()
        {
            if (_draft == null)
                return;
            var check = RitsuDebugStatePresetActions.ValidateStoredPreset(_draft);
            if (!check.Success)
            {
                _setStatus(check.Feedback.GetLocalizedText(), true);
                return;
            }

            DisplayServer.ClipboardSet(RitsuDebugStatePresetStore.Export(_draft));
            _setStatus(L("ritsulib.debugTools.statePresets.exported",
                "Preset copied to the clipboard."), false);
        }

        private void ImportFromClipboard()
        {
            if (!RitsuDebugStatePresetStore.TryImport(
                    DisplayServer.ClipboardGet(),
                    out var preset,
                    out var feedback))
            {
                _setStatus(feedback.GetLocalizedText(), true);
                return;
            }

            preset.Name = CreateUniqueName(preset.Name);
            RunAfterDiscardConfirmation(() =>
            {
                _draft = preset;
                _dirty = true;
                CloseDrawer(false);
                RebuildAll();
            });
        }

        private void DuplicateDraft()
        {
            if (_draft == null)
                return;
            var copy = _draft.Clone(true);
            copy.Name = CreateUniqueName(string.Format(
                L("ritsulib.debugTools.statePresets.copyName", "{0} copy"),
                _draft.Name));
            _draft = copy;
            _dirty = true;
            CloseDrawer(false);
            RebuildAll();
        }

        private void DeleteDraft()
        {
            if (_draft == null)
                return;
            var preset = _draft;
            var stored = RitsuDebugStatePresetStore.GetSnapshot().Any(candidate => candidate.Id == preset.Id);
            if (!stored)
            {
                CreateNewPreset();
                return;
            }

            ModSettingsUiFactory.ShowStyledConfirm(
                this,
                L("ritsulib.debugTools.statePresets.deleteTitle", "Delete preset?"),
                string.Format(
                    L("ritsulib.debugTools.statePresets.deleteBody", "Delete '{0}' permanently?"),
                    preset.Name),
                L("ritsulib.debugTools.statePresets.cancel", "Cancel"),
                L("ritsulib.debugTools.statePresets.delete", "Delete"),
                true,
                () =>
                {
                    RitsuDebugStatePresetStore.TryDelete(preset.Id);
                    CloseDrawer(false);
                    SelectInitialPreset();
                });
        }

        private static string CreateUniqueName(string requested)
        {
            var names = RitsuDebugStatePresetStore.GetSnapshot()
                .Select(static preset => preset.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var baseName = requested.Trim();
            if (baseName.Length == 0)
                baseName = L("ritsulib.debugTools.statePresets.fallbackName", "Preset");
            baseName = baseName[..Math.Min(baseName.Length, RitsuDebugStatePresetStore.MaximumNameLength)];
            var name = baseName;
            for (var suffix = 2; names.Contains(name); suffix++)
            {
                var suffixText = $" {suffix}";
                name = $"{baseName[..Math.Min(
                    baseName.Length,
                    RitsuDebugStatePresetStore.MaximumNameLength - suffixText.Length)]}{suffixText}";
            }

            return name;
        }

        private void ShowCollectionLimit(int maximum)
        {
            _setStatus(string.Format(
                L("ritsulib.debugTools.statePresets.itemLimit",
                    "This page can contain at most {0} saved items."),
                maximum), true);
        }
    }
}
