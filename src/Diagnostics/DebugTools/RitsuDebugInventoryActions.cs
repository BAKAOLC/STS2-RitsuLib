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
        internal const string EditRelicActionId = "inventory.relic.edit";
        internal const string RemoveRelicActionId = "inventory.relic.remove";
        internal const string AddPotionActionId = "inventory.potion.add";
        internal const string EditPotionActionId = "inventory.potion.edit";
        internal const string DiscardPotionActionId = "inventory.potion.discard";
        internal const string ClearInventoryActionId = "inventory.clear";
        internal const int MaxRelicStackCount = 9_999;

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<RelicValuesPayload>(
                AddRelicActionId,
                ValidateAddRelic,
                ExecuteAddRelicAsync);
            RitsuDebugActionProtocol.Register<RelicEditPayload>(
                EditRelicActionId,
                ValidateEditRelic,
                ExecuteEditRelicAsync);
            RitsuDebugActionProtocol.Register<RelicInstancePayload>(
                RemoveRelicActionId,
                ValidateRemoveRelic,
                ExecuteRemoveRelicAsync);
            RitsuDebugActionProtocol.Register<ModelValuesPayload>(
                AddPotionActionId,
                ValidateAddPotion,
                ExecuteAddPotionAsync);
            RitsuDebugActionProtocol.Register<PotionValuesPayload>(
                EditPotionActionId,
                ValidateEditPotion,
                ExecuteEditPotionAsync);
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
            string relicId,
            int stackCount = 1,
            IReadOnlyDictionary<string, int>? dynamicVars = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                AddRelicActionId,
                requester,
                target,
                new RelicValuesPayload(relicId, stackCount, CopyOverrides(dynamicVars)));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEditRelic(
            Player requester,
            Player target,
            string relicId,
            IReadOnlyDictionary<string, int> dynamicVars,
            int? relicIndex = null)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EditRelicActionId,
                requester,
                target,
                new RelicEditPayload(relicId, relicIndex, CopyOverrides(dynamicVars)!));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitRemoveRelic(
            Player requester,
            Player target,
            string relicId,
            int? relicIndex = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                RemoveRelicActionId,
                requester,
                target,
                new RelicInstancePayload(relicId, relicIndex));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitAddPotion(
            Player requester,
            Player target,
            string potionId,
            IReadOnlyDictionary<string, int>? dynamicVars = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                AddPotionActionId,
                requester,
                target,
                new ModelValuesPayload(potionId, CopyOverrides(dynamicVars)));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEditPotion(
            Player requester,
            Player target,
            int slotIndex,
            string expectedPotionId,
            IReadOnlyDictionary<string, int> dynamicVars)
        {
            ArgumentNullException.ThrowIfNull(dynamicVars);
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EditPotionActionId,
                requester,
                target,
                new PotionValuesPayload(slotIndex, expectedPotionId, CopyOverrides(dynamicVars)!));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
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

        internal static bool TryResolveRelic(
            string input,
            out RelicModel relic,
            out RitsuDebugActionFeedback feedback)
        {
            return TryResolveModel(ModelDb.AllRelics, input, "relic", out relic, out feedback);
        }

        internal static bool TryResolvePotion(
            string input,
            out PotionModel potion,
            out RitsuDebugActionFeedback feedback)
        {
            return TryResolveModel(ModelDb.AllPotions, input, "potion", out potion, out feedback);
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

        internal static RitsuDebugActionCheck ValidateAddRelic(
            RitsuDebugActionContext context,
            RelicValuesPayload payload)
        {
            if (!TryResolveRelic(payload.ModelId, out var relic, out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (payload.StackCount is < 1 or > MaxRelicStackCount)
                return RitsuDebugActionCheck.Fail(
                    "inventory.relicStackRange",
                    "Relic stack count must be between 1 and {0}.",
                    MaxRelicStackCount);
            if (!relic.IsStackable && payload.StackCount != 1)
                return RitsuDebugActionCheck.Fail(
                    "inventory.relicNotStackable",
                    "Relic {0} does not support multiple stacks.",
                    relic.Id);
            var valuesCheck = RitsuDebugModelValueOverrides.Validate(relic.DynamicVars, payload.DynamicVars);
            if (!valuesCheck.Success)
                return valuesCheck;

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateEditRelic(
            RitsuDebugActionContext context,
            RelicEditPayload payload)
        {
            if (payload.DynamicVars == null || payload.DynamicVars.Count == 0)
                return RitsuDebugActionCheck.Fail(
                    "model.dynamicVarEditEmpty",
                    "Change at least one model value before applying the edit.");
            if (!TryResolveRelic(payload.ModelId, out var canonical, out var error))
                return RitsuDebugActionCheck.Fail(error);
            var relic = GetOwnedRelic(context.Target, canonical, payload.RelicIndex);
            return relic == null
                ? RitsuDebugActionCheck.Fail(
                    "inventory.relicNotOwned",
                    "The target player does not own {0}.",
                    canonical.Id)
                : RitsuDebugModelValueOverrides.Validate(relic.DynamicVars, payload.DynamicVars);
        }

        private static RitsuDebugActionCheck ValidateRemoveRelic(
            RitsuDebugActionContext context,
            RelicInstancePayload payload)
        {
            if (!TryResolveRelic(payload.ModelId, out var relic, out var error))
                return RitsuDebugActionCheck.Fail(error);

            return GetOwnedRelic(context.Target, relic, payload.RelicIndex) != null
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "inventory.relicNotOwned",
                    "The target player does not own {0}.",
                    relic.Id);
        }

        internal static RitsuDebugActionCheck ValidateAddPotion(
            RitsuDebugActionContext context,
            ModelValuesPayload payload)
        {
            if (!TryResolvePotion(payload.ModelId, out var potion, out var error))
                return RitsuDebugActionCheck.Fail(error);
            var valuesCheck = RitsuDebugModelValueOverrides.Validate(potion.DynamicVars, payload.DynamicVars);
            if (!valuesCheck.Success)
                return valuesCheck;

            return FindEmptyPotionSlot(context.Target) >= 0
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "inventory.potionBeltFull",
                    "The target player's potion belt is full.");
        }

        private static RitsuDebugActionCheck ValidateEditPotion(
            RitsuDebugActionContext context,
            PotionValuesPayload payload)
        {
            if (payload.DynamicVars == null || payload.DynamicVars.Count == 0)
                return RitsuDebugActionCheck.Fail(
                    "model.dynamicVarEditEmpty",
                    "Change at least one model value before applying the edit.");
            if (!TryResolvePotionSlot(
                    context.Target,
                    payload.SlotIndex,
                    payload.ExpectedPotionId,
                    out var potion,
                    out var error))
                return RitsuDebugActionCheck.Fail(error);
            return RitsuDebugModelValueOverrides.Validate(potion.DynamicVars, payload.DynamicVars);
        }

        private static RitsuDebugActionCheck ValidateDiscardPotion(
            RitsuDebugActionContext context,
            PotionSlotPayload payload)
        {
            if (payload.SlotIndex < 0 || payload.SlotIndex >= context.Target.MaxPotionCount)
                return RitsuDebugActionCheck.Fail(
                    "inventory.potionSlotRange",
                    "Potion slot must be between 0 and {0}.",
                    context.Target.MaxPotionCount - 1);

            var potion = context.Target.GetPotionAtSlotIndex(payload.SlotIndex);
            if (potion == null)
                return RitsuDebugActionCheck.Fail(
                    "inventory.potionSlotEmpty",
                    "Potion slot {0} is empty.",
                    payload.SlotIndex);

            return potion.Id.ToString().Equals(payload.ExpectedPotionId, StringComparison.Ordinal)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "inventory.potionSlotChanged",
                    "The potion in slot {0} changed before the action could run.",
                    payload.SlotIndex);
        }

        internal static RitsuDebugActionCheck ValidateClearInventory(
            RitsuDebugActionContext context,
            ClearInventoryPayload payload)
        {
            if (!Enum.IsDefined(payload.Kind))
                return RitsuDebugActionCheck.Fail(
                    "inventory.invalidKind",
                    "The inventory type is invalid.");
            var hasItems = payload.Kind switch
            {
                RitsuDebugInventoryKind.Relics => context.Target.Relics.Count > 0,
                RitsuDebugInventoryKind.Potions => Enumerable.Range(0, context.Target.MaxPotionCount)
                    .Any(index => context.Target.GetPotionAtSlotIndex(index) != null),
                _ => false,
            };
            return hasItems
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "inventory.alreadyEmpty",
                    "The selected inventory is already empty.");
        }

        internal static async Task<string> ExecuteAddRelicAsync(
            RitsuDebugActionContext context,
            RelicValuesPayload payload)
        {
            _ = TryResolveRelic(payload.ModelId, out var relic, out _);
            var mutableRelic = relic.ToMutable();
            RitsuDebugModelValueOverrides.Apply(mutableRelic.DynamicVars, payload.DynamicVars);
            for (var stack = 1; stack < payload.StackCount; stack++)
                mutableRelic.IncrementStackCount();
            await RelicCmd.Obtain(mutableRelic, context.Target);
            return $"Added relic {relic.Id} to the selected player.";
        }

        private static Task<string> ExecuteEditRelicAsync(
            RitsuDebugActionContext context,
            RelicEditPayload payload)
        {
            _ = TryResolveRelic(payload.ModelId, out var canonical, out _);
            var relic = GetOwnedRelic(context.Target, canonical, payload.RelicIndex)!;
            RitsuDebugModelValueOverrides.Apply(relic.DynamicVars, payload.DynamicVars);
            relic.InvokeExecutionFinished();
            return Task.FromResult($"Updated values for relic {relic.Id}.");
        }

        private static async Task<string> ExecuteRemoveRelicAsync(
            RitsuDebugActionContext context,
            RelicInstancePayload payload)
        {
            _ = TryResolveRelic(payload.ModelId, out var relic, out _);
            var ownedRelic = GetOwnedRelic(context.Target, relic, payload.RelicIndex)!;
            await RelicCmd.Remove(ownedRelic);
            return $"Removed relic {relic.Id} from the selected player.";
        }

        internal static async Task<string> ExecuteAddPotionAsync(
            RitsuDebugActionContext context,
            ModelValuesPayload payload)
        {
            return await ExecuteAddPotionAtSlotAsync(context, payload, -1);
        }

        internal static Task<string> ExecuteAddPotionAtSlotAsync(
            RitsuDebugActionContext context,
            ModelPayload payload,
            int slotIndex)
        {
            return ExecuteAddPotionAtSlotAsync(context, new ModelValuesPayload(payload.ModelId, null), slotIndex);
        }

        internal static async Task<string> ExecuteAddPotionAtSlotAsync(
            RitsuDebugActionContext context,
            ModelValuesPayload payload,
            int slotIndex)
        {
            _ = TryResolvePotion(payload.ModelId, out var potion, out _);
            var mutablePotion = potion.ToMutable();
            RitsuDebugModelValueOverrides.Apply(mutablePotion.DynamicVars, payload.DynamicVars);
            var result = await PotionCmd.TryToProcure(mutablePotion, context.Target, slotIndex);
            if (!result.success)
                throw new RitsuDebugActionExecutionException(
                    RitsuDebugActionFeedback.Create(
                        "inventory.potionAddFailed",
                        "The game did not add potion {0} to the selected player.",
                        potion.Id));
            return $"Added potion {potion.Id} to the selected player.";
        }

        private static Task<string> ExecuteEditPotionAsync(
            RitsuDebugActionContext context,
            PotionValuesPayload payload)
        {
            _ = TryResolvePotionSlot(
                context.Target,
                payload.SlotIndex,
                payload.ExpectedPotionId,
                out var potion,
                out _);
            RitsuDebugModelValueOverrides.Apply(potion.DynamicVars, payload.DynamicVars);
            potion.InvokeExecutionFinished();
            return Task.FromResult($"Updated values for potion {potion.Id}.");
        }

        private static async Task<string> ExecuteDiscardPotionAsync(
            RitsuDebugActionContext context,
            PotionSlotPayload payload)
        {
            var potion = context.Target.GetPotionAtSlotIndex(payload.SlotIndex)!;
            await PotionCmd.Discard(potion);
            return $"Discarded potion {potion.Id} from slot {payload.SlotIndex + 1}.";
        }

        internal static async Task<string> ExecuteClearInventoryAsync(
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

        private static RelicModel? GetOwnedRelic(Player player, RelicModel canonical, int? relicIndex)
        {
            if (!relicIndex.HasValue)
                return player.GetRelicById(canonical.Id);
            return relicIndex.Value >= 0 &&
                   relicIndex.Value < player.Relics.Count &&
                   player.Relics[relicIndex.Value].Id == canonical.Id
                ? player.Relics[relicIndex.Value]
                : null;
        }

        private static bool TryResolvePotionSlot(
            Player player,
            int slotIndex,
            string expectedPotionId,
            out PotionModel potion,
            out RitsuDebugActionFeedback feedback)
        {
            potion = null!;
            if (slotIndex < 0 || slotIndex >= player.MaxPotionCount)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "inventory.potionSlotRange",
                    "Potion slot must be between 0 and {0}.",
                    player.MaxPotionCount - 1);
                return false;
            }

            var candidate = player.GetPotionAtSlotIndex(slotIndex);
            if (candidate == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "inventory.potionSlotEmpty",
                    "Potion slot {0} is empty.",
                    slotIndex);
                return false;
            }

            if (!candidate.Id.ToString().Equals(expectedPotionId, StringComparison.Ordinal))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "inventory.potionSlotChanged",
                    "The potion in slot {0} changed before the action could run.",
                    slotIndex);
                return false;
            }

            potion = candidate;
            feedback = default;
            return true;
        }

        private static Dictionary<string, int>? CopyOverrides(IReadOnlyDictionary<string, int>? values)
        {
            return values?.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value,
                StringComparer.Ordinal);
        }

        private static bool TryResolveModel<TModel>(
            IEnumerable<TModel> candidates,
            string input,
            string kind,
            out TModel model,
            out RitsuDebugActionFeedback feedback)
            where TModel : AbstractModel
        {
            model = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    $"model.{kind}IdInvalid",
                    $"The {kind} ID is empty or too long.");
                return false;
            }

            var candidateArray = candidates as TModel[] ?? [.. candidates];
            var fullMatches = candidateArray
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                :
                [
                    .. candidateArray
                        .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                        .Take(2),
                ];
            if (matches.Length == 1)
            {
                model = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create(
                    $"model.{kind}Unknown",
                    $"Unknown {kind} '{{0}}'.",
                    input)
                : RitsuDebugActionFeedback.Create(
                    $"model.{kind}Ambiguous",
                    $"The {kind} ID '{{0}}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        internal readonly record struct ModelPayload(string ModelId);

        internal readonly record struct ModelValuesPayload(
            string ModelId,
            Dictionary<string, int>? DynamicVars);

        internal readonly record struct RelicValuesPayload(
            string ModelId,
            int StackCount,
            Dictionary<string, int>? DynamicVars);

        internal readonly record struct RelicEditPayload(
            string ModelId,
            int? RelicIndex,
            Dictionary<string, int> DynamicVars);

        internal readonly record struct RelicInstancePayload(string ModelId, int? RelicIndex);

        internal readonly record struct PotionSlotPayload(int SlotIndex, string ExpectedPotionId);

        internal readonly record struct PotionValuesPayload(
            int SlotIndex,
            string ExpectedPotionId,
            Dictionary<string, int> DynamicVars);

        internal readonly record struct ClearInventoryPayload(RitsuDebugInventoryKind Kind);
    }
}
