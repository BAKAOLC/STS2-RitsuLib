using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines RitsuLib target types and registers deterministic, mod-owned <see cref="TargetType" /> values.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 RitsuLib 目标类型，并注册确定性的模组自有 <see cref="TargetType" /> 值。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Multi-target types control reticles and target resolution. The card or potion itself still runs once
    ///         with no selected creature.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         群体目标类型控制目标指示器与目标解析；卡牌或药水本身仍会在没有已选生物的情况下执行一次。
    ///     </para>
    /// </remarks>
    public static class CustomTargetType
    {
        /// <summary>
        ///     <para xml:lang="en">Targets every living non-pet creature.</para>
        ///     <para xml:lang="zh-CN">以所有存活且非宠物的生物为目标。</para>
        /// </summary>
        public static TargetType Everyone { get; } = Mint("everyone");

        /// <summary>
        ///     <para xml:lang="en">Allows selecting any living non-pet creature.</para>
        ///     <para xml:lang="zh-CN">允许选择任意存活且非宠物的生物。</para>
        /// </summary>
        public static TargetType Anyone { get; } = Mint("anyone");

        /// <summary>
        ///     <para xml:lang="en">Targets every living enemy that intends to attack.</para>
        ///     <para xml:lang="zh-CN">以所有具有攻击意图的存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllAttackingEnemies { get; } = Mint("all_attacking_enemies");

        /// <summary>
        ///     <para xml:lang="en">Allows selecting a living enemy that intends to attack.</para>
        ///     <para xml:lang="zh-CN">允许选择具有攻击意图的存活敌人。</para>
        /// </summary>
        public static TargetType AnyAttackingEnemy { get; } = Mint("any_attacking_enemy");

        /// <summary>
        ///     <para xml:lang="en">Targets every living enemy with Block.</para>
        ///     <para xml:lang="zh-CN">以所有拥有格挡的存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllBlockingEnemies { get; } = Mint("all_blocking_enemies");

        /// <summary>
        ///     <para xml:lang="en">Allows selecting a living enemy with Block.</para>
        ///     <para xml:lang="zh-CN">允许选择拥有格挡的存活敌人。</para>
        /// </summary>
        public static TargetType AnyBlockingEnemy { get; } = Mint("any_blocking_enemy");

        /// <summary>
        ///     <para xml:lang="en">Targets every living enemy without Block.</para>
        ///     <para xml:lang="zh-CN">以所有没有格挡的存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllNonBlockingEnemies { get; } = Mint("all_non_blocking_enemies");

        /// <summary>
        ///     <para xml:lang="en">Allows selecting a living enemy without Block.</para>
        ///     <para xml:lang="zh-CN">允许选择没有格挡的存活敌人。</para>
        /// </summary>
        public static TargetType AnyNonBlockingEnemy { get; } = Mint("any_non_blocking_enemy");

        /// <summary>
        ///     <para xml:lang="en">Targets all living enemies tied for the highest current HP.</para>
        ///     <para xml:lang="zh-CN">以当前生命值并列最高的所有存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllHighestHpEnemies { get; } = Mint("all_highest_hp_enemies");

        /// <summary>
        ///     <para xml:lang="en">Targets all living enemies tied for the lowest current HP.</para>
        ///     <para xml:lang="zh-CN">以当前生命值并列最低的所有存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllLowestHpEnemies { get; } = Mint("all_lowest_hp_enemies");

        /// <summary>
        ///     <para xml:lang="en">Allows selecting a living enemy at full HP.</para>
        ///     <para xml:lang="zh-CN">允许选择生命值已满的存活敌人。</para>
        /// </summary>
        public static TargetType AnyFullLifeEnemy { get; } = Mint("any_full_life_enemy");

        /// <summary>
        ///     <para xml:lang="en">Targets every living enemy at full HP.</para>
        ///     <para xml:lang="zh-CN">以所有生命值已满的存活敌人为目标。</para>
        /// </summary>
        public static TargetType AllFullLifeEnemies { get; } = Mint("all_full_life_enemies");

        /// <summary>
        ///     <para xml:lang="en">Determines whether <paramref name="type" /> is registered by RitsuLib.</para>
        ///     <para xml:lang="zh-CN">判断 <paramref name="type" /> 是否由 RitsuLib 注册。</para>
        /// </summary>
        /// <param name="type">
        ///     <para xml:lang="en">The target type to inspect.</para>
        ///     <para xml:lang="zh-CN">待检查的目标类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if RitsuLib registered the type.</para>
        ///     <para xml:lang="zh-CN">该类型由 RitsuLib 注册时为 <see langword="true" />。</para>
        /// </returns>
        public static bool IsRitsuCustom(TargetType type)
        {
            return CustomTargetTypeRegistry.IsRitsuCustom(type);
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether <paramref name="type" /> is a custom single-target type.</para>
        ///     <para xml:lang="zh-CN">判断 <paramref name="type" /> 是否为自定义单体目标类型。</para>
        /// </summary>
        /// <param name="type">
        ///     <para xml:lang="en">The target type to inspect.</para>
        ///     <para xml:lang="zh-CN">待检查的目标类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the type selects one creature.</para>
        ///     <para xml:lang="zh-CN">该类型选择单个生物时为 <see langword="true" />。</para>
        /// </returns>
        public static bool IsCustomSingleTargetType(TargetType type)
        {
            return CustomTargetTypeResolver.IsCustomSingleTargetType(type);
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether <paramref name="type" /> is a custom multi-target type.</para>
        ///     <para xml:lang="zh-CN">判断 <paramref name="type" /> 是否为自定义群体目标类型。</para>
        /// </summary>
        /// <param name="type">
        ///     <para xml:lang="en">The target type to inspect.</para>
        ///     <para xml:lang="zh-CN">待检查的目标类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the type resolves multiple creatures.</para>
        ///     <para xml:lang="zh-CN">该类型解析多个生物时为 <see langword="true" />。</para>
        /// </returns>
        public static bool IsCustomMultiTargetType(TargetType type)
        {
            return CustomTargetTypeResolver.IsCustomMultiTargetType(type);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a deterministic, mod-owned single-target type.</para>
        ///     <para xml:lang="zh-CN">注册确定性的模组自有单体目标类型。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">The stable mod-local ID stem.</para>
        ///     <para xml:lang="zh-CN">稳定的模组内 ID 词干。</para>
        /// </param>
        /// <param name="canTarget">
        ///     <para xml:lang="en">The predicate that accepts or rejects a candidate creature.</para>
        ///     <para xml:lang="zh-CN">接受或拒绝候选生物的谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The value derived from <paramref name="modId" /> and <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由 <paramref name="modId" /> 和 <paramref name="localStem" /> 派生的值。
        ///     </para>
        /// </returns>
        public static TargetType RegisterSingleTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> canTarget)
        {
            ArgumentNullException.ThrowIfNull(canTarget);
            return RegisterSingleTargetType(modId, localStem, (creature, _) => canTarget(creature));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a deterministic single-target type whose predicate receives the user.</para>
        ///     <para xml:lang="zh-CN">注册谓词可接收使用者的确定性单体目标类型。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">The stable mod-local ID stem.</para>
        ///     <para xml:lang="zh-CN">稳定的模组内 ID 词干。</para>
        /// </param>
        /// <param name="canTarget">
        ///     <para xml:lang="en">The predicate that receives a candidate creature and the card or potion user.</para>
        ///     <para xml:lang="zh-CN">接收候选生物和卡牌或药水使用者的谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The value derived from <paramref name="modId" /> and <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由 <paramref name="modId" /> 和 <paramref name="localStem" /> 派生的值。
        ///     </para>
        /// </returns>
        public static TargetType RegisterSingleTargetType(
            string modId,
            string localStem,
            Func<Creature, Player, bool> canTarget)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(canTarget);

            var (_, id, type) = DynamicEnumValueRegistry<TargetType>.RegisterOwned(modId, localStem);
            CustomTargetTypeRegistry.RegisterSingleTargetType(type, id, canTarget);
            return type;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a deterministic single-target type whose predicate receives the source card or potion.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册谓词可接收来源卡牌或药水的确定性单体目标类型。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">The stable mod-local ID stem.</para>
        ///     <para xml:lang="zh-CN">稳定的模组内 ID 词干。</para>
        /// </param>
        /// <param name="canTarget">
        ///     <para xml:lang="en">The predicate that receives a <see cref="CustomTargetContext" />.</para>
        ///     <para xml:lang="zh-CN">接收 <see cref="CustomTargetContext" /> 的谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The value derived from <paramref name="modId" /> and <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由 <paramref name="modId" /> 和 <paramref name="localStem" /> 派生的值。
        ///     </para>
        /// </returns>
        public static TargetType RegisterSingleTargetTypeWithContext(
            string modId,
            string localStem,
            Func<CustomTargetContext, bool> canTarget)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(canTarget);

            var (_, id, type) = DynamicEnumValueRegistry<TargetType>.RegisterOwned(modId, localStem);
            CustomTargetTypeRegistry.RegisterSingleTargetTypeWithContext(type, id, canTarget);
            return type;
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a deterministic, mod-owned multi-target type.</para>
        ///     <para xml:lang="zh-CN">注册确定性的模组自有群体目标类型。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The card or potion runs once with no selected creature. Resolve its affected creatures through
        ///         <see cref="CardModelTargetingExtensions.GetTargets" /> or
        ///         <see cref="PotionModelTargetingExtensions.GetTargets" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         卡牌或药水会在没有已选生物的情况下执行一次。请通过
        ///         <see cref="CardModelTargetingExtensions.GetTargets" /> 或
        ///         <see cref="PotionModelTargetingExtensions.GetTargets" /> 解析受影响的生物。
        ///     </para>
        /// </remarks>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">The stable mod-local ID stem.</para>
        ///     <para xml:lang="zh-CN">稳定的模组内 ID 词干。</para>
        /// </param>
        /// <param name="includeTarget">
        ///     <para xml:lang="en">The predicate that accepts or rejects each creature.</para>
        ///     <para xml:lang="zh-CN">接受或拒绝各生物的谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The value derived from <paramref name="modId" /> and <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由 <paramref name="modId" /> 和 <paramref name="localStem" /> 派生的值。
        ///     </para>
        /// </returns>
        public static TargetType RegisterMultiTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> includeTarget)
        {
            ArgumentNullException.ThrowIfNull(includeTarget);
            return RegisterMultiTargetType(modId, localStem, (creature, _) => includeTarget(creature));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a deterministic multi-target type whose predicate receives the user.</para>
        ///     <para xml:lang="zh-CN">注册谓词可接收使用者的确定性群体目标类型。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The card or potion runs once with no selected creature. Resolve its affected creatures through
        ///         <see cref="CardModelTargetingExtensions.GetTargets" /> or
        ///         <see cref="PotionModelTargetingExtensions.GetTargets" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         卡牌或药水会在没有已选生物的情况下执行一次。请通过
        ///         <see cref="CardModelTargetingExtensions.GetTargets" /> 或
        ///         <see cref="PotionModelTargetingExtensions.GetTargets" /> 解析受影响的生物。
        ///     </para>
        /// </remarks>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="localStem">
        ///     <para xml:lang="en">The stable mod-local ID stem.</para>
        ///     <para xml:lang="zh-CN">稳定的模组内 ID 词干。</para>
        /// </param>
        /// <param name="includeTarget">
        ///     <para xml:lang="en">The predicate that receives a creature and the card or potion user.</para>
        ///     <para xml:lang="zh-CN">接收生物和卡牌或药水使用者的谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The value derived from <paramref name="modId" /> and <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         由 <paramref name="modId" /> 和 <paramref name="localStem" /> 派生的值。
        ///     </para>
        /// </returns>
        public static TargetType RegisterMultiTargetType(
            string modId,
            string localStem,
            Func<Creature, Player, bool> includeTarget)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(includeTarget);

            var (_, id, type) = DynamicEnumValueRegistry<TargetType>.RegisterOwned(modId, localStem);
            CustomTargetTypeRegistry.RegisterMultiTargetType(type, id, includeTarget);
            return type;
        }

        private static TargetType Mint(string localStem)
        {
            var id = ModContentRegistry.GetQualifiedTargetTypeId(Const.ModId, localStem);
            return DynamicEnumValueRegistry<TargetType>.GetValue(id);
        }
    }
}
