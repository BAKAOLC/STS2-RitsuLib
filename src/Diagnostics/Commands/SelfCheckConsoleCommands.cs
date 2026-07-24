using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.Commands
{
    /// <summary>
    ///     RitsuLib diagnostic console command entry.
    ///     RitsuLib 诊断控制台命令入口。
    /// </summary>
    public sealed class RitsuLibConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] RootCommands = ["selfcheck", "settings"];
        private static readonly string[] SelfCheckActions = ["run", "open-output"];
        private static readonly string[] SettingsActions = ["open"];

        /// <inheritdoc />
        public override string CmdName => "ritsulib";

        /// <inheritdoc />
        public override string Args =>
            "selfcheck run|open-output OR settings open <modId> [pageId] [sectionId] [entryId]";

        /// <inheritdoc />
        public override string Description => "RitsuLib tools: selfcheck run/open-output; settings open.";

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

            if (args[0].Equals("settings", StringComparison.OrdinalIgnoreCase))
                return CompleteSettingsArguments(args);

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
            if (args.Length >= 1 && args[0].Equals("settings", StringComparison.OrdinalIgnoreCase))
                return ProcessSettings(args);

            if (args.Length < 2 || !args[0].Equals("selfcheck", StringComparison.OrdinalIgnoreCase))
                return new(false, UsageText());

            if (args[1].Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                var ok = SelfCheckBundleCoordinator.TryManualRunFromConsole(out var message);
                return new(ok, message);
            }

            if (!args[1].Equals("open-output", StringComparison.OrdinalIgnoreCase))
                return new(false, UsageText());
            SelfCheckBundleCoordinator.TryOpenOutputFolderFromSettings();
            return new(true, "Requested to open RitsuLib self-check output folder.");
        }

        private static CmdResult ProcessSettings(string[] args)
        {
            if (args.Length < 3 || args.Length > 6 || !args[1].Equals("open", StringComparison.OrdinalIgnoreCase))
                return new(false, UsageText());

            var result = ModSettingsNavigator.RequestOpenByIds(
                args[2],
                GetOptionalArg(args, 3),
                GetOptionalArg(args, 4),
                GetOptionalArg(args, 5));
            return new(result.Success, result.Message);
        }

        private CompletionResult CompleteSettingsArguments(string[] args)
        {
            var partial = args[^1];
            var completed = args.Take(args.Length - 1).ToArray();
            if (args.Length <= 2)
                return CompleteArgument(SettingsActions, completed, partial, CompletionType.Subcommand);

            if (!args[1].Equals("open", StringComparison.OrdinalIgnoreCase))
                return base.GetArgumentCompletions(null, args);

            return args.Length switch
            {
                3 => CompleteArgument(GetModIdCandidates(), completed, partial),
                4 => CompleteArgument(GetPageIdCandidates(args[2]), completed, partial),
                5 => CompleteArgument(GetSectionIdCandidates(args[2], args[3]), completed, partial),
                6 => CompleteArgument(GetEntryIdCandidates(args[2], args[3], args[4]), completed, partial),
                _ => base.GetArgumentCompletions(null, args),
            };
        }

        private static string? GetOptionalArg(string[] args, int index)
        {
            return args.Length <= index || string.IsNullOrWhiteSpace(args[index]) ? null : args[index];
        }

        private static string UsageText()
        {
            return
                "Usage: ritsulib selfcheck run|open-output OR ritsulib settings open <modId> [pageId] [sectionId] [entryId]";
        }

        private static string[] GetModIdCandidates()
        {
            RefreshSettingsPagesForCompletion();
            return
            [
                .. ModSettingsRegistry.GetPages()
                    .Where(ModSettingsVisibility.IsPageVisible)
                    .Select(static page => page.ModId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase),
            ];
        }

        private static string[] GetPageIdCandidates(string modId)
        {
            RefreshSettingsPagesForCompletion();
            return
            [
                .. ModSettingsRegistry.GetPages()
                    .Where(page => string.Equals(page.ModId, modId, StringComparison.OrdinalIgnoreCase))
                    .Where(ModSettingsVisibility.IsPageVisible)
                    .Select(static page => page.Id)
                    .Order(StringComparer.OrdinalIgnoreCase),
            ];
        }

        private static string[] GetSectionIdCandidates(string modId, string pageId)
        {
            RefreshSettingsPagesForCompletion();
            return ModSettingsRegistry.TryGetPage(modId, pageId, out var page) && page != null &&
                   ModSettingsVisibility.IsPageVisible(page)
                ?
                [
                    .. page.Sections.Where(section => ModSettingsVisibility.IsSectionVisible(page, section))
                        .Select(static section => section.Id)
                        .Order(StringComparer.OrdinalIgnoreCase),
                ]
                : [];
        }

        private static string[] GetEntryIdCandidates(string modId, string pageId, string sectionId)
        {
            RefreshSettingsPagesForCompletion();
            if (!ModSettingsRegistry.TryGetPage(modId, pageId, out var page) || page == null ||
                !ModSettingsVisibility.IsPageVisible(page))
                return [];

            var section = page.Sections.FirstOrDefault(s => string.Equals(s.Id, sectionId,
                StringComparison.OrdinalIgnoreCase));
            return section == null || !ModSettingsVisibility.IsSectionVisible(page, section)
                ? []
                :
                [
                    .. section.Entries.Where(entry => ModSettingsVisibility.IsEntryVisible(page, entry))
                        .Select(static entry => entry.Id)
                        .Order(StringComparer.OrdinalIgnoreCase),
                ];
        }

        private static void RefreshSettingsPagesForCompletion()
        {
            try
            {
                RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
                ModSettingsMirrorRegistrarBootstrap.TryRegisterMirroredPages();
                RitsuLibModSettingsBootstrap.RefreshDynamicPages();
            }
            catch
            {
                // Completion is best-effort; command execution reports concrete failures.
            }
        }
    }
}
