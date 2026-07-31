using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Optionally determines whether a mod <see cref="EncounterModel" /> can enter the encounter pool for a
    ///         particular <see cref="ActModel" /> during room generation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选地决定模组 <see cref="EncounterModel" /> 在生成房间时能否进入指定
    ///         <see cref="ActModel" /> 的遭遇池。
    ///     </para>
    /// </summary>
    public interface IModEncounterActValidity
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether this encounter is valid for <paramref name="act" />. A value of
        ///         <see langword="false" /> excludes it from <see cref="ActModel.GenerateAllEncounters" />, including
        ///         normal, elite, and boss encounter pools.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此遭遇是否适用于 <paramref name="act" />。返回 <see langword="false" /> 时，它会从
        ///         <see cref="ActModel.GenerateAllEncounters" /> 的普通、精英和首领遭遇池中排除。
        ///     </para>
        /// </summary>
        bool IsValidForAct(ActModel act);
    }
}
