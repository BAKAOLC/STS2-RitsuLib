using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class RitsuDebugLiveDetailContainer : VBoxContainer
    {
        private readonly List<Action> _refreshCallbacks = [];

        internal void RegisterRefresh(Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _refreshCallbacks.Add(callback);
        }

        internal void RefreshState()
        {
            foreach (var callback in _refreshCallbacks)
                callback();
        }
    }

    internal sealed partial class RitsuDebugToolsPanel
    {
        private Control CreateCardDetail(CardModel card)
        {
            var root = DetailShell(
                card.Id.ToString(),
                () => card.Portrait,
                $"{EnumLabel(card.Type)} · {EnumLabel(card.Rarity)} · {L("ritsulib.debugTools.cost", "Cost")} {CardCost(card)}",
                SafeCardDescription(card));
            AddSectionTitle(root, L("ritsulib.debugTools.action.createCard", "Create card"));
            PileType[] destinationPiles = TryGetTargetPlayer(out var target) && HasActiveCombatState(target)
                ? [.. RitsuDebugCardActions.GetMutablePileNames().Select(static name => Enum.Parse<PileType>(name))]
                : [PileType.Deck];
            var selectedPile = destinationPiles.Contains(PileType.Hand)
                ? PileType.Hand
                : destinationPiles[0];
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.field.pile", "Destination pile"),
                [.. destinationPiles.Select(pile => (pile, EnumLabel(pile)))],
                selectedPile,
                value => selectedPile = value));
            AddHint(root, L("ritsulib.debugTools.fullHandPlacement",
                "Cards sent to a full hand follow the game's rules and enter the discard pile."));
            var count = IntField(root, L("ritsulib.debugTools.field.cardCount", "Number of cards"), "1");
            var upgrades = IntField(
                root,
                L("ritsulib.debugTools.field.upgrades", "Upgrade levels"),
                "0");

            AddSectionTitle(root, L("ritsulib.debugTools.action.initialCardState", "Initial card state"));
            AddHint(root, L(
                "ritsulib.debugTools.initialCardStateHint",
                "Displayed values are the card's defaults. Only values you change override the created card."));

            var baseCostChanged = false;
            LineEdit? baseCost = null;
            if (!card.EnergyCost.CostsX)
            {
                baseCost = CreateIntegerEdit(
                    card.EnergyCost.GetWithModifiers(CostModifiers.None).ToString());
                baseCost.TextChanged += _ => baseCostChanged = true;
                root.AddChild(Field(OperationLabel(RitsuDebugCardEditField.Cost), baseCost));
            }

            var replayChanged = false;
            var replay = CreateIntegerEdit(card.BaseReplayCount.ToString());
            replay.TextChanged += _ => replayChanged = true;
            root.AddChild(Field(L("ritsulib.debugTools.field.replay", "Replay count"), replay));

            var changedDynamicVariables = new HashSet<string>(StringComparer.Ordinal);
            var dynamicVariableEditors = new Dictionary<string, LineEdit>(StringComparer.Ordinal);
            foreach (var (key, dynamicVar) in card.DynamicVars.OrderBy(
                         static pair => pair.Key,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (dynamicVar.BaseValue is < 0 or > RitsuDebugCardActions.MaxCardEditValue ||
                    dynamicVar.BaseValue != decimal.Truncate(dynamicVar.BaseValue))
                    continue;

                var dynamicVariableKey = key;
                var editor = CreateIntegerEdit(decimal.ToInt32(dynamicVar.BaseValue).ToString());
                editor.TextChanged += _ => changedDynamicVariables.Add(dynamicVariableKey);
                dynamicVariableEditors.Add(dynamicVariableKey, editor);
                root.AddChild(Field(dynamicVariableKey, editor));
            }

            bool? exhaust = null;
            bool? ethereal = null;
            bool? unplayable = null;
            var localKeywords = card.GetKeywordsWithSources(KeywordSources.Local);
            root.AddChild(ModSettingsUiControlTheming.CreateCompactEditorRow(
                2,
                InitialFlagField(
                    RitsuDebugCardEditField.Exhaust,
                    localKeywords.Contains(CardKeyword.Exhaust),
                    value => exhaust = value),
                InitialFlagField(
                    RitsuDebugCardEditField.Ethereal,
                    localKeywords.Contains(CardKeyword.Ethereal),
                    value => ethereal = value),
                InitialFlagField(
                    RitsuDebugCardEditField.Unplayable,
                    localKeywords.Contains(CardKeyword.Unplayable),
                    value => unplayable = value)));

            var enchantmentOptions = ModelDb.DebugEnchantments
                .OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase)
                .Select(enchantment => new RitsuDebugEnchantmentOption(
                    enchantment.Id.ToString(),
                    SafeTitle(enchantment),
                    () => enchantment.Icon))
                .ToArray();
            var specifyEnchantment = false;
            RitsuDebugEnchantmentPicker? enchantmentPicker = null;
            LineEdit? enchantmentAmount = null;
            if (enchantmentOptions.Length > 0)
            {
                enchantmentPicker = new(
                    L("ritsulib.debugTools.field.enchantment", "Enchantment"),
                    enchantmentOptions,
                    null)
                {
                    Visible = false,
                };
                enchantmentAmount = CreateIntegerEdit("1");
                enchantmentPicker.AddExpandedControl(Field(
                    L("ritsulib.debugTools.field.enchantmentAmount", "Enchantment amount"),
                    enchantmentAmount));
                var picker = enchantmentPicker;
                root.AddChild(Field(
                    L("ritsulib.debugTools.field.specifyEnchantment", "Specify enchantment"),
                    ModSettingsUiControlTheming.CreateCompactStateToggle(false, enabled =>
                    {
                        specifyEnchantment = enabled;
                        picker.Visible = enabled;
                    })));
                root.AddChild(enchantmentPicker);
            }

            root.AddChild(ActionButton(
                L("ritsulib.debugTools.action.add", "Add"),
                ModSettingsButtonTone.Accent,
                Submit));
            return root;

            Control InitialFlagField(
                RitsuDebugCardEditField field,
                bool initialValue,
                Action<bool> changed)
            {
                return ModSettingsUiControlTheming.CreateCompactToggleField(
                    OperationLabel(field),
                    ModSettingsUiControlTheming.CreateCompactStateToggle(initialValue, changed));
            }

            void Submit()
            {
                if (!TryReadInt(count, 1, RitsuDebugCardActions.MaxCreateCount, out var cardCount) ||
                    !TryReadInt(upgrades, 0, card.MaxUpgradeLevel, out var upgradeLevels))
                    return;

                int? costValue = null;
                if (baseCostChanged)
                {
                    if (baseCost == null || !TryReadInt(
                            baseCost,
                            0,
                            RitsuDebugCardActions.MaxCardEditValue,
                            out var parsedCost))
                        return;
                    costValue = parsedCost;
                }

                int? replayValue = null;
                if (replayChanged)
                {
                    if (!TryReadInt(replay, 0, RitsuDebugCardActions.MaxReplayCount, out var parsedReplay))
                        return;
                    replayValue = parsedReplay;
                }

                Dictionary<string, int>? dynamicVariables = null;
                foreach (var key in changedDynamicVariables)
                {
                    if (!TryReadInt(
                            dynamicVariableEditors[key],
                            0,
                            RitsuDebugCardActions.MaxCardEditValue,
                            out var value))
                        return;
                    dynamicVariables ??= new(StringComparer.Ordinal);
                    dynamicVariables.Add(key, value);
                }

                string? enchantmentId = null;
                int? enchantmentAmountValue = null;
                if (specifyEnchantment)
                {
                    if (enchantmentPicker?.SelectedId == null)
                    {
                        SetStatus(L(
                            "ritsulib.debugTools.selectEnchantment",
                            "Select an enchantment before creating the card."), true);
                        return;
                    }

                    if (enchantmentAmount == null ||
                        !TryReadInt(
                            enchantmentAmount,
                            1,
                            RitsuDebugCardActions.MaxCardEditValue,
                            out var parsedEnchantmentAmount))
                        return;
                    enchantmentId = enchantmentPicker.SelectedId;
                    enchantmentAmountValue = parsedEnchantmentAmount;
                }

                if (!TryGetActionContext(out var requester, out var selectedTarget))
                    return;
                var initialState = new RitsuDebugCardActions.CardStatePayload(
                    costValue,
                    replayValue,
                    dynamicVariables,
                    exhaust,
                    ethereal,
                    unplayable,
                    enchantmentId,
                    enchantmentAmountValue);
                RunAction(() => RitsuDebugCardActions.SubmitCreateCard(
                    requester,
                    selectedTarget,
                    card.Id.ToString(),
                    selectedPile,
                    cardCount,
                    upgradeLevels,
                    initialState));
            }
        }

        private Control CreatePileCardDetail(PileCardEntry entry)
        {
            var root = DetailShell(
                entry.Card.Id.ToString(),
                () => entry.Card.Portrait,
                $"{EnumLabel(entry.PileType)} #{entry.Index + 1} · {EnumLabel(entry.Card.Type)} · {EnumLabel(entry.Card.Rarity)}",
                SafeCardDescription(entry.Card),
                descriptionRefreshFactory: () => SafeCardDescription(entry.Card));
            AddSectionTitle(root, L("ritsulib.debugTools.action.cardState", "Card state"));
            var upgrades = CreateIntegerEdit("1");
            var upgradeButton = ActionButton(
                L("ritsulib.debugTools.action.upgrade", "Upgrade"),
                ModSettingsButtonTone.Accent,
                () =>
                {
                    if (!TryReadInt(upgrades, 1, RitsuDebugCardActions.MaxBulkUpgradeLevels, out var levels) ||
                        !TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitUpgradeCard(
                        requester,
                        target,
                        entry.PileType,
                        entry.Index,
                        entry.Card.Id.ToString(),
                        levels,
                        entry.CombatCardId));
                });
            root.AddChild(ActionField(
                L("ritsulib.debugTools.field.upgrades", "Upgrade levels"),
                upgrades,
                upgradeButton));
            var replay = CreateIntegerEdit(entry.Card.BaseReplayCount.ToString());
            var replayButton = ActionButton(
                L("ritsulib.debugTools.action.apply", "Apply"),
                ModSettingsButtonTone.Normal,
                () =>
                {
                    if (!TryReadInt(replay, 0, RitsuDebugCardActions.MaxReplayCount, out var count) ||
                        !TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitSetReplayCount(
                        requester,
                        target,
                        entry.PileType,
                        entry.Index,
                        entry.Card.Id.ToString(),
                        count,
                        entry.CombatCardId));
                });
            root.AddChild(ActionField(
                L("ritsulib.debugTools.field.replay", "Replay count"),
                replay,
                replayButton));

            AddSectionTitle(root, L("ritsulib.debugTools.action.cardPlacement", "Copies and pile placement"));
            PileType[] destinationPiles = HasActiveCombatState(entry.Card.Owner)
                ? [.. RitsuDebugCardActions.GetMutablePileNames().Select(static name => Enum.Parse<PileType>(name))]
                : [PileType.Deck];
            var destinationPile = entry.PileType == PileType.Deck
                ? PileType.Deck
                : destinationPiles.FirstOrDefault(pile => pile != entry.PileType, PileType.Hand);
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.field.destinationPile", "Destination pile"),
                [.. destinationPiles.Select(pile => (pile, EnumLabel(pile)))],
                destinationPile,
                value => destinationPile = value));
            AddHint(root, L("ritsulib.debugTools.fullHandPlacement",
                "Cards sent to a full hand follow the game's rules and enter the discard pile."));
            var copyCount = IntField(root, L("ritsulib.debugTools.field.copyCount", "Number of copies"), "1");
            var placementActions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            placementActions.AddThemeConstantOverride("separation", 8);
            placementActions.AddChild(ActionButton(
                L("ritsulib.debugTools.action.copyCard", "Copy"),
                ModSettingsButtonTone.Accent,
                () =>
                {
                    if (!TryReadInt(copyCount, 1, RitsuDebugCardActions.MaxCopyCount, out var count) ||
                        !TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitCopyCard(
                        requester,
                        target,
                        entry.PileType,
                        entry.Index,
                        entry.Card.Id.ToString(),
                        destinationPile,
                        count,
                        entry.CombatCardId));
                }));
            var moveButton = ActionButton(
                L("ritsulib.debugTools.action.moveCard", "Move"),
                ModSettingsButtonTone.Normal,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitMoveCard(
                        requester,
                        target,
                        entry.PileType,
                        entry.Index,
                        entry.Card.Id.ToString(),
                        destinationPile,
                        entry.CombatCardId));
                });
            moveButton.Disabled = !entry.PileType.IsCombatPile();
            placementActions.AddChild(moveButton);
            root.AddChild(placementActions);

            AddSectionTitle(root, L("ritsulib.debugTools.action.cardProperties", "Card properties"));
            if (!entry.Card.EnergyCost.CostsX)
                AddCardValueEditor(
                    root,
                    entry,
                    OperationLabel(RitsuDebugCardEditField.Cost),
                    RitsuDebugCardEditField.Cost,
                    entry.Card.EnergyCost.GetWithModifiers(CostModifiers.None).ToString());
            foreach (var (key, dynamicVar) in entry.Card.DynamicVars.OrderBy(static pair => pair.Key,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (dynamicVar.BaseValue is < 0 or > RitsuDebugCardActions.MaxCardEditValue ||
                    dynamicVar.BaseValue != decimal.Truncate(dynamicVar.BaseValue))
                    continue;
                AddCardValueEditor(
                    root,
                    entry,
                    key,
                    RitsuDebugCardEditField.DynamicVar,
                    decimal.ToInt32(dynamicVar.BaseValue).ToString(),
                    key);
            }

            AddSectionTitle(root, L("ritsulib.debugTools.action.cardFlags", "Card flags"));
            var localKeywords = entry.Card.GetKeywordsWithSources(KeywordSources.Local);
            root.AddChild(ModSettingsUiControlTheming.CreateCompactEditorRow(
                2,
                CardFlagField(root, entry, RitsuDebugCardEditField.Exhaust,
                    localKeywords.Contains(CardKeyword.Exhaust)),
                CardFlagField(root, entry, RitsuDebugCardEditField.Ethereal,
                    localKeywords.Contains(CardKeyword.Ethereal)),
                CardFlagField(root, entry, RitsuDebugCardEditField.Unplayable,
                    localKeywords.Contains(CardKeyword.Unplayable))));

            var enchantments = ModelDb.DebugEnchantments
                .OrderBy(SafeTitle, StringComparer.CurrentCultureIgnoreCase)
                .Select(enchantment => new RitsuDebugEnchantmentOption(
                    enchantment.Id.ToString(),
                    SafeTitle(enchantment),
                    () => enchantment.Icon))
                .ToArray();
            AddSectionTitle(root, L("ritsulib.debugTools.field.enchantment", "Enchantment"));
            if (enchantments.Length > 0)
            {
                var currentEnchantmentId = entry.Card.Enchantment?.Id.ToString();
                var enchantmentPicker = new RitsuDebugEnchantmentPicker(
                    L("ritsulib.debugTools.field.enchantment", "Enchantment"),
                    enchantments,
                    currentEnchantmentId);
                root.AddChild(enchantmentPicker);
                var enchantmentAmount = CreateIntegerEdit("1");
                var enchantButton = ActionButton(
                    L("ritsulib.debugTools.action.enchant", "Set enchantment"),
                    ModSettingsButtonTone.Accent,
                    () =>
                    {
                        if (!TryReadInt(enchantmentAmount, 1, RitsuDebugCardActions.MaxCardEditValue,
                                out var amount) ||
                            enchantmentPicker.SelectedId == null ||
                            !TryGetActionContext(out var requester, out var target))
                            return;
                        RunAction(() => RitsuDebugCardActions.SubmitEnchantCard(
                            requester,
                            target,
                            entry.PileType,
                            entry.Index,
                            entry.Card.Id.ToString(),
                            enchantmentPicker.SelectedId,
                            amount,
                            entry.CombatCardId));
                    });
                enchantButton.Disabled = enchantmentPicker.SelectedId == null;
                enchantmentPicker.SelectionChanged += selectedId => enchantButton.Disabled = selectedId == null;
                enchantmentPicker.AddExpandedControl(ActionField(
                    L("ritsulib.debugTools.field.enchantmentAmount", "Enchantment amount"),
                    enchantmentAmount,
                    enchantButton));
                enchantmentPicker.AddExpandedControl(ActionButton(
                    L("ritsulib.debugTools.action.clearEnchantment", "Clear enchantment"),
                    ModSettingsButtonTone.Normal,
                    () =>
                    {
                        if (!TryGetActionContext(out var requester, out var target))
                            return;
                        RunAction(() => RitsuDebugCardActions.SubmitClearCardEnchantment(
                            requester,
                            target,
                            entry.PileType,
                            entry.Index,
                            entry.Card.Id.ToString(),
                            entry.CombatCardId));
                    }));
            }
            else
            {
                AddHint(root, L("ritsulib.debugTools.noEnchantments", "No enchantments are available."));
            }

            root.AddChild(ActionButton(
                L("ritsulib.debugTools.action.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitRemoveCard(
                        requester,
                        target,
                        entry.PileType,
                        entry.Index,
                        entry.Card.Id.ToString(),
                        entry.CombatCardId));
                }));
            return root;
        }

        private void AddCardValueEditor(
            VBoxContainer root,
            PileCardEntry entry,
            string label,
            RitsuDebugCardEditField field,
            string initialValue,
            string? dynamicVarKey = null)
        {
            var edit = CreateIntegerEdit(initialValue);
            root.AddChild(ActionField(
                label,
                edit,
                ActionButton(
                    L("ritsulib.debugTools.action.apply", "Apply"),
                    ModSettingsButtonTone.Normal,
                    () => SubmitCardEdit(entry, field, edit, dynamicVarKey))));
        }

        private Control CardFlagField(
            RitsuDebugLiveDetailContainer detail,
            PileCardEntry entry,
            RitsuDebugCardEditField field,
            bool enabled)
        {
            var acceptedValue = enabled;
            var toggleHolder = new ModSettingsToggleControl?[1];
            var toggle = ModSettingsUiControlTheming.CreateCompactStateToggle(enabled, value =>
            {
                if (SubmitCardEdit(entry, field, value ? 1 : 0))
                {
                    acceptedValue = value;
                    return;
                }

                toggleHolder[0]?.SetValue(acceptedValue);
            });
            toggleHolder[0] = toggle;
            detail.RegisterRefresh(() =>
            {
                acceptedValue = ReadCardFlag(entry.Card, field);
                toggle.SetValue(acceptedValue);
            });
            return ModSettingsUiControlTheming.CreateCompactToggleField(OperationLabel(field), toggle);
        }

        private static bool ReadCardFlag(CardModel card, RitsuDebugCardEditField field)
        {
            var keyword = field switch
            {
                RitsuDebugCardEditField.Exhaust => CardKeyword.Exhaust,
                RitsuDebugCardEditField.Ethereal => CardKeyword.Ethereal,
                RitsuDebugCardEditField.Unplayable => CardKeyword.Unplayable,
                RitsuDebugCardEditField.Cost or RitsuDebugCardEditField.DynamicVar =>
                    throw new ArgumentOutOfRangeException(nameof(field), field, "The field is not a card flag."),
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
            return card.GetKeywordsWithSources(KeywordSources.Local).Contains(keyword);
        }

        private void SubmitCardEdit(
            PileCardEntry entry,
            RitsuDebugCardEditField field,
            LineEdit edit,
            string? dynamicVarKey)
        {
            if (!TryReadInt(edit, 0, RitsuDebugCardActions.MaxCardEditValue, out var value))
                return;
            SubmitCardEdit(entry, field, value, dynamicVarKey);
        }

        private bool SubmitCardEdit(
            PileCardEntry entry,
            RitsuDebugCardEditField field,
            int value,
            string? dynamicVarKey = null)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return false;
            return RunAction(() => RitsuDebugCardActions.SubmitEditCard(
                requester,
                target,
                entry.PileType,
                entry.Index,
                entry.Card.Id.ToString(),
                field,
                value,
                dynamicVarKey,
                entry.CombatCardId));
        }

        private Control CreateRelicDetail(RelicModel relic)
        {
            var root = DetailShell(
                relic.Id.ToString(),
                () => relic.BigIcon,
                EnumLabel(relic.Rarity),
                SafeDescription(() => relic.DynamicDescription.GetFormattedText()));
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(ActionButton(L("ritsulib.debugTools.action.add", "Add"), ModSettingsButtonTone.Accent,
                () => SubmitInventoryAction((requester, target) =>
                    RitsuDebugInventoryActions.SubmitAddRelic(requester, target, relic.Id.ToString()))));
            row.AddChild(ActionButton(L("ritsulib.debugTools.action.remove", "Remove"), ModSettingsButtonTone.Danger,
                () => SubmitInventoryAction((requester, target) =>
                    RitsuDebugInventoryActions.SubmitRemoveRelic(requester, target, relic.Id.ToString()))));
            root.AddChild(row);
            return root;
        }

        private Control CreatePotionDetail(PotionModel potion)
        {
            var root = DetailShell(
                potion.Id.ToString(),
                () => PotionPreviewImage(potion),
                EnumLabel(potion.Rarity),
                SafeDescription(() => potion.DynamicDescription.GetFormattedText()));
            root.AddChild(ActionButton(L("ritsulib.debugTools.action.add", "Add"), ModSettingsButtonTone.Accent,
                () => SubmitInventoryAction((requester, target) =>
                    RitsuDebugInventoryActions.SubmitAddPotion(requester, target, potion.Id.ToString()))));
            return root;
        }

        private Control CreatePowerDetail(PowerModel power)
        {
            var root = DetailShell(
                power.Id.ToString(),
                () => power.Icon,
                EnumLabel(power.Type),
                SafeDescription(() => power.Description.GetFormattedText()));
            var creatures = CurrentCreatures();
            if (creatures.Length == 0)
            {
                AddHint(root, L("ritsulib.debugTools.noCombat", "Start combat to use creature and power actions."));
                return root;
            }

            _selectedCreatureCombatId = PreferredCreatureCombatId(creatures);
            var amount = IntField(root, L("ritsulib.debugTools.field.amount", "Stack amount"), "1");
            root.AddChild(ActionGrid(
            [
                (L("ritsulib.debugTools.action.applySelectedCreature", "Apply to selected"),
                    ModSettingsButtonTone.Accent,
                    ApplySelected),
                (L("ritsulib.debugTools.action.applyAllCreatures", "Apply to all creatures"),
                    ModSettingsButtonTone.Normal,
                    () => ApplyMany(static creature => !creature.IsDead)),
                (L("ritsulib.debugTools.action.applyAllPlayers", "Apply to all players"),
                    ModSettingsButtonTone.Normal,
                    () => ApplyMany(static creature => creature is { IsPlayer: true, IsDead: false })),
                (L("ritsulib.debugTools.action.applyAllEnemies", "Apply to all enemies"),
                    ModSettingsButtonTone.Normal,
                    () => ApplyMany(static creature => creature is { IsPlayer: false, IsDead: false })),
                (L("ritsulib.debugTools.action.removeSelectedCreature", "Remove from selected"),
                    ModSettingsButtonTone.Danger,
                    RemoveSelected),
            ]));
            AddCurrentPowerManager(root, SelectedCombatId, false);
            return root;

            uint? SelectedCombatId()
            {
                var currentCreatures = CurrentCreatures();
                if (currentCreatures.Length == 0)
                    return null;
                var combatId = PreferredCreatureCombatId(currentCreatures);
                _selectedCreatureCombatId = combatId;
                return combatId;
            }

            void ApplySelected()
            {
                if (!TryReadInt(amount, 1, RitsuDebugCombatActions.MaxAmount, out var value) ||
                    !TryGetActionContext(out var requester, out var target))
                    return;
                if (SelectedCombatId() is not { } combatId)
                {
                    SetStatus(L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available."), true);
                    return;
                }

                RunAction(() => RitsuDebugCombatActions.SubmitApplyPower(
                    requester,
                    target,
                    combatId,
                    power.Id.ToString(),
                    value));
            }

            void ApplyMany(Func<Creature, bool> matches)
            {
                if (!TryReadInt(amount, 1, RitsuDebugCombatActions.MaxAmount, out var value) ||
                    !TryGetActionContext(out var requester, out var target))
                    return;
                uint[] combatIds =
                [
                    .. CurrentCreatures()
                        .Where(matches)
                        .Select(static creature => creature.CombatId!.Value),
                ];
                if (combatIds.Length == 0)
                {
                    SetStatus(L("ritsulib.debugTools.noMatchingCreatures",
                        "No matching creatures are available."), true);
                    return;
                }

                RunAction(() => RitsuDebugCombatActions.SubmitApplyPowerToCreatures(
                    requester,
                    target,
                    combatIds,
                    power.Id.ToString(),
                    value));
            }

            void RemoveSelected()
            {
                if (!TryGetActionContext(out var requester, out var target))
                    return;
                if (SelectedCombatId() is not { } combatId)
                {
                    SetStatus(L("ritsulib.debugTools.targetChanged",
                        "The selected target is no longer available."), true);
                    return;
                }

                RunAction(() => RitsuDebugCombatActions.SubmitRemovePower(
                    requester,
                    target,
                    combatId,
                    power.Id.ToString()));
            }
        }

        private Control CreatePlayerDetail(Player player)
        {
            var root = DetailShell(
                player.Character.Id.ToString(),
                null,
                PlayerVitals(player),
                string.Format(L("ritsulib.debugTools.playerSummary", "Max energy {0} · Potion slots {1}"),
                    player.MaxEnergy,
                    player.MaxPotionCount),
                () => PlayerVitals(player),
                () => string.Format(
                    L("ritsulib.debugTools.playerSummary", "Max energy {0} · Potion slots {1}"),
                    player.MaxEnergy,
                    player.MaxPotionCount));
            AddSectionTitle(root, L("ritsulib.debugTools.action.playerState", "Player state"));
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.AddGold, "100",
                -RitsuDebugPlayerActions.MaxGold, RitsuDebugPlayerActions.MaxGold);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetGold, player.Gold.ToString(), 0,
                RitsuDebugPlayerActions.MaxGold);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.Heal, "1", 0,
                RitsuDebugPlayerActions.MaxHitPoints);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetCurrentHp,
                player.Creature.CurrentHp.ToString(), 1, RitsuDebugPlayerActions.MaxHitPoints,
                ActionButton(
                    L("ritsulib.debugTools.action.fullHeal", "Restore full HP"),
                    ModSettingsButtonTone.Accent,
                    () => SubmitPlayerOperation(
                        player.NetId,
                        RitsuDebugPlayerOperation.Heal,
                        player.Creature.MaxHp)));
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetMaxHp,
                player.Creature.MaxHp.ToString(), 1, RitsuDebugPlayerActions.MaxHitPoints);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetMaxEnergy,
                player.MaxEnergy.ToString(), 1, RitsuDebugPlayerActions.MaxCombatResource);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetPotionSlots,
                player.MaxPotionCount.ToString(), 0, RitsuDebugPlayerActions.MaxPotionSlots);
            AddHint(root, L("ritsulib.debugTools.potionSlotReduction",
                "Reducing potion slots keeps potions that still fit and discards the rest."));
            var combatState = player.PlayerCombatState;
            if (HasActiveCombatState(player) && combatState != null)
            {
                AddSectionTitle(root, L("ritsulib.debugTools.action.combatState", "Combat state"));
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.GainBlock, "1", 0,
                    RitsuDebugPlayerActions.MaxHitPoints);
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.AddEnergy, "1", 0,
                    RitsuDebugPlayerActions.MaxCombatResource);
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetEnergy,
                    combatState.Energy.ToString(), 0, RitsuDebugPlayerActions.MaxCombatResource);
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.AddStars, "1", 0,
                    RitsuDebugPlayerActions.MaxCombatResource);
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetStars,
                    combatState.Stars.ToString(), 0, RitsuDebugPlayerActions.MaxCombatResource);
                AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.Draw, "1", 1,
                    RitsuDebugPlayerActions.MaxDrawCount);
                if (player.Creature.CombatId is { } combatId)
                {
                    _selectedCreatureCombatId = combatId;
                    AddCurrentPowerManager(root, () => combatId, true);
                }
            }

            AddSectionTitle(root, L("ritsulib.debugTools.action.cardPiles", "Card piles"));
            var piles = HasActiveCombatState(player)
                ? RitsuDebugCardActions.GetMutablePileNames()
                    .Select(static name => Enum.Parse<PileType>(name))
                    .ToArray()
                : [PileType.Deck];
            var pileActions = new List<(string Text, ModSettingsButtonTone Tone, Action Action)>();
            foreach (var pile in piles)
            {
                var capturedPile = pile;
                pileActions.Add((
                    string.Format(L("ritsulib.debugTools.action.clearPile", "Clear {0}"), EnumLabel(capturedPile)),
                    ModSettingsButtonTone.Danger,
                    () => SubmitTargetedAction(player.NetId, (requester, target) =>
                        RitsuDebugCardActions.SubmitModifyPile(
                            requester,
                            target,
                            capturedPile,
                            RitsuDebugCardPileOperation.Clear))));
                pileActions.Add((
                    string.Format(L("ritsulib.debugTools.action.upgradePile", "Upgrade {0}"),
                        EnumLabel(capturedPile)),
                    ModSettingsButtonTone.Normal,
                    () => SubmitTargetedAction(player.NetId, (requester, target) =>
                        RitsuDebugCardActions.SubmitModifyPile(
                            requester,
                            target,
                            capturedPile,
                            RitsuDebugCardPileOperation.Upgrade,
                            1))));
            }

            root.AddChild(ActionGrid(pileActions));

            AddSectionTitle(root, L("ritsulib.debugTools.action.inventory", "Inventory"));
            root.AddChild(ActionGrid(
            [
                (L("ritsulib.debugTools.action.clearRelics", "Remove all relics"),
                    ModSettingsButtonTone.Danger,
                    () => SubmitTargetedAction(player.NetId, (requester, target) =>
                        RitsuDebugInventoryActions.SubmitClearInventory(
                            requester,
                            target,
                            RitsuDebugInventoryKind.Relics))),
                (L("ritsulib.debugTools.action.clearPotions", "Discard all potions"),
                    ModSettingsButtonTone.Danger,
                    () => SubmitTargetedAction(player.NetId, (requester, target) =>
                        RitsuDebugInventoryActions.SubmitClearInventory(
                            requester,
                            target,
                            RitsuDebugInventoryKind.Potions))),
            ]));

            AddPotionDiscardButtons(root, player);
            return root;
        }

        private Control CreateCreatureDetail(Creature creature)
        {
            if (!creature.CombatId.HasValue)
                return EmptyBrowser(L("ritsulib.debugTools.targetChanged",
                    "The selected target is no longer available."));
            var combatId = creature.CombatId.Value;
            _selectedCreatureCombatId = combatId;
            var root = DetailShell(
                string.Format(
                    L("ritsulib.debugTools.creatureIdentity", "Creature #{0} · {1}"),
                    creature.CombatId,
                    creature.ModelId),
                null,
                CreatureVitals(creature),
                creature.LogName,
                () => CreatureVitals(creature));
            AddSectionTitle(root, L("ritsulib.debugTools.action.creatureValues", "Creature values"));
            AddCreatureOperationEditor(
                root,
                creature,
                RitsuDebugCreatureOperation.SetCurrentHp,
                L("ritsulib.debugTools.field.currentHp", "Current HP"),
                creature.CurrentHp.ToString(),
                creature.IsPlayer ? 1 : 0,
                creature.MaxHp,
                L("ritsulib.debugTools.action.set", "Set"));
            AddCreatureOperationEditor(
                root,
                creature,
                RitsuDebugCreatureOperation.SetMaxHp,
                L("ritsulib.debugTools.field.maxHp", "Maximum HP"),
                creature.MaxHp.ToString(),
                Math.Max(1, creature.CurrentHp),
                RitsuDebugCombatActions.MaxAmount,
                L("ritsulib.debugTools.action.set", "Set"));
            AddCreatureOperationEditor(
                root,
                creature,
                RitsuDebugCreatureOperation.SetBlock,
                L("ritsulib.debugTools.field.block", "Block"),
                creature.Block.ToString(),
                0,
                RitsuDebugCombatActions.MaxAmount,
                L("ritsulib.debugTools.action.set", "Set"));

            AddSectionTitle(root, L("ritsulib.debugTools.action.creatureActions", "Quick actions"));
            var quickActions = new List<(string Text, ModSettingsButtonTone Tone, Action Action)>
            {
                (L("ritsulib.debugTools.action.fullHeal", "Restore full HP"),
                    ModSettingsButtonTone.Accent,
                    () => SubmitCreatureOperation(creature, RitsuDebugCreatureOperation.Heal, creature.MaxHp)),
            };
            if (!creature.IsPlayer)
            {
                quickActions.Add((
                    OperationLabel(RitsuDebugCreatureOperation.Kill),
                    ModSettingsButtonTone.Danger,
                    () => SubmitCreatureOperation(creature, RitsuDebugCreatureOperation.Kill, 0)));
            }

            root.AddChild(ActionGrid(quickActions));
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.Damage, "1", 0,
                RitsuDebugCombatActions.MaxAmount,
                L("ritsulib.debugTools.action.execute", "Execute"));
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.Heal, "1", 0,
                RitsuDebugCombatActions.MaxAmount,
                L("ritsulib.debugTools.action.execute", "Execute"));
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.GainBlock, "1", 0,
                RitsuDebugCombatActions.MaxAmount,
                L("ritsulib.debugTools.action.execute", "Execute"));
            if (creature is { IsPlayer: false, Monster.MoveStateMachine: not null })
                AddMonsterIntentManager(root, creature);
            AddCreaturePresetManager(root, creature);
            AddCurrentPowerManager(root, () => combatId, true);

            var directActions = new List<(string Text, ModSettingsButtonTone Tone, Action Action)>();
            if (!creature.IsPlayer)
            {
                if (creature.Monster != null)
                    directActions.Add((
                        L("ritsulib.debugTools.action.duplicateCreature", "Add another of this creature"),
                        ModSettingsButtonTone.Normal,
                        () =>
                        {
                            if (!TryGetActionContext(out var requester, out var target))
                                return;
                            RunAction(() => RitsuDebugCombatActions.SubmitDuplicateCreature(
                                requester,
                                target,
                                creature));
                        }
                    ));

                directActions.Add((
                    L("ritsulib.debugTools.action.defeatAllEnemies", "Defeat all enemies"),
                    ModSettingsButtonTone.Danger,
                    () =>
                    {
                        if (!TryGetActionContext(out var requester, out var target))
                            return;
                        RunAction(() => RitsuDebugCombatActions.SubmitDefeatAllEnemies(requester, target));
                    }
                ));
            }

            if (directActions.Count > 0)
            {
                AddSectionTitle(root, L("ritsulib.debugTools.action.enemyActions", "Enemy actions"));
                root.AddChild(ActionGrid(directActions));
            }

            return root;
        }

        private void AddMonsterIntentManager(RitsuDebugLiveDetailContainer root, Creature creature)
        {
            var combatId = creature.CombatId!.Value;
            AddSectionTitle(root, L("ritsulib.debugTools.action.monsterIntent", "Intent"));
            var picker = new RitsuDebugMonsterIntentPicker(creature);
            picker.OpenRequested += () => OpenMonsterIntentWindow(combatId);
            root.AddChild(picker);

            root.AddChild(ActionGrid([
                (L("ritsulib.debugTools.action.performMonsterIntent", "Perform current intent"),
                    ModSettingsButtonTone.Accent,
                    () => SubmitPerformMonsterIntent(combatId)),
                (L("ritsulib.debugTools.action.stunMonster", "Stun"),
                    ModSettingsButtonTone.Normal,
                    () => SubmitStunMonster(combatId)),
            ]));
            AddHint(root, L("ritsulib.debugTools.monsterIntentHint",
                "Open the floating intent map to watch transitions and switch intents while viewing combat."));

            root.RegisterRefresh(() =>
            {
                if (RitsuDebugCombatActions.FindCreature(combatId) is { Monster: not null } current)
                    picker.Refresh(current);
            });
        }

        private void AddCurrentPowerManager(
            RitsuDebugLiveDetailContainer root,
            Func<uint?> combatIdFactory,
            bool includePowerLibraryButton)
        {
            AddSectionTitle(root, L("ritsulib.debugTools.action.powers", "Powers"));
            var toolbar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            toolbar.AddThemeConstantOverride("separation", 8);
            if (includePowerLibraryButton)
                toolbar.AddChild(ActionButton(
                    L("ritsulib.debugTools.action.addPower", "Add Power"),
                    ModSettingsButtonTone.Accent,
                    () =>
                    {
                        if (!combatIdFactory().HasValue)
                            return;
                        _selectedCreatureCombatId = combatIdFactory();
                        SelectPage($"{Const.ModId}:powers");
                    }));

            var clearButton = ActionButton(
                L("ritsulib.debugTools.action.clearPowers", "Clear all"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (combatIdFactory() is not { } combatId)
                        return;
                    SubmitCreatureOperation(combatId, RitsuDebugCreatureOperation.ClearPowers, 0);
                });
            toolbar.AddChild(clearButton);
            root.AddChild(toolbar);

            var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            list.AddThemeConstantOverride("separation", 6);
            root.AddChild(list);

            RefreshPowerList();
            root.RegisterRefresh(RefreshPowerList);
            return;

            void RefreshPowerList()
            {
                foreach (var child in list.GetChildren())
                {
                    list.RemoveChild(child);
                    child.QueueFree();
                }

                var currentCombatId = combatIdFactory();
                var creature = currentCombatId.HasValue
                    ? RitsuDebugCombatActions.FindCreature(currentCombatId.Value)
                    : null;
                var powers = creature?.Powers.ToArray() ?? [];
                clearButton.Disabled = powers.Length == 0;
                if (powers.Length == 0)
                {
                    list.AddChild(CreatePowerListHint(creature == null
                        ? L("ritsulib.debugTools.targetChanged", "The selected target is no longer available.")
                        : L("ritsulib.debugTools.noActivePowers", "No active Powers.")));
                    return;
                }

                foreach (var power in powers)
                    list.AddChild(CreateCurrentPowerRow(currentCombatId!.Value, power));
            }
        }

        private Control CreateCurrentPowerRow(uint combatId, PowerModel power)
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle());
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_top", 6);
            margin.AddThemeConstantOverride("margin_right", 8);
            margin.AddThemeConstantOverride("margin_bottom", 6);
            panel.AddChild(margin);
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            margin.AddChild(row);

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

            row.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new(32f, 32f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Visible = icon != null,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            identity.AddThemeConstantOverride("separation", 1);
            row.AddChild(identity);
            var title = new Label
            {
                Text = $"{SafeTitle(power)}  ×{power.Amount}",
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            title.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            title.AddThemeFontSizeOverride("font_size", DetailMetadataFontSize);
            title.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            identity.AddChild(title);
            var id = new Label
            {
                Text = power.Id.ToString(),
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            id.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            id.AddThemeFontSizeOverride("font_size", DetailIdentifierFontSize);
            id.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            identity.AddChild(id);

            var remove = new ModSettingsTextButton(
                L("ritsulib.debugTools.action.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () => SubmitRemovePower(combatId, power.Id.ToString()))
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                CustomMinimumSize = new(84f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            row.AddChild(remove);
            return panel;
        }

        private static Label CreatePowerListHint(string text)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", DetailMetadataFontSize);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            return label;
        }

        private void SubmitRemovePower(uint combatId, string powerId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitRemovePower(
                requester,
                target,
                combatId,
                powerId));
        }

        private void AddPlayerOperationEditor(
            VBoxContainer root,
            Player player,
            RitsuDebugPlayerOperation operation,
            string initialValue,
            int minimum,
            int maximum,
            Button? secondaryAction = null)
        {
            AddIntegerActionRow(
                root,
                OperationLabel(operation),
                initialValue,
                minimum,
                maximum,
                value => SubmitPlayerOperation(player.NetId, operation, value),
                secondaryAction: secondaryAction);
        }

        private void SubmitPlayerOperation(ulong playerNetId, RitsuDebugPlayerOperation operation, int value)
        {
            if (!TryGetActionContext(out var requester, out _))
                return;
            var current = GetPlayers().FirstOrDefault(candidate => candidate.NetId == playerNetId);
            if (current == null)
            {
                SetStatus(L("ritsulib.debugTools.targetChanged", "The selected target is no longer available."), true);
                return;
            }

            RunAction(() => RitsuDebugPlayerActions.Submit(requester, current, operation, value));
        }

        private void SubmitCreatureOperation(
            Creature creature,
            RitsuDebugCreatureOperation operation,
            int value)
        {
            if (!creature.CombatId.HasValue)
                return;
            SubmitCreatureOperation(creature.CombatId.Value, operation, value);
        }

        private void SubmitCreatureOperation(
            uint combatId,
            RitsuDebugCreatureOperation operation,
            int value)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitModifyCreature(
                requester,
                target,
                combatId,
                operation,
                value));
        }

        private void SubmitStunMonster(uint combatId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitStunMonster(requester, target, combatId));
        }

        private void OpenMonsterIntentWindow(uint combatId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            if (RitsuOverlayHostService.TryOpenMonsterIntentWindow(
                    combatId,
                    requester.NetId,
                    target.NetId,
                    out var error))
                return;
            SetStatus(error, true);
        }

        private void SubmitPerformMonsterIntent(uint combatId)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitPerformMonsterIntent(requester, target, combatId));
        }

        private void AddCreatureOperationEditor(
            VBoxContainer root,
            Creature creature,
            RitsuDebugCreatureOperation operation,
            string initialValue,
            int minimum,
            int maximum,
            string? actionText = null)
        {
            AddCreatureOperationEditor(
                root,
                creature,
                operation,
                OperationLabel(operation),
                initialValue,
                minimum,
                maximum,
                actionText ?? L("ritsulib.debugTools.action.apply", "Apply"));
        }

        private void AddCreatureOperationEditor(
            VBoxContainer root,
            Creature creature,
            RitsuDebugCreatureOperation operation,
            string label,
            string initialValue,
            int minimum,
            int maximum,
            string actionText)
        {
            AddIntegerActionRow(
                root,
                label,
                initialValue,
                minimum,
                maximum,
                value => SubmitCreatureOperation(creature, operation, value),
                actionText);
        }

        private Control CreateEncounterDetail(EncounterModel encounter)
        {
            var root = DetailShell(encounter.Id.ToString(), null,
                L("ritsulib.debugTools.encounter", "Encounter"), string.Empty);
            var monsters = GetEncounterMonsters(encounter);
            if (monsters.Length > 0)
                root.AddChild(new RitsuDebugCreaturePreview(monsters));
            AddTransitionNotice(root);
            root.AddChild(ActionGrid(
            [
                (L("ritsulib.debugTools.action.enterEncounter", "Enter encounter"),
                    ModSettingsButtonTone.Danger,
                    () =>
                    {
                        if (!TryGetActionContext(out var requester, out _))
                            return;
                        RunAction(() =>
                            RitsuDebugRunActions.SubmitEnterEncounter(requester, encounter.Id.ToString()));
                    }),
                (L("ritsulib.debugTools.action.addEncounterEnemies", "Add its enemies to combat"),
                    ModSettingsButtonTone.Normal,
                    () =>
                    {
                        if (!TryGetActionContext(out var requester, out var target))
                            return;
                        RunAction(() => RitsuDebugCombatActions.SubmitAddEncounter(
                            requester,
                            target,
                            encounter.Id.ToString()));
                    }),
            ]));
            return root;
        }

        private Control CreateMonsterDetail(MonsterModel monster)
        {
            var root = DetailShell(
                monster.Id.ToString(),
                null,
                MonsterVitals(monster),
                L("ritsulib.debugTools.monsterAddDescription",
                    "Adds this monster to the current combat and lays out enemies automatically when no slot remains."));
            root.AddChild(new RitsuDebugCreaturePreview([monster]));
            root.AddChild(ActionButton(
                L("ritsulib.debugTools.action.addMonster", "Add to combat"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCombatActions.SubmitAddMonster(
                        requester,
                        target,
                        monster.Id.ToString()));
                }));
            return root;
        }

        private Control CreateRoomDetail(RoomType roomType)
        {
            var root = DetailShell(
                roomType.ToString(),
                null,
                L("ritsulib.debugTools.room", "Room"),
                string.Empty);
            AddTransitionNotice(root);
            root.AddChild(ActionButton(
                L("ritsulib.debugTools.action.enterRoom", "Enter room"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out _))
                        return;
                    RunAction(() => RitsuDebugRunActions.SubmitEnterRoom(requester, roomType));
                }));
            return root;
        }

        private Control CreateEventDetail(EventModel eventModel)
        {
            var root = DetailShell(
                eventModel.Id.ToString(),
                null,
                eventModel is AncientEventModel
                    ? L("ritsulib.debugTools.ancient", "Ancient")
                    : L("ritsulib.debugTools.event", "Event"),
                string.Empty);
            AddTransitionNotice(root);
            string? ancientOption = null;
            if (eventModel is AncientEventModel ancient)
            {
                var optionFeedback = default(RitsuDebugActionFeedback);
                var options = Array.Empty<(string Value, string Label)>();
                if (TryGetTargetPlayer(out var target) &&
                    RitsuDebugRunActions.TryGetAvailableAncientOptions(
                        ancient,
                        target,
                        out var availableOptions,
                        out optionFeedback))
                    options =
                    [
                        .. availableOptions.Select(option =>
                        {
                            var token = RitsuDebugRunActions.GetAncientOptionToken(option);
                            var title = SafeDescription(() => option.Title?.GetFormattedText());
                            return (token, string.IsNullOrWhiteSpace(title) ? token : title);
                        }),
                    ];

                if (optionFeedback.IsValid())
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not list options for '{ancient.Id}': " +
                        optionFeedback.GetEnglishText());
                    AddHint(root, L("ritsulib.debugTools.ancientOptionsUnavailable",
                        "Available options could not be determined for the selected player."));
                }

                if (options.Length > 0)
                {
                    var selectableOptions = options;
                    var optionFieldHolder = new Control?[1];
                    var specifyOption = ModSettingsUiControlTheming.CreateCompactStateToggle(false, enabled =>
                    {
                        ancientOption = enabled ? selectableOptions[0].Value : null;
                        if (optionFieldHolder[0] != null)
                            optionFieldHolder[0]!.Visible = enabled;
                    });
                    root.AddChild(Field(
                        L("ritsulib.debugTools.field.specifyAncientOption", "Specify option"),
                        specifyOption));
                    var optionField = DropdownField(
                        L("ritsulib.debugTools.field.ancientOption", "Ancient option"),
                        selectableOptions,
                        selectableOptions[0].Value,
                        value => ancientOption = value);
                    optionFieldHolder[0] = optionField;
                    optionField.Hide();
                    root.AddChild(optionField);
                }
            }

            root.AddChild(ActionButton(
                L("ritsulib.debugTools.action.enterEvent", "Enter event"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugRunActions.SubmitEnterEvent(
                        requester,
                        target,
                        eventModel.Id.ToString(),
                        ancientOption));
                }));
            return root;
        }

        private void SubmitInventoryAction(Func<Player, Player, RitsuDebugActionSubmission> action)
        {
            if (!TryGetActionContext(out var requester, out var target))
                return;
            RunAction(() => action(requester, target));
        }

        private void SubmitTargetedAction(
            ulong targetNetId,
            Func<Player, Player, RitsuDebugActionSubmission> action)
        {
            if (!TryGetActionContext(out var requester, out _))
                return;
            var target = GetPlayers().FirstOrDefault(player => player.NetId == targetNetId);
            if (target == null)
            {
                SetStatus(L("ritsulib.debugTools.targetChanged", "The selected target is no longer available."), true);
                return;
            }

            RunAction(() => action(requester, target));
        }

        private void AddPotionDiscardButtons(VBoxContainer root, Player player)
        {
            var occupied = Enumerable.Range(0, player.MaxPotionCount)
                .Select(index => (Index: index, Potion: player.GetPotionAtSlotIndex(index)))
                .Where(static entry => entry.Potion != null)
                .ToArray();
            if (occupied.Length == 0)
                return;
            AddSectionTitle(root, L("ritsulib.debugTools.action.discardPotion", "Discard potion"));
            foreach (var entry in occupied)
            {
                var potion = entry.Potion!;
                root.AddChild(ActionButton(
                    $"#{entry.Index + 1} · {SafeTitle(potion)}",
                    ModSettingsButtonTone.Danger,
                    () => SubmitTargetedAction(player.NetId, (requester, target) =>
                        RitsuDebugInventoryActions.SubmitDiscardPotion(
                            requester,
                            target,
                            entry.Index,
                            potion.Id.ToString()))));
            }
        }

        private static RitsuDebugLiveDetailContainer DetailShell(
            string id,
            Func<Texture2D?>? textureFactory,
            string metadata,
            string description,
            Func<string>? metadataRefreshFactory = null,
            Func<string>? descriptionRefreshFactory = null)
        {
            var root = new RitsuDebugLiveDetailContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            root.AddThemeConstantOverride("separation", 12);
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddThemeConstantOverride("separation", 12);
            if (textureFactory != null)
            {
                Texture2D? texture = null;
                try
                {
                    texture = textureFactory();
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not load detail image for '{id}': {ex.Message}");
                }

                if (texture != null)
                {
                    var frame = new PanelContainer { CustomMinimumSize = new(132f, 132f) };
                    frame.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateListItemCardStyle(true));
                    var image = new TextureRect
                    {
                        Texture = texture,
                        CustomMinimumSize = new(120f, 120f),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    };
                    frame.AddChild(image);
                    header.AddChild(frame);
                }
            }

            var identity = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            identity.AddThemeConstantOverride("separation", 4);
            var idLabel = new Label
            {
                Text = id,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            idLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            idLabel.AddThemeFontSizeOverride("font_size", DetailIdentifierFontSize);
            idLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichMuted);
            identity.AddChild(idLabel);
            var metaLabel = new Label { Text = metadata, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            metaLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            metaLabel.AddThemeFontSizeOverride("font_size", DetailMetadataFontSize);
            metaLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichSecondary);
            identity.AddChild(metaLabel);
            if (metadataRefreshFactory != null)
                root.RegisterRefresh(() => metaLabel.Text = metadataRefreshFactory());
            header.AddChild(identity);
            root.AddChild(header);
            if (string.IsNullOrWhiteSpace(description))
                return root;

            var descriptionLabel = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                FitContent = true,
                ScrollActive = false,
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Theme = ModSettingsUiResources.SettingsLineTheme,
                IsHorizontallyBound = true,
                MinFontSize = DetailBodyFontSize,
                MaxFontSize = DetailBodyFontSize,
            };
            descriptionLabel.AddThemeFontOverride("normal_font", RitsuShellTheme.Current.Font.Body);
            descriptionLabel.AddThemeFontOverride("bold_font", RitsuShellTheme.Current.Font.BodyBold);
            descriptionLabel.AddThemeFontSizeOverride("normal_font_size", DetailBodyFontSize);
            descriptionLabel.AddThemeFontSizeOverride("bold_font_size", DetailBodyFontSize);
            descriptionLabel.AddThemeFontSizeOverride("italics_font_size", DetailBodyFontSize);
            descriptionLabel.AddThemeFontSizeOverride("bold_italics_font_size", DetailBodyFontSize);
            descriptionLabel.AddThemeFontSizeOverride("mono_font_size", DetailBodyFontSize);
            descriptionLabel.AddThemeColorOverride("default_color", RitsuShellTheme.Current.Text.RichBody);
            descriptionLabel.SetTextAutoSize(description);
            root.AddChild(descriptionLabel);
            if (descriptionRefreshFactory != null)
                root.RegisterRefresh(() => descriptionLabel.SetTextAutoSize(descriptionRefreshFactory()));

            return root;
        }

        private static Control DropdownField<TValue>(
            string label,
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue selected,
            Action<TValue> changed)
        {
            var dropdown = new ModSettingsDropdownChoiceControl<TValue>(options, selected, changed)
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(180f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            return Field(label, dropdown);
        }

        private static LineEdit IntField(VBoxContainer root, string label, string value)
        {
            var edit = CreateIntegerEdit(value);
            root.AddChild(Field(label, edit));
            return edit;
        }

        private static LineEdit CreateIntegerEdit(string value)
        {
            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                value,
                L("ritsulib.debugTools.integerHint", "Enter an integer"),
                70f);
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            return edit;
        }

        private static LineEdit TextField(VBoxContainer root, string label, string value, string placeholder)
        {
            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(value, placeholder, 160f);
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            root.AddChild(Field(label, edit));
            return edit;
        }

        private static HBoxContainer Field(string label, Control editor)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 10);
            var labelNode = new Label
            {
                Text = label,
                CustomMinimumSize = new(92f, 0f),
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            labelNode.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            labelNode.AddThemeFontSizeOverride("font_size", DetailMetadataFontSize);
            labelNode.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            row.AddChild(labelNode);
            editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(editor);
            return row;
        }

        private static HBoxContainer ActionField(string label, Control editor, Button action)
        {
            var row = Field(label, editor);
            action.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            action.CustomMinimumSize = new(92f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);
            action.TooltipText = action.Text;
            row.AddChild(action);
            return row;
        }

        private void AddIntegerActionRow(
            VBoxContainer root,
            string label,
            string initialValue,
            int minimum,
            int maximum,
            Action<int> submit,
            string? actionText = null,
            Button? secondaryAction = null)
        {
            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                initialValue,
                L("ritsulib.debugTools.integerHint", "Enter an integer"),
                120f);
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var row = Field(label, edit);
            row.AddChild(ActionButton(
                actionText ?? L("ritsulib.debugTools.action.apply", "Apply"),
                ModSettingsButtonTone.Normal,
                () =>
                {
                    if (TryReadInt(edit, minimum, maximum, out var value))
                        submit(value);
                }));
            if (secondaryAction != null)
                row.AddChild(secondaryAction);
            root.AddChild(row);
        }

        private static Button ActionButton(string text, ModSettingsButtonTone tone, Action action)
        {
            return new ModSettingsTextButton(text, tone, action)
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
        }

        private static Control ActionGrid(
            IEnumerable<(string Text, ModSettingsButtonTone Tone, Action Action)> actions)
        {
            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 8);
            foreach (var (text, tone, action) in actions)
                grid.AddChild(ActionButton(text, tone, action));
            return grid;
        }

        private static void AddSectionTitle(VBoxContainer root, string text)
        {
            var divider = new HSeparator();
            divider.AddThemeColorOverride("separator", RitsuShellTheme.Current.Color.Divider);
            root.AddChild(divider);
            var label = new Label { Text = text };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeFontSizeOverride("font_size", DetailSectionFontSize);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            root.AddChild(label);
        }

        private static void AddHint(VBoxContainer root, string text)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", DetailMetadataFontSize);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            root.AddChild(label);
        }

        private static void AddTransitionNotice(VBoxContainer root)
        {
            AddHint(root, L("ritsulib.debugTools.transitionWarning",
                "Entering the selected content exits and replaces the current room."));
        }

        private bool TryReadInt(LineEdit edit, int minimum, int maximum, out int value)
        {
            if (int.TryParse(edit.Text.Trim(), out value) && value >= minimum && value <= maximum)
                return true;
            SetStatus(string.Format(
                L("ritsulib.debugTools.invalidInteger", "Enter an integer between {0} and {1}."),
                minimum,
                maximum), true);
            return false;
        }

        private static Creature[] CurrentCreatures()
        {
            return CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature.CombatId.HasValue)
                .OrderBy(static creature => creature.CombatId)
                .ToArray() ?? [];
        }

        private uint PreferredCreatureCombatId(IReadOnlyList<Creature> creatures)
        {
            if (_selectedCreatureCombatId is { } selected &&
                creatures.Any(creature => creature.CombatId == selected))
                return selected;
            if (TryGetTargetPlayer(out var target) && target.Creature.CombatId is { } targetCombatId &&
                creatures.Any(creature => creature.CombatId == targetCombatId))
                return targetCombatId;
            return creatures[0].CombatId!.Value;
        }

        private static bool HasActiveCombatState(Player player)
        {
            return CombatManager.Instance.IsInProgress &&
                   !CombatManager.Instance.IsOverOrEnding &&
                   player.PlayerCombatState != null;
        }

        private static Texture2D PotionPreviewImage(PotionModel potion)
        {
#if STS2_AT_LEAST_0_110_0
            return potion.LargeImage;
#else
            return potion.Image;
#endif
        }

        private static string OperationLabel<TValue>(TValue value)
            where TValue : struct, Enum
        {
            return L($"ritsulib.debugTools.operation.{typeof(TValue).Name}.{value}", value.ToString());
        }
    }
}
