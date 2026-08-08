using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Ancients.Options
{
    /// <summary>
    ///     <para xml:lang="en">Registers rules that add choices to an Ancient's initial option list.</para>
    ///     <para xml:lang="zh-CN">注册向先古之民初始选项列表添加选项的规则。</para>
    /// </summary>
    public static class ModAncientOptionRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<Type, List<RegisteredRule>> RulesByAncientType = [];
        private static long _registrationCounter;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an option rule for <typeparamref name="TAncient" /> and its derived types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TAncient" /> 及其派生类型注册选项规则。
        ///     </para>
        /// </summary>
        public static void Register<TAncient>(string ownerModId, ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            Register(typeof(TAncient), ownerModId, rule);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an option rule for <paramref name="ancientType" /> and its derived types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="ancientType" /> 及其派生类型注册选项规则。
        ///     </para>
        /// </summary>
        public static void Register(Type ancientType, string ownerModId, ModAncientOptionRule rule)
        {
            ArgumentNullException.ThrowIfNull(ancientType);
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);
            ArgumentNullException.ThrowIfNull(rule);

            if (ModContentRegistry.IsFrozen)
                throw new InvalidOperationException(
                    "Cannot register ancient option rules after content registration has been frozen. " +
                    "Register from your mod initializer before ModelDb initializes.");

            if (ancientType.IsAbstract || ancientType.IsInterface || ancientType.ContainsGenericParameters ||
                !typeof(AncientEventModel).IsAssignableFrom(ancientType))
                throw new ArgumentException(
                    $"Type '{ancientType.FullName}' must be a closed concrete subtype of " +
                    $"{typeof(AncientEventModel).FullName}.",
                    nameof(ancientType));

            var registered = new RegisteredRule(
                ownerModId.Trim(),
                rule,
                Interlocked.Increment(ref _registrationCounter));

            lock (SyncRoot)
            {
                if (!RulesByAncientType.TryGetValue(ancientType, out var list))
                {
                    list = [];
                    RulesByAncientType[ancientType] = list;
                }

                list.Add(registered);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Clears all registered rules for tests or hot-reload tooling.</para>
        ///     <para xml:lang="zh-CN">清除所有已注册规则，供测试或热重载工具使用。</para>
        /// </summary>
        public static void ClearForTests()
        {
            lock (SyncRoot)
            {
                RulesByAncientType.Clear();
                _registrationCounter = 0;
            }
        }

        internal static void AppendRegisteredOptions(AncientEventModel ancient, List<EventOption> options)
        {
            ArgumentNullException.ThrowIfNull(ancient);
            ArgumentNullException.ThrowIfNull(options);

            var existingTextKeys = new HashSet<string>(
                options
                    .Select(static option => option.TextKey)
                    .Where(static textKey => !string.IsNullOrWhiteSpace(textKey)),
                StringComparer.OrdinalIgnoreCase);

            var snapshot = GetApplicableRulesSnapshot(ancient.GetType());
            foreach (var registration in snapshot)
            {
                if (!ShouldApply(registration, ancient))
                    continue;

                EventOption[]? generated;
                try
                {
                    generated = registration.Rule.OptionFactory(ancient)?.ToArray();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.CreateLogger(registration.OwnerModId).Warn(
                        $"[AncientOption] OptionFactory threw for ancient '{ancient.Id.Entry}': {ex}");
                    continue;
                }

                if (generated == null)
                    continue;

                foreach (var option in generated)
                {
                    if (option == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(option.TextKey))
                    {
                        var isNewTextKey = existingTextKeys.Add(option.TextKey);
                        if (registration.Rule.SkipDuplicateTextKeys && !isNewTextKey)
                            continue;
                    }

                    options.Add(option);
                }
            }
        }

        private static bool ShouldApply(RegisteredRule registration, AncientEventModel ancient)
        {
            var condition = registration.Rule.Condition;
            if (condition == null)
                return true;

            try
            {
                return condition(ancient);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.CreateLogger(registration.OwnerModId).Warn(
                    $"[AncientOption] Condition threw for ancient '{ancient.Id.Entry}': {ex}");
                return false;
            }
        }

        private static RegisteredRule[] GetApplicableRulesSnapshot(Type ancientType)
        {
            var collected = new List<RegisteredRule>();

            lock (SyncRoot)
            {
                for (var type = ancientType;
                     type != null && typeof(AncientEventModel).IsAssignableFrom(type);
                     type = type.BaseType)
                    if (RulesByAncientType.TryGetValue(type, out var list))
                        collected.AddRange(list);
            }

            return
            [
                .. collected
                    .OrderByDescending(static rule => rule.Rule.Priority)
                    .ThenBy(static rule => rule.RegistrationOrder),
            ];
        }

        private readonly record struct RegisteredRule(
            string OwnerModId,
            ModAncientOptionRule Rule,
            long RegistrationOrder);
    }
}
