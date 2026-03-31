using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks relics from declared CLR types and optional timeline expansions.
    /// </summary>
    public abstract class RelicUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="RelicModel" /> instances for <see cref="RelicTypes" />.
        /// </summary>
        public IReadOnlyList<RelicModel> Relics => RelicTypes
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();

        /// <inheritdoc />
        public override string UnlockText => CreateRelicUnlockText(Relics.ToList());

        /// <summary>
        ///     CLR types of relics to unlock; each must be registered in <see cref="ModelDb" />.
        /// </summary>
        protected abstract IEnumerable<Type> RelicTypes { get; }

        /// <summary>
        ///     Additional epoch types to append when this epoch unlocks; default none.
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
            NTimelineScreen.Instance.QueueRelicUnlock(Relics.ToList());

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
