#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     <para xml:lang="en">Provides context for modifying an amount of healing received by a creature.</para>
    ///     <para xml:lang="zh-CN">提供修正生物所受治疗量的上下文。</para>
    /// </summary>
    public sealed class HealContext
    {
        internal HealContext(Creature creature, decimal originalAmount, bool playAnim)
        {
            Creature = creature;
            OriginalAmount = originalAmount;
            PlayAnim = playAnim;
            CombatState = creature.CombatState;
            RunState = creature.Player?.RunState ?? creature.CombatState?.RunState;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the creature receiving healing.</para>
        ///     <para xml:lang="zh-CN">获取接受治疗的生物。</para>
        /// </summary>
        public Creature Creature { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount passed to <c>CreatureCmd.Heal</c> before RitsuLib listeners modify it.</para>
        ///     <para xml:lang="zh-CN">获取 RitsuLib 监听器修正前传给 <c>CreatureCmd.Heal</c> 的数值。</para>
        /// </summary>
        public decimal OriginalAmount { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this heal command requests the vanilla healing animation.</para>
        ///     <para xml:lang="zh-CN">获取此治疗命令是否请求播放原版治疗动画。</para>
        /// </summary>
        public bool PlayAnim { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the combat containing the creature, or <see langword="null" /> outside combat.</para>
        ///     <para xml:lang="zh-CN">获取生物所在的战斗；非战斗治疗时为 <see langword="null" />。</para>
        /// </summary>
        public CombatStateLike? CombatState { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the run associated with this healing, if available.</para>
        ///     <para xml:lang="zh-CN">获取与此次治疗关联的局内状态（如果有）。</para>
        /// </summary>
        public IRunState? RunState { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount of HP the creature can currently recover.</para>
        ///     <para xml:lang="zh-CN">获取该生物当前可恢复的生命值。</para>
        /// </summary>
        public decimal MissingHp => Math.Max(0m, Creature.MaxHp - Creature.CurrentHp);
    }
}
