using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System.Text.Json.Serialization;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugCapabilityOperation
    {
        Add,
        Remove,
        Clear,
    }

    internal enum RitsuDebugCapabilityTargetKind
    {
        Character,
        Card,
        Relic,
        Potion,
        Power,
        Orb,
        Enchantment,
        Affliction,
        Monster,
    }

    internal readonly record struct RitsuDebugCapabilityTarget(
        [property: JsonPropertyName("kind")] RitsuDebugCapabilityTargetKind Kind,
        [property: JsonPropertyName("model_id")]
        string ModelId,
        [property: JsonPropertyName("index")] int Index = -1,
        [property: JsonPropertyName("pile")] string? Pile = null,
        [property: JsonPropertyName("creature_combat_id")]
        uint? CreatureCombatId = null,
        [property: JsonPropertyName("container_model_id")]
        string? ContainerModelId = null);

    internal readonly record struct RitsuDebugCapabilityModelTarget(
        RitsuDebugCapabilityTarget Reference,
        AbstractModel Model);

    internal static class RitsuDebugCapabilityActions
    {
        internal const int MaximumCapabilitiesPerModel = 256;
        internal const int MaximumTargetCount = 16384;
        internal const string ModifyCapabilityActionId = "capabilities.modify";

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModifyCapabilityPayload>(
                ModifyCapabilityActionId,
                ValidateModify,
                ExecuteModifyAsync,
                RitsuLibSidecarInternalPeerFeatures.ExtendedDeveloperStateActionsV1,
                static payload => payload.Target is
                                      { Kind: RitsuDebugCapabilityTargetKind.Card, Pile: { } pile } &&
                                  RitsuDebugCardActions.TryParseMutablePileType(pile, out var pileType) &&
                                  ModCardPileRegistry.IsModPileType(pileType)
                    ? RitsuLibSidecarInternalPeerFeatures.ExtendedDeveloperStateActionsV1
                    : RitsuLibSidecarPeerFeatures.None);
        }

        internal static RitsuDebugActionSubmission Submit(
            Player requester,
            Player target,
            RitsuDebugCapabilityTarget capabilityTarget,
            RitsuDebugCapabilityOperation operation,
            string? capabilityId = null,
            int capabilityIndex = -1)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ModifyCapabilityActionId,
                requester,
                target,
                new ModifyCapabilityPayload(capabilityTarget, operation, capabilityId, capabilityIndex));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static IReadOnlyList<RitsuDebugCapabilityModelTarget> GetTargets(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            var targets = new List<RitsuDebugCapabilityModelTarget>();
            if (player.Character != null)
                targets.Add(new(
                    new(RitsuDebugCapabilityTargetKind.Character, player.Character.Id.ToString()),
                    player.Character));

            foreach (var pileType in RitsuDebugCardActions.GetMutablePileTypes())
            {
                var pile = RitsuDebugCardActions.GetExistingPile(player, pileType);
                if (pile == null)
                    continue;
                var pileToken = RitsuDebugCardActions.GetPileToken(pileType);
                for (var index = 0; index < pile.Cards.Count; index++)
                {
                    var card = pile.Cards[index];
                    targets.Add(new(
                        new(RitsuDebugCapabilityTargetKind.Card, card.Id.ToString(), index, pileToken),
                        card));
                    if (card.Enchantment != null)
                        targets.Add(new(
                            new(
                                RitsuDebugCapabilityTargetKind.Enchantment,
                                card.Enchantment.Id.ToString(),
                                index,
                                pileToken,
                                ContainerModelId: card.Id.ToString()),
                            card.Enchantment));
                    if (card.Affliction != null)
                        targets.Add(new(
                            new(
                                RitsuDebugCapabilityTargetKind.Affliction,
                                card.Affliction.Id.ToString(),
                                index,
                                pileToken,
                                ContainerModelId: card.Id.ToString()),
                            card.Affliction));
                }
            }

            for (var index = 0; index < player.Relics.Count; index++)
            {
                var relic = player.Relics[index];
                targets.Add(new(
                    new(RitsuDebugCapabilityTargetKind.Relic, relic.Id.ToString(), index),
                    relic));
            }

            for (var slot = 0; slot < player.MaxPotionCount; slot++)
            {
                var potion = player.GetPotionAtSlotIndex(slot);
                if (potion == null)
                    continue;
                targets.Add(new(
                    new(RitsuDebugCapabilityTargetKind.Potion, potion.Id.ToString(), slot),
                    potion));
            }

            var creatures = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature.CombatId.HasValue && (creature.IsPlayer || !creature.IsDead))
                .OrderBy(static creature => creature.CombatId)
                .ToArray() ?? [];
            foreach (var creature in creatures)
            {
                if (creature is { CombatId: { } combatId, Monster: { } monster })
                    targets.Add(new(
                        new(
                            RitsuDebugCapabilityTargetKind.Monster,
                            monster.Id.ToString(),
                            CreatureCombatId: combatId),
                        monster));
                for (var index = 0; index < creature.Powers.Count; index++)
                {
                    var power = creature.Powers[index];
                    targets.Add(new(
                        new(
                            RitsuDebugCapabilityTargetKind.Power,
                            power.Id.ToString(),
                            index,
                            CreatureCombatId: creature.CombatId),
                        power));
                }
            }

            var orbs = player.PlayerCombatState?.OrbQueue.Orbs;
            if (orbs != null)
                for (var index = 0; index < orbs.Count; index++)
                {
                    var orb = orbs[index];
                    targets.Add(new(
                        new(RitsuDebugCapabilityTargetKind.Orb, orb.Id.ToString(), index),
                        orb));
                }

            return Array.AsReadOnly(
            [
                .. targets.Take(MaximumTargetCount),
            ]);
        }

        internal static bool TryResolveTarget(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (!IsValidTargetReference(reference))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "capability.targetInvalid",
                    "The selected capability target is invalid.");
                return false;
            }

            return reference.Kind switch
            {
                RitsuDebugCapabilityTargetKind.Character => TryResolveCharacter(target, reference, out model,
                    out feedback),
                RitsuDebugCapabilityTargetKind.Card => TryResolveCard(target, reference, out model, out feedback),
                RitsuDebugCapabilityTargetKind.Relic => TryResolveRelic(target, reference, out model, out feedback),
                RitsuDebugCapabilityTargetKind.Potion => TryResolvePotion(target, reference, out model, out feedback),
                RitsuDebugCapabilityTargetKind.Power => TryResolvePower(reference, out model, out feedback),
                RitsuDebugCapabilityTargetKind.Orb => TryResolveOrb(target, reference, out model, out feedback),
                RitsuDebugCapabilityTargetKind.Enchantment => TryResolveCardAttachment(
                    target,
                    reference,
                    true,
                    out model,
                    out feedback),
                RitsuDebugCapabilityTargetKind.Affliction => TryResolveCardAttachment(
                    target,
                    reference,
                    false,
                    out model,
                    out feedback),
                RitsuDebugCapabilityTargetKind.Monster => TryResolveMonster(reference, out model, out feedback),
                _ => FailTarget(out feedback),
            };
        }

        internal static bool IsValidTargetReference(RitsuDebugCapabilityTarget reference)
        {
            if (!Enum.IsDefined(reference.Kind) ||
                string.IsNullOrWhiteSpace(reference.ModelId) ||
                reference.ModelId.Length > 128)
                return false;
            return reference.Kind switch
            {
                RitsuDebugCapabilityTargetKind.Character =>
                    reference is
                    {
                        Index: -1,
                        Pile: null,
                        CreatureCombatId: null,
                        ContainerModelId: null,
                    },
                RitsuDebugCapabilityTargetKind.Card =>
                    reference is
                    {
                        Index: >= 0,
                        Pile: { } pile,
                        CreatureCombatId: null,
                        ContainerModelId: null,
                    } &&
                    RitsuDebugCardActions.TryParseMutablePileType(pile, out _),
                RitsuDebugCapabilityTargetKind.Relic or
                    RitsuDebugCapabilityTargetKind.Potion or
                    RitsuDebugCapabilityTargetKind.Orb =>
                    reference is
                    {
                        Index: >= 0,
                        Pile: null,
                        CreatureCombatId: null,
                        ContainerModelId: null,
                    },
                RitsuDebugCapabilityTargetKind.Power =>
                    reference is
                    {
                        Index: >= 0,
                        Pile: null,
                        CreatureCombatId: not null,
                        ContainerModelId: null,
                    },
                RitsuDebugCapabilityTargetKind.Enchantment or RitsuDebugCapabilityTargetKind.Affliction =>
                    reference is
                    {
                        Index: >= 0,
                        Pile: { } pile,
                        CreatureCombatId: null,
                        ContainerModelId: { } containerModelId,
                    } &&
                    RitsuDebugCardActions.TryParseMutablePileType(pile, out _) &&
                    !string.IsNullOrWhiteSpace(containerModelId) &&
                    containerModelId.Length <= 128,
                RitsuDebugCapabilityTargetKind.Monster =>
                    reference is
                    {
                        Index: -1,
                        Pile: null,
                        CreatureCombatId: not null,
                        ContainerModelId: null,
                    },
                _ => false,
            };
        }

        private static RitsuDebugActionCheck ValidateModify(
            RitsuDebugActionContext context,
            ModifyCapabilityPayload payload)
        {
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail(
                    "capability.operationInvalid",
                    "The requested capability operation is invalid.");
            if (!TryResolveTarget(context.Target, payload.Target, out var model, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            var capabilities = ModelCapabilities.Get(model);
            return payload.Operation switch
            {
                RitsuDebugCapabilityOperation.Add => ValidateAdd(payload.CapabilityId, model, capabilities),
                RitsuDebugCapabilityOperation.Remove => TryResolveCapability(
                    capabilities,
                    payload.CapabilityId,
                    payload.CapabilityIndex,
                    out _,
                    out feedback)
                    ? RitsuDebugActionCheck.Ok
                    : RitsuDebugActionCheck.Fail(feedback),
                RitsuDebugCapabilityOperation.Clear => capabilities.Count == 0
                    ? RitsuDebugActionCheck.Fail(
                        "capability.noneAttached",
                        "The selected model has no attached capabilities.")
                    : RitsuDebugActionCheck.Ok,
                _ => RitsuDebugActionCheck.Fail(
                    "capability.operationInvalid",
                    "The requested capability operation is invalid."),
            };
        }

        private static RitsuDebugActionCheck ValidateAdd(
            string? capabilityId,
            AbstractModel model,
            ModelCapabilitySet capabilities)
        {
            if (string.IsNullOrWhiteSpace(capabilityId) || capabilityId.Length > 128 ||
                !ModelCapabilityRegistry.TryGetCapabilityType(capabilityId, out var capabilityType))
                return RitsuDebugActionCheck.Fail(
                    "capability.unknown",
                    "Unknown capability '{0}'.",
                    capabilityId ?? string.Empty);
            if (!ModelCapabilityRegistry.IsCompatibleWith(capabilityType, model))
                return RitsuDebugActionCheck.Fail(
                    "capability.incompatible",
                    "Capability '{0}' cannot attach to the selected model.",
                    capabilityId);
            return capabilities.Count >= MaximumCapabilitiesPerModel
                ? RitsuDebugActionCheck.Fail(
                    "capability.limit",
                    "A model cannot have more than {0} capabilities through developer tools.",
                    MaximumCapabilitiesPerModel)
                : RitsuDebugActionCheck.Ok;
        }

        private static Task<string> ExecuteModifyAsync(
            RitsuDebugActionContext context,
            ModifyCapabilityPayload payload)
        {
            _ = TryResolveTarget(context.Target, payload.Target, out var model, out _);
            var capabilities = ModelCapabilities.Get(model);
            var message = payload.Operation switch
            {
                RitsuDebugCapabilityOperation.Add => AddCapability(capabilities, payload.CapabilityId!),
                RitsuDebugCapabilityOperation.Remove => RemoveCapability(
                    capabilities,
                    payload.CapabilityId!,
                    payload.CapabilityIndex),
                RitsuDebugCapabilityOperation.Clear => ClearCapabilities(capabilities),
                _ => throw new ArgumentOutOfRangeException(nameof(payload)),
            };
            RequestVisualReload(context.Target, payload.Target, model);
            return Task.FromResult(message);
        }

        private static void RequestVisualReload(
            Player target,
            RitsuDebugCapabilityTarget reference,
            AbstractModel model)
        {
            switch (model)
            {
                case CardModel card:
                    card.RequestVisualReload();
                    return;
                case RelicModel relic:
                    relic.RequestVisualReload();
                    return;
                case PotionModel potion:
                    potion.RequestVisualReload();
                    return;
                case PowerModel power:
                    power.RequestVisualReload();
                    return;
                case OrbModel orb:
                    orb.RequestVisualReload();
                    return;
            }

            if (reference.Kind is not (RitsuDebugCapabilityTargetKind.Enchantment or
                RitsuDebugCapabilityTargetKind.Affliction))
                return;
            var cardReference = new RitsuDebugCapabilityTarget(
                RitsuDebugCapabilityTargetKind.Card,
                reference.ContainerModelId!,
                reference.Index,
                reference.Pile);
            if (TryResolveCard(target, cardReference, out var cardModel, out _) &&
                cardModel is CardModel ownerCard)
                ownerCard.RequestVisualReload();
        }

        private static string AddCapability(ModelCapabilitySet capabilities, string capabilityId)
        {
            var capability = ModelCapabilityRegistry.Create(capabilityId);
            capabilities.Apply(capability);
            return $"Applied capability {capabilityId} to {capabilities.Owner.Id}.";
        }

        private static string RemoveCapability(
            ModelCapabilitySet capabilities,
            string capabilityId,
            int capabilityIndex)
        {
            _ = TryResolveCapability(capabilities, capabilityId, capabilityIndex, out var capability, out _);
            capabilities.Remove(capability);
            return $"Removed capability {capabilityId} from {capabilities.Owner.Id}.";
        }

        private static string ClearCapabilities(ModelCapabilitySet capabilities)
        {
            var count = capabilities.Count;
            capabilities.Clear();
            return $"Removed {count} capabilities from {capabilities.Owner.Id}.";
        }

        private static bool TryResolveCapability(
            ModelCapabilitySet capabilities,
            string? capabilityId,
            int capabilityIndex,
            out IModelCapability capability,
            out RitsuDebugActionFeedback feedback)
        {
            capability = null!;
            if (string.IsNullOrWhiteSpace(capabilityId) ||
                capabilityId.Length > 128 ||
                capabilityIndex < 0 ||
                capabilityIndex >= capabilities.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "capability.selectionChanged",
                    "The selected capability is no longer available.");
                return false;
            }

            var candidate = capabilities.All[capabilityIndex];
            if (!string.Equals(candidate.CapabilityId, capabilityId, StringComparison.Ordinal))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "capability.selectionChanged",
                    "The selected capability is no longer available.");
                return false;
            }

            capability = candidate;
            feedback = default;
            return true;
        }

        private static bool TryResolveCharacter(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = target.Character;
            return model != null && MatchesModelId(model, reference.ModelId)
                ? Succeed(out feedback)
                : FailTarget(out feedback);
        }

        private static bool TryResolveCard(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (reference.Pile == null ||
                !RitsuDebugCardActions.TryParseMutablePileType(reference.Pile, out var pileType))
                return FailTarget(out feedback);
            var pile = RitsuDebugCardActions.GetExistingPile(target, pileType);
            if (pile == null || reference.Index < 0 || reference.Index >= pile.Cards.Count)
                return FailTarget(out feedback);
            var card = pile.Cards[reference.Index];
            if (!MatchesModelId(card, reference.ModelId))
                return FailTarget(out feedback);
            model = card;
            return Succeed(out feedback);
        }

        private static bool TryResolveRelic(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (reference.Index < 0 || reference.Index >= target.Relics.Count)
                return FailTarget(out feedback);
            var relic = target.Relics[reference.Index];
            if (!MatchesModelId(relic, reference.ModelId))
                return FailTarget(out feedback);
            model = relic;
            return Succeed(out feedback);
        }

        private static bool TryResolvePotion(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (reference.Index < 0 || reference.Index >= target.MaxPotionCount)
                return FailTarget(out feedback);
            var potion = target.GetPotionAtSlotIndex(reference.Index);
            if (potion == null || !MatchesModelId(potion, reference.ModelId))
                return FailTarget(out feedback);
            model = potion;
            return Succeed(out feedback);
        }

        private static bool TryResolvePower(
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (!reference.CreatureCombatId.HasValue)
                return FailTarget(out feedback);
            var creature = RitsuDebugCombatActions.FindCreature(reference.CreatureCombatId.Value);
            if (creature == null || reference.Index < 0 || reference.Index >= creature.Powers.Count)
                return FailTarget(out feedback);
            var power = creature.Powers[reference.Index];
            if (!MatchesModelId(power, reference.ModelId))
                return FailTarget(out feedback);
            model = power;
            return Succeed(out feedback);
        }

        private static bool TryResolveOrb(
            Player target,
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            var orbs = target.PlayerCombatState?.OrbQueue.Orbs;
            if (orbs == null || reference.Index < 0 || reference.Index >= orbs.Count)
                return FailTarget(out feedback);
            var orb = orbs[reference.Index];
            if (!MatchesModelId(orb, reference.ModelId))
                return FailTarget(out feedback);
            model = orb;
            return Succeed(out feedback);
        }

        private static bool TryResolveCardAttachment(
            Player target,
            RitsuDebugCapabilityTarget reference,
            bool enchantment,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            var cardReference = new RitsuDebugCapabilityTarget(
                RitsuDebugCapabilityTargetKind.Card,
                reference.ContainerModelId!,
                reference.Index,
                reference.Pile);
            if (!TryResolveCard(target, cardReference, out var cardModel, out feedback))
            {
                model = null!;
                return false;
            }

            if (cardModel is not CardModel card)
                throw new InvalidOperationException("A resolved card target did not contain a card model.");

            AbstractModel? attachment = enchantment ? card.Enchantment : card.Affliction;
            if (attachment == null || !MatchesModelId(attachment, reference.ModelId))
            {
                model = null!;
                return FailTarget(out feedback);
            }

            model = attachment;
            return Succeed(out feedback);
        }

        private static bool TryResolveMonster(
            RitsuDebugCapabilityTarget reference,
            out AbstractModel model,
            out RitsuDebugActionFeedback feedback)
        {
            model = null!;
            if (!reference.CreatureCombatId.HasValue)
                return FailTarget(out feedback);
            var monster = RitsuDebugCombatActions.FindCreature(reference.CreatureCombatId.Value)?.Monster;
            if (monster == null || !MatchesModelId(monster, reference.ModelId))
                return FailTarget(out feedback);
            model = monster;
            return Succeed(out feedback);
        }

        private static bool MatchesModelId(AbstractModel model, string expectedId)
        {
            return model.Id.ToString().Equals(expectedId, StringComparison.Ordinal);
        }

        private static bool Succeed(out RitsuDebugActionFeedback feedback)
        {
            feedback = default;
            return true;
        }

        private static bool FailTarget(out RitsuDebugActionFeedback feedback)
        {
            feedback = RitsuDebugActionFeedback.Create(
                "capability.targetChanged",
                "The selected capability target changed or is no longer available.");
            return false;
        }

        internal readonly record struct ModifyCapabilityPayload(
            RitsuDebugCapabilityTarget Target,
            RitsuDebugCapabilityOperation Operation,
            string? CapabilityId,
            int CapabilityIndex);
    }
}
