namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Built-in dev-console autocomplete bindings for vanilla model-entry-id arguments.
    /// </summary>
    internal static class DevConsoleAutocompleteDefaults
    {
        public static void Register()
        {
            RegisterFirstModelEntryId("card");
            RegisterFirstModelEntryId("potion");
            RegisterFirstModelEntryId("event");
            RegisterFirstModelEntryId("ancient");
            RegisterFirstModelEntryId("fight");
            RegisterFirstModelEntryId("afflict");
            RegisterFirstModelEntryId("enchant");
            RegisterFirstModelEntryId("remove_card");

            DevConsoleAutocompleteRegistry.Register(
                "relic",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                IsDirectRelicIdArgument);

            DevConsoleAutocompleteRegistry.Register(
                "relic",
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                IsRelicIdAfterSubcommand);
        }

        private static void RegisterFirstModelEntryId(string commandName)
        {
            DevConsoleAutocompleteRegistry.Register(
                commandName,
                DevConsoleAutocompleteEnhancements.RitsuLibModEntryId,
                IsFirstModelEntryIdArgument);
        }

        private static bool IsFirstModelEntryIdArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 0, CompletedArgs.Count: 0 };
        }

        private static bool IsDirectRelicIdArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 0, CompletedArgs.Count: 0 };
        }

        private static bool IsRelicIdAfterSubcommand(DevConsoleAutocompleteContext context)
        {
            if (context.ArgumentIndex != 1 || context.CompletedArgs.Count != 1)
                return false;

            var subcommand = context.CompletedArgs[0];
            return subcommand.Equals("add", StringComparison.OrdinalIgnoreCase) ||
                   subcommand.Equals("remove", StringComparison.OrdinalIgnoreCase);
        }
    }
}
