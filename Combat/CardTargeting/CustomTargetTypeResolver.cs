using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

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

        internal static bool TryIsAllowedSingleTarget(TargetType type, Creature creature, out bool allowed)
        {
            return CustomTargetTypeRegistry.TryIsAllowedSingleTarget(type, creature, out allowed) ||
                   BaseLibTargetTypeBridge.TryIsAllowedSingleTarget(type, creature, out allowed);
        }

        internal static bool TryShouldIncludeMultiTarget(TargetType type, Creature creature, out bool include)
        {
            return CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(type, creature, out include) ||
                   BaseLibTargetTypeBridge.TryShouldIncludeMultiTarget(type, creature, out include);
        }
    }
}
