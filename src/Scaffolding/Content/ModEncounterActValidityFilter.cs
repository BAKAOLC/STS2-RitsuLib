using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies <see cref="IModEncounterActValidity" /> when building each act's encounter candidate pool.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         创建各章节的遭遇候选池时应用 <see cref="IModEncounterActValidity" />。
    ///     </para>
    /// </summary>
    public static class ModEncounterActValidityFilter
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="encounter" /> can appear in <paramref name="act" />. Encounters
        ///         without <see cref="IModEncounterActValidity" /> are allowed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="encounter" /> 能否出现在 <paramref name="act" /> 中。未实现
        ///         <see cref="IModEncounterActValidity" /> 的遭遇默认允许出现。
        ///     </para>
        /// </summary>
        public static bool IsValidForAct(ActModel act, EncounterModel encounter)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(encounter);

            return encounter is not IModEncounterActValidity validity || validity.IsValidForAct(act);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Keeps only encounters for which <see cref="IsValidForAct" /> returns <see langword="true" /> for
        ///         <paramref name="act" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         只保留针对 <paramref name="act" /> 调用 <see cref="IsValidForAct" /> 时返回
        ///         <see langword="true" /> 的遭遇。
        ///     </para>
        /// </summary>
        public static IEnumerable<EncounterModel> FilterForAct(
            ActModel act,
            IEnumerable<EncounterModel> encounters)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(encounters);

            return encounters.Where(encounter => IsValidForAct(act, encounter));
        }
    }
}
