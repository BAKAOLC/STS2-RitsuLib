using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks potions from declared CLR types and optional timeline expansions.
    /// </summary>
    /// <remarks>
    ///     Pool visibility still depends on <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> for each
    ///     potion type. Register those bindings from a pack using <see cref="TimelineColumnPackEntry{TStory}" /> with
    ///     <c>EpochSlotBuilder&lt;TEpoch&gt;</c> callbacks (<c>RequireAllPotionsInPool&lt;TPool&gt;()</c>,
    ///     <c>Potions(IReadOnlyList&lt;Type&gt;)</c>), or enqueue equivalent <c>RequireEpoch</c> steps from
    ///     <see cref="ModContentPackBuilder" />.
    ///     池可见性仍取决于每种
    ///     药水类型的 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />。使用
    ///     <see cref="TimelineColumnPackEntry{TStory}" /> 搭配
    ///     <c>EpochSlotBuilder&lt;TEpoch&gt;</c> 回调（<c>RequireAllPotionsInPool&lt;TPool&gt;()</c>、
    ///     <c>Potions(IReadOnlyList&lt;Type&gt;)</c>），或从 <see cref="ModContentPackBuilder" /> 入队等效的 <c>RequireEpoch</c> 步骤。
    /// </remarks>
    public abstract class PotionUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="PotionModel" /> instances for <see cref="PotionTypes" />.
        ///     解析出的 <see cref="PotionModel" /> 实例，用于 <see cref="PotionTypes" />。
        /// </summary>
        public IReadOnlyList<PotionModel> Potions =>
        [
            .. PotionTypes
                .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type))),
        ];

        /// <inheritdoc />
        public override string UnlockText => CreatePotionUnlockText([.. Potions]);

        /// <summary>
        ///     CLR types of potions to unlock; each must be registered in <see cref="ModelDb" />.
        ///     要解锁的药水 CLR 类型; 每个都必须注册到 <see cref="ModelDb" />。
        /// </summary>
        protected abstract IEnumerable<Type> PotionTypes { get; }

        /// <summary>
        ///     Additional epoch types to append when this epoch unlocks; default none.
        ///     要追加的额外纪元类型 当此纪元解锁时; 默认为无。
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
            NTimelineScreen.Instance.QueuePotionUnlock([.. Potions]);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
