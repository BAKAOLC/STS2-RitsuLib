using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Registry of per-command dev-console autocomplete enhancement bindings.
    /// </summary>
    public static class DevConsoleAutocompleteRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<DevConsoleAutocompleteBinding> Bindings = [];
        private static bool _builtInRegistered;

        static DevConsoleAutocompleteRegistry()
        {
            RegisterBuiltInBindings();
        }

        /// <summary>
        ///     Registers a binding. Later bindings merge enhancements when multiple bindings match the same slot.
        /// </summary>
        public static void Register(DevConsoleAutocompleteBinding binding)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentException.ThrowIfNullOrWhiteSpace(binding.CommandName);

            lock (SyncRoot)
            {
                Bindings.Add(binding);
            }
        }

        /// <summary>
        ///     Registers enhancements for a command argument slot.
        /// </summary>
        public static void Register(
            string commandName,
            int argumentIndex,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool>? appliesWhen = null)
        {
            Register(new()
            {
                CommandName = commandName,
                ArgumentIndex = argumentIndex,
                Enhancements = enhancements,
                AppliesWhen = appliesWhen,
            });
        }

        /// <summary>
        ///     Registers enhancements when <paramref name="appliesWhen" /> returns true (any argument index unless restricted).
        /// </summary>
        public static void Register(
            string commandName,
            DevConsoleAutocompleteEnhancements enhancements,
            Func<DevConsoleAutocompleteContext, bool> appliesWhen)
        {
            ArgumentNullException.ThrowIfNull(appliesWhen);

            Register(new()
            {
                CommandName = commandName,
                Enhancements = enhancements,
                AppliesWhen = appliesWhen,
            });
        }

        /// <summary>
        ///     Resolves merged enhancements for a completion call.
        /// </summary>
        public static DevConsoleAutocompleteEnhancements Resolve(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(completedArgs);

            var context = new DevConsoleAutocompleteContext(command, completedArgs, argumentIndex);
            var merged = DevConsoleAutocompleteEnhancements.None;

            lock (SyncRoot)
            {
                merged = Bindings.Where(binding => BindingMatches(binding, context))
                    .Aggregate(merged, (current, binding) => current | binding.Enhancements);
            }

            return merged;
        }

        /// <summary>
        ///     Returns whether any enhancements apply to the completion call.
        /// </summary>
        public static bool HasEnhancements(
            AbstractConsoleCmd command,
            string[] completedArgs,
            int argumentIndex)
        {
            return Resolve(command, completedArgs, argumentIndex) != DevConsoleAutocompleteEnhancements.None;
        }

        private static bool BindingMatches(DevConsoleAutocompleteBinding binding, DevConsoleAutocompleteContext context)
        {
            if (!binding.CommandName.Equals(context.CommandName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (binding.ArgumentIndex is int index && index != context.ArgumentIndex)
                return false;

            return binding.AppliesWhen?.Invoke(context) ?? true;
        }

        private static void RegisterBuiltInBindings()
        {
            lock (SyncRoot)
            {
                if (_builtInRegistered)
                    return;

                DevConsoleAutocompleteDefaults.Register();
                _builtInRegistered = true;
            }
        }
    }
}
