using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Diagnostics.DebugTools;
using STS2RitsuLib.Diagnostics.DevConsole;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.Commands
{
    public sealed partial class RitsuLibConsoleCmd
    {
        private CmdResult ProcessDebug(Player? issuingPlayer, string[] args)
        {
            return RouteDebugCommand(issuingPlayer, args);
        }

        private static CmdResult ProcessCreateCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 4 or > 7)
                return new(false, DebugUsageText());

            var cardInput = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugCardActions.TryResolveCanonicalCard(cardInput, out var canonical, out var cardError))
                return DebugFailure(cardError);

            var pileType = PileType.Hand;
            if (args.Length >= 5 && !RitsuDebugCardActions.TryParseMutablePileType(args[4], out pileType))
                return DebugFailure("card.unsupportedPile", "Unsupported pile '{0}'.", args[4]);

            var upgradeLevels = 0;
            if (args.Length >= 6 && !int.TryParse(args[5], out upgradeLevels))
                return DebugFailure(
                    "console.upgradeLevelsInteger",
                    "Upgrade levels must be an int, got '{0}'.",
                    args[5]);

            if (!TryResolveTargetPlayer(issuingPlayer, args, 6, out var target, out var targetError))
                return DebugFailure(targetError);

            return ToCmdResult(RitsuDebugCardActions.SubmitCreateCard(
                issuingPlayer,
                target,
                canonical.Id.ToString(),
                pileType,
                1,
                upgradeLevels,
                default));
        }

        private static CmdResult ProcessSetReplayCount(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 6 or > 7)
                return new(false, DebugUsageText());

            if (!RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
                return DebugFailure("card.unsupportedPile", "Unsupported pile '{0}'.", args[3]);

            if (!int.TryParse(args[4], out var cardIndex))
                return DebugFailure("console.cardIndexInteger", "Card index must be an int, got '{0}'.", args[4]);

            if (!int.TryParse(args[5], out var replayCount))
                return DebugFailure(
                    "console.replayCountInteger",
                    "Replay count must be an int, got '{0}'.",
                    args[5]);

            if (!TryResolveTargetPlayer(issuingPlayer, args, 6, out var target, out var targetError))
                return DebugFailure(targetError);

            var pile = RitsuDebugCardActions.GetPile(target, pileType);
            if (pile == null)
                return DebugFailure(
                    "card.pileUnavailable",
                    "Pile '{0}' is unavailable for the selected player.",
                    pileType);

            if (cardIndex < 0 || cardIndex >= pile.Cards.Count)
                return DebugFailure(
                    "card.indexRange",
                    "Card index {0} is outside {1}'s range 0-{2}.",
                    cardIndex,
                    pileType,
                    pile.Cards.Count - 1);

            return ToCmdResult(RitsuDebugCardActions.SubmitSetReplayCount(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                pile.Cards[cardIndex].Id.ToString(),
                replayCount));
        }

        private static CmdResult ProcessRemoveCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 5 or > 6)
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    5,
                    out var target,
                    out var pileType,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);

            return ToCmdResult(RitsuDebugCardActions.SubmitRemoveCard(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                expectedCardId));
        }

        private static CmdResult ProcessEditCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 7 or > 9)
                return new(false, DebugUsageText());
            if (!RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
                return DebugFailure("card.unsupportedPile", "Unsupported pile '{0}'.", args[3]);
            if (!int.TryParse(args[4], out var cardIndex))
                return DebugFailure("console.cardIndexInteger", "Card index must be an int, got '{0}'.", args[4]);
            if (!TryParseCardEditField(args[5], out var field))
                return DebugFailure(
                    "console.unknownCardEditField",
                    "Unknown card edit field '{0}'.",
                    args[5]);

            string? dynamicVarKey = null;
            var valueIndex = 6;
            var playerIndex = 7;
            if (field == RitsuDebugCardEditField.DynamicVar)
            {
                if (args.Length < 8)
                    return DebugFailure(
                        "console.dynamicVarNeedsKeyValue",
                        "Dynamic-variable edits require a key and a value.");
                dynamicVarKey = args[6];
                valueIndex = 7;
                playerIndex = 8;
            }

            if (!int.TryParse(args[valueIndex], out var value))
                return DebugFailure(
                    "console.cardEditInteger",
                    "Card edit value must be an int, got '{0}'.",
                    args[valueIndex]);
            if (!TryResolveTargetPlayer(issuingPlayer, args, playerIndex, out var target, out var targetError))
                return DebugFailure(targetError);

            var pile = RitsuDebugCardActions.GetPile(target, pileType);
            if (pile == null || cardIndex < 0 || cardIndex >= pile.Cards.Count)
                return DebugFailure(
                    "console.cardIndexInvalid",
                    "Card index {0} is invalid for {1}.",
                    cardIndex,
                    pileType);

            return ToCmdResult(RitsuDebugCardActions.SubmitEditCard(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                pile.Cards[cardIndex].Id.ToString(),
                field,
                value,
                dynamicVarKey));
        }

        private static CmdResult ProcessEnchantCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 7 or > 8)
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    7,
                    out var target,
                    out var pileType,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[5]);
            if (!RitsuDebugCardActions.TryResolveEnchantment(input, out var enchantment, out error))
                return DebugFailure(error);
            if (!int.TryParse(args[6], out var amount))
                return DebugFailure(
                    "console.enchantmentAmountInteger",
                    "Enchantment amount must be an int, got '{0}'.",
                    args[6]);
            return ToCmdResult(RitsuDebugCardActions.SubmitEnchantCard(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                expectedCardId,
                enchantment.Id.ToString(),
                amount));
        }

        private static CmdResult ProcessClearCardEnchantment(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 5 or > 6)
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    5,
                    out var target,
                    out var pileType,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCardActions.SubmitClearCardEnchantment(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                expectedCardId));
        }

        private static CmdResult ProcessUpgradeCard(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 6 or > 7 || !int.TryParse(args[5], out var levels))
                return new(false, DebugUsageText());
            if (!TryResolveCardLocation(
                    issuingPlayer,
                    args,
                    3,
                    4,
                    6,
                    out var target,
                    out var pileType,
                    out var cardIndex,
                    out var expectedCardId,
                    out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCardActions.SubmitUpgradeCard(
                issuingPlayer,
                target,
                pileType,
                cardIndex,
                expectedCardId,
                levels));
        }

        private static CmdResult ProcessDebugRoom(Player issuingPlayer, string[] args)
        {
            if (args.Length != 3 || !Enum.TryParse<RoomType>(args[2], true, out var roomType))
                return new(false, DebugUsageText());
            return ToCmdResult(RitsuDebugRunActions.SubmitEnterRoom(issuingPlayer, roomType));
        }

        private static CmdResult ProcessDebugEncounter(Player issuingPlayer, string[] args)
        {
            if (args.Length != 4)
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugRunActions.TryResolveEncounter(input, out var encounter, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugRunActions.SubmitEnterEncounter(
                issuingPlayer,
                encounter.Id.ToString()));
        }

        private static CmdResult ProcessDebugMonster(Player issuingPlayer, string[] args)
        {
            if (args.Length != 4 || !args[2].Equals("add", StringComparison.OrdinalIgnoreCase))
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]);
            if (!RitsuDebugCombatActions.TryResolveMonster(input, out var monster, out var error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugCombatActions.SubmitAddMonster(
                issuingPlayer,
                issuingPlayer,
                monster.Id.ToString()));
        }

        private static CmdResult ProcessDebugEvent(Player issuingPlayer, string[] args)
        {
            if (args.Length is < 3 or > 5)
                return new(false, DebugUsageText());
            var input = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[2]);
            if (!RitsuDebugRunActions.TryResolveEvent(input, out var eventModel, out var error))
                return DebugFailure(error);
            var ancientOption = args.Length >= 4 ? args[3] : null;
            if (!TryResolveTargetPlayer(issuingPlayer, args, 4, out var historyPlayer, out error))
                return DebugFailure(error);
            return ToCmdResult(RitsuDebugRunActions.SubmitEnterEvent(
                issuingPlayer,
                historyPlayer,
                eventModel.Id.ToString(),
                ancientOption));
        }

        private CompletionResult CompleteDebugArguments(Player? player, string[] args)
        {
            return RouteDebugCompletion(player, args);
        }

        private CompletionResult CompleteCreateCardArguments(Player? player, string[] args)
        {
            if (args.Length == 4)
            {
                var completed = args.Take(args.Length - 1).ToArray();
                var partial = args[^1];
                var result = CompleteArgument(
                    ModelDb.AllCards.Select(static card => card.Id.Entry),
                    completed,
                    partial,
                    matchPredicate: DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch());
                DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);
                return result;
            }

            if (args.Length == 5)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);

            if (args.Length == 6)
            {
                var maxLevel = RitsuDebugCardActions.TryResolveCanonicalCard(
                    DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[3]),
                    out var card,
                    out _)
                    ? card.MaxUpgradeLevel
                    : 1;
                return CompleteCurrentArgument(
                    Enumerable.Range(0, Math.Max(0, maxLevel) + 1).Select(static value => value.ToString()),
                    args);
            }

            return args.Length == 7
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteSetReplayArguments(Player? player, string[] args)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);

            if (args.Length == 5 && player != null &&
                RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
            {
                var pile = RitsuDebugCardActions.GetPile(player, pileType);
                var candidates = pile == null
                    ? []
                    : Enumerable.Range(0, pile.Cards.Count).Select(static value => value.ToString());
                return CompleteCurrentArgument(candidates, args);
            }

            if (args.Length == 6)
                return CompleteCurrentArgument(["0", "1", "2", "3"], args);

            return args.Length == 7
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteCardLocationArguments(Player? player, string[] args, int playerLength)
        {
            if (args.Length == 4)
                return CompleteCurrentArgument(RitsuDebugCardActions.GetMutablePileNames(), args);
            if (args.Length == 5 && player != null &&
                RitsuDebugCardActions.TryParseMutablePileType(args[3], out var pileType))
            {
                var pile = RitsuDebugCardActions.GetPile(player, pileType);
                return CompleteCurrentArgument(
                    pile == null ? [] : Enumerable.Range(0, pile.Cards.Count).Select(static index => index.ToString()),
                    args);
            }

            return args.Length == playerLength
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteEditCardArguments(Player? player, string[] args)
        {
            if (args.Length <= 5)
                return CompleteCardLocationArguments(player, args, -1);
            if (args.Length == 6)
                return CompleteCurrentArgument(GetCardEditFieldNames(), args);
            if (!TryParseCardEditField(args[5], out var field))
                return base.GetArgumentCompletions(player, args);
            if (field == RitsuDebugCardEditField.DynamicVar)
            {
                if (args.Length == 7 && player != null &&
                    TryGetCardAt(player, args[3], args[4], out var card))
                    return CompleteCurrentArgument(card.DynamicVars.Keys, args);
                if (args.Length == 8)
                    return CompleteCurrentArgument(["0", "1", "2", "3", "5", "10"], args);
                return args.Length == 9
                    ? CompletePlayerIndex(player, args)
                    : base.GetArgumentCompletions(player, args);
            }

            if (args.Length == 7)
                return CompleteCurrentArgument(
                    field is RitsuDebugCardEditField.Exhaust or RitsuDebugCardEditField.Ethereal or
                        RitsuDebugCardEditField.Unplayable
                        ? ["0", "1"]
                        : ["0", "1", "2", "3", "5", "10"],
                    args);
            return args.Length == 8
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteEnchantCardArguments(Player? player, string[] args)
        {
            if (args.Length <= 5)
                return CompleteCardLocationArguments(player, args, -1);
            if (args.Length == 6)
                return CompleteModelIds(ModelDb.DebugEnchantments, args);
            if (args.Length == 7)
                return CompleteCurrentArgument(["1", "2", "3", "5", "10"], args);
            return args.Length == 8
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteUpgradeCardArguments(Player? player, string[] args)
        {
            if (args.Length <= 5)
                return CompleteCardLocationArguments(player, args, -1);
            if (args.Length == 6)
                return CompleteCurrentArgument(["1", "2", "3"], args);
            return args.Length == 7
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);
        }

        private CompletionResult CompleteEventArguments(Player? player, string[] args)
        {
            if (args.Length == 3)
                return CompleteModelIds(
                    ModelDb.AllEvents.Concat(ModelDb.AllAncients).DistinctBy(static model => model.Id),
                    args);
            if (args.Length == 4 &&
                RitsuDebugRunActions.TryResolveEvent(
                    DevConsoleAutocompleteDisplay.StripLocalizedSuffix(args[2]),
                    out var eventModel,
                    out _) &&
                eventModel is AncientEventModel ancient &&
                player != null)
            {
                var tokens = player.RunState.Players
                    .SelectMany(target => GetAvailableAncientOptionTokens(ancient, target))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                return CompleteCurrentArgument(
                    tokens,
                    args);
            }

            return args.Length == 5
                ? CompletePlayerIndex(player, args)
                : base.GetArgumentCompletions(player, args);

            static IEnumerable<string> GetAvailableAncientOptionTokens(
                AncientEventModel ancient,
                Player target)
            {
                return RitsuDebugRunActions.TryGetAvailableAncientOptions(
                    ancient,
                    target,
                    out var options,
                    out _)
                    ? options.Select(RitsuDebugRunActions.GetAncientOptionToken)
                    : [];
            }
        }

        private CompletionResult CompleteModelIds<TModel>(IEnumerable<TModel> models, string[] args)
            where TModel : AbstractModel
        {
            var result = CompleteArgument(
                models.Select(static model => model.Id.Entry),
                args.Take(args.Length - 1).ToArray(),
                args[^1],
                matchPredicate: DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch());
            DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);
            return result;
        }

        private CompletionResult CompletePlayerIndex(Player? player, string[] args)
        {
            var players = player?.RunState.Players ?? RunManager.Instance?.DebugOnlyGetState()?.Players ?? [];
            return CompleteCurrentArgument(
                Enumerable.Range(0, players.Count).Select(static value => value.ToString()),
                args);
        }

        private static bool TryResolveCardLocation(
            Player issuingPlayer,
            string[] args,
            int pileArgumentIndex,
            int cardIndexArgumentIndex,
            int playerArgumentIndex,
            out Player target,
            out PileType pileType,
            out int cardIndex,
            out string expectedCardId,
            out RitsuDebugActionFeedback feedback)
        {
            target = issuingPlayer;
            pileType = default;
            cardIndex = 0;
            expectedCardId = string.Empty;
            if (!RitsuDebugCardActions.TryParseMutablePileType(args[pileArgumentIndex], out pileType))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "card.unsupportedPile",
                    "Unsupported pile '{0}'.",
                    args[pileArgumentIndex]);
                return false;
            }

            if (!int.TryParse(args[cardIndexArgumentIndex], out cardIndex))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "console.cardIndexInteger",
                    "Card index must be an int, got '{0}'.",
                    args[cardIndexArgumentIndex]);
                return false;
            }

            if (!TryResolveTargetPlayer(issuingPlayer, args, playerArgumentIndex, out target, out feedback))
                return false;

            var pile = RitsuDebugCardActions.GetPile(target, pileType);
            if (pile == null || cardIndex < 0 || cardIndex >= pile.Cards.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "console.cardIndexInvalid",
                    "Card index {0} is invalid for {1}.",
                    cardIndex,
                    pileType);
                return false;
            }

            expectedCardId = pile.Cards[cardIndex].Id.ToString();
            feedback = default;
            return true;
        }

        private static bool TryGetCardAt(Player player, string pileInput, string indexInput, out CardModel card)
        {
            card = null!;
            if (!RitsuDebugCardActions.TryParseMutablePileType(pileInput, out var pileType) ||
                !int.TryParse(indexInput, out var cardIndex))
                return false;
            var pile = RitsuDebugCardActions.GetPile(player, pileType);
            if (pile == null || cardIndex < 0 || cardIndex >= pile.Cards.Count)
                return false;
            card = pile.Cards[cardIndex];
            return true;
        }

        private static bool TryParseCardEditField(string input, out RitsuDebugCardEditField field)
        {
            field = input.ToLowerInvariant() switch
            {
                "cost" => RitsuDebugCardEditField.Cost,
                "exhaust" => RitsuDebugCardEditField.Exhaust,
                "ethereal" => RitsuDebugCardEditField.Ethereal,
                "unplayable" => RitsuDebugCardEditField.Unplayable,
                "dynamic-var" => RitsuDebugCardEditField.DynamicVar,
                _ => (RitsuDebugCardEditField)(-1),
            };
            return Enum.IsDefined(field);
        }

        private static string[] GetCardEditFieldNames()
        {
            return
            [
                "cost", "exhaust", "ethereal", "unplayable", "dynamic-var",
            ];
        }

        private CompletionResult CompleteCurrentArgument(
            IEnumerable<string> candidates,
            string[] args,
            CompletionType completionType = CompletionType.Argument)
        {
            return CompleteArgument(
                candidates,
                args.Take(args.Length - 1).ToArray(),
                args[^1],
                completionType);
        }

        private static bool TryResolveTargetPlayer(
            Player issuingPlayer,
            string[] args,
            int argumentIndex,
            out Player target,
            out RitsuDebugActionFeedback feedback)
        {
            target = issuingPlayer;
            feedback = default;
            if (args.Length <= argumentIndex)
                return true;

            if (!int.TryParse(args[argumentIndex], out var playerIndex))
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "console.targetPlayerIndexInteger",
                    "Target player index must be an int, got '{0}'.",
                    args[argumentIndex]);
                return false;
            }

            var players = issuingPlayer.RunState.Players;
            if (playerIndex < 0 || playerIndex >= players.Count)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "console.targetPlayerIndexRange",
                    "Target player index {0} is outside the valid range 0-{1}.",
                    playerIndex,
                    players.Count - 1);
                return false;
            }

            target = players[playerIndex];
            return true;
        }

        private static CmdResult ToCmdResult(RitsuDebugActionSubmission submission)
        {
            return new(submission.Accepted, submission.Message);
        }

        private static CmdResult DebugFailure(RitsuDebugActionFeedback feedback)
        {
            return new(false, feedback.GetLocalizedText());
        }

        private static CmdResult DebugFailure(
            string code,
            string fallback,
            params object?[] arguments)
        {
            return DebugFailure(RitsuDebugActionFeedback.Create(code, fallback, arguments));
        }

        private static string DebugUsageText()
        {
            return string.Format(
                ModSettingsLocalization.Get(
                    "ritsulib.debugTools.feedback.console.usage",
                    "Usage: ritsulib debug <{0}> ...; use console completion to list actions and arguments."),
                string.Join('|', DebugConsoleGroups.Select(static group => group.Name)));
        }
    }
}
