using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.CardTargeting
{
    /// <summary>
    ///     Source-aware context passed to custom single-target predicates.
    ///     传递给自定义单体目标谓词的来源上下文。
    /// </summary>
    public sealed class CustomTargetContext
    {
        /// <summary>
        ///     Creates a source-aware custom targeting context.
        ///     创建一个感知来源的自定义目标上下文。
        /// </summary>
        /// <param name="targetCreature">
        ///     Candidate creature being evaluated.
        ///     正在判定的候选生物。
        /// </param>
        /// <param name="player">
        ///     Player using the card or potion.
        ///     使用卡牌或药水的玩家。
        /// </param>
        /// <param name="card">
        ///     Source card when targeting was started by a card, otherwise null.
        ///     由卡牌发起选目标时的来源卡牌；否则为 null。
        /// </param>
        /// <param name="potion">
        ///     Source potion when targeting was started by a potion, otherwise null.
        ///     由药水发起选目标时的来源药水；否则为 null。
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
        ///     Candidate creature being evaluated.
        ///     正在判定的候选生物。
        /// </summary>
        public Creature TargetCreature { get; }

        /// <summary>
        ///     Player using the card or potion.
        ///     使用卡牌或药水的玩家。
        /// </summary>
        public Player Player { get; }

        /// <summary>
        ///     Source card when targeting was started by a card, otherwise null.
        ///     由卡牌发起选目标时的来源卡牌；否则为 null。
        /// </summary>
        public CardModel? Card { get; }

        /// <summary>
        ///     Source potion when targeting was started by a potion, otherwise null.
        ///     由药水发起选目标时的来源药水；否则为 null。
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
