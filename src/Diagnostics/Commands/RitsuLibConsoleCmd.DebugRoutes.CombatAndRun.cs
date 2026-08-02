using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Diagnostics.DevConsole;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private static DebugConsoleGroup DebugMonsterGroup()
        {
            return DebugConsoleGroup.Branch(
                "monster",
                [
                    DebugConsoleAction.Create(
                        "add",
                        static (_, player, args) => ProcessDebugMonster(player!, args),
                        static (command, _, args) => command.CompleteModelIds(ModelDb.Monsters, args)),
                ]);
        }

        private static DebugConsoleGroup DebugPowerGroup()
        {
            return DebugConsoleGroup.Branch(
                "power",
                [
                    DebugConsoleAction.Create(
                        "apply",
                        static (_, player, args) => ProcessApplyPower(player!, args),
                        static (command, player, args) => command.CompleteApplyPower(player, args)),
                    DebugConsoleAction.Create(
                        "remove",
                        static (_, player, args) => ProcessRemovePower(player!, args),
                        static (command, player, args) => command.CompleteRemovePower(player, args)),
                ]);
        }

        private static DebugConsoleGroup DebugRoomGroup()
        {
            return DebugConsoleGroup.Leaf(
                "room",
                true,
                static (_, player, args) => ProcessDebugRoom(player!, args),
                static (command, player, args) => args.Length == 3
                    ? command.CompleteCurrentArgument(
                        Enum.GetNames<RoomType>().Where(static name => name != nameof(RoomType.Unassigned)),
                        args)
                    : command.BaseDebugCompletion(player, args));
        }

        private static DebugConsoleGroup DebugEncounterGroup()
        {
            return DebugConsoleGroup.Branch(
                "encounter",
                [
                    DebugConsoleAction.Create(
                        "enter",
                        static (_, player, args) => ProcessDebugEncounter(player!, args),
                        static (command, player, args) => args.Length == 4
                            ? command.CompleteModelIds(ModelDb.AllEncounters, args)
                            : command.BaseDebugCompletion(player, args)),
                    DebugConsoleAction.Create(
                        "add",
                        static (_, player, args) => ProcessAddEncounter(player!, args),
                        static (command, player, args) => args.Length == 4
                            ? command.CompleteModelIds(ModelDb.AllEncounters, args)
                            : command.BaseDebugCompletion(player, args)),
                ]);
        }

        private static CmdResult ProcessAddEncounter(Player issuingPlayer, string[] args)
        {
            if (args.Length != 4)
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugRunActions.TryResolveEncounter(input, out var encounter, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCombatActions.SubmitAddEncounter(
                issuingPlayer,
                issuingPlayer,
                encounter.Id.ToString()));
        }

        private static DebugConsoleGroup DebugEventGroup()
        {
            return DebugConsoleGroup.Leaf(
                "event",
                true,
                static (_, player, args) => ProcessDebugEvent(player!, args),
                static (command, player, args) => command.CompleteEventArguments(player, args));
        }

        private static CmdResult ProcessApplyPower(Player issuingPlayer, string[] args)
        {
            if (args.Length != 6 || !int.TryParse(args[4], out var amount) ||
                !uint.TryParse(args[5], out var combatId))
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugCombatActions.TryResolvePower(input, out var power, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCombatActions.SubmitApplyPower(
                issuingPlayer,
                issuingPlayer,
                combatId,
                power.Id.ToString(),
                amount));
        }

        private static CmdResult ProcessRemovePower(Player issuingPlayer, string[] args)
        {
            if (args.Length != 5 || !uint.TryParse(args[4], out var combatId))
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugCombatActions.TryResolvePower(input, out var power, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCombatActions.SubmitRemovePower(
                issuingPlayer,
                issuingPlayer,
                combatId,
                power.Id.ToString()));
        }

        private CompletionResult CompleteApplyPower(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompletePowerModel(args);
            if (args.Length == 5)
                return CompleteCurrentArgument(["1", "2", "3", "5", "10"], args);
            return args.Length == 6
                ? CompleteCombatCreature(args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteRemovePower(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompletePowerModel(args);
            return args.Length == 5
                ? CompleteCombatCreature(args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompletePowerModel(string[] args)
        {
            var result = CompleteArgument(
                ModelDb.AllPowers.Select(static model => model.Id.Entry),
                args.Take(args.Length - 1).ToArray(),
                args[^1],
                matchPredicate: DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch());
            DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);
            return result;
        }

        private CompletionResult CompleteCombatCreature(string[] args)
        {
            var ids = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature.CombatId.HasValue)
                .Select(static creature => creature.CombatId!.Value.ToString()) ?? [];
            return CompleteCurrentArgument(ids, args);
        }
    }
}
