using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugCardEditField
    {
        Cost,
        Exhaust,
        Ethereal,
        Unplayable,
        DynamicVar,
    }

    internal enum RitsuDebugCardPileOperation
    {
        Clear,
        Upgrade,
    }

    internal static class RitsuDebugCardActions
    {
        internal const string ModifyPileActionId = "cards.pile.modify";
        internal const string CreateCardActionId = "cards.create";
        internal const string CopyCardActionId = "cards.copy";
        internal const string MoveCardActionId = "cards.move";
        internal const string SetReplayCountActionId = "cards.set-replay";
        internal const string RemoveCardActionId = "cards.remove";
        internal const string EditCardActionId = "cards.edit";
        internal const string EnchantCardActionId = "cards.enchant";
        internal const string ClearCardEnchantmentActionId = "cards.enchantment.clear";
        internal const string UpgradeCardActionId = "cards.upgrade";
        internal const int MaxReplayCount = 99;
        internal const int MaxBulkUpgradeLevels = 99;
        internal const int MaxCopyCount = 100;
        internal const int MaxCreateCount = 100;
        internal const int MaxCardEditValue = 999_999;
        internal const int MaxDynamicVariableCount = 64;

        private static readonly PileType[] MutablePileTypes =
            [PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Deck];

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModifyPilePayload>(
                ModifyPileActionId,
                ValidateModifyPile,
                ExecuteModifyPileAsync);
            RitsuDebugActionProtocol.Register<CreateCardPayload>(
                CreateCardActionId,
                ValidateCreateCard,
                ExecuteCreateCardAsync);
            RitsuDebugActionProtocol.Register<CopyCardPayload>(
                CopyCardActionId,
                ValidateCopyCard,
                ExecuteCopyCardAsync);
            RitsuDebugActionProtocol.Register<MoveCardPayload>(
                MoveCardActionId,
                ValidateMoveCard,
                ExecuteMoveCardAsync);
            RitsuDebugActionProtocol.Register<SetReplayCountPayload>(
                SetReplayCountActionId,
                ValidateSetReplayCount,
                ExecuteSetReplayCountAsync);
            RitsuDebugActionProtocol.Register<CardLocationPayload>(
                RemoveCardActionId,
                ValidateCardLocation,
                ExecuteRemoveCardAsync);
            RitsuDebugActionProtocol.Register<EditCardPayload>(
                EditCardActionId,
                ValidateEditCard,
                ExecuteEditCardAsync);
            RitsuDebugActionProtocol.Register<EnchantCardPayload>(
                EnchantCardActionId,
                ValidateEnchantCard,
                ExecuteEnchantCardAsync);
            RitsuDebugActionProtocol.Register<CardLocationPayload>(
                ClearCardEnchantmentActionId,
                ValidateClearCardEnchantment,
                ExecuteClearCardEnchantmentAsync);
            RitsuDebugActionProtocol.Register<UpgradeCardPayload>(
                UpgradeCardActionId,
                ValidateUpgradeCard,
                ExecuteUpgradeCardAsync);
        }

        internal static RitsuDebugActionSubmission SubmitModifyPile(
            Player requester,
            Player target,
            PileType pileType,
            RitsuDebugCardPileOperation operation,
            int levels = 0)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ModifyPileActionId,
                requester,
                target,
                new ModifyPilePayload(pileType.ToString(), operation, levels));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitCopyCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            PileType destinationPile,
            int count,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                CopyCardActionId,
                requester,
                target,
                new CopyCardPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    destinationPile.ToString(),
                    count));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitMoveCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            PileType destinationPile,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                MoveCardActionId,
                requester,
                target,
                new MoveCardPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    destinationPile.ToString()));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitCreateCard(
            Player requester,
            Player target,
            string cardId,
            PileType pileType,
            int count,
            int upgradeLevels,
            CardStatePayload state)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                CreateCardActionId,
                requester,
                target,
                new CreateCardPayload(cardId, pileType.ToString(), count, upgradeLevels, state));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitSetReplayCount(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            int replayCount,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                SetReplayCountActionId,
                requester,
                target,
                new SetReplayCountPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    replayCount));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitRemoveCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                RemoveCardActionId,
                requester,
                target,
                CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEditCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            RitsuDebugCardEditField field,
            int value,
            string? dynamicVarKey = null,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EditCardActionId,
                requester,
                target,
                new EditCardPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    field,
                    value,
                    dynamicVarKey));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEnchantCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            string enchantmentId,
            int amount,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EnchantCardActionId,
                requester,
                target,
                new EnchantCardPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    enchantmentId,
                    amount));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitClearCardEnchantment(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ClearCardEnchantmentActionId,
                requester,
                target,
                CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitUpgradeCard(
            Player requester,
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            int levels,
            uint? combatCardId = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                UpgradeCardActionId,
                requester,
                target,
                new UpgradeCardPayload(
                    CreateCardLocationPayload(target, pileType, cardIndex, expectedCardId, combatCardId),
                    levels));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static bool TryResolveEnchantment(
            string input,
            out EnchantmentModel enchantment,
            out RitsuDebugActionFeedback feedback)
        {
            enchantment = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "model.enchantmentIdInvalid",
                    "The enchantment ID is empty or too long.");
                return false;
            }

            var full = ModelDb.DebugEnchantments
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = full.Length > 0
                ? full
                :
                [
                    .. ModelDb.DebugEnchantments
                        .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                        .Take(2),
                ];
            if (matches.Length == 1)
            {
                enchantment = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create(
                    "model.enchantmentUnknown",
                    "Unknown enchantment '{0}'.",
                    input)
                : RitsuDebugActionFeedback.Create(
                    "model.enchantmentAmbiguous",
                    "The enchantment ID '{0}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        internal static bool TryParseMutablePileType(string input, out PileType pileType)
        {
            return Enum.TryParse(input, true, out pileType) && MutablePileTypes.Contains(pileType);
        }

        internal static string[] GetMutablePileNames()
        {
            return [.. MutablePileTypes.Select(static pileType => pileType.ToString())];
        }

        internal static CardPile? GetPile(Player player, PileType pileType)
        {
            return MutablePileTypes.Contains(pileType) ? CardPile.Get(pileType, player) : null;
        }

        internal static uint? GetCombatCardId(CardModel card)
        {
            if (!(card.Pile?.IsCombatPile ?? false) || !card.IsMutable)
                return null;

            return NetCombatCardDb.Instance.TryGetCardId(card, out var cardId) ? cardId : null;
        }

        internal static bool TryResolveCanonicalCard(
            string cardId,
            out CardModel card,
            out RitsuDebugActionFeedback feedback)
        {
            card = null!;
            if (string.IsNullOrWhiteSpace(cardId) || cardId.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "model.cardIdInvalid",
                    "The card ID is empty or too long.");
                return false;
            }

            var fullMatches = ModelDb.AllCards
                .Where(candidate => candidate.Id.ToString().Equals(cardId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (fullMatches.Length == 1)
            {
                card = fullMatches[0];
                feedback = default;
                return true;
            }

            var entryMatches = ModelDb.AllCards
                .Where(candidate => candidate.Id.Entry.Equals(cardId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (entryMatches.Length == 1)
            {
                card = entryMatches[0];
                feedback = default;
                return true;
            }

            feedback = entryMatches.Length > 1 || fullMatches.Length > 1
                ? RitsuDebugActionFeedback.Create(
                    "model.cardAmbiguous",
                    "Card ID '{0}' is ambiguous; use the full model ID.",
                    cardId)
                : RitsuDebugActionFeedback.Create(
                    "model.cardUnknown",
                    "Card '{0}' was not found.",
                    cardId);
            return false;
        }

        internal static RitsuDebugActionCheck ValidateModifyPile(
            RitsuDebugActionContext context,
            ModifyPilePayload payload)
        {
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail(
                    "card.invalidPileOperation",
                    "The card-pile operation is invalid.");
            if (!TryParseMutablePileType(payload.Pile, out var pileType))
                return RitsuDebugActionCheck.Fail(
                    "card.unsupportedPile",
                    "Unsupported pile '{0}'.",
                    payload.Pile);
            if (pileType != PileType.Deck && !TryRequireActiveCombat(context.Target, out var combatFeedback))
                return RitsuDebugActionCheck.Fail(combatFeedback);
            if (GetPile(context.Target, pileType) == null)
                return RitsuDebugActionCheck.Fail(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    pileType);

            return payload.Operation switch
            {
                RitsuDebugCardPileOperation.Clear when payload.Levels == 0 => RitsuDebugActionCheck.Ok,
                RitsuDebugCardPileOperation.Clear =>
                    RitsuDebugActionCheck.Fail(
                        "card.clearPileUpgradeLevels",
                        "Clearing a card pile does not accept upgrade levels."),
                RitsuDebugCardPileOperation.Upgrade when payload.Levels is >= 1 and <= MaxBulkUpgradeLevels =>
                    RitsuDebugActionCheck.Ok,
                RitsuDebugCardPileOperation.Upgrade => RitsuDebugActionCheck.Fail(
                    "card.upgradeLevelsRange",
                    "Upgrade levels must be between 1 and {0}.",
                    MaxBulkUpgradeLevels),
                _ => RitsuDebugActionCheck.Fail(
                    "card.invalidPileOperation",
                    "The card-pile operation is invalid."),
            };
        }

        internal static async Task<string> ExecuteModifyPileAsync(
            RitsuDebugActionContext context,
            ModifyPilePayload payload)
        {
            _ = TryParseMutablePileType(payload.Pile, out var pileType);
            var pile = GetPile(context.Target, pileType)!;
            var cards = pile.Cards.ToArray();
            if (payload.Operation == RitsuDebugCardPileOperation.Clear)
            {
                if (pileType == PileType.Deck)
                {
                    await CardPileCmd.RemoveFromDeck(cards, false);
                }
                else
                {
                    await CardPileCmd.RemoveFromCombat(cards, true);
                    NotifySilentPileRemovals(pile, cards.Length);
                }

                return cards.Length == 1
                    ? $"Removed 1 card from {pileType}."
                    : $"Removed {cards.Length} cards from {pileType}.";
            }

            var upgradedCards = 0;
            foreach (var card in cards)
            {
                var initialLevel = card.CurrentUpgradeLevel;
                ApplyAvailableUpgradeLevels(card, payload.Levels);
                if (card.CurrentUpgradeLevel <= initialLevel)
                    continue;
                upgradedCards++;
                RefreshVisibleCardNode(card, pileType);
            }

            return upgradedCards == 1
                ? $"Upgraded 1 card in {pileType}."
                : $"Upgraded {upgradedCards} cards in {pileType}.";
        }

        private static void NotifySilentPileRemovals(CardPile pile, int removedCardCount)
        {
            if (removedCardCount == 0)
                return;
            pile.InvokeContentsChanged();
            for (var index = 0; index < removedCardCount; index++)
                pile.InvokeCardRemoveFinished();
        }

        internal static RitsuDebugActionCheck ValidateCreateCard(
            RitsuDebugActionContext context,
            CreateCardPayload payload)
        {
            if (!TryParseMutablePileType(payload.Pile, out var pileType))
                return RitsuDebugActionCheck.Fail(
                    "card.unsupportedPile",
                    "Unsupported pile '{0}'.",
                    payload.Pile);

            if (!TryResolveCanonicalCard(payload.CardId, out var canonical, out var cardFeedback))
                return RitsuDebugActionCheck.Fail(cardFeedback);

            if (payload.Count is < 1 or > MaxCreateCount)
                return RitsuDebugActionCheck.Fail(
                    "card.createCountRange",
                    "Card count must be between 1 and {0}.",
                    MaxCreateCount);

            if (payload.UpgradeLevels < 0 || payload.UpgradeLevels > canonical.MaxUpgradeLevel)
                return RitsuDebugActionCheck.Fail(
                    "card.createUpgradeRange",
                    "Upgrade levels for {0} must be between 0 and {1}.",
                    canonical.Id,
                    canonical.MaxUpgradeLevel);

            var preview = canonical.ToMutable();
            ApplyUpgradeLevels(preview, payload.UpgradeLevels);
            var stateCheck = ValidateCardState(preview, payload.State);
            if (!stateCheck.Success)
                return stateCheck;

            if (pileType != PileType.Deck && !TryRequireActiveCombat(context.Target, out var combatFeedback))
                return RitsuDebugActionCheck.Fail(combatFeedback);

            var pile = GetPile(context.Target, pileType);
            if (pile == null)
                return RitsuDebugActionCheck.Fail(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    pileType);
            return RitsuDebugActionCheck.Ok;
        }

        internal static async Task<string> ExecuteCreateCardAsync(
            RitsuDebugActionContext context,
            CreateCardPayload payload)
        {
            return await ExecuteCreateCardAsync(context, payload, true);
        }

        internal static async Task<string> ExecuteCreateCardAsync(
            RitsuDebugActionContext context,
            CreateCardPayload payload,
            bool previewDeckAdds)
        {
            _ = TryParseMutablePileType(payload.Pile, out var pileType);
            _ = TryResolveCanonicalCard(payload.CardId, out var canonical, out _);

            if (pileType == PileType.Deck)
            {
                for (var index = 0; index < payload.Count; index++)
                {
                    var deckCard = context.Target.RunState.CreateCard(canonical, context.Target);
                    ApplyUpgradeLevels(deckCard, payload.UpgradeLevels);
                    ApplyCardState(deckCard, payload.State);
                    var result = await CardPileCmd.Add(deckCard, PileType.Deck);
                    if (!result.success)
                        throw new RitsuDebugActionExecutionException(
                            RitsuDebugActionFeedback.Create(
                                "card.addToDeckFailed",
                                "The game did not add {0} to the deck.",
                                canonical.Id));

                    if (previewDeckAdds)
                        CardCmd.PreviewCardPileAdd(result);
                }

                return payload.Count == 1
                    ? $"Created {canonical.Id} in the deck."
                    : $"Created {payload.Count} copies of {canonical.Id} in the deck.";
            }

            var combatState = context.Target.Creature.CombatState!;
            var actualPiles = new HashSet<PileType>();
            for (var index = 0; index < payload.Count; index++)
            {
                var combatCard = combatState.CreateCard(canonical, context.Target);
                ApplyUpgradeLevels(combatCard, payload.UpgradeLevels);
                ApplyCardState(combatCard, payload.State);
                var addResult = await CardPileCmd.AddGeneratedCardToCombat(
                    combatCard,
                    pileType,
                    context.Target);
                if (!addResult.success)
                    throw new RitsuDebugActionExecutionException(
                        RitsuDebugActionFeedback.Create(
                            "card.addToPileFailed",
                            "The game did not add {0} to {1}.",
                            canonical.Id,
                            pileType));

                var actualPile = combatCard.Pile?.Type ?? pileType;
                actualPiles.Add(actualPile);
                if (actualPile is PileType.Draw or PileType.Discard or PileType.Exhaust)
                    combatCard.Pile?.InvokeCardAddFinished();
            }

            return actualPiles.Count == 1 && actualPiles.Contains(pileType)
                ? $"Created {payload.Count} {canonical.Id} card(s) in {pileType}."
                : $"Created {payload.Count} {canonical.Id} card(s); game pile rules selected their final placement.";
        }

        internal static RitsuDebugActionCheck ValidateCardState(CardModel card, CardStatePayload state)
        {
            if (state.BaseCost is < 0 or > MaxCardEditValue ||
                state.ReplayCount is < 0 or > MaxReplayCount)
                return RitsuDebugActionCheck.Fail(
                    "card.stateValueRange",
                    "Card state values are outside the supported range.");
            if (state.BaseCost.HasValue && card.EnergyCost.CostsX)
                return RitsuDebugActionCheck.Fail(
                    "card.xCostCannotReplace",
                    "The base cost of an X-cost card cannot be replaced.");

            if (state.DynamicVars is { Count: > MaxDynamicVariableCount })
                return RitsuDebugActionCheck.Fail(
                    "card.dynamicVarLimit",
                    "A card state can change at most {0} dynamic variables.",
                    MaxDynamicVariableCount);
            if (state.DynamicVars != null)
                foreach (var (key, value) in state.DynamicVars)
                {
                    if (string.IsNullOrWhiteSpace(key) || key.Length > 64)
                        return RitsuDebugActionCheck.Fail(
                            "card.dynamicVarKeyRequired",
                            "A valid dynamic-variable key is required.");
                    if (value is < 0 or > MaxCardEditValue)
                        return RitsuDebugActionCheck.Fail(
                            "card.editValueRange",
                            "Card edit values must be between 0 and 999999.");
                    if (!card.DynamicVars.ContainsKey(key))
                        return RitsuDebugActionCheck.Fail(
                            "card.dynamicVarMissing",
                            "Card {0} has no dynamic variable named '{1}'.",
                            card.Id,
                            key);
                }

            if (state.EnchantmentId == null)
                return state.EnchantmentAmount == null
                    ? RitsuDebugActionCheck.Ok
                    : RitsuDebugActionCheck.Fail(
                        "card.enchantmentUnexpectedAmount",
                        "An enchantment amount requires an enchantment.");
            if (state.EnchantmentAmount is null or < 1 or > MaxCardEditValue)
                return RitsuDebugActionCheck.Fail(
                    "card.enchantmentAmountRange",
                    "Enchantment amount must be between 1 and 999999.");
            if (!TryResolveEnchantment(state.EnchantmentId, out var enchantment, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            var preview = (CardModel)card.ClonePreservingMutability();
            ApplyCardStateWithoutEnchantment(preview, state);
            CardCmd.ClearEnchantment(preview);
            return enchantment.CanEnchant(preview)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "card.enchantmentIncompatible",
                    "Enchantment {0} cannot be applied to {1}.",
                    enchantment.Id,
                    card.Id);
        }

        internal static void ApplyCardState(CardModel card, CardStatePayload state)
        {
            ApplyCardStateWithoutEnchantment(card, state);
            if (state.EnchantmentId == null)
                return;

            _ = TryResolveEnchantment(state.EnchantmentId, out var enchantment, out _);
            CardCmd.ClearEnchantment(card);
            if (CardCmd.Enchant(enchantment.ToMutable(), card, state.EnchantmentAmount!.Value) == null)
                throw new RitsuDebugActionExecutionException(
                    RitsuDebugActionFeedback.Create(
                        "card.enchantmentApplyFailed",
                        "The game did not apply enchantment {0} to card {1}.",
                        enchantment.Id,
                        card.Id));
        }

        private static void ApplyCardStateWithoutEnchantment(CardModel card, CardStatePayload state)
        {
            if (state.BaseCost.HasValue)
                card.EnergyCost.SetCustomBaseCost(state.BaseCost.Value);
            if (state.ReplayCount.HasValue)
                card.BaseReplayCount = state.ReplayCount.Value;
            if (state.DynamicVars != null)
                foreach (var (key, value) in state.DynamicVars)
                    SetDynamicVar(card, key, value);
            if (state.Exhaust.HasValue)
                SetKeyword(card, CardKeyword.Exhaust, state.Exhaust.Value);
            if (state.Ethereal.HasValue)
                SetKeyword(card, CardKeyword.Ethereal, state.Ethereal.Value);
            if (state.Unplayable.HasValue)
                SetKeyword(card, CardKeyword.Unplayable, state.Unplayable.Value);
        }

        private static RitsuDebugActionCheck ValidateCopyCard(
            RitsuDebugActionContext context,
            CopyCardPayload payload)
        {
            if (payload.Count is < 1 or > MaxCopyCount)
                return RitsuDebugActionCheck.Fail(
                    "card.copyCountRange",
                    "Copy count must be between 1 and {0}.",
                    MaxCopyCount);
            if (!TryGetLocatedCard(context.Target, payload.Location, out _, out _, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (!TryParseMutablePileType(payload.DestinationPile, out var destinationPile))
                return RitsuDebugActionCheck.Fail(
                    "card.unsupportedDestinationPile",
                    "Unsupported destination pile '{0}'.",
                    payload.DestinationPile);
            if (destinationPile != PileType.Deck &&
                !TryRequireActiveCombat(context.Target, out var combatFeedback))
                return RitsuDebugActionCheck.Fail(combatFeedback);

            var destination = GetPile(context.Target, destinationPile);
            if (destination == null)
                return RitsuDebugActionCheck.Fail(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    destinationPile);

            return RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteCopyCardAsync(
            RitsuDebugActionContext context,
            CopyCardPayload payload)
        {
            _ = TryGetLocatedCard(context.Target, payload.Location, out _, out var source, out _);
            _ = TryParseMutablePileType(payload.DestinationPile, out var destinationPile);
            var copies = new CardModel[payload.Count];
            for (var index = 0; index < copies.Length; index++)
                copies[index] = destinationPile == PileType.Deck
                    ? context.Target.RunState.CloneCard(source)
                    : source.Pile?.IsCombatPile == true
                        ? source.CreateClone()
                        : context.Target.Creature.CombatState!.CloneCard(source);

            IReadOnlyList<CardPileAddResult> results;
            if (destinationPile == PileType.Deck)
            {
                results = await CardPileCmd.Add(copies, PileType.Deck);
                CardCmd.PreviewCardPileAdd(results);
            }
            else
            {
                results = await CardPileCmd.AddGeneratedCardsToCombat(copies, destinationPile, context.Target);
            }

            var actualPiles = new List<PileType>(payload.Count);
            for (var index = 0; index < copies.Length; index++)
            {
                if (index < results.Count && results[index] is { success: true } result)
                {
                    var actualPile = result.cardAdded.Pile?.Type ?? destinationPile;
                    actualPiles.Add(actualPile);
                    if (actualPile is PileType.Draw or PileType.Discard or PileType.Exhaust)
                        result.cardAdded.Pile?.InvokeCardAddFinished();
                    continue;
                }

                copies[index].RemoveFromState();
            }

            switch (actualPiles.Count)
            {
                case 0:
                    throw new RitsuDebugActionExecutionException(
                        RitsuDebugActionFeedback.Create(
                            "card.copyFailed",
                            "The card could not be copied to {0}.",
                            destinationPile));
                case 1:
                    return $"Copied {source.Id} to {actualPiles[0]}.";
            }

            var placement = string.Join(", ", actualPiles
                .GroupBy(static pile => pile)
                .OrderBy(static group => group.Key)
                .Select(static group => $"{group.Key} {group.Count()}"));
            return actualPiles.Count == payload.Count
                ? $"Copied {source.Id} {payload.Count} times: {placement}."
                : $"Copied {actualPiles.Count} of {payload.Count} requested {source.Id} cards: {placement}.";
        }

        private static RitsuDebugActionCheck ValidateMoveCard(
            RitsuDebugActionContext context,
            MoveCardPayload payload)
        {
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out var sourcePile,
                    out _,
                    out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (!TryParseMutablePileType(payload.DestinationPile, out var destinationPile) ||
                !destinationPile.IsCombatPile())
                return RitsuDebugActionCheck.Fail(
                    "card.moveCombatOnly",
                    "Cards can be moved only between combat piles; use Copy to cross the deck boundary.");
            if (!sourcePile.IsCombatPile())
                return RitsuDebugActionCheck.Fail(
                    "card.deckMoveRequiresCopy",
                    "Deck cards cannot be moved directly into combat; copy the card instead.");
            if (sourcePile == destinationPile)
                return RitsuDebugActionCheck.Fail(
                    "card.alreadyInPile",
                    "The card is already in the selected pile.");

            var destination = GetPile(context.Target, destinationPile);
            if (destination == null)
                return RitsuDebugActionCheck.Fail(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    destinationPile);
            return RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteMoveCardAsync(
            RitsuDebugActionContext context,
            MoveCardPayload payload)
        {
            _ = TryGetLocatedCard(context.Target, payload.Location, out var sourcePile, out var card, out _);
            _ = TryParseMutablePileType(payload.DestinationPile, out var destinationPile);
            var result = await CardPileCmd.Add(card, destinationPile);
            if (!result.success)
                throw new RitsuDebugActionExecutionException(
                    RitsuDebugActionFeedback.Create(
                        "card.moveFailed",
                        "The card could not be moved to {0}.",
                        destinationPile));

            var actualPile = card.Pile?.Type ?? destinationPile;
            return actualPile == destinationPile
                ? $"Moved {card.Id} from {sourcePile} to {actualPile}."
                : $"Moved {card.Id} from {sourcePile} to {actualPile} because " +
                  $"{destinationPile} could not accept another card.";
        }

        private static RitsuDebugActionCheck ValidateSetReplayCount(
            RitsuDebugActionContext context,
            SetReplayCountPayload payload)
        {
            if (payload.ReplayCount is < 0 or > MaxReplayCount)
                return RitsuDebugActionCheck.Fail(
                    "card.replayRange",
                    "Replay count must be between 0 and {0}.",
                    MaxReplayCount);
            return TryGetLocatedCard(context.Target, payload.Location, out _, out _, out var feedback)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(feedback);
        }

        private static Task<string> ExecuteSetReplayCountAsync(
            RitsuDebugActionContext context,
            SetReplayCountPayload payload)
        {
            _ = TryGetLocatedCard(context.Target, payload.Location, out var pileType, out var card, out _);
            card.BaseReplayCount = payload.ReplayCount;
            RefreshVisibleCardNode(card, pileType);
            return Task.FromResult(
                $"Set {card.Id} in {pileType} to Replay {payload.ReplayCount}.");
        }

        private static RitsuDebugActionCheck ValidateCardLocation(
            RitsuDebugActionContext context,
            CardLocationPayload payload)
        {
            if (!TryGetLocatedCard(context.Target, payload, out _, out _, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            return RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteRemoveCardAsync(
            RitsuDebugActionContext context,
            CardLocationPayload payload)
        {
            _ = TryGetLocatedCard(context.Target, payload, out var pileType, out var card, out _);
            if (pileType == PileType.Deck)
                await CardPileCmd.RemoveFromDeck(card);
            else
                await CardPileCmd.RemoveFromCombat(card);
            return $"Removed {card.Id} from {pileType}.";
        }

        private static RitsuDebugActionCheck ValidateEditCard(
            RitsuDebugActionContext context,
            EditCardPayload payload)
        {
            if (!Enum.IsDefined(payload.Field))
                return RitsuDebugActionCheck.Fail(
                    "card.invalidEditField",
                    "The card edit field is invalid.");
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out _,
                    out var card,
                    out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            if (payload.Value is < 0 or > 999_999)
                return RitsuDebugActionCheck.Fail(
                    "card.editValueRange",
                    "Card edit values must be between 0 and 999999.");
            switch (payload.Field)
            {
                case RitsuDebugCardEditField.Exhaust or
                    RitsuDebugCardEditField.Ethereal or
                    RitsuDebugCardEditField.Unplayable when payload.Value is not (0 or 1):
                    return RitsuDebugActionCheck.Fail(
                        "card.flagValue",
                        "Card flag values must be 0 or 1.");
                case RitsuDebugCardEditField.Cost when card.EnergyCost.CostsX:
                    return RitsuDebugActionCheck.Fail(
                        "card.xCostCannotReplace",
                        "The base cost of an X-cost card cannot be replaced.");
                case RitsuDebugCardEditField.DynamicVar:
                    if (string.IsNullOrWhiteSpace(payload.DynamicVarKey) || payload.DynamicVarKey.Length > 64)
                        return RitsuDebugActionCheck.Fail(
                            "card.dynamicVarKeyRequired",
                            "A valid dynamic-variable key is required.");
                    if (!card.DynamicVars.ContainsKey(payload.DynamicVarKey))
                        return RitsuDebugActionCheck.Fail(
                            "card.dynamicVarMissing",
                            "Card {0} has no dynamic variable named '{1}'.",
                            card.Id,
                            payload.DynamicVarKey);
                    break;
                default:
                    if (payload.DynamicVarKey != null)
                        return RitsuDebugActionCheck.Fail(
                            "card.dynamicVarUnexpected",
                            "A dynamic-variable key is valid only for DynamicVar edits.");
                    break;
            }

            return RitsuDebugActionCheck.Ok;
        }

        private static Task<string> ExecuteEditCardAsync(
            RitsuDebugActionContext context,
            EditCardPayload payload)
        {
            _ = TryGetLocatedCard(
                context.Target,
                payload.Location,
                out var pileType,
                out var card,
                out _);
            switch (payload.Field)
            {
                case RitsuDebugCardEditField.Cost:
                    card.EnergyCost.SetCustomBaseCost(payload.Value);
                    break;
                case RitsuDebugCardEditField.Exhaust:
                    SetKeyword(card, CardKeyword.Exhaust, payload.Value != 0);
                    break;
                case RitsuDebugCardEditField.Ethereal:
                    SetKeyword(card, CardKeyword.Ethereal, payload.Value != 0);
                    break;
                case RitsuDebugCardEditField.Unplayable:
                    SetKeyword(card, CardKeyword.Unplayable, payload.Value != 0);
                    break;
                case RitsuDebugCardEditField.DynamicVar:
                    SetDynamicVar(card, payload.DynamicVarKey!, payload.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(payload.Field));
            }

            RefreshVisibleCardNode(card, pileType);

            return Task.FromResult(
                $"Set {payload.Field} on {card.Id} in {pileType} to {payload.Value}.");
        }

        private static RitsuDebugActionCheck ValidateEnchantCard(
            RitsuDebugActionContext context,
            EnchantCardPayload payload)
        {
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out _,
                    out var card,
                    out var feedback) ||
                !TryResolveEnchantment(payload.EnchantmentId, out var enchantment, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (payload.Amount is < 1 or > 999_999)
                return RitsuDebugActionCheck.Fail(
                    "card.enchantmentAmountRange",
                    "Enchantment amount must be between 1 and 999999.");
            var preview = (CardModel)card.ClonePreservingMutability();
            CardCmd.ClearEnchantment(preview);
            return enchantment.CanEnchant(preview)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "card.enchantmentIncompatible",
                    "Enchantment {0} cannot be applied to {1}.",
                    enchantment.Id,
                    card.Id);
        }

        private static Task<string> ExecuteEnchantCardAsync(
            RitsuDebugActionContext context,
            EnchantCardPayload payload)
        {
            _ = TryGetLocatedCard(
                context.Target,
                payload.Location,
                out var pileType,
                out var card,
                out _);
            _ = TryResolveEnchantment(payload.EnchantmentId, out var enchantment, out _);
            var previousEnchantment = card.Enchantment?.CanonicalInstance;
            var previousAmount = card.Enchantment?.Amount ?? 0;
            CardCmd.ClearEnchantment(card);
            try
            {
                if (CardCmd.Enchant(enchantment.ToMutable(), card, payload.Amount) == null)
                    throw new RitsuDebugActionExecutionException(
                        RitsuDebugActionFeedback.Create(
                            "card.enchantmentApplyFailed",
                            "The game did not apply enchantment {0} to card {1}.",
                            enchantment.Id,
                            card.Id));
            }
            catch (Exception applyException) when (RitsuLibExceptionPolicy.IsRecoverable(applyException))
            {
                if (previousEnchantment != null)
                    try
                    {
                        if (CardCmd.Enchant(previousEnchantment.ToMutable(), card, previousAmount) == null)
                            throw new RitsuDebugActionExecutionException(
                                RitsuDebugActionFeedback.Create(
                                    "card.enchantmentRestoreFailed",
                                    "The previous enchantment {0} could not be restored to card {1}.",
                                    previousEnchantment.Id,
                                    card.Id));
                    }
                    catch (Exception restoreException) when (RitsuLibExceptionPolicy.IsRecoverable(restoreException))
                    {
                        throw new AggregateException(
                            "The new enchantment failed and the previous enchantment could not be restored.",
                            applyException,
                            restoreException);
                    }

                throw;
            }

            RefreshVisibleCardNode(card, pileType);

            return Task.FromResult(
                $"Set {card.Id} in {pileType} to enchantment {enchantment.Id} " +
                $"with amount {payload.Amount}.");
        }

        private static RitsuDebugActionCheck ValidateClearCardEnchantment(
            RitsuDebugActionContext context,
            CardLocationPayload payload)
        {
            if (!TryGetLocatedCard(context.Target, payload, out _, out var card, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return card.Enchantment != null
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "card.noEnchantment",
                    "Card {0} has no enchantment.",
                    card.Id);
        }

        private static Task<string> ExecuteClearCardEnchantmentAsync(
            RitsuDebugActionContext context,
            CardLocationPayload payload)
        {
            _ = TryGetLocatedCard(context.Target, payload, out var pileType, out var card, out _);
            var enchantmentId = card.Enchantment!.Id;
            CardCmd.ClearEnchantment(card);
            RefreshVisibleCardNode(card, pileType);
            return Task.FromResult(
                $"Cleared enchantment {enchantmentId} from {card.Id} in {pileType}.");
        }

        private static RitsuDebugActionCheck ValidateUpgradeCard(
            RitsuDebugActionContext context,
            UpgradeCardPayload payload)
        {
            if (payload.Levels is < 1 or > MaxBulkUpgradeLevels)
                return RitsuDebugActionCheck.Fail(
                    "card.upgradeLevelsRange",
                    "Upgrade levels must be between 1 and {0}.",
                    MaxBulkUpgradeLevels);
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out _,
                    out var card,
                    out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return card.IsUpgradable
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "card.maximumUpgrade",
                    "Card {0} is already at its maximum upgrade level.",
                    card.Id);
        }

        private static Task<string> ExecuteUpgradeCardAsync(
            RitsuDebugActionContext context,
            UpgradeCardPayload payload)
        {
            _ = TryGetLocatedCard(
                context.Target,
                payload.Location,
                out var pileType,
                out var card,
                out _);
            var initialLevel = card.CurrentUpgradeLevel;
            for (var level = 0; level < payload.Levels && card.IsUpgradable; level++)
                CardCmd.Upgrade(card, CardPreviewStyle.None);
            RefreshVisibleCardNode(card, pileType);
            return Task.FromResult(
                $"Upgraded {card.Id} in {pileType} from {initialLevel} " +
                $"to {card.CurrentUpgradeLevel}.");
        }

        private static void RefreshVisibleCardNode(CardModel card, PileType pileType)
        {
            NCard.FindOnTable(card, pileType)?.UpdateVisuals(pileType, CardPreviewMode.Normal);
        }

        private static bool TryGetLocatedCard(
            Player target,
            CardLocationPayload payload,
            out PileType pileType,
            out CardModel card,
            out RitsuDebugActionFeedback feedback)
        {
            card = null!;
            if (string.IsNullOrWhiteSpace(payload.ExpectedCardId) || payload.ExpectedCardId.Length > 128)
            {
                pileType = default;
                feedback = RitsuDebugActionFeedback.Create(
                    "card.selectedInvalid",
                    "The selected card is invalid.");
                return false;
            }

            if (!TryParseMutablePileType(payload.Pile, out pileType))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.unsupportedPile",
                    "Unsupported pile '{0}'.",
                    payload.Pile);
                return false;
            }

            if (pileType != PileType.Deck && !TryRequireActiveCombat(target, out feedback))
                return false;

            var pile = GetPile(target, pileType);
            if (pile == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    pileType);
                return false;
            }

            if (pileType != PileType.Deck)
            {
                if (!payload.CombatCardId.HasValue ||
                    !NetCombatCardDb.Instance.TryGetCard(payload.CombatCardId.Value, out var locatedCard) ||
                    locatedCard == null ||
                    locatedCard.Owner.NetId != target.NetId ||
                    !ReferenceEquals(locatedCard.Pile, pile) ||
                    !locatedCard.Id.ToString().Equals(payload.ExpectedCardId, StringComparison.Ordinal))
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "card.selectedMoved",
                        "The selected card moved or is no longer available.");
                    return false;
                }

                card = locatedCard;
                feedback = default;
                return true;
            }

            if (payload.CardIndex < 0 || payload.CardIndex >= pile.Cards.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.indexRange",
                    "Card index {0} is outside {1}'s range 0-{2}.",
                    payload.CardIndex,
                    pileType,
                    pile.Cards.Count - 1);
                return false;
            }

            card = pile.Cards[payload.CardIndex];
            if (!card.Id.ToString().Equals(payload.ExpectedCardId, StringComparison.Ordinal))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.slotChanged",
                    "The card at {0}[{1}] changed before the action could run.",
                    pileType,
                    payload.CardIndex);
                return false;
            }

            feedback = default;
            return true;
        }

        private static CardLocationPayload CreateCardLocationPayload(
            Player target,
            PileType pileType,
            int cardIndex,
            string expectedCardId,
            uint? combatCardId)
        {
            if (pileType != PileType.Deck && !combatCardId.HasValue)
            {
                var pile = GetPile(target, pileType);
                if (pile != null && cardIndex >= 0 && cardIndex < pile.Cards.Count)
                    combatCardId = GetCombatCardId(pile.Cards[cardIndex]);
            }

            return new(
                pileType.ToString(),
                cardIndex,
                expectedCardId,
                combatCardId);
        }

        private static void SetDynamicVar(CardModel card, string key, int value)
        {
            var dynamicVar = card.DynamicVars[key];
            dynamicVar.BaseValue = value;
            dynamicVar.ResetToBase();
            dynamicVar.PreviewValue = value;
        }

        private static void SetKeyword(CardModel card, CardKeyword keyword, bool enabled)
        {
            if (enabled)
                card.AddKeyword(keyword);
            else
                card.RemoveKeyword(keyword);
        }

        private static bool TryRequireActiveCombat(
            Player target,
            out RitsuDebugActionFeedback feedback)
        {
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                target.PlayerCombatState == null || target.Creature.CombatState == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "action.activeCombatRequired",
                    "This change requires an active combat.");
                return false;
            }

            feedback = default;
            return true;
        }

        private static void ApplyUpgradeLevels(CardModel card, int levels)
        {
            for (var level = 0; level < levels; level++)
                CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        internal static void ApplyAvailableUpgradeLevels(CardModel card, int levels)
        {
            for (var level = 0; level < levels && card.IsUpgradable; level++)
                CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        internal readonly record struct ModifyPilePayload(
            string Pile,
            RitsuDebugCardPileOperation Operation,
            int Levels);

        internal readonly record struct CreateCardPayload(
            string CardId,
            string Pile,
            int Count,
            int UpgradeLevels,
            CardStatePayload State);

        internal readonly record struct CardStatePayload(
            int? BaseCost,
            int? ReplayCount,
            Dictionary<string, int>? DynamicVars,
            bool? Exhaust,
            bool? Ethereal,
            bool? Unplayable,
            string? EnchantmentId,
            int? EnchantmentAmount);

        internal readonly record struct CopyCardPayload(
            CardLocationPayload Location,
            string DestinationPile,
            int Count);

        internal readonly record struct MoveCardPayload(
            CardLocationPayload Location,
            string DestinationPile);

        internal readonly record struct SetReplayCountPayload(
            CardLocationPayload Location,
            int ReplayCount);

        internal readonly record struct CardLocationPayload(
            string Pile,
            int CardIndex,
            string ExpectedCardId,
            uint? CombatCardId);

        internal readonly record struct EditCardPayload(
            CardLocationPayload Location,
            RitsuDebugCardEditField Field,
            int Value,
            string? DynamicVarKey);

        internal readonly record struct EnchantCardPayload(
            CardLocationPayload Location,
            string EnchantmentId,
            int Amount);

        internal readonly record struct UpgradeCardPayload(
            CardLocationPayload Location,
            int Levels);
    }
}
