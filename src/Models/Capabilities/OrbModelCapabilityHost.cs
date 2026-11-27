using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal static partial class ModelCapabilityHost
    {
        private const string OrbPassiveTriggerSurface = "orb lifecycle/passive-triggered";
        private const string OrbBeforeTurnEndTriggerSurface = "orb lifecycle/before-turn-end-triggered";
        private const string OrbAfterTurnStartTriggerSurface = "orb lifecycle/after-turn-start-triggered";
        private const string OrbEvokeSurface = "orb lifecycle/evoked";

        internal static bool HasOrbCapabilities(OrbModel orb)
        {
            return HasCapabilities(orb, ModelCapabilityInterfaceFlags.OrbCapability);
        }

        internal static async Task AfterOwnerOrbPassiveTriggered(
            OrbModel orb,
            PlayerChoiceContext choiceContext,
            Creature? target)
        {
            if (!TryGetCapabilitySnapshot(orb, ModelCapabilityInterfaceFlags.OrbCapability, out var snapshot))
                return;

            var context = new OrbPassiveTriggerContext(orb, choiceContext, target);
            foreach (var item in snapshot)
            {
                if (item is not OrbCapability capability || !IsStillAttachedToOrb(capability, orb))
                    continue;

                await TryRunAsync(capability, orb, OrbPassiveTriggerSurface, async () =>
                {
                    if (IsStillAttachedToOrb(capability, orb))
                        await capability.NotifyOwnerOrbPassiveTriggered(context);
                });
            }
        }

        internal static async Task AfterOwnerOrbBeforeTurnEndTriggered(
            OrbModel orb,
            PlayerChoiceContext choiceContext)
        {
            if (!TryGetCapabilitySnapshot(orb, ModelCapabilityInterfaceFlags.OrbCapability, out var snapshot))
                return;

            var context = new OrbBeforeTurnEndTriggerContext(orb, choiceContext);
            foreach (var item in snapshot)
            {
                if (item is not OrbCapability capability || !IsStillAttachedToOrb(capability, orb))
                    continue;

                await TryRunAsync(capability, orb, OrbBeforeTurnEndTriggerSurface, async () =>
                {
                    if (IsStillAttachedToOrb(capability, orb))
                        await capability.NotifyOwnerOrbBeforeTurnEndTriggered(context);
                });
            }
        }

        internal static async Task AfterOwnerOrbAfterTurnStartTriggered(
            OrbModel orb,
            PlayerChoiceContext choiceContext)
        {
            if (!TryGetCapabilitySnapshot(orb, ModelCapabilityInterfaceFlags.OrbCapability, out var snapshot))
                return;

            var context = new OrbAfterTurnStartTriggerContext(orb, choiceContext);
            foreach (var item in snapshot)
            {
                if (item is not OrbCapability capability || !IsStillAttachedToOrb(capability, orb))
                    continue;

                await TryRunAsync(capability, orb, OrbAfterTurnStartTriggerSurface, async () =>
                {
                    if (IsStillAttachedToOrb(capability, orb))
                        await capability.NotifyOwnerOrbAfterTurnStartTriggered(context);
                });
            }
        }

        internal static async Task AfterOwnerOrbEvoked(
            OrbModel orb,
            PlayerChoiceContext choiceContext,
            IEnumerable<Creature> targets)
        {
            if (!TryGetCapabilitySnapshot(orb, ModelCapabilityInterfaceFlags.OrbCapability, out var snapshot))
                return;

            var context = new OrbEvokeContext(orb, choiceContext, [.. targets]);
            foreach (var item in snapshot)
            {
                if (item is not OrbCapability capability || !IsStillAttachedToOrb(capability, orb))
                    continue;

                await TryRunAsync(capability, orb, OrbEvokeSurface, async () =>
                {
                    if (IsStillAttachedToOrb(capability, orb))
                        await capability.NotifyOwnerOrbEvoked(context);
                });
            }
        }

        private static bool IsStillAttachedToOrb(OrbCapability capability, OrbModel orb)
        {
            return ReferenceEquals(capability.Owner, orb);
        }

        private static async Task TryRunAsync(
            IModelCapability capability,
            AbstractModel model,
            string surface,
            Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(surface, model, capability, ex);
            }
        }
    }
}
