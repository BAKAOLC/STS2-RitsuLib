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
        private static readonly HashSet<DevConsoleAutocompleteBinding> FaultedBindings =
            new(ReferenceEqualityComparer.Instance);
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
            if (binding.ArgumentIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(binding), binding.ArgumentIndex,
                    "The autocomplete argument index cannot be negative.");
            ValidateEnhancements(binding.Enhancements, nameof(binding));

            binding = new()
            {
                CommandName = binding.CommandName.Trim(),
                ArgumentIndex = binding.ArgumentIndex,
                AppliesWhen = binding.AppliesWhen,
                Enhancements = binding.Enhancements,
            };

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
            DevConsoleAutocompleteBinding[] bindings;

            lock (SyncRoot)
            {
                bindings = [.. Bindings];
            }

            foreach (var binding in bindings)
                if (BindingMatches(binding, context))
                    merged |= binding.Enhancements;

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

            if (binding.ArgumentIndex is { } index && index != context.ArgumentIndex)
                return false;

            try
            {
                return binding.AppliesWhen?.Invoke(context) ?? true;
            }
            catch (Exception ex)
            {
                var shouldLog = false;
                lock (SyncRoot)
                {
                    shouldLog = FaultedBindings.Add(binding);
                }

                if (shouldLog)
                    RitsuLibFramework.Logger.Warn(
                        $"[DevConsole] Autocomplete predicate for '{binding.CommandName}' failed: {ex.Message}");
                return false;
            }
        }

        private static void ValidateEnhancements(
            DevConsoleAutocompleteEnhancements enhancements,
            string paramName)
        {
            const DevConsoleAutocompleteEnhancements allSupported =
                DevConsoleAutocompleteEnhancements.LocalizedTitleMatch |
                DevConsoleAutocompleteEnhancements.LocalizedDisplayLabels |
                DevConsoleAutocompleteEnhancements.RitsuLibOwnedIdShorthandMatch |
                DevConsoleAutocompleteEnhancements.DeduplicateCandidates |
                DevConsoleAutocompleteEnhancements.IncludeModPileCandidates |
                DevConsoleAutocompleteEnhancements.PileNameLocalizedTitleMatch |
                DevConsoleAutocompleteEnhancements.PileNameDisplayLabels |
                DevConsoleAutocompleteEnhancements.AncientChoiceLocalizedTitleMatch |
                DevConsoleAutocompleteEnhancements.AncientChoiceDisplayLabels |
                DevConsoleAutocompleteEnhancements.IncludeSecondaryResourceCandidates |
                DevConsoleAutocompleteEnhancements.SecondaryResourceLocalizedTitleMatch |
                DevConsoleAutocompleteEnhancements.SecondaryResourceDisplayLabels;
            if ((enhancements & ~allSupported) != 0)
                throw new ArgumentOutOfRangeException(paramName, enhancements,
                    "The autocomplete binding contains unsupported enhancement flags.");
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
