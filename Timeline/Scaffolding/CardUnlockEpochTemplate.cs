using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks a set of cards (from declared CLR types) and optional timeline
    ///     expansions.
    /// </summary>
    public abstract class CardUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="CardModel" /> instances for <see cref="CardTypes" />.
        /// </summary>
        public IReadOnlyList<CardModel> Cards => CardTypes
            .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
            .ToArray();

        /// <inheritdoc />
        public override string UnlockText => CreateCardUnlockText(Cards.ToList());

        /// <summary>
        ///     CLR types of cards to unlock; each must be registered in <see cref="ModelDb" />.
        /// </summary>
        protected abstract IEnumerable<Type> CardTypes { get; }

        /// <summary>
        ///     Additional epoch types to append to the timeline when this epoch unlocks; default none.
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <inheritdoc />
        public override EpochModel[] GetTimelineExpansion()
        {
            return ExpansionEpochTypes.Select(type => Get(GetId(type))).ToArray();
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
