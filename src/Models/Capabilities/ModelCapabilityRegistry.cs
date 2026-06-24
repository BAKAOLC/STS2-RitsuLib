using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Registry for capability ids and factories.
    ///     能力 ID 与工厂的注册表。
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
        ///     Registers or replaces a capability factory.
        ///     注册或替换能力工厂。
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
        ///     Registers a capability factory.
        ///     注册能力工厂。
        /// </summary>
        public static void Register<TCapability>(string capabilityId, Func<TCapability> factory)
            where TCapability : IModelCapability
        {
            ArgumentNullException.ThrowIfNull(factory);
            Register(capabilityId, typeof(TCapability), () => factory());
        }

        /// <summary>
        ///     Registers a parameterless capability factory.
        ///     注册无参能力工厂。
        /// </summary>
        public static void Register<TCapability>(string capabilityId)
            where TCapability : IModelCapability, new()
        {
            Register(capabilityId, static () => new TCapability());
        }

        /// <summary>
        ///     Creates a capability by id.
        ///     通过 ID 创建能力。
        /// </summary>
        public static bool TryCreate(string capabilityId, out IModelCapability capability)
        {
            lock (SyncRoot)
            {
                if (!Factories.TryGetValue(capabilityId, out var factory))
                {
                    capability = null!;
                    return false;
                }

                capability = factory();
                return true;
            }
        }

        /// <summary>
        ///     Creates a capability by registered type, if a matching factory exists.
        ///     按已注册类型创建能力（如果存在匹配工厂）。
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
        ///     Creates a capability by id or throws when no factory is registered.
        ///     通过 ID 创建能力；未注册工厂时抛出异常。
        /// </summary>
        public static IModelCapability Create(string capabilityId)
        {
            return TryCreate(capabilityId, out var capability)
                ? capability
                : throw new InvalidOperationException($"Model capability id is not registered: {capabilityId}");
        }

        /// <summary>
        ///     Creates a capability by registered type or throws when no matching factory is registered.
        ///     按已注册类型创建能力；未注册匹配工厂时抛出异常。
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
        ///     Gets the registered capability id for a capability type, if any.
        ///     获取能力类型已注册的能力 ID（如果存在）。
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
        ///     Gets the registered capability id for <typeparamref name="TCapability" />, if any.
        ///     获取 <typeparamref name="TCapability" /> 已注册的能力 ID（如果存在）。
        /// </summary>
        public static string? GetCapabilityId<TCapability>() where TCapability : IModelCapability
        {
            return GetCapabilityId(typeof(TCapability));
        }

        /// <summary>
        ///     Attempts to resolve the capability type registered for <paramref name="capabilityId" />.
        ///     尝试解析 <paramref name="capabilityId" /> 注册的能力类型。
        /// </summary>
        public static bool TryGetCapabilityType(string capabilityId, out Type capabilityType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);
            lock (SyncRoot)
            {
                return TypesById.TryGetValue(capabilityId, out capabilityType!);
            }
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
