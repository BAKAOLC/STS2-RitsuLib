using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores developer-console autocomplete enhancement bindings by command.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按命令存储开发者控制台自动补全增强绑定。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers a binding. When multiple bindings match the same argument slot, their enhancements are
        ///         combined.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个绑定。多个绑定匹配同一参数位置时，会合并其增强标志。
        ///     </para>
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
        ///     <para xml:lang="en">Registers enhancements for a command argument slot.</para>
        ///     <para xml:lang="zh-CN">为命令的指定参数位置注册增强。</para>
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
        ///     <para xml:lang="en">
        ///         Registers enhancements that apply when <paramref name="appliesWhen" /> returns <see langword="true" />;
        ///         the binding applies to every argument position.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册在 <paramref name="appliesWhen" /> 返回 <see langword="true" /> 时生效的增强；该绑定适用于所有
        ///         参数位置。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Resolves and combines all enhancements that apply to an autocomplete call.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析并合并适用于一次自动补全调用的所有增强。
        ///     </para>
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

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var binding in bindings)
                if (BindingMatches(binding, context))
                    merged |= binding.Enhancements;

            return merged;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns whether any enhancements apply to an autocomplete call.</para>
        ///     <para xml:lang="zh-CN">返回是否有任何增强适用于一次自动补全调用。</para>
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
                bool shouldLog;
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
