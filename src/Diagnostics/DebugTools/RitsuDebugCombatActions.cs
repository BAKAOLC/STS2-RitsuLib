using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Networking.Sidecar;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugCreatureOperation
    {
        Kill,
        Damage,
        Heal,
        GainBlock,
        SetCurrentHp,
        SetMaxHp,
        SetBlock,
        ClearPowers,
    }

    internal static class RitsuDebugCombatActions
    {
        internal const string ModifyCreatureActionId = "combat.creature.modify";
        internal const string AddMonsterActionId = "combat.monster.add";
        internal const string AddEncounterActionId = "combat.encounter.add";
        internal const string DefeatAllEnemiesActionId = "combat.enemies.defeat-all";
        internal const string ApplyPowerActionId = "combat.power.apply";
        internal const string ApplyPowerToCreaturesActionId = "combat.power.apply-many";
        internal const string RemovePowerActionId = "combat.power.remove";
        internal const string AdjustPowerInstanceActionId = "combat.power.adjust-instance";
        internal const string EditPowerInstanceActionId = "combat.power.edit-instance";
        internal const string RemovePowerInstanceActionId = "combat.power.remove-instance";
        internal const string StunMonsterActionId = "combat.monster.stun";
        internal const string SetMonsterIntentActionId = "combat.monster.intent.set";
        internal const string RandomMonsterIntentActionId = "combat.monster.intent.random-group";
        internal const string PerformMonsterIntentActionId = "combat.monster.intent.perform";
        internal const int MaxAmount = 999_999_999;
        internal const int MaxCreatureTargetCount = 128;
        internal const int MaxEnemyCount = 64;

        internal static bool IsMonsterIntentActionFor(
            RitsuDebugActionExecutionResult result,
            uint combatId)
        {
            try
            {
                return result.ActionId switch
                {
                    SetMonsterIntentActionId =>
                        JsonSerializer.Deserialize<MonsterIntentPayload>(result.PayloadJson).CombatId == combatId,
                    RandomMonsterIntentActionId =>
                        JsonSerializer.Deserialize<MonsterIntentGroupPayload>(result.PayloadJson).CombatId == combatId,
                    PerformMonsterIntentActionId or StunMonsterActionId =>
                        JsonSerializer.Deserialize<MonsterActionPayload>(result.PayloadJson).CombatId == combatId,
                    _ => false,
                };
            }
            catch (JsonException)
            {
                return false;
            }
        }

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModifyCreaturePayload>(
                ModifyCreatureActionId,
                ValidateModifyCreature,
                ExecuteModifyCreatureAsync);
            RitsuDebugActionProtocol.Register<RitsuDebugRunActions.ModelPayload>(
                AddMonsterActionId,
                ValidateAddMonster,
                ExecuteAddMonsterAsync);
            RitsuDebugActionProtocol.Register<RitsuDebugRunActions.ModelPayload>(
                AddEncounterActionId,
                ValidateAddEncounter,
                ExecuteAddEncounterAsync);
            RitsuDebugActionProtocol.Register<ConfirmedPayload>(
                DefeatAllEnemiesActionId,
                ValidateDefeatAllEnemies,
                ExecuteDefeatAllEnemiesAsync);
            RitsuDebugActionProtocol.Register<PowerPayload>(
                ApplyPowerActionId,
                ValidateApplyPower,
                ExecuteApplyPowerAsync);
            RitsuDebugActionProtocol.Register<PowerGroupPayload>(
                ApplyPowerToCreaturesActionId,
                ValidateApplyPowerToCreatures,
                ExecuteApplyPowerToCreaturesAsync);
            RitsuDebugActionProtocol.Register<PowerPayload>(
                RemovePowerActionId,
                ValidateRemovePower,
                ExecuteRemovePowerAsync);
            RitsuDebugActionProtocol.Register<PowerInstancePayload>(
                AdjustPowerInstanceActionId,
                ValidateAdjustPowerInstance,
                ExecuteAdjustPowerInstanceAsync);
            RitsuDebugActionProtocol.Register<PowerInstanceValuesPayload>(
                EditPowerInstanceActionId,
                ValidateEditPowerInstance,
                ExecuteEditPowerInstanceAsync);
            RitsuDebugActionProtocol.Register<PowerInstancePayload>(
                RemovePowerInstanceActionId,
                ValidateRemovePowerInstance,
                ExecuteRemovePowerInstanceAsync);
            RitsuDebugActionProtocol.Register<MonsterActionPayload>(
                StunMonsterActionId,
                ValidateStunMonster,
                ExecuteStunMonsterAsync,
                RitsuLibSidecarInternalPeerFeatures.MonsterIntentActionsV1);
            RitsuDebugActionProtocol.Register<MonsterIntentPayload>(
                SetMonsterIntentActionId,
                ValidateSetMonsterIntent,
                ExecuteSetMonsterIntentAsync,
                RitsuLibSidecarInternalPeerFeatures.MonsterIntentActionsV1);
            RitsuDebugActionProtocol.Register<MonsterIntentGroupPayload>(
                RandomMonsterIntentActionId,
                ValidateRandomMonsterIntent,
                ExecuteRandomMonsterIntentAsync,
                RitsuLibSidecarInternalPeerFeatures.MonsterIntentActionsV1);
            RitsuDebugActionProtocol.Register<MonsterActionPayload>(
                PerformMonsterIntentActionId,
                ValidatePerformMonsterIntent,
                ExecutePerformMonsterIntentAsync,
                RitsuLibSidecarInternalPeerFeatures.MonsterIntentActionsV1);
            RitsuDebugCreaturePresetActions.RegisterBuiltInActions();
        }

        internal static RitsuDebugActionSubmission SubmitModifyCreature(
            Player requester,
            Player actionTarget,
            uint combatId,
            RitsuDebugCreatureOperation operation,
            int value = 0)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ModifyCreatureActionId,
                requester,
                actionTarget,
                new ModifyCreaturePayload(combatId, operation, value));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitApplyPower(
            Player requester,
            Player actionTarget,
            uint combatId,
            string powerId,
            int amount,
            IReadOnlyDictionary<string, int>? dynamicVars = null)
        {
            return SubmitPower(requester, actionTarget, ApplyPowerActionId, combatId, powerId, amount, dynamicVars);
        }

        internal static RitsuDebugActionSubmission SubmitApplyPowerToCreatures(
            Player requester,
            Player actionTarget,
            IReadOnlyCollection<uint> combatIds,
            string powerId,
            int amount,
            IReadOnlyDictionary<string, int>? dynamicVars = null)
        {
            ArgumentNullException.ThrowIfNull(combatIds);
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ApplyPowerToCreaturesActionId,
                requester,
                actionTarget,
                new PowerGroupPayload([.. combatIds], powerId, amount, CopyOverrides(dynamicVars)));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitAddMonster(
            Player requester,
            Player actionTarget,
            string monsterId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                AddMonsterActionId,
                requester,
                actionTarget,
                new RitsuDebugRunActions.ModelPayload(monsterId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitAddEncounter(
            Player requester,
            Player actionTarget,
            string encounterId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                AddEncounterActionId,
                requester,
                actionTarget,
                new RitsuDebugRunActions.ModelPayload(encounterId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitDuplicateCreature(
            Player requester,
            Player actionTarget,
            Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.Monster == null
                ? RitsuDebugActionSubmission.Reject(
                    "combat.creatureModelRequired",
                    "Only creatures backed by a monster model can be copied.")
                : SubmitAddMonster(requester, actionTarget, creature.Monster.Id.ToString());
        }

        internal static RitsuDebugActionSubmission SubmitDefeatAllEnemies(
            Player requester,
            Player actionTarget)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                DefeatAllEnemiesActionId,
                requester,
                actionTarget,
                new ConfirmedPayload(true));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitRemovePower(
            Player requester,
            Player actionTarget,
            uint combatId,
            string powerId)
        {
            return SubmitPower(requester, actionTarget, RemovePowerActionId, combatId, powerId, 0);
        }

        internal static RitsuDebugActionSubmission SubmitAdjustPowerInstance(
            Player requester,
            Player actionTarget,
            uint combatId,
            int index,
            string powerId,
            int offset)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                AdjustPowerInstanceActionId,
                requester,
                actionTarget,
                new PowerInstancePayload(combatId, index, powerId, offset));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEditPowerInstance(
            Player requester,
            Player actionTarget,
            uint combatId,
            int index,
            string powerId,
            int? amount,
            IReadOnlyDictionary<string, int>? dynamicVars)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EditPowerInstanceActionId,
                requester,
                actionTarget,
                new PowerInstanceValuesPayload(
                    combatId,
                    index,
                    powerId,
                    amount,
                    CopyOverrides(dynamicVars)));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitRemovePowerInstance(
            Player requester,
            Player actionTarget,
            uint combatId,
            int index,
            string powerId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                RemovePowerInstanceActionId,
                requester,
                actionTarget,
                new PowerInstancePayload(combatId, index, powerId, 0));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitSetMonsterIntent(
            Player requester,
            Player actionTarget,
            uint combatId,
            string moveId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                SetMonsterIntentActionId,
                requester,
                actionTarget,
                new MonsterIntentPayload(combatId, moveId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitStunMonster(
            Player requester,
            Player actionTarget,
            uint combatId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                StunMonsterActionId,
                requester,
                actionTarget,
                new MonsterActionPayload(combatId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitRandomMonsterIntent(
            Player requester,
            Player actionTarget,
            uint combatId,
            string groupId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                RandomMonsterIntentActionId,
                requester,
                actionTarget,
                new MonsterIntentGroupPayload(combatId, groupId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitPerformMonsterIntent(
            Player requester,
            Player actionTarget,
            uint combatId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                PerformMonsterIntentActionId,
                requester,
                actionTarget,
                new MonsterActionPayload(combatId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static bool TryResolvePower(
            string input,
            out PowerModel power,
            out RitsuDebugActionFeedback feedback)
        {
            power = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "model.powerIdInvalid",
                    "The power ID is empty or too long.");
                return false;
            }

            var fullMatches = ModelDb.AllPowers
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                :
                [
                    .. ModelDb.AllPowers
                        .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                        .Take(2),
                ];
            if (matches.Length == 1)
            {
                power = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create(
                    "model.powerUnknown",
                    "Unknown power '{0}'.",
                    input)
                : RitsuDebugActionFeedback.Create(
                    "model.powerAmbiguous",
                    "The power ID '{0}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        internal static bool TryResolveMonster(
            string input,
            out MonsterModel monster,
            out RitsuDebugActionFeedback feedback)
        {
            monster = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "model.monsterIdInvalid",
                    "The monster ID is empty or too long.");
                return false;
            }

            var monsters = ModelDb.Monsters.ToArray();
            var fullMatches = monsters
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                :
                [
                    .. monsters
                        .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                        .Take(2),
                ];
            if (matches.Length == 1)
            {
                monster = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create(
                    "model.monsterUnknown",
                    "Unknown monster '{0}'.",
                    input)
                : RitsuDebugActionFeedback.Create(
                    "model.monsterAmbiguous",
                    "The monster ID '{0}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        internal static Creature? FindCreature(uint combatId)
        {
            return CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .FirstOrDefault(creature => creature.CombatId == combatId);
        }

        private static RitsuDebugActionSubmission SubmitPower(
            Player requester,
            Player actionTarget,
            string actionId,
            uint combatId,
            string powerId,
            int amount,
            IReadOnlyDictionary<string, int>? dynamicVars = null)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                actionId,
                requester,
                actionTarget,
                new PowerPayload(combatId, powerId, amount, CopyOverrides(dynamicVars)));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        private static Dictionary<string, int>? CopyOverrides(IReadOnlyDictionary<string, int>? values)
        {
            return values?.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value,
                StringComparer.Ordinal);
        }

        private static RitsuDebugActionCheck ValidateModifyCreature(
            RitsuDebugActionContext context,
            ModifyCreaturePayload payload)
        {
            if (!TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail(
                    "combat.invalidCreatureOperation",
                    "The creature operation is invalid.");

            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");

            switch (payload.Operation)
            {
                case RitsuDebugCreatureOperation.Kill when creature.IsPlayer:
                    return RitsuDebugActionCheck.Fail(
                        "combat.killPlayerUnsupported",
                        "Killing player characters is not supported.");
                case RitsuDebugCreatureOperation.Kill when creature.IsDead:
                    return RitsuDebugActionCheck.Fail(
                        "combat.creatureAlreadyDead",
                        "The selected creature is already dead.");
            }

            if (payload.Operation is not (RitsuDebugCreatureOperation.Kill or
                    RitsuDebugCreatureOperation.ClearPowers) &&
                payload.Value is < 0 or > MaxAmount)
                return RitsuDebugActionCheck.Fail(
                    "combat.amountRange",
                    "The amount must be between 0 and {0}.",
                    MaxAmount);

            return payload.Operation switch
            {
                RitsuDebugCreatureOperation.SetCurrentHp when payload.Value > creature.MaxHp =>
                    RitsuDebugActionCheck.Fail(
                        "combat.currentHpExceedsMax",
                        "Current HP cannot exceed the creature's max HP ({0}).",
                        creature.MaxHp),
                RitsuDebugCreatureOperation.SetCurrentHp when creature.IsPlayer && payload.Value == 0 =>
                    RitsuDebugActionCheck.Fail(
                        "combat.playerHpAboveZero",
                        "A player's current HP must remain above zero."),
                RitsuDebugCreatureOperation.SetMaxHp when payload.Value < Math.Max(1, creature.CurrentHp) =>
                    RitsuDebugActionCheck.Fail(
                        "combat.maxHpBelowCurrent",
                        "Max HP cannot be lower than the creature's current HP ({0}).",
                        creature.CurrentHp),
                _ => RitsuDebugActionCheck.Ok,
            };
        }

        private static RitsuDebugActionCheck ValidateAddMonster(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            if (!TryRequireCombat(out var feedback) ||
                !TryResolveMonster(payload.ModelId, out _, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            if (combatState.Enemies.Count >= MaxEnemyCount)
                return RitsuDebugActionCheck.Fail(
                    "combat.enemyLimit",
                    "These tools support at most {0} enemies in one combat.",
                    MaxEnemyCount);

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateAddEncounter(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            if (!TryRequireCombat(out var feedback) ||
                !RitsuDebugRunActions.TryResolveEncounter(payload.ModelId, out var encounter, out feedback) ||
                !TryCreateEncounterMonsters(encounter, context.Target, out var monsters, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);

            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            if (combatState.Enemies.Count + monsters.Length > MaxEnemyCount)
                return RitsuDebugActionCheck.Fail(
                    "combat.encounterWouldExceedEnemyLimit",
                    "Adding this encounter would exceed the limit of {0} enemies in one combat.",
                    MaxEnemyCount);

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateDefeatAllEnemies(
            RitsuDebugActionContext context,
            ConfirmedPayload payload)
        {
            if (!payload.Confirmed)
                return RitsuDebugActionCheck.Fail(
                    "combat.defeatNotConfirmed",
                    "Defeating all enemies was not confirmed.");
            if (!TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return CombatManager.Instance.DebugOnlyGetState()!.Enemies.Any(static creature => !creature.IsDead)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.noLivingEnemies",
                    "There are no living enemies in the current combat.");
        }

        private static RitsuDebugActionCheck ValidateApplyPower(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            if (!TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");
            if (!creature.CanReceivePowers)
                return RitsuDebugActionCheck.Fail(
                    "combat.cannotReceivePowers",
                    "The selected creature cannot receive powers right now.");
            if (!TryResolvePower(payload.PowerId, out var canonical, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            var overrideCheck = RitsuDebugModelValueOverrides.Validate(canonical.DynamicVars, payload.DynamicVars);
            if (!overrideCheck.Success)
                return overrideCheck;
            return payload.Amount is < 1 or > MaxAmount
                ? RitsuDebugActionCheck.Fail(
                    "combat.powerAmountRange",
                    "Power amount must be between 1 and {0}.",
                    MaxAmount)
                : RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateApplyPowerToCreatures(
            RitsuDebugActionContext context,
            PowerGroupPayload payload)
        {
            if (!TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (payload.CombatIds == null || payload.CombatIds.Length is < 1 or > MaxCreatureTargetCount)
                return RitsuDebugActionCheck.Fail(
                    "combat.powerTargetCountRange",
                    "Select between 1 and {0} creatures.",
                    MaxCreatureTargetCount);
            if (payload.CombatIds.Distinct().Count() != payload.CombatIds.Length)
                return RitsuDebugActionCheck.Fail(
                    "combat.duplicatePowerTargets",
                    "A creature can appear only once in a batch Power action.");
            if (!TryResolvePower(payload.PowerId, out var canonical, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            var overrideCheck = RitsuDebugModelValueOverrides.Validate(canonical.DynamicVars, payload.DynamicVars);
            if (!overrideCheck.Success)
                return overrideCheck;
            if (payload.Amount is < 1 or > MaxAmount)
                return RitsuDebugActionCheck.Fail(
                    "combat.powerAmountRange",
                    "Power amount must be between 1 and {0}.",
                    MaxAmount);

            foreach (var combatId in payload.CombatIds)
            {
                var creature = FindCreature(combatId);
                if (creature == null)
                    return RitsuDebugActionCheck.Fail(
                        "combat.creatureUnavailable",
                        "A selected creature is no longer available.");
                if (!creature.CanReceivePowers)
                    return RitsuDebugActionCheck.Fail(
                        "combat.cannotReceivePowers",
                        "A selected creature cannot receive powers right now.");
            }

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateRemovePower(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            if (!TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");
            if (!TryResolvePower(payload.PowerId, out var canonical, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return creature.Powers.Any(power => power.Id == canonical.Id)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.powerMissing",
                    "The selected creature does not have {0}.",
                    canonical.Id);
        }

        private static RitsuDebugActionCheck ValidateAdjustPowerInstance(
            RitsuDebugActionContext context,
            PowerInstancePayload payload)
        {
            if (payload.Offset is 0 or < -MaxAmount or > MaxAmount)
                return RitsuDebugActionCheck.Fail(
                    "combat.powerOffsetRange",
                    "Power adjustment must be between {0} and {1}, excluding zero.",
                    -MaxAmount,
                    MaxAmount);
            if (!TryResolvePowerInstance(payload, out var creature, out _, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return creature.CanReceivePowers
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.cannotReceivePowers",
                    "The selected creature cannot receive power changes right now.");
        }

        private static RitsuDebugActionCheck ValidateRemovePowerInstance(
            RitsuDebugActionContext context,
            PowerInstancePayload payload)
        {
            return TryResolvePowerInstance(payload, out _, out _, out var feedback)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(feedback);
        }

        private static RitsuDebugActionCheck ValidateEditPowerInstance(
            RitsuDebugActionContext context,
            PowerInstanceValuesPayload payload)
        {
            if (!payload.Amount.HasValue && (payload.DynamicVars == null || payload.DynamicVars.Count == 0))
                return RitsuDebugActionCheck.Fail(
                    "combat.powerEditEmpty",
                    "Change at least one Power value before applying the edit.");
            var reference = new PowerInstancePayload(payload.CombatId, payload.Index, payload.PowerId, 0);
            if (!TryResolvePowerInstance(reference, out var creature, out var power, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (payload.Amount is { } amount)
            {
                var minimum = power.AllowNegative ? -MaxAmount : 0;
                if (amount < minimum || amount > MaxAmount)
                    return RitsuDebugActionCheck.Fail(
                        "combat.powerValueRange",
                        "Power amount must be between {0} and {1}.",
                        minimum,
                        MaxAmount);
                if (amount != power.Amount && !creature.CanReceivePowers)
                    return RitsuDebugActionCheck.Fail(
                        "combat.cannotReceivePowers",
                        "The selected creature cannot receive power changes right now.");
            }

            return RitsuDebugModelValueOverrides.Validate(power.DynamicVars, payload.DynamicVars);
        }

        private static RitsuDebugActionCheck ValidateSetMonsterIntent(
            RitsuDebugActionContext context,
            MonsterIntentPayload payload)
        {
            if (!TryResolveMonsterActionTarget(payload.CombatId, out var creature, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (string.IsNullOrWhiteSpace(payload.MoveId) || payload.MoveId.Length > 128)
                return RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentIdInvalid",
                    "The selected monster intent is invalid.");

            return payload.MoveId is not ("UNSET_MOVE" or "STUNNED") &&
                   creature.Monster!.MoveStateMachine!.States.TryGetValue(payload.MoveId, out var state) &&
                   state is MoveState
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentUnavailable",
                    "The selected intent is not available for this monster.");
        }

        private static RitsuDebugActionCheck ValidateStunMonster(
            RitsuDebugActionContext context,
            MonsterActionPayload payload)
        {
            if (!TryResolveMonsterActionTarget(payload.CombatId, out var creature, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (creature.IsStunned)
                return RitsuDebugActionCheck.Fail(
                    "combat.monsterAlreadyStunned",
                    "The selected monster is already stunned.");
            return creature.Monster!.NextMove.CanTransitionAway
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentLocked",
                    "The selected monster's current intent cannot be interrupted.");
        }

        private static RitsuDebugActionCheck ValidateRandomMonsterIntent(
            RitsuDebugActionContext context,
            MonsterIntentGroupPayload payload)
        {
            if (!TryResolveMonsterActionTarget(payload.CombatId, out var creature, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (string.IsNullOrWhiteSpace(payload.GroupId) || payload.GroupId.Length > 128)
                return RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentGroupInvalid",
                    "The selected intent group is invalid.");
            var machine = creature.Monster!.MoveStateMachine!;
            if (!machine.States.TryGetValue(payload.GroupId, out var state) ||
                state is not RandomBranchState { States.Count: > 0 and <= 128 } group ||
                group.States.Any(branch => string.IsNullOrWhiteSpace(branch.stateId) ||
                                           !machine.States.ContainsKey(branch.stateId)))
                return RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentGroupUnavailable",
                    "The selected random intent group is not available for this monster.");
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidatePerformMonsterIntent(
            RitsuDebugActionContext context,
            MonsterActionPayload payload)
        {
            if (!TryResolveMonsterActionTarget(payload.CombatId, out var creature, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return creature.Monster!.NextMove.Id == "UNSET_MOVE"
                ? RitsuDebugActionCheck.Fail(
                    "combat.monsterIntentUnavailable",
                    "The selected monster does not currently have an intent to perform.")
                : RitsuDebugActionCheck.Ok;
        }

        private static async Task<string> ExecuteModifyCreatureAsync(
            RitsuDebugActionContext context,
            ModifyCreaturePayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            switch (payload.Operation)
            {
                case RitsuDebugCreatureOperation.Kill:
                    await CreatureCmd.Kill(creature, true);
                    if (creature is { IsPet: true, CombatState: { } petCombatState } &&
                        petCombatState.ContainsCreature(creature))
                    {
                        CombatManager.Instance.RemoveCreature(creature);
                        petCombatState.RemoveCreature(creature);
                    }

                    break;
                case RitsuDebugCreatureOperation.Damage:
                    await CreatureCmd.Damage(
                        new BlockingPlayerChoiceContext(),
                        creature,
                        payload.Value,
                        ValueProp.Unpowered,
                        null,
                        null);
                    await CombatManager.Instance.CheckWinCondition();
                    break;
                case RitsuDebugCreatureOperation.Heal:
                    await CreatureCmd.Heal(creature, payload.Value);
                    break;
                case RitsuDebugCreatureOperation.GainBlock:
                    await CreatureCmd.GainBlock(creature, payload.Value, ValueProp.Unpowered, null);
                    break;
                case RitsuDebugCreatureOperation.SetCurrentHp:
                    await CreatureCmd.SetCurrentHp(creature, payload.Value);
                    break;
                case RitsuDebugCreatureOperation.SetMaxHp:
                    await CreatureCmd.SetMaxHp(creature, payload.Value);
                    break;
                case RitsuDebugCreatureOperation.SetBlock:
                    await SetCreatureBlockAsync(creature, payload.Value);
                    break;
                case RitsuDebugCreatureOperation.ClearPowers:
                    foreach (var power in creature.Powers.ToArray())
                        await PowerCmd.Remove(power);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(payload.Operation));
            }

            return DescribeCreatureResult(payload.Operation);
        }

        private static Task<string> ExecuteSetMonsterIntentAsync(
            RitsuDebugActionContext context,
            MonsterIntentPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            var monster = creature.Monster!;
            var move = (MoveState)monster.MoveStateMachine!.States[payload.MoveId];
            monster.SetMoveImmediate(move, true);
            return Task.FromResult($"Changed the selected monster's intent to {payload.MoveId}.");
        }

        private static async Task<string> ExecuteStunMonsterAsync(
            RitsuDebugActionContext context,
            MonsterActionPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            await CreatureCmd.Stun(creature, creature.Monster!.NextMove.Id);
            return "Stunned the selected monster.";
        }

        private static Task<string> ExecuteRandomMonsterIntentAsync(
            RitsuDebugActionContext context,
            MonsterIntentGroupPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            var monster = creature.Monster!;
            var group = (RandomBranchState)monster.MoveStateMachine!.States[payload.GroupId];
            try
            {
                var move = ResolveRandomGroupMove(creature, group);
                monster.SetMoveImmediate(move, true);
                return Task.FromResult($"Randomly changed the selected monster's intent to {move.Id}.");
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Could not select an intent from group '{payload.GroupId}': {ex}");
                throw new RitsuDebugActionExecutionException(RitsuDebugActionFeedback.Create(
                    "combat.monsterIntentGroupUnavailable",
                    "The selected random intent group is not available for this monster."));
            }
        }

        private static async Task<string> ExecutePerformMonsterIntentAsync(
            RitsuDebugActionContext context,
            MonsterActionPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            var monster = creature.Monster!;
            var moveId = monster.NextMove.Id;
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (creatureNode != null)
                await creatureNode.PerformIntent();
            await monster.PerformMove();

            if (!await CombatManager.Instance.CheckWinCondition())
            {
                var combatState = CombatManager.Instance.DebugOnlyGetState();
                if (combatState != null && combatState.ContainsCreature(creature) && !creature.IsDead)
                    creature.PrepareForNextTurn(combatState.PlayerCreatures);
            }

            return $"Performed the selected monster's current intent ({moveId}).";
        }

        private static async Task<string> ExecuteAddMonsterAsync(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            _ = TryResolveMonster(payload.ModelId, out var canonical, out _);
            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            PreloadMonsterAssets(canonical);

            var slot = GetAvailableEncounterSlot(combatState);
            _ = await CreatureCmd.Add(canonical.ToMutable(), combatState, CombatSide.Enemy, slot);
            if (slot == null)
                RepositionSlotlessEnemies(combatState);

            return $"Added monster {canonical.Id} to the current combat.";
        }

        private static async Task<string> ExecuteAddEncounterAsync(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            _ = RitsuDebugRunActions.TryResolveEncounter(payload.ModelId, out var encounter, out _);
            _ = TryCreateEncounterMonsters(encounter, context.Target, out var monsters, out _);
            foreach (var monster in monsters)
                PreloadMonsterAssets(monster);

            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            var addedWithoutSlot = false;
            foreach (var monster in monsters)
            {
                var slot = GetAvailableEncounterSlot(combatState);
                if (slot == null)
                {
                    addedWithoutSlot = true;
                }

                _ = await CreatureCmd.Add(monster, combatState, CombatSide.Enemy, slot);
            }

            if (addedWithoutSlot)
                RepositionSlotlessEnemies(combatState);

            return monsters.Length == 1
                ? $"Added 1 enemy from encounter {encounter.Id}."
                : $"Added {monsters.Length} enemies from encounter {encounter.Id}.";
        }

        private static async Task<string> ExecuteDefeatAllEnemiesAsync(
            RitsuDebugActionContext context,
            ConfirmedPayload payload)
        {
            var enemies = CombatManager.Instance.DebugOnlyGetState()!.Enemies
                .Where(static creature => !creature.IsDead)
                .ToArray();
            await CreatureCmd.Kill(enemies, true);
            return enemies.Length == 1
                ? "Defeated the remaining enemy."
                : $"Defeated all {enemies.Length} remaining enemies.";
        }

        private static async Task<string> ExecuteApplyPowerAsync(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            _ = TryResolvePower(payload.PowerId, out var canonical, out _);
            var choiceContext = new BlockingPlayerChoiceContext();
            var mutable = canonical.ToMutable();
            var existing = PowerCmd.FindExistingInstanceForStacking(mutable, creature, null);
            RitsuDebugModelValueOverrides.Apply(mutable.DynamicVars, payload.DynamicVars);
            await PowerCmd.Apply(choiceContext, mutable, creature, payload.Amount, null, null);
            if (existing != null && payload.DynamicVars is { Count: > 0 })
            {
                RitsuDebugModelValueOverrides.Apply(existing.DynamicVars, payload.DynamicVars);
                existing.InvokeExecutionFinished();
            }

            return $"Applied {canonical.Id} to the selected creature.";
        }

        private static async Task<string> ExecuteApplyPowerToCreaturesAsync(
            RitsuDebugActionContext context,
            PowerGroupPayload payload)
        {
            _ = TryResolvePower(payload.PowerId, out var canonical, out _);
            var choiceContext = new BlockingPlayerChoiceContext();
            foreach (var combatId in payload.CombatIds)
            {
                var creature = FindCreature(combatId)!;
                var mutable = canonical.ToMutable();
                var existing = PowerCmd.FindExistingInstanceForStacking(mutable, creature, null);
                RitsuDebugModelValueOverrides.Apply(mutable.DynamicVars, payload.DynamicVars);
                await PowerCmd.Apply(choiceContext, mutable, creature, payload.Amount, null, null);
                if (existing != null && payload.DynamicVars is { Count: > 0 })
                {
                    RitsuDebugModelValueOverrides.Apply(existing.DynamicVars, payload.DynamicVars);
                    existing.InvokeExecutionFinished();
                }
            }

            return $"Applied {canonical.Id} to {payload.CombatIds.Length} creatures.";
        }

        private static async Task<string> ExecuteRemovePowerAsync(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            var creature = FindCreature(payload.CombatId)!;
            _ = TryResolvePower(payload.PowerId, out var canonical, out _);
            var ownedPower = creature.Powers.First(power => power.Id == canonical.Id);
            await PowerCmd.Remove(ownedPower);
            return $"Removed {canonical.Id} from the selected creature.";
        }

        private static async Task<string> ExecuteAdjustPowerInstanceAsync(
            RitsuDebugActionContext context,
            PowerInstancePayload payload)
        {
            _ = TryResolvePowerInstance(payload, out _, out var power, out _);
            await PowerCmd.ModifyAmount(
                new BlockingPlayerChoiceContext(),
                power,
                payload.Offset,
                null,
                null);
            return $"Adjusted {power.Id} by {payload.Offset}.";
        }

        private static async Task<string> ExecuteRemovePowerInstanceAsync(
            RitsuDebugActionContext context,
            PowerInstancePayload payload)
        {
            _ = TryResolvePowerInstance(payload, out _, out var power, out _);
            await PowerCmd.Remove(power);
            return $"Removed {power.Id} from the selected creature.";
        }

        private static async Task<string> ExecuteEditPowerInstanceAsync(
            RitsuDebugActionContext context,
            PowerInstanceValuesPayload payload)
        {
            var reference = new PowerInstancePayload(payload.CombatId, payload.Index, payload.PowerId, 0);
            _ = TryResolvePowerInstance(reference, out _, out var power, out _);
            RitsuDebugModelValueOverrides.Apply(power.DynamicVars, payload.DynamicVars);
            if (payload.Amount is { } amount && amount != power.Amount)
            {
                await PowerCmd.ModifyAmount(
                    new BlockingPlayerChoiceContext(),
                    power,
                    amount - power.Amount,
                    null,
                    null);
            }
            else
            {
                power.InvokeExecutionFinished();
            }

            return $"Updated values for {power.Id}.";
        }

        private static bool TryResolvePowerInstance(
            PowerInstancePayload payload,
            out Creature creature,
            out PowerModel power,
            out RitsuDebugActionFeedback feedback)
        {
            creature = null!;
            power = null!;
            if (!TryRequireCombat(out feedback))
                return false;
            creature = FindCreature(payload.CombatId)!;
            if (creature == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");
                return false;
            }

            if (payload.Index < 0 || payload.Index >= creature.Powers.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.powerInstanceChanged",
                    "The selected Power instance is no longer available.");
                return false;
            }

            power = creature.Powers[payload.Index];
            if (!string.Equals(power.Id.ToString(), payload.PowerId, StringComparison.Ordinal))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.powerInstanceChanged",
                    "The selected Power instance has changed. Refresh and try again.");
                return false;
            }

            feedback = default;
            return true;
        }

        private static bool TryCreateEncounterMonsters(
            EncounterModel canonical,
            Player target,
            out MonsterModel[] monsters,
            out RitsuDebugActionFeedback feedback)
        {
            try
            {
                var encounter = canonical.ToMutable();
                encounter.GenerateMonstersWithSlots(target.RunState);
                monsters =
                [
                    .. encounter.MonstersWithSlots
                        .Select(static entry => entry.Item1)
                        .Take(MaxEnemyCount + 1),
                ];
                switch (monsters.Length)
                {
                    case 0:
                        feedback = RitsuDebugActionFeedback.Create(
                            "combat.encounterEmpty",
                            "Encounter {0} does not generate any enemies.",
                            canonical.Id);
                        return false;
                    case > MaxEnemyCount:
                        feedback = RitsuDebugActionFeedback.Create(
                            "combat.encounterTooLarge",
                            "Encounter {0} generates more than {1} enemies.",
                            canonical.Id,
                            MaxEnemyCount);
                        monsters = [];
                        return false;
                }

                feedback = default;
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Could not generate enemies for encounter '{canonical.Id}': {ex}");
                monsters = [];
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.encounterGenerationFailed",
                    "The selected encounter could not generate enemies for the current run.");
                return false;
            }
        }

        internal static void PreloadMonsterAssets(MonsterModel monster)
        {
            foreach (var path in monster.AssetPaths)
                _ = PreloadManager.Cache.GetAsset<Resource>(path);
        }

        internal static bool TryRequireCombat(out RitsuDebugActionFeedback feedback)
        {
            if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
                CombatManager.Instance.DebugOnlyGetState() != null)
            {
                feedback = default;
                return true;
            }

            feedback = RitsuDebugActionFeedback.Create(
                "action.activeCombatRequired",
                "This change requires an active combat.");
            return false;
        }

        private static string DescribeCreatureResult(RitsuDebugCreatureOperation operation)
        {
            return operation switch
            {
                RitsuDebugCreatureOperation.Kill => "Defeated the selected creature.",
                RitsuDebugCreatureOperation.Damage => "Damaged the selected creature.",
                RitsuDebugCreatureOperation.Heal => "Healed the selected creature.",
                RitsuDebugCreatureOperation.GainBlock => "Granted block to the selected creature.",
                RitsuDebugCreatureOperation.SetCurrentHp => "Updated the selected creature's current HP.",
                RitsuDebugCreatureOperation.SetMaxHp => "Updated the selected creature's maximum HP.",
                RitsuDebugCreatureOperation.SetBlock => "Updated the selected creature's block.",
                RitsuDebugCreatureOperation.ClearPowers => "Removed all powers from the selected creature.",
                _ => "Updated the selected creature.",
            };
        }

        internal static void RepositionSlotlessEnemies(ICombatState combatState)
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null)
                return;

            var enemies = combatRoom.CreatureNodes
                .Where(static node => GodotObject.IsInstanceValid(node) &&
                                      GodotObject.IsInstanceValid(node.Visuals) &&
                                      node.Entity is { IsPlayer: false, PetOwner: null, IsDead: false })
                .Take(MaxEnemyCount)
                .ToArray();
            if (enemies.Length == 0)
                return;

            var scaling = combatState.Encounter?.GetCameraScaling() ?? 1f;
            if (!float.IsFinite(scaling) || scaling <= 0f)
                scaling = 1f;
            var availableWidth = 960f / scaling;
            var padding = 70f;
            var widths = enemies.Select(static node =>
            {
                var width = node.Visuals.Bounds.Size.X;
                return float.IsFinite(width) && width >= 0f ? width : 120f;
            }).ToArray();
            var creatureWidth = widths.Sum();
            var totalWidth = creatureWidth + (enemies.Length - 1) * padding;
            var startX = Math.Max((availableWidth - totalWidth) * 0.5f, 150f);
            var alternatingY = 0f;
            if (startX + totalWidth > availableWidth && enemies.Length > 1)
            {
                padding = Math.Max((availableWidth - 150f - creatureWidth) / (enemies.Length - 1), 5f);
                totalWidth = creatureWidth + (enemies.Length - 1) * padding;
                startX = (availableWidth - totalWidth) * 0.5f;
                if (padding < 30f)
                    alternatingY = float.Lerp(60f, 40f, (padding - 5f) / 25f);
            }

            var x = startX;
            for (var index = 0; index < enemies.Length; index++)
            {
                enemies[index].Position = new(
                    x + widths[index] * 0.5f,
                    200f - (index % 2 == 0 ? 0f : alternatingY));
                x += widths[index] + padding;
            }
        }

        private static string? GetAvailableEncounterSlot(ICombatState combatState)
        {
            var slot = combatState.Encounter?.GetNextSlot(combatState);
            var encounterSlots = NCombatRoom.Instance?.EncounterSlots;
            return !string.IsNullOrEmpty(slot) &&
                   encounterSlots != null &&
                   encounterSlots.HasNode(slot)
                ? slot
                : null;
        }

        internal static async Task SetCreatureBlockAsync(Creature creature, int block)
        {
            var difference = block - creature.Block;
            switch (difference)
            {
                case > 0:
                    await CreatureCmd.GainBlock(creature, difference, ValueProp.Unpowered, null);
                    break;
                case < 0:
#if STS2_AT_LEAST_0_109_0
                    await CreatureCmd.LoseBlock(new BlockingPlayerChoiceContext(), creature, -difference, null);
#else
                    await CreatureCmd.LoseBlock(creature, -difference);
#endif
                    break;
            }
        }

        private static bool TryResolveMonsterActionTarget(
            uint combatId,
            out Creature creature,
            out RitsuDebugActionFeedback feedback)
        {
            creature = null!;
            if (!TryRequireCombat(out feedback))
                return false;
            var candidate = FindCreature(combatId);
            if (candidate?.Monster?.MoveStateMachine == null || candidate.IsPlayer || candidate.IsDead)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.monsterIntentTargetRequired",
                    "Select a living enemy monster with available intents.");
                return false;
            }

            if (!TryValidateMonsterActionTarget(candidate, out feedback))
                return false;

            creature = candidate;
            return true;
        }

        private static MoveState ResolveRandomGroupMove(Creature creature, RandomBranchState group)
        {
            var monster = creature.Monster!;
            var machine = monster.MoveStateMachine!;
            MonsterState state = group;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (state is not MoveState)
            {
                if (!visited.Add(state.Id) || visited.Count > machine.States.Count)
                    throw new InvalidOperationException($"Intent group '{group.Id}' contains a branch cycle.");
                var nextStateId = state.GetNextState(creature, monster.RunRng.MonsterAi);
                if (string.IsNullOrWhiteSpace(nextStateId) ||
                    !machine.States.TryGetValue(nextStateId, out var nextState))
                    throw new InvalidOperationException(
                        $"Intent group '{group.Id}' resolved to unknown state '{nextStateId}'.");
                state = nextState;
            }

            return (MoveState)state;
        }

        private static bool TryValidateMonsterActionTarget(
            Creature creature,
            out RitsuDebugActionFeedback feedback)
        {
            if (creature.IsPlayer || creature.Monster?.MoveStateMachine == null || creature.IsDead)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.monsterIntentTargetRequired",
                    "Select a living enemy monster with available intents.");
                return false;
            }

            if (creature.CombatState?.CurrentSide != CombatSide.Player || creature.Monster.IsPerformingMove)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "combat.monsterActionPlayerTurnRequired",
                    "Monster intent actions are available during the player turn while the monster is idle.");
                return false;
            }

            feedback = default;
            return true;
        }

        internal readonly record struct ModifyCreaturePayload(
            uint CombatId,
            RitsuDebugCreatureOperation Operation,
            int Value);

        internal readonly record struct PowerPayload(
            uint CombatId,
            string PowerId,
            int Amount,
            Dictionary<string, int>? DynamicVars);

        internal readonly record struct PowerInstancePayload(uint CombatId, int Index, string PowerId, int Offset);

        internal readonly record struct PowerInstanceValuesPayload(
            uint CombatId,
            int Index,
            string PowerId,
            int? Amount,
            Dictionary<string, int>? DynamicVars);

        internal readonly record struct PowerGroupPayload(
            uint[] CombatIds,
            string PowerId,
            int Amount,
            Dictionary<string, int>? DynamicVars);

        internal readonly record struct MonsterIntentPayload(uint CombatId, string MoveId);

        internal readonly record struct MonsterIntentGroupPayload(uint CombatId, string GroupId);

        internal readonly record struct MonsterActionPayload(uint CombatId);

        internal readonly record struct ConfirmedPayload(bool Confirmed);
    }
}
