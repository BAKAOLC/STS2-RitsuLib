using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugCreaturePresetActions
    {
        internal const string ApplyPresetActionId = "combat.creature-preset.apply";
        internal const string AddPresetMonsterActionId = "combat.creature-preset.add";

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ApplyPayload>(
                ApplyPresetActionId,
                ValidateApplyPreset,
                ExecuteApplyPresetAsync);
            RitsuDebugActionProtocol.Register<PresetPayload>(
                AddPresetMonsterActionId,
                ValidateAddPresetMonster,
                ExecuteAddPresetMonsterAsync);
        }

        internal static RitsuDebugActionSubmission SubmitApplyPreset(
            Player requester,
            Player actionTarget,
            uint combatId,
            RitsuDebugCreaturePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            return Submit(
                requester,
                actionTarget,
                ApplyPresetActionId,
                new ApplyPayload(combatId, preset.Clone()));
        }

        internal static RitsuDebugActionSubmission SubmitAddPresetMonster(
            Player requester,
            Player actionTarget,
            RitsuDebugCreaturePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            return Submit(
                requester,
                actionTarget,
                AddPresetMonsterActionId,
                new PresetPayload(preset.Clone()));
        }

        internal static RitsuDebugActionCheck ValidateStoredPreset(RitsuDebugCreaturePreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            if (preset.Id is not { Length: 32 } || !Guid.TryParseExact(preset.Id, "N", out _) ||
                string.IsNullOrWhiteSpace(preset.Name) ||
                preset.Name.Length > RitsuDebugCreaturePresetStore.MaximumNameLength ||
                preset.Powers == null ||
                preset.CurrentHp < 1 || preset.MaxHp is < 1 or > RitsuDebugCombatActions.MaxAmount ||
                preset.CurrentHp > preset.MaxHp || preset.Block is < 0 or > RitsuDebugCombatActions.MaxAmount)
                return RitsuDebugActionCheck.Fail(
                    "creaturePreset.invalid",
                    "The creature preset is missing required data or contains unsupported values.");
            if (!RitsuDebugCombatActions.TryResolveMonster(preset.MonsterId, out _, out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (preset.Powers.Count > RitsuDebugStatePresetStore.MaximumPowers)
                return RitsuDebugActionCheck.Fail(
                    "creaturePreset.powerLimit",
                    "At most {0} powers can be stored in one creature preset.",
                    RitsuDebugStatePresetStore.MaximumPowers);

            var powerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var power in preset.Powers)
            {
                if (power == null || power.Amount is < 1 or > RitsuDebugCombatActions.MaxAmount)
                    return RitsuDebugActionCheck.Fail(
                        "creaturePreset.powerInvalid",
                        "A saved Power entry is invalid or duplicated.");
                if (!RitsuDebugCombatActions.TryResolvePower(power.PowerId, out var canonical, out feedback))
                    return RitsuDebugActionCheck.Fail(feedback);
                if (!powerIds.Add(canonical.Id.ToString()))
                    return RitsuDebugActionCheck.Fail(
                        "creaturePreset.powerInvalid",
                        "A saved Power entry is invalid or duplicated.");
            }

            return JsonSerializer.Serialize(preset).Length <= RitsuDebugActionProtocol.MaxActionPayloadCharacters
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "creaturePreset.dataLimit",
                    "The creature preset contains too much data to apply.");
        }

        private static RitsuDebugActionSubmission Submit<TPayload>(
            Player requester,
            Player actionTarget,
            string actionId,
            TPayload payload)
        {
            return RitsuDebugActionProtocol.Submit(
                requester,
                RitsuDebugActionProtocol.CreateEnvelope(actionId, requester, actionTarget, payload));
        }

        private static RitsuDebugActionCheck ValidateApplyPreset(
            RitsuDebugActionContext context,
            ApplyPayload payload)
        {
            var check = ValidateForCombat(payload.Preset);
            if (!check.Success)
                return check;
            var creature = RitsuDebugCombatActions.FindCreature(payload.CombatId);
            if (creature == null)
                return RitsuDebugActionCheck.Fail(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");
            if (creature.IsPlayer)
                return RitsuDebugActionCheck.Fail(
                    "creaturePreset.enemyRequired",
                    "Creature presets can only be applied to enemies.");
            return creature.CanReceivePowers || payload.Preset.Powers.Count == 0
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.cannotReceivePowers",
                    "The selected creature cannot receive powers right now.");
        }

        private static RitsuDebugActionCheck ValidateAddPresetMonster(
            RitsuDebugActionContext context,
            PresetPayload payload)
        {
            var check = ValidateForCombat(payload.Preset);
            if (!check.Success)
                return check;
            return CombatManager.Instance.DebugOnlyGetState()!.Enemies.Count < RitsuDebugCombatActions.MaxEnemyCount
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "combat.enemyLimit",
                    "These tools support at most {0} enemies in one combat.",
                    RitsuDebugCombatActions.MaxEnemyCount);
        }

        private static RitsuDebugActionCheck ValidateForCombat(RitsuDebugCreaturePreset preset)
        {
            if (!RitsuDebugCombatActions.TryRequireCombat(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return ValidateStoredPreset(preset);
        }

        private static async Task<string> ExecuteApplyPresetAsync(
            RitsuDebugActionContext context,
            ApplyPayload payload)
        {
            var creature = RitsuDebugCombatActions.FindCreature(payload.CombatId)!;
            await ApplyPreset(creature, payload.Preset);
            return $"Applied creature preset {payload.Preset.Name} to the selected enemy.";
        }

        private static async Task<string> ExecuteAddPresetMonsterAsync(
            RitsuDebugActionContext context,
            PresetPayload payload)
        {
            _ = RitsuDebugCombatActions.TryResolveMonster(payload.Preset.MonsterId, out var canonical, out _);
            RitsuDebugCombatActions.PreloadMonsterAssets(canonical);
            var combatState = CombatManager.Instance.DebugOnlyGetState()!;
            var slot = combatState.Encounter?.GetNextSlot(combatState);
            if (string.IsNullOrEmpty(slot))
                slot = null;
            var creature = await CreatureCmd.Add(canonical.ToMutable(), combatState, CombatSide.Enemy, slot);
            await ApplyPreset(creature, payload.Preset);
            if (slot == null)
                RitsuDebugCombatActions.RepositionSlotlessEnemies(combatState);
            return $"Added creature preset {payload.Preset.Name} to the current combat.";
        }

        private static async Task ApplyPreset(Creature creature, RitsuDebugCreaturePreset preset)
        {
            var currentHpSetFirst = preset.MaxHp < creature.CurrentHp;
            if (currentHpSetFirst && creature.CurrentHp != preset.CurrentHp)
                await CreatureCmd.SetCurrentHp(creature, preset.CurrentHp);
            if (creature.MaxHp != preset.MaxHp)
                await CreatureCmd.SetMaxHp(creature, preset.MaxHp);
            if (!currentHpSetFirst && creature.CurrentHp != preset.CurrentHp)
                await CreatureCmd.SetCurrentHp(creature, preset.CurrentHp);

            await RitsuDebugCombatActions.SetCreatureBlockAsync(creature, preset.Block);

            foreach (var power in creature.Powers.ToArray())
                await PowerCmd.Remove(power);
            var choiceContext = new BlockingPlayerChoiceContext();
            foreach (var power in preset.Powers)
            {
                _ = RitsuDebugCombatActions.TryResolvePower(power.PowerId, out var canonical, out _);
                await PowerCmd.Apply(choiceContext, canonical.ToMutable(), creature, power.Amount, null, null);
            }
        }

        internal readonly record struct ApplyPayload(uint CombatId, RitsuDebugCreaturePreset Preset);

        internal readonly record struct PresetPayload(RitsuDebugCreaturePreset Preset);
    }
}
