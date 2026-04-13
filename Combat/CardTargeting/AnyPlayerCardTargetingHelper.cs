using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    internal static class AnyPlayerCardTargetingHelper
    {
        internal static bool IsAnyPlayerMultiplayer(CardModel? card)
        {
            return card is { TargetType: TargetType.AnyPlayer }
                   && card.Owner.RunState.Players.Count > 1;
        }

        internal static bool IsAnyPlayerTargetValid(Creature? target)
        {
            return target is { IsAlive: true, IsPlayer: true };
        }

        internal static bool IsSingleTargetType(TargetType type)
        {
            return type is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer;
        }
    }
}
