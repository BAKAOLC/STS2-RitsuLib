using System.Runtime.CompilerServices;
using SmartFormat;
using SmartFormat.Core.Extensions;

namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     Injects registered mod SmartFormat extensions into a live <c>SmartFormatter</c> instance.
    ///     将已注册的 mod SmartFormat 扩展注入到实时 <c>SmartFormatter</c> 实例中。
    /// </summary>
    public static class SmartFormatExtensionInjector
    {
        private static readonly ConditionalWeakTable<SmartFormatter, InjectedFormatterNames>
            InjectedFormatterNamesByFormatter = new();

        /// <summary>
        ///     Injects all registered sources first, then formatters.
        ///     先注入所有已注册的 source，然后注入 formatter。
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
        ///     Injects a single registered extension into <paramref name="formatter" />.
        ///     将单个已注册扩展注入到 <paramref name="formatter" />。
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
            private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
            private readonly Lock _syncRoot = new();

            public InjectedFormatterNames(SmartFormatter formatter)
            {
                foreach (var existingFormatter in formatter.GetFormatterExtensions())
                    if (!string.IsNullOrWhiteSpace(existingFormatter.Name))
                        _names.Add(existingFormatter.Name);
            }

            public bool TryAdd(string formatterName)
            {
                lock (_syncRoot)
                {
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
