using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <see cref="EpochModel" /> base that unlocks relics from declared CLR types and optional timeline expansions.
    ///     <see cref="EpochModel" /> 基类：从声明的 CLR 类型解锁遗物，并可选扩展时间线。
    /// </summary>
    public abstract class RelicUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     Resolved <see cref="RelicModel" /> instances for <see cref="RelicTypes" />.
        ///     解析出的 <see cref="RelicModel" /> 实例，用于 <see cref="RelicTypes" />。
        /// </summary>
        public IReadOnlyList<RelicModel> Relics =>
        [
            .. RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type))),
        ];

        /// <inheritdoc />
        public override string UnlockText => CreateRelicUnlockText([.. Relics]);

        /// <summary>
        ///     CLR types of relics to unlock; each must be registered in <see cref="ModelDb" />.
        ///     要解锁的遗物 CLR 类型; 每个都必须注册到 <see cref="ModelDb" />。
        /// </summary>
        protected abstract IEnumerable<Type> RelicTypes { get; }

        /// <summary>
        ///     Additional epoch types to append when this epoch unlocks; default none.
        ///     要追加的额外纪元类型 当此纪元解锁时; 默认为无。
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <summary>
        ///     Same as <see cref="RelicTypes" /> for batch <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />
        ///     registration from a content-pack manifest.
        ///     同 <see cref="RelicTypes" /> 用于批量 <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />
        ///     从内容包 manifest 注册。
        /// </summary>
        public IEnumerable<Type> EnumerateUnlockRelicTypes()
        {
            return RelicTypes;
        }

        /// <inheritdoc />
        public override EpochModel[] GetTimelineExpansion()
        {
            return [.. ExpansionEpochTypes.Select(type => Get(GetId(type)))];
        }

        /// <inheritdoc />
        public override void QueueUnlocks()
        {
            NTimelineScreen.Instance.QueueRelicUnlock([.. Relics]);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
