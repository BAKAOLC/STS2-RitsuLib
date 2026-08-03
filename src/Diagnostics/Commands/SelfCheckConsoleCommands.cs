using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.Commands
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Implements the root console command for RitsuLib diagnostics, settings navigation, and opt-in
    ///         run-state editing tools.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         实现 RitsuLib 诊断、设置导航与需主动启用的对局状态编辑工具根控制台指令。
    ///     </para>
    /// </summary>
    public sealed partial class RitsuLibConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] StandardRootCommands = ["selfcheck", "settings"];
        private static readonly string[] SelfCheckActions = ["run", "open-output"];
        private static readonly string[] SettingsActions = ["open"];

        /// <inheritdoc />
        public override string CmdName => "ritsulib";

        /// <inheritdoc />
        public override string Args => RitsuLibSettingsStore.AreDeveloperToolsEnabled()
            ? "selfcheck run|open-output OR settings open <modId> [pageId] [sectionId] [entryId] OR debug <group> ..."
            : "selfcheck run|open-output OR settings open <modId> [pageId] [sectionId] [entryId]";

        /// <inheritdoc />
        public override string Description => RitsuLibSettingsStore.AreDeveloperToolsEnabled()
            ? "RitsuLib tools: self-check, settings navigation, and explicitly enabled run-state editing."
            : "RitsuLib tools: selfcheck run/open-output; settings open.";

        /// <inheritdoc />
        public override bool IsNetworked => false;

        /// <inheritdoc />
        public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
        {
            if (args.Length <= 1)
            {
                var partial = args.Length == 0 ? string.Empty : args[0];
                return CompleteArgument(GetRootCommands(), [], partial, CompletionType.Subcommand);
            }

            if (args[0].Equals("debug", StringComparison.OrdinalIgnoreCase))
                return RitsuLibSettingsStore.AreDeveloperToolsEnabled()
                    ? CompleteDebugArguments(player, args)
                    : base.GetArgumentCompletions(player, args);

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
            if (args.Length == 0)
                return new(false, UsageText());

            switch (args[0])
            {
                case var command when command.Equals("debug", StringComparison.OrdinalIgnoreCase):
                    return RitsuLibSettingsStore.AreDeveloperToolsEnabled()
                        ? ProcessDebug(issuingPlayer, args)
                        : new(false, ModSettingsLocalization.Get(
                            "ritsulib.debugTools.feedback.protocol.toolsDisabled",
                            "RitsuLib developer tools are disabled in settings."));
                case var command when command.Equals("settings", StringComparison.OrdinalIgnoreCase):
                    return ProcessSettings(args);
                case var command when command.Equals("selfcheck", StringComparison.OrdinalIgnoreCase) &&
                                      args.Length >= 2:
                    break;
                default:
                    return new(false, UsageText());
            }

            if (args.Length == 2 && args[1].Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                var ok = SelfCheckBundleCoordinator.TryManualRunFromConsole(out var message);
                return new(ok, message);
            }

            if (args.Length != 2 || !args[1].Equals("open-output", StringComparison.OrdinalIgnoreCase))
                return new(false, UsageText());
            var opened = SelfCheckBundleCoordinator.TryOpenOutputFolderFromSettings(out var openMessage);
            return new(opened, openMessage);
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
            return RitsuLibSettingsStore.AreDeveloperToolsEnabled()
                ? "Usage: ritsulib selfcheck run|open-output OR ritsulib settings open <modId> [pageId] [sectionId] [entryId] OR ritsulib debug <group> ..."
                : "Usage: ritsulib selfcheck run|open-output OR ritsulib settings open <modId> [pageId] [sectionId] [entryId]";
        }

        private static string[] GetRootCommands()
        {
            return RitsuLibSettingsStore.AreDeveloperToolsEnabled()
                ? [.. StandardRootCommands, "debug"]
                : StandardRootCommands;
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Settings] Could not refresh console completion data: {ex}");
            }
        }
    }
}
