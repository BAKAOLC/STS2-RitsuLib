using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Specifies when a model-saved-data slot is written.</para>
    ///     <para xml:lang="zh-CN">指定模型保存数据槽位的写入时机。</para>
    /// </summary>
    public enum ModelSavedDataWritePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Writes only after the slot has been explicitly changed.</para>
        ///     <para xml:lang="zh-CN">仅在槽位被显式修改后写入。</para>
        /// </summary>
        WhenSet,

        /// <summary>
        ///     <para xml:lang="en">Writes when the current value differs from its default value.</para>
        ///     <para xml:lang="zh-CN">当前值不同于默认值时写入。</para>
        /// </summary>
        WhenNonDefault,

        /// <summary>
        ///     <para xml:lang="en">Writes whenever the slot has a value.</para>
        ///     <para xml:lang="zh-CN">只要槽位中存在值便写入。</para>
        /// </summary>
        AlwaysWhenPresent,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies how model-saved data behaves when a model is cloned.</para>
    ///     <para xml:lang="zh-CN">指定模型复制时如何处理模型保存数据。</para>
    /// </summary>
    public enum ModelSavedDataClonePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Deep-copies the saved value through the slot serializer.</para>
        ///     <para xml:lang="zh-CN">通过槽位序列化器深复制保存值。</para>
        /// </summary>
        Copy,

        /// <summary>
        ///     <para xml:lang="en">Does not copy this slot to the cloned model.</para>
        ///     <para xml:lang="zh-CN">不将此槽位复制到模型副本。</para>
        /// </summary>
        Drop,

        /// <summary>
        ///     <para xml:lang="en">Shares the same in-memory value with the cloned model.</para>
        ///     <para xml:lang="zh-CN">与模型副本共享同一个内存值。</para>
        /// </summary>
        Share,
    }

    /// <summary>
    ///     <para xml:lang="en">Configures one model-saved-data slot.</para>
    ///     <para xml:lang="zh-CN">配置单个模型保存数据槽位。</para>
    /// </summary>
    public sealed class ModelSavedDataOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the current schema version written for this slot.</para>
        ///     <para xml:lang="zh-CN">获取或初始化此槽位写入的当前架构版本。</para>
        /// </summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes when this slot is written.</para>
        ///     <para xml:lang="zh-CN">获取或初始化此槽位的写入时机。</para>
        /// </summary>
        public ModelSavedDataWritePolicy WritePolicy { get; init; } = ModelSavedDataWritePolicy.WhenSet;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes how this slot is copied during <c>AbstractModel.MutableClone</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化此槽位在 <c>AbstractModel.MutableClone</c> 期间的复制方式。
        ///     </para>
        /// </summary>
        public ModelSavedDataClonePolicy ClonePolicy { get; init; } = ModelSavedDataClonePolicy.Copy;

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the optional slot migrations.</para>
        ///     <para xml:lang="zh-CN">获取或初始化可选的槽位迁移。</para>
        /// </summary>
        public IReadOnlyList<IMigration>? Migrations { get; init; }
    }
}
