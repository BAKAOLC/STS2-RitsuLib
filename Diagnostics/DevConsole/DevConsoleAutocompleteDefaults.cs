namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Built-in dev-console autocomplete bindings aligned with vanilla command completion slots.
    /// </summary>
    /// <remarks>
    ///     Vanilla commands with completions but no model-id enhancement:
    ///     <c>act</c> (no vanilla autocomplete; accepts run act index or act model id),
    ///     <c>art</c> (content-type names),
    ///     <c>room</c> (<see cref="MegaCrit.Sts2.Core.Rooms.RoomType" />),
    ///     <c>kill</c> / <c>upgrade</c> (numeric indices),
    ///     <c>achievement</c> / <c>open</c> / <c>leaderboard</c> (fixed option lists),
    ///     <c>relic</c> arg 0 when choosing <c>add</c>/<c>remove</c> subcommands,
    ///     <c>unlock</c> arg 0 (discovery type names).
    /// </remarks>
    internal static class DevConsoleAutocompleteDefaults
    {
        public static void Register()
        {
            RegisterModelEntryIdFirstArgument(
                "power",
                "afflict",
                "ancient",
                "card",
                "enchant",
                "event",
                "fight",
                "potion",
                "remove_card");

            RegisterPileNameSecondArgument("card", "remove_card");

            DevConsoleAutocompleteRegistry.Register(
                "ancient",
                DevConsoleAutocompleteEnhancements.AncientChoice,
                DevConsoleAutocompleteContextPredicates.IsAncientChoiceArgument);

            DevConsoleAutocompleteRegistry.Register(
                "relic",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                DevConsoleAutocompleteContextPredicates.IsRelicIdArgument);

            DevConsoleAutocompleteRegistry.Register(
                "unlock",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                DevConsoleAutocompleteContextPredicates.IsUnlockDiscoveryIdArgument);
        }

        private static void RegisterModelEntryIdFirstArgument(params string[] commandNames)
        {
            foreach (var commandName in commandNames)
                DevConsoleAutocompleteRegistry.Register(
                    commandName,
                    DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                    DevConsoleAutocompleteContextPredicates.IsFirstArgument);
        }

        private static void RegisterPileNameSecondArgument(params string[] commandNames)
        {
            foreach (var commandName in commandNames)
                DevConsoleAutocompleteRegistry.Register(
                    commandName,
                    DevConsoleAutocompleteEnhancements.PileName,
                    DevConsoleAutocompleteContextPredicates.IsSecondArgument);
        }
    }
}
