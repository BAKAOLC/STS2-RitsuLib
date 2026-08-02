using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Diagnostics.DebugTools;
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
                SafeTitle(card),
                card.Id.ToString(),
                () => card.Portrait,
                $"{EnumLabel(card.Type)} · {EnumLabel(card.Rarity)} · {L("ritsulib.debugTools.cost", "Cost")} {CardCost(card)}",
                SafeCardDescription(card));
            AddSectionTitle(root, L("ritsulib.debugTools.action.createCard", "Create card"));
            var destinationPiles = TryGetTargetPlayer(out var target) && HasActiveCombatState(target)
                ? RitsuDebugCardActions.GetMutablePileNames()
                    .Select(static name => Enum.Parse<PileType>(name))
                    .ToArray()
                : [PileType.Deck];
            var selectedPile = destinationPiles.Contains(PileType.Hand)
                ? PileType.Hand
                : destinationPiles[0];
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.field.pile", "Destination pile"),
                destinationPiles
                    .Select(static pile => (pile, EnumLabel(pile)))
                    .ToArray(),
                selectedPile,
                value => selectedPile = value));
            AddHint(root, L("ritsulib.debugTools.fullHandPlacement",
                "Cards sent to a full hand follow the game's rules and enter the discard pile."));
            var upgrades = CreateIntegerEdit("0");
            var addButton = ActionButton(
                L("ritsulib.debugTools.action.add", "Add"),
                ModSettingsButtonTone.Accent,
                () =>
                {
                    if (!TryReadInt(upgrades, 0, card.MaxUpgradeLevel, out var upgradeLevels) ||
                        !TryGetActionContext(out var requester, out var selectedTarget))
                        return;
                    RunAction(() => RitsuDebugCardActions.SubmitCreateCard(
                        requester,
                        selectedTarget,
                        card.Id.ToString(),
                        selectedPile,
                        upgradeLevels));
                });
            root.AddChild(ActionField(
                L("ritsulib.debugTools.field.upgrades", "Upgrade levels"),
                upgrades,
                addButton));
            return root;
        }

        private Control CreatePileCardDetail(PileCardEntry entry)
        {
            var root = DetailShell(
                SafeTitle(entry.Card),
                entry.Card.Id.ToString(),
                () => entry.Card.Portrait,
                $"{EnumLabel(entry.PileType)} #{entry.Index + 1} · {EnumLabel(entry.Card.Type)} · {EnumLabel(entry.Card.Rarity)}",
                SafeCardDescription(entry.Card));
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
                L("ritsulib.debugTools.action.setReplay", "Set replay"),
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
            var destinationPiles = HasActiveCombatState(entry.Card.Owner)
                ? RitsuDebugCardActions.GetMutablePileNames()
                    .Select(static name => Enum.Parse<PileType>(name))
                    .ToArray()
                : [PileType.Deck];
            var destinationPile = entry.PileType == PileType.Deck
                ? PileType.Deck
                : destinationPiles.FirstOrDefault(pile => pile != entry.PileType, PileType.Hand);
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.field.destinationPile", "Destination pile"),
                destinationPiles.Select(static pile => (pile, EnumLabel(pile))).ToArray(),
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
                if (dynamicVar.BaseValue is < 0 or > 999_999 ||
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
                        if (!TryReadInt(enchantmentAmount, 1, 999_999, out var amount) ||
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
                AddHint(root, L("ritsulib.debugTools.noEnchantments", "No enchantments are available."));
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
            if (!TryReadInt(edit, 0, 999_999, out var value))
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
                SafeTitle(relic),
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
                SafeTitle(potion),
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
                SafeTitle(power),
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

            var combatId = creatures[0].CombatId!.Value;
            root.AddChild(DropdownField(
                L("ritsulib.debugTools.field.creature", "Creature"),
                creatures.Select(creature =>
                        (creature.CombatId!.Value, string.Format(
                            L("ritsulib.debugTools.creatureChoice", "#{0} · {1}"),
                            creature.CombatId.Value,
                            creature.Name)))
                    .ToArray(),
                combatId,
                value => combatId = value));
            var amount = IntField(root, L("ritsulib.debugTools.field.amount", "Stack amount"), "1");
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(ActionButton(
                L("ritsulib.debugTools.action.apply", "Apply"),
                ModSettingsButtonTone.Accent,
                () =>
                {
                    if (!TryReadInt(amount, 1, RitsuDebugCombatActions.MaxAmount, out var value) ||
                        !TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCombatActions.SubmitApplyPower(
                        requester,
                        target,
                        combatId,
                        power.Id.ToString(),
                        value));
                }));
            row.AddChild(ActionButton(
                L("ritsulib.debugTools.action.remove", "Remove"),
                ModSettingsButtonTone.Danger,
                () =>
                {
                    if (!TryGetActionContext(out var requester, out var target))
                        return;
                    RunAction(() => RitsuDebugCombatActions.SubmitRemovePower(
                        requester,
                        target,
                        combatId,
                        power.Id.ToString()));
                }));
            root.AddChild(row);
            return root;
        }

        private Control CreatePlayerDetail(Player player)
        {
            var root = DetailShell(
                SafeTitle(player.Character),
                player.Character.Id.ToString(),
                null,
                PlayerVitals(player),
                string.Format(L("ritsulib.debugTools.playerSummary", "Max energy {0} · Potion slots {1}"),
                    player.MaxEnergy,
                    player.MaxPotionCount));
            AddSectionTitle(root, L("ritsulib.debugTools.action.playerState", "Player state"));
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.AddGold, "100", -999_999, 999_999);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetGold, player.Gold.ToString(), 0,
                RitsuDebugPlayerActions.MaxGold);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.Heal, "1", 0,
                RitsuDebugPlayerActions.MaxHitPoints);
            AddPlayerOperationEditor(root, player, RitsuDebugPlayerOperation.SetCurrentHp,
                player.Creature.CurrentHp.ToString(), 1, RitsuDebugPlayerActions.MaxHitPoints);
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
            var root = DetailShell(
                creature.Name,
                string.Format(
                    L("ritsulib.debugTools.creatureIdentity", "Creature #{0} · {1}"),
                    creature.CombatId,
                    creature.ModelId),
                null,
                CreatureVitals(creature),
                creature.LogName);
            AddSectionTitle(root, L("ritsulib.debugTools.action.creatureState", "Creature state"));
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.Damage, "1", 0,
                RitsuDebugCombatActions.MaxAmount);
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.Heal, "1", 0,
                RitsuDebugCombatActions.MaxAmount);
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.GainBlock, "1", 0,
                RitsuDebugCombatActions.MaxAmount);
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.SetCurrentHp,
                creature.CurrentHp.ToString(), creature.IsPlayer ? 1 : 0, creature.MaxHp);
            AddCreatureOperationEditor(root, creature, RitsuDebugCreatureOperation.SetMaxHp,
                creature.MaxHp.ToString(), Math.Max(1, creature.CurrentHp), RitsuDebugCombatActions.MaxAmount);
            var directActions = new List<(string Text, ModSettingsButtonTone Tone, Action Action)>
            {
                (OperationLabel(RitsuDebugCreatureOperation.ClearPowers), ModSettingsButtonTone.Normal,
                    () => SubmitCreatureOperation(creature, RitsuDebugCreatureOperation.ClearPowers, 0)),
            };
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

                directActions.Add((OperationLabel(RitsuDebugCreatureOperation.Kill), ModSettingsButtonTone.Danger,
                    () => SubmitCreatureOperation(creature, RitsuDebugCreatureOperation.Kill, 0)));
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

            root.AddChild(ActionGrid(directActions));
            return root;
        }

        private void AddPlayerOperationEditor(
            VBoxContainer root,
            Player player,
            RitsuDebugPlayerOperation operation,
            string initialValue,
            int minimum,
            int maximum)
        {
            AddIntegerActionRow(
                root,
                OperationLabel(operation),
                initialValue,
                minimum,
                maximum,
                value => SubmitPlayerOperation(player.NetId, operation, value));
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
            if (!TryGetActionContext(out var requester, out var target) ||
                !creature.CombatId.HasValue)
                return;
            RunAction(() => RitsuDebugCombatActions.SubmitModifyCreature(
                requester,
                target,
                creature.CombatId.Value,
                operation,
                value));
        }

        private void AddCreatureOperationEditor(
            VBoxContainer root,
            Creature creature,
            RitsuDebugCreatureOperation operation,
            string initialValue,
            int minimum,
            int maximum)
        {
            AddIntegerActionRow(
                root,
                OperationLabel(operation),
                initialValue,
                minimum,
                maximum,
                value => SubmitCreatureOperation(creature, operation, value));
        }

        private Control CreateEncounterDetail(EncounterModel encounter)
        {
            var root = DetailShell(SafeTitle(encounter), encounter.Id.ToString(), null,
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
                SafeTitle(monster),
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
                RoomLabel(roomType),
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
                SafeTitle(eventModel),
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
                var options = Array.Empty<(string? Value, string Label)>();
                if (TryGetTargetPlayer(out var target) &&
                    RitsuDebugRunActions.TryGetAvailableAncientOptions(
                        ancient,
                        target,
                        out var availableOptions,
                        out optionFeedback))
                    options = availableOptions.Select(option =>
                    {
                        var token = RitsuDebugRunActions.GetAncientOptionToken(option);
                        var title = SafeDescription(() => option.Title?.GetFormattedText());
                        return ((string?)token, string.IsNullOrWhiteSpace(title) ? token : title);
                    }).ToArray();

                if (options.Length == 0 && optionFeedback.IsValid())
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[DebugToolsUi] Could not list options for '{ancient.Id}': " +
                        optionFeedback.GetEnglishText());
                    AddHint(root, L("ritsulib.debugTools.ancientOptionsUnavailable",
                        "Available options could not be determined for the selected player."));
                }

                if (options.Length > 0)
                {
                    ancientOption = options[0].Item1;
                    root.AddChild(DropdownField(
                        L("ritsulib.debugTools.field.ancientOption", "Ancient option"),
                        options,
                        ancientOption,
                        value => ancientOption = value));
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
            string title,
            string id,
            Func<Texture2D?>? textureFactory,
            string metadata,
            string description)
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
            var titleLabel = new Label
            {
                Text = title,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            titleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            titleLabel.AddThemeFontSizeOverride("font_size", DetailTitleFontSize);
            titleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichTitle);
            identity.AddChild(titleLabel);
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
            header.AddChild(identity);
            root.AddChild(header);
            if (!string.IsNullOrWhiteSpace(description))
            {
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
            }

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
            Action<int> submit)
        {
            var edit = ModSettingsUiControlTheming.CreateStyledLineEdit(
                initialValue,
                L("ritsulib.debugTools.integerHint", "Enter an integer"),
                120f);
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var row = Field(label, edit);
            row.AddChild(ActionButton(
                L("ritsulib.debugTools.action.apply", "Apply"),
                ModSettingsButtonTone.Normal,
                () =>
                {
                    if (TryReadInt(edit, minimum, maximum, out var value))
                        submit(value);
                }));
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
