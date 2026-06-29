using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     LocString table/key mapping for a known model family.
    ///     已知模型族的 LocString 表/key 映射。
    /// </summary>
    public sealed record ModelLocStringSource(
        Type ModelType,
        string Table,
        Func<AbstractModel, string> Key,
        Func<AbstractModel, LocString> Resolve)
    {
        /// <summary>
        ///     Returns whether this source applies to <paramref name="model" />.
        ///     返回此来源是否适用于 <paramref name="model" />。
        /// </summary>
        public bool Matches(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return ModelType.IsInstanceOfType(model);
        }

        /// <summary>
        ///     Creates a LocString from the mapped table and key without reading the model property.
        ///     仅使用映射的表和 key 创建 LocString，不读取模型属性。
        /// </summary>
        public LocString CreateDefault(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new(Table, Key(model));
        }
    }
}
