using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies <see cref="IModAncientActValidity" /> when building each act's Ancient candidate pool.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         创建各章节的先古之民候选池时应用 <see cref="IModAncientActValidity" />。
    ///     </para>
    /// </summary>
    public static class ModAncientActValidityFilter
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="ancient" /> can appear in <paramref name="act" />. Ancients that do
        ///         not implement <see cref="IModAncientActValidity" /> are allowed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="ancient" /> 能否出现在 <paramref name="act" /> 中。未实现
        ///         <see cref="IModAncientActValidity" /> 的先古之民默认允许出现。
        ///     </para>
        /// </summary>
        public static bool IsValidForAct(ActModel act, AncientEventModel ancient)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(ancient);

            return ancient is not IModAncientActValidity validity || validity.IsValidForAct(act);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps only the Ancients for which <see cref="IsValidForAct" /> returns
        ///         <see langword="true" /> for <paramref name="act" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         只保留针对 <paramref name="act" /> 调用 <see cref="IsValidForAct" /> 时返回
        ///         <see langword="true" /> 的先古之民。
        ///     </para>
        /// </summary>
        public static IEnumerable<AncientEventModel> FilterForAct(
            ActModel act,
            IEnumerable<AncientEventModel> ancients)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(ancients);

            return ancients.Where(ancient => IsValidForAct(act, ancient));
        }
    }
}
