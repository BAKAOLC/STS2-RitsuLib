using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    /// <para xml:lang="en">Applies <see cref="ModelDbGetterMerge" /> then mod append delegates for patched <see cref="ModelDb" /> getter postfixes.</para>
    /// <para xml:lang="zh-CN">为已修补的 <see cref="ModelDb" /> getter postfix 应用 <see cref="ModelDbGetterMerge" /> 与 mod 追加委托。</para>
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
