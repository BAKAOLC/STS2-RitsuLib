using SmartFormat;
using SmartFormat.Core.Extensions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     Per-mod registration surface for SmartFormat sources and formatters used by the game localization formatter.
    /// </summary>
    public sealed class ModSmartFormatExtensionRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSmartFormatExtensionRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly List<ModSmartFormatExtensionDefinition> Formatters = [];
        private static readonly List<ModSmartFormatExtensionDefinition> Sources = [];

        private static long _nextSequence;
        private static SmartFormatter? _initializedSmartFormatter;

        private readonly Logger _logger;

        private ModSmartFormatExtensionRegistry(string modId)
        {
            ModId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     Owning mod id for this registry facade.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        /// </summary>
        public static ModSmartFormatExtensionRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModSmartFormatExtensionRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers an already-created SmartFormat formatter instance.
        /// </summary>
        public void Register(IFormatter formatter, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            RegisterCore(SmartFormatExtensionKind.Formatter, formatter.GetType(), formatter, order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat formatter type.
        /// </summary>
        public void Register<TFormatter>(int order = 0)
            where TFormatter : IFormatter, new()
        {
            Register(new TFormatter(), order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat formatter type.
        /// </summary>
        public void RegisterFormatterType(Type formatterType, int order = 0)
        {
            RegisterType(formatterType, typeof(IFormatter),
                SmartFormatExtensionKind.Formatter, order);
        }

        /// <summary>
        ///     Registers an already-created SmartFormat source instance.
        /// </summary>
        public void RegisterSource(ISource source, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            RegisterCore(SmartFormatExtensionKind.Source, source.GetType(), source, order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat source type.
        /// </summary>
        public void RegisterSource<TSource>(int order = 0)
            where TSource : ISource, new()
        {
            RegisterSource(new TSource(), order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat source type.
        /// </summary>
        public void RegisterSourceType(Type sourceType, int order = 0)
        {
            RegisterType(sourceType, typeof(ISource),
                SmartFormatExtensionKind.Source, order);
        }

        /// <summary>
        ///     Snapshot of all registered formatter definitions in deterministic injection order.
        /// </summary>
        public static IReadOnlyList<ModSmartFormatExtensionDefinition> GetFormattersSnapshot()
        {
            lock (SyncRoot)
            {
                return SortSnapshot(Formatters);
            }
        }

        /// <summary>
        ///     Snapshot of all registered source definitions in deterministic injection order.
        /// </summary>
        public static IReadOnlyList<ModSmartFormatExtensionDefinition> GetSourcesSnapshot()
        {
            lock (SyncRoot)
            {
                return SortSnapshot(Sources);
            }
        }

        /// <summary>
        ///     Resolves which mod registered a formatter name, if any.
        /// </summary>
        public static bool TryGetFormatterOwnerModId(string formatterName, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(formatterName);

            lock (SyncRoot)
            {
                foreach (var definition in Formatters)
                    if (definition.Instance is IFormatter formatter
                        && StringComparer.OrdinalIgnoreCase.Equals(formatter.Name, formatterName))
                    {
                        modId = definition.OwnerModId;
                        return true;
                    }
            }

            modId = string.Empty;
            return false;
        }

        internal static void NotifyInitialized(SmartFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            lock (SyncRoot)
            {
                _initializedSmartFormatter = formatter;
            }
        }

        private void RegisterType(Type extensionType, Type expectedType, SmartFormatExtensionKind kind, int order)
        {
            ArgumentNullException.ThrowIfNull(extensionType);
            ArgumentNullException.ThrowIfNull(expectedType);

            if (!ValidateExtensionType(extensionType, expectedType, kind))
                return;

            object instance;
            try
            {
                instance = Activator.CreateInstance(extensionType)!;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"[SmartFormat] Failed to instantiate {kind} '{extensionType.FullName}': {ex.Message}");
                return;
            }

            RegisterCore(kind, extensionType, instance, order);
        }

        private bool ValidateExtensionType(Type extensionType, Type expectedType, SmartFormatExtensionKind kind)
        {
            if (extensionType.ContainsGenericParameters)
            {
                _logger.Error($"[SmartFormat] Cannot register open generic {kind} type '{extensionType.FullName}'.");
                return false;
            }

            if (extensionType.IsAbstract || extensionType.IsInterface || !expectedType.IsAssignableFrom(extensionType))
            {
                _logger.Error(
                    $"[SmartFormat] Type '{extensionType.FullName}' must be a concrete implementation of '{expectedType.FullName}'.");
                return false;
            }

            if (extensionType.GetConstructor(Type.EmptyTypes) != null)
                return true;

            _logger.Error(
                $"[SmartFormat] Type '{extensionType.FullName}' must have a parameterless constructor.");
            return false;
        }

        private void RegisterCore(SmartFormatExtensionKind kind, Type implementationType, object instance, int order)
        {
            ModSmartFormatExtensionDefinition definition;
            SmartFormatter? initializedFormatter;

            lock (SyncRoot)
            {
                definition = new(
                    ModId,
                    kind,
                    implementationType,
                    order,
                    instance,
                    _nextSequence++);

                GetBucket(kind).Add(definition);
                initializedFormatter = _initializedSmartFormatter;
            }

            _logger.Info(
                $"[SmartFormat] Registered {kind}: {implementationType.FullName} (order={order}).");

            if (initializedFormatter != null)
                SmartFormatExtensionInjector.Inject(initializedFormatter, definition);
        }

        private static List<ModSmartFormatExtensionDefinition> GetBucket(SmartFormatExtensionKind kind)
        {
            return kind == SmartFormatExtensionKind.Formatter ? Formatters : Sources;
        }

        private static ModSmartFormatExtensionDefinition[] SortSnapshot(
            IEnumerable<ModSmartFormatExtensionDefinition> definitions)
        {
            return definitions
                .OrderBy(def => def.OwnerModId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.Order)
                .ThenBy(def => def.ImplementationType.FullName ?? def.ImplementationType.Name, StringComparer.Ordinal)
                .ThenBy(def => def.Sequence)
                .ToArray();
        }
    }
}
