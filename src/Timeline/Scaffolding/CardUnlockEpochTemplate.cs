using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks a set of cards (from declared CLR types) and optional timeline
    ///     expansions.
    ///     <see cref="EpochModel" /> 基类，用于解锁一组卡牌（来自声明的 CLR 类型）以及可选 timeline
    ///     扩展。
    /// </summary>
    public abstract class CardUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="CardModel" /> instances for <see cref="CardTypes" />.
        ///     解析出的 <see cref="CardModel" /> 实例，用于 <see cref="CardTypes" />。
        /// </summary>
        public IReadOnlyList<CardModel> Cards =>
        [
            .. CardTypes
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))),
        ];

        /// <inheritdoc />
        public override string UnlockText => CreateCardUnlockText([.. Cards]);

        /// <summary>
        ///     CLR types of cards to unlock; each must be registered in <see cref="ModelDb" />.
        ///     要解锁的卡牌 CLR 类型; 每个都必须注册到 <see cref="ModelDb" />。
        /// </summary>
        protected abstract IEnumerable<Type> CardTypes { get; }

        /// <summary>
        ///     Additional epoch types to append to the timeline when this epoch unlocks; default none.
        ///     此纪元解锁时要追加到时间线的额外纪元类型；默认为无。
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <summary>
        ///     Same as <see cref="CardTypes" /> for batch <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />
        ///     registration from a content-pack manifest.
        ///     同 <see cref="CardTypes" /> 用于批量 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />
        ///     从内容包 manifest 注册。
        /// </summary>
        public IEnumerable<Type> EnumerateUnlockCardTypes()
        {
            return CardTypes;
        }

        /// <inheritdoc />
        public override EpochModel[] GetTimelineExpansion()
        {
            return [.. ExpansionEpochTypes.Select(type => Get(GetId(type)))];
        }

        /// <inheritdoc />
        public override void QueueUnlocks()
        {
            NTimelineScreen.Instance.QueueCardUnlock(Cards);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
