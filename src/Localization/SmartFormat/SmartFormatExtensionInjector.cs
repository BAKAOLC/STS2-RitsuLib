using System.Runtime.CompilerServices;
using SmartFormat;
using SmartFormat.Core.Extensions;

namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     <para xml:lang="en">Injects registered mod SmartFormat extensions into an active <c>SmartFormatter</c> instance.</para>
    ///     <para xml:lang="zh-CN">将已注册的模组 SmartFormat 扩展注入正在使用的 <c>SmartFormatter</c> 实例。</para>
    /// </summary>
    public static class SmartFormatExtensionInjector
    {
        private static readonly ConditionalWeakTable<SmartFormatter, InjectedFormatterNames>
            InjectedFormatterNamesByFormatter = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Injects a snapshot of all registered selector sources first, followed by all registered
        ///         formatters.
        ///     </para>
        ///     <para xml:lang="zh-CN">先注入全部已注册选择器数据源的快照，再注入全部已注册格式化器的快照。</para>
        /// </summary>
        public static void InjectAll(SmartFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            foreach (var definition in ModSmartFormatExtensionRegistry.GetSourcesSnapshot())
                Inject(formatter, definition);

            foreach (var definition in ModSmartFormatExtensionRegistry.GetFormattersSnapshot())
                Inject(formatter, definition);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to inject one registered extension into <paramref name="formatter" />. Unsupported
        ///         kinds, invalid instances, duplicate formatter names, and injection failures are logged instead of being
        ///         propagated.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将一个已注册扩展注入 <paramref name="formatter" />。不支持的类别、无效实例、重复的格式化器名称及注入失败均会被记录，而不会向调用方继续抛出。</para>
        /// </summary>
        public static void Inject(
            SmartFormatter formatter,
            ModSmartFormatExtensionDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            ArgumentNullException.ThrowIfNull(definition);

            try
            {
                switch (definition.Kind)
                {
                    case SmartFormatExtensionKind.Source:
                        InjectSource(formatter, definition);
                        break;
                    case SmartFormatExtensionKind.Formatter:
                        InjectFormatter(formatter, definition);
                        break;
                    default:
                        RitsuLibFramework.Logger.Warn(
                            $"[SmartFormat] Unknown extension kind '{definition.Kind}' for '{definition.ImplementationType.FullName}'.");
                        break;
                }
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SmartFormat] Failed to inject {definition.Kind} '{definition.ImplementationType.FullName}' "
                    + $"from mod '{definition.OwnerModId}': {ex.Message}");
            }
        }

        private static void InjectSource(
            SmartFormatter smartFormatter,
            ModSmartFormatExtensionDefinition definition)
        {
            if (definition.Instance is not ISource source)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SmartFormat] Skipping source '{definition.ImplementationType.FullName}' because its instance does not implement ISource.");
                return;
            }

            smartFormatter.AddExtensions(source);
        }

        private static void InjectFormatter(
            SmartFormatter smartFormatter,
            ModSmartFormatExtensionDefinition definition)
        {
            if (definition.Instance is not IFormatter formatter)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SmartFormat] Skipping formatter '{definition.ImplementationType.FullName}' because its instance does not implement IFormatter.");
                return;
            }

            var formatterName = formatter.Name;
            if (string.IsNullOrWhiteSpace(formatterName))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SmartFormat] Skipping formatter '{definition.ImplementationType.FullName}' from mod "
                    + $"'{definition.OwnerModId}' because its name is empty.");
                return;
            }

            var injectedNames = InjectedFormatterNamesByFormatter.GetValue(
                smartFormatter,
                static currentFormatter => new(currentFormatter));

            if (!injectedNames.TryAdd(formatterName))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SmartFormat] Skipping formatter '{definition.ImplementationType.FullName}' from mod "
                    + $"'{definition.OwnerModId}' because formatter name '{formatterName}' is already registered.");
                return;
            }

            try
            {
                smartFormatter.AddExtensions(formatter);
            }
            catch
            {
                injectedNames.Remove(formatterName);
                throw;
            }
        }

        private sealed class InjectedFormatterNames
        {
            private readonly SmartFormatter _formatter;
            private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
            private readonly Lock _syncRoot = new();

            public InjectedFormatterNames(SmartFormatter formatter)
            {
                _formatter = formatter;
                foreach (var existingFormatter in formatter.GetFormatterExtensions())
                    if (!string.IsNullOrWhiteSpace(existingFormatter.Name))
                        _names.Add(existingFormatter.Name);
            }

            public bool TryAdd(string formatterName)
            {
                lock (_syncRoot)
                {
                    foreach (var existingFormatter in _formatter.GetFormatterExtensions())
                        if (!string.IsNullOrWhiteSpace(existingFormatter.Name))
                            _names.Add(existingFormatter.Name);

                    return _names.Add(formatterName);
                }
            }

            public void Remove(string formatterName)
            {
                lock (_syncRoot)
                {
                    _names.Remove(formatterName);
                }
            }
        }
    }
}
