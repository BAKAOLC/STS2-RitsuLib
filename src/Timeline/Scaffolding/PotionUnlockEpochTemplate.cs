using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an <see cref="EpochModel" /> base that unlocks potions declared by CLR type and optionally expands
    ///         the timeline.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按 CLR 类型声明并解锁药水的 <see cref="EpochModel" /> 基类，也可选择扩展时间线。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Potion-pool visibility still requires <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> for
    ///         each potion type. A content pack can add these requirements through
    ///         <see cref="TimelineColumnPackEntry{TStory}" /> callbacks or equivalent
    ///         <see cref="ModContentPackBuilder" /> steps.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         药水池可见性仍要求为每种药水类型调用
    ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />。内容包可通过
    ///         <see cref="TimelineColumnPackEntry{TStory}" /> 回调或等效的 <see cref="ModContentPackBuilder" /> 步骤添加要求。
    ///     </para>
    /// </remarks>
    public abstract class PotionUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the <see cref="PotionModel" /> instances resolved from <see cref="PotionTypes" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取从 <see cref="PotionTypes" /> 解析出的 <see cref="PotionModel" /> 实例。</para>
        /// </summary>
        public IReadOnlyList<PotionModel> Potions => RequireUnlockPresentationItems(
            [.. PotionTypes.Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))],
            nameof(PotionTypes));

        /// <inheritdoc />
        public override string UnlockText => CreatePotionUnlockText([.. Potions]);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the CLR types of potions to unlock; each must be registered in <see cref="ModelDb" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取要解锁的药水 CLR 类型；每种类型都必须已注册到 <see cref="ModelDb" />。</para>
        /// </summary>
        protected abstract IEnumerable<Type> PotionTypes { get; }

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
            var potions = Potions;
            NTimelineScreen.Instance.QueuePotionUnlock([.. potions]);

            var expansion = GetTimelineExpansion();
            if (expansion.Length > 0)
                QueueTimelineExpansion(expansion);
        }
    }
}
