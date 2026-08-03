using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Diagnostics.DevConsole;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private static readonly DebugConsoleGroup[] DebugConsoleGroups =
        [
            DebugUiGroup(),
            DebugInspectGroup(),
            DebugPileGroup(),
            DebugCardGroup(),
            PlayerGroup(),
            DebugRelicGroup(),
            DebugPotionGroup(),
            CreatureGroup(),
            DebugMonsterGroup(),
            DebugPowerGroup(),
            DebugRoomGroup(),
            DebugEncounterGroup(),
            DebugEventGroup(),
        ];

        private static readonly IReadOnlyDictionary<string, DebugConsoleGroup> DebugConsoleGroupsByName =
            DebugConsoleGroups.ToDictionary(static group => group.Name, StringComparer.OrdinalIgnoreCase);

        private CmdResult RouteDebugCommand(Player? player, string[] args)
        {
            if (args.Length < 2 || !DebugConsoleGroupsByName.TryGetValue(args[1], out var group))
                return new(false, DebugUsageText());
            if (group.RequiresPlayer && player == null)
                return DebugFailure(
                    "console.playerRequired",
                    "A player is required to change or inspect run state.");

            if (group.Actions.Count == 0)
                return group.Execute!(this, player, args);
            if (args.Length < 3 || !group.ActionsByName.TryGetValue(args[2], out var action))
                return new(false, DebugUsageText());
            return action.Execute(this, player!, args);
        }

        private CompletionResult RouteDebugCompletion(Player? player, string[] args)
        {
            if (args.Length <= 2)
                return CompleteCurrentArgument(
                    DebugConsoleGroups.Select(static group => group.Name),
                    args,
                    CompletionType.Subcommand);
            if (!DebugConsoleGroupsByName.TryGetValue(args[1], out var group))
                return base.GetArgumentCompletions(player, args);
            if (group.Actions.Count == 0)
                return group.Complete?.Invoke(this, player, args) ?? base.GetArgumentCompletions(player, args);
            if (args.Length <= 3)
                return CompleteCurrentArgument(
                    group.Actions.Select(static action => action.Name),
                    args,
                    CompletionType.Subcommand);
            if (!group.ActionsByName.TryGetValue(args[2], out var action))
                return base.GetArgumentCompletions(player, args);
            return action.Complete?.Invoke(this, player, args) ?? base.GetArgumentCompletions(player, args);
        }

        private CompletionResult BaseDebugCompletion(Player? player, string[] args)
        {
            return base.GetArgumentCompletions(player, args);
        }

        private static DebugConsoleGroup PlayerGroup()
        {
            return DebugConsoleGroup.Branch(
                "player",
                true,
                [
                    PlayerAction("add-gold", RitsuDebugPlayerOperation.AddGold),
                    PlayerAction("set-gold", RitsuDebugPlayerOperation.SetGold),
                    PlayerAction("heal", RitsuDebugPlayerOperation.Heal),
                    PlayerAction("set-hp", RitsuDebugPlayerOperation.SetCurrentHp),
                    PlayerAction("set-max-hp", RitsuDebugPlayerOperation.SetMaxHp),
                    PlayerAction("gain-block", RitsuDebugPlayerOperation.GainBlock),
                    PlayerAction("add-energy", RitsuDebugPlayerOperation.AddEnergy),
                    PlayerAction("set-energy", RitsuDebugPlayerOperation.SetEnergy),
                    PlayerAction("add-stars", RitsuDebugPlayerOperation.AddStars),
                    PlayerAction("set-stars", RitsuDebugPlayerOperation.SetStars),
                    PlayerAction("set-max-energy", RitsuDebugPlayerOperation.SetMaxEnergy),
                    PlayerAction("set-potion-slots", RitsuDebugPlayerOperation.SetPotionSlots),
                    PlayerAction("draw", RitsuDebugPlayerOperation.Draw),
                ]);
        }

        private static DebugConsoleAction PlayerAction(string name, RitsuDebugPlayerOperation operation)
        {
            return DebugConsoleAction.Create(
                name,
                (_, player, args) => ProcessPlayerOperation(player!, args, operation),
                static (command, player, args) => command.CompletePlayerOperation(player, args));
        }

        private static DebugConsoleGroup CreatureGroup()
        {
            return DebugConsoleGroup.Branch(
                "creature",
                true,
                [
                    CreatureAction("kill", RitsuDebugCreatureOperation.Kill, false),
                    CreatureAction("damage", RitsuDebugCreatureOperation.Damage, true),
                    CreatureAction("heal", RitsuDebugCreatureOperation.Heal, true),
                    CreatureAction("gain-block", RitsuDebugCreatureOperation.GainBlock, true),
                    CreatureAction("set-hp", RitsuDebugCreatureOperation.SetCurrentHp, true),
                    CreatureAction("set-max-hp", RitsuDebugCreatureOperation.SetMaxHp, true),
                    CreatureAction("clear-powers", RitsuDebugCreatureOperation.ClearPowers, false),
                    DebugConsoleAction.Create(
                        "duplicate",
                        static (_, player, args) => ProcessDuplicateCreature(player!, args),
                        static (command, player, args) => command.CompleteCreatureIdentity(player, args)),
                    DebugConsoleAction.Create(
                        "defeat-all-enemies",
                        static (_, player, args) => ProcessDefeatAllEnemies(player!, args)),
                ]);
        }

        private static DebugConsoleAction CreatureAction(
            string name,
            RitsuDebugCreatureOperation operation,
            bool acceptsValue)
        {
            return DebugConsoleAction.Create(
                name,
                (_, player, args) => ProcessCreatureOperation(player!, args, operation, acceptsValue),
                (command, player, args) => command.CompleteCreatureOperation(player, args, acceptsValue));
        }

        private static DebugConsoleAction InventoryAction(string name, bool relic, bool add)
        {
            return DebugConsoleAction.Create(
                name,
                (_, player, args) => ProcessInventoryModel(player!, args, relic, add),
                (command, player, args) => command.CompleteInventoryModel(player, args, relic));
        }

        private static CmdResult ProcessPlayerOperation(
            Player issuingPlayer,
            string[] args,
            RitsuDebugPlayerOperation operation)
        {
            if (args.Length is < 4 or > 5 || !int.TryParse(args[3], out var value))
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var target, out var error))
                return DebugFailure(error);

            return ToCmdResult(RitsuDebugPlayerActions.Submit(issuingPlayer, target, operation, value));
        }

        private CompletionResult CompletePlayerOperation(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(["0", "1", "5", "10", "100"], args);
            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private static CmdResult ProcessCreatureOperation(
            Player issuingPlayer,
            string[] args,
            RitsuDebugCreatureOperation operation,
            bool acceptsValue)
        {
            if (args.Length < 4 || !uint.TryParse(args[3], out var combatId))
                return new(false, DebugUsageText());
            var value = 0;
            if (acceptsValue)
            {
                if (args.Length != 5 || !int.TryParse(args[4], out value))
                    return DebugFailure(
                        "console.creatureIntegerValue",
                        "This creature change requires an integer value.");
            }
            else if (args.Length != 4)
            {
                return DebugFailure(
                    "console.creatureNoValue",
                    "This creature change does not accept a value.");
            }

            return ToCmdResult(RitsuDebugCombatActions.SubmitModifyCreature(
                issuingPlayer,
                issuingPlayer,
                combatId,
                operation,
                value));
        }

        private CompletionResult CompleteCreatureOperation(Player? player, string[] args, bool acceptsValue)
        {
            if (args.Length == 4)
            {
                var ids = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                    .Where(static creature => creature.CombatId.HasValue)
                    .Select(static creature => creature.CombatId!.Value.ToString()) ?? [];
                return CompleteCurrentArgument(ids, args);
            }

            if (acceptsValue && args.Length == 5)
                return CompleteCurrentArgument(["0", "1", "5", "10", "100"], args);
            return base.GetArgumentCompletions(player, args);
        }

        private static CmdResult ProcessDuplicateCreature(Player issuingPlayer, string[] args)
        {
            if (args.Length != 4 || !uint.TryParse(args[3], out var combatId))
                return new(false, DebugUsageText());
            var creature = RitsuDebugCombatActions.FindCreature(combatId);
            if (creature == null)
                return DebugFailure(
                    "combat.creatureUnavailable",
                    "The selected creature is no longer available.");
            return ToCmdResult(RitsuDebugCombatActions.SubmitDuplicateCreature(
                issuingPlayer,
                issuingPlayer,
                creature));
        }

        private static CmdResult ProcessDefeatAllEnemies(Player issuingPlayer, string[] args)
        {
            return args.Length == 3
                ? ToCmdResult(RitsuDebugCombatActions.SubmitDefeatAllEnemies(issuingPlayer, issuingPlayer))
                : new(false, DebugUsageText());
        }

        private CompletionResult CompleteCreatureIdentity(Player? player, string[] args)
        {
            if (args.Length != 4)
                return base.GetArgumentCompletions(player, args);
            var ids = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature is { CombatId: not null, Monster: not null })
                .Select(static creature => creature.CombatId!.Value.ToString()) ?? [];
            return CompleteCurrentArgument(ids, args);
        }

        private static CmdResult ProcessInventoryModel(
            Player issuingPlayer,
            string[] args,
            bool relic,
            bool add)
        {
            if (args.Length is < 4 or > 5)
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var target, out var targetError))
                return DebugFailure(targetError);
            var modelInput = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (relic)
            {
                if (!RitsuDebugInventoryActions.TryResolveRelic(modelInput, out var model, out var modelError))
                    return DebugFailure(modelError);
                return ToCmdResult(add
                    ? RitsuDebugInventoryActions.SubmitAddRelic(issuingPlayer, target, model.Id.ToString())
                    : RitsuDebugInventoryActions.SubmitRemoveRelic(issuingPlayer, target, model.Id.ToString()));
            }

            if (!RitsuDebugInventoryActions.TryResolvePotion(modelInput, out var potion, out var potionError))
                return DebugFailure(potionError);
            return ToCmdResult(RitsuDebugInventoryActions.SubmitAddPotion(
                issuingPlayer,
                target,
                potion.Id.ToString()));
        }

        private CompletionResult CompleteInventoryModel(Player? player, string[] args, bool relic)
        {
            if (args.Length == 4)
            {
                var ids = relic
                    ? ModelDb.AllRelics.Select(static model => model.Id.Entry)
                    : ModelDb.AllPotions.Select(static model => model.Id.Entry);
                var result = CompleteArgument(
                    ids,
                    [.. args.Take(args.Length - 1)],
                    args[^1],
                    matchPredicate: DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch());
                DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);
                return result;
            }

            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private delegate CmdResult DebugConsoleExecute(
            RitsuLibConsoleCmd command,
            Player? player,
            string[] args);

        private delegate CompletionResult DebugConsoleComplete(
            RitsuLibConsoleCmd command,
            Player? player,
            string[] args);

        private sealed class DebugConsoleGroup
        {
            private DebugConsoleGroup(
                string name,
                bool requiresPlayer,
                DebugConsoleExecute? execute,
                DebugConsoleComplete? complete,
                IReadOnlyList<DebugConsoleAction> actions)
            {
                Name = name;
                RequiresPlayer = requiresPlayer;
                Execute = execute;
                Complete = complete;
                Actions = actions;
                ActionsByName = actions.ToDictionary(static action => action.Name, StringComparer.OrdinalIgnoreCase);
            }

            internal string Name { get; }
            internal bool RequiresPlayer { get; }
            internal DebugConsoleExecute? Execute { get; }
            internal DebugConsoleComplete? Complete { get; }
            internal IReadOnlyList<DebugConsoleAction> Actions { get; }
            internal IReadOnlyDictionary<string, DebugConsoleAction> ActionsByName { get; }

            internal static DebugConsoleGroup Leaf(
                string name,
                bool requiresPlayer,
                DebugConsoleExecute execute,
                DebugConsoleComplete? complete = null)
            {
                return new(name, requiresPlayer, execute, complete, []);
            }

            internal static DebugConsoleGroup Branch(
                string name,
                bool requiresPlayer,
                IReadOnlyList<DebugConsoleAction> actions)
            {
                return new(name, requiresPlayer, null, null, actions);
            }

            internal static DebugConsoleGroup Branch(
                string name,
                IReadOnlyList<DebugConsoleAction> actions)
            {
                return Branch(name, true, actions);
            }
        }

        private sealed record DebugConsoleAction(
            string Name,
            DebugConsoleExecute Execute,
            DebugConsoleComplete? Complete)
        {
            internal static DebugConsoleAction Create(
                string name,
                DebugConsoleExecute execute,
                DebugConsoleComplete? complete = null)
            {
                return new(name, execute, complete);
            }
        }
    }
}
