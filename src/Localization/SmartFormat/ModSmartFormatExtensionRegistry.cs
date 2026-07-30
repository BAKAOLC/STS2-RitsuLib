using SmartFormat;
using SmartFormat.Core.Extensions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     <para xml:lang="en">Provides a per-mod registry for SmartFormat selector sources and formatters used by the game's localization formatter.</para>
    ///     <para xml:lang="zh-CN">提供按模组划分的注册表，用于登记游戏本地化格式化器所使用的 SmartFormat 选择器数据源和格式化器。</para>
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
        ///     <para xml:lang="en">Gets the ID of the mod that owns this registry.</para>
        ///     <para xml:lang="zh-CN">获取此注册表所属模组的 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared registry for <paramref name="modId" />, creating it on first use. mod IDs are compared without regard to case.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 对应的共享注册表，并在首次使用时创建。模组 ID 比较不区分大小写。</para>
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
        ///     <para xml:lang="en">Registers an existing SmartFormat formatter instance.</para>
        ///     <para xml:lang="zh-CN">注册已有的 SmartFormat 格式化器实例。</para>
        /// </summary>
        public void Register(IFormatter formatter, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            RegisterCore(SmartFormatExtensionKind.Formatter, formatter.GetType(), formatter, order);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and registers a SmartFormat formatter of type <typeparamref name="TFormatter" />.</para>
        ///     <para xml:lang="zh-CN">创建并注册 <typeparamref name="TFormatter" /> 类型的 SmartFormat 格式化器。</para>
        /// </summary>
        public void Register<TFormatter>(int order = 0)
            where TFormatter : IFormatter, new()
        {
            Register(new TFormatter(), order);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and registers the specified SmartFormat formatter type. Invalid types or construction failures are logged and ignored.</para>
        ///     <para xml:lang="zh-CN">创建并注册指定的 SmartFormat 格式化器类型。无效类型或构造失败会被记录并忽略。</para>
        /// </summary>
        public void RegisterFormatterType(Type formatterType, int order = 0)
        {
            RegisterType(formatterType, typeof(IFormatter),
                SmartFormatExtensionKind.Formatter, order);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an existing SmartFormat selector-source instance.</para>
        ///     <para xml:lang="zh-CN">注册已有的 SmartFormat 选择器数据源实例。</para>
        /// </summary>
        public void RegisterSource(ISource source, int order = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            RegisterCore(SmartFormatExtensionKind.Source, source.GetType(), source, order);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and registers a SmartFormat selector source of type <typeparamref name="TSource" />.</para>
        ///     <para xml:lang="zh-CN">创建并注册 <typeparamref name="TSource" /> 类型的 SmartFormat 选择器数据源。</para>
        /// </summary>
        public void RegisterSource<TSource>(int order = 0)
            where TSource : ISource, new()
        {
            RegisterSource(new TSource(), order);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates and registers the specified SmartFormat selector-source type. Invalid types or construction failures are logged and ignored.</para>
        ///     <para xml:lang="zh-CN">创建并注册指定的 SmartFormat 选择器数据源类型。无效类型或构造失败会被记录并忽略。</para>
        /// </summary>
        public void RegisterSourceType(Type sourceType, int order = 0)
        {
            RegisterType(sourceType, typeof(ISource),
                SmartFormatExtensionKind.Source, order);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a snapshot of all registered formatter definitions, ordered by owning mod ID, ordering value, implementation type, and registration sequence.</para>
        ///     <para xml:lang="zh-CN">返回全部已注册格式化器定义的快照，依次按所属模组 ID、排序值、实现类型和注册顺序排列。</para>
        /// </summary>
        public static IReadOnlyList<ModSmartFormatExtensionDefinition> GetFormattersSnapshot()
        {
            lock (SyncRoot)
            {
                return SortSnapshot(Formatters);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a snapshot of all registered selector-source definitions, ordered by owning mod ID, ordering value, implementation type, and registration sequence.</para>
        ///     <para xml:lang="zh-CN">返回全部已注册选择器数据源定义的快照，依次按所属模组 ID、排序值、实现类型和注册顺序排列。</para>
        /// </summary>
        public static IReadOnlyList<ModSmartFormatExtensionDefinition> GetSourcesSnapshot()
        {
            lock (SyncRoot)
            {
                return SortSnapshot(Sources);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Finds the first registered formatter with the specified name, using the snapshot order, and returns its owning mod ID.</para>
        ///     <para xml:lang="zh-CN">按快照顺序查找首个具有指定名称的已注册格式化器，并返回其所属模组 ID。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if a matching formatter was found; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">找到匹配的格式化器时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryGetFormatterOwnerModId(string formatterName, out string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(formatterName);

            lock (SyncRoot)
            {
                foreach (var definition in SortSnapshot(Formatters))
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
