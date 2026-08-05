using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugStatePresetActions
    {
        internal const string ApplyPresetActionId = "state-presets.apply";
        internal const int MaximumDecodedPresetBytes = 64 * 1024;
        internal const int MaximumImportCharacters = 256 * 1024;

        private const int CompressedPayloadEncoding = 1;
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<RitsuDebugStatePresetWirePayload>(
                ApplyPresetActionId,
                ValidateWirePreset,
                ExecuteWirePresetAsync,
                payloadPeerFeatures: RequiredFeaturesForPresetPayload);
        }

        internal static RitsuDebugActionSubmission SubmitApplyPreset(
            Player requester,
            Player target,
            RitsuDebugStatePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            if (!TryEncodePreset(preset, out var payload, out var feedback))
                return RitsuDebugActionSubmission.Reject(feedback);
            return RitsuDebugActionProtocol.Submit(
                requester,
                RitsuDebugActionProtocol.CreateEnvelope(ApplyPresetActionId, requester, target, payload));
        }

        internal static RitsuDebugActionCheck ValidateStoredPreset(RitsuDebugStatePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            if (preset.Id is not { Length: 32 } || !Guid.TryParseExact(preset.Id, "N", out _) ||
                string.IsNullOrWhiteSpace(preset.Name) ||
                preset.Name.Length > RitsuDebugStatePresetStore.MaximumNameLength ||
                preset.CardPiles == null || !preset.HasAnyContent)
                return RitsuDebugActionCheck.Fail(
                    "statePreset.invalid",
                    "The preset is missing required data or exceeds a supported limit.");

            var pileNames = new HashSet<PileType>();
            foreach (var pile in preset.CardPiles)
            {
                if (pile == null || !Enum.IsDefined(pile.ApplyMode) ||
                    !RitsuDebugCardActions.TryParseMutablePileType(pile.Pile, out var pileType) ||
                    !pileNames.Add(pileType) || pile.Cards == null)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.cardPileInvalid",
                        "A card-pile configuration is invalid or duplicated.");
                var total = 0;
                foreach (var card in pile.Cards)
                {
                    if (card == null)
                        return RitsuDebugActionCheck.Fail(
                            "statePreset.cardInvalid",
                            "A saved card entry is invalid.");
                    if (!RitsuDebugCardActions.TryResolveCanonicalCard(
                            card.CardId,
                            out var canonical,
                            out var feedback))
                        return RitsuDebugActionCheck.Fail(feedback);
                    if (card.Count is < 1 or > RitsuDebugCardActions.MaxCreateCount ||
                        (total += card.Count) > RitsuDebugStatePresetStore.MaximumCardsPerPile ||
                        card.UpgradeLevels < 0 || card.UpgradeLevels > canonical.MaxUpgradeLevel)
                        return RitsuDebugActionCheck.Fail(
                            "statePreset.cardInvalid",
                            "A saved card entry is invalid.");
                    var preview = canonical.ToMutable();
                    RitsuDebugCardActions.ApplyAvailableUpgradeLevels(preview, card.UpgradeLevels);
                    var stateCheck = RitsuDebugCardActions.ValidateCardState(preview, card.ToCardState());
                    if (!stateCheck.Success)
                        return stateCheck;
                }

                if (pile is { ApplyMode: RitsuDebugStatePresetApplyMode.Add, Cards.Count: 0 })
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.emptyAddGroup",
                        "An add-mode group must contain at least one item.");
            }

            var relicCheck = ValidateRelics(preset.Relics);
            if (!relicCheck.Success)
                return relicCheck;
            var potionCheck = ValidatePotions(preset.Potions);
            if (!potionCheck.Success)
                return potionCheck;
            var powerCheck = ValidatePowers(preset.Powers);
            if (!powerCheck.Success)
                return powerCheck;
            var playerCheck = ValidatePlayer(preset.Player, false);
            if (!playerCheck.Success)
                return playerCheck;
            var resourceCheck = ValidateSecondaryResources(preset.SecondaryResources);
            if (!resourceCheck.Success)
                return resourceCheck;
            var capabilityCheck = ValidateCapabilityTargets(preset.CapabilityTargets);
            if (!capabilityCheck.Success)
                return capabilityCheck;
            return TryEncodePreset(preset, out _, out var encodeFeedback)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(encodeFeedback);
        }

        internal static bool TryEncodePreset(
            RitsuDebugStatePreset preset,
            out RitsuDebugStatePresetWirePayload payload,
            out RitsuDebugActionFeedback feedback)
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(preset);
            if (serialized.Length > MaximumDecodedPresetBytes)
            {
                payload = default;
                feedback = RitsuDebugActionFeedback.Create(
                    "statePreset.decodedDataLimit",
                    "The preset contains too much data to apply.");
                return false;
            }

            using var output = new MemoryStream();
            using (var compressor = new BrotliStream(output, CompressionLevel.Optimal, true))
            {
                compressor.Write(serialized);
            }

            payload = new(
                CompressedPayloadEncoding,
                Convert.ToBase64String(output.GetBuffer(), 0, (int)output.Length));
            if (JsonSerializer.Serialize(payload).Length <= RitsuDebugActionProtocol.MaxActionPayloadCharacters)
            {
                feedback = default;
                return true;
            }

            payload = default;
            feedback = RitsuDebugActionFeedback.Create(
                "statePreset.dataLimit",
                "The preset contains too much data to apply.");
            return false;
        }

        private static RitsuDebugActionCheck ValidateWirePreset(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetWirePayload payload)
        {
            return TryDecodePreset(payload, out var preset, out var feedback)
                ? ValidatePreset(context, preset)
                : RitsuDebugActionCheck.Fail(feedback);
        }

        private static async Task<string> ExecuteWirePresetAsync(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetWirePayload payload)
        {
            if (!TryDecodePreset(payload, out var preset, out var feedback))
                throw new RitsuDebugActionExecutionException(feedback);
            return await ExecutePresetAsync(context, preset);
        }

        private static RitsuLibSidecarPeerFeatures RequiredFeaturesForPresetPayload(
            RitsuDebugStatePresetWirePayload payload)
        {
            if (!TryDecodePreset(payload, out var preset, out _))
                return RitsuLibSidecarPeerFeatures.None;
            if (preset.SecondaryResources is { Count: > 0 } ||
                preset.CapabilityTargets is { Count: > 0 })
                return RitsuLibSidecarInternalPeerFeatures.ExtendedDeveloperStateActionsV1;
            foreach (var pile in preset.CardPiles)
                if (RitsuDebugCardActions.TryParseMutablePileType(pile.Pile, out var pileType) &&
                    ModCardPileRegistry.IsModPileType(pileType))
                    return RitsuLibSidecarInternalPeerFeatures.ExtendedDeveloperStateActionsV1;
            return RitsuLibSidecarPeerFeatures.None;
        }

        private static bool TryDecodePreset(
            RitsuDebugStatePresetWirePayload payload,
            out RitsuDebugStatePreset preset,
            out RitsuDebugActionFeedback feedback)
        {
            preset = null!;
            if (payload.Encoding != CompressedPayloadEncoding ||
                string.IsNullOrWhiteSpace(payload.Data) ||
                payload.Data.Length > RitsuDebugActionProtocol.MaxActionPayloadCharacters)
            {
                feedback = InvalidEncoding();
                return false;
            }

            try
            {
                var compressed = Convert.FromBase64String(payload.Data);
                using var input = new MemoryStream(compressed, false);
                using var decompressor = new BrotliStream(input, CompressionMode.Decompress, false);
                using var output = new MemoryStream();
                var buffer = new byte[4096];
                while (true)
                {
                    var read = decompressor.Read(buffer);
                    if (read == 0)
                        break;
                    if (output.Length + read > MaximumDecodedPresetBytes)
                    {
                        feedback = RitsuDebugActionFeedback.Create(
                            "statePreset.decodedDataLimit",
                            "The preset contains too much data to apply.");
                        return false;
                    }

                    output.Write(buffer, 0, read);
                }

                preset = JsonSerializer.Deserialize<RitsuDebugStatePreset>(
                    StrictUtf8.GetString(output.GetBuffer(), 0, (int)output.Length))!;
                if (preset != null)
                {
                    feedback = default;
                    return true;
                }
            }
            catch (Exception exception) when (exception is FormatException or IOException or JsonException or
                                                  DecoderFallbackException or NotSupportedException)
            {
                // Rejected below with a stable protocol error.
            }

            feedback = InvalidEncoding();
            return false;
        }

        private static RitsuDebugActionCheck ValidatePreset(
            RitsuDebugActionContext context,
            RitsuDebugStatePreset preset)
        {
            var stored = ValidateStoredPreset(preset);
            if (!stored.Success)
                return stored;
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var pile in preset.CardPiles)
            {
                var pileCheck = RitsuDebugCardActions.ValidateModifyPile(
                    context,
                    new(pile.Pile, RitsuDebugCardPileOperation.Clear, 0));
                if (!pileCheck.Success)
                    return pileCheck;
                foreach (var card in pile.Cards)
                {
                    var createCheck = RitsuDebugCardActions.ValidateCreateCard(
                        context,
                        new(card.CardId, pile.Pile, card.Count, card.UpgradeLevels, card.ToCardState()));
                    if (!createCheck.Success)
                        return createCheck;
                }
            }

            if (preset.Potions != null)
            {
                var potionSlotCount = preset.Player?.PotionSlots ?? context.Target.MaxPotionCount;
                if (preset.Potions.ApplyMode == RitsuDebugStatePresetApplyMode.Add)
                {
                    var freeSlots = Enumerable.Range(0, potionSlotCount)
                        .Count(index => context.Target.GetPotionAtSlotIndex(index) == null);
                    if (freeSlots < preset.Potions.Items.Count)
                        return RitsuDebugActionCheck.Fail(
                            "inventory.potionBeltFull",
                            "The target player does not have enough empty potion slots.");
                }
                else if (preset.Potions.Items.Any(item => item.SlotIndex >= potionSlotCount))
                {
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.potionSlots",
                        "The target player does not have enough potion slots for this preset.");
                }
            }

            if (preset.Powers != null &&
                (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                 context.Target.PlayerCombatState == null || !context.Target.Creature.CanReceivePowers))
                return RitsuDebugActionCheck.Fail(
                    "player.activeCombatRequired",
                    "The saved powers require an active combat in which the target can receive powers.");
            if (preset.SecondaryResources != null)
                foreach (var (resourceId, amount) in preset.SecondaryResources)
                {
                    var resourceCheck = RitsuDebugSecondaryResourceActions.ValidateModify(
                        context,
                        new(resourceId, RitsuDebugSecondaryResourceOperation.Set, amount));
                    if (!resourceCheck.Success)
                        return resourceCheck;
                }

            var playerCheck = ValidatePlayer(preset.Player, true);
            if (!playerCheck.Success || preset.Player == null)
                return playerCheck;
            if (preset.Player.CurrentHp.HasValue && !preset.Player.MaxHp.HasValue &&
                preset.Player.CurrentHp.Value > context.Target.Creature.MaxHp)
                return RitsuDebugActionCheck.Fail(
                    "player.currentHpExceedsMax",
                    "Current HP cannot exceed the target's max HP ({0}).",
                    context.Target.Creature.MaxHp);
            if (preset.Player.MaxHp.HasValue && !preset.Player.CurrentHp.HasValue &&
                preset.Player.MaxHp.Value < context.Target.Creature.CurrentHp)
                return RitsuDebugActionCheck.Fail(
                    "player.maxHpBelowCurrent",
                    "Max HP cannot be lower than the target's current HP ({0}).",
                    context.Target.Creature.CurrentHp);
            return RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecutePresetAsync(
            RitsuDebugActionContext context,
            RitsuDebugStatePreset preset)
        {
            if (preset.Player?.PotionSlots is { } potionSlots)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetPotionSlots, potionSlots);
            foreach (var pile in preset.CardPiles)
            {
                _ = RitsuDebugCardActions.TryParseMutablePileType(pile.Pile, out var pileType);
                if (pile.ApplyMode == RitsuDebugStatePresetApplyMode.Replace)
                    await RitsuDebugCardActions.ExecuteModifyPileAsync(
                        context,
                        new(pile.Pile, RitsuDebugCardPileOperation.Clear, 0));
                foreach (var card in pile.Cards)
                    await RitsuDebugCardActions.ExecuteCreateCardAsync(
                        context,
                        new(
                            card.CardId,
                            pile.Pile,
                            card.Count,
                            preset.ApplyInternalValues ? card.UpgradeLevels : 0,
                            card.ToCardState(preset.ApplyInternalValues)),
                        false);
                if (pileType != PileType.Deck ||
                    RitsuDebugCardActions.GetPile(context.Target, pileType) is not { } deck)
                    continue;
                var addedCardCount = pile.Cards.Sum(static card => card.Count);
                for (var index = 0; index < addedCardCount; index++)
                    deck.InvokeCardAddFinished();
            }

            if (preset.Relics != null)
                await ApplyRelics(context, preset.Relics, preset.ApplyInternalValues);
            if (preset.Potions != null)
                await ApplyPotions(context, preset.Potions, preset.ApplyInternalValues);
            if (preset.Powers != null)
                await ApplyPowers(context, preset.Powers, preset.ApplyInternalValues);
            if (preset.SecondaryResources != null)
                foreach (var (resourceId, amount) in preset.SecondaryResources)
                    await RitsuDebugSecondaryResourceActions.ExecuteModifyAsync(
                        context,
                        new(resourceId, RitsuDebugSecondaryResourceOperation.Set, amount));
            if (preset.Player != null)
                await ApplyPlayer(context, preset.Player, true);
            if (preset.CapabilityTargets != null)
                ApplyCapabilityTargets(context.Target, preset.CapabilityTargets);
            return $"Applied state preset '{preset.Name}'.";
        }

        private static RitsuDebugActionCheck ValidateRelics(RitsuDebugStatePresetInventory? relics)
        {
            if (relics == null)
                return RitsuDebugActionCheck.Ok;
            if (!Enum.IsDefined(relics.ApplyMode) || relics.ModelIds == null ||
                relics.ModelIds.Count > RitsuDebugStatePresetStore.MaximumRelics ||
                relics.ModelIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != relics.ModelIds.Count ||
                relics is { ApplyMode: RitsuDebugStatePresetApplyMode.Add, ModelIds.Count: 0 })
                return RitsuDebugActionCheck.Fail(
                    "statePreset.relicsInvalid",
                    "The saved relic collection is invalid.");
            foreach (var modelId in relics.ModelIds)
            {
                if (!RitsuDebugInventoryActions.TryResolveRelic(modelId, out var canonical, out var feedback))
                    return RitsuDebugActionCheck.Fail(feedback);
                if (relics.InternalValues?.GetValueOrDefault(modelId) is not { } values)
                    continue;
                if (values.StackCount is < 1 or > RitsuDebugInventoryActions.MaxRelicStackCount)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.relicsInvalid",
                        "The saved relic collection is invalid.");
                var valueCheck = RitsuDebugModelValueOverrides.Validate(canonical.DynamicVars, values.DynamicVars);
                if (!valueCheck.Success)
                    return valueCheck;
            }

            if (relics.InternalValues != null &&
                relics.InternalValues.Keys.Any(id => !relics.ModelIds.Contains(id, StringComparer.Ordinal)))
                return RitsuDebugActionCheck.Fail(
                    "statePreset.relicsInvalid",
                    "The saved relic collection is invalid.");
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidatePotions(RitsuDebugStatePresetPotions? potions)
        {
            if (potions == null)
                return RitsuDebugActionCheck.Ok;
            if (!Enum.IsDefined(potions.ApplyMode) || potions.Items == null ||
                potions.Items.Count > RitsuDebugPlayerActions.MaxPotionSlots ||
                potions is { ApplyMode: RitsuDebugStatePresetApplyMode.Add, Items.Count: 0 })
                return RitsuDebugActionCheck.Fail(
                    "statePreset.potionsInvalid",
                    "The saved potion collection is invalid.");
            var slots = new HashSet<int>();
            foreach (var potion in potions.Items)
            {
                if (potion == null)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.potionsInvalid",
                        "The saved potion collection is invalid.");
                if (!RitsuDebugInventoryActions.TryResolvePotion(
                        potion.PotionId,
                        out var canonical,
                        out var feedback))
                    return RitsuDebugActionCheck.Fail(feedback);
                var valueCheck = RitsuDebugModelValueOverrides.Validate(canonical.DynamicVars, potion.DynamicVars);
                if (!valueCheck.Success)
                    return valueCheck;
                if (potions.ApplyMode == RitsuDebugStatePresetApplyMode.Add)
                {
                    if (potion.SlotIndex.HasValue)
                        return RitsuDebugActionCheck.Fail(
                            "statePreset.potionsInvalid",
                            "Add-mode potions cannot require a fixed slot.");
                }
                else if (potion.SlotIndex is not { } slot ||
                         slot is < 0 or >= RitsuDebugPlayerActions.MaxPotionSlots || !slots.Add(slot))
                {
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.potionsInvalid",
                        "Replace-mode potion slots must be unique and valid.");
                }
            }

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidatePowers(RitsuDebugStatePresetPowers? powers)
        {
            if (powers == null)
                return RitsuDebugActionCheck.Ok;
            if (!Enum.IsDefined(powers.ApplyMode) || powers.Items == null ||
                powers.Items.Count > RitsuDebugStatePresetStore.MaximumPowers ||
                powers.Items.Any(static item => item == null) ||
                powers.Items.Select(static item => item.PowerId)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() != powers.Items.Count ||
                powers is { ApplyMode: RitsuDebugStatePresetApplyMode.Add, Items.Count: 0 })
                return RitsuDebugActionCheck.Fail(
                    "statePreset.powersInvalid",
                    "The saved power collection is invalid.");
            foreach (var power in powers.Items)
            {
                if (power == null || power.Amount is < 1 or > RitsuDebugCombatActions.MaxAmount)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.powersInvalid",
                        "The saved power collection is invalid.");
                if (!RitsuDebugCombatActions.TryResolvePower(
                        power.PowerId,
                        out var canonical,
                        out var feedback))
                    return RitsuDebugActionCheck.Fail(feedback);
                var valueCheck = RitsuDebugModelValueOverrides.Validate(canonical.DynamicVars, power.DynamicVars);
                if (!valueCheck.Success)
                    return valueCheck;
            }

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateSecondaryResources(
            IReadOnlyDictionary<string, int>? resources)
        {
            if (resources == null)
                return RitsuDebugActionCheck.Ok;
            if (resources.Count is 0 or > RitsuDebugStatePresetStore.MaximumSecondaryResources)
                return RitsuDebugActionCheck.Fail(
                    "statePreset.secondaryResourcesInvalid",
                    "The saved secondary-resource collection is empty or exceeds a supported limit.");
            foreach (var (resourceId, amount) in resources)
                if (string.IsNullOrWhiteSpace(resourceId) || resourceId.Length > 256 ||
                    !ModSecondaryResourceRegistry.TryGet(resourceId, out var definition) ||
                    amount < definition.MinAmount || amount > definition.HardMaxAmount)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.secondaryResourceInvalid",
                        "A saved secondary-resource value is invalid or its resource is not registered.");
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateCapabilityTargets(
            IReadOnlyList<RitsuDebugStatePresetCapabilityTarget>? targets)
        {
            if (targets == null)
                return RitsuDebugActionCheck.Ok;
            if (targets.Count > RitsuDebugStatePresetStore.MaximumCapabilityTargets)
                return RitsuDebugActionCheck.Fail(
                    "statePreset.capabilityLimit",
                    "The capability state exceeds the supported preset limits.");
            var uniqueTargets = new HashSet<RitsuDebugCapabilityTarget>();
            foreach (var target in targets)
            {
                if (target == null ||
                    !IsStableCapabilityTargetKind(target.Target.Kind) ||
                    !RitsuDebugCapabilityActions.IsValidTargetReference(target.Target) ||
                    !uniqueTargets.Add(target.Target) ||
                    target.Capabilities?.Capabilities == null ||
                    target.Capabilities.Capabilities.Count >
                    RitsuDebugCapabilityActions.MaximumCapabilitiesPerModel)
                    return RitsuDebugActionCheck.Fail(
                        "statePreset.capabilityInvalid",
                        "A saved model-capability target or entry is invalid.");
                foreach (var capability in target.Capabilities.Capabilities)
                    if (capability == null ||
                        string.IsNullOrWhiteSpace(capability.Id) ||
                        capability.Id.Length > 128 ||
                        capability.Schema < 1)
                        return RitsuDebugActionCheck.Fail(
                            "statePreset.capabilityInvalid",
                            "A saved model-capability target or entry is invalid.");
            }

            return RitsuDebugActionCheck.Ok;
        }

        internal static bool IsStableCapabilityTargetKind(RitsuDebugCapabilityTargetKind kind)
        {
            return kind is RitsuDebugCapabilityTargetKind.Character or
                RitsuDebugCapabilityTargetKind.Card or
                RitsuDebugCapabilityTargetKind.Relic or
                RitsuDebugCapabilityTargetKind.Potion or
                RitsuDebugCapabilityTargetKind.Power or
                RitsuDebugCapabilityTargetKind.Orb;
        }

        private static RitsuDebugActionCheck ValidatePlayer(RitsuDebugStatePresetPlayer? player, bool requireCombat)
        {
            if (player == null)
                return RitsuDebugActionCheck.Ok;
            if (!player.HasAnyValue || player.Gold is < 0 or > RitsuDebugPlayerActions.MaxGold ||
                player.CurrentHp is < 1 or > RitsuDebugPlayerActions.MaxHitPoints ||
                player.MaxHp is < 1 or > RitsuDebugPlayerActions.MaxHitPoints ||
                player.MaxEnergy is < 1 or > RitsuDebugPlayerActions.MaxCombatResource ||
                player.PotionSlots is < 0 or > RitsuDebugPlayerActions.MaxPotionSlots ||
                player.Energy is < 0 or > RitsuDebugPlayerActions.MaxCombatResource ||
                player.Stars is < 0 or > RitsuDebugPlayerActions.MaxCombatResource ||
                player.Block is < 0 or > RitsuDebugPlayerActions.MaxHitPoints ||
                player.CurrentHp > player.MaxHp)
                return RitsuDebugActionCheck.Fail(
                    "statePreset.playerInvalid",
                    "One or more saved player values are invalid.");
            if (requireCombat && (player.Energy.HasValue || player.Stars.HasValue || player.Block.HasValue) &&
                (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding))
                return RitsuDebugActionCheck.Fail(
                    "player.activeCombatRequired",
                    "The saved combat values require an active combat.");
            return RitsuDebugActionCheck.Ok;
        }

        private static void ApplyCapabilityTargets(
            Player player,
            IReadOnlyList<RitsuDebugStatePresetCapabilityTarget> targets)
        {
            foreach (var target in targets)
            {
                if (!RitsuDebugCapabilityActions.TryResolveTarget(
                        player,
                        target.Target,
                        out var model,
                        out var feedback))
                    throw new RitsuDebugActionExecutionException(feedback);
                var capabilities = ModelCapabilities.Get(model);
                capabilities.Load(target.Capabilities);
                capabilities.MarkDirty();
            }
        }

        private static async Task ApplyRelics(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetInventory relics,
            bool applyInternalValues)
        {
            if (relics.ApplyMode == RitsuDebugStatePresetApplyMode.Replace)
                await RitsuDebugInventoryActions.ExecuteClearInventoryAsync(
                    context,
                    new(RitsuDebugInventoryKind.Relics));
            foreach (var modelId in relics.ModelIds)
            {
                _ = RitsuDebugInventoryActions.TryResolveRelic(modelId, out var model, out _);
                if (context.Target.GetRelicById(model.Id) == null)
                {
                    var values = applyInternalValues
                        ? relics.InternalValues?.GetValueOrDefault(modelId)
                        : null;
                    await RitsuDebugInventoryActions.ExecuteAddRelicAsync(
                        context,
                        new(modelId, values?.StackCount ?? 1, values?.DynamicVars));
                }
            }
        }

        private static async Task ApplyPotions(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetPotions potions,
            bool applyInternalValues)
        {
            if (potions.ApplyMode == RitsuDebugStatePresetApplyMode.Replace)
                await RitsuDebugInventoryActions.ExecuteClearInventoryAsync(
                    context,
                    new(RitsuDebugInventoryKind.Potions));
            foreach (var potion in potions.Items)
                await RitsuDebugInventoryActions.ExecuteAddPotionAtSlotAsync(
                    context,
                    new RitsuDebugInventoryActions.ModelValuesPayload(
                        potion.PotionId,
                        applyInternalValues ? potion.DynamicVars : null),
                    potions.ApplyMode == RitsuDebugStatePresetApplyMode.Replace
                        ? potion.SlotIndex!.Value
                        : -1);
        }

        private static async Task ApplyPowers(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetPowers powers,
            bool applyInternalValues)
        {
            if (powers.ApplyMode == RitsuDebugStatePresetApplyMode.Replace)
                foreach (var power in context.Target.Creature.Powers.ToArray())
                    await PowerCmd.Remove(power);
            foreach (var saved in powers.Items)
            {
                _ = RitsuDebugCombatActions.TryResolvePower(saved.PowerId, out var canonical, out _);
                var mutable = canonical.ToMutable();
                if (applyInternalValues)
                    RitsuDebugModelValueOverrides.Apply(mutable.DynamicVars, saved.DynamicVars);
                await PowerCmd.Apply(
                    new BlockingPlayerChoiceContext(),
                    mutable,
                    context.Target.Creature,
                    applyInternalValues ? saved.Amount : 1,
                    null,
                    null);
            }
        }

        private static async Task ApplyPlayer(
            RitsuDebugActionContext context,
            RitsuDebugStatePresetPlayer state,
            bool potionSlotsAlreadyApplied)
        {
            if (state.Gold.HasValue)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetGold, state.Gold.Value);
            if (state is { CurrentHp: { } currentHp, MaxHp: { } maxHp })
            {
                if (context.Target.Creature.CurrentHp > maxHp)
                    await SetPlayerValue(context, RitsuDebugPlayerOperation.SetCurrentHp, currentHp);
                if (context.Target.Creature.MaxHp != maxHp)
                    await SetPlayerValue(context, RitsuDebugPlayerOperation.SetMaxHp, maxHp);
                if (context.Target.Creature.CurrentHp != currentHp)
                    await SetPlayerValue(context, RitsuDebugPlayerOperation.SetCurrentHp, currentHp);
            }
            else if (state.CurrentHp.HasValue)
            {
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetCurrentHp, state.CurrentHp.Value);
            }
            else if (state.MaxHp.HasValue)
            {
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetMaxHp, state.MaxHp.Value);
            }

            if (state.MaxEnergy.HasValue)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetMaxEnergy, state.MaxEnergy.Value);
            if (!potionSlotsAlreadyApplied && state.PotionSlots.HasValue)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetPotionSlots, state.PotionSlots.Value);
            if (state.Energy.HasValue)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetEnergy, state.Energy.Value);
            if (state.Stars.HasValue)
                await SetPlayerValue(context, RitsuDebugPlayerOperation.SetStars, state.Stars.Value);
            if (!state.Block.HasValue || context.Target.Creature.Block == state.Block.Value)
                return;
            var difference = state.Block.Value - context.Target.Creature.Block;
            if (difference > 0)
                await CreatureCmd.GainBlock(context.Target.Creature, difference, ValueProp.Unpowered, null);
            else
#if STS2_AT_LEAST_0_109_0
                await CreatureCmd.LoseBlock(
                    new BlockingPlayerChoiceContext(),
                    context.Target.Creature,
                    -difference,
                    null);
#else
                await CreatureCmd.LoseBlock(context.Target.Creature, -difference);
#endif
        }

        private static async Task SetPlayerValue(
            RitsuDebugActionContext context,
            RitsuDebugPlayerOperation operation,
            int value)
        {
            await RitsuDebugPlayerActions.ExecuteModifyPlayerAsync(context, new(operation, value));
        }

        private static RitsuDebugActionFeedback InvalidEncoding()
        {
            return RitsuDebugActionFeedback.Create(
                "statePreset.encodingInvalid",
                "The received preset data is invalid.");
        }

        internal readonly record struct RitsuDebugStatePresetWirePayload(
            [property: JsonPropertyName("encoding")]
            int Encoding,
            [property: JsonPropertyName("data")] string Data);
    }
}
