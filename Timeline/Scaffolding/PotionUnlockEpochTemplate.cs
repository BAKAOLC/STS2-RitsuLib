using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks potions from declared CLR types and optional timeline expansions.
    /// </summary>
    public abstract class PotionUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="PotionModel" /> instances for <see cref="PotionTypes" />.
        /// </summary>
        public IReadOnlyList<PotionModel> Potions => PotionTypes
            .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))
            .ToArray();

        /// <inheritdoc />
        public override string UnlockText => CreatePotionUnlockText(Potions.ToList());

        /// <summary>
        ///     CLR types of potions to unlock; each must be registered in <see cref="ModelDb" />.
        /// </summary>
        protected abstract IEnumerable<Type> PotionTypes { get; }

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
            NTimelineScreen.Instance.QueuePotionUnlock(Potions.ToList());

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
