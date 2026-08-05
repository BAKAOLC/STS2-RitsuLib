using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal readonly record struct ModelCapabilityRegistration(string Id, Type CapabilityType);

    /// <summary>
    ///     <para xml:lang="en">Registry for capability ids and factories.</para>
    ///     <para xml:lang="zh-CN">能力 ID 与工厂的注册表。</para>
    /// </summary>
    public static class ModelCapabilityRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, Func<IModelCapability>> Factories =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Type> TypesById =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<Type, string> TypeIds = [];

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a capability factory.</para>
        ///     <para xml:lang="zh-CN">注册或替换能力工厂。</para>
        /// </summary>
        public static void Register(string capabilityId, Type capabilityType, Func<IModelCapability> factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);
            ArgumentNullException.ThrowIfNull(capabilityType);
            ArgumentNullException.ThrowIfNull(factory);

            if (!typeof(IModelCapability).IsAssignableFrom(capabilityType))
                throw new ArgumentException("Capability type must implement IModelCapability.", nameof(capabilityType));

            lock (SyncRoot)
            {
                if (TypesById.TryGetValue(capabilityId, out var existingType) &&
                    existingType != capabilityType)
                    throw new InvalidOperationException(
                        $"Model capability id is already registered for '{existingType.FullName}': {capabilityId}");

                if (TypeIds.TryGetValue(capabilityType, out var existingId) &&
                    !string.Equals(existingId, capabilityId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Model capability id is already registered: {capabilityId}");

                Factories[capabilityId] = factory;
                TypesById[capabilityId] = capabilityType;
                TypeIds[capabilityType] = capabilityId;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a capability factory.</para>
        ///     <para xml:lang="zh-CN">注册能力工厂。</para>
        /// </summary>
        public static void Register<TCapability>(string capabilityId, Func<TCapability> factory)
            where TCapability : IModelCapability
        {
            ArgumentNullException.ThrowIfNull(factory);
            Register(capabilityId, typeof(TCapability), () => factory());
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a parameterless capability factory.</para>
        ///     <para xml:lang="zh-CN">注册无参能力工厂。</para>
        /// </summary>
        public static void Register<TCapability>(string capabilityId)
            where TCapability : IModelCapability, new()
        {
            Register(capabilityId, static () => new TCapability());
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a capability by ID.</para>
        ///     <para xml:lang="zh-CN">通过 ID 创建能力。</para>
        /// </summary>
        public static bool TryCreate(string capabilityId, out IModelCapability capability)
        {
            Type capabilityType;
            Func<IModelCapability> factory;
            lock (SyncRoot)
            {
                if (!Factories.TryGetValue(capabilityId, out factory!) ||
                    !TypesById.TryGetValue(capabilityId, out capabilityType!))
                {
                    capability = null!;
                    return false;
                }
            }

            capability = factory()
                         ?? throw new InvalidOperationException(
                             $"Model capability factory returned null: {capabilityId}");
            if (!capabilityType.IsInstanceOfType(capability))
                throw new InvalidOperationException(
                    $"Model capability factory for '{capabilityId}' returned '{capability.GetType().FullName}', " +
                    $"which is not assignable to '{capabilityType.FullName}'.");
            if (!string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Model capability factory for '{capabilityId}' returned a capability with id " +
                    $"'{capability.CapabilityId}'.");

            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a capability by registered type, if a matching factory exists.</para>
        ///     <para xml:lang="zh-CN">按已注册类型创建能力（如果存在匹配工厂）。</para>
        /// </summary>
        public static bool TryCreate<TCapability>(out TCapability capability)
            where TCapability : class, IModelCapability
        {
            var capabilityId = GetCapabilityId<TCapability>();
            if (capabilityId == null ||
                !TryCreate(capabilityId, out var created) ||
                created is not TCapability typed)
            {
                capability = null!;
                return false;
            }

            capability = typed;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a capability by ID or throws when no factory is registered.</para>
        ///     <para xml:lang="zh-CN">通过 ID 创建能力；未注册工厂时抛出异常。</para>
        /// </summary>
        public static IModelCapability Create(string capabilityId)
        {
            return TryCreate(capabilityId, out var capability)
                ? capability
                : throw new InvalidOperationException($"Model capability id is not registered: {capabilityId}");
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a capability by registered type or throws when no matching factory is registered.</para>
        ///     <para xml:lang="zh-CN">按已注册类型创建能力；未注册匹配工厂时抛出异常。</para>
        /// </summary>
        public static TCapability Create<TCapability>()
            where TCapability : class, IModelCapability
        {
            var capabilityId = GetCapabilityId<TCapability>();
            if (capabilityId == null)
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}");

            var capability = Create(capabilityId) as TCapability;
            return capability ?? throw new InvalidOperationException(
                $"Registered capability '{capabilityId}' is not a '{typeof(TCapability).FullName}'.");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered capability ID for a capability type, if any.</para>
        ///     <para xml:lang="zh-CN">获取能力类型已注册的能力 ID（如果存在）。</para>
        /// </summary>
        public static string? GetCapabilityId(Type capabilityType)
        {
            ArgumentNullException.ThrowIfNull(capabilityType);
            lock (SyncRoot)
            {
                return TypeIds.GetValueOrDefault(capabilityType);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered capability ID for <typeparamref name="TCapability" />, if any.</para>
        ///     <para xml:lang="zh-CN">获取 <typeparamref name="TCapability" /> 已注册的能力 ID（如果存在）。</para>
        /// </summary>
        public static string? GetCapabilityId<TCapability>() where TCapability : IModelCapability
        {
            return GetCapabilityId(typeof(TCapability));
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to resolve the capability type registered for <paramref name="capabilityId" />.</para>
        ///     <para xml:lang="zh-CN">尝试解析 <paramref name="capabilityId" /> 注册的能力类型。</para>
        /// </summary>
        public static bool TryGetCapabilityType(string capabilityId, out Type capabilityType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);
            lock (SyncRoot)
            {
                return TypesById.TryGetValue(capabilityId, out capabilityType!);
            }
        }

        internal static IReadOnlyList<ModelCapabilityRegistration> GetRegistrationsSnapshot()
        {
            lock (SyncRoot)
            {
                return Array.AsReadOnly(
                [
                    .. TypesById
                        .Select(static pair => new ModelCapabilityRegistration(pair.Key, pair.Value))
                        .OrderBy(static registration => registration.Id, StringComparer.Ordinal),
                ]);
            }
        }

        internal static bool IsCompatibleWith(Type capabilityType, AbstractModel owner)
        {
            ArgumentNullException.ThrowIfNull(capabilityType);
            ArgumentNullException.ThrowIfNull(owner);
            var typedOwnerTypes = capabilityType.GetInterfaces()
                .Where(static candidate => candidate.IsGenericType &&
                                           candidate.GetGenericTypeDefinition() == typeof(IModelCapability<>))
                .Select(static candidate => candidate.GetGenericArguments()[0])
                .Distinct()
                .ToArray();
            return typedOwnerTypes.Length == 0 || typedOwnerTypes.Any(ownerType => ownerType.IsInstanceOfType(owner));
        }

        internal static void RegisterModelCapability(Type capabilityType, string capabilityId)
        {
            if (!typeof(ModelCapability).IsAssignableFrom(capabilityType))
                throw new ArgumentException("Capability type must inherit ModelCapability.", nameof(capabilityType));

            Register(capabilityId, capabilityType, () => (IModelCapability)ModelDb.Get(capabilityType).MutableClone());
        }

        internal static string GetModelCapabilityId(Type capabilityType)
        {
            return GetCapabilityId(capabilityType) ??
                   throw new InvalidOperationException(
                       $"Model capability type is not registered: {capabilityType.FullName}");
        }
    }
}
