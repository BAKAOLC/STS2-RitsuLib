using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Unified custom TargetType lookup across RitsuLib and compatible external registries.
    /// </summary>
    internal static class CustomTargetTypeResolver
    {
        internal static bool IsCustomSingleTargetType(TargetType type)
        {
            return CustomTargetTypeRegistry.IsCustomSingleTargetType(type)
                   || BaseLibTargetTypeBridge.IsCustomSingleTargetType(type);
        }

        internal static bool IsCustomMultiTargetType(TargetType type)
        {
            return CustomTargetTypeRegistry.IsCustomMultiTargetType(type)
                   || BaseLibTargetTypeBridge.IsCustomMultiTargetType(type);
        }

        internal static bool TryIsAllowedSingleTarget(TargetType type, Creature creature, Player player,
            out bool allowed)
        {
            return TryIsAllowedSingleTarget(type, new(creature, player), out allowed);
        }

        internal static bool TryIsAllowedSingleTarget(
            TargetType type,
            CustomTargetContext context,
            out bool allowed)
        {
            return CustomTargetTypeRegistry.TryIsAllowedSingleTarget(type, context, out allowed) ||
                   BaseLibTargetTypeBridge.TryIsAllowedSingleTarget(
                       type,
                       context.TargetCreature,
                       context.Player,
                       out allowed);
        }

        internal static bool TryShouldIncludeMultiTarget(TargetType type, Creature creature, Player player,
            out bool include)
        {
            return CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(type, creature, player, out include) ||
                   BaseLibTargetTypeBridge.TryShouldIncludeMultiTarget(type, creature, player, out include);
        }
    }
}
