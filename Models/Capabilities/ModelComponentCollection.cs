using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Mutable component collection attached to one model instance.
    ///     附加到单个模型实例上的可变组件集合。
    /// </summary>
    public sealed class ModelComponentCollection
    {
        private readonly List<IModelComponent> _components = [];
        private readonly HashSet<IModelComponent> _defaultComponents = new(ReferenceEqualityComparer.Instance);
        private readonly List<ModelComponentSaveEntry> _unknownEntries = [];

        internal ModelComponentCollection(AbstractModel owner)
        {
            Owner = owner;
        }

        /// <summary>
        ///     Owning model.
        ///     所属模型。
        /// </summary>
        public AbstractModel Owner { get; }

        /// <summary>
        ///     Current attached components.
        ///     当前附加组件。
        /// </summary>
        public IReadOnlyList<IModelComponent> Components => _components;

        internal bool IsDirty { get; private set; }

        /// <summary>
        ///     Number of currently attached components.
        ///     当前附加组件数量。
        /// </summary>
        public int Count => _components.Count;

        /// <summary>
        ///     Applies a component, optionally merging it with an existing component.
        ///     应用组件，并可选择与已有组件合并。
        /// </summary>
        public IModelComponent? Apply(IModelComponent incoming, ApplyModelComponentOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(incoming);

            if (options.AllowMerge)
                for (var i = 0; i < _components.Count; i++)
                {
                    var existing = _components[i];
                    if (existing is not IModelComponentMergeHandler mergeHandler)
                        continue;

                    var didMerge = options.UseSubtractiveMerge
                        ? mergeHandler.TrySubtractiveMergeWith(incoming, options, out var merged)
                        : mergeHandler.TryMergeWith(incoming, options, out merged);

                    if (!didMerge)
                        continue;

                    if (ReferenceEquals(merged, existing))
                    {
                        MarkDynamicVarsJustUpgraded(existing, options);
                        MarkDirty();
                        return existing;
                    }

                    var wasDefault = _defaultComponents.Remove(existing);
                    var defaultComponentId = wasDefault ? existing.ComponentId : null;
                    existing.Detach();

                    if (merged == null)
                    {
                        _components.RemoveAt(i);
                        MarkDirty();
                        return null;
                    }

                    _components[i] = merged;
                    if (defaultComponentId != null &&
                        string.Equals(merged.ComponentId, defaultComponentId, StringComparison.Ordinal))
                        _defaultComponents.Add(merged);
                    merged.Attach(Owner);
                    MarkDynamicVarsJustUpgraded(merged, options);
                    MarkDirty();
                    return merged;
                }

            if (options.UseSubtractiveMerge)
                return null;

            _components.Add(incoming);
            incoming.Attach(Owner);
            MarkDynamicVarsJustUpgraded(incoming, options);
            MarkDirty();
            return incoming;
        }

        /// <summary>
        ///     Applies a component and returns the typed result.
        ///     应用组件并返回类型化结果。
        /// </summary>
        public TComponent? Apply<TComponent>(TComponent incoming, ApplyModelComponentOptions options = new())
            where TComponent : class, IModelComponent
        {
            return Apply((IModelComponent)incoming, options) as TComponent;
        }

        /// <summary>
        ///     Applies several components in order.
        ///     按顺序应用多个组件。
        /// </summary>
        public IReadOnlyList<IModelComponent?> ApplyRange(
            IEnumerable<IModelComponent> components,
            ApplyModelComponentOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(components);

            return components.Select(component => Apply(component, options)).ToList();
        }

        /// <summary>
        ///     Adds a component without subtractive merge behavior.
        ///     添加组件，不使用减法合并行为。
        /// </summary>
        public IModelComponent? Add(IModelComponent component, bool allowMerge = true, bool isUpgrade = false)
        {
            return Apply(component, new(allowMerge, false, isUpgrade));
        }

        /// <summary>
        ///     Adds a component as part of an owner upgrade.
        ///     作为 owner 升级的一部分添加组件。
        /// </summary>
        public IModelComponent? AddForUpgrade(IModelComponent component, bool allowMerge = true)
        {
            return Apply(component, ApplyModelComponentOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Adds a component and returns the typed result.
        ///     添加组件并返回类型化结果。
        /// </summary>
        public TComponent? Add<TComponent>(TComponent component, bool allowMerge = true, bool isUpgrade = false)
            where TComponent : class, IModelComponent
        {
            return Add((IModelComponent)component, allowMerge, isUpgrade) as TComponent;
        }

        /// <summary>
        ///     Adds a component as part of an owner upgrade and returns the typed result.
        ///     作为 owner 升级的一部分添加组件并返回类型化结果。
        /// </summary>
        public TComponent? AddForUpgrade<TComponent>(TComponent component, bool allowMerge = true)
            where TComponent : class, IModelComponent
        {
            return AddForUpgrade((IModelComponent)component, allowMerge) as TComponent;
        }

        /// <summary>
        ///     Creates a registered component and applies it as part of an owner upgrade.
        ///     创建已注册组件，并作为 owner 升级的一部分应用。
        /// </summary>
        public TComponent? AddRegisteredForUpgrade<TComponent>(bool allowMerge = true)
            where TComponent : class, IModelComponent
        {
            var componentId = ModelComponentRegistry.GetComponentId<TComponent>();
            if (componentId == null)
                throw new InvalidOperationException(
                    $"Model component type is not registered: {typeof(TComponent).FullName}");

            return Apply(ModelComponentRegistry.Create(componentId),
                ApplyModelComponentOptions.Upgrade(allowMerge)) as TComponent;
        }

        /// <summary>
        ///     Subtracts a component through merge handlers.
        ///     通过合并处理器减去组件。
        /// </summary>
        public IModelComponent? Subtract(IModelComponent component, bool isUpgrade = false)
        {
            return Apply(component, new(true, true, isUpgrade));
        }

        /// <summary>
        ///     Removes the first component of type <typeparamref name="TComponent" />.
        ///     移除第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public TComponent? Remove<TComponent>() where TComponent : class, IModelComponent
        {
            var index = _components.FindIndex(static c => c is TComponent);
            if (index < 0)
                return null;

            var removed = (TComponent)_components[index];
            removed.Detach();
            _components.RemoveAt(index);
            _defaultComponents.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes the first component with <paramref name="componentId" />.
        ///     移除第一个组件 ID 为 <paramref name="componentId" /> 的组件。
        /// </summary>
        public IModelComponent? Remove(string componentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

            var index = _components.FindIndex(component =>
                string.Equals(component.ComponentId, componentId, StringComparison.Ordinal));
            if (index < 0)
                return null;

            var removed = _components[index];
            removed.Detach();
            _components.RemoveAt(index);
            _defaultComponents.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes this exact component instance.
        ///     移除此组件实例。
        /// </summary>
        public bool Remove(IModelComponent component)
        {
            ArgumentNullException.ThrowIfNull(component);
            var index = _components.FindIndex(c => ReferenceEquals(c, component));
            if (index < 0)
                return false;

            _components[index].Detach();
            _defaultComponents.Remove(_components[index]);
            _components.RemoveAt(index);
            MarkDirty();
            return true;
        }

        /// <summary>
        ///     Removes all components of type <typeparamref name="TComponent" />.
        ///     移除所有 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public IReadOnlyList<TComponent> RemoveAll<TComponent>() where TComponent : class, IModelComponent
        {
            List<TComponent> removed = [];
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                if (_components[i] is not TComponent component)
                    continue;

                component.Detach();
                _components.RemoveAt(i);
                _defaultComponents.Remove(component);
                removed.Add(component);
            }

            if (removed.Count == 0)
                return [];

            removed.Reverse();
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes all components with <paramref name="componentId" />.
        ///     移除所有组件 ID 为 <paramref name="componentId" /> 的组件。
        /// </summary>
        public IReadOnlyList<IModelComponent> RemoveAll(string componentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

            List<IModelComponent> removed = [];
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];
                if (!string.Equals(component.ComponentId, componentId, StringComparison.Ordinal))
                    continue;

                component.Detach();
                _components.RemoveAt(i);
                _defaultComponents.Remove(component);
                removed.Add(component);
            }

            if (removed.Count == 0)
                return [];

            removed.Reverse();
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Clears known components, optionally clearing unknown saved entries as well.
        ///     清空已知组件，并可选择同时清空未知保存条目。
        /// </summary>
        public void Clear(UnknownModelComponentPolicy unknownPolicy = UnknownModelComponentPolicy.Preserve)
        {
            if (_components.Count == 0 &&
                (unknownPolicy == UnknownModelComponentPolicy.Preserve || _unknownEntries.Count == 0))
                return;

            foreach (var component in _components)
                component.Detach();

            _components.Clear();
            _defaultComponents.Clear();
            if (unknownPolicy == UnknownModelComponentPolicy.Remove)
                _unknownEntries.Clear();

            MarkDirty();
        }

        /// <summary>
        ///     Replaces all known components with <paramref name="components" />.
        ///     使用 <paramref name="components" /> 替换所有已知组件。
        /// </summary>
        public void ReplaceAll(
            IEnumerable<IModelComponent> components,
            UnknownModelComponentPolicy unknownPolicy = UnknownModelComponentPolicy.Preserve)
        {
            ArgumentNullException.ThrowIfNull(components);

            foreach (var component in _components)
                component.Detach();

            _components.Clear();
            _defaultComponents.Clear();
            if (unknownPolicy == UnknownModelComponentPolicy.Remove)
                _unknownEntries.Clear();

            foreach (var component in components)
            {
                _components.Add(component);
                component.Attach(Owner);
            }

            MarkDirty();
        }

        /// <summary>
        ///     Gets the first component of type <typeparamref name="TComponent" />.
        ///     获取第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public TComponent? Get<TComponent>() where TComponent : class, IModelComponent
        {
            return _components.OfType<TComponent>().FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to get the first component of type <typeparamref name="TComponent" />.
        ///     尝试获取第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public bool TryGet<TComponent>(out TComponent component) where TComponent : class, IModelComponent
        {
            component = Get<TComponent>()!;
            return component != null;
        }

        /// <summary>
        ///     Gets the first component with <paramref name="componentId" />.
        ///     获取第一个组件 ID 为 <paramref name="componentId" /> 的组件。
        /// </summary>
        public IModelComponent? Get(string componentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

            return _components.FirstOrDefault(component =>
                string.Equals(component.ComponentId, componentId, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Returns true when at least one component of type <typeparamref name="TComponent" /> is attached.
        ///     当至少附加了一个 <typeparamref name="TComponent" /> 类型组件时返回 true。
        /// </summary>
        public bool Contains<TComponent>() where TComponent : class, IModelComponent
        {
            return _components.Any(static c => c is TComponent);
        }

        /// <summary>
        ///     Returns true when at least one component with <paramref name="componentId" /> is attached.
        ///     当至少附加了一个组件 ID 为 <paramref name="componentId" /> 的组件时返回 true。
        /// </summary>
        public bool Contains(string componentId)
        {
            return Get(componentId) != null;
        }

        /// <summary>
        ///     Gets all components of type <typeparamref name="TComponent" />.
        ///     获取所有 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public IReadOnlyList<TComponent> GetAll<TComponent>() where TComponent : class, IModelComponent
        {
            return _components.OfType<TComponent>().ToArray();
        }

        /// <summary>
        ///     Gets all components with <paramref name="componentId" />.
        ///     获取所有组件 ID 为 <paramref name="componentId" /> 的组件。
        /// </summary>
        public IReadOnlyList<IModelComponent> GetAll(string componentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

            return _components
                .Where(component => string.Equals(component.ComponentId, componentId, StringComparison.Ordinal))
                .ToArray();
        }

        /// <summary>
        ///     Gets an existing component of type <typeparamref name="TComponent" />, or applies a new component
        ///     created by <paramref name="factory" />.
        ///     获取已有 <typeparamref name="TComponent" /> 组件；不存在时应用由 <paramref name="factory" /> 创建的新组件。
        /// </summary>
        public TComponent GetOrAdd<TComponent>(
            Func<TComponent> factory,
            ApplyModelComponentOptions options = new())
            where TComponent : class, IModelComponent
        {
            ArgumentNullException.ThrowIfNull(factory);

            var existing = Get<TComponent>();
            if (existing != null)
                return existing;

            var component = Apply(factory(), options);
            return component ?? throw new InvalidOperationException(
                $"Applying component '{typeof(TComponent).FullName}' did not produce a component of that type.");
        }

        /// <summary>
        ///     Gets an existing component of type <typeparamref name="TComponent" />, or creates one from
        ///     <see cref="ModelComponentRegistry" />.
        ///     获取已有 <typeparamref name="TComponent" /> 组件；不存在时通过 <see cref="ModelComponentRegistry" /> 创建。
        /// </summary>
        public TComponent GetOrCreateRegistered<TComponent>(ApplyModelComponentOptions options = new())
            where TComponent : class, IModelComponent
        {
            var existing = Get<TComponent>();
            if (existing != null)
                return existing;

            var componentId = ModelComponentRegistry.GetComponentId<TComponent>();
            if (componentId == null)
                throw new InvalidOperationException(
                    $"Model component type is not registered: {typeof(TComponent).FullName}");

            var component = Apply(ModelComponentRegistry.Create(componentId), options) as TComponent;
            return component ?? throw new InvalidOperationException(
                $"Registered component '{componentId}' is not a '{typeof(TComponent).FullName}'.");
        }

        /// <summary>
        ///     Gets an existing registered component, or creates it as part of an owner upgrade.
        ///     获取已有已注册组件；不存在时作为 owner 升级的一部分创建。
        /// </summary>
        public TComponent GetOrCreateRegisteredForUpgrade<TComponent>(bool allowMerge = true)
            where TComponent : class, IModelComponent
        {
            return GetOrCreateRegistered<TComponent>(ApplyModelComponentOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Enumerates components that implement a capability interface.
        ///     枚举实现某个能力接口的组件。
        /// </summary>
        public IEnumerable<TCapability> Capabilities<TCapability>() where TCapability : class
        {
            return _components.OfType<TCapability>();
        }

        /// <summary>
        ///     Marks the collection dirty after a component mutates itself in place.
        ///     组件原地修改自身后，将 collection 标记为已变更。
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
            ModelComponents.MarkSavedDataDirty(Owner);
        }

        internal bool ShouldSave()
        {
            return IsDirty ||
                   _components.Count > 0 ||
                   _unknownEntries.Count > 0 ||
                   _components.Any(ComponentHasSavedState);
        }

        internal void Load(ModelComponentSaveDocument? document)
        {
            foreach (var component in _components)
                component.Detach(true);

            _components.Clear();
            _defaultComponents.Clear();
            _unknownEntries.Clear();
            IsDirty = false;

            var defaultComponents = CreateDefaultComponents();
            if (document == null)
            {
                AddMissingDefaultComponents(defaultComponents);
                return;
            }

            Load(document, defaultComponents);
        }

        private void Load(ModelComponentSaveDocument document, DefaultComponentLoadState defaultComponents)
        {
            foreach (var entry in document.Components)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                if (defaultComponents.TryTake(entry.Id, out var defaultComponent))
                {
                    LoadComponentState(defaultComponent, entry);
                    AddDefaultComponent(defaultComponent);
                    continue;
                }

                if (!ModelComponentRegistry.TryCreate(entry.Id, out var component))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                LoadComponentState(component, entry);
                _components.Add(component);
                component.Attach(Owner, true);
            }
        }

        internal ModelComponentSaveDocument? Save()
        {
            if (_components.Count == 0 && _unknownEntries.Count == 0 && !IsDirty)
                return null;

            var document = new ModelComponentSaveDocument();
            document.Components.AddRange(_unknownEntries.Select(CloneEntry));

            foreach (var component in _components)
            {
                var state = component as IModelComponentJsonState;
                document.Components.Add(new()
                {
                    Id = component.ComponentId,
                    Schema = state?.SchemaVersion ?? 1,
                    Data = state?.SaveState()?.DeepClone(),
                });
            }

            return document;
        }

        internal void CopyTo(ModelComponentCollection target)
        {
            foreach (var component in target._components)
                component.Detach(true);

            target._components.Clear();
            target._defaultComponents.Clear();
            target._unknownEntries.Clear();
            target._unknownEntries.AddRange(_unknownEntries.Select(CloneEntry));
            target.IsDirty = false;

            foreach (var component in _components)
            {
                var cloned = component is IModelComponentCloneHandler cloneHandler
                    ? cloneHandler.CloneFor(target.Owner)
                    : CloneThroughSave(component, target.Owner);

                target._components.Add(cloned);
                if (_defaultComponents.Contains(component))
                    target._defaultComponents.Add(cloned);

                if (!ReferenceEquals(cloned.Owner, target.Owner))
                    cloned.Attach(target.Owner, true);

                if (cloned is IModelComponentCloneNotification notification)
                    notification.AfterOwnerCloned(Owner, target.Owner, component);
            }

            if (IsDirty || _unknownEntries.Count > 0 ||
                _components.Any(component => !_defaultComponents.Contains(component)))
                target.MarkDirty();
        }

        private DefaultComponentLoadState CreateDefaultComponents()
        {
            var state = new DefaultComponentLoadState();
            foreach (var component in ModelDefaultComponents.Create(Owner))
                state.Add(component);

            return state;
        }

        private void AddDefaultComponent(IModelComponent component)
        {
            _components.Add(component);
            _defaultComponents.Add(component);
            component.Attach(Owner, true);
        }

        private void AddMissingDefaultComponents(DefaultComponentLoadState defaultComponents)
        {
            foreach (var component in defaultComponents.TakeRemaining())
                AddDefaultComponent(component);
        }

        private static void LoadComponentState(IModelComponent component, ModelComponentSaveEntry entry)
        {
            if (component is IModelComponentJsonState state)
                state.LoadState(entry.Data?.DeepClone(), entry.Schema);
        }

        internal void MarkDirtyFromHost()
        {
            IsDirty = true;
        }

        private static bool ComponentHasSavedState(IModelComponent component)
        {
            return component is IModelComponentJsonState state && state.SaveState() != null;
        }

        private static void MarkDynamicVarsJustUpgraded(
            IModelComponent component,
            ApplyModelComponentOptions options)
        {
            if (options.IsUpgrade && component is ModelComponent modelComponent)
                modelComponent.MarkDynamicVarsJustUpgraded();
        }

        private static IModelComponent CloneThroughSave(IModelComponent component, AbstractModel clonedOwner)
        {
            if (!ModelComponentRegistry.TryCreate(component.ComponentId, out var clone))
                throw new InvalidOperationException($"Cannot clone unknown model component '{component.ComponentId}'.");

            if (component is IModelComponentJsonState sourceState && clone is IModelComponentJsonState targetState)
                targetState.LoadState(sourceState.SaveState()?.DeepClone(), sourceState.SchemaVersion);

            clone.Attach(clonedOwner, true);
            return clone;
        }

        private static ModelComponentSaveEntry CloneEntry(ModelComponentSaveEntry entry)
        {
            return new()
            {
                Id = entry.Id,
                Schema = entry.Schema,
                Data = entry.Data?.DeepClone(),
            };
        }

        private sealed class DefaultComponentLoadState
        {
            private readonly Dictionary<string, Queue<IModelComponent>> _queues = new(StringComparer.Ordinal);
            private readonly List<IModelComponent> _remaining = [];

            public void Add(IModelComponent component)
            {
                _remaining.Add(component);
                if (!_queues.TryGetValue(component.ComponentId, out var queue))
                {
                    queue = new();
                    _queues[component.ComponentId] = queue;
                }

                queue.Enqueue(component);
            }

            public bool TryTake(string componentId, out IModelComponent component)
            {
                component = null!;
                if (!_queues.TryGetValue(componentId, out var queue) || queue.Count == 0)
                    return false;

                component = queue.Dequeue();
                var taken = component;
                var index = _remaining.FindIndex(candidate => ReferenceEquals(candidate, taken));
                if (index >= 0)
                    _remaining.RemoveAt(index);

                return true;
            }

            public IReadOnlyList<IModelComponent> TakeRemaining()
            {
                var remaining = _remaining.ToArray();
                _remaining.Clear();

                foreach (var queue in _queues.Values)
                    queue.Clear();

                return remaining;
            }
        }
    }
}
