using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Diagnostics.DevConsole;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Dev-console command for inspecting and mutating secondary resources.
    ///     用于查看和修改次级资源的开发控制台指令。
    /// </summary>
    public sealed class SecondaryResourceConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] Actions = ["get", "gain", "lose", "set", "reset", "resetmax", "list"];

        /// <inheritdoc />
        public override string CmdName => "sresource";

        /// <inheritdoc />
        public override string Args =>
            "get <resourceId> [playerIndex] | gain|lose|set <resourceId> <amount:int> [playerIndex] | reset|resetmax <resourceId> [playerIndex] | list";

        /// <inheritdoc />
        public override string Description => "Inspect or mutate RitsuLib secondary resources.";

        /// <inheritdoc />
        public override bool IsNetworked => true;

        /// <inheritdoc />
        public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
        {
            if (args.Length <= 1)
            {
                var partial = args.Length == 0 ? string.Empty : args[0];
                return CompleteArgument(Actions, [], partial, CompletionType.Subcommand);
            }

            var action = args[0];
            if (!IsKnownAction(action) || action.Equals("list", StringComparison.OrdinalIgnoreCase))
                return base.GetArgumentCompletions(player, args);

            var completed = args.Take(args.Length - 1).ToArray();
            var partialArg = args[^1];
            if (args.Length == 2)
            {
                var result = CompleteArgument(
                    DevConsoleSecondaryResourceAutocompleteCatalog.GetResourceIds(),
                    completed,
                    partialArg,
                    matchPredicate: DevConsoleAutocompleteMatchExtensions.WithSecondaryResourceLocalizedTitleMatch());
                DevConsoleAutocompleteMatchExtensions.ApplySecondaryResourceDisplayLabels(ref result);
                return result;
            }

            if ((IsReadAction(action) && args.Length == 3) ||
                (IsMutationWithAmountAction(action) && args.Length == 4))
                return CompletePlayerIndex(player, completed, partialArg);

            return base.GetArgumentCompletions(player, args);
        }

        /// <inheritdoc />
        public override CmdResult Process(Player? issuingPlayer, string[] args)
        {
            if (args.Length == 0)
                return new(false, UsageText());

            var action = args[0];
            if (action.Equals("list", StringComparison.OrdinalIgnoreCase))
                return ListResources();

            if (!IsKnownAction(action) || !ValidateArity(action, args.Length))
                return new(false, UsageText());

            if (!TryResolveResource(args[1], out var definition, out var resourceError))
                return new(false, resourceError);

            var targetArgIndex = IsMutationWithAmountAction(action) ? 3 : 2;
            if (!TryResolveTargetPlayer(issuingPlayer, args, targetArgIndex, out var target, out var playerError))
                return new(false, playerError);

            if (action.Equals("get", StringComparison.OrdinalIgnoreCase))
                return GetResource(target, definition);

            if (action.Equals("reset", StringComparison.OrdinalIgnoreCase))
                return MutateResource(target, definition, SecondaryResourceCmd.Reset(target, definition.Id), "Reset");

            if (action.Equals("resetmax", StringComparison.OrdinalIgnoreCase))
                return MutateResource(target, definition, SecondaryResourceCmd.Reset(target, definition.Id, true),
                    "Reset");

            if (!int.TryParse(args[2], out var amount))
                return new(false, "Amount must be an int.");

            if (amount < 0 && !action.Equals("set", StringComparison.OrdinalIgnoreCase))
                return new(false, "Amount cannot be negative.");

            if (action.Equals("gain", StringComparison.OrdinalIgnoreCase))
                return MutateResource(target, definition, SecondaryResourceCmd.Gain(target, definition.Id, amount),
                    "Gained");

            if (action.Equals("lose", StringComparison.OrdinalIgnoreCase))
                return MutateResource(target, definition, SecondaryResourceCmd.Lose(target, definition.Id, amount),
                    "Lost");

            return MutateResource(target, definition, SecondaryResourceCmd.Set(target, definition.Id, amount), "Set");
        }

        private CompletionResult CompletePlayerIndex(Player? player, string[] completedArgs, string partialArg)
        {
            var candidates = GetCombatPlayers(player)
                .Select(static (_, index) => index.ToString())
                .ToArray();

            return CompleteArgument(candidates, completedArgs, partialArg);
        }

        private static CmdResult ListResources()
        {
            var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
            if (definitions.Length == 0)
                return new(true, "No secondary resources are registered.");

            var lines = definitions.Select(definition =>
            {
                var title = DevConsoleSecondaryResourceAutocompleteCatalog.TryGetLocalizedTitle(definition);
                return string.IsNullOrWhiteSpace(title)
                    ? definition.Id
                    : $"{definition.Id} ({title})";
            });
            return new(true, string.Join("\n", lines));
        }

        private static CmdResult GetResource(Player target, SecondaryResourceDefinition definition)
        {
            var amount = SecondaryResourceCmd.Get(target, definition.Id);
            var maxAmount = SecondaryResourceCmd.GetMax(target, definition.Id);
            var maxText = maxAmount.HasValue ? $"/{maxAmount.Value}" : string.Empty;
            return new(true, $"{definition.Id} for player {GetPlayerIndex(target)} = {amount}{maxText}.");
        }

        private static CmdResult MutateResource(
            Player target,
            SecondaryResourceDefinition definition,
            Task<int> task,
            string verb)
        {
            return new(task, true, $"{verb} {definition.Id} for player {GetPlayerIndex(target)}.");
        }

        private static bool TryResolveResource(
            string input,
            out SecondaryResourceDefinition definition,
            out string error)
        {
            if (DevConsoleSecondaryResourceAutocompleteCatalog.TryResolveResource(input, out definition))
            {
                error = string.Empty;
                return true;
            }

            error =
                $"Secondary resource '{DevConsoleAutocompleteDisplay.StripLocalizedSuffix(input)}' is not registered.";
            return false;
        }

        private static bool TryResolveTargetPlayer(
            Player? issuingPlayer,
            string[] args,
            int targetArgIndex,
            out Player target,
            out string error)
        {
            target = null!;
            if (issuingPlayer?.PlayerCombatState == null)
            {
                error = "This command only works in combat.";
                return false;
            }

            if (args.Length <= targetArgIndex)
            {
                target = issuingPlayer;
                error = string.Empty;
                return true;
            }

            if (!int.TryParse(args[targetArgIndex], out var targetIndex))
            {
                error = $"Target player index must be an int, got '{args[targetArgIndex]}'.";
                return false;
            }

            var players = GetCombatPlayers(issuingPlayer);
            if (targetIndex < 0 || targetIndex >= players.Count)
            {
                error = $"Invalid player index {targetIndex}. Valid range: 0-{players.Count - 1}.";
                return false;
            }

            target = players[targetIndex];
            error = string.Empty;
            return true;
        }

        private static IReadOnlyList<Player> GetCombatPlayers(Player? player)
        {
            return player?.Creature?.CombatState?.Players ??
                   CombatManager.Instance.DebugOnlyGetState()?.Players ??
                   [];
        }

        private static int GetPlayerIndex(Player player)
        {
            var players = GetCombatPlayers(player);
            for (var i = 0; i < players.Count; i++)
                if (ReferenceEquals(players[i], player))
                    return i;

            return -1;
        }

        private static bool ValidateArity(string action, int argCount)
        {
            if (action.Equals("get", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("reset", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("resetmax", StringComparison.OrdinalIgnoreCase))
                return argCount is 2 or 3;

            return IsMutationWithAmountAction(action) && argCount is 3 or 4;
        }

        private static bool IsKnownAction(string action)
        {
            return Actions.Any(candidate => candidate.Equals(action, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsReadAction(string action)
        {
            return action.Equals("get", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("reset", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("resetmax", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMutationWithAmountAction(string action)
        {
            return action.Equals("gain", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("lose", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("set", StringComparison.OrdinalIgnoreCase);
        }

        private static string UsageText()
        {
            return
                "Usage: sresource get <resourceId> [playerIndex] | sresource gain|lose|set <resourceId> <amount:int> [playerIndex] | sresource reset|resetmax <resourceId> [playerIndex] | sresource list";
        }
    }
}
