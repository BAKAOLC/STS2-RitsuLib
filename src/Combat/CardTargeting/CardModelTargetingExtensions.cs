using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">Resolves card targets according to their target type.</para>
    ///     <para xml:lang="zh-CN">根据卡牌的目标类型解析目标。</para>
    /// </summary>
    public static class CardModelTargetingExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves targets from the card's current <see cref="CardModel.TargetType" />.</para>
        ///     <para xml:lang="zh-CN">根据卡牌当前的 <see cref="CardModel.TargetType" /> 解析目标。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">The card whose targets are resolved.</para>
        ///     <para xml:lang="zh-CN">待解析目标的卡牌。</para>
        /// </param>
        /// <param name="selectedTarget">
        ///     <para xml:lang="en">
        ///         The selected target for selection-based vanilla or custom single-target types. When omitted, those
        ///         types resolve no targets; <see cref="TargetType.Self" /> still resolves to the owner.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         基于选择的原版或自定义单体目标类型所用的已选目标。省略时这些类型不解析出目标；
        ///         <see cref="TargetType.Self" /> 仍解析为卡牌所有者。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The targets resolved for the card.</para>
        ///     <para xml:lang="zh-CN">为该卡牌解析出的目标。</para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Resolving <see cref="TargetType.RandomEnemy" /> advances the owner's combat-target RNG.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <see cref="TargetType.RandomEnemy" /> 会推进所有者的战斗目标随机数生成器。
        ///     </para>
        /// </remarks>
        public static List<Creature> GetTargets(this CardModel card, Creature? selectedTarget = null)
        {
            ArgumentNullException.ThrowIfNull(card);

            var state = card.CombatState;
            switch (card.TargetType)
            {
                case TargetType.AnyEnemy:
                case TargetType.AnyAlly:
                case TargetType.AnyPlayer:
                {
                    if (selectedTarget == null)
                        return [];
                    return card.IsValidTarget(selectedTarget) ? [selectedTarget] : [];
                }
                case TargetType.AllAllies:
                    return state?.PlayerCreatures.Where(c => c.IsAlive).ToList() ?? [];
                case TargetType.AllEnemies:
                    return state?.HittableEnemies.ToList() ?? [];
                case TargetType.RandomEnemy:
                {
                    var allTargets = state?.HittableEnemies.ToList();
                    if (allTargets == null || allTargets.Count == 0)
                        return [];
                    var target = card.Owner.RunState.Rng.CombatTargets.NextItem(allTargets);
                    return target == null ? [] : [target];
                }
                case TargetType.None:
                    return [];
                case TargetType.Self:
                    return [card.Owner.Creature];
                default:
                {
                    if (CustomTargetTypeResolver.IsCustomSingleTargetType(card.TargetType))
                    {
                        if (selectedTarget == null)
                            return [];
                        return CustomTargetTypeResolver.TryIsAllowedSingleTarget(
                                   card.TargetType,
                                   CustomTargetContext.ForCard(selectedTarget, card),
                                   out var allowed) &&
                               allowed
                            ? [selectedTarget]
                            : [];
                    }

                    if (!CustomTargetTypeResolver.IsCustomMultiTargetType(card.TargetType))
                        return [];

                    return state?.Creatures
                               .Where(c =>
                                   CustomTargetTypeResolver.TryShouldIncludeMultiTarget(card.TargetType, c,
                                       card.Owner,
                                       out var include) && include)
                               .ToList() ??
                           [];
                }
            }
        }
    }
}
