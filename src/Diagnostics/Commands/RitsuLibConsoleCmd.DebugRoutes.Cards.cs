using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Diagnostics.DebugTools;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private static DebugConsoleGroup DebugPileGroup()
        {
            return DebugConsoleGroup.Branch(
                "pile",
                [
                    DebugConsoleAction.Create(
                        "clear",
                        static (_, player, args) => ProcessClearPile(player!, args),
                        static (command, player, args) => command.CompleteClearPile(player, args)),
                    DebugConsoleAction.Create(
                        "upgrade",
                        static (_, player, args) => ProcessUpgradePile(player!, args),
                        static (command, player, args) => command.CompleteUpgradePile(player, args)),
                ]);
        }

        private static DebugConsoleGroup DebugCardGroup()
        {
            return DebugConsoleGroup.Branch(
                "card",
                [
                    DebugConsoleAction.Create(
                        "create",
                        static (_, player, args) => ProcessCreateCard(player!, args),
                        static (command, player, args) => command.CompleteCreateCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "remove",
                        static (_, player, args) => ProcessRemoveCard(player!, args),
                        static (command, player, args) => command.CompleteCardLocationArguments(player, args, 6)),
                    DebugConsoleAction.Create(
                        "copy",
                        static (_, player, args) => ProcessCopyCard(player!, args),
                        static (command, player, args) => command.CompleteCopyCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "move",
                        static (_, player, args) => ProcessMoveCard(player!, args),
                        static (command, player, args) => command.CompleteMoveCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "upgrade",
                        static (_, player, args) => ProcessUpgradeCard(player!, args),
                        static (command, player, args) => command.CompleteUpgradeCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "edit",
                        static (_, player, args) => ProcessEditCard(player!, args),
                        static (command, player, args) => command.CompleteEditCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "enchant",
                        static (_, player, args) => ProcessEnchantCard(player!, args),
                        static (command, player, args) => command.CompleteEnchantCardArguments(player, args)),
                    DebugConsoleAction.Create(
                        "clear-enchantment",
                        static (_, player, args) => ProcessClearCardEnchantment(player!, args),
                        static (command, player, args) => command.CompleteCardLocationArguments(player, args, 6)),
                    DebugConsoleAction.Create(
                        "set-replay",
                        static (_, player, args) => ProcessSetReplayCount(player!, args),
                        static (command, player, args) => command.CompleteSetReplayArguments(player, args)),
                ]);
        }

        private static CmdResult ProcessClearPile(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 4 or > 5 ||
                !RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var target, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCardActions.SubmitModifyPile(
                issuingPlayer,
                target,
                pileType,
                RitsuDebugCardPileOperation.Clear));
        }

        private static CmdResult ProcessUpgradePile(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 5 or > 6 ||
                !RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType) ||
                !int.TryParse(args[4], out var levels))
                return new(false, DebugUsageText());
            if (!TryResolveTargetPlayer(issuingPlayer, args, 5, out var target, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCardActions.SubmitModifyPile(
                issuingPlayer,
                target,
                pileType,
                RitsuDebugCardPileOperation.Upgrade,
                levels));
        }

        private static CmdResult ProcessCopyCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 7 or > 8 ||
                !RitsuDebugCardActions.TryParseMutablePileType(args[5], out var destinationPile) ||
                !int.TryParse(args[6], out var count))
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    7,
                    out var target,
                    out var sourcePile,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);

            return ToCmdResult(RitsuDebugCardActions.SubmitCopyCard(
                issuingPlayer,
                target,
                sourcePile,
                cardIndex,
                expectedCardId,
                destinationPile,
                count));
        }

        private static CmdResult ProcessMoveCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 6 or > 7 ||
                !RitsuDebugCardActions.TryParseMutablePileType(args[5], out var destinationPile))
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    6,
                    out var target,
                    out var sourcePile,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);

            return ToCmdResult(RitsuDebugCardActions.SubmitMoveCard(
                issuingPlayer,
                target,
                sourcePile,
                cardIndex,
                expectedCardId,
                destinationPile));
        }

        private CompletionResult CompleteClearPile(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);
            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteUpgradePile(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);
            if (args.Length == 5)
                return CompleteCurrentArgument(["1", "2", "3"], args);
            return args.Length == 6
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteCopyCardArguments(Player? player, string[] args)
        {
            if (args.Length <= 5)
                return CompleteCardLocationArguments(player, args, -1);
            if (args.Length == 6)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);
            if (args.Length == 7)
                return CompleteCurrentArgument(["1", "2", "3", "5", "10"], args);
            return args.Length == 8
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteMoveCardArguments(Player? player, string[] args)
        {
            if (args.Length <= 5)
                return CompleteCardLocationArguments(player, args, -1);
            if (args.Length == 6)
                return CompleteCurrentArgument(
                    RitsuDebugCardActions.GetMutablePileNames()
                        .Where(static name => !name.Equals(nameof(PileType.Deck),
                            StringComparison.OrdinalIgnoreCase)),
                    args);
            return args.Length == 7
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }
    }
}
