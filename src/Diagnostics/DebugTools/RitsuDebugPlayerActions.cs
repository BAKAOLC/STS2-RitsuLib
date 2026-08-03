using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal enum RitsuDebugPlayerOperation
    {
        AddGold,
        SetGold,
        Heal,
        SetCurrentHp,
        SetMaxHp,
        GainBlock,
        AddEnergy,
        SetEnergy,
        AddStars,
        SetStars,
        SetMaxEnergy,
        SetPotionSlots,
        Draw,
    }

    internal static class RitsuDebugPlayerActions
    {
        internal const string ModifyPlayerActionId = "player.modify";
        internal const int MaxGold = 999_999;
        internal const int MaxHitPoints = 999_999;
        internal const int MaxCombatResource = 999;
        internal const int MaxPotionSlots = 20;
        internal const int MaxDrawCount = 100;

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<ModifyPlayerPayload>(
                ModifyPlayerActionId,
                ValidateModifyPlayer,
                ExecuteModifyPlayerAsync);
        }

        internal static RitsuDebugActionSubmission Submit(
            Player requester,
            Player target,
            RitsuDebugPlayerOperation operation,
            int value)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                ModifyPlayerActionId,
                requester,
                target,
                new ModifyPlayerPayload(operation, value));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionCheck ValidateModifyPlayer(
            RitsuDebugActionContext context,
            ModifyPlayerPayload payload)
        {
            if (!Enum.IsDefined(payload.Operation))
                return RitsuDebugActionCheck.Fail(
                    "player.invalidOperation",
                    "The player operation is invalid.");

            var validRange = payload.Operation switch
            {
                RitsuDebugPlayerOperation.AddGold => (-MaxGold, MaxGold),
                RitsuDebugPlayerOperation.SetGold => (0, MaxGold),
                RitsuDebugPlayerOperation.Heal => (0, MaxHitPoints),
                RitsuDebugPlayerOperation.SetCurrentHp => (1, MaxHitPoints),
                RitsuDebugPlayerOperation.SetMaxHp => (1, MaxHitPoints),
                RitsuDebugPlayerOperation.GainBlock => (0, MaxHitPoints),
                RitsuDebugPlayerOperation.AddEnergy => (0, MaxCombatResource),
                RitsuDebugPlayerOperation.SetEnergy => (0, MaxCombatResource),
                RitsuDebugPlayerOperation.AddStars => (0, MaxCombatResource),
                RitsuDebugPlayerOperation.SetStars => (0, MaxCombatResource),
                RitsuDebugPlayerOperation.SetMaxEnergy => (1, MaxCombatResource),
                RitsuDebugPlayerOperation.SetPotionSlots => (0, MaxPotionSlots),
                RitsuDebugPlayerOperation.Draw => (1, MaxDrawCount),
                _ => (1, 0),
            };
            if (payload.Value < validRange.Item1 || payload.Value > validRange.Item2)
                return RitsuDebugActionCheck.Fail(
                    "player.valueRange",
                    "The value must be between {0} and {1}.",
                    validRange.Item1,
                    validRange.Item2);

            if (payload.Operation == RitsuDebugPlayerOperation.AddGold &&
                (long)context.Target.Gold + payload.Value is < 0 or > MaxGold)
                return RitsuDebugActionCheck.Fail(
                    "player.goldResultRange",
                    "The resulting gold amount must be between 0 and {0}.",
                    MaxGold);

            if (payload.Operation is RitsuDebugPlayerOperation.AddEnergy or
                RitsuDebugPlayerOperation.SetEnergy or
                RitsuDebugPlayerOperation.AddStars or
                RitsuDebugPlayerOperation.SetStars or
                RitsuDebugPlayerOperation.GainBlock or
                RitsuDebugPlayerOperation.Draw)
                if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                    context.Target.PlayerCombatState == null)
                    return RitsuDebugActionCheck.Fail(
                        "player.activeCombatRequired",
                        "This player operation requires an active combat.");

            if (payload.Operation == RitsuDebugPlayerOperation.SetCurrentHp &&
                payload.Value > context.Target.Creature.MaxHp)
                return RitsuDebugActionCheck.Fail(
                    "player.currentHpExceedsMax",
                    "Current HP cannot exceed the target's max HP ({0}).",
                    context.Target.Creature.MaxHp);

            if (payload.Operation == RitsuDebugPlayerOperation.SetMaxHp &&
                payload.Value < context.Target.Creature.CurrentHp)
                return RitsuDebugActionCheck.Fail(
                    "player.maxHpBelowCurrent",
                    "Max HP cannot be lower than the target's current HP ({0}).",
                    context.Target.Creature.CurrentHp);

            return RitsuDebugActionCheck.Ok;
        }

        internal static async Task<string> ExecuteModifyPlayerAsync(
            RitsuDebugActionContext context,
            ModifyPlayerPayload payload)
        {
            var player = context.Target;
            switch (payload.Operation)
            {
                case RitsuDebugPlayerOperation.AddGold:
                    if (payload.Value >= 0)
                        await PlayerCmd.GainGold(payload.Value, player);
                    else
                        await PlayerCmd.LoseGold(-payload.Value, player);
                    break;
                case RitsuDebugPlayerOperation.SetGold:
                    await PlayerCmd.SetGold(payload.Value, player);
                    break;
                case RitsuDebugPlayerOperation.Heal:
                    await CreatureCmd.Heal(player.Creature, payload.Value);
                    break;
                case RitsuDebugPlayerOperation.SetCurrentHp:
                    await CreatureCmd.SetCurrentHp(player.Creature, payload.Value);
                    break;
                case RitsuDebugPlayerOperation.SetMaxHp:
                    await CreatureCmd.SetMaxHp(player.Creature, payload.Value);
                    break;
                case RitsuDebugPlayerOperation.GainBlock:
                    await CreatureCmd.GainBlock(player.Creature, payload.Value, ValueProp.Unpowered, null);
                    break;
                case RitsuDebugPlayerOperation.AddEnergy:
                    await PlayerCmd.GainEnergy(payload.Value, player);
                    break;
                case RitsuDebugPlayerOperation.SetEnergy:
                    player.PlayerCombatState!.Energy = payload.Value;
                    break;
                case RitsuDebugPlayerOperation.AddStars:
                    await PlayerCmd.GainStars(payload.Value, player);
                    break;
                case RitsuDebugPlayerOperation.SetStars:
                    player.PlayerCombatState!.Stars = payload.Value;
                    break;
                case RitsuDebugPlayerOperation.SetMaxEnergy:
                    player.MaxEnergy = payload.Value;
                    break;
                case RitsuDebugPlayerOperation.SetPotionSlots:
                    await SetPotionSlotsAsync(player, payload.Value);
                    break;
                case RitsuDebugPlayerOperation.Draw:
                    var choiceContext = new HookPlayerChoiceContext(
                        player,
                        player.NetId,
                        GameActionType.Combat);
                    await choiceContext.AssignTaskAndWaitForPauseOrCompletion(
                        CardPileCmd.Draw(choiceContext, payload.Value, player));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(payload.Operation));
            }

            return DescribeResult(payload.Operation, payload.Value);
        }

        private static Task SetPotionSlotsAsync(Player player, int count)
        {
            var difference = count - player.MaxPotionCount;
            return difference switch
            {
                > 0 => PlayerCmd.GainMaxPotionCount(difference, player),
                < 0 => PlayerCmd.LoseMaxPotionCount(-difference, player),
                _ => Task.CompletedTask,
            };
        }

        private static string DescribeOperation(RitsuDebugPlayerOperation operation)
        {
            return operation switch
            {
                RitsuDebugPlayerOperation.AddGold => "gold",
                RitsuDebugPlayerOperation.SetGold => "gold",
                RitsuDebugPlayerOperation.Heal => "health",
                RitsuDebugPlayerOperation.SetCurrentHp => "current HP",
                RitsuDebugPlayerOperation.SetMaxHp => "maximum HP",
                RitsuDebugPlayerOperation.GainBlock => "block",
                RitsuDebugPlayerOperation.AddEnergy => "energy",
                RitsuDebugPlayerOperation.SetEnergy => "energy",
                RitsuDebugPlayerOperation.AddStars => "stars",
                RitsuDebugPlayerOperation.SetStars => "stars",
                RitsuDebugPlayerOperation.SetMaxEnergy => "maximum energy",
                RitsuDebugPlayerOperation.SetPotionSlots => "potion slots",
                RitsuDebugPlayerOperation.Draw => "number of cards to draw",
                _ => "state",
            };
        }

        private static string DescribeResult(RitsuDebugPlayerOperation operation, int value)
        {
            return operation switch
            {
                RitsuDebugPlayerOperation.Heal => "Healed the selected player.",
                RitsuDebugPlayerOperation.GainBlock => "Granted block to the selected player.",
                RitsuDebugPlayerOperation.AddEnergy => "Added energy to the selected player.",
                RitsuDebugPlayerOperation.AddStars => "Added stars to the selected player.",
                RitsuDebugPlayerOperation.Draw => $"Drew {value} card(s) for the selected player.",
                _ => $"Updated the selected player's {DescribeOperation(operation)}.",
            };
        }

        internal readonly record struct ModifyPlayerPayload(
            RitsuDebugPlayerOperation Operation,
            int Value);
    }
}
