using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     Relic-unlock epoch whose gated relic types are declared in the content pack via
    ///     <see cref="TimelineColumnPackEntry{TStory}" />.
    ///     遗物解锁纪元 其受门控的遗物类型通过内容包中的声明：
    ///     <see cref="TimelineColumnPackEntry{TStory}" />。
    /// </summary>
    public abstract class PackDeclaredRelicUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Relics resolved from <see cref="ModEpochGatedContentRegistry" /> for this epoch’s <see cref="EpochModel.Id" />.
        ///     解析出的遗物，来源： <see cref="ModEpochGatedContentRegistry" /> 用于此纪元的 <see cref="EpochModel.Id" />。
        /// </summary>
        public IReadOnlyList<RelicModel> Relics => ModEpochGatedContentRegistry.ResolveRelics(Id);

        /// <inheritdoc />
        public override string UnlockText => CreateRelicUnlockText(Relics.ToList());

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
            if (Relics.Count == 0)
                throw new InvalidOperationException(
                    $"Pack-declared relic epoch '{Id}' has no relics in {nameof(ModEpochGatedContentRegistry)}. " +
                    "Register gated relics for this epoch via TimelineColumnPackEntry (e.g. .Epoch<TEpoch>(e => e.RelicsFromPool<...>())) with a non-empty pool.");

            NTimelineScreen.Instance.QueueRelicUnlock(Relics.ToList());

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
