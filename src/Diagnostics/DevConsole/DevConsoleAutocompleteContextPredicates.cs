namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Reusable <see cref="DevConsoleAutocompleteContext" /> guards for slot registration.
    /// </summary>
    public static class DevConsoleAutocompleteContextPredicates
    {
        /// <summary>
        ///     First argument while no prior arguments are present.
        /// </summary>
        public static bool IsFirstArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 0, CompletedArgs.Count: 0 };
        }

        /// <summary>
        ///     Second argument while exactly one prior argument is present.
        /// </summary>
        public static bool IsSecondArgument(DevConsoleAutocompleteContext context)
        {
            return context is { ArgumentIndex: 1, CompletedArgs.Count: 1 };
        }

        /// <summary>
        ///     <c>ancient</c> second argument: event option token after the ancient id is chosen.
        /// </summary>
        public static bool IsAncientChoiceArgument(DevConsoleAutocompleteContext context)
        {
            if (!context.CommandName.Equals("ancient", StringComparison.OrdinalIgnoreCase))
                return false;

            return context is { ArgumentIndex: 1, CompletedArgs.Count: 1 };
        }

        /// <summary>
        ///     <c>relic</c> command: relic id as the only token, or as the token after <c>add</c>/<c>remove</c>.
        /// </summary>
        public static bool IsRelicIdArgument(DevConsoleAutocompleteContext context)
        {
            if (IsFirstArgument(context))
                return true;

            if (context.ArgumentIndex != 1 || context.CompletedArgs.Count != 1)
                return false;

            var subcommand = context.CompletedArgs[0];
            return subcommand.Equals("add", StringComparison.OrdinalIgnoreCase) ||
                   subcommand.Equals("remove", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     <c>unlock</c> trailing arguments that name individual discovery ids.
        /// </summary>
        public static bool IsUnlockDiscoveryIdArgument(DevConsoleAutocompleteContext context)
        {
            if (context.ArgumentIndex < 1 || context.CompletedArgs.Count < 1)
                return false;

            return DevConsoleUnlockAutocompleteSources.SupportsDiscoveryIds(context.CompletedArgs[0]);
        }
    }
}
