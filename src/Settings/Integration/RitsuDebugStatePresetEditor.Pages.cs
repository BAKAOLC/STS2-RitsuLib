using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugStatePresetEditor
    {
        private void BuildCardsPage()
        {
            var editablePiles = RitsuDebugCardActions.GetMutablePileTypes();
            if (!editablePiles.Contains(_selectedPile))
                _selectedPile = PileType.Deck;
            var pileTabs = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            pileTabs.AddThemeConstantOverride("h_separation", 6);
            pileTabs.AddThemeConstantOverride("v_separation", 6);
            foreach (var pileType in editablePiles)
            {
                var capturedPile = pileType;
                var count = FindPile(pileType)?.Cards.Sum(static card => card.Count) ?? 0;
                var text = $"{PileLabel(pileType)} · {count}";
                var button = new ModSettingsMiniButton(text, () =>
                {
                    _selectedPile = capturedPile;
                    CloseDrawer(false);
                    RebuildMain();
                })
                {
                    CustomMinimumSize = new(118f, 36f),
                    Icon = RitsuDebugToolsIcons.Get(
                        RitsuDebugToolsGlyph.Cards,
                        17,
                        RitsuShellTheme.Current.Text.LabelPrimary),
                };
                ApplySelectionStyle(button, _selectedPile == pileType);
                pileTabs.AddChild(button);
            }

            _contentBody.AddChild(pileTabs);
            var pile = FindPile(_selectedPile);
            if (pile == null)
            {
                _contentBody.AddChild(Hint(string.Format(
                    L("ritsulib.debugTools.statePresets.pileDisabled",
                        "{0} is not changed by this preset."),
                    PileLabel(_selectedPile))));
                _contentBody.AddChild(CompactButton(
                    L("ritsulib.debugTools.statePresets.enablePile", "Configure this pile"),
                    ModSettingsButtonTone.Accent,
                    () =>
                    {
                        _draft!.CardPiles.Add(new()
                        {
                            Pile = RitsuDebugCardActions.GetPileToken(_selectedPile),
                            ApplyMode = RitsuDebugStatePresetApplyMode.Add,
                        });
                        MarkDirty(true);
                    },
                    168f));
                return;
            }

            var header = BuildGroupToolbar(
                PileLabel(_selectedPile),
                pile.ApplyMode,
                mode =>
                {
                    pile.ApplyMode = mode;
                    MarkDirty();
                },
                () =>
                {
                    _draft!.CardPiles.Remove(pile);
                    MarkDirty(true);
                });
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.addCard", "Add card"),
                ModSettingsButtonTone.Accent,
                () => ShowCardPicker(pile),
                92f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CapturePile(_selectedPile),
                86f));
            _contentBody.AddChild(header);
            if (pile.Cards.Count == 0)
            {
                _contentBody.AddChild(Hint(pile.ApplyMode == RitsuDebugStatePresetApplyMode.Replace
                    ? L("ritsulib.debugTools.statePresets.emptyReplacePile",
                        "Applying this preset clears the pile and leaves it empty.")
                    : L("ritsulib.debugTools.statePresets.emptyAddPile", "Add cards to this pile.")));
                return;
            }

            _contentBody.AddChild(Hint(L(
                "ritsulib.debugTools.statePresets.dragCardHint",
                "Drag cards to reorder.")));
            _cardGrid = new(
                _dragLayer,
                index => ShowCardEditor(pile, index),
                (sourceIndex, destinationIndex) => MovePresetCard(pile, sourceIndex, destinationIndex));
            _cardGrid.SetCards(pile.Cards, -1);
            _contentBody.AddChild(_cardGrid);
        }

        private bool MovePresetCard(
            RitsuDebugStatePresetCardPile pile,
            int sourceIndex,
            int destinationIndex)
        {
            if (sourceIndex < 0 ||
                sourceIndex >= pile.Cards.Count ||
                destinationIndex < 0 ||
                destinationIndex >= pile.Cards.Count ||
                sourceIndex == destinationIndex)
                return false;
            var card = pile.Cards[sourceIndex];
            pile.Cards.RemoveAt(sourceIndex);
            pile.Cards.Insert(destinationIndex, card);
            MarkDirty();
            _cardGrid?.SetCards(pile.Cards, destinationIndex);
            return true;
        }

        private void BuildRelicsPage()
        {
            var relics = _draft!.Relics;
            if (relics == null)
            {
                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.relics", "Relics"),
                    () =>
                    {
                        _draft.Relics = new() { ApplyMode = RitsuDebugStatePresetApplyMode.Add };
                        MarkDirty(true);
                    });
                return;
            }

            var header = BuildGroupToolbar(
                L("ritsulib.debugTools.category.relics", "Relics"),
                relics.ApplyMode,
                mode =>
                {
                    relics.ApplyMode = mode;
                    MarkDirty();
                },
                () =>
                {
                    _draft.Relics = null;
                    MarkDirty(true);
                });
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.addRelic", "Add relic"),
                ModSettingsButtonTone.Accent,
                () => ShowRelicPicker(relics),
                92f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(RitsuDebugStatePresetCaptureScope.Relics),
                86f));
            _contentBody.AddChild(header);
            BuildModelCollection(
                relics.ModelIds,
                id => ModelDb.AllRelics.FirstOrDefault(model => model.Id.ToString() == id),
                static model => model.Icon,
                relics.ApplyMode,
                id => ShowRelicEditor(relics, id));
        }

        private void BuildPotionsPage()
        {
            var potions = _draft!.Potions;
            if (potions == null)
            {
                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.potions", "Potions"),
                    () =>
                    {
                        _draft.Potions = new() { ApplyMode = RitsuDebugStatePresetApplyMode.Add };
                        MarkDirty(true);
                    });
                return;
            }

            var header = BuildGroupToolbar(
                L("ritsulib.debugTools.category.potions", "Potions"),
                potions.ApplyMode,
                mode =>
                {
                    potions.ApplyMode = mode;
                    if (mode == RitsuDebugStatePresetApplyMode.Add)
                        foreach (var potion in potions.Items)
                            potion.SlotIndex = null;
                    else
                        AssignMissingPotionSlots(potions);
                    MarkDirty(true);
                },
                () =>
                {
                    _draft.Potions = null;
                    MarkDirty(true);
                });
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.addPotion", "Add potion"),
                ModSettingsButtonTone.Accent,
                () => ShowPotionPicker(potions),
                98f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(RitsuDebugStatePresetCaptureScope.Potions),
                86f));
            _contentBody.AddChild(header);

            var flow = CollectionFlow();
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var potion in potions.Items)
            {
                var saved = potion;
                var model = ModelDb.AllPotions.FirstOrDefault(candidate =>
                    candidate.Id.ToString() == potion.PotionId);
                if (model == null)
                    continue;
                flow.AddChild(CreateCollectionTile(
                    SafeTitle(model),
                    potions.ApplyMode == RitsuDebugStatePresetApplyMode.Replace
                        ? string.Format(
                            L("ritsulib.debugTools.statePresets.slot", "Slot {0}"),
                            potion.SlotIndex.GetValueOrDefault() + 1)
                        : model.Id.ToString(),
                    SafeTexture(() => model.Image),
                    () => ShowPotionEditor(potions, saved)));
            }

            AddCollectionOrEmpty(flow, potions.Items.Count, potions.ApplyMode);
        }

        private void BuildPowersPage()
        {
            var powers = _draft!.Powers;
            if (powers == null)
            {
                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.powers", "Powers"),
                    () =>
                    {
                        _draft.Powers = new() { ApplyMode = RitsuDebugStatePresetApplyMode.Add };
                        MarkDirty(true);
                    });
                return;
            }

            var header = BuildGroupToolbar(
                L("ritsulib.debugTools.category.powers", "Powers"),
                powers.ApplyMode,
                mode =>
                {
                    powers.ApplyMode = mode;
                    MarkDirty();
                },
                () =>
                {
                    _draft.Powers = null;
                    MarkDirty(true);
                });
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.addPower", "Add power"),
                ModSettingsButtonTone.Accent,
                () => ShowPowerPicker(powers),
                96f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(RitsuDebugStatePresetCaptureScope.Powers),
                86f));
            _contentBody.AddChild(header);

            var flow = CollectionFlow();
            foreach (var power in powers.Items)
            {
                var saved = power;
                var model = ModelDb.AllPowers.FirstOrDefault(candidate => candidate.Id.ToString() == power.PowerId);
                if (model == null)
                    continue;
                flow.AddChild(CreateCollectionTile(
                    SafeTitle(model),
                    $"×{power.Amount} · {model.Id}",
                    SafeTexture(() => model.Icon),
                    () => ShowPowerEditor(powers, saved)));
            }

            AddCollectionOrEmpty(flow, powers.Items.Count, powers.ApplyMode);
        }

        private void BuildPlayerPage()
        {
            var player = _draft!.Player;
            if (player == null)
            {
                BuildDisabledGroup(
                    L("ritsulib.debugTools.category.players", "Player values"),
                    () =>
                    {
                        _draft.Player = new();
                        MarkDirty(true);
                    });
                return;
            }

            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 6);
            var title = SectionTitle(L("ritsulib.debugTools.category.players", "Player values"));
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(title);
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.fillPage", "Fill page"),
                ModSettingsButtonTone.Normal,
                () => CaptureScopes(
                    RitsuDebugStatePresetCaptureScope.Player |
                    (_getTarget() is { } target && RitsuDebugStatePresetCapture.HasActiveCombat(target)
                        ? RitsuDebugStatePresetCaptureScope.CombatValues
                        : RitsuDebugStatePresetCaptureScope.None)),
                86f));
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.disablePage", "Remove page"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    _draft.Player = null;
                    MarkDirty(true);
                },
                108f));
            _contentBody.AddChild(header);

            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            grid.AddThemeConstantOverride("h_separation", 10);
            grid.AddThemeConstantOverride("v_separation", 8);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetGold", "Gold",
                player.Gold, 0, RitsuDebugPlayerActions.MaxGold, value => player.Gold = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetCurrentHp", "Current HP",
                player.CurrentHp, 1, RitsuDebugPlayerActions.MaxHitPoints, value => player.CurrentHp = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetMaxHp", "Maximum HP",
                player.MaxHp, 1, RitsuDebugPlayerActions.MaxHitPoints, value => player.MaxHp = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetMaxEnergy", "Maximum energy",
                player.MaxEnergy, 1, RitsuDebugPlayerActions.MaxCombatResource, value => player.MaxEnergy = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetPotionSlots", "Potion slots",
                player.PotionSlots, 0, RitsuDebugPlayerActions.MaxPotionSlots, value => player.PotionSlots = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetEnergy", "Energy",
                player.Energy, 0, RitsuDebugPlayerActions.MaxCombatResource, value => player.Energy = value);
            AddPlayerValue("ritsulib.debugTools.operation.RitsuDebugPlayerOperation.SetStars", "Stars",
                player.Stars, 0, RitsuDebugPlayerActions.MaxCombatResource, value => player.Stars = value);
            AddPlayerValue("ritsulib.debugTools.field.block", "Block",
                player.Block, 0, RitsuDebugPlayerActions.MaxHitPoints, value => player.Block = value);
            _contentBody.AddChild(grid);
            return;

            void AddPlayerValue(
                string key,
                string fallback,
                int? value,
                int minimum,
                int maximum,
                Action<int?> changed)
            {
                grid.AddChild(OptionalIntegerField(L(key, fallback), value, minimum, maximum, changed));
            }
        }

        private HBoxContainer BuildGroupToolbar(
            string titleText,
            RitsuDebugStatePresetApplyMode mode,
            Action<RitsuDebugStatePresetApplyMode> modeChanged,
            Action remove)
        {
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 6);
            var title = SectionTitle(titleText);
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(title);
            var modeControl = new ModSettingsDropdownChoiceControl<RitsuDebugStatePresetApplyMode>(
                [
                    (RitsuDebugStatePresetApplyMode.Add,
                        L("ritsulib.debugTools.statePresets.modeAdd", "Add")),
                    (RitsuDebugStatePresetApplyMode.Replace,
                        L("ritsulib.debugTools.statePresets.modeReplace", "Replace")),
                ],
                mode,
                modeChanged)
            {
                CustomMinimumSize = new(140f, 34f),
            };
            header.AddChild(modeControl);
            header.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.disablePage", "Remove page"),
                ModSettingsButtonTone.Danger,
                remove,
                108f));
            return header;
        }

        private void BuildDisabledGroup(string title, Action enable)
        {
            _contentBody.AddChild(SectionTitle(title));
            _contentBody.AddChild(Hint(L(
                "ritsulib.debugTools.statePresets.pageDisabled",
                "This page is not changed by the preset.")));
            _contentBody.AddChild(CompactButton(
                L("ritsulib.debugTools.statePresets.enablePage", "Configure this page"),
                ModSettingsButtonTone.Accent,
                enable,
                176f));
        }

        private void BuildModelCollection<TModel>(
            IReadOnlyList<string> ids,
            Func<string, TModel?> resolve,
            Func<TModel, Texture2D?> icon,
            RitsuDebugStatePresetApplyMode mode,
            Action<string> selected)
            where TModel : AbstractModel
        {
            var flow = CollectionFlow();
            foreach (var id in ids)
            {
                var model = resolve(id);
                if (model == null)
                    continue;
                flow.AddChild(CreateCollectionTile(
                    SafeTitle(model),
                    model.Id.ToString(),
                    SafeTexture(() => icon(model)),
                    () => selected(id)));
            }

            AddCollectionOrEmpty(flow, ids.Count, mode);
        }

        private void AddCollectionOrEmpty(
            HFlowContainer flow,
            int count,
            RitsuDebugStatePresetApplyMode mode)
        {
            if (count == 0)
            {
                _contentBody.AddChild(Hint(mode == RitsuDebugStatePresetApplyMode.Replace
                    ? L("ritsulib.debugTools.statePresets.emptyReplaceGroup",
                        "Applying this preset clears the current contents and leaves this page empty.")
                    : L("ritsulib.debugTools.statePresets.emptyAddGroup", "Add items to this page.")));
                return;
            }

            var scroll = CreateScroll();
            scroll.AddChild(flow);
            _contentBody.AddChild(scroll);
        }

        private static HFlowContainer CollectionFlow()
        {
            var flow = new HFlowContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            flow.AddThemeConstantOverride("h_separation", 8);
            flow.AddThemeConstantOverride("v_separation", 8);
            return flow;
        }

        private Control CreateCollectionTile(
            string title,
            string subtitle,
            Texture2D? icon,
            Action selected)
        {
            var button = new ModSettingsMiniButton(string.Empty, selected)
            {
                CustomMinimumSize = new(248f, 76f),
                TooltipText = $"{title}\n{subtitle}",
            };
            button.Pressed += () => ApplySelectionStyle(button, true);
            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            row.AddThemeConstantOverride("separation", 8);
            if (icon != null)
                row.AddChild(new TextureRect
                {
                    Texture = icon,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new(52f, 52f),
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            var labels = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            labels.AddThemeConstantOverride("separation", 2);
            var titleLabel = new Label
            {
                Text = title,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            titleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            titleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            labels.AddChild(titleLabel);
            var subtitleLabel = new Label
            {
                Text = subtitle,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            subtitleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            subtitleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HintSmall);
            subtitleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            labels.AddChild(subtitleLabel);
            row.AddChild(labels);
            var padding = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            padding.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            padding.AddThemeConstantOverride("margin_left", 10);
            padding.AddThemeConstantOverride("margin_right", 10);
            padding.AddThemeConstantOverride("margin_top", 8);
            padding.AddThemeConstantOverride("margin_bottom", 8);
            padding.AddChild(row);
            button.AddChild(padding);
            return button;
        }

        private RitsuDebugStatePresetCardPile? FindPile(PileType pileType)
        {
            return _draft!.CardPiles.FirstOrDefault(candidate =>
                RitsuDebugCardActions.TryParseMutablePileType(candidate.Pile, out var candidatePileType) &&
                candidatePileType == pileType);
        }

        private static void AssignMissingPotionSlots(RitsuDebugStatePresetPotions potions)
        {
            var used = potions.Items.Where(static item => item.SlotIndex.HasValue)
                .Select(static item => item.SlotIndex!.Value)
                .ToHashSet();
            var next = 0;
            foreach (var potion in potions.Items.Where(static item => !item.SlotIndex.HasValue))
            {
                while (used.Contains(next))
                    next++;
                potion.SlotIndex = next;
                used.Add(next++);
            }
        }
    }
}
