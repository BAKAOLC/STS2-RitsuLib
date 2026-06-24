using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Controls when a model-saved data slot is written.
    ///     控制模型保存数据槽位何时写入。
    /// </summary>
    public enum ModelSavedDataWritePolicy
    {
        /// <summary>
        ///     Write only after the slot has been explicitly changed.
        ///     仅在槽位被显式修改后写入。
        /// </summary>
        WhenSet,

        /// <summary>
        ///     Write when the current value differs from the default value.
        ///     当前值不同于默认值时写入。
        /// </summary>
        WhenNonDefault,

        /// <summary>
        ///     Write whenever the slot has a value.
        ///     只要槽位有值就写入。
        /// </summary>
        AlwaysWhenPresent,
    }

    /// <summary>
    ///     Controls how model-saved data behaves when a model is cloned.
    ///     控制模型保存数据在模型复制时的行为。
    /// </summary>
    public enum ModelSavedDataClonePolicy
    {
        /// <summary>
        ///     Deep-copy the saved value using the slot serializer.
        ///     使用槽位序列化器深复制保存值。
        /// </summary>
        Copy,

        /// <summary>
        ///     Do not copy this slot to the cloned model.
        ///     不将此槽位复制到模型副本。
        /// </summary>
        Drop,

        /// <summary>
        ///     Share the same in-memory value with the cloned model.
        ///     与模型副本共享同一个内存值。
        /// </summary>
        Share,
    }

    /// <summary>
    ///     Options for one model-saved data slot.
    ///     单个模型保存数据槽位的选项。
    /// </summary>
    public sealed class ModelSavedDataOptions
    {
        /// <summary>
        ///     Current schema version written for this slot.
        ///     此槽位写入的当前 schema 版本。
        /// </summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>
        ///     Determines when this slot is written.
        ///     决定此槽位何时写入。
        /// </summary>
        public ModelSavedDataWritePolicy WritePolicy { get; init; } = ModelSavedDataWritePolicy.WhenSet;

        /// <summary>
        ///     Determines how this slot is copied during <c>AbstractModel.MutableClone</c>.
        ///     决定此槽位在 <c>AbstractModel.MutableClone</c> 期间如何复制。
        /// </summary>
        public ModelSavedDataClonePolicy ClonePolicy { get; init; } = ModelSavedDataClonePolicy.Copy;

        /// <summary>
        ///     Optional slot migrations.
        ///     可选的槽位迁移。
        /// </summary>
        public IReadOnlyList<IMigration>? Migrations { get; init; }
    }
}
