using Godot;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts custom card-pile authoring points into control positions and nominal flight centers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将自定义卡牌牌堆定位点转换为控件位置和名义飞行中心。
    ///     </para>
    /// </summary>
    internal static class ModCardPileCustomMountGeometry
    {
        // Matches NModCardPileButton DefaultButtonWidth/Height.
        internal static readonly Vector2 PileButtonChromeSize = new(80f, 80f);

        internal static Vector2 NominalChromeSize(ModCardPileUiStyle style)
        {
            return style switch
            {
                ModCardPileUiStyle.ExtraHand => NModExtraHand.DefaultChromeSize,
                _ => PileButtonChromeSize,
            };
        }

        internal static Vector2 ControlTopLeftFromAuthoring(ModCardPileAnchor anchor, ModCardPileUiStyle style)
        {
            var size = NominalChromeSize(style);
            var pivot = anchor.CustomAuthoringPivot;
            return anchor.CustomPosition + anchor.Offset
                   - new Vector2(size.X * pivot.X, size.Y * pivot.Y);
        }

        internal static Vector2 NominalCentreFromTopLeft(Vector2 chromeTopLeft, ModCardPileUiStyle style)
        {
            return chromeTopLeft + NominalChromeSize(style) * 0.5f;
        }
    }
}
