using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Extension helpers for resolving potion targets by target type.
    ///     用于按目标类型解析药水目标集合的扩展 helper。
    /// </summary>
    public static class PotionModelTargetingExtensions
    {
        /// <summary>
        ///     Returns targets resolved from the potion's current <see cref="TargetType" />.
        ///     For single-target types, pass <paramref name="selectedTarget" /> to keep one unified execution path.
        ///     返回根据当前药水 <see cref="TargetType" /> 解析得到的目标列表。
        ///     对单体目标类型，可传入 <paramref name="selectedTarget" /> 来保持统一执行路径。
        /// </summary>
        /// <param name="potion">
        ///     Potion model whose target type is used for resolution.
        ///     用于解析目标集合的药水模型。
        /// </param>
        /// <param name="selectedTarget">
        ///     Optional selected target for single-target types (vanilla or custom).
        ///     If null, single-target branches return an empty list, except <see cref="TargetType.Self" />.
        ///     原版或自定义单体目标类型可选传入的已选目标。
        ///     为 null 时，除 <see cref="TargetType.Self" /> 外，单体目标分支返回空列表。
        /// </param>
        public static List<Creature> GetTargets(this PotionModel potion, Creature? selectedTarget = null)
        {
            ArgumentNullException.ThrowIfNull(potion);

            var owner = potion.Owner.Creature;
            var state = owner.CombatState;
            switch (potion.TargetType)
            {
                case TargetType.AnyEnemy:
                case TargetType.AnyAlly:
                case TargetType.AnyPlayer:
                {
                    if (selectedTarget == null)
                        return [];
                    return IsValidTarget(potion, selectedTarget) ? [selectedTarget] : [];
                }
                case TargetType.AllAllies:
                    return state?.GetCreaturesOnSide(owner.Side).Where(c => c.IsAlive).ToList() ?? [];
                case TargetType.AllEnemies:
                    return state?.HittableEnemies.ToList() ?? [];
                case TargetType.RandomEnemy:
                {
                    var allTargets = state?.HittableEnemies.ToList();
                    if (allTargets == null || allTargets.Count == 0)
                        return [];
                    var target = potion.Owner.RunState.Rng.CombatTargets.NextItem(allTargets);
                    return target == null ? [] : [target];
                }
                case TargetType.None:
                case TargetType.TargetedNoCreature:
                    return [];
                case TargetType.Self:
                    return IsValidTarget(potion, selectedTarget ?? owner) ? [owner] : [];
                default:
                {
                    if (CustomTargetTypeResolver.IsCustomSingleTargetType(potion.TargetType))
                    {
                        if (selectedTarget == null)
                            return [];
                        return CustomTargetTypeResolver.TryIsAllowedSingleTarget(
                                   potion.TargetType,
                                   CustomTargetContext.ForPotion(selectedTarget, potion),
                                   out var allowed) &&
                               allowed
                            ? [selectedTarget]
                            : [];
                    }

                    if (!CustomTargetTypeResolver.IsCustomMultiTargetType(potion.TargetType))
                        return [];

                    return state?.Creatures
                               .Where(c =>
                                   CustomTargetTypeResolver.TryShouldIncludeMultiTarget(potion.TargetType, c,
                                       potion.Owner,
                                       out var include) && include)
                               .ToList() ??
                           [];
                }
            }
        }

        private static bool IsValidTarget(PotionModel potion, Creature? target)
        {
#if STS2_AT_LEAST_0_106_0
            return potion.IsValidTarget(target);
#else
            if (target == null)
                return potion.TargetType == TargetType.TargetedNoCreature || !potion.TargetType.IsSingleTarget();

            if (!target.IsAlive)
                return false;

            if (potion.TargetType == TargetType.AnyEnemy)
                return target.Side != potion.Owner.Creature.Side;

            if (potion.TargetType == TargetType.AnyAlly)
                return target.Side == potion.Owner.Creature.Side && target != potion.Owner.Creature;

            if (potion.TargetType == TargetType.AnyPlayer)
                return target.IsPlayer;

            if (potion.TargetType == TargetType.Self)
                return target == potion.Owner.Creature;

            return false;
#endif
        }
    }
}
