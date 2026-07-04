using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Runtime context passed to optional mod card-pile view style providers.
    ///     传给可选 mod 牌堆查看界面样式 provider 的运行时上下文。
    /// </summary>
    public sealed record ModCardPileViewStyleContext(
        ModCardPileDefinition Definition,
        CardPile Pile,
        NCardPileScreen Screen);
}
