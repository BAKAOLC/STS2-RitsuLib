using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Mutable list used while resolving a model's default components.
    ///     解析模型默认组件时使用的可变列表。
    /// </summary>
    public sealed class ModelCapabilityList
    {
        private readonly List<IModelCapability> _components = [];

        /// <summary>
        ///     Current components in default order.
        ///     当前默认顺序中的组件。
        /// </summary>
        public IReadOnlyList<IModelCapability> Items => _components;

        /// <summary>
        ///     Number of components in the list.
        ///     列表中的组件数量。
        /// </summary>
        public int Count => _components.Count;

        /// <summary>
        ///     Adds <paramref name="component" /> to the end of the list.
        ///     将 <paramref name="component" /> 添加到列表末尾。
        /// </summary>
        public IModelCapability Add(IModelCapability component)
        {
            ArgumentNullException.ThrowIfNull(component);
            _components.Add(component);
            return component;
        }

        /// <summary>
        ///     Creates a component and adds it to the end of the list.
        ///     创建组件并添加到列表末尾。
        /// </summary>
        public TCapability Add<TCapability>() where TCapability : class, IModelCapability
        {
            var component = CreateCapability<TCapability>();
            Add(component);
            return component;
        }

        /// <summary>
        ///     Creates a component of <paramref name="capabilityType" /> and adds it to the end of the list.
        ///     创建 <paramref name="capabilityType" /> 类型组件并添加到列表末尾。
        /// </summary>
        public IModelCapability Add(Type capabilityType)
        {
            var component = CreateComponent(capabilityType);
            Add(component);
            return component;
        }

        /// <summary>
        ///     Creates a registered capability and adds it to the end of the list.
        ///     创建已注册组件并添加到列表末尾。
        /// </summary>
        public TCapability AddFromRegistry<TCapability>() where TCapability : class, IModelCapability
        {
            var component = CreateFromRegistry<TCapability>();
            Add(component);
            return component;
        }

        /// <summary>
        ///     Inserts a created component at <paramref name="index" />.
        ///     创建组件并插入到 <paramref name="index" />。
        /// </summary>
        public TCapability Insert<TCapability>(int index) where TCapability : class, IModelCapability
        {
            var component = CreateCapability<TCapability>();
            Insert(index, component);
            return component;
        }

        /// <summary>
        ///     Creates a component of <paramref name="capabilityType" /> and inserts it at <paramref name="index" />.
        ///     创建 <paramref name="capabilityType" /> 类型组件并插入到 <paramref name="index" />。
        /// </summary>
        public IModelCapability Insert(int index, Type capabilityType)
        {
            var component = CreateComponent(capabilityType);
            Insert(index, component);
            return component;
        }

        private static TCapability CreateCapability<TCapability>() where TCapability : class, IModelCapability
        {
            var capabilityId = ModelCapabilityRegistry.GetCapabilityId<TCapability>();
            if (capabilityId != null)
                return CreateFromRegistry<TCapability>(capabilityId);

            if (typeof(ModelCapability).IsAssignableFrom(typeof(TCapability)))
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}");

            var component = Activator.CreateInstance(typeof(TCapability)) as TCapability;
            return component ?? throw new InvalidOperationException(
                $"Component type must have a public parameterless constructor: {typeof(TCapability).FullName}");
        }

        private static IModelCapability CreateComponent(Type capabilityType)
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
                    $"Model capability type is not registered: {capabilityType.FullName}");

            var component = Activator.CreateInstance(capabilityType) as IModelCapability;
            return component ?? throw new InvalidOperationException(
                $"Component type must have a public parameterless constructor: {capabilityType.FullName}");
        }

        private static TCapability CreateFromRegistry<TCapability>(string? capabilityId = null)
            where TCapability : class, IModelCapability
        {
            capabilityId ??= ModelCapabilityRegistry.GetCapabilityId<TCapability>();
            if (capabilityId == null)
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}");

            var component = ModelCapabilityRegistry.Create(capabilityId) as TCapability;
            return component ?? throw new InvalidOperationException(
                $"Registered capability '{capabilityId}' is not a '{typeof(TCapability).FullName}'.");
        }

        /// <summary>
        ///     Adds all <paramref name="components" /> to the end of the list.
        ///     将所有 <paramref name="components" /> 添加到列表末尾。
        /// </summary>
        public void AddRange(IEnumerable<IModelCapability> components)
        {
            ArgumentNullException.ThrowIfNull(components);
            foreach (var component in components)
                Add(component);
        }

        /// <summary>
        ///     Inserts <paramref name="component" /> at <paramref name="index" />.
        ///     在 <paramref name="index" /> 插入 <paramref name="component" />。
        /// </summary>
        public IModelCapability Insert(int index, IModelCapability component)
        {
            ArgumentNullException.ThrowIfNull(component);
            if (index < 0 || index > _components.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the list bounds.");

            _components.Insert(index, component);
            return component;
        }

        /// <summary>
        ///     Inserts <paramref name="component" /> before the first component of type
        ///     <typeparamref name="TExisting" />.
        ///     将 <paramref name="component" /> 插入到第一个 <typeparamref name="TExisting" /> 组件之前。
        /// </summary>
        public bool InsertBefore<TExisting>(IModelCapability component) where TExisting : class, IModelCapability
        {
            var index = _components.FindIndex(static existing => existing is TExisting);
            if (index < 0)
                return false;

            Insert(index, component);
            return true;
        }

        /// <summary>
        ///     Inserts <paramref name="component" /> after the first component of type
        ///     <typeparamref name="TExisting" />.
        ///     将 <paramref name="component" /> 插入到第一个 <typeparamref name="TExisting" /> 组件之后。
        /// </summary>
        public bool InsertAfter<TExisting>(IModelCapability component) where TExisting : class, IModelCapability
        {
            var index = _components.FindIndex(static existing => existing is TExisting);
            if (index < 0)
                return false;

            Insert(index + 1, component);
            return true;
        }

        /// <summary>
        ///     Removes the first component of type <typeparamref name="TCapability" />.
        ///     移除第一个 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public TCapability? Remove<TCapability>() where TCapability : class, IModelCapability
        {
            var index = _components.FindIndex(static component => component is TCapability);
            if (index < 0)
                return null;

            var removed = (TCapability)_components[index];
            _components.RemoveAt(index);
            return removed;
        }

        /// <summary>
        ///     Removes every component of type <typeparamref name="TCapability" />.
        ///     移除所有 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public IReadOnlyList<TCapability> RemoveAll<TCapability>() where TCapability : class, IModelCapability
        {
            List<TCapability> removed = [];
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                if (_components[i] is not TCapability component)
                    continue;

                _components.RemoveAt(i);
                removed.Add(component);
            }

            removed.Reverse();
            return removed;
        }

        /// <summary>
        ///     Replaces the first component of type <typeparamref name="TCapability" />.
        ///     替换第一个 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public bool Replace<TCapability>(IModelCapability replacement) where TCapability : class, IModelCapability
        {
            ArgumentNullException.ThrowIfNull(replacement);

            var index = _components.FindIndex(static component => component is TCapability);
            if (index < 0)
                return false;

            _components[index] = replacement;
            return true;
        }

        /// <summary>
        ///     Gets the first component of type <typeparamref name="TCapability" />.
        ///     获取第一个 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public TCapability? Get<TCapability>() where TCapability : class, IModelCapability
        {
            return _components.OfType<TCapability>().FirstOrDefault();
        }

        /// <summary>
        ///     Gets all components of type <typeparamref name="TCapability" />.
        ///     获取所有 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, IModelCapability
        {
            return _components.OfType<TCapability>().ToArray();
        }

        /// <summary>
        ///     Returns true when the list contains a component of type <typeparamref name="TCapability" />.
        ///     当列表包含 <typeparamref name="TCapability" /> 类型组件时返回 true。
        /// </summary>
        public bool Contains<TCapability>() where TCapability : class, IModelCapability
        {
            return _components.Any(static component => component is TCapability);
        }

        internal IModelCapability[] ToArray()
        {
            return _components.ToArray();
        }
    }

    internal static class ModelCapabilityDefaults
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<ModelDefaultCapabilityModifierEntry> Modifiers = [];
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
                (model, components) => modifier((TModel)model, components),
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
                        $"Default component modifier is already registered: {modId}/{modifierId}");

                Modifiers.Add(new(
                    modId,
                    modifierId,
                    ownerType,
                    order,
                    _nextOrder++,
                    modifier));
            }
        }

        internal static bool HasDefaultCapabilitySource(AbstractModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (owner is IModelCapabilitySource)
                return true;

            var ownerType = owner.GetType();
            lock (SyncRoot)
            {
                return Modifiers.Any(entry => entry.OwnerType.IsAssignableFrom(ownerType));
            }
        }

        internal static IReadOnlyList<IModelCapability> Create(AbstractModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            var components = new ModelCapabilityList();
            if (owner is IModelCapabilitySource provider)
                TryRunProvider(owner, provider, components);

            foreach (var modifier in GetModifiers(owner))
                TryRunModifier(owner, modifier, components);

            return components.ToArray();
        }

        private static ModelDefaultCapabilityModifierEntry[] GetModifiers(AbstractModel owner)
        {
            var ownerType = owner.GetType();
            lock (SyncRoot)
            {
                return Modifiers
                    .Where(entry => entry.OwnerType.IsAssignableFrom(ownerType))
                    .OrderBy(static entry => entry.Order)
                    .ThenBy(static entry => entry.RegistrationOrder)
                    .ToArray();
            }
        }

        private static void TryRunProvider(
            AbstractModel owner,
            IModelCapabilitySource provider,
            ModelCapabilityList components)
        {
            try
            {
                provider.BuildDefaultCapabilities(components);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelCapabilities] Default component provider failed for {owner.Id}: {ex.Message}");
            }
        }

        private static void TryRunModifier(
            AbstractModel owner,
            ModelDefaultCapabilityModifierEntry modifier,
            ModelCapabilityList components)
        {
            try
            {
                modifier.Modify(owner, components);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelCapabilities] Default component modifier '{modifier.ModId}/{modifier.ModifierId}' failed for {owner.Id}: {ex.Message}");
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
