using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional gate for whether a mod <see cref="AncientEventModel" /> may enter the ancient lottery for a given
    ///     <see cref="ActModel" /> during room generation. Implement on
    ///     <see cref="ModAncientEventTemplate" /> or your ancient type; the framework applies it in
    ///     <see cref="ModAncientActValidityFilter" />.
    /// </summary>
    public interface IModAncientActValidity
    {
        /// <summary>
        ///     When false, this ancient is excluded from <see cref="ActModel.GetUnlockedAncients" /> results for
        ///     <paramref name="act" /> and from that act's shared-ancient subset at <see cref="ActModel.GenerateRooms" />.
        /// </summary>
        bool IsValidForAct(ActModel act);
    }
}
