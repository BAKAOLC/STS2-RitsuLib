using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     RitsuLib-defined <see cref="TargetType" /> extensions minted via <see cref="DynamicEnumValueMinter{TEnum}" />.
    ///     Unlike BaseLib's CustomEnum system, these values are generated deterministically from stable string ids and
    ///     live entirely in the reserved high-value band.
    ///     由 RitsuLib 定义并通过 <see cref="DynamicEnumValueMinter{TEnum}" /> 铸造的 <see cref="TargetType" /> 扩展。
    ///     与 BaseLib 的 CustomEnum 体系不同，这些值由稳定字符串 id 确定性生成，并位于保留的高位值区间。
    /// </summary>
    public static class CustomTargetType
    {
        /// <summary>
        ///     Multi-target selection that displays reticles over all living creatures in the combat room.
        ///     This is a visual-only targeting mode: the card's play action still runs once with <c>null</c> target
        ///     unless the card model itself implements a different behavior.
        ///     在战斗房间内所有存活生物上显示多目标指示器的群体目标模式。
        ///     该模式仅影响可视化：除非CardModel自行实现其他行为，否则打牌逻辑仍会以 <c>null</c> 目标执行一次。
        /// </summary>
        public static TargetType Everyone { get; } = Mint("everyone");

        /// <summary>
        ///     Single-target selection that allows choosing any living creature (ally or enemy).
        ///     允许选择任意存活生物（友方或敌方）的单体目标模式。
        /// </summary>
        public static TargetType Anyone { get; } = Mint("anyone");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies currently intending to attack.
        ///     群体目标模式：包含所有当前有攻击意图的存活敌人。
        /// </summary>
        public static TargetType AllAttackingEnemies { get; } = Mint("all_attacking_enemies");

        /// <summary>
        ///     Single-target mode that allows selecting one alive enemy currently intending to attack.
        ///     单体目标模式：允许选择一个当前有攻击意图的存活敌人。
        /// </summary>
        public static TargetType AnyAttackingEnemy { get; } = Mint("any_attacking_enemy");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies with block greater than zero.
        ///     群体目标模式：包含所有护甲大于零的存活敌人。
        /// </summary>
        public static TargetType AllBlockingEnemies { get; } = Mint("all_blocking_enemies");

        /// <summary>
        ///     Single-target mode that allows selecting one alive enemy with block greater than zero.
        ///     单体目标模式：允许选择一个护甲大于零的存活敌人。
        /// </summary>
        public static TargetType AnyBlockingEnemy { get; } = Mint("any_blocking_enemy");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies with zero block.
        ///     群体目标模式：包含所有护甲为零的存活敌人。
        /// </summary>
        public static TargetType AllNonBlockingEnemies { get; } = Mint("all_non_blocking_enemies");

        /// <summary>
        ///     Single-target mode that allows selecting one alive enemy with zero block.
        ///     单体目标模式：允许选择一个护甲为零的存活敌人。
        /// </summary>
        public static TargetType AnyNonBlockingEnemy { get; } = Mint("any_non_blocking_enemy");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies tied for highest current HP.
        ///     群体目标模式：包含当前生命值并列最高的所有存活敌人。
        /// </summary>
        public static TargetType AllHighestHpEnemies { get; } = Mint("all_highest_hp_enemies");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies tied for lowest current HP.
        ///     群体目标模式：包含当前生命值并列最低的所有存活敌人。
        /// </summary>
        public static TargetType AllLowestHpEnemies { get; } = Mint("all_lowest_hp_enemies");

        /// <summary>
        ///     Single-target mode that allows selecting one alive enemy at full HP.
        ///     单体目标模式：允许选择一个满生命值的存活敌人。
        /// </summary>
        public static TargetType AnyFullLifeEnemy { get; } = Mint("any_full_life_enemy");

        /// <summary>
        ///     Multi-target mode that includes all alive enemies at full HP.
        ///     群体目标模式：包含所有满生命值的存活敌人。
        /// </summary>
        public static TargetType AllFullLifeEnemies { get; } = Mint("all_full_life_enemies");

        /// <summary>
        ///     Whether <paramref name="type" /> is one of RitsuLib's custom target types.
        ///     判断 <paramref name="type" /> 是否属于 RitsuLib 定义的自定义目标类型。
        /// </summary>
        public static bool IsRitsuCustom(TargetType type)
        {
            return CustomTargetTypeRegistry.IsRitsuCustom(type);
        }

        /// <summary>
        ///     Whether <paramref name="type" /> is registered as a custom single-target type.
        ///     判断 <paramref name="type" /> 是否已注册为自定义单体目标类型。
        /// </summary>
        public static bool IsCustomSingleTargetType(TargetType type)
        {
            return CustomTargetTypeResolver.IsCustomSingleTargetType(type);
        }

        /// <summary>
        ///     Whether <paramref name="type" /> is registered as a custom multi-target type.
        ///     判断 <paramref name="type" /> 是否已注册为自定义群体目标类型。
        /// </summary>
        public static bool IsCustomMultiTargetType(TargetType type)
        {
            return CustomTargetTypeResolver.IsCustomMultiTargetType(type);
        }

        /// <summary>
        ///     Registers a mod-scoped single-target <see cref="TargetType" /> and returns the deterministic enum value.
        ///     The returned value is stable for the same <paramref name="modId" /> and <paramref name="localStem" />.
        ///     注册一个 mod 作用域的单体目标 <see cref="TargetType" />，并返回确定性的枚举值。
        ///     相同 <paramref name="modId" /> 与 <paramref name="localStem" /> 会得到稳定相同的返回值。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod id.
        ///     所属 mod ID。
        /// </param>
        /// <param name="localStem">
        ///     Stable local id stem, normalized into <c>MODID_TARGETTYPE_STEM</c>.
        ///     稳定的本地 ID 词干，会规范化为 <c>MODID_TARGETTYPE_STEM</c>。
        /// </param>
        /// <param name="canTarget">
        ///     Predicate used by mouse/controller targeting and card validation for candidate creatures.
        ///     鼠标 / 手柄选目标与卡牌目标校验使用的候选生物谓词。
        /// </param>
        public static TargetType RegisterSingleTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> canTarget)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(canTarget);

            var (_, id, type) = DynamicEnumValueRegistry<TargetType>.RegisterOwned(modId, localStem);
            CustomTargetTypeRegistry.RegisterSingleTargetType(type, id, canTarget);
            return type;
        }

        /// <summary>
        ///     Registers a mod-scoped multi-target <see cref="TargetType" /> and returns the deterministic enum value.
        ///     Cards using this target type play once with a null selected target; use
        ///     <c>CardModelTargetingExtensions.GetTargets(...)</c> to resolve the affected creatures in card logic.
        ///     注册一个 mod 作用域的群体目标 <see cref="TargetType" />，并返回确定性的枚举值。
        ///     使用此目标类型的卡牌会以 null 已选目标打出一次；卡牌逻辑中可用
        ///     <c>CardModelTargetingExtensions.GetTargets(...)</c> 解析实际影响的生物。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod id.
        ///     所属 mod ID。
        /// </param>
        /// <param name="localStem">
        ///     Stable local id stem, normalized into <c>MODID_TARGETTYPE_STEM</c>.
        ///     稳定的本地 ID 词干，会规范化为 <c>MODID_TARGETTYPE_STEM</c>。
        /// </param>
        /// <param name="includeTarget">
        ///     Predicate used for multi-target reticles and target resolution.
        ///     群体目标指示器与目标解析使用的谓词。
        /// </param>
        public static TargetType RegisterMultiTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> includeTarget)
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
