using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies model-sequence merge functions while protecting nested <see cref="ModelDb" /> lookups.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         应用模型序列合并函数，同时保护嵌套的 <see cref="ModelDb" /> 查找。
    ///     </para>
    /// </summary>
    internal static class ModelDbContentPatchHelper
    {
        internal static void Append<TModel>(
            ref IEnumerable<TModel> result,
            Func<IEnumerable<TModel>, IEnumerable<TModel>> append)
            where TModel : AbstractModel
        {
            result = ModelDbGetterMerge.MergeEnumerable(result, append);
        }

        internal static void Append<TItem>(
            ref IReadOnlyList<TItem> result,
            Func<IReadOnlyList<TItem>, IReadOnlyList<TItem>> append)
        {
            result = ModelDbGetterMerge.MergeReadOnlyList(result, append);
        }
    }
}
