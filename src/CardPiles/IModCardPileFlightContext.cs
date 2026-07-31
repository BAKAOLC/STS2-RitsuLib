using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Exposes data shared by mod card-pile flight-position requests.</para>
    ///     <para xml:lang="zh-CN">公开模组卡牌牌堆飞行位置请求共用的数据。</para>
    /// </summary>
    public interface IModCardPileFlightContext
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the definition associated with the request.</para>
        ///     <para xml:lang="zh-CN">获取与请求关联的牌堆定义。</para>
        /// </summary>
        ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the default position computed by RitsuLib.</para>
        ///     <para xml:lang="zh-CN">获取 RitsuLib 计算的默认位置。</para>
        /// </summary>
        Vector2 DefaultPosition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source pile when the request represents a pile-to-pile flight.</para>
        ///     <para xml:lang="zh-CN">请求表示牌堆间飞行时，获取源牌堆。</para>
        /// </summary>
        CardPile? StartPile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the destination pile when the request represents a pile-to-pile flight.</para>
        ///     <para xml:lang="zh-CN">请求表示牌堆间飞行时，获取目标牌堆。</para>
        /// </summary>
        CardPile? TargetPile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card node involved in the request, when available.</para>
        ///     <para xml:lang="zh-CN">获取请求涉及的卡牌节点（如有）。</para>
        /// </summary>
        NCard? CardNode { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card model represented by <see cref="CardNode" />, when available.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="CardNode" /> 表示的卡牌模型（如有）。</para>
        /// </summary>
        CardModel? CardModel { get; }
    }
}
