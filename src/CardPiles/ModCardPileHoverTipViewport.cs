using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides viewport containment for manually positioned mod card-pile hover tips.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为手动定位的模组卡牌牌堆悬停提示提供视口边界约束。
    ///     </para>
    /// </summary>
    public static class ModCardPileHoverTipViewport
    {
        private const float Margin = 8f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clamps a hover tip's global top-left position to the game viewport with an eight-pixel margin.
        ///         If the tip is larger than an axis of the viewport, it is centered on that axis. An invalid tip,
        ///         missing game instance, or unresolved tip size leaves the requested position unchanged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将悬停提示的全局左上角位置限制在游戏视口内，并保留八像素边距。提示在某一轴上大于视口时，
        ///         会在该轴上居中。提示无效、游戏实例不存在或无法确定提示尺寸时，保持请求位置不变。
        ///     </para>
        /// </summary>
        /// <param name="tipSet">
        ///     <para xml:lang="en">The hover-tip control whose outer size defines the constrained rectangle.</para>
        ///     <para xml:lang="zh-CN">外部尺寸用于确定约束矩形的悬停提示控件。</para>
        /// </param>
        /// <param name="globalTopLeft">
        ///     <para xml:lang="en">The requested global top-left position.</para>
        ///     <para xml:lang="zh-CN">请求的全局左上角位置。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The constrained position, or the original position when it cannot be resolved.</para>
        ///     <para xml:lang="zh-CN">约束后的位置；无法解析时为原始位置。</para>
        /// </returns>
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
