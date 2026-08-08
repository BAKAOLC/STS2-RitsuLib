using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugOrbActions
    {
        internal const string ChannelOrbActionId = "combat.orb.channel";
        internal const string ReplaceOrbActionId = "combat.orb.replace";
        internal const string RemoveOrbActionId = "combat.orb.remove";
        internal const string SetOrbSlotsActionId = "combat.orb-slots.set";
        internal const int MaximumOrbSlots = 10;

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<OrbValuesPayload>(
                ChannelOrbActionId,
                ValidateChannelOrb,
                ExecuteChannelOrbAsync);
            RitsuDebugActionProtocol.Register<OrbReplacementPayload>(
                ReplaceOrbActionId,
                ValidateReplaceOrb,
                ExecuteReplaceOrbAsync);
            RitsuDebugActionProtocol.Register<OrbInstancePayload>(
                RemoveOrbActionId,
                ValidateRemoveOrb,
                ExecuteRemoveOrbAsync);
            RitsuDebugActionProtocol.Register<OrbSlotsPayload>(
                SetOrbSlotsActionId,
                ValidateSetOrbSlots,
                ExecuteSetOrbSlotsAsync);
        }

        internal static RitsuDebugActionSubmission SubmitChannelOrb(
            Player requester,
            Player target,
            string orbId)
        {
            return Submit(
                requester,
                target,
                ChannelOrbActionId,
                new OrbValuesPayload(orbId));
        }

        internal static RitsuDebugActionSubmission SubmitReplaceOrb(
            Player requester,
            Player target,
            int slotIndex,
            string expectedOrbId,
            string replacementOrbId)
        {
            return Submit(
                requester,
                target,
                ReplaceOrbActionId,
                new OrbReplacementPayload(
                    slotIndex,
                    expectedOrbId,
                    replacementOrbId));
        }

        internal static RitsuDebugActionSubmission SubmitRemoveOrb(
            Player requester,
            Player target,
            int slotIndex,
            string expectedOrbId)
        {
            return Submit(
                requester,
                target,
                RemoveOrbActionId,
                new OrbInstancePayload(slotIndex, expectedOrbId));
        }

        internal static RitsuDebugActionSubmission SubmitSetOrbSlots(
            Player requester,
            Player target,
            int capacity)
        {
            return Submit(requester, target, SetOrbSlotsActionId, new OrbSlotsPayload(capacity));
        }

        internal static bool TryResolveOrb(
            string input,
            out OrbModel orb,
            out RitsuDebugActionFeedback feedback)
        {
            orb = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "model.orbIdInvalid",
                    "The orb ID is empty or too long.");
                return false;
            }

            var models = ModelDb.Orbs.ToArray();
            var fullMatches = models
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                :
                [
                    .. models
                        .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                        .Take(2),
                ];
            if (matches.Length == 1)
            {
                orb = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create("model.orbUnknown", "Unknown orb '{0}'.", input)
                : RitsuDebugActionFeedback.Create(
                    "model.orbAmbiguous",
                    "The orb ID '{0}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        private static RitsuDebugActionSubmission Submit<TPayload>(
            Player requester,
            Player target,
            string actionId,
            TPayload payload)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(actionId, requester, target, payload);
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        private static RitsuDebugActionCheck ValidateChannelOrb(
            RitsuDebugActionContext context,
            OrbValuesPayload payload)
        {
            if (!TryRequireOrbQueue(context.Target, out _, out var feedback) ||
                !TryResolveOrb(payload.OrbId, out _, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateReplaceOrb(
            RitsuDebugActionContext context,
            OrbReplacementPayload payload)
        {
            if (!TryResolveOrbInstance(context.Target, payload.SlotIndex, payload.ExpectedOrbId, out _,
                    out var feedback) ||
                !TryResolveOrb(payload.ReplacementOrbId, out _, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateRemoveOrb(
            RitsuDebugActionContext context,
            OrbInstancePayload payload)
        {
            return TryResolveOrbInstance(
                context.Target,
                payload.SlotIndex,
                payload.ExpectedOrbId,
                out _,
                out var feedback)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(feedback);
        }

        private static RitsuDebugActionCheck ValidateSetOrbSlots(
            RitsuDebugActionContext context,
            OrbSlotsPayload payload)
        {
            if (!TryRequireOrbQueue(context.Target, out _, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return payload.Capacity is < 0 or > MaximumOrbSlots
                ? RitsuDebugActionCheck.Fail(
                    "combat.orbSlotRange",
                    "Orb slot count must be between 0 and {0}.",
                    MaximumOrbSlots)
                : RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteChannelOrbAsync(
            RitsuDebugActionContext context,
            OrbValuesPayload payload)
        {
            _ = TryResolveOrb(payload.OrbId, out var canonical, out _);
            var mutable = canonical.ToMutable();
            await OrbCmd.Channel(new BlockingPlayerChoiceContext(), mutable, context.Target);
            return $"Channeled orb {canonical.Id}.";
        }

        private static Task<string> ExecuteReplaceOrbAsync(
            RitsuDebugActionContext context,
            OrbReplacementPayload payload)
        {
            _ = TryResolveOrbInstance(
                context.Target,
                payload.SlotIndex,
                payload.ExpectedOrbId,
                out var current,
                out _);
            _ = TryResolveOrb(payload.ReplacementOrbId, out var canonical, out _);
            var replacement = canonical.ToMutable();
            replacement.Owner = context.Target;
            var queue = context.Target.PlayerCombatState!.OrbQueue;
            _ = queue.Remove(current);
            queue.Insert(payload.SlotIndex, replacement);
            NCombatRoom.Instance?.GetCreatureNode(context.Target.Creature)?.OrbManager
                ?.ReplaceOrb(current, replacement);
            current.RemoveInternal();
            return Task.FromResult($"Replaced orb {current.Id} with {replacement.Id}.");
        }

        private static Task<string> ExecuteRemoveOrbAsync(
            RitsuDebugActionContext context,
            OrbInstancePayload payload)
        {
            _ = TryResolveOrbInstance(
                context.Target,
                payload.SlotIndex,
                payload.ExpectedOrbId,
                out var orb,
                out _);
            var queue = context.Target.PlayerCombatState!.OrbQueue;
            NCombatRoom.Instance?.GetCreatureNode(context.Target.Creature)?.OrbManager?.EvokeOrbAnim(orb);
            _ = queue.Remove(orb);
            orb.RemoveInternal();
            return Task.FromResult($"Removed orb {orb.Id}.");
        }

        private static async Task<string> ExecuteSetOrbSlotsAsync(
            RitsuDebugActionContext context,
            OrbSlotsPayload payload)
        {
            var queue = context.Target.PlayerCombatState!.OrbQueue;
            var difference = payload.Capacity - queue.Capacity;
            switch (difference)
            {
                case > 0:
                    await OrbCmd.AddSlots(context.Target, difference);
                    break;
                case < 0:
                    OrbCmd.RemoveSlots(context.Target, -difference);
                    break;
            }

            return $"Set orb slots to {payload.Capacity}.";
        }

        private static bool TryRequireOrbQueue(
            Player target,
            out IReadOnlyList<OrbModel> orbs,
            out RitsuDebugActionFeedback feedback)
        {
            orbs = [];
            if (!RitsuDebugCombatActions.TryRequireCombat(out feedback))
                return false;
            if (target.PlayerCombatState == null || target.Creature.CombatState == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.orbQueueUnavailable",
                    "The selected player does not have an active orb queue.");
                return false;
            }

            orbs = target.PlayerCombatState.OrbQueue.Orbs;
            feedback = default;
            return true;
        }

        private static bool TryResolveOrbInstance(
            Player target,
            int slotIndex,
            string expectedOrbId,
            out OrbModel orb,
            out RitsuDebugActionFeedback feedback)
        {
            orb = null!;
            if (!TryRequireOrbQueue(target, out var orbs, out feedback))
                return false;
            if (slotIndex < 0 || slotIndex >= orbs.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.orbInstanceChanged",
                    "The selected orb is no longer available.");
                return false;
            }

            var candidate = orbs[slotIndex];
            if (!candidate.Id.ToString().Equals(expectedOrbId, StringComparison.Ordinal))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.orbInstanceChanged",
                    "The selected orb has changed. Refresh and try again.");
                return false;
            }

            orb = candidate;
            feedback = default;
            return true;
        }

        internal readonly record struct OrbValuesPayload(string OrbId);

        internal readonly record struct OrbInstancePayload(int SlotIndex, string ExpectedOrbId);

        internal readonly record struct OrbReplacementPayload(
            int SlotIndex,
            string ExpectedOrbId,
            string ReplacementOrbId);

        internal readonly record struct OrbSlotsPayload(int Capacity);
    }
}
