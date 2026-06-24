using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Base contract for a capability attached to an <see cref="AbstractModel" /> instance.
    ///     附加到 <see cref="AbstractModel" /> 实例上的能力基础协定。
    /// </summary>
    public interface IModelCapability
    {
        /// <summary>
        ///     Stable capability id used for runtime lookup and persistence.
        ///     用于运行时查找和持久化的稳定能力 ID。
        /// </summary>
        string CapabilityId { get; }

        /// <summary>
        ///     Current owning model, or null when detached.
        ///     当前所属模型；未附加时为 null。
        /// </summary>
        AbstractModel? Owner { get; }

        /// <summary>
        ///     Called when the capability is attached to a model.
        ///     当能力附加到模型时调用。
        /// </summary>
        void Attach(AbstractModel owner, bool isInternal = false);

        /// <summary>
        ///     Called when the capability is detached from a model.
        ///     当能力从模型分离时调用。
        /// </summary>
        void Detach(bool isInternal = false);
    }

    /// <summary>
    ///     Typed capability contract for capabilities that are only valid on one model family.
    ///     只适用于某个模型族的类型化能力协定。
    /// </summary>
    public interface IModelCapability<out TModel> : IModelCapability
        where TModel : AbstractModel
    {
        /// <summary>
        ///     Current owning model, or null when detached.
        ///     当前所属模型；未附加时为 null。
        /// </summary>
        new TModel? Owner { get; }
    }

    /// <summary>
    ///     Optional provider implemented by ordinary model classes to seed their default capability list.
    ///     普通模型类可实现的可选提供器，用于填充默认能力列表。
    /// </summary>
    public interface IModelCapabilitySource
    {
        /// <summary>
        ///     Adds this model's own default capabilities to <paramref name="capabilities" />.
        ///     将此模型自身的默认能力添加到 <paramref name="capabilities" />。
        /// </summary>
        void BuildDefaultCapabilities(ModelCapabilityList capabilities);
    }

    /// <summary>
    ///     Optional capability merge behavior used by <see cref="ModelCapabilitySet.Apply" />.
    ///     <see cref="ModelCapabilitySet.Apply" /> 使用的可选能力合并行为。
    /// </summary>
    public interface IModelCapabilityMergeHandler
    {
        /// <summary>
        ///     Attempts to merge <paramref name="incoming" /> into this capability.
        ///     尝试将 <paramref name="incoming" /> 合并到此能力。
        /// </summary>
        bool TryMergeWith(
            IModelCapability incoming,
            ApplyModelCapabilityOptions options,
            out IModelCapability? merged);

        /// <summary>
        ///     Attempts to subtract <paramref name="incoming" /> from this capability.
        ///     尝试从此能力中减去 <paramref name="incoming" />。
        /// </summary>
        bool TrySubtractiveMergeWith(
            IModelCapability incoming,
            ApplyModelCapabilityOptions options,
            out IModelCapability? merged);
    }

    /// <summary>
    ///     Optional capability JSON persistence behavior.
    ///     可选能力 JSON 持久化行为。
    /// </summary>
    public interface IModelCapabilityJsonState
    {
        /// <summary>
        ///     Current schema version written for this capability's state.
        ///     此能力状态写入的当前 schema 版本。
        /// </summary>
        int SchemaVersion => 1;

        /// <summary>
        ///     Saves capability state. Return null for stateless capabilities.
        ///     保存能力状态。无状态能力可返回 null。
        /// </summary>
        JsonNode? SaveState();

        /// <summary>
        ///     Loads capability state.
        ///     加载能力状态。
        /// </summary>
        void LoadState(JsonNode? state, int schemaVersion);
    }

    /// <summary>
    ///     Optional capability callback invoked after its owner has been cloned.
    ///     owner 被复制后调用的可选能力回调。
    /// </summary>
    public interface IModelCapabilityCloneNotification
    {
        /// <summary>
        ///     Called on the cloned capability after it has been attached to <paramref name="clonedOwner" />.
        ///     在复制出的能力附加到 <paramref name="clonedOwner" /> 后调用。
        /// </summary>
        void AfterOwnerCloned(AbstractModel originalOwner, AbstractModel clonedOwner,
            IModelCapability originalCapability);
    }

    /// <summary>
    ///     Optional capability cloning behavior.
    ///     可选能力复制行为。
    /// </summary>
    public interface IModelCapabilityCloneHandler
    {
        /// <summary>
        ///     Creates the capability instance attached to a cloned owner.
        ///     创建附加到复制后 owner 的能力实例。
        /// </summary>
        IModelCapability CloneFor(AbstractModel clonedOwner);
    }

    /// <summary>
    ///     Options used while applying a capability.
    ///     应用能力时使用的选项。
    /// </summary>
    public readonly record struct ApplyModelCapabilityOptions(
        bool AllowMerge = true,
        bool UseSubtractiveMerge = false,
        bool IsUpgrade = false,
        IReadOnlyDictionary<string, object?>? Extra = null)
    {
        /// <summary>
        ///     Creates options for applying a capability as part of an owner upgrade.
        ///     创建用于在 owner 升级期间应用能力的选项。
        /// </summary>
        public static ApplyModelCapabilityOptions Upgrade(
            bool allowMerge = true,
            IReadOnlyDictionary<string, object?>? extra = null)
        {
            return new(allowMerge, false, true, extra);
        }
    }

    /// <summary>
    ///     Controls how unknown saved capability entries are handled by bulk collection operations.
    ///     控制批量 collection 操作如何处理未知的已保存能力条目。
    /// </summary>
    public enum UnknownModelCapabilityPolicy
    {
        /// <summary>
        ///     Keep unknown entries so future/optional capability data round-trips.
        ///     保留未知条目，以便未来或可选能力数据能继续往返保存。
        /// </summary>
        Preserve,

        /// <summary>
        ///     Remove unknown entries as well.
        ///     同时移除未知条目。
        /// </summary>
        Remove,
    }

    /// <summary>
    ///     Controls what ordered insertion helpers do when the requested anchor capability is not attached.
    ///     控制有序插入 helper 找不到锚点能力时的行为。
    /// </summary>
    public enum MissingModelCapabilityAnchorPolicy
    {
        /// <summary>
        ///     Add the capability at the end.
        ///     添加到末尾。
        /// </summary>
        Append,

        /// <summary>
        ///     Add the capability at the beginning.
        ///     添加到开头。
        /// </summary>
        Prepend,

        /// <summary>
        ///     Do not add the capability.
        ///     不添加能力。
        /// </summary>
        Skip,

        /// <summary>
        ///     Throw an exception.
        ///     抛出异常。
        /// </summary>
        Throw,
    }
}
