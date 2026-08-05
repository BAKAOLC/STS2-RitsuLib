using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugSecondaryResourceOperation
    {
        Gain,
        Lose,
        Set,
        Reset,
        ResetToMax,
    }

    internal static class RitsuDebugSecondaryResourceActions
    {
        internal const string ModifyActionId = "secondary-resources.modify";

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModifyPayload>(
                ModifyActionId,
                ValidateModify,
                ExecuteModifyAsync,
                RitsuLibSidecarInternalPeerFeatures.ExtendedDeveloperStateActionsV1);
        }

        internal static RitsuDebugActionSubmission Submit(
            Player requester,
            Player target,
            string resourceId,
            RitsuDebugSecondaryResourceOperation operation,
            int value = 0)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ModifyActionId,
                requester,
                target,
                new ModifyPayload(resourceId, operation, value));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionCheck ValidateModify(
            RitsuDebugActionContext context,
            ModifyPayload payload)
        {
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail(
                    "secondaryResource.invalidOperation",
                    "The secondary-resource operation is invalid.");
            if (string.IsNullOrWhiteSpace(payload.ResourceId) || payload.ResourceId.Length > 256 ||
                !ModSecondaryResourceRegistry.TryGet(payload.ResourceId, out var definition))
                return RitsuDebugActionCheck.Fail(
                    "secondaryResource.unknown",
                    "Secondary resource '{0}' is not registered.",
                    payload.ResourceId);
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                context.Target.PlayerCombatState == null || context.Target.Creature.CombatState == null)
                return RitsuDebugActionCheck.Fail(
                    "player.activeCombatRequired",
                    "This secondary-resource operation requires an active combat.");

            return payload.Operation switch
            {
                RitsuDebugSecondaryResourceOperation.Gain or RitsuDebugSecondaryResourceOperation.Lose
                    when payload.Value is < 1 => RitsuDebugActionCheck.Fail(
                        "secondaryResource.positiveAmountRequired",
                        "The amount must be a positive integer."),
                RitsuDebugSecondaryResourceOperation.Set
                    when payload.Value < definition.MinAmount || payload.Value > definition.HardMaxAmount =>
                    RitsuDebugActionCheck.Fail(
                        "secondaryResource.valueRange",
                        "The amount must be between {0} and {1}.",
                        definition.MinAmount,
                        definition.HardMaxAmount),
                RitsuDebugSecondaryResourceOperation.Reset or RitsuDebugSecondaryResourceOperation.ResetToMax
                    when payload.Value != 0 => RitsuDebugActionCheck.Fail(
                        "secondaryResource.unexpectedAmount",
                        "Reset operations do not accept an amount."),
                RitsuDebugSecondaryResourceOperation.ResetToMax
                    when SecondaryResourceCmd.GetMax(context.Target, definition.Id) == null =>
                    RitsuDebugActionCheck.Fail(
                        "secondaryResource.noMaximum",
                        "Secondary resource '{0}' does not have a maximum.",
                        definition.Id),
                _ => RitsuDebugActionCheck.Ok,
            };
        }

        internal static async Task<string> ExecuteModifyAsync(
            RitsuDebugActionContext context,
            ModifyPayload payload)
        {
            var amount = payload.Operation switch
            {
                RitsuDebugSecondaryResourceOperation.Gain =>
                    await SecondaryResourceCmd.Gain(context.Target, payload.ResourceId, payload.Value),
                RitsuDebugSecondaryResourceOperation.Lose =>
                    await SecondaryResourceCmd.Lose(context.Target, payload.ResourceId, payload.Value),
                RitsuDebugSecondaryResourceOperation.Set =>
                    await SecondaryResourceCmd.Set(context.Target, payload.ResourceId, payload.Value),
                RitsuDebugSecondaryResourceOperation.Reset =>
                    await SecondaryResourceCmd.Reset(context.Target, payload.ResourceId),
                RitsuDebugSecondaryResourceOperation.ResetToMax =>
                    await SecondaryResourceCmd.Reset(context.Target, payload.ResourceId, true),
                _ => throw new ArgumentOutOfRangeException(nameof(payload.Operation)),
            };
            return $"Set {payload.ResourceId} to {amount}.";
        }

        internal readonly record struct ModifyPayload(
            string ResourceId,
            RitsuDebugSecondaryResourceOperation Operation,
            int Value);
    }
}
