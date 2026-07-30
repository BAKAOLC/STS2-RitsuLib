using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Optionally determines whether a mod <see cref="AncientEventModel" /> can enter the Ancient selection
    ///         pool for a particular <see cref="ActModel" /> during room generation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选地决定模组 <see cref="AncientEventModel" /> 在生成房间时能否进入指定
    ///         <see cref="ActModel" /> 的先古之民选择池。
    ///     </para>
    /// </summary>
    public interface IModAncientActValidity
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether this Ancient is valid for <paramref name="act" />. A value of
        ///         <see langword="false" /> excludes it from <see cref="ActModel.GetUnlockedAncients" /> and the
        ///         shared-Ancient subset used by <see cref="ActModel.GenerateRooms" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此先古之民是否适用于 <paramref name="act" />。返回 <see langword="false" /> 时，它会从
        ///         <see cref="ActModel.GetUnlockedAncients" /> 的结果以及
        ///         <see cref="ActModel.GenerateRooms" /> 使用的共享先古之民子集中排除。
        ///     </para>
        /// </summary>
        bool IsValidForAct(ActModel act);
    }
}
