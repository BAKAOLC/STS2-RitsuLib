using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Ui.Overlay;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private static DebugConsoleGroup DebugUiGroup()
        {
            return DebugConsoleGroup.Leaf(
                "ui",
                false,
                static (_, _, args) =>
                {
                    if (args.Length != 2)
                        return new(false, DebugUsageText());
                    var opened = RitsuOverlayHostService.TryOpenDebugTools(out var message);
                    return new(opened, message);
                });
        }

        private static DebugConsoleGroup DebugInspectGroup()
        {
            return DebugConsoleGroup.Branch(
                "inspect",
                [
                    DebugConsoleAction.Create("players", static (_, player, args) => InspectPlayers(player!, args)),
                    DebugConsoleAction.Create("creatures", static (_, _, args) => InspectCreatures(args)),
                    DebugConsoleAction.Create(
                        "cards",
                        static (_, player, args) => InspectCards(player!, args),
                        static (command, player, args) => command.CompleteInspectCards(player, args)),
                    DebugConsoleAction.Create(
                        "relics",
                        static (_, player, args) => InspectRelics(player!, args),
                        static (command, player, args) => command.CompleteOptionalPlayer(player, args)),
                    DebugConsoleAction.Create(
                        "potions",
                        static (_, player, args) => InspectPotions(player!, args),
                        static (command, player, args) => command.CompleteOptionalPlayer(player, args)),
                    DebugConsoleAction.Create(
                        "powers",
                        static (_, _, args) => InspectPowers(args),
                        static (command, _, args) => command.CompleteInspectPowers(args)),
                ]);
        }

        private static CmdResult InspectPlayers(Player issuingPlayer, string[] args)
        {
            if (args.Length != 3)
                return new(false, DebugUsageText());
            return new(true, string.Join(
                Environment.NewLine,
                issuingPlayer.RunState.Players.Select((player, index) =>
                    $"[{index}] character={player.Character.Id} " +
                    $"hp={player.Creature.CurrentHp}/{player.Creature.MaxHp} gold={player.Gold}")));
        }

        private static CmdResult InspectCreatures(string[] args)
        {
            if (args.Length != 3)
                return new(false, DebugUsageText());
            var creatures = CombatManager.Instance.DebugOnlyGetState()?.Creatures;
            if (creatures == null)
                return DebugFailure(
                    "console.activeCombatInspection",
                    "This inspection requires an active combat.");
            return new(true, string.Join(
                Environment.NewLine,
                creatures.Select(creature =>
                    $"creature={creature.CombatId?.ToString() ?? "none"} " +
                    $"model={creature.ModelId} side={creature.Side} " +
                    $"hp={creature.CurrentHp}/{creature.MaxHp} block={creature.Block}")));
        }

        private static CmdResult InspectCards(Player issuingPlayer, string[] args)
        {
            if (args.Length is not (4 or 5))
                return new(false, DebugUsageText());
            if (!RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
                return DebugFailure("card.unsupportedPile", "Unsupported pile '{0}'.", args[3]);
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var target, out var error))
                return DebugFailure(error);
            var pile = RitsuDebugCardActions.GetPile(target, pileType);
            return pile == null
                ? new(false, $"Pile {pileType} is unavailable.")
                : new(true, string.Join(
                    Environment.NewLine,
                    pile.Cards.Select((card, index) =>
                        $"[{index}] {card.Id} upgrade={card.CurrentUpgradeLevel} replay={card.BaseReplayCount}")));
        }

        private static CmdResult InspectRelics(Player issuingPlayer, string[] args)
        {
            if (args.Length is not (3 or 4))
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 3, out var target, out var error))
                return DebugFailure(error);
            return new(true, string.Join(
                Environment.NewLine,
                target.Relics.Select((relic, index) => $"[{index}] {relic.Id}")));
        }

        private static CmdResult InspectPotions(Player issuingPlayer, string[] args)
        {
            if (args.Length is not (3 or 4))
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 3, out var target, out var error))
                return DebugFailure(error);
            return new(true, string.Join(
                Environment.NewLine,
                Enumerable.Range(0, target.MaxPotionCount).Select(index =>
                    $"[{index}] {target.GetPotionAtSlotIndex(index)?.Id.ToString() ?? "empty"}")));
        }

        private static CmdResult InspectPowers(string[] args)
        {
            if (args.Length != 4 || !uint.TryParse(args[3], out var combatId))
                return new(false, DebugUsageText());
            var creature = RitsuDebugCombatActions.FindCreature(combatId);
            return creature == null
                ? new(false, $"Creature {combatId} was not found.")
                : new(true, string.Join(
                    Environment.NewLine,
                    creature.Powers.Select((power, index) => $"[{index}] {power.Id} amount={power.Amount}")));
        }

        private CompletionResult CompleteInspectCards(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);
            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteOptionalPlayer(Player? player, string[] args)
        {
            return args.Length == 4
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteInspectPowers(string[] args)
        {
            if (args.Length != 4)
                return base.GetArgumentCompletions(null, args);
            var ids = CombatManager.Instance.DebugOnlyGetState()?.Creatures
                .Where(static creature => creature.CombatId.HasValue)
                .Select(static creature => creature.CombatId!.Value.ToString()) ?? [];
            return CompleteCurrentArgument(ids, args);
        }
    }
}
