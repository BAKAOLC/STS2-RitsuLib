using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugInventoryKind
    {
        Relics,
        Potions,
    }

    internal static class RitsuDebugInventoryActions
    {
        internal const string AddRelicActionId = "inventory.relic.add";
        internal const string RemoveRelicActionId = "inventory.relic.remove";
        internal const string AddPotionActionId = "inventory.potion.add";
        internal const string DiscardPotionActionId = "inventory.potion.discard";
        internal const string ClearInventoryActionId = "inventory.clear";

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModelPayload>(
                AddRelicActionId,
                ValidateAddRelic,
                ExecuteAddRelicAsync);
            RitsuDebugActionProtocol.Register<ModelPayload>(
                RemoveRelicActionId,
                ValidateRemoveRelic,
                ExecuteRemoveRelicAsync);
            RitsuDebugActionProtocol.Register<ModelPayload>(
                AddPotionActionId,
                ValidateAddPotion,
                ExecuteAddPotionAsync);
            RitsuDebugActionProtocol.Register<PotionSlotPayload>(
                DiscardPotionActionId,
                ValidateDiscardPotion,
                ExecuteDiscardPotionAsync);
            RitsuDebugActionProtocol.Register<ClearInventoryPayload>(
                ClearInventoryActionId,
                ValidateClearInventory,
                ExecuteClearInventoryAsync);
        }

        internal static RitsuDebugActionSubmission SubmitAddRelic(
            Player requester,
            Player target,
            string relicId)
        {
            return SubmitModel(requester, target, AddRelicActionId, relicId);
        }

        internal static RitsuDebugActionSubmission SubmitRemoveRelic(
            Player requester,
            Player target,
            string relicId)
        {
            return SubmitModel(requester, target, RemoveRelicActionId, relicId);
        }

        internal static RitsuDebugActionSubmission SubmitAddPotion(
            Player requester,
            Player target,
            string potionId)
        {
            return SubmitModel(requester, target, AddPotionActionId, potionId);
        }

        internal static RitsuDebugActionSubmission SubmitDiscardPotion(
            Player requester,
            Player target,
            int slotIndex,
            string expectedPotionId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                DiscardPotionActionId,
                requester,
                target,
                new PotionSlotPayload(slotIndex, expectedPotionId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitClearInventory(
            Player requester,
            Player target,
            RitsuDebugInventoryKind kind)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ClearInventoryActionId,
                requester,
                target,
                new ClearInventoryPayload(kind));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static bool TryResolveRelic(string input, out RelicModel relic, out string error)
        {
            return TryResolveModel(ModelDb.AllRelics, input, "relic", out relic, out error);
        }

        internal static bool TryResolvePotion(string input, out PotionModel potion, out string error)
        {
            return TryResolveModel(ModelDb.AllPotions, input, "potion", out potion, out error);
        }

        private static RitsuDebugActionSubmission SubmitModel(
            Player requester,
            Player target,
            string actionId,
            string modelId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                actionId,
                requester,
                target,
                new ModelPayload(modelId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        private static RitsuDebugActionCheck ValidateAddRelic(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            if (!TryResolveRelic(payload.ModelId, out var relic, out var error))
                return RitsuDebugActionCheck.Fail(error);

            return context.Target.GetRelicById(relic.Id) == null
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"The target player already owns {relic.Id}.");
        }

        private static RitsuDebugActionCheck ValidateRemoveRelic(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            if (!TryResolveRelic(payload.ModelId, out var relic, out var error))
                return RitsuDebugActionCheck.Fail(error);

            return context.Target.GetRelicById(relic.Id) != null
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"The target player does not own {relic.Id}.");
        }

        private static RitsuDebugActionCheck ValidateAddPotion(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            if (!TryResolvePotion(payload.ModelId, out _, out var error))
                return RitsuDebugActionCheck.Fail(error);

            return FindEmptyPotionSlot(context.Target) >= 0
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail("The target player's potion belt is full.");
        }

        private static RitsuDebugActionCheck ValidateDiscardPotion(
            RitsuDebugActionContext context,
            PotionSlotPayload payload)
        {
            if (payload.SlotIndex < 0 || payload.SlotIndex >= context.Target.MaxPotionCount)
                return RitsuDebugActionCheck.Fail(
                    $"Potion slot must be between 0 and {context.Target.MaxPotionCount - 1}.");

            var potion = context.Target.GetPotionAtSlotIndex(payload.SlotIndex);
            if (potion == null)
                return RitsuDebugActionCheck.Fail($"Potion slot {payload.SlotIndex} is empty.");

            return potion.Id.ToString().Equals(payload.ExpectedPotionId, StringComparison.Ordinal)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    $"The potion in slot {payload.SlotIndex} changed before the action could run.");
        }

        private static RitsuDebugActionCheck ValidateClearInventory(
            RitsuDebugActionContext context,
            ClearInventoryPayload payload)
        {
            if (!Enum.IsDefined(payload.Kind))
                return RitsuDebugActionCheck.Fail("The inventory type is invalid.");
            var hasItems = payload.Kind switch
            {
                RitsuDebugInventoryKind.Relics => context.Target.Relics.Count > 0,
                RitsuDebugInventoryKind.Potions => Enumerable.Range(0, context.Target.MaxPotionCount)
                    .Any(index => context.Target.GetPotionAtSlotIndex(index) != null),
                _ => false,
            };
            return hasItems
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail("The selected inventory is already empty.");
        }

        private static async Task<string> ExecuteAddRelicAsync(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            _ = TryResolveRelic(payload.ModelId, out var relic, out _);
            await RelicCmd.Obtain(relic.ToMutable(), context.Target);
            return $"Added relic {relic.Id} to the selected player.";
        }

        private static async Task<string> ExecuteRemoveRelicAsync(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            _ = TryResolveRelic(payload.ModelId, out var relic, out _);
            var ownedRelic = context.Target.GetRelicById(relic.Id)!;
            await RelicCmd.Remove(ownedRelic);
            return $"Removed relic {relic.Id} from the selected player.";
        }

        private static async Task<string> ExecuteAddPotionAsync(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            _ = TryResolvePotion(payload.ModelId, out var potion, out _);
            var result = await PotionCmd.TryToProcure(potion.ToMutable(), context.Target);
            if (!result.success)
                throw new RitsuDebugActionExecutionException(
                    $"The game did not add potion {potion.Id} to the selected player.");
            return $"Added potion {potion.Id} to the selected player.";
        }

        private static async Task<string> ExecuteDiscardPotionAsync(
            RitsuDebugActionContext context,
            PotionSlotPayload payload)
        {
            var potion = context.Target.GetPotionAtSlotIndex(payload.SlotIndex)!;
            await PotionCmd.Discard(potion);
            return $"Discarded potion {potion.Id} from slot {payload.SlotIndex + 1}.";
        }

        private static async Task<string> ExecuteClearInventoryAsync(
            RitsuDebugActionContext context,
            ClearInventoryPayload payload)
        {
            if (payload.Kind == RitsuDebugInventoryKind.Relics)
            {
                var relics = context.Target.Relics.ToArray();
                foreach (var relic in relics)
                    await RelicCmd.Remove(relic);
                return relics.Length == 1
                    ? "Removed the selected player's relic."
                    : $"Removed all {relics.Length} relics from the selected player.";
            }

            var potions = Enumerable.Range(0, context.Target.MaxPotionCount)
                .Select(context.Target.GetPotionAtSlotIndex)
                .OfType<PotionModel>()
                .ToArray();
            foreach (var potion in potions)
                await PotionCmd.Discard(potion);
            return potions.Length == 1
                ? "Discarded the selected player's potion."
                : $"Discarded all {potions.Length} potions from the selected player.";
        }

        private static int FindEmptyPotionSlot(Player player)
        {
            for (var index = 0; index < player.MaxPotionCount; index++)
                if (player.GetPotionAtSlotIndex(index) == null)
                    return index;

            return -1;
        }

        private static bool TryResolveModel<TModel>(
            IEnumerable<TModel> candidates,
            string input,
            string kind,
            out TModel model,
            out string error)
            where TModel : AbstractModel
        {
            model = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                error = $"The {kind} ID is empty or too long.";
                return false;
            }

            var candidateArray = candidates as TModel[] ?? candidates.ToArray();
            var fullMatches = candidateArray
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                : candidateArray
                    .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
            if (matches.Length == 1)
            {
                model = matches[0];
                error = string.Empty;
                return true;
            }

            error = matches.Length == 0
                ? $"Unknown {kind} '{input}'."
                : $"The {kind} ID '{input}' is ambiguous; use the full model ID.";
            return false;
        }

        internal readonly record struct ModelPayload(string ModelId);

        internal readonly record struct PotionSlotPayload(int SlotIndex, string ExpectedPotionId);

        internal readonly record struct ClearInventoryPayload(RitsuDebugInventoryKind Kind);
    }
}
