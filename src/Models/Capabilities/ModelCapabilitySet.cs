using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Mutable capability set attached to one model instance.
    ///     附加到单个模型实例上的可变能力集合。
    /// </summary>
    public sealed class ModelCapabilitySet
    {
        private const string LoadSurface = "model-capability-load";
        private readonly List<IModelCapability> _capabilities = [];
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
        ///     All attached capabilities in execution order.
        ///     按执行顺序排列的所有已附加能力。
        /// </summary>
        public IReadOnlyList<IModelCapability> All => _capabilities;

        /// <summary>
        ///     All attached capabilities in execution order.
        ///     按执行顺序排列的所有已附加能力。
        /// </summary>
        public IReadOnlyList<IModelCapability> Attached => _capabilities;

        internal bool IsDirty { get; private set; }

        /// <summary>
        ///     Number of currently attached capabilities.
        ///     当前附加能力数量。
        /// </summary>
        public int Count => _capabilities.Count;

        /// <summary>
        ///     Applies a capability, optionally merging it with an existing capability.
        ///     应用能力，并可选择与已有能力合并。
        /// </summary>
        public IModelCapability? Apply(IModelCapability incoming, ApplyModelCapabilityOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(incoming);

            if (options.AllowMerge)
                for (var i = 0; i < _capabilities.Count; i++)
                {
                    var existing = _capabilities[i];
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
                        _capabilities.RemoveAt(i);
                        MarkDirty();
                        return null;
                    }

                    _capabilities[i] = merged;
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

            _capabilities.Add(incoming);
            incoming.Attach(Owner);
            MarkDynamicVarsJustUpgraded(incoming, options);
            MarkDirty();
            return incoming;
        }

        /// <summary>
        ///     Applies a capability and returns the typed result.
        ///     应用能力并返回类型化结果。
        /// </summary>
        public TCapability? Apply<TCapability>(TCapability incoming, ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return Apply((IModelCapability)incoming, options) as TCapability;
        }

        /// <summary>
        ///     Applies several capabilities in order.
        ///     按顺序应用多个能力。
        /// </summary>
        public IReadOnlyList<IModelCapability?> ApplyRange(
            IEnumerable<IModelCapability> capabilities,
            ApplyModelCapabilityOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(capabilities);

            return capabilities.Select(capability => Apply(capability, options)).ToList();
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> at <paramref name="index" /> without merge behavior.
        ///     在 <paramref name="index" /> 插入 <paramref name="capability" />，不执行合并。
        /// </summary>
        public IModelCapability Insert(int index, IModelCapability capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            if (index < 0 || index > _capabilities.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the set bounds.");

            _capabilities.Insert(index, capability);
            capability.Attach(Owner);
            MarkDirty();
            return capability;
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> at <paramref name="index" /> without merge behavior and returns
        ///     the typed capability.
        ///     在 <paramref name="index" /> 插入 <paramref name="capability" />，不执行合并，并返回类型化能力。
        /// </summary>
        public TCapability Insert<TCapability>(int index, TCapability capability)
            where TCapability : class, IModelCapability
        {
            return (TCapability)Insert(index, (IModelCapability)capability);
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> before the first attached <typeparamref name="TExisting" />.
        ///     将 <paramref name="capability" /> 插入到第一个已附加 <typeparamref name="TExisting" /> 之前。
        /// </summary>
        public IModelCapability? InsertBefore<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertRelativeTo<TExisting>(capability, false, missingAnchorPolicy);
        }

        /// <summary>
        ///     Inserts <paramref name="capability" /> after the first attached <typeparamref name="TExisting" />.
        ///     将 <paramref name="capability" /> 插入到第一个已附加 <typeparamref name="TExisting" /> 之后。
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
        ///     Adds a capability without subtractive merge behavior.
        ///     添加能力，不使用减法合并行为。
        /// </summary>
        public IModelCapability? Add(IModelCapability capability, bool allowMerge = true, bool isUpgrade = false)
        {
            return Apply(capability, new(allowMerge, false, isUpgrade));
        }

        /// <summary>
        ///     Adds a capability as part of an owner upgrade.
        ///     作为 owner 升级的一部分添加能力。
        /// </summary>
        public IModelCapability? AddForUpgrade(IModelCapability capability, bool allowMerge = true)
        {
            return Apply(capability, ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Adds a capability and returns the typed result.
        ///     添加能力并返回类型化结果。
        /// </summary>
        public TCapability? Add<TCapability>(TCapability capability, bool allowMerge = true, bool isUpgrade = false)
            where TCapability : class, IModelCapability
        {
            return Add((IModelCapability)capability, allowMerge, isUpgrade) as TCapability;
        }

        /// <summary>
        ///     Adds a capability as part of an owner upgrade and returns the typed result.
        ///     作为 owner 升级的一部分添加能力并返回类型化结果。
        /// </summary>
        public TCapability? AddForUpgrade<TCapability>(TCapability capability, bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return AddForUpgrade((IModelCapability)capability, allowMerge) as TCapability;
        }

        /// <summary>
        ///     Creates a registered capability and applies it as part of an owner upgrade.
        ///     创建已注册能力，并作为 owner 升级的一部分应用。
        /// </summary>
        public TCapability? AddUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return Apply(
                ModelCapabilityRegistry.Create<TCapability>(),
                ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Subtracts a capability through merge handlers.
        ///     通过合并处理器减去能力。
        /// </summary>
        public IModelCapability? Subtract(IModelCapability capability, bool isUpgrade = false)
        {
            return Apply(capability, new(true, true, isUpgrade));
        }

        /// <summary>
        ///     Removes the first capability of type <typeparamref name="TCapability" />.
        ///     移除第一个 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public TCapability? Remove<TCapability>() where TCapability : class, IModelCapability
        {
            var index = _capabilities.FindIndex(static c => c is TCapability);
            if (index < 0)
                return null;

            var removed = (TCapability)_capabilities[index];
            removed.Detach();
            _capabilities.RemoveAt(index);
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes the first capability with <paramref name="capabilityId" />.
        ///     移除第一个能力 ID 为 <paramref name="capabilityId" /> 的能力。
        /// </summary>
        public IModelCapability? Remove(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            var index = _capabilities.FindIndex(capability =>
                string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal));
            if (index < 0)
                return null;

            var removed = _capabilities[index];
            removed.Detach();
            _capabilities.RemoveAt(index);
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes this exact capability instance.
        ///     移除此能力实例。
        /// </summary>
        public bool Remove(IModelCapability capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            var index = _capabilities.FindIndex(c => ReferenceEquals(c, capability));
            if (index < 0)
                return false;

            _capabilities[index].Detach();
            _defaultCapabilities.Remove(_capabilities[index]);
            _capabilities.RemoveAt(index);
            MarkDirty();
            return true;
        }

        /// <summary>
        ///     Removes all capabilities of type <typeparamref name="TCapability" />.
        ///     移除所有 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public IReadOnlyList<TCapability> RemoveAll<TCapability>() where TCapability : class, IModelCapability
        {
            List<TCapability> removed = [];
            for (var i = _capabilities.Count - 1; i >= 0; i--)
            {
                if (_capabilities[i] is not TCapability capability)
                    continue;

                capability.Detach();
                _capabilities.RemoveAt(i);
                _defaultCapabilities.Remove(capability);
                removed.Add(capability);
            }

            if (removed.Count == 0)
                return [];

            removed.Reverse();
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Removes all capabilities with <paramref name="capabilityId" />.
        ///     移除所有能力 ID 为 <paramref name="capabilityId" /> 的能力。
        /// </summary>
        public IReadOnlyList<IModelCapability> RemoveAll(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            List<IModelCapability> removed = [];
            for (var i = _capabilities.Count - 1; i >= 0; i--)
            {
                var capability = _capabilities[i];
                if (!string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal))
                    continue;

                capability.Detach();
                _capabilities.RemoveAt(i);
                _defaultCapabilities.Remove(capability);
                removed.Add(capability);
            }

            if (removed.Count == 0)
                return [];

            removed.Reverse();
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     Clears known capabilities, optionally clearing unknown saved entries as well.
        ///     清空已知能力，并可选择同时清空未知保存条目。
        /// </summary>
        public void Clear(UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            if (_capabilities.Count == 0 &&
                (unknownPolicy == UnknownModelCapabilityPolicy.Preserve || _unknownEntries.Count == 0))
                return;

            foreach (var capability in _capabilities)
                capability.Detach();

            _capabilities.Clear();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            MarkDirty();
        }

        /// <summary>
        ///     Replaces all known capabilities with <paramref name="capabilities" />.
        ///     使用 <paramref name="capabilities" /> 替换所有已知能力。
        /// </summary>
        public void ReplaceAll(
            IEnumerable<IModelCapability> capabilities,
            UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            ArgumentNullException.ThrowIfNull(capabilities);

            foreach (var capability in _capabilities)
                capability.Detach();

            _capabilities.Clear();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            foreach (var capability in capabilities)
            {
                _capabilities.Add(capability);
                capability.Attach(Owner);
            }

            MarkDirty();
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
        ///     Attempts to get the first capability of type <typeparamref name="TCapability" />.
        ///     尝试获取第一个 <typeparamref name="TCapability" /> 类型能力。
        /// </summary>
        public bool TryGet<TCapability>(out TCapability capability) where TCapability : class, IModelCapability
        {
            capability = Get<TCapability>()!;
            return capability != null;
        }

        /// <summary>
        ///     Gets the first capability with <paramref name="capabilityId" />.
        ///     获取第一个能力 ID 为 <paramref name="capabilityId" /> 的能力。
        /// </summary>
        public IModelCapability? Get(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return _capabilities.FirstOrDefault(capability =>
                string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Returns true when at least one capability of type <typeparamref name="TCapability" /> is attached.
        ///     当至少附加了一个 <typeparamref name="TCapability" /> 类型能力时返回 true。
        /// </summary>
        public bool Contains<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.Any(static c => c is TCapability);
        }

        /// <summary>
        ///     Returns true when at least one capability with <paramref name="capabilityId" /> is attached.
        ///     当至少附加了一个能力 ID 为 <paramref name="capabilityId" /> 的能力时返回 true。
        /// </summary>
        public bool Contains(string capabilityId)
        {
            return Get(capabilityId) != null;
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
        ///     Gets all capabilities with <paramref name="capabilityId" />.
        ///     获取所有能力 ID 为 <paramref name="capabilityId" /> 的能力。
        /// </summary>
        public IReadOnlyList<IModelCapability> GetAll(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return _capabilities
                .Where(capability => string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal))
                .ToArray();
        }

        /// <summary>
        ///     Gets an existing capability of type <typeparamref name="TCapability" />, or applies a new capability
        ///     created by <paramref name="factory" />.
        ///     获取已有 <typeparamref name="TCapability" /> 能力；不存在时应用由 <paramref name="factory" /> 创建的新能力。
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

            var capability = Apply(factory(), options);
            return capability ?? throw new InvalidOperationException(
                $"Applying capability '{typeof(TCapability).FullName}' did not produce a capability of that type.");
        }

        /// <summary>
        ///     Gets an existing capability of type <typeparamref name="TCapability" />, or creates one from
        ///     <see cref="ModelCapabilityRegistry" />.
        ///     获取已有 <typeparamref name="TCapability" /> 能力；不存在时通过 <see cref="ModelCapabilityRegistry" /> 创建。
        /// </summary>
        public TCapability GetOrCreate<TCapability>(ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            var existing = Get<TCapability>();
            if (existing != null)
                return existing;

            var created = ModelCapabilityRegistry.Create<TCapability>();
            var capability = Apply(created, options);
            return capability ?? throw new InvalidOperationException(
                $"Applying capability '{created.CapabilityId}' did not produce a '{typeof(TCapability).FullName}'.");
        }

        /// <summary>
        ///     Gets an existing registered capability, or creates it as part of an owner upgrade.
        ///     获取已有已注册能力；不存在时作为 owner 升级的一部分创建。
        /// </summary>
        public TCapability GetOrCreateUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return GetOrCreate<TCapability>(ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     Enumerates capabilities that implement a capability interface.
        ///     枚举实现某个能力接口的能力。
        /// </summary>
        public IEnumerable<TCapability> Capabilities<TCapability>() where TCapability : class
        {
            return _capabilities.OfType<TCapability>();
        }

        /// <summary>
        ///     Marks the collection dirty after a capability mutates itself in place.
        ///     能力原地修改自身后，将 collection 标记为已变更。
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
            ModelCapabilities.MarkSavedDataDirty(Owner);
        }

        internal bool ShouldSave()
        {
            return IsDirty ||
                   _capabilities.Count > 0 ||
                   _unknownEntries.Count > 0 ||
                   _capabilities.Any(CapabilityHasSavedState);
        }

        internal void Load(ModelCapabilitySaveDocument? document)
        {
            foreach (var capability in _capabilities)
                capability.Detach(true);

            _capabilities.Clear();
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
                    LoadCapabilityState(defaultCapability, entry);
                    AddDefaultCapability(defaultCapability);
                    NotifyCapabilityLoadedFromSave(defaultCapability);
                    continue;
                }

                if (!ModelCapabilityRegistry.TryCreate(entry.Id, out var capability))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                LoadCapabilityState(capability, entry);
                _capabilities.Add(capability);
                capability.Attach(Owner, true);
                NotifyCapabilityLoadedFromSave(capability);
            }
        }

        internal ModelCapabilitySaveDocument? Save()
        {
            if (_capabilities.Count == 0 && _unknownEntries.Count == 0 && !IsDirty)
                return null;

            var document = new ModelCapabilitySaveDocument();
            document.Capabilities.AddRange(_unknownEntries.Select(CloneEntry));

            foreach (var capability in _capabilities)
            {
                var state = capability as IModelCapabilityJsonState;
                document.Capabilities.Add(new()
                {
                    Id = capability.CapabilityId,
                    Schema = state?.SchemaVersion ?? 1,
                    Data = state?.SaveState()?.DeepClone(),
                });
            }

            return document;
        }

        internal void CopyTo(ModelCapabilitySet target)
        {
            foreach (var capability in target._capabilities)
                capability.Detach(true);

            target._capabilities.Clear();
            target._defaultCapabilities.Clear();
            target._unknownEntries.Clear();
            target._unknownEntries.AddRange(_unknownEntries.Select(CloneEntry));
            target.IsDirty = false;

            foreach (var capability in _capabilities)
            {
                var cloned = capability is IModelCapabilityCloneHandler cloneHandler
                    ? cloneHandler.CloneFor(target.Owner)
                    : CloneThroughSave(capability, target.Owner);

                target._capabilities.Add(cloned);
                if (_defaultCapabilities.Contains(capability))
                    target._defaultCapabilities.Add(cloned);

                if (!ReferenceEquals(cloned.Owner, target.Owner))
                    cloned.Attach(target.Owner, true);

                if (cloned is IModelCapabilityCloneNotification notification)
                    notification.AfterOwnerCloned(Owner, target.Owner, capability);
            }

            if (IsDirty || _unknownEntries.Count > 0 ||
                _capabilities.Any(capability => !_defaultCapabilities.Contains(capability)))
                target.MarkDirty();
        }

        private DefaultCapabilityLoadState CreateDefaultCapabilities()
        {
            var state = new DefaultCapabilityLoadState();
            foreach (var capability in ModelCapabilityDefaults.Create(Owner))
                state.Add(capability);

            return state;
        }

        private void AddDefaultCapability(IModelCapability capability)
        {
            _capabilities.Add(capability);
            _defaultCapabilities.Add(capability);
            capability.Attach(Owner, true);
        }

        private void AddMissingDefaultCapabilities(DefaultCapabilityLoadState defaultItems)
        {
            foreach (var capability in defaultItems.TakeRemaining())
                AddDefaultCapability(capability);
        }

        private static void LoadCapabilityState(IModelCapability capability, ModelCapabilitySaveEntry entry)
        {
            if (capability is IModelCapabilityJsonState state)
                state.LoadState(entry.Data?.DeepClone(), entry.Schema);
        }

        private void NotifyCapabilityLoadedFromSave(IModelCapability capability)
        {
            if (capability is not ModelCapability modelCapability ||
                !ReferenceEquals(capability.Owner, Owner))
                return;

            try
            {
                modelCapability.NotifyLoadedFromSave();
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(LoadSurface, Owner, capability, ex);
            }
        }

        internal void MarkDirtyFromHost()
        {
            IsDirty = true;
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
                    $"Cannot find capability anchor '{typeof(TExisting).FullName}' on model '{Owner.Id}'."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(missingAnchorPolicy),
                    missingAnchorPolicy,
                    "Unknown missing anchor policy."),
            };
        }

        private static bool CapabilityHasSavedState(IModelCapability capability)
        {
            return capability is IModelCapabilityJsonState state && state.SaveState() != null;
        }

        private static void MarkDynamicVarsJustUpgraded(
            IModelCapability capability,
            ApplyModelCapabilityOptions options)
        {
            if (options.IsUpgrade && capability is ModelCapability modelCapability)
                modelCapability.MarkDynamicVarsJustUpgraded();
        }

        private static IModelCapability CloneThroughSave(IModelCapability capability, AbstractModel clonedOwner)
        {
            if (!ModelCapabilityRegistry.TryCreate(capability.CapabilityId, out var clone))
                throw new InvalidOperationException(
                    $"Cannot clone unknown model capability '{capability.CapabilityId}'.");

            if (capability is IModelCapabilityJsonState sourceState && clone is IModelCapabilityJsonState targetState)
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

            public void Add(IModelCapability capability)
            {
                _remaining.Add(capability);
                if (!_queues.TryGetValue(capability.CapabilityId, out var queue))
                {
                    queue = new();
                    _queues[capability.CapabilityId] = queue;
                }

                queue.Enqueue(capability);
            }

            public bool TryTake(string capabilityId, out IModelCapability capability)
            {
                capability = null!;
                if (!_queues.TryGetValue(capabilityId, out var queue) || queue.Count == 0)
                    return false;

                capability = queue.Dequeue();
                var taken = capability;
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
