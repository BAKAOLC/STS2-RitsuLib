using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Opt-in contract for model-backed capabilities that should receive the owner's vanilla model hook callbacks.
    ///     Gameplay-affecting multiplayer logic should use this path because vanilla hooks await model callbacks.
    ///     基于模型的能力可实现此协定，以接收 owner 的原版模型 hook 回调。
    ///     影响多人同步的 gameplay 逻辑应使用此路径，因为原版 hook 会 await 模型回调。
    /// </summary>
    public interface IModelCapabilityHookListener
    {
        /// <summary>
        ///     Whether this capability should be inserted into the owning model's vanilla hook listener stream.
        ///     此能力是否应插入所属模型的原版 hook listener 流。
        /// </summary>
        bool ShouldReceiveOwnerHooks => true;

        /// <summary>
        ///     Ordering relative to the owner. Negative values run before the owner, zero and positive values after.
        ///     相对 owner 的顺序。负值在 owner 前运行，零和正值在 owner 后运行。
        /// </summary>
        int OwnerHookOrder => 0;
    }

    internal static class ModelCapabilityHookListeners
    {
        private static readonly ConditionalWeakTable<AbstractModel, DefaultCapabilitySourceCache>
            DefaultCapabilitySourceCaches = [];

        internal static void InvalidateDefaultCapabilitySourceCache()
        {
            DefaultCapabilitySourceCaches.Clear();
        }

        internal static IEnumerable<AbstractModel> ExpandOwnerHookListeners(IEnumerable<AbstractModel> owners)
        {
            foreach (var owner in owners)
            {
                if (owner is IModelCapability capability)
                {
                    if (capability.Owner == null)
                        yield return owner;
                    continue;
                }

                var capabilities = GetOwnerHookCapabilities(owner);
                var index = 0;

                for (; index < capabilities.Count && capabilities[index].OwnerHookOrder < 0; index++)
                {
                    var entry = capabilities[index];
                    if (TryGetStillAttachedModel(entry, owner, out var model))
                        yield return model;
                }

                yield return owner;

                for (; index < capabilities.Count; index++)
                {
                    var entry = capabilities[index];
                    if (TryGetStillAttachedModel(entry, owner, out var model))
                        yield return model;
                }
            }
        }

        private static OwnerHookCapabilitySnapshot GetOwnerHookCapabilities(AbstractModel owner)
        {
            if (!ModelCapabilities.TryGet(owner, out var collection))
            {
                var sourceCache = DefaultCapabilitySourceCaches.GetValue(
                    owner,
                    static model => new(ModelCapabilityDefaults.HasDefaultCapabilitySource(model)));
                if (!sourceCache.HasDefaultCapabilitySource)
                    return default;

                collection = ModelCapabilities.Get(owner);
            }

            var capabilities = collection.GetOwnerHookCandidateSnapshot();
            if (capabilities.Length == 0)
                return default;

            OwnerHookCapabilityEntry? singleListener = null;
            List<OwnerHookCapabilityEntry>? listeners = null;
            for (var index = 0; index < capabilities.Length; index++)
            {
                var capability = capabilities[index];
                if (capability is not (IModelCapabilityHookListener { ShouldReceiveOwnerHooks: true } listener
                    and AbstractModel model))
                    continue;

                var entry = new OwnerHookCapabilityEntry(capability, model, listener.OwnerHookOrder, index);
                if (!singleListener.HasValue)
                {
                    singleListener = entry;
                    continue;
                }

                listeners ??= new(capabilities.Length)
                {
                    singleListener.Value,
                };
                listeners.Add(entry);
            }

            if (listeners == null)
                return new(singleListener);

            listeners.Sort(static (left, right) =>
            {
                var order = left.OwnerHookOrder.CompareTo(right.OwnerHookOrder);
                return order != 0 ? order : left.Index.CompareTo(right.Index);
            });

            return new(listeners);
        }

        private static bool TryGetStillAttachedModel(
            OwnerHookCapabilityEntry entry,
            AbstractModel owner,
            out AbstractModel model)
        {
            model = entry.Model;
            return ReferenceEquals(entry.Capability.Owner, owner);
        }

        private readonly record struct OwnerHookCapabilityEntry(
            IModelCapability Capability,
            AbstractModel Model,
            int OwnerHookOrder,
            int Index);

        private sealed record DefaultCapabilitySourceCache(bool HasDefaultCapabilitySource);

        private readonly struct OwnerHookCapabilitySnapshot
        {
            private readonly OwnerHookCapabilityEntry? _single;
            private readonly List<OwnerHookCapabilityEntry>? _multiple;

            public OwnerHookCapabilitySnapshot(OwnerHookCapabilityEntry? single)
            {
                _single = single;
                _multiple = null;
            }

            public OwnerHookCapabilitySnapshot(List<OwnerHookCapabilityEntry> multiple)
            {
                _single = null;
                _multiple = multiple;
            }

            public int Count => _multiple?.Count ?? (_single.HasValue ? 1 : 0);

            public OwnerHookCapabilityEntry this[int index]
            {
                get
                {
                    if (_multiple != null)
                        return _multiple[index];
                    if (index == 0 && _single is { } single)
                        return single;

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }
    }
}
