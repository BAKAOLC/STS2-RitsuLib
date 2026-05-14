using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Extension helpers for resolving card targets by target type.
    ///     用于按目标类型解析卡牌目标集合的扩展 helper。
    /// </summary>
    public static class CardModelTargetingExtensions
    {
        /// <summary>
        ///     Returns targets resolved from the card's current <see cref="TargetType" />.
        ///     For single-target types, pass <paramref name="selectedTarget" /> to keep one unified execution path.
        ///     返回根据当前卡牌 <see cref="TargetType" /> 解析得到的目标列表。
        ///     对单体目标类型，可传入 <paramref name="selectedTarget" /> 来保持统一执行路径。
        /// </summary>
        /// <param name="card">
        ///     Card model whose target type is used for resolution.
        ///     用于解析目标集合的卡牌模型。
        /// </param>
        /// <param name="selectedTarget">
        ///     Optional selected target for single-target types (vanilla or custom).
        ///     If null, single-target branches return an empty list.
        ///     原版或自定义单体目标类型可选传入的已选目标。
        ///     为 null 时，单体目标分支返回空列表。
        /// </param>
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
                    if (CustomTargetTypeRegistry.IsCustomSingleTargetType(card.TargetType))
                    {
                        if (selectedTarget == null)
                            return [];
                        return CustomTargetTypeRegistry.TryIsAllowedSingleTarget(
                                   card.TargetType,
                                   selectedTarget,
                                   out var allowed) &&
                               allowed
                            ? [selectedTarget]
                            : [];
                    }

                    if (!CustomTargetTypeRegistry.IsCustomMultiTargetType(card.TargetType))
                        return [];

                    return state?.Creatures
                               .Where(c =>
                                   CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(card.TargetType, c,
                                       out var include) && include)
                               .ToList() ??
                           [];
                }
            }
        }
    }
}
