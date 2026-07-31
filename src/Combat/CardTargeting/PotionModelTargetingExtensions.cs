using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">Resolves potion targets according to their target type.</para>
    ///     <para xml:lang="zh-CN">根据药水的目标类型解析目标。</para>
    /// </summary>
    public static class PotionModelTargetingExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves targets from the potion's current <see cref="PotionModel.TargetType" />.</para>
        ///     <para xml:lang="zh-CN">根据药水当前的 <see cref="PotionModel.TargetType" /> 解析目标。</para>
        /// </summary>
        /// <param name="potion">
        ///     <para xml:lang="en">The potion whose targets are resolved.</para>
        ///     <para xml:lang="zh-CN">待解析目标的药水。</para>
        /// </param>
        /// <param name="selectedTarget">
        ///     <para xml:lang="en">
        ///         The selected target for selection-based vanilla or custom single-target types. When omitted, those
        ///         types resolve no targets; <see cref="TargetType.Self" /> still resolves to the owner when valid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         基于选择的原版或自定义单体目标类型所用的已选目标。省略时这些类型不解析出目标；
        ///         <see cref="TargetType.Self" /> 在有效时仍解析为药水所有者。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The targets resolved for the potion.</para>
        ///     <para xml:lang="zh-CN">为该药水解析出的目标。</para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Resolving <see cref="TargetType.RandomEnemy" /> advances the owner's combat-target RNG.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <see cref="TargetType.RandomEnemy" /> 会推进所有者的战斗目标随机数生成器。
        ///     </para>
        /// </remarks>
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
