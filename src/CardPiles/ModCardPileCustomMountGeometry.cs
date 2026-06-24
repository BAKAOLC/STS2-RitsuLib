using Godot;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Shared math for interpreting <see cref="ModCardPileAnchorKind.Custom" /> authoring points versus
    ///     injected control positions and nominal fly-target centres.
    ///     解释 <see cref="ModCardPileAnchorKind.Custom" /> 创作点、注入 control 位置、
    ///     以及名义飞行目标中心之间关系的共享数学逻辑。
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
