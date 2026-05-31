using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Mutable capability set attached to one model instance.
    ///     附加到单个模型实例上的可变组件集合。
    /// </summary>
    public sealed class ModelCapabilitySet
    {
        private readonly List<IModelCapability> _components = [];
        private readonly HashSet<IModelCapability> _defaultCapabilities = new(ReferenceEqualityComparer.Instance);
        private readonly List<ModelCapabilitySaveEntry> _unknownEntries = [];

        internal ModelCapabilitySet(AbstractModel owner)
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
        public IReadOnlyList<IModelCapability> Items => _components;

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
        public IModelCapability? Apply(IModelCapability incoming, ApplyModelCapabilityOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(incoming);

            if (options.AllowMerge)
                for (var i = 0; i < _components.Count; i++)
                {
                    var existing = _components[i];
                    if (existing is not IModelCapabilityMergeHandler mergeHandler)
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

                    var wasDefault = _defaultCapabilities.Remove(existing);
                    var defaultCapabilityId = wasDefault ? existing.CapabilityId : null;
                    existing.Detach();

                    if (merged == null)
                    {
                        _components.RemoveAt(i);
                        MarkDirty();
                        return null;
                    }

                    _components[i] = merged;
                    if (defaultCapabilityId != null &&
                        string.Equals(merged.CapabilityId, defaultCapabilityId, StringComparison.Ordinal))
                        _defaultCapabilities.Add(merged);
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
        public TCapability? Apply<TCapability>(TCapability incoming, ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return Apply((IModelCapability)incoming, options) as TCapability;
        }

        /// <summary>
        ///     Applies several components in order.
        ///     按顺序应用多个组件。
        /// </summary>
        public IReadOnlyList<IModelCapability?> ApplyRange(
            IEnumerable<IModelCapability> components,
            ApplyModelCapabilityOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(components);

            return components.Select(component => Apply(component, options)).ToList();
        }

        /// <summary>
        ///     Adds a component without subtractive merge behavior.
        ///     添加组件，不使用减法合并行为。
        /// </summary>
        public IModelCapability? Add(IModelCapability component, bool allowMerge = true, bool isUpgrade = false)
        {
            return Apply(component, new(allowMerge, false, isUpgrade));
        }

        /// <summary>
        ///     Adds a component as part of an owner upgrade.
        ///     作为 owner 升级的一部分添加组件。
        /// </summary>
        public IModelCapability? AddForUpgrade(IModelCapability component, bool allowMerge = true)
        {
            return Apply(component, ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Adds a component and returns the typed result.
        ///     添加组件并返回类型化结果。
        /// </summary>
        public TCapability? Add<TCapability>(TCapability component, bool allowMerge = true, bool isUpgrade = false)
            where TCapability : class, IModelCapability
        {
            return Add((IModelCapability)component, allowMerge, isUpgrade) as TCapability;
        }

        /// <summary>
        ///     Adds a component as part of an owner upgrade and returns the typed result.
        ///     作为 owner 升级的一部分添加组件并返回类型化结果。
        /// </summary>
        public TCapability? AddForUpgrade<TCapability>(TCapability component, bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return AddForUpgrade((IModelCapability)component, allowMerge) as TCapability;
        }

        /// <summary>
        ///     Creates a registered capability and applies it as part of an owner upgrade.
        ///     创建已注册组件，并作为 owner 升级的一部分应用。
        /// </summary>
        public TCapability? AddUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            var capabilityId = ModelCapabilityRegistry.GetCapabilityId<TCapability>();
            if (capabilityId == null)
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}");

            return Apply(ModelCapabilityRegistry.Create(capabilityId),
                ApplyModelCapabilityOptions.Upgrade(allowMerge)) as TCapability;
        }

        /// <summary>
        ///     Subtracts a component through merge handlers.
        ///     通过合并处理器减去组件。
        /// </summary>
        public IModelCapability? Subtract(IModelCapability component, bool isUpgrade = false)
        {
            return Apply(component, new(true, true, isUpgrade));
        }

        /// <summary>
        ///     Removes the first component of type <typeparamref name="TCapability" />.
        ///     移除第一个 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public TCapability? Remove<TCapability>() where TCapability : class, IModelCapability
        {
            var index = _components.FindIndex(static c => c is TCapability);
            if (index < 0)
                return null;

            var removed = (TCapability)_components[index];
            removed.Detach();
            _components.RemoveAt(index);
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes the first component with <paramref name="capabilityId" />.
        ///     移除第一个组件 ID 为 <paramref name="capabilityId" /> 的组件。
        /// </summary>
        public IModelCapability? Remove(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            var index = _components.FindIndex(component =>
                string.Equals(component.CapabilityId, capabilityId, StringComparison.Ordinal));
            if (index < 0)
                return null;

            var removed = _components[index];
            removed.Detach();
            _components.RemoveAt(index);
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes this exact component instance.
        ///     移除此组件实例。
        /// </summary>
        public bool Remove(IModelCapability component)
        {
            ArgumentNullException.ThrowIfNull(component);
            var index = _components.FindIndex(c => ReferenceEquals(c, component));
            if (index < 0)
                return false;

            _components[index].Detach();
            _defaultCapabilities.Remove(_components[index]);
            _components.RemoveAt(index);
            MarkDirty();
            return true;
        }

        /// <summary>
        ///     Removes all components of type <typeparamref name="TCapability" />.
        ///     移除所有 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public IReadOnlyList<TCapability> RemoveAll<TCapability>() where TCapability : class, IModelCapability
        {
            List<TCapability> removed = [];
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                if (_components[i] is not TCapability component)
                    continue;

                component.Detach();
                _components.RemoveAt(i);
                _defaultCapabilities.Remove(component);
                removed.Add(component);
            }

            if (removed.Count == 0)
                return [];

            removed.Reverse();
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes all components with <paramref name="capabilityId" />.
        ///     移除所有组件 ID 为 <paramref name="capabilityId" /> 的组件。
        /// </summary>
        public IReadOnlyList<IModelCapability> RemoveAll(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            List<IModelCapability> removed = [];
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];
                if (!string.Equals(component.CapabilityId, capabilityId, StringComparison.Ordinal))
                    continue;

                component.Detach();
                _components.RemoveAt(i);
                _defaultCapabilities.Remove(component);
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
        public void Clear(UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            if (_components.Count == 0 &&
                (unknownPolicy == UnknownModelCapabilityPolicy.Preserve || _unknownEntries.Count == 0))
                return;

            foreach (var component in _components)
                component.Detach();

            _components.Clear();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            MarkDirty();
        }

        /// <summary>
        ///     Replaces all known components with <paramref name="components" />.
        ///     使用 <paramref name="components" /> 替换所有已知组件。
        /// </summary>
        public void ReplaceAll(
            IEnumerable<IModelCapability> components,
            UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            ArgumentNullException.ThrowIfNull(components);

            foreach (var component in _components)
                component.Detach();

            _components.Clear();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            foreach (var component in components)
            {
                _components.Add(component);
                component.Attach(Owner);
            }

            MarkDirty();
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
        ///     Attempts to get the first component of type <typeparamref name="TCapability" />.
        ///     尝试获取第一个 <typeparamref name="TCapability" /> 类型组件。
        /// </summary>
        public bool TryGet<TCapability>(out TCapability component) where TCapability : class, IModelCapability
        {
            component = Get<TCapability>()!;
            return component != null;
        }

        /// <summary>
        ///     Gets the first component with <paramref name="capabilityId" />.
        ///     获取第一个组件 ID 为 <paramref name="capabilityId" /> 的组件。
        /// </summary>
        public IModelCapability? Get(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return _components.FirstOrDefault(component =>
                string.Equals(component.CapabilityId, capabilityId, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Returns true when at least one component of type <typeparamref name="TCapability" /> is attached.
        ///     当至少附加了一个 <typeparamref name="TCapability" /> 类型组件时返回 true。
        /// </summary>
        public bool Contains<TCapability>() where TCapability : class, IModelCapability
        {
            return _components.Any(static c => c is TCapability);
        }

        /// <summary>
        ///     Returns true when at least one component with <paramref name="capabilityId" /> is attached.
        ///     当至少附加了一个组件 ID 为 <paramref name="capabilityId" /> 的组件时返回 true。
        /// </summary>
        public bool Contains(string capabilityId)
        {
            return Get(capabilityId) != null;
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
        ///     Gets all components with <paramref name="capabilityId" />.
        ///     获取所有组件 ID 为 <paramref name="capabilityId" /> 的组件。
        /// </summary>
        public IReadOnlyList<IModelCapability> GetAll(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return _components
                .Where(component => string.Equals(component.CapabilityId, capabilityId, StringComparison.Ordinal))
                .ToArray();
        }

        /// <summary>
        ///     Gets an existing component of type <typeparamref name="TCapability" />, or applies a new component
        ///     created by <paramref name="factory" />.
        ///     获取已有 <typeparamref name="TCapability" /> 组件；不存在时应用由 <paramref name="factory" /> 创建的新组件。
        /// </summary>
        public TCapability GetOrAdd<TCapability>(
            Func<TCapability> factory,
            ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            ArgumentNullException.ThrowIfNull(factory);

            var existing = Get<TCapability>();
            if (existing != null)
                return existing;

            var component = Apply(factory(), options);
            return component ?? throw new InvalidOperationException(
                $"Applying component '{typeof(TCapability).FullName}' did not produce a component of that type.");
        }

        /// <summary>
        ///     Gets an existing component of type <typeparamref name="TCapability" />, or creates one from
        ///     <see cref="ModelCapabilityRegistry" />.
        ///     获取已有 <typeparamref name="TCapability" /> 组件；不存在时通过 <see cref="ModelCapabilityRegistry" /> 创建。
        /// </summary>
        public TCapability GetOrCreate<TCapability>(ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            var existing = Get<TCapability>();
            if (existing != null)
                return existing;

            var capabilityId = ModelCapabilityRegistry.GetCapabilityId<TCapability>();
            if (capabilityId == null)
                throw new InvalidOperationException(
                    $"Model capability type is not registered: {typeof(TCapability).FullName}");

            var component = Apply(ModelCapabilityRegistry.Create(capabilityId), options) as TCapability;
            return component ?? throw new InvalidOperationException(
                $"Registered capability '{capabilityId}' is not a '{typeof(TCapability).FullName}'.");
        }

        /// <summary>
        ///     Gets an existing registered capability, or creates it as part of an owner upgrade.
        ///     获取已有已注册组件；不存在时作为 owner 升级的一部分创建。
        /// </summary>
        public TCapability GetOrCreateUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return GetOrCreate<TCapability>(ApplyModelCapabilityOptions.Upgrade(allowMerge));
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
            ModelCapabilities.MarkSavedDataDirty(Owner);
        }

        internal bool ShouldSave()
        {
            return IsDirty ||
                   _components.Count > 0 ||
                   _unknownEntries.Count > 0 ||
                   _components.Any(ComponentHasSavedState);
        }

        internal void Load(ModelCapabilitySaveDocument? document)
        {
            foreach (var component in _components)
                component.Detach(true);

            _components.Clear();
            _defaultCapabilities.Clear();
            _unknownEntries.Clear();
            IsDirty = false;

            var defaultCapabilities = CreateDefaultCapabilities();
            if (document == null)
            {
                AddMissingDefaultCapabilities(defaultCapabilities);
                return;
            }

            Load(document, defaultCapabilities);
        }

        private void Load(ModelCapabilitySaveDocument document, DefaultCapabilityLoadState defaultItems)
        {
            foreach (var entry in document.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                if (defaultItems.TryTake(entry.Id, out var defaultCapability))
                {
                    LoadComponentState(defaultCapability, entry);
                    AddDefaultCapability(defaultCapability);
                    continue;
                }

                if (!ModelCapabilityRegistry.TryCreate(entry.Id, out var component))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                LoadComponentState(component, entry);
                _components.Add(component);
                component.Attach(Owner, true);
            }
        }

        internal ModelCapabilitySaveDocument? Save()
        {
            if (_components.Count == 0 && _unknownEntries.Count == 0 && !IsDirty)
                return null;

            var document = new ModelCapabilitySaveDocument();
            document.Capabilities.AddRange(_unknownEntries.Select(CloneEntry));

            foreach (var component in _components)
            {
                var state = component as IModelCapabilityJsonState;
                document.Capabilities.Add(new()
                {
                    Id = component.CapabilityId,
                    Schema = state?.SchemaVersion ?? 1,
                    Data = state?.SaveState()?.DeepClone(),
                });
            }

            return document;
        }

        internal void CopyTo(ModelCapabilitySet target)
        {
            foreach (var component in target._components)
                component.Detach(true);

            target._components.Clear();
            target._defaultCapabilities.Clear();
            target._unknownEntries.Clear();
            target._unknownEntries.AddRange(_unknownEntries.Select(CloneEntry));
            target.IsDirty = false;

            foreach (var component in _components)
            {
                var cloned = component is IModelCapabilityCloneHandler cloneHandler
                    ? cloneHandler.CloneFor(target.Owner)
                    : CloneThroughSave(component, target.Owner);

                target._components.Add(cloned);
                if (_defaultCapabilities.Contains(component))
                    target._defaultCapabilities.Add(cloned);

                if (!ReferenceEquals(cloned.Owner, target.Owner))
                    cloned.Attach(target.Owner, true);

                if (cloned is IModelCapabilityCloneNotification notification)
                    notification.AfterOwnerCloned(Owner, target.Owner, component);
            }

            if (IsDirty || _unknownEntries.Count > 0 ||
                _components.Any(component => !_defaultCapabilities.Contains(component)))
                target.MarkDirty();
        }

        private DefaultCapabilityLoadState CreateDefaultCapabilities()
        {
            var state = new DefaultCapabilityLoadState();
            foreach (var component in ModelCapabilityDefaults.Create(Owner))
                state.Add(component);

            return state;
        }

        private void AddDefaultCapability(IModelCapability component)
        {
            _components.Add(component);
            _defaultCapabilities.Add(component);
            component.Attach(Owner, true);
        }

        private void AddMissingDefaultCapabilities(DefaultCapabilityLoadState defaultItems)
        {
            foreach (var component in defaultItems.TakeRemaining())
                AddDefaultCapability(component);
        }

        private static void LoadComponentState(IModelCapability component, ModelCapabilitySaveEntry entry)
        {
            if (component is IModelCapabilityJsonState state)
                state.LoadState(entry.Data?.DeepClone(), entry.Schema);
        }

        internal void MarkDirtyFromHost()
        {
            IsDirty = true;
        }

        private static bool ComponentHasSavedState(IModelCapability component)
        {
            return component is IModelCapabilityJsonState state && state.SaveState() != null;
        }

        private static void MarkDynamicVarsJustUpgraded(
            IModelCapability component,
            ApplyModelCapabilityOptions options)
        {
            if (options.IsUpgrade && component is ModelCapability modelComponent)
                modelComponent.MarkDynamicVarsJustUpgraded();
        }

        private static IModelCapability CloneThroughSave(IModelCapability component, AbstractModel clonedOwner)
        {
            if (!ModelCapabilityRegistry.TryCreate(component.CapabilityId, out var clone))
                throw new InvalidOperationException($"Cannot clone unknown model capability '{component.CapabilityId}'.");

            if (component is IModelCapabilityJsonState sourceState && clone is IModelCapabilityJsonState targetState)
                targetState.LoadState(sourceState.SaveState()?.DeepClone(), sourceState.SchemaVersion);

            clone.Attach(clonedOwner, true);
            return clone;
        }

        private static ModelCapabilitySaveEntry CloneEntry(ModelCapabilitySaveEntry entry)
        {
            return new()
            {
                Id = entry.Id,
                Schema = entry.Schema,
                Data = entry.Data?.DeepClone(),
            };
        }

        private sealed class DefaultCapabilityLoadState
        {
            private readonly Dictionary<string, Queue<IModelCapability>> _queues = new(StringComparer.Ordinal);
            private readonly List<IModelCapability> _remaining = [];

            public void Add(IModelCapability component)
            {
                _remaining.Add(component);
                if (!_queues.TryGetValue(component.CapabilityId, out var queue))
                {
                    queue = new();
                    _queues[component.CapabilityId] = queue;
                }

                queue.Enqueue(component);
            }

            public bool TryTake(string capabilityId, out IModelCapability component)
            {
                component = null!;
                if (!_queues.TryGetValue(capabilityId, out var queue) || queue.Count == 0)
                    return false;

                component = queue.Dequeue();
                var taken = component;
                var index = _remaining.FindIndex(candidate => ReferenceEquals(candidate, taken));
                if (index >= 0)
                    _remaining.RemoveAt(index);

                return true;
            }

            public IReadOnlyList<IModelCapability> TakeRemaining()
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
