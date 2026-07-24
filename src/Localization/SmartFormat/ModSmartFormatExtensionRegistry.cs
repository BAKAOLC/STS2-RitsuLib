using SmartFormat;
using SmartFormat.Core.Extensions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     Per-mod registration surface for SmartFormat sources and formatters used by the game localization formatter.
    ///     按 mod 提供的注册入口，用于游戏本地化格式化器使用的 SmartFormat source 和 formatter。
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
        ///     此注册表 facade 所属的 mod id。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例注册表，首次使用时创建。
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
        ///     注册已创建的 SmartFormat formatter 实例。
        /// </summary>
        public void Register(IFormatter formatter, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            RegisterCore(SmartFormatExtensionKind.Formatter, formatter.GetType(), formatter, order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat formatter type.
        ///     创建并注册 SmartFormat formatter 类型。
        /// </summary>
        public void Register<TFormatter>(int order = 0)
            where TFormatter : IFormatter, new()
        {
            Register(new TFormatter(), order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat formatter type.
        ///     创建并注册 SmartFormat formatter 类型。
        /// </summary>
        public void RegisterFormatterType(Type formatterType, int order = 0)
        {
            RegisterType(formatterType, typeof(IFormatter),
                SmartFormatExtensionKind.Formatter, order);
        }

        /// <summary>
        ///     Registers an already-created SmartFormat source instance.
        ///     注册已创建的 SmartFormat source 实例。
        /// </summary>
        public void RegisterSource(ISource source, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            RegisterCore(SmartFormatExtensionKind.Source, source.GetType(), source, order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat source type.
        ///     创建并注册 SmartFormat source 类型。
        /// </summary>
        public void RegisterSource<TSource>(int order = 0)
            where TSource : ISource, new()
        {
            RegisterSource(new TSource(), order);
        }

        /// <summary>
        ///     Creates and registers a SmartFormat source type.
        ///     创建并注册 SmartFormat source 类型。
        /// </summary>
        public void RegisterSourceType(Type sourceType, int order = 0)
        {
            RegisterType(sourceType, typeof(ISource),
                SmartFormatExtensionKind.Source, order);
        }

        /// <summary>
        ///     Snapshot of all registered formatter definitions in deterministic injection order.
        ///     按确定性注入顺序排列的所有已注册 formatter 定义快照。
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
        ///     按确定性注入顺序排列的所有已注册 source 定义快照。
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
        ///     解析哪个 mod 注册了 formatter 名称；没有则返回空结果。
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
                _logger.ErrorNoTrace(
                    $"[SmartFormat] Failed to instantiate {kind} '{extensionType.FullName}': {ex.Message}");
                return;
            }

            RegisterCore(kind, extensionType, instance, order);
        }

        private bool ValidateExtensionType(Type extensionType, Type expectedType, SmartFormatExtensionKind kind)
        {
            if (extensionType.ContainsGenericParameters)
            {
                _logger.ErrorNoTrace(
                    $"[SmartFormat] Cannot register open generic {kind} type '{extensionType.FullName}'.");
                return false;
            }

            if (extensionType.IsAbstract || extensionType.IsInterface || !expectedType.IsAssignableFrom(extensionType))
            {
                _logger.ErrorNoTrace(
                    $"[SmartFormat] Type '{extensionType.FullName}' must be a concrete implementation of '{expectedType.FullName}'.");
                return false;
            }

            if (extensionType.GetConstructor(Type.EmptyTypes) != null)
                return true;

            _logger.ErrorNoTrace(
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
            return
            [
                .. definitions
                    .OrderBy(def => def.OwnerModId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(def => def.Order)
                    .ThenBy(def => def.ImplementationType.FullName ?? def.ImplementationType.Name,
                        StringComparer.Ordinal)
                    .ThenBy(def => def.Sequence),
            ];
        }
    }
}
