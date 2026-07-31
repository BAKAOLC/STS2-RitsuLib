using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a relic-unlock epoch whose gated relic types are declared through
    ///         <see cref="TimelineColumnPackEntry{TStory}" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供通过 <see cref="TimelineColumnPackEntry{TStory}" /> 声明受限遗物类型的遗物解锁纪元。
    ///     </para>
    /// </summary>
    public abstract class PackDeclaredRelicUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the relics registered for this epoch's <see cref="EpochModel.Id" />.</para>
        ///     <para xml:lang="zh-CN">获取为此纪元的 <see cref="EpochModel.Id" /> 注册的遗物。</para>
        /// </summary>
        public IReadOnlyList<RelicModel> Relics => RequireUnlockPresentationItems(
            ModEpochGatedContentRegistry.ResolveRelics(Id),
            nameof(ModEpochGatedContentRegistry));

        /// <inheritdoc />
        public override string UnlockText => CreateRelicUnlockText([.. Relics]);

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
            var relics = Relics;
            NTimelineScreen.Instance.QueueRelicUnlock([.. relics]);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
