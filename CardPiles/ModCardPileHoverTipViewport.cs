using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Clamps an <see cref="NHoverTipSet" /> top-left into the visible viewport. Vanilla pile tips that use
    ///     <c>HoverTipAlignment.None</c> set <see cref="Control.GlobalPosition" /> manually and never run
    ///     <c>NHoverTipSet.SetAlignment</c>, so they do not get <c>CorrectVerticalOverflow</c> /
    ///     <c>CorrectHorizontalOverflow</c>; this helper reproduces that containment for mod pile buttons.
    /// </summary>
    public static class ModCardPileHoverTipViewport
    {
        private const float Margin = 8f;

        /// <summary>
        ///     Returns <paramref name="globalTopLeft" /> nudged so the tip rect stays inside
        ///     <see cref="NGame.Instance" />'s viewport with a small margin.
        /// </summary>
        public static Vector2 ClampTipTopLeft(NHoverTipSet tipSet, Vector2 globalTopLeft)
        {
            if (tipSet == null || !GodotObject.IsInstanceValid(tipSet))
                return globalTopLeft;

            var game = NGame.Instance;
            if (game == null)
                return globalTopLeft;

            var tipSize = ResolveTipOuterSize(tipSet);
            if (tipSize.X < 1f || tipSize.Y < 1f)
                return globalTopLeft;

            var vp = game.GetViewportRect();
            var maxX = vp.Size.X - tipSize.X - Margin;
            var maxY = vp.Size.Y - tipSize.Y - Margin;
            var minX = Margin;
            var minY = Margin;

            if (maxX < minX)
            {
                var cx = (vp.Size.X - tipSize.X) * 0.5f;
                minX = maxX = cx;
            }

            if (!(maxY < minY))
                return new(
                    Mathf.Clamp(globalTopLeft.X, minX, maxX),
                    Mathf.Clamp(globalTopLeft.Y, minY, maxY));
            var cy = (vp.Size.Y - tipSize.Y) * 0.5f;
            minY = maxY = cy;

            return new(
                Mathf.Clamp(globalTopLeft.X, minX, maxX),
                Mathf.Clamp(globalTopLeft.Y, minY, maxY));
        }

        private static Vector2 ResolveTipOuterSize(NHoverTipSet tipSet)
        {
            var s = tipSet.Size;
            if (s is { X: >= 1f, Y: >= 1f })
                return s;
            var combined = tipSet.GetCombinedMinimumSize();
            return combined is { X: >= 1f, Y: >= 1f } ? combined : s;
        }
    }
}
