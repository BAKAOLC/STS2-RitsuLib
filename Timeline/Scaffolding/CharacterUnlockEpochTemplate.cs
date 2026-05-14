using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks <typeparamref name="TCharacter" /> and optional follow-on epochs.
    /// </summary>
    /// <typeparam name="TCharacter">
    ///     Character model type being unlocked.
    ///     正在解锁的角色模型类型。
    /// </typeparam>
    public abstract class CharacterUnlockEpochTemplate<TCharacter> : ModEpochTemplate
        where TCharacter : CharacterModel
    {
        /// <inheritdoc />
        public override bool IsArtPlaceholder => false;

        /// <summary>
        ///     Additional epoch types to append when this unlock fires; default none.
        ///     要追加的额外纪元类型 当此解锁触发时; 默认为无。
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
            NTimelineScreen.Instance.QueueCharacterUnlock<TCharacter>(this);
            SaveManager.Instance.Progress.PendingCharacterUnlock = ModelDb.GetId<TCharacter>();

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
