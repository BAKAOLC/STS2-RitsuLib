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
        ExhaustOnNextPlay,
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
            int upgradeLevels)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                CreateCardActionId,
                requester,
                target,
                new CreateCardPayload(cardId, pileType.ToString(), upgradeLevels));
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
            out string error)
        {
            enchantment = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                error = "The enchantment ID is empty or too long.";
                return false;
            }

            var full = ModelDb.DebugEnchantments
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = full.Length > 0
                ? full
                : ModelDb.DebugEnchantments
                    .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
            if (matches.Length == 1)
            {
                enchantment = matches[0];
                error = string.Empty;
                return true;
            }

            error = matches.Length == 0
                ? $"Unknown enchantment '{input}'."
                : $"The enchantment ID '{input}' is ambiguous; use the full model ID.";
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
            out string error)
        {
            card = null!;
            if (string.IsNullOrWhiteSpace(cardId) || cardId.Length > 128)
            {
                error = "The card ID is empty or too long.";
                return false;
            }

            var fullMatches = ModelDb.AllCards
                .Where(candidate => candidate.Id.ToString().Equals(cardId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (fullMatches.Length == 1)
            {
                card = fullMatches[0];
                error = string.Empty;
                return true;
            }

            var entryMatches = ModelDb.AllCards
                .Where(candidate => candidate.Id.Entry.Equals(cardId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (entryMatches.Length == 1)
            {
                card = entryMatches[0];
                error = string.Empty;
                return true;
            }

            error = entryMatches.Length > 1 || fullMatches.Length > 1
                ? $"Card ID '{cardId}' is ambiguous; use the full model ID."
                : $"Card '{cardId}' was not found.";
            return false;
        }

        private static RitsuDebugActionCheck ValidateModifyPile(
            RitsuDebugActionContext context,
            ModifyPilePayload payload)
        {
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail("The card-pile operation is invalid.");
            if (!TryParseMutablePileType(payload.Pile, out var pileType))
                return RitsuDebugActionCheck.Fail($"Unsupported pile '{payload.Pile}'.");
            if (pileType != PileType.Deck && !TryRequireActiveCombat(context.Target, out var combatError))
                return RitsuDebugActionCheck.Fail(combatError);
            if (GetPile(context.Target, pileType) == null)
                return RitsuDebugActionCheck.Fail($"Pile '{pileType}' is unavailable for the selected player.");

            return payload.Operation switch
            {
                RitsuDebugCardPileOperation.Clear when payload.Levels == 0 => RitsuDebugActionCheck.Ok,
                RitsuDebugCardPileOperation.Clear =>
                    RitsuDebugActionCheck.Fail("Clearing a card pile does not accept upgrade levels."),
                RitsuDebugCardPileOperation.Upgrade when payload.Levels is >= 1 and <= MaxBulkUpgradeLevels =>
                    RitsuDebugActionCheck.Ok,
                RitsuDebugCardPileOperation.Upgrade => RitsuDebugActionCheck.Fail(
                    $"Upgrade levels must be between 1 and {MaxBulkUpgradeLevels}."),
                _ => RitsuDebugActionCheck.Fail("The card-pile operation is invalid."),
            };
        }

        private static async Task<string> ExecuteModifyPileAsync(
            RitsuDebugActionContext context,
            ModifyPilePayload payload)
        {
            _ = TryParseMutablePileType(payload.Pile, out var pileType);
            var cards = GetPile(context.Target, pileType)!.Cards.ToArray();
            if (payload.Operation == RitsuDebugCardPileOperation.Clear)
            {
                if (pileType == PileType.Deck)
                    await CardPileCmd.RemoveFromDeck(cards, false);
                else
                    await CardPileCmd.RemoveFromCombat(cards, true);

                return cards.Length == 1
                    ? $"Removed 1 card from {pileType}."
                    : $"Removed {cards.Length} cards from {pileType}.";
            }

            var upgradedCards = 0;
            foreach (var card in cards)
            {
                var initialLevel = card.CurrentUpgradeLevel;
                ApplyAvailableUpgradeLevels(card, payload.Levels);
                if (card.CurrentUpgradeLevel > initialLevel)
                {
                    upgradedCards++;
                    RefreshVisibleCardNode(card, pileType);
                }
            }

            return upgradedCards == 1
                ? $"Upgraded 1 card in {pileType}."
                : $"Upgraded {upgradedCards} cards in {pileType}.";
        }

        private static RitsuDebugActionCheck ValidateCreateCard(
            RitsuDebugActionContext context,
            CreateCardPayload payload)
        {
            if (!TryParseMutablePileType(payload.Pile, out var pileType))
                return RitsuDebugActionCheck.Fail(
                    $"Unsupported pile '{payload.Pile}'. Valid piles: {string.Join(", ", GetMutablePileNames())}.");

            if (!TryResolveCanonicalCard(payload.CardId, out var canonical, out var cardError))
                return RitsuDebugActionCheck.Fail(cardError);

            if (payload.UpgradeLevels < 0 || payload.UpgradeLevels > canonical.MaxUpgradeLevel)
                return RitsuDebugActionCheck.Fail(
                    $"Upgrade levels for {canonical.Id} must be between 0 and {canonical.MaxUpgradeLevel}.");

            if (pileType != PileType.Deck && !TryRequireActiveCombat(context.Target, out var combatError))
                return RitsuDebugActionCheck.Fail(combatError);

            var pile = GetPile(context.Target, pileType);
            if (pile == null)
                return RitsuDebugActionCheck.Fail($"Pile '{pileType}' is unavailable for the target player.");
            return RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteCreateCardAsync(
            RitsuDebugActionContext context,
            CreateCardPayload payload)
        {
            _ = TryParseMutablePileType(payload.Pile, out var pileType);
            _ = TryResolveCanonicalCard(payload.CardId, out var canonical, out _);

            if (pileType == PileType.Deck)
            {
                var deckCard = context.Target.RunState.CreateCard(canonical, context.Target);
                ApplyUpgradeLevels(deckCard, payload.UpgradeLevels);
                var result = await CardPileCmd.Add(deckCard, PileType.Deck);
                if (!result.success)
                    throw new RitsuDebugActionExecutionException(
                        $"The game did not add {canonical.Id} to the deck.");

                CardCmd.PreviewCardPileAdd(result);
                return $"Created {canonical.Id} in the deck at upgrade level {deckCard.CurrentUpgradeLevel}.";
            }

            var combatState = context.Target.Creature.CombatState!;
            var combatCard = combatState.CreateCard(canonical, context.Target);
            ApplyUpgradeLevels(combatCard, payload.UpgradeLevels);
            var addResult = await CardPileCmd.AddGeneratedCardToCombat(
                combatCard,
                pileType,
                context.Target);
            if (!addResult.success)
                throw new RitsuDebugActionExecutionException(
                    $"The game did not add {canonical.Id} to {pileType}.");

            var actualPile = combatCard.Pile?.Type ?? pileType;
            if (actualPile is PileType.Draw or PileType.Discard or PileType.Exhaust)
                combatCard.Pile?.InvokeCardAddFinished();

            return actualPile == pileType
                ? $"Created {canonical.Id} in {actualPile} at upgrade level {combatCard.CurrentUpgradeLevel}."
                : $"Created {canonical.Id} in {actualPile} because {pileType} could not accept another card.";
        }

        private static RitsuDebugActionCheck ValidateCopyCard(
            RitsuDebugActionContext context,
            CopyCardPayload payload)
        {
            if (payload.Count is < 1 or > MaxCopyCount)
                return RitsuDebugActionCheck.Fail($"Copy count must be between 1 and {MaxCopyCount}.");
            if (!TryGetLocatedCard(context.Target, payload.Location, out _, out _, out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!TryParseMutablePileType(payload.DestinationPile, out var destinationPile))
                return RitsuDebugActionCheck.Fail($"Unsupported destination pile '{payload.DestinationPile}'.");
            if (destinationPile != PileType.Deck &&
                !TryRequireActiveCombat(context.Target, out var combatError))
                return RitsuDebugActionCheck.Fail(combatError);

            var destination = GetPile(context.Target, destinationPile);
            if (destination == null)
                return RitsuDebugActionCheck.Fail(
                    $"Pile '{destinationPile}' is unavailable for the selected player.");

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

            if (actualPiles.Count == 0)
                throw new RitsuDebugActionExecutionException(
                    $"The card could not be copied to {destinationPile}.");

            if (actualPiles.Count == 1)
                return $"Copied {source.Id} to {actualPiles[0]}.";

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
                    out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!TryParseMutablePileType(payload.DestinationPile, out var destinationPile) ||
                !destinationPile.IsCombatPile())
                return RitsuDebugActionCheck.Fail(
                    "Cards can be moved only between combat piles; use Copy to cross the deck boundary.");
            if (!sourcePile.IsCombatPile())
                return RitsuDebugActionCheck.Fail(
                    "Deck cards cannot be moved directly into combat; copy the card instead.");
            if (sourcePile == destinationPile)
                return RitsuDebugActionCheck.Fail("The card is already in the selected pile.");

            var destination = GetPile(context.Target, destinationPile);
            if (destination == null)
                return RitsuDebugActionCheck.Fail(
                    $"Pile '{destinationPile}' is unavailable for the selected player.");
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
                    $"The card could not be moved to {destinationPile}.");

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
                    $"Replay count must be between 0 and {MaxReplayCount}.");
            return TryGetLocatedCard(context.Target, payload.Location, out _, out _, out var error)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(error);
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
            if (!TryGetLocatedCard(context.Target, payload, out _, out _, out var error))
                return RitsuDebugActionCheck.Fail(error);

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
                return RitsuDebugActionCheck.Fail("The card edit field is invalid.");
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out _,
                    out var card,
                    out var error))
                return RitsuDebugActionCheck.Fail(error);

            if (payload.Value is < 0 or > 999_999)
                return RitsuDebugActionCheck.Fail("Card edit values must be between 0 and 999999.");
            if (payload.Field is RitsuDebugCardEditField.Exhaust or
                    RitsuDebugCardEditField.Ethereal or
                    RitsuDebugCardEditField.Unplayable or
                    RitsuDebugCardEditField.ExhaustOnNextPlay && payload.Value is not (0 or 1))
                return RitsuDebugActionCheck.Fail("Card flag values must be 0 or 1.");
            if (payload.Field == RitsuDebugCardEditField.Cost && card.EnergyCost.CostsX)
                return RitsuDebugActionCheck.Fail("The base cost of an X-cost card cannot be replaced.");
            if (payload.Field == RitsuDebugCardEditField.DynamicVar)
            {
                if (string.IsNullOrWhiteSpace(payload.DynamicVarKey) || payload.DynamicVarKey.Length > 64)
                    return RitsuDebugActionCheck.Fail("A valid dynamic-variable key is required.");
                if (!card.DynamicVars.ContainsKey(payload.DynamicVarKey))
                    return RitsuDebugActionCheck.Fail(
                        $"Card {card.Id} has no dynamic variable named '{payload.DynamicVarKey}'.");
            }
            else if (payload.DynamicVarKey != null)
            {
                return RitsuDebugActionCheck.Fail("A dynamic-variable key is valid only for DynamicVar edits.");
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
                case RitsuDebugCardEditField.ExhaustOnNextPlay:
                    card.ExhaustOnNextPlay = payload.Value != 0;
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
                    out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!TryResolveEnchantment(payload.EnchantmentId, out var enchantment, out error))
                return RitsuDebugActionCheck.Fail(error);
            if (payload.Amount is < 1 or > 999_999)
                return RitsuDebugActionCheck.Fail("Enchantment amount must be between 1 and 999999.");
            var preview = (CardModel)card.ClonePreservingMutability();
            CardCmd.ClearEnchantment(preview);
            return enchantment.CanEnchant(preview)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"Enchantment {enchantment.Id} cannot be applied to {card.Id}.");
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
                        $"The game did not apply enchantment {enchantment.Id} to card {card.Id}.");
            }
            catch (Exception applyException) when (RitsuLibExceptionPolicy.IsRecoverable(applyException))
            {
                if (previousEnchantment != null)
                    try
                    {
                        if (CardCmd.Enchant(previousEnchantment.ToMutable(), card, previousAmount) == null)
                            throw new RitsuDebugActionExecutionException(
                                $"The previous enchantment {previousEnchantment.Id} could not be restored to " +
                                $"card {card.Id}.");
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
            if (!TryGetLocatedCard(context.Target, payload, out _, out var card, out var error))
                return RitsuDebugActionCheck.Fail(error);
            return card.Enchantment != null
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"Card {card.Id} has no enchantment.");
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
                    $"Upgrade levels must be between 1 and {MaxBulkUpgradeLevels}.");
            if (!TryGetLocatedCard(
                    context.Target,
                    payload.Location,
                    out _,
                    out var card,
                    out var error))
                return RitsuDebugActionCheck.Fail(error);
            return card.IsUpgradable
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"Card {card.Id} is already at its maximum upgrade level.");
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
            out string error)
        {
            card = null!;
            if (string.IsNullOrWhiteSpace(payload.ExpectedCardId) || payload.ExpectedCardId.Length > 128)
            {
                pileType = default;
                error = "The selected card is invalid.";
                return false;
            }

            if (!TryParseMutablePileType(payload.Pile, out pileType))
            {
                error = $"Unsupported pile '{payload.Pile}'.";
                return false;
            }

            if (pileType != PileType.Deck && !TryRequireActiveCombat(target, out error))
                return false;

            var pile = GetPile(target, pileType);
            if (pile == null)
            {
                error = $"Pile '{pileType}' is unavailable for the target player.";
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
                    error = "The selected card moved or is no longer available.";
                    return false;
                }

                card = locatedCard;
                error = string.Empty;
                return true;
            }

            if (payload.CardIndex < 0 || payload.CardIndex >= pile.Cards.Count)
            {
                error = $"Card index {payload.CardIndex} is outside {pileType}'s range 0-{pile.Cards.Count - 1}.";
                return false;
            }

            card = pile.Cards[payload.CardIndex];
            if (!card.Id.ToString().Equals(payload.ExpectedCardId, StringComparison.Ordinal))
            {
                error = $"The card at {pileType}[{payload.CardIndex}] changed before the action could run.";
                return false;
            }

            error = string.Empty;
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

        private static bool TryRequireActiveCombat(Player target, out string error)
        {
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                target.PlayerCombatState == null || target.Creature.CombatState == null)
            {
                error = "This change requires an active combat.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void ApplyUpgradeLevels(CardModel card, int levels)
        {
            for (var level = 0; level < levels; level++)
                CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        private static void ApplyAvailableUpgradeLevels(CardModel card, int levels)
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
            int UpgradeLevels);

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
