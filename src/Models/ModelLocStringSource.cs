using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en"><see cref="LocString" /> table-and-key mapping for a known model family.</para>
    ///     <para xml:lang="zh-CN">已知模型族的 <see cref="LocString" /> 表与键映射。</para>
    /// </summary>
    public sealed record ModelLocStringSource(
        Type ModelType,
        string Table,
        Func<AbstractModel, string> Key,
        Func<AbstractModel, LocString> Resolve)
    {
        /// <summary>
        ///     <para xml:lang="en">Determines whether this source applies to <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">确定此来源是否适用于 <paramref name="model" />。</para>
        /// </summary>
        public bool Matches(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return ModelType.IsInstanceOfType(model);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="LocString" /> from the mapped table and key without reading the model property.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅使用映射的表与键创建 <see cref="LocString" />，不读取模型属性。
        ///     </para>
        /// </summary>
        public LocString CreateDefault(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new(Table, Key(model));
        }
    }
}
