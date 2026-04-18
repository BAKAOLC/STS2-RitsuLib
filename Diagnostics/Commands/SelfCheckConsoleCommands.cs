using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Diagnostics.Commands
{
    /// <summary>
    ///     RitsuLib diagnostic console command entry.
    /// </summary>
    public sealed class RitsuLibConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] RootCommands = ["selfcheck"];
        private static readonly string[] SelfCheckActions = ["run", "open-output"];

        /// <inheritdoc />
        public override string CmdName => "ritsulib";

        /// <inheritdoc />
        public override string Args => "selfcheck run|open-output";

        /// <inheritdoc />
        public override string Description => "RitsuLib tools: selfcheck run/open-output.";

        /// <inheritdoc />
        public override bool IsNetworked => false;

        /// <inheritdoc />
        public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
        {
            if (args.Length <= 1)
            {
                var partial = args.Length == 0 ? string.Empty : args[0];
                return CompleteArgument(RootCommands, [], partial, CompletionType.Subcommand);
            }

            if (!args[0].Equals("selfcheck", StringComparison.OrdinalIgnoreCase))
                return base.GetArgumentCompletions(player, args);
            {
                var completed = args.Take(args.Length - 1).ToArray();
                var partial = args[^1];
                return CompleteArgument(SelfCheckActions, completed, partial);
            }
        }

        /// <inheritdoc />
        public override CmdResult Process(Player? issuingPlayer, string[] args)
        {
            if (args.Length < 2 || !args[0].Equals("selfcheck", StringComparison.OrdinalIgnoreCase))
                return new(false, "Usage: ritsulib selfcheck run|open-output");

            if (args[1].Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                var ok = SelfCheckBundleCoordinator.TryManualRunFromConsole(out var message);
                return new(ok, message);
            }

            if (!args[1].Equals("open-output", StringComparison.OrdinalIgnoreCase))
                return new(false, "Usage: ritsulib selfcheck run|open-output");
            SelfCheckBundleCoordinator.TryOpenOutputFolderFromSettings();
            return new(true, "Requested to open RitsuLib self-check output folder.");
        }
    }
}
