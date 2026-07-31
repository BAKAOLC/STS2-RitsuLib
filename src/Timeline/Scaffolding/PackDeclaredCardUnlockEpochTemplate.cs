using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a card-unlock epoch whose gated card types are declared through
    ///         <see cref="TimelineColumnPackEntry{TStory}" /> rather than on the epoch subclass. One content-pack
    ///         registration supplies its unlock action, unlock text, and epoch requirements.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供通过 <see cref="TimelineColumnPackEntry{TStory}" /> 而非纪元子类声明受限卡牌类型的卡牌解锁纪元。
    ///         一项内容包注册即可同时提供解锁操作、解锁文本和纪元要求。
    ///     </para>
    /// </summary>
    public abstract class PackDeclaredCardUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the cards registered for this epoch's <see cref="EpochModel.Id" />.</para>
        ///     <para xml:lang="zh-CN">获取为此纪元的 <see cref="EpochModel.Id" /> 注册的卡牌。</para>
        /// </summary>
        public IReadOnlyList<CardModel> Cards => RequireUnlockPresentationItems(
            ModEpochGatedContentRegistry.ResolveCards(Id),
            nameof(ModEpochGatedContentRegistry));

        /// <inheritdoc />
        public override string UnlockText => CreateCardUnlockText([.. Cards]);

        /// <summary>
        ///     <para xml:lang="en">Gets additional epoch types appended when this epoch unlocks.</para>
        ///     <para xml:lang="zh-CN">获取此纪元解锁时追加的其他纪元类型。</para>
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <inheritdoc />
        public override EpochModel[] GetTimelineExpansion()
        {
            return [.. ExpansionEpochTypes.Select(type => Get(GetId(type)))];
        }

        /// <inheritdoc />
        public override void QueueUnlocks()
        {
            var cards = Cards;
            NTimelineScreen.Instance.QueueCardUnlock(cards);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
