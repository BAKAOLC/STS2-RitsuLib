using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     <para xml:lang="en">Provides the candidate, player, and source model to a custom target predicate.</para>
    ///     <para xml:lang="zh-CN">向自定义目标谓词提供候选目标、玩家和来源模型。</para>
    /// </summary>
    public sealed class CustomTargetContext
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a custom target context.</para>
        ///     <para xml:lang="zh-CN">初始化自定义目标上下文。</para>
        /// </summary>
        /// <param name="targetCreature">
        ///     <para xml:lang="en">The candidate creature being evaluated.</para>
        ///     <para xml:lang="zh-CN">正在判定的候选生物。</para>
        /// </param>
        /// <param name="player">
        ///     <para xml:lang="en">The player using the card or potion.</para>
        ///     <para xml:lang="zh-CN">使用卡牌或药水的玩家。</para>
        /// </param>
        /// <param name="card">
        ///     <para xml:lang="en">The source card, or <see langword="null" /> when the source is not a card.</para>
        ///     <para xml:lang="zh-CN">来源卡牌；来源不是卡牌时为 <see langword="null" />。</para>
        /// </param>
        /// <param name="potion">
        ///     <para xml:lang="en">The source potion, or <see langword="null" /> when the source is not a potion.</para>
        ///     <para xml:lang="zh-CN">来源药水；来源不是药水时为 <see langword="null" />。</para>
        /// </param>
        public CustomTargetContext(
            Creature targetCreature,
            Player player,
            CardModel? card = null,
            PotionModel? potion = null)
        {
            ArgumentNullException.ThrowIfNull(targetCreature);
            ArgumentNullException.ThrowIfNull(player);

            TargetCreature = targetCreature;
            Player = player;
            Card = card;
            Potion = potion;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the candidate creature being evaluated.</para>
        ///     <para xml:lang="zh-CN">获取正在判定的候选生物。</para>
        /// </summary>
        public Creature TargetCreature { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the player using the card or potion.</para>
        ///     <para xml:lang="zh-CN">获取使用卡牌或药水的玩家。</para>
        /// </summary>
        public Player Player { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source card, if any.</para>
        ///     <para xml:lang="zh-CN">获取来源卡牌（如有）。</para>
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source potion, if any.</para>
        ///     <para xml:lang="zh-CN">获取来源药水（如有）。</para>
        /// </summary>
        public PotionModel? Potion { get; }

        internal static CustomTargetContext ForCard(Creature targetCreature, CardModel card)
        {
            return new(targetCreature, card.Owner, card);
        }

        internal static CustomTargetContext ForPotion(Creature targetCreature, PotionModel potion)
        {
            return new(targetCreature, potion.Owner, null, potion);
        }
    }
}
