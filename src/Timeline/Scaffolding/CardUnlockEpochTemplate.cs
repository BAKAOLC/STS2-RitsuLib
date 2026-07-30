using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline.Scaffolding
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an <see cref="EpochModel" /> base that unlocks cards declared by CLR type and optionally expands
    ///         the timeline.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按 CLR 类型声明并解锁卡牌的 <see cref="EpochModel" /> 基类，也可选择扩展时间线。
    ///     </para>
    /// </summary>
    public abstract class CardUnlockEpochTemplate : ModEpochTemplate
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the <see cref="CardModel" /> instances resolved from <see cref="CardTypes" />.</para>
        ///     <para xml:lang="zh-CN">获取从 <see cref="CardTypes" /> 解析出的 <see cref="CardModel" /> 实例。</para>
        /// </summary>
        public IReadOnlyList<CardModel> Cards => RequireUnlockPresentationItems(
            CardTypes
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray(),
            nameof(CardTypes));

        /// <inheritdoc />
        public override string UnlockText => CreateCardUnlockText([.. Cards]);

        /// <summary>
        ///     <para xml:lang="en">Gets the CLR types of cards to unlock; each must be registered in <see cref="ModelDb" />.</para>
        ///     <para xml:lang="zh-CN">获取要解锁的卡牌 CLR 类型；每种类型都必须已注册到 <see cref="ModelDb" />。</para>
        /// </summary>
        protected abstract IEnumerable<Type> CardTypes { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets additional epoch types appended when this epoch unlocks.</para>
        ///     <para xml:lang="zh-CN">获取此纪元解锁时追加的其他纪元类型。</para>
        /// </summary>
        protected virtual IEnumerable<Type> ExpansionEpochTypes => [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enumerates <see cref="CardTypes" /> for batch
        ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> registration by a content pack.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         枚举 <see cref="CardTypes" />，供内容包批量调用
        ///         <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" /> 注册。
        ///     </para>
        /// </summary>
        public IEnumerable<Type> EnumerateUnlockCardTypes()
        {
            return CardTypes.ToArray();
        }

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
