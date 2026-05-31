using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Registry for capability ids and factories.
    ///     组件 ID 与工厂的注册表。
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
        ///     Registers or replaces a component factory.
        ///     注册或替换组件工厂。
        /// </summary>
        public static void Register(string capabilityId, Type capabilityType, Func<IModelCapability> factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);
            ArgumentNullException.ThrowIfNull(capabilityType);
            ArgumentNullException.ThrowIfNull(factory);

            if (!typeof(IModelCapability).IsAssignableFrom(capabilityType))
                throw new ArgumentException("Component type must implement IModelCapability.", nameof(capabilityType));

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
        ///     Registers a component factory.
        ///     注册组件工厂。
        /// </summary>
        public static void Register<TCapability>(string capabilityId, Func<TCapability> factory)
            where TCapability : IModelCapability
        {
            ArgumentNullException.ThrowIfNull(factory);
            Register(capabilityId, typeof(TCapability), () => factory());
        }

        /// <summary>
        ///     Registers a parameterless component factory.
        ///     注册无参组件工厂。
        /// </summary>
        public static void Register<TCapability>(string capabilityId)
            where TCapability : IModelCapability, new()
        {
            Register(capabilityId, static () => new TCapability());
        }

        /// <summary>
        ///     Creates a component by id.
        ///     通过 ID 创建组件。
        /// </summary>
        public static bool TryCreate(string capabilityId, out IModelCapability component)
        {
            lock (SyncRoot)
            {
                if (!Factories.TryGetValue(capabilityId, out var factory))
                {
                    component = null!;
                    return false;
                }

                component = factory();
                return true;
            }
        }

        /// <summary>
        ///     Creates a component by id or throws when no factory is registered.
        ///     通过 ID 创建组件；未注册工厂时抛出异常。
        /// </summary>
        public static IModelCapability Create(string capabilityId)
        {
            return TryCreate(capabilityId, out var component)
                ? component
                : throw new InvalidOperationException($"Model capability id is not registered: {capabilityId}");
        }

        /// <summary>
        ///     Gets the registered capability id for a capability type, if any.
        ///     获取组件类型已注册的组件 ID（如果存在）。
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
        ///     获取 <typeparamref name="TCapability" /> 已注册的组件 ID（如果存在）。
        /// </summary>
        public static string? GetCapabilityId<TCapability>() where TCapability : IModelCapability
        {
            return GetCapabilityId(typeof(TCapability));
        }

        /// <summary>
        ///     Attempts to resolve the capability type registered for <paramref name="capabilityId" />.
        ///     尝试解析 <paramref name="capabilityId" /> 注册的组件类型。
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
                throw new ArgumentException("Component type must inherit ModelCapability.", nameof(capabilityType));

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
