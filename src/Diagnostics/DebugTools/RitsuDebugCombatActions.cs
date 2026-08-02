using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

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
        ClearPowers,
    }

    internal static class RitsuDebugCombatActions
    {
        internal const string ModifyCreatureActionId = "combat.creature.modify";
        internal const string AddMonsterActionId = "combat.monster.add";
        internal const string AddEncounterActionId = "combat.encounter.add";
        internal const string DefeatAllEnemiesActionId = "combat.enemies.defeat-all";
        internal const string ApplyPowerActionId = "combat.power.apply";
        internal const string RemovePowerActionId = "combat.power.remove";
        internal const int MaxAmount = 999_999;
        private const int MaxEnemyCount = 64;

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
            RitsuDebugActionProtocol.Register<PowerPayload>(
                RemovePowerActionId,
                ValidateRemovePower,
                ExecuteRemovePowerAsync);
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
            int amount)
        {
            return SubmitPower(requester, actionTarget, ApplyPowerActionId, combatId, powerId, amount);
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
                ? new(false, "Only creatures backed by a monster model can be copied.")
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

        internal static bool TryResolvePower(string input, out PowerModel power, out string error)
        {
            power = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                error = "The power ID is empty or too long.";
                return false;
            }

            var fullMatches = ModelDb.AllPowers
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                : ModelDb.AllPowers
                    .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
            if (matches.Length == 1)
            {
                power = matches[0];
                error = string.Empty;
                return true;
            }

            error = matches.Length == 0
                ? $"Unknown power '{input}'."
                : $"The power ID '{input}' is ambiguous; use the full model ID.";
            return false;
        }

        internal static bool TryResolveMonster(string input, out MonsterModel monster, out string error)
        {
            monster = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                error = "The monster ID is empty or too long.";
                return false;
            }

            var monsters = ModelDb.Monsters.ToArray();
            var fullMatches = monsters
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = fullMatches.Length > 0
                ? fullMatches
                : monsters
                    .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
            if (matches.Length == 1)
            {
                monster = matches[0];
                error = string.Empty;
                return true;
            }

            error = matches.Length == 0
                ? $"Unknown monster '{input}'."
                : $"The monster ID '{input}' is ambiguous; use the full model ID.";
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
            int amount)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                actionId,
                requester,
                actionTarget,
                new PowerPayload(combatId, powerId, amount));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        private static RitsuDebugActionCheck ValidateModifyCreature(
            RitsuDebugActionContext context,
            ModifyCreaturePayload payload)
        {
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail("The creature operation is invalid.");

            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail("The selected creature is no longer available.");

            if (payload.Operation == RitsuDebugCreatureOperation.Kill && creature.IsPlayer)
                return RitsuDebugActionCheck.Fail("Killing player characters is not supported.");
            if (payload.Operation == RitsuDebugCreatureOperation.Kill && creature.IsDead)
                return RitsuDebugActionCheck.Fail("The selected creature is already dead.");

            if (payload.Operation is not (RitsuDebugCreatureOperation.Kill or
                    RitsuDebugCreatureOperation.ClearPowers) &&
                payload.Value is < 0 or > MaxAmount)
                return RitsuDebugActionCheck.Fail($"The amount must be between 0 and {MaxAmount}.");

            if (payload.Operation == RitsuDebugCreatureOperation.SetCurrentHp &&
                payload.Value > creature.MaxHp)
                return RitsuDebugActionCheck.Fail(
                    $"Current HP cannot exceed the creature's max HP ({creature.MaxHp}).");
            if (payload.Operation == RitsuDebugCreatureOperation.SetCurrentHp && creature.IsPlayer &&
                payload.Value == 0)
                return RitsuDebugActionCheck.Fail(
                    "A player's current HP must remain above zero.");
            if (payload.Operation == RitsuDebugCreatureOperation.SetMaxHp &&
                payload.Value < Math.Max(1, creature.CurrentHp))
                return RitsuDebugActionCheck.Fail(
                    $"Max HP cannot be lower than the creature's current HP ({creature.CurrentHp}).");

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateAddMonster(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!TryResolveMonster(payload.ModelId, out _, out error))
                return RitsuDebugActionCheck.Fail(error);

            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            if (combatState.Enemies.Count >= MaxEnemyCount)
                return RitsuDebugActionCheck.Fail(
                    $"These tools support at most {MaxEnemyCount} enemies in one combat.");

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateAddEncounter(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            if (!RitsuDebugRunActions.TryResolveEncounter(payload.ModelId, out var encounter, out error))
                return RitsuDebugActionCheck.Fail(error);
            if (!TryCreateEncounterMonsters(encounter, context.Target, out var monsters, out error))
                return RitsuDebugActionCheck.Fail(error);

            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            if (combatState.Enemies.Count + monsters.Length > MaxEnemyCount)
                return RitsuDebugActionCheck.Fail(
                    $"Adding this encounter would exceed the limit of {MaxEnemyCount} enemies in one combat.");

            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateDefeatAllEnemies(
            RitsuDebugActionContext context,
            ConfirmedPayload payload)
        {
            if (!payload.Confirmed)
                return RitsuDebugActionCheck.Fail("Defeating all enemies was not confirmed.");
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            return CombatManager.Instance.DebugOnlyGetState()!.Enemies.Any(static creature => !creature.IsDead)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail("There are no living enemies in the current combat.");
        }

        private static RitsuDebugActionCheck ValidateApplyPower(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail("The selected creature is no longer available.");
            if (!creature.CanReceivePowers)
                return RitsuDebugActionCheck.Fail("The selected creature cannot receive powers right now.");
            if (!TryResolvePower(payload.PowerId, out _, out error))
                return RitsuDebugActionCheck.Fail(error);
            return payload.Amount is < 1 or > MaxAmount
                ? RitsuDebugActionCheck.Fail($"Power amount must be between 1 and {MaxAmount}.")
                : RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateRemovePower(
            RitsuDebugActionContext context,
            PowerPayload payload)
        {
            if (!TryRequireCombat(out var error))
                return RitsuDebugActionCheck.Fail(error);
            var creature = FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail("The selected creature is no longer available.");
            if (!TryResolvePower(payload.PowerId, out var canonical, out error))
                return RitsuDebugActionCheck.Fail(error);
            return creature.Powers.Any(power => power.Id == canonical.Id)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail($"The selected creature does not have {canonical.Id}.");
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
                case RitsuDebugCreatureOperation.ClearPowers:
                    foreach (var power in creature.Powers.ToArray())
                        await PowerCmd.Remove(power);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(payload.Operation));
            }

            return DescribeCreatureResult(payload.Operation);
        }

        private static async Task<string> ExecuteAddMonsterAsync(
            RitsuDebugActionContext context,
            RitsuDebugRunActions.ModelPayload payload)
        {
            _ = TryResolveMonster(payload.ModelId, out var canonical, out _);
            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            PreloadMonsterAssets(canonical);

            var slot = combatState.Encounter?.GetNextSlot(combatState);
            if (string.IsNullOrEmpty(slot))
                slot = null;
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
            var currentEncounter = combatState.Encounter;
            var addedWithoutSlot = false;
            foreach (var monster in monsters)
            {
                var slot = currentEncounter?.GetNextSlot(combatState);
                if (string.IsNullOrEmpty(slot))
                {
                    slot = null;
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
            await PowerCmd.Apply(choiceContext, canonical.ToMutable(), creature, payload.Amount, null, null);

            return $"Applied {canonical.Id} to the selected creature.";
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

        private static bool TryCreateEncounterMonsters(
            EncounterModel canonical,
            Player target,
            out MonsterModel[] monsters,
            out string error)
        {
            try
            {
                var encounter = canonical.ToMutable();
                encounter.GenerateMonstersWithSlots(target.RunState);
                monsters = encounter.MonstersWithSlots
                    .Select(static entry => entry.Item1)
                    .Take(MaxEnemyCount + 1)
                    .ToArray();
                if (monsters.Length == 0)
                {
                    error = $"Encounter {canonical.Id} does not generate any enemies.";
                    return false;
                }

                if (monsters.Length > MaxEnemyCount)
                {
                    error = $"Encounter {canonical.Id} generates more than {MaxEnemyCount} enemies.";
                    monsters = [];
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Could not generate enemies for encounter '{canonical.Id}': {ex}");
                monsters = [];
                error = "The selected encounter could not generate enemies for the current run.";
                return false;
            }
        }

        private static void PreloadMonsterAssets(MonsterModel monster)
        {
            foreach (var path in monster.AssetPaths)
                _ = PreloadManager.Cache.GetAsset<Resource>(path);
        }

        private static bool TryRequireCombat(out string error)
        {
            if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
                CombatManager.Instance.DebugOnlyGetState() != null)
            {
                error = string.Empty;
                return true;
            }

            error = "This change requires an active combat.";
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
                RitsuDebugCreatureOperation.ClearPowers => "Removed all powers from the selected creature.",
                _ => "Updated the selected creature.",
            };
        }

        private static void RepositionSlotlessEnemies(ICombatState combatState)
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

        internal readonly record struct ModifyCreaturePayload(
            uint CombatId,
            RitsuDebugCreatureOperation Operation,
            int Value);

        internal readonly record struct PowerPayload(uint CombatId, string PowerId, int Amount);

        internal readonly record struct ConfirmedPayload(bool Confirmed);
    }
}
