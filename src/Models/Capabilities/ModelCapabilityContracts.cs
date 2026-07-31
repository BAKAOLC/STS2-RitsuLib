using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Base contract for a capability attached to an <see cref="AbstractModel" /> instance.</para>
    ///     <para xml:lang="zh-CN">附加到 <see cref="AbstractModel" /> 实例的能力基础契约。</para>
    /// </summary>
    public interface IModelCapability
    {
        /// <summary>
        ///     <para xml:lang="en">Stable capability ID used for runtime lookup and persistence.</para>
        ///     <para xml:lang="zh-CN">用于运行时查找和持久化的稳定能力 ID。</para>
        /// </summary>
        string CapabilityId { get; }

        /// <summary>
        ///     <para xml:lang="en">Current owning model, or <see langword="null" /> when detached.</para>
        ///     <para xml:lang="zh-CN">当前所属模型；未附加时为 <see langword="null" />。</para>
        /// </summary>
        AbstractModel? Owner { get; }

        /// <summary>
        ///     <para xml:lang="en">Called when the capability is attached to a model.</para>
        ///     <para xml:lang="zh-CN">当能力附加到模型时调用。</para>
        /// </summary>
        void Attach(AbstractModel owner, bool isInternal = false);

        /// <summary>
        ///     <para xml:lang="en">Called when the capability is detached from a model.</para>
        ///     <para xml:lang="zh-CN">当能力从模型分离时调用。</para>
        /// </summary>
        void Detach(bool isInternal = false);
    }

    /// <summary>
    ///     <para xml:lang="en">Typed capability contract for capabilities that are only valid on one model family.</para>
    ///     <para xml:lang="zh-CN">只适用于某个模型族的类型化能力协定。</para>
    /// </summary>
    public interface IModelCapability<out TModel> : IModelCapability
        where TModel : AbstractModel
    {
        /// <summary>
        ///     <para xml:lang="en">Current owning model, or <see langword="null" /> when detached.</para>
        ///     <para xml:lang="zh-CN">当前所属模型；未附加时为 <see langword="null" />。</para>
        /// </summary>
        new TModel? Owner { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Optional provider implemented by ordinary model classes to seed their default capability list.</para>
    ///     <para xml:lang="zh-CN">普通模型类可实现的可选提供器，用于填充默认能力列表。</para>
    /// </summary>
    public interface IModelCapabilitySource
    {
        /// <summary>
        ///     <para xml:lang="en">Adds this model's own default capabilities to <paramref name="capabilities" />.</para>
        ///     <para xml:lang="zh-CN">将此模型自身的默认能力添加到 <paramref name="capabilities" />。</para>
        /// </summary>
        void BuildDefaultCapabilities(ModelCapabilityList capabilities);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional capability merge behavior used by <see cref="ModelCapabilitySet.Apply" />.</para>
    ///     <para xml:lang="zh-CN"><see cref="ModelCapabilitySet.Apply" /> 使用的可选能力合并行为。</para>
    /// </summary>
    public interface IModelCapabilityMergeHandler
    {
        /// <summary>
        ///     <para xml:lang="en">Attempts to merge <paramref name="incoming" /> into this capability.</para>
        ///     <para xml:lang="zh-CN">尝试将 <paramref name="incoming" /> 合并到此能力。</para>
        /// </summary>
        bool TryMergeWith(
            IModelCapability incoming,
            ApplyModelCapabilityOptions options,
            out IModelCapability? merged);

        /// <summary>
        ///     <para xml:lang="en">Attempts to subtract <paramref name="incoming" /> from this capability.</para>
        ///     <para xml:lang="zh-CN">尝试从此能力中减去 <paramref name="incoming" />。</para>
        /// </summary>
        bool TrySubtractiveMergeWith(
            IModelCapability incoming,
            ApplyModelCapabilityOptions options,
            out IModelCapability? merged);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional capability JSON persistence behavior.</para>
    ///     <para xml:lang="zh-CN">可选能力 JSON 持久化行为。</para>
    /// </summary>
    public interface IModelCapabilityJsonState
    {
        /// <summary>
        ///     <para xml:lang="en">Current schema version written for this capability's state.</para>
        ///     <para xml:lang="zh-CN">此能力状态写入的当前架构版本。</para>
        /// </summary>
        int SchemaVersion => 1;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Saves capability state. Return <see langword="null" /> for stateless capabilities.
        ///     </para>
        ///     <para xml:lang="zh-CN">保存能力状态。无状态能力可返回 <see langword="null" />。</para>
        /// </summary>
        JsonNode? SaveState();

        /// <summary>
        ///     <para xml:lang="en">Loads capability state.</para>
        ///     <para xml:lang="zh-CN">加载能力状态。</para>
        /// </summary>
        void LoadState(JsonNode? state, int schemaVersion);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional capability callback invoked after its owner has been cloned.</para>
    ///     <para xml:lang="zh-CN">所属模型被复制后调用的可选能力回调。</para>
    /// </summary>
    public interface IModelCapabilityCloneNotification
    {
        /// <summary>
        ///     <para xml:lang="en">Called on the cloned capability after it has been attached to <paramref name="clonedOwner" />.</para>
        ///     <para xml:lang="zh-CN">在复制出的能力附加到 <paramref name="clonedOwner" /> 后调用。</para>
        /// </summary>
        void AfterOwnerCloned(AbstractModel originalOwner, AbstractModel clonedOwner,
            IModelCapability originalCapability);
    }

    /// <summary>
    ///     <para xml:lang="en">Optional capability cloning behavior.</para>
    ///     <para xml:lang="zh-CN">可选能力复制行为。</para>
    /// </summary>
    public interface IModelCapabilityCloneHandler
    {
        /// <summary>
        ///     <para xml:lang="en">Creates the capability instance attached to a cloned owner.</para>
        ///     <para xml:lang="zh-CN">创建附加到复制后所属模型的能力实例。</para>
        /// </summary>
        IModelCapability CloneFor(AbstractModel clonedOwner);
    }

    /// <summary>
    ///     <para xml:lang="en">Options used while applying a capability.</para>
    ///     <para xml:lang="zh-CN">应用能力时使用的选项。</para>
    /// </summary>
    public readonly record struct ApplyModelCapabilityOptions(
        bool AllowMerge = true,
        bool UseSubtractiveMerge = false,
        bool IsUpgrade = false,
        IReadOnlyDictionary<string, object?>? Extra = null)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates options for applying a capability as part of an owner upgrade.</para>
        ///     <para xml:lang="zh-CN">创建在所属模型升级期间应用能力时使用的选项。</para>
        /// </summary>
        public static ApplyModelCapabilityOptions Upgrade(
            bool allowMerge = true,
            IReadOnlyDictionary<string, object?>? extra = null)
        {
            return new(allowMerge, false, true, extra);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Controls how unknown saved capability entries are handled by bulk collection operations.</para>
    ///     <para xml:lang="zh-CN">控制批量集合操作如何处理未知的已保存能力条目。</para>
    /// </summary>
    public enum UnknownModelCapabilityPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Keep unknown entries so future/optional capability data round-trips.</para>
        ///     <para xml:lang="zh-CN">保留未知条目，以便未来或可选能力数据能继续往返保存。</para>
        /// </summary>
        Preserve,

        /// <summary>
        ///     <para xml:lang="en">Remove unknown entries as well.</para>
        ///     <para xml:lang="zh-CN">同时移除未知条目。</para>
        /// </summary>
        Remove,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Controls ordered insertion when the requested anchor capability is not attached.
    ///     </para>
    ///     <para xml:lang="zh-CN">控制有序插入时找不到锚点能力的处理方式。</para>
    /// </summary>
    public enum MissingModelCapabilityAnchorPolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Add the capability at the end.</para>
        ///     <para xml:lang="zh-CN">添加到末尾。</para>
        /// </summary>
        Append,

        /// <summary>
        ///     <para xml:lang="en">Add the capability at the beginning.</para>
        ///     <para xml:lang="zh-CN">添加到开头。</para>
        /// </summary>
        Prepend,

        /// <summary>
        ///     <para xml:lang="en">Do not add the capability.</para>
        ///     <para xml:lang="zh-CN">不添加能力。</para>
        /// </summary>
        Skip,

        /// <summary>
        ///     <para xml:lang="en">Throw an exception.</para>
        ///     <para xml:lang="zh-CN">抛出异常。</para>
        /// </summary>
        Throw,
    }
}
