using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    [Flags]
    internal enum RitsuDebugStatePresetCaptureScope
    {
        None = 0,
        Deck = 1 << 0,
        Hand = 1 << 1,
        Draw = 1 << 2,
        Discard = 1 << 3,
        Exhaust = 1 << 4,
        CombatPiles = Hand | Draw | Discard | Exhaust,
        Relics = 1 << 5,
        Potions = 1 << 6,
        Powers = 1 << 7,
        Player = 1 << 8,
        CombatValues = 1 << 9,
    }

    internal readonly record struct RitsuDebugStatePresetCaptureResult(
        RitsuDebugStatePreset Preset,
        int SkippedValueCount);

    internal static class RitsuDebugStatePresetCapture
    {
        private static readonly PileType[] CombatPileTypes =
            [PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust];

        internal static bool HasActiveCombat(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
                   player is { PlayerCombatState: not null, Creature.CombatState: not null };
        }

        internal static bool TryCapture(
            Player player,
            RitsuDebugStatePreset source,
            RitsuDebugStatePresetCaptureScope scope,
            out RitsuDebugStatePresetCaptureResult result,
            out RitsuDebugActionFeedback feedback)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(source);
            if (scope == RitsuDebugStatePresetCaptureScope.None)
            {
                result = default;
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.captureScopeRequired",
                    "Select at least one state page to fill.");
                return false;
            }

            var needsCombat = (scope & RitsuDebugStatePresetCaptureScope.CombatPiles) != 0 ||
                              scope.HasFlag(RitsuDebugStatePresetCaptureScope.Powers) ||
                              scope.HasFlag(RitsuDebugStatePresetCaptureScope.CombatValues);
            if (needsCombat && !HasActiveCombat(player))
            {
                result = default;
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.captureCombatRequired",
                    "Combat piles, powers, and combat values can only be filled during an active combat.");
                return false;
            }

            var preset = source.Clone();
            var skipped = 0;
            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.Deck) &&
                !TryCapturePile(player, PileType.Deck, preset, ref skipped, out feedback))
            {
                result = default;
                return false;
            }

            foreach (var pileType in CombatPileTypes)
            {
                var pileScope = ScopeForPile(pileType);
                if (!scope.HasFlag(pileScope) ||
                    TryCapturePile(player, pileType, preset, ref skipped, out feedback))
                    continue;
                result = default;
                return false;
            }

            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.Relics))
            {
                if (player.Relics.Count > RitsuDebugStatePresetStore.MaximumRelics)
                {
                    result = default;
                    feedback = RitsuDebugActionFeedback.Create(
                        "statePreset.relicLimit",
                        "At most {0} relics can be stored in one preset.",
                        RitsuDebugStatePresetStore.MaximumRelics);
                    return false;
                }

                preset.Relics = new()
                {
                    ApplyMode = RitsuDebugStatePresetApplyMode.Replace,
                    ModelIds = player.Relics.Select(static relic => relic.Id.ToString()).ToList(),
                };
            }

            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.Potions))
                preset.Potions = new()
                {
                    ApplyMode = RitsuDebugStatePresetApplyMode.Replace,
                    Items = player.PotionSlots
                        .Select(static (potion, slot) => potion == null
                            ? null
                            : new RitsuDebugStatePresetPotion
                            {
                                PotionId = potion.Id.ToString(),
                                SlotIndex = slot,
                            })
                        .OfType<RitsuDebugStatePresetPotion>()
                        .ToList(),
                };

            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.Powers))
            {
                var powers = new List<RitsuDebugStatePresetPower>();
                foreach (var power in player.Creature.Powers)
                {
                    if (power.Amount is < 1 or > RitsuDebugCombatActions.MaxAmount)
                    {
                        skipped++;
                        continue;
                    }

                    if (powers.Count >= RitsuDebugStatePresetStore.MaximumPowers)
                    {
                        result = default;
                        feedback = RitsuDebugActionFeedback.Create(
                            "statePreset.powerLimit",
                            "At most {0} powers can be stored in one preset.",
                            RitsuDebugStatePresetStore.MaximumPowers);
                        return false;
                    }

                    powers.Add(new() { PowerId = power.Id.ToString(), Amount = power.Amount });
                }

                preset.Powers = new()
                {
                    ApplyMode = RitsuDebugStatePresetApplyMode.Replace,
                    Items = powers,
                };
            }

            preset.Player ??= new();
            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.Player))
            {
                preset.Player.Gold = player.Gold;
                preset.Player.CurrentHp = player.Creature.CurrentHp;
                preset.Player.MaxHp = player.Creature.MaxHp;
                preset.Player.MaxEnergy = player.MaxEnergy;
                preset.Player.PotionSlots = player.MaxPotionCount;
            }

            if (scope.HasFlag(RitsuDebugStatePresetCaptureScope.CombatValues))
            {
                preset.Player.Energy = player.PlayerCombatState!.Energy;
                preset.Player.Stars = player.PlayerCombatState.Stars;
                preset.Player.Block = player.Creature.Block;
            }

            if (preset.Player is { HasAnyValue: false })
                preset.Player = null;
            var check = RitsuDebugStatePresetActions.ValidateStoredPreset(preset);
            if (!check.Success)
            {
                result = default;
                feedback = check.Feedback;
                return false;
            }

            result = new(preset, skipped);
            feedback = default;
            return true;
        }

        private static bool TryCapturePile(
            Player player,
            PileType pileType,
            RitsuDebugStatePreset preset,
            ref int skipped,
            out RitsuDebugActionFeedback feedback)
        {
            var pile = RitsuDebugCardActions.GetPile(player, pileType);
            if (pile == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    pileType);
                return false;
            }

            if (pile.Cards.Count > RitsuDebugStatePresetStore.MaximumCardsPerPile)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.cardLimit",
                    "At most {0} cards can be stored for each pile.",
                    RitsuDebugStatePresetStore.MaximumCardsPerPile);
                return false;
            }

            var cards = new List<RitsuDebugStatePresetCard>();
            foreach (var card in pile.Cards)
            {
                var captured = CaptureCard(card, ref skipped);
                if (cards.LastOrDefault() is { } previous && HaveSameState(previous, captured) &&
                    previous.Count < RitsuDebugCardActions.MaxCreateCount)
                    previous.Count++;
                else
                    cards.Add(captured);
            }

            preset.CardPiles.RemoveAll(candidate =>
                candidate.Pile.Equals(pileType.ToString(), StringComparison.OrdinalIgnoreCase));
            preset.CardPiles.Add(new()
            {
                Pile = pileType.ToString(),
                ApplyMode = RitsuDebugStatePresetApplyMode.Replace,
                Cards = cards,
            });
            feedback = default;
            return true;
        }

        private static RitsuDebugStatePresetCard CaptureCard(CardModel card, ref int skipped)
        {
            var dynamicVars = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (key, dynamicVar) in card.DynamicVars)
            {
                var value = dynamicVar.BaseValue;
                if (dynamicVars.Count >= RitsuDebugCardActions.MaxDynamicVariableCount ||
                    value != decimal.Truncate(value) || value is < 0 or > RitsuDebugCardActions.MaxCardEditValue)
                {
                    skipped++;
                    continue;
                }

                dynamicVars.Add(key, decimal.ToInt32(value));
            }

            var localKeywords = card.GetKeywordsWithSources(KeywordSources.Local);
            var baseCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
            int? savedCost = null;
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (!card.EnergyCost.CostsX && baseCost is >= 0 and <= RitsuDebugCardActions.MaxCardEditValue)
                savedCost = baseCost;
            else if (!card.EnergyCost.CostsX && baseCost >= 0)
                skipped++;
            int? replay = null;
            if (card.BaseReplayCount is >= 0 and <= RitsuDebugCardActions.MaxReplayCount)
                replay = card.BaseReplayCount;
            else
                skipped++;
            return new()
            {
                CardId = card.Id.ToString(),
                UpgradeLevels = card.CurrentUpgradeLevel,
                BaseCost = savedCost,
                ReplayCount = replay,
                DynamicVars = dynamicVars.Count == 0 ? null : dynamicVars,
                Exhaust = localKeywords.Contains(CardKeyword.Exhaust),
                Ethereal = localKeywords.Contains(CardKeyword.Ethereal),
                Unplayable = localKeywords.Contains(CardKeyword.Unplayable),
                EnchantmentId = card.Enchantment?.Id.ToString(),
                EnchantmentAmount = card.Enchantment?.Amount,
            };
        }

        private static bool HaveSameState(
            RitsuDebugStatePresetCard left,
            RitsuDebugStatePresetCard right)
        {
            return left.CardId.Equals(right.CardId, StringComparison.Ordinal) &&
                   left.UpgradeLevels == right.UpgradeLevels && left.BaseCost == right.BaseCost &&
                   left.ReplayCount == right.ReplayCount && left.Exhaust == right.Exhaust &&
                   left.Ethereal == right.Ethereal && left.Unplayable == right.Unplayable &&
                   left.EnchantmentId == right.EnchantmentId &&
                   left.EnchantmentAmount == right.EnchantmentAmount &&
                   DictionariesEqual(left.DynamicVars, right.DynamicVars);
        }

        private static bool DictionariesEqual(
            IReadOnlyDictionary<string, int>? left,
            IReadOnlyDictionary<string, int>? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Count != right.Count)
                return false;
            return left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);
        }

        private static RitsuDebugStatePresetCaptureScope ScopeForPile(PileType pileType)
        {
            return pileType switch
            {
                PileType.Hand => RitsuDebugStatePresetCaptureScope.Hand,
                PileType.Draw => RitsuDebugStatePresetCaptureScope.Draw,
                PileType.Discard => RitsuDebugStatePresetCaptureScope.Discard,
                PileType.Exhaust => RitsuDebugStatePresetCaptureScope.Exhaust,
                _ => RitsuDebugStatePresetCaptureScope.None,
            };
        }
    }
}
