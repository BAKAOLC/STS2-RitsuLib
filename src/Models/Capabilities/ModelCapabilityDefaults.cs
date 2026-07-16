using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Mutable list used while resolving a model's default capabilities.
    ///     解析模型默认能力时使用的可变列表。
    /// </summary>
    public sealed class ModelCapabilityList
    {
        private readonly List<IModelCapability> _capabilities = [];

        /// <summary>
        ///     All capabilities in default execution order.
        ///     默认执行顺序中的所有能力。
        /// </summary>
        public IReadOnlyList<IModelCapability> All => _capabilities;

        /// <summary>
        ///     Number of capabilities in the list.
        ///     列表中的能力数量。
        /// </summary>
        public int Count => _capabilities.Count;

        /// <summary>
        ///     Adds an already-created capability instance to the end of the list.
        ///     添加一个已经创建好的能力实例到列表末尾。
        /// </summary>
        /// <remarks>
        ///     Use this overload for plain, non-model-backed <see cref="IModelCapability" /> implementations or
        ///     for instances that already came from the registry. Do not call constructors for types inheriting
        ///     <see cref="ModelCapability" /> here; register the type with <c>RegisterModelCapability</c> and add
        ///     it with <see cref="Add{TCapability}()" /> or <see cref="Add(Type)" /> so creation goes through
        ///     <c>ModelDb</c>.
        ///     此重载用于普通的、非模型化 <see cref="IModelCapability" /> 实现，或已由 registry 创建的实例。
        ///     不要在这里直接构造继承 <see cref="ModelCapability" /> 的类型；应先用
        ///     <c>RegisterModelCapability</c> 注册类型，再通过 <see cref="Add{TCapability}()" /> 或
        ///     <see cref="Add(Type)" /> 添加，让创建流程经过 <c>ModelDb</c>。
        /// </remarks>
        public IModelCapability Add(IModelCapability capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            if (capability is ModelCapability &&
                ModelCapabilityRegistry.GetCapabilityId(capability.GetType()) == null)
                throw new InvalidOperationException(
                    $"Model-backed capability type is not registered: {capability.GetType().FullName}. " +
                    "Register it with RegisterModelCapability and add it by type instead of passing a constructed instance.");

            _capabilities.Add(capability);
            return capability;
        }

        /// <summary>
        ///     Creates a capability and adds it to the end of the list.
        ///     创建能力并添加到列表末尾。
        /// </summary>
        /// <remarks>
        ///     This is the preferred default-capability path for registered <see cref="ModelCapability" /> types.
        ///     Registered model-backed capabilities are created through <c>ModelDb</c>, not by direct constructors.
        ///     这是注册过的 <see cref="ModelCapability" /> 类型在默认能力中的首选入口。
        ///     已注册的模型化能力会通过 <c>ModelDb</c> 创建，而不是直接调用构造函数。
        /// </remarks>
        public TCapability Add<TCapability>() where TCapability : class, IModelCapability
        {
            var capability = CreateCapability<TCapability>();
            Add(capability);
            return capability;
        }

        /// <summary>
        ///     Creates a capability of <paramref name="capabilityType" /> and adds it to the end of the list.
        ///     创建 <paramref name="capabilityType" /> 类型能力并添加到列表末尾。
        /// </summary>
        /// <remarks>
        ///     Use this overload when the capability type is only known at runtime. Registered
        ///     <see cref="ModelCapability" /> types are created through <c>ModelDb</c>.
        ///     当能力类型只在运行时已知时使用此重载。已注册的 <see cref="ModelCapability" /> 类型会通过
        ///     <c>ModelDb</c> 创建。
        /// </remarks>
        public IModelCapability Add(Type capabilityType)
        {
            var capability = CreateCapability(capabilityType);
            Add(capability);
            return capability;
        }

        /// <summary>
        ///     Creates a registered capability and adds it to the end of the list.
        ///     创建已注册能力并添加到列表末尾。
        /// </summary>
        public TCapability AddFromRegistry<TCapability>() where TCapability : class, IModelCapability
        {
            var capability = CreateFromRegistry<TCapability>();
            Add(capability);
            return capability;
        }

        /// <summary>
        ///     Inserts a created capability at <paramref name="index" />.
        ///     创建能力并插入到 <paramref name="index" />。
        /// </summary>
        public TCapability Insert<TCapability>(int index) where TCapability : class, IModelCapability
        {
            var capability = CreateCapability<TCapability>();
            Insert(index, capability);
            return capability;
        }

        /// <summary>
        ///     Creates a capability of <paramref name="capabilityType" /> and inserts it at <paramref name="index" />.
        ///     创建 <paramref name="capabilityType" /> 类型能力并插入到 <paramref name="index" />。
        /// </summary>
        public IModelCapability Insert(int index, Type capabilityType)
        {
            var capability = CreateCapability(capabilityType);
            Insert(index, capability);
            return capability;
        }

        private static TCapability CreateCapability<TCapability>() where TCapability : class, IModelCapability
        {
            var capabilityId = ModelCapabilityRegistry.GetCapabilityId<TCapability>();
            if (capabilityId != null)
                return CreateFromRegistry<TCapability>(capabilityId);

            if (typeof(ModelCapability).IsAssignableFrom(typeof(TCapability)))
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}. " +
                    "Register it with RegisterModelCapability before adding it to a default capability list.");

            var capability = Activator.CreateInstance<TCapability>();
            return capability ?? throw new InvalidOperationException(
                $"Capability type must have a public parameterless constructor: {typeof(TCapability).FullName}");
        }

        private static IModelCapability CreateCapability(Type capabilityType)
        {
            ArgumentNullException.ThrowIfNull(capabilityType);
            if (capabilityType.ContainsGenericParameters ||
                capabilityType.IsAbstract ||
                capabilityType.IsInterface ||
                !typeof(IModelCapability).IsAssignableFrom(capabilityType))
                throw new ArgumentException(
                    $"Type '{capabilityType.FullName}' must be a concrete implementation of IModelCapability.",
                    nameof(capabilityType));

            var capabilityId = ModelCapabilityRegistry.GetCapabilityId(capabilityType);
            if (capabilityId != null)
                return ModelCapabilityRegistry.Create(capabilityId);

            if (typeof(ModelCapability).IsAssignableFrom(capabilityType))
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {capabilityType.FullName}. " +
                    "Register it with RegisterModelCapability before adding it to a default capability list.");

            var capability = Activator.CreateInstance(capabilityType) as IModelCapability;
            return capability ?? throw new InvalidOperationException(
                $"Capability type must have a public parameterless constructor: {capabilityType.FullName}");
        }

        private static TCapability CreateFromRegistry<TCapability>(string? capabilityId = null)
            where TCapability : class, IModelCapability
        {
            return capabilityId == null
                ? ModelCapabilityRegistry.Create<TCapability>()
                : ModelCapabilityRegistry.Create(capabilityId) as TCapability ??
                  throw new InvalidOperationException(
                      $"Registered capability '{capabilityId}' is not a '{typeof(TCapability).FullName}'.");
        }

        /// <summary>
        ///     Adds all <paramref name="capabilities" /> to the end of the list.
        ///     将所有 <paramref name="capabilities" /> 添加到列表末尾。
        /// </summary>
        public void AddRange(IEnumerable<IModelCapability> capabilities)
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            foreach (var capability in capabilities)
                Add(capability);
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> at <paramref name="index" />.
        ///     在 <paramref name="index" /> 插入 <paramref name="capability" />。
        /// </summary>
        public IModelCapability Insert(int index, IModelCapability capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            if (index < 0 || index > _capabilities.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the list bounds.");

            _capabilities.Insert(index, capability);
            return capability;
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> before the first capability of type
        ///     <typeparamref name="TExisting" />.
        ///     将 <paramref name="capability" /> 插入到第一个 <typeparamref name="TExisting" /> 能力之前。
        /// </summary>
        public IModelCapability? InsertBefore<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertRelativeTo<TExisting>(capability, false, missingAnchorPolicy);
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> after the first capability of type
        ///     <typeparamref name="TExisting" />.
        ///     将 <paramref name="capability" /> 插入到第一个 <typeparamref name="TExisting" /> 能力之后。
        /// </summary>
        public IModelCapability? InsertAfter<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertRelativeTo<TExisting>(capability, true, missingAnchorPolicy);
        }

        /// <summary>
        ///     Shorthand for <see cref="InsertBefore{TExisting}" />.
        ///     <see cref="InsertBefore{TExisting}" /> 的简写。
        /// </summary>
        public IModelCapability? Before<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertBefore<TExisting>(capability, missingAnchorPolicy);
        }

        /// <summary>
        ///     Shorthand for <see cref="InsertAfter{TExisting}" />.
        ///     <see cref="InsertAfter{TExisting}" /> 的简写。
        /// </summary>
        public IModelCapability? After<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertAfter<TExisting>(capability, missingAnchorPolicy);
        }

        /// <summary>
        ///     Removes the first capability of type <typeparamref name="TCapability" />.
        ///     移除第一个 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public TCapability? Remove<TCapability>() where TCapability : class, IModelCapability
        {
            var index = _capabilities.FindIndex(static capability => capability is TCapability);
            if (index < 0)
                return null;

            var removed = (TCapability)_capabilities[index];
            _capabilities.RemoveAt(index);
            return removed;
        }

        /// <summary>
        ///     Removes every capability of type <typeparamref name="TCapability" />.
        ///     移除所有 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public IReadOnlyList<TCapability> RemoveAll<TCapability>() where TCapability : class, IModelCapability
        {
            List<TCapability> removed = [];
            for (var i = _capabilities.Count - 1; i >= 0; i--)
            {
                if (_capabilities[i] is not TCapability capability)
                    continue;

                _capabilities.RemoveAt(i);
                removed.Add(capability);
            }

            removed.Reverse();
            return removed;
        }

        /// <summary>
        ///     Replaces the first capability of type <typeparamref name="TCapability" />.
        ///     替换第一个 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public bool Replace<TCapability>(IModelCapability replacement) where TCapability : class, IModelCapability
        {
            ArgumentNullException.ThrowIfNull(replacement);

            var index = _capabilities.FindIndex(static capability => capability is TCapability);
            if (index < 0)
                return false;

            _capabilities[index] = replacement;
            return true;
        }

        /// <summary>
        ///     Gets the first capability of type <typeparamref name="TCapability" />.
        ///     获取第一个 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public TCapability? Get<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.OfType<TCapability>().FirstOrDefault();
        }

        /// <summary>
        ///     Gets all capabilities of type <typeparamref name="TCapability" />.
        ///     获取所有 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.OfType<TCapability>().ToArray();
        }

        /// <summary>
        ///     Returns true when the list contains a capability of type <typeparamref name="TCapability" />.
        ///     当列表包含 <typeparamref name="TCapability" /> 类型能力时返回 true。
        /// </summary>
        public bool Contains<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.Any(static capability => capability is TCapability);
        }

        private IModelCapability? InsertRelativeTo<TExisting>(
            IModelCapability capability,
            bool after,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy)
            where TExisting : class, IModelCapability
        {
            ArgumentNullException.ThrowIfNull(capability);

            var index = _capabilities.FindIndex(static existing => existing is TExisting);
            if (index >= 0)
                return Insert(after ? index + 1 : index, capability);

            return missingAnchorPolicy switch
            {
                MissingModelCapabilityAnchorPolicy.Append => Insert(_capabilities.Count, capability),
                MissingModelCapabilityAnchorPolicy.Prepend => Insert(0, capability),
                MissingModelCapabilityAnchorPolicy.Skip => null,
                MissingModelCapabilityAnchorPolicy.Throw => throw new InvalidOperationException(
                    $"Cannot find capability anchor '{typeof(TExisting).FullName}' in the default capability list."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(missingAnchorPolicy),
                    missingAnchorPolicy,
                    "Unknown missing anchor policy."),
            };
        }

        internal IModelCapability[] ToArray()
        {
            return _capabilities.ToArray();
        }
    }

    internal static class ModelCapabilityDefaults
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<ModelDefaultCapabilityModifierEntry> Modifiers = [];
        private static Dictionary<Type, ModelDefaultCapabilityModifierEntry[]> _modifiersByOwnerType = [];
        private static long _nextOrder;

        public static void Modify<TModel>(
            string modId,
            string modifierId,
            Action<TModel, ModelCapabilityList> modifier,
            int order = 0)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(modifier);
            Modify(
                modId,
                modifierId,
                typeof(TModel),
                (model, capabilities) => modifier((TModel)model, capabilities),
                order);
        }

        public static void Modify(
            string modId,
            string modifierId,
            Type ownerType,
            Action<AbstractModel, ModelCapabilityList> modifier,
            int order = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(modifierId);
            ArgumentNullException.ThrowIfNull(ownerType);
            ArgumentNullException.ThrowIfNull(modifier);

            if (ownerType.ContainsGenericParameters ||
                ownerType.IsInterface ||
                !typeof(AbstractModel).IsAssignableFrom(ownerType))
                throw new ArgumentException(
                    $"Type '{ownerType.FullName}' must be an abstract model type or a concrete model type.",
                    nameof(ownerType));

            lock (SyncRoot)
            {
                if (Modifiers.Any(entry =>
                        string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(entry.ModifierId, modifierId, StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        $"Default capability modifier is already registered: {modId}/{modifierId}");

                Modifiers.Add(new(
                    modId,
                    modifierId,
                    ownerType,
                    order,
                    _nextOrder++,
                    modifier));
                Volatile.Write(ref _modifiersByOwnerType, []);
            }
        }

        internal static bool HasDefaultCapabilitySource(AbstractModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (owner is IModelCapabilitySource)
                return true;

            return GetModifiers(owner.GetType()).Length > 0;
        }

        internal static IReadOnlyList<IModelCapability> Create(AbstractModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            var capabilities = new ModelCapabilityList();
            if (owner is IModelCapabilitySource provider)
                TryRunProvider(owner, provider, capabilities);

            foreach (var modifier in GetModifiers(owner))
                TryRunModifier(owner, modifier, capabilities);

            return capabilities.ToArray();
        }

        private static ModelDefaultCapabilityModifierEntry[] GetModifiers(AbstractModel owner)
        {
            return GetModifiers(owner.GetType());
        }

        private static ModelDefaultCapabilityModifierEntry[] GetModifiers(Type ownerType)
        {
            var cache = Volatile.Read(ref _modifiersByOwnerType);
            if (cache.TryGetValue(ownerType, out var modifiers))
                return modifiers;

            lock (SyncRoot)
            {
                cache = Volatile.Read(ref _modifiersByOwnerType);
                if (cache.TryGetValue(ownerType, out modifiers))
                    return modifiers;

                modifiers = Modifiers
                    .Where(entry => entry.OwnerType.IsAssignableFrom(ownerType))
                    .OrderBy(static entry => entry.Order)
                    .ThenBy(static entry => entry.RegistrationOrder)
                    .ToArray();
                var updatedCache = new Dictionary<Type, ModelDefaultCapabilityModifierEntry[]>(cache)
                {
                    [ownerType] = modifiers,
                };
                Volatile.Write(ref _modifiersByOwnerType, updatedCache);
                return modifiers;
            }
        }

        private static void TryRunProvider(
            AbstractModel owner,
            IModelCapabilitySource provider,
            ModelCapabilityList capabilities)
        {
            try
            {
                provider.BuildDefaultCapabilities(capabilities);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelCapabilities] Default capability provider failed for {owner.Id}: {ex.Message}");
            }
        }

        private static void TryRunModifier(
            AbstractModel owner,
            ModelDefaultCapabilityModifierEntry modifier,
            ModelCapabilityList capabilities)
        {
            try
            {
                modifier.Modify(owner, capabilities);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelCapabilities] Default capability modifier '{modifier.ModId}/{modifier.ModifierId}' failed for {owner.Id}: {ex.Message}");
            }
        }

        private readonly record struct ModelDefaultCapabilityModifierEntry(
            string ModId,
            string ModifierId,
            Type OwnerType,
            int Order,
            long RegistrationOrder,
            // ReSharper disable once MemberHidesStaticFromOuterClass
            Action<AbstractModel, ModelCapabilityList> Modify);
    }
}
