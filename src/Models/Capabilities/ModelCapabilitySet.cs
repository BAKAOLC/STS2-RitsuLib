using System.Collections.ObjectModel;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Represents the mutable capability set attached to one model instance.</para>
    ///     <para xml:lang="zh-CN">表示附加到单个模型实例的可变能力集合。</para>
    /// </summary>
    public sealed class ModelCapabilitySet
    {
        private const string LoadSurface = "model-capability-load";
        private readonly List<IModelCapability> _capabilities = [];
        private readonly HashSet<IModelCapability> _defaultCapabilities = new(ReferenceEqualityComparer.Instance);
        private readonly ReadOnlyCollection<IModelCapability> _readOnlyCapabilities;
        private readonly List<ModelCapabilitySaveEntry> _unknownEntries = [];
        private IModelCapability[]? _attachedSnapshot;
        private IModelCapability[]? _ownerHookCandidateSnapshot;

        internal ModelCapabilitySet(AbstractModel owner)
        {
            Owner = owner;
            _readOnlyCapabilities = _capabilities.AsReadOnly();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the owning model.</para>
        ///     <para xml:lang="zh-CN">获取所属模型。</para>
        /// </summary>
        public AbstractModel Owner { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets all attached capabilities in execution order.</para>
        ///     <para xml:lang="zh-CN">获取按执行顺序排列的所有已附加能力。</para>
        /// </summary>
        public IReadOnlyList<IModelCapability> All => _readOnlyCapabilities;

        /// <summary>
        ///     <para xml:lang="en">Gets all attached capabilities in execution order.</para>
        ///     <para xml:lang="zh-CN">获取按执行顺序排列的所有已附加能力。</para>
        /// </summary>
        public IReadOnlyList<IModelCapability> Attached => _readOnlyCapabilities;

        internal bool IsDirty { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of currently attached capabilities.</para>
        ///     <para xml:lang="zh-CN">获取当前已附加能力的数量。</para>
        /// </summary>
        public int Count => _capabilities.Count;

        internal IModelCapability[] GetAttachedSnapshot()
        {
            return _attachedSnapshot ??= [.. _capabilities];
        }

        internal IModelCapability[] GetOwnerHookCandidateSnapshot()
        {
            return _ownerHookCandidateSnapshot ??=
            [
                .. _capabilities
                    .Where(static capability => capability is IModelCapabilityHookListener and AbstractModel),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a capability, optionally merging it with an existing capability.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用能力，并可选择将其与已有能力合并。
        ///     </para>
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

                    if (merged != null)
                        EnsureCanAttach(merged);

                    var wasDefault = _defaultCapabilities.Remove(existing);
                    var defaultCapabilityId = wasDefault ? existing.CapabilityId : null;
                    existing.Detach();

                    if (merged == null)
                    {
                        _capabilities.RemoveAt(i);
                        InvalidateAttachedSnapshot();
                        MarkDirty();
                        return null;
                    }

                    _capabilities[i] = merged;
                    InvalidateAttachedSnapshot();
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

            EnsureCanAttach(incoming);
            _capabilities.Add(incoming);
            InvalidateAttachedSnapshot();
            incoming.Attach(Owner);
            MarkDynamicVarsJustUpgraded(incoming, options);
            MarkDirty();
            return incoming;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies a capability and returns the typed result.</para>
        ///     <para xml:lang="zh-CN">应用能力并返回类型化结果。</para>
        /// </summary>
        public TCapability? Apply<TCapability>(TCapability incoming, ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return Apply((IModelCapability)incoming, options) as TCapability;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies multiple capabilities in order. If a later operation throws, earlier applications remain.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按顺序应用多个能力。后续操作抛出异常时，先前已应用的能力会保留。
        ///     </para>
        /// </summary>
        public IReadOnlyList<IModelCapability?> ApplyRange(
            IEnumerable<IModelCapability> capabilities,
            ApplyModelCapabilityOptions options = new())
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            var incoming = capabilities.ToArray();
            if (incoming.Any(static capability => capability == null))
                throw new ArgumentException("Capability collection cannot contain null entries.", nameof(capabilities));
            if (incoming.Distinct(ReferenceEqualityComparer.Instance).Count() != incoming.Length)
                throw new ArgumentException(
                    "Capability collection cannot contain the same instance more than once.",
                    nameof(capabilities));

            return [.. incoming.Select(capability => Apply(capability, options))];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts <paramref name="capability" /> at <paramref name="index" /> without merging.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="index" /> 插入 <paramref name="capability" />，不执行合并。
        ///     </para>
        /// </summary>
        public IModelCapability Insert(int index, IModelCapability capability)
        {
            ArgumentNullException.ThrowIfNull(capability);
            EnsureCanAttach(capability);
            if (index < 0 || index > _capabilities.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the set bounds.");

            _capabilities.Insert(index, capability);
            InvalidateAttachedSnapshot();
            capability.Attach(Owner);
            MarkDirty();
            return capability;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts <paramref name="capability" /> at <paramref name="index" /> without merging and returns the
        ///         typed capability.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="index" /> 插入 <paramref name="capability" />，不执行合并，并返回类型化能力。
        ///     </para>
        /// </summary>
        public TCapability Insert<TCapability>(int index, TCapability capability)
            where TCapability : class, IModelCapability
        {
            return (TCapability)Insert(index, (IModelCapability)capability);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts <paramref name="capability" /> before the first attached <typeparamref name="TExisting" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="capability" /> 插入到第一个已附加的 <typeparamref name="TExisting" /> 之前。
        ///     </para>
        /// </summary>
        public IModelCapability? InsertBefore<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertRelativeTo<TExisting>(capability, false, missingAnchorPolicy);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts <paramref name="capability" /> after the first attached <typeparamref name="TExisting" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="capability" /> 插入到第一个已附加的 <typeparamref name="TExisting" /> 之后。
        ///     </para>
        /// </summary>
        public IModelCapability? InsertAfter<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertRelativeTo<TExisting>(capability, true, missingAnchorPolicy);
        }

        /// <summary>
        ///     <para xml:lang="en">Shorthand for <see cref="InsertBefore{TExisting}" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="InsertBefore{TExisting}" /> 的简写。</para>
        /// </summary>
        public IModelCapability? Before<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertBefore<TExisting>(capability, missingAnchorPolicy);
        }

        /// <summary>
        ///     <para xml:lang="en">Shorthand for <see cref="InsertAfter{TExisting}" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="InsertAfter{TExisting}" /> 的简写。</para>
        /// </summary>
        public IModelCapability? After<TExisting>(
            IModelCapability capability,
            MissingModelCapabilityAnchorPolicy missingAnchorPolicy = MissingModelCapabilityAnchorPolicy.Append)
            where TExisting : class, IModelCapability
        {
            return InsertAfter<TExisting>(capability, missingAnchorPolicy);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a capability without subtractive merging.</para>
        ///     <para xml:lang="zh-CN">添加能力，不执行减法合并。</para>
        /// </summary>
        public IModelCapability? Add(IModelCapability capability, bool allowMerge = true, bool isUpgrade = false)
        {
            return Apply(capability, new(allowMerge, false, isUpgrade));
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a capability as part of an owner upgrade.</para>
        ///     <para xml:lang="zh-CN">添加能力，并将其视为所属模型升级的一部分。</para>
        /// </summary>
        public IModelCapability? AddForUpgrade(IModelCapability capability, bool allowMerge = true)
        {
            return Apply(capability, ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a capability and returns the typed result.</para>
        ///     <para xml:lang="zh-CN">添加能力并返回类型化结果。</para>
        /// </summary>
        public TCapability? Add<TCapability>(TCapability capability, bool allowMerge = true, bool isUpgrade = false)
            where TCapability : class, IModelCapability
        {
            return Add((IModelCapability)capability, allowMerge, isUpgrade) as TCapability;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a capability as part of an owner upgrade and returns the typed result.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加能力并将其视为所属模型升级的一部分，然后返回类型化结果。
        ///     </para>
        /// </summary>
        public TCapability? AddForUpgrade<TCapability>(TCapability capability, bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return AddForUpgrade((IModelCapability)capability, allowMerge) as TCapability;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a registered capability and applies it as part of an owner upgrade.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建已注册能力，并将其作为所属模型升级的一部分应用。
        ///     </para>
        /// </summary>
        public TCapability? AddUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return Apply(
                ModelCapabilityRegistry.Create<TCapability>(),
                ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     <para xml:lang="en">Subtracts a capability through merge handlers.</para>
        ///     <para xml:lang="zh-CN">通过合并处理程序减去能力。</para>
        /// </summary>
        public IModelCapability? Subtract(IModelCapability capability, bool isUpgrade = false)
        {
            return Apply(capability, new(true, true, isUpgrade));
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the first capability of type <typeparamref name="TCapability" />.</para>
        ///     <para xml:lang="zh-CN">移除第一个 <typeparamref name="TCapability" /> 类型的能力。</para>
        /// </summary>
        public TCapability? Remove<TCapability>() where TCapability : class, IModelCapability
        {
            var index = _capabilities.FindIndex(static c => c is TCapability);
            if (index < 0)
                return null;

            var removed = (TCapability)_capabilities[index];
            removed.Detach();
            _capabilities.RemoveAt(index);
            InvalidateAttachedSnapshot();
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the first capability with <paramref name="capabilityId" />.</para>
        ///     <para xml:lang="zh-CN">移除第一个 ID 为 <paramref name="capabilityId" /> 的能力。</para>
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
            InvalidateAttachedSnapshot();
            _defaultCapabilities.Remove(removed);
            MarkDirty();
            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes this exact capability instance.</para>
        ///     <para xml:lang="zh-CN">移除这一特定能力实例。</para>
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
            InvalidateAttachedSnapshot();
            MarkDirty();
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes all capabilities of type <typeparamref name="TCapability" />. If a detach callback throws,
        ///         capabilities processed earlier remain removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除所有 <typeparamref name="TCapability" /> 类型的能力。分离回调抛出异常时，先前已处理的能力仍会
        ///         保持移除状态。
        ///     </para>
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
                InvalidateAttachedSnapshot();
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
        ///     <para xml:lang="en">
        ///         Removes all capabilities with <paramref name="capabilityId" />. If a detach callback throws,
        ///         capabilities processed earlier remain removed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除所有 ID 为 <paramref name="capabilityId" /> 的能力。分离回调抛出异常时，先前已处理的能力仍会
        ///         保持移除状态。
        ///     </para>
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
                InvalidateAttachedSnapshot();
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
        ///     <para xml:lang="en">
        ///         Clears known capabilities and optionally clears unknown saved entries. If a detach callback throws,
        ///         capabilities detached earlier remain present in the set until a later operation repairs the state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         清除已知能力，并可选择同时清除未知的保存条目。分离回调抛出异常时，先前已完成分离的能力仍会
        ///         留在集合中，直到后续操作修复该状态。
        ///     </para>
        /// </summary>
        public void Clear(UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            if (_capabilities.Count == 0 &&
                (unknownPolicy == UnknownModelCapabilityPolicy.Preserve || _unknownEntries.Count == 0))
                return;

            foreach (var capability in _capabilities)
                capability.Detach();

            _capabilities.Clear();
            InvalidateAttachedSnapshot();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            MarkDirty();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces all known capabilities with <paramref name="capabilities" />. The operation is not
        ///         transactional; a detach or attach callback failure can leave a partially replaced set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="capabilities" /> 替换所有已知能力。此操作不具备事务性；分离或附加回调失败
        ///         时，集合可能处于只完成部分替换的状态。
        ///     </para>
        /// </summary>
        public void ReplaceAll(
            IEnumerable<IModelCapability> capabilities,
            UnknownModelCapabilityPolicy unknownPolicy = UnknownModelCapabilityPolicy.Preserve)
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            var replacements = capabilities.ToArray();
            if (replacements.Any(static capability => capability == null))
                throw new ArgumentException("Capability collection cannot contain null entries.", nameof(capabilities));
            if (replacements.Distinct(ReferenceEqualityComparer.Instance).Count() != replacements.Length)
                throw new ArgumentException(
                    "Capability collection cannot contain the same instance more than once.",
                    nameof(capabilities));
            foreach (var capability in replacements)
                EnsureCanAttach(capability, true);

            foreach (var capability in _capabilities)
                capability.Detach();

            _capabilities.Clear();
            InvalidateAttachedSnapshot();
            _defaultCapabilities.Clear();
            if (unknownPolicy == UnknownModelCapabilityPolicy.Remove)
                _unknownEntries.Clear();

            foreach (var capability in replacements)
            {
                _capabilities.Add(capability);
                InvalidateAttachedSnapshot();
                capability.Attach(Owner);
            }

            MarkDirty();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the first capability of type <typeparamref name="TCapability" />.</para>
        ///     <para xml:lang="zh-CN">获取第一个 <typeparamref name="TCapability" /> 类型的能力。</para>
        /// </summary>
        public TCapability? Get<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.OfType<TCapability>().FirstOrDefault();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to get the first capability of type <typeparamref name="TCapability" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取第一个 <typeparamref name="TCapability" /> 类型的能力。
        ///     </para>
        /// </summary>
        public bool TryGet<TCapability>(out TCapability capability) where TCapability : class, IModelCapability
        {
            capability = Get<TCapability>()!;
            return capability != null;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the first capability with <paramref name="capabilityId" />.</para>
        ///     <para xml:lang="zh-CN">获取第一个 ID 为 <paramref name="capabilityId" /> 的能力。</para>
        /// </summary>
        public IModelCapability? Get(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return _capabilities.FirstOrDefault(capability =>
                string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when at least one capability of type
        ///         <typeparamref name="TCapability" /> is attached.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         至少附加一个 <typeparamref name="TCapability" /> 类型的能力时返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool Contains<TCapability>() where TCapability : class, IModelCapability
        {
            return _capabilities.Any(static c => c is TCapability);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when at least one capability with
        ///         <paramref name="capabilityId" /> is attached.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         至少附加一个 ID 为 <paramref name="capabilityId" /> 的能力时返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool Contains(string capabilityId)
        {
            return Get(capabilityId) != null;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets all capabilities of type <typeparamref name="TCapability" />.</para>
        ///     <para xml:lang="zh-CN">获取所有 <typeparamref name="TCapability" /> 类型的能力。</para>
        /// </summary>
        public IReadOnlyList<TCapability> GetAll<TCapability>() where TCapability : class, IModelCapability
        {
            return [.. _capabilities.OfType<TCapability>()];
        }

        /// <summary>
        ///     <para xml:lang="en">Gets all capabilities with <paramref name="capabilityId" />.</para>
        ///     <para xml:lang="zh-CN">获取所有 ID 为 <paramref name="capabilityId" /> 的能力。</para>
        /// </summary>
        public IReadOnlyList<IModelCapability> GetAll(string capabilityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

            return
            [
                .. _capabilities
                    .Where(capability =>
                        string.Equals(capability.CapabilityId, capabilityId, StringComparison.Ordinal)),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an existing capability of type <typeparamref name="TCapability" />, or applies a new capability
        ///         created by <paramref name="factory" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已有的 <typeparamref name="TCapability" /> 能力；不存在时，应用由
        ///         <paramref name="factory" /> 创建的新能力。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets an existing capability of type <typeparamref name="TCapability" />, or creates and applies one
        ///         through <see cref="ModelCapabilityRegistry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已有的 <typeparamref name="TCapability" /> 能力；不存在时，通过
        ///         <see cref="ModelCapabilityRegistry" /> 创建并应用一个新能力。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets an existing registered capability, or creates it as part of an owner upgrade.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已有的已注册能力；不存在时，将新能力作为所属模型升级的一部分创建。
        ///     </para>
        /// </summary>
        public TCapability GetOrCreateUpgrade<TCapability>(bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return GetOrCreate<TCapability>(ApplyModelCapabilityOptions.Upgrade(allowMerge));
        }

        /// <summary>
        ///     <para xml:lang="en">Enumerates capabilities assignable to <typeparamref name="TCapability" />.</para>
        ///     <para xml:lang="zh-CN">枚举可赋值给 <typeparamref name="TCapability" /> 的能力。</para>
        /// </summary>
        public IEnumerable<TCapability> Capabilities<TCapability>() where TCapability : class
        {
            return _capabilities.OfType<TCapability>();
        }

        /// <summary>
        ///     <para xml:lang="en">Marks the capability set dirty after an in-place capability mutation.</para>
        ///     <para xml:lang="zh-CN">能力发生原地修改后，将能力集合标记为脏。</para>
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
            LoadDocument(document, CreateDefaultCapabilityLoadState());
        }

        internal void LoadAfterCardUpgradeReplay(ModelCapabilitySaveDocument? document)
        {
            if (document == null)
                return;

            LoadDocument(document, CreateCurrentCapabilityLoadState());
        }

        private void LoadDocument(ModelCapabilitySaveDocument? document, CapabilityLoadState loadState)
        {
            foreach (var capability in _capabilities)
                capability.Detach(true);

            _capabilities.Clear();
            InvalidateAttachedSnapshot();
            _defaultCapabilities.Clear();
            _unknownEntries.Clear();
            IsDirty = false;

            if (document == null)
            {
                AddMissingCapabilities(loadState);
                return;
            }

            LoadDocumentEntries(document, loadState);
        }

        private void LoadDocumentEntries(ModelCapabilitySaveDocument document, CapabilityLoadState loadState)
        {
            foreach (var entry in document.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                if (loadState.TryTake(entry.Id, out var loadItem))
                {
                    LoadCapabilityState(loadItem.Capability, entry);
                    AddLoadedCapability(loadItem);
                    NotifyCapabilityLoadedFromSave(loadItem.Capability);
                    continue;
                }

                if (!ModelCapabilityRegistry.TryCreate(entry.Id, out var capability))
                {
                    _unknownEntries.Add(CloneEntry(entry));
                    continue;
                }

                LoadCapabilityState(capability, entry);
                _capabilities.Add(capability);
                InvalidateAttachedSnapshot();
                capability.Attach(Owner, true);
                NotifyCapabilityLoadedFromSave(capability);
            }

            AddMissingCapabilities(loadState);
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
            target.InvalidateAttachedSnapshot();
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
                target.InvalidateAttachedSnapshot();
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

        private CapabilityLoadState CreateDefaultCapabilityLoadState()
        {
            var state = new CapabilityLoadState();
            foreach (var capability in ModelCapabilityDefaults.Create(Owner))
                state.Add(capability, true);

            return state;
        }

        private CapabilityLoadState CreateCurrentCapabilityLoadState()
        {
            var state = new CapabilityLoadState();
            foreach (var capability in _capabilities)
                state.Add(capability, _defaultCapabilities.Contains(capability));

            return state;
        }

        private void AddLoadedCapability(CapabilityLoadItem item)
        {
            _capabilities.Add(item.Capability);
            InvalidateAttachedSnapshot();
            if (item.IsDefault)
                _defaultCapabilities.Add(item.Capability);

            item.Capability.Attach(Owner, true);
        }

        private void AddMissingCapabilities(CapabilityLoadState loadState)
        {
            foreach (var item in loadState.TakeRemaining())
                AddLoadedCapability(item);
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

        private void InvalidateAttachedSnapshot()
        {
            _attachedSnapshot = null;
            _ownerHookCandidateSnapshot = null;
            ModelCapabilityHookListeners.InvalidateOwnerHookCapabilityCache(Owner);
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

        private void EnsureCanAttach(IModelCapability capability, bool allowCurrentAttachment = false)
        {
            if (capability.Owner == null)
                return;
            if (allowCurrentAttachment &&
                ReferenceEquals(capability.Owner, Owner) &&
                _capabilities.Any(existing => ReferenceEquals(existing, capability)))
                return;

            throw new InvalidOperationException(
                $"Capability '{capability.CapabilityId}' is already attached to model '{capability.Owner.Id}'.");
        }

        private readonly record struct CapabilityLoadItem(IModelCapability Capability, bool IsDefault);

        private sealed class CapabilityLoadState
        {
            private readonly Dictionary<string, Queue<CapabilityLoadItem>> _queues = new(StringComparer.Ordinal);
            private readonly List<CapabilityLoadItem> _remaining = [];

            public void Add(IModelCapability capability, bool isDefault)
            {
                var item = new CapabilityLoadItem(capability, isDefault);
                _remaining.Add(item);
                if (!_queues.TryGetValue(capability.CapabilityId, out var queue))
                {
                    queue = new();
                    _queues[capability.CapabilityId] = queue;
                }

                queue.Enqueue(item);
            }

            public bool TryTake(string capabilityId, out CapabilityLoadItem item)
            {
                item = default;
                if (!_queues.TryGetValue(capabilityId, out var queue) || queue.Count == 0)
                    return false;

                item = queue.Dequeue();
                var capability = item.Capability;
                var index = _remaining.FindIndex(candidate => ReferenceEquals(candidate.Capability, capability));
                if (index >= 0)
                    _remaining.RemoveAt(index);

                return true;
            }

            public IReadOnlyList<CapabilityLoadItem> TakeRemaining()
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
