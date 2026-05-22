using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Base contract for a component attached to an <see cref="AbstractModel" /> instance.
    ///     附加到 <see cref="AbstractModel" /> 实例上的组件基础协定。
    /// </summary>
    public interface IModelComponent
    {
        /// <summary>
        ///     Stable component id used for runtime lookup and persistence.
        ///     用于运行时查找和持久化的稳定组件 ID。
        /// </summary>
        string ComponentId { get; }

        /// <summary>
        ///     Current owning model, or null when detached.
        ///     当前所属模型；未附加时为 null。
        /// </summary>
        AbstractModel? Owner { get; }

        /// <summary>
        ///     Called when the component is attached to a model.
        ///     当组件附加到模型时调用。
        /// </summary>
        void Attach(AbstractModel owner, bool isInternal = false);

        /// <summary>
        ///     Called when the component is detached from a model.
        ///     当组件从模型分离时调用。
        /// </summary>
        void Detach(bool isInternal = false);
    }

    /// <summary>
    ///     Typed component contract for components that are only valid on one model family.
    ///     只适用于某个模型族的类型化组件协定。
    /// </summary>
    public interface IModelComponent<out TModel> : IModelComponent
        where TModel : AbstractModel
    {
        /// <summary>
        ///     Current owning model, or null when detached.
        ///     当前所属模型；未附加时为 null。
        /// </summary>
        new TModel? Owner { get; }
    }

    /// <summary>
    ///     Optional provider implemented by ordinary model classes to seed their default component list.
    ///     普通模型类可实现的可选提供器，用于填充默认组件列表。
    /// </summary>
    public interface IModelComponentProvider
    {
        /// <summary>
        ///     Adds this model's own default components to <paramref name="components" />.
        ///     将此模型自身的默认组件添加到 <paramref name="components" />。
        /// </summary>
        void BuildDefaultComponents(ModelDefaultComponentList components);
    }

    /// <summary>
    ///     Optional component merge behavior used by <see cref="ModelComponentCollection.Apply" />.
    ///     <see cref="ModelComponentCollection.Apply" /> 使用的可选组件合并行为。
    /// </summary>
    public interface IModelComponentMergeHandler
    {
        /// <summary>
        ///     Attempts to merge <paramref name="incoming" /> into this component.
        ///     尝试将 <paramref name="incoming" /> 合并到此组件。
        /// </summary>
        bool TryMergeWith(
            IModelComponent incoming,
            ApplyModelComponentOptions options,
            out IModelComponent? merged);

        /// <summary>
        ///     Attempts to subtract <paramref name="incoming" /> from this component.
        ///     尝试从此组件中减去 <paramref name="incoming" />。
        /// </summary>
        bool TrySubtractiveMergeWith(
            IModelComponent incoming,
            ApplyModelComponentOptions options,
            out IModelComponent? merged);
    }

    /// <summary>
    ///     Optional component JSON persistence behavior.
    ///     可选组件 JSON 持久化行为。
    /// </summary>
    public interface IModelComponentJsonState
    {
        /// <summary>
        ///     Current schema version written for this component's state.
        ///     此组件状态写入的当前 schema 版本。
        /// </summary>
        int SchemaVersion => 1;

        /// <summary>
        ///     Saves component state. Return null for stateless components.
        ///     保存组件状态。无状态组件可返回 null。
        /// </summary>
        JsonNode? SaveState();

        /// <summary>
        ///     Loads component state.
        ///     加载组件状态。
        /// </summary>
        void LoadState(JsonNode? state, int schemaVersion);
    }

    /// <summary>
    ///     Optional component callback invoked after its owner has been cloned.
    ///     owner 被复制后调用的可选组件回调。
    /// </summary>
    public interface IModelComponentCloneNotification
    {
        /// <summary>
        ///     Called on the cloned component after it has been attached to <paramref name="clonedOwner" />.
        ///     在复制出的组件附加到 <paramref name="clonedOwner" /> 后调用。
        /// </summary>
        void AfterOwnerCloned(AbstractModel originalOwner, AbstractModel clonedOwner,
            IModelComponent originalComponent);
    }

    /// <summary>
    ///     Optional component cloning behavior.
    ///     可选组件复制行为。
    /// </summary>
    public interface IModelComponentCloneHandler
    {
        /// <summary>
        ///     Creates the component instance attached to a cloned owner.
        ///     创建附加到复制后 owner 的组件实例。
        /// </summary>
        IModelComponent CloneFor(AbstractModel clonedOwner);
    }

    /// <summary>
    ///     Options used while applying a component.
    ///     应用组件时使用的选项。
    /// </summary>
    public readonly record struct ApplyModelComponentOptions(
        bool AllowMerge = true,
        bool UseSubtractiveMerge = false,
        bool IsUpgrade = false,
        IReadOnlyDictionary<string, object?>? Extra = null)
    {
        /// <summary>
        ///     Creates options for applying a component as part of an owner upgrade.
        ///     创建用于在 owner 升级期间应用组件的选项。
        /// </summary>
        public static ApplyModelComponentOptions Upgrade(
            bool allowMerge = true,
            IReadOnlyDictionary<string, object?>? extra = null)
        {
            return new(allowMerge, false, true, extra);
        }
    }

    /// <summary>
    ///     Controls how unknown saved component entries are handled by bulk collection operations.
    ///     控制批量 collection 操作如何处理未知的已保存组件条目。
    /// </summary>
    public enum UnknownModelComponentPolicy
    {
        /// <summary>
        ///     Keep unknown entries so future/optional component data round-trips.
        ///     保留未知条目，以便未来或可选组件数据能继续往返保存。
        /// </summary>
        Preserve,

        /// <summary>
        ///     Remove unknown entries as well.
        ///     同时移除未知条目。
        /// </summary>
        Remove,
    }
}
