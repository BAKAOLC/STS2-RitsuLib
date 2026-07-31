using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an <see cref="EpochModel" /> base that unlocks relics declared by CLR type and optionally expands
    ///         the timeline.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按 CLR 类型声明并解锁遗物的 <see cref="EpochModel" /> 基类，也可选择扩展时间线。
    ///     </para>
    /// </summary>
    public abstract class RelicUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="RelicModel" /> instances resolved from <see cref="RelicTypes" />.</para>
        ///     <para xml:lang="zh-CN">获取从 <see cref="RelicTypes" /> 解析出的 <see cref="RelicModel" /> 实例。</para>
        /// </summary>
        public IReadOnlyList<RelicModel> Relics => RequireUnlockPresentationItems(
            RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray(),
            nameof(RelicTypes));

        /// <inheritdoc />
        public override string UnlockText => CreateRelicUnlockText([.. Relics]);

        /// <summary>
        ///     <para xml:lang="en">Gets the CLR types of relics to unlock; each must be registered in <see cref="ModelDb" />.</para>
        ///     <para xml:lang="zh-CN">获取要解锁的遗物 CLR 类型；每种类型都必须已注册到 <see cref="ModelDb" />。</para>
        /// </summary>
        protected abstract IEnumerable<Type> RelicTypes { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets additional epoch types appended when this epoch unlocks.</para>
        ///     <para xml:lang="zh-CN">获取此纪元解锁时追加的其他纪元类型。</para>
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enumerates <see cref="RelicTypes" /> for batch
        ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> registration by a content pack.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         枚举 <see cref="RelicTypes" />，供内容包批量调用
        ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> 注册。
        ///     </para>
        /// </summary>
        public IEnumerable<Type> EnumerateUnlockRelicTypes()
        {
            return RelicTypes.ToArray();
        }

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
