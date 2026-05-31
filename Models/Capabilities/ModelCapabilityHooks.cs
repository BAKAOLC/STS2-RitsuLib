using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Opt-in contract for model-backed capabilities that should receive the owner's vanilla model hook callbacks.
    ///     Gameplay-affecting multiplayer logic should use this path because vanilla hooks await model callbacks.
    ///     基于模型的组件可实现此协定，以接收 owner 的原版模型 hook 回调。
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
        internal static IEnumerable<AbstractModel> ExpandOwnerHookListeners(IEnumerable<AbstractModel> owners)
        {
            var snapshot = owners.ToArray();
            HashSet<AbstractModel> alreadyExpandedOwners = new(ReferenceEqualityComparer.Instance);

            foreach (var component in snapshot.OfType<IModelCapability>())
                if (component.Owner != null)
                    alreadyExpandedOwners.Add(component.Owner);

            foreach (var owner in snapshot)
            {
                if (owner is IModelCapability)
                {
                    yield return owner;
                    continue;
                }

                var components =
                    alreadyExpandedOwners.Contains(owner) ? [] : GetOwnerHookCapabilities(owner);

                foreach (var entry in components)
                    if (entry.OwnerHookOrder < 0 && TryGetStillAttachedModel(entry, owner, out var model))
                        yield return model;

                yield return owner;

                foreach (var entry in components)
                    if (entry.OwnerHookOrder >= 0 && TryGetStillAttachedModel(entry, owner, out var model))
                        yield return model;
            }
        }

        private static IReadOnlyList<OwnerHookCapabilityEntry> GetOwnerHookCapabilities(AbstractModel owner)
        {
            if (!ModelCapabilities.TryGet(owner, out var collection))
            {
                if (!ModelCapabilityDefaults.HasDefaultCapabilitySource(owner))
                    return [];

                collection = ModelCapabilities.Get(owner);
            }

            var capabilities = collection.Items;
            if (capabilities.Count == 0)
                return [];

            return capabilities
                .Select(static (capability, index) => new OwnerHookCapabilityEntry(capability, index))
                .Where(static entry => entry is { ShouldReceiveOwnerHooks: true, Model: not null })
                .OrderBy(static entry => entry.OwnerHookOrder)
                .ThenBy(static entry => entry.Index)
                .ToArray();
        }

        private static bool TryGetStillAttachedModel(
            OwnerHookCapabilityEntry entry,
            AbstractModel owner,
            out AbstractModel model)
        {
            model = entry.Model!;
            return model != null && ReferenceEquals(entry.Capability.Owner, owner);
        }

        private readonly record struct OwnerHookCapabilityEntry(IModelCapability Capability, int Index)
        {
            public bool ShouldReceiveOwnerHooks =>
                Capability is IModelCapabilityHookListener { ShouldReceiveOwnerHooks: true };

            public int OwnerHookOrder =>
                Capability is IModelCapabilityHookListener listener ? listener.OwnerHookOrder : 0;

            public AbstractModel? Model => Capability as AbstractModel;
        }
    }
}
