using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Card-unlock epoch whose gated card types are declared in the content pack via
    ///     <see cref="TimelineColumnPackEntry{TStory}" /> (not on the epoch subclass). Keeps <see cref="QueueUnlocks" />,
    ///     <see cref="EpochModel.UnlockText" />, and <see cref="Unlocks.ModUnlockRegistry" /> in sync from one manifest
    ///     registration.
    ///     卡牌解锁纪元，其受门控的卡牌类型通过内容包中的
    ///     <see cref="TimelineColumnPackEntry{TStory}" /> 声明（而不是在纪元子类上声明）。它让 <see cref="QueueUnlocks" />、
    ///     <see cref="EpochModel.UnlockText" /> 和 <see cref="Unlocks.ModUnlockRegistry" /> 从同一份 manifest
    ///     注册保持同步。
    /// </summary>
    public abstract class PackDeclaredCardUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Cards resolved from <see cref="ModEpochGatedContentRegistry" /> for this epoch’s <see cref="EpochModel.Id" />.
        ///     解析出的卡牌，来源： <see cref="ModEpochGatedContentRegistry" /> 用于此纪元的 <see cref="EpochModel.Id" />。
        /// </summary>
        public IReadOnlyList<CardModel> Cards => ModEpochGatedContentRegistry.ResolveCards(Id);

        /// <inheritdoc />
        public override string UnlockText => CreateCardUnlockText(Cards.ToList());

        /// <summary>
        ///     Additional epoch types to append when this epoch unlocks.
        ///     要追加的额外纪元类型 当此纪元解锁时。
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
            if (Cards.Count == 0)
                throw new InvalidOperationException(
                    $"Pack-declared card epoch '{Id}' has no cards in {nameof(ModEpochGatedContentRegistry)}. " +
                    "Register gated cards for this epoch via TimelineColumnPackEntry (e.g. .Epoch<TEpoch>(e => e.Cards(...))) with a non-empty list.");

            NTimelineScreen.Instance.QueueCardUnlock(Cards);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
