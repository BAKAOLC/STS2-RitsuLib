using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     RitsuLib-defined <see cref="TargetType" /> extensions minted via <see cref="DynamicEnumValueMinter{TEnum}" />.
    ///     Unlike BaseLib's CustomEnum system, these values are generated deterministically from stable string ids and
    ///     live entirely in the reserved high-value band.
    /// </summary>
    public static class CustomTargetType
    {
        private static readonly DynamicEnumValueMinter<TargetType> TargetTypeMinter = new();

        /// <summary>
        ///     Multi-target selection that displays reticles over all living creatures in the combat room.
        ///     This is a visual-only targeting mode: the card's play action still runs once with <c>null</c> target
        ///     unless the card model itself implements a different behavior.
        /// </summary>
        public static TargetType Everyone { get; } = Mint("everyone");

        /// <summary>
        ///     Single-target selection that allows choosing any living creature (ally or enemy).
        /// </summary>
        public static TargetType Anyone { get; } = Mint("anyone");

        /// <summary>
        ///     Whether <paramref name="type" /> is one of RitsuLib's custom target types.
        /// </summary>
        public static bool IsRitsuCustom(TargetType type)
        {
            return type == Everyone || type == Anyone;
        }

        private static TargetType Mint(string localStem)
        {
            var id = ModContentRegistry.GetCompoundId(Const.ModId, "TARGETTYPE", localStem);
            return TargetTypeMinter.Mint(id);
        }
    }
}
