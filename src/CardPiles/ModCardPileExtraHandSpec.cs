using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Presentation and interaction options for <see cref="ModCardPileUiStyle.ExtraHand" />.
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> 的展示与交互选项。
    /// </summary>
    public sealed record ModCardPileExtraHandSpec
    {
        /// <summary>
        ///     Built-in arrangement direction. Defaults to the vanilla hand layout.
        ///     内置排列方向。默认为原版手牌布局。
        /// </summary>
        public ModExtraHandLayoutDirection Direction { get; init; } = ModExtraHandLayoutDirection.VanillaHand;

        /// <summary>
        ///     Distance in pixels between adjacent holder origins for horizontal and vertical layouts.
        ///     Defaults to <c>110</c> and is ignored by the vanilla hand layout.
        ///     水平与垂直布局中相邻 holder 原点之间的像素距离。默认为 <c>110</c>，原版手牌布局会忽略此值。
        /// </summary>
        public float Spacing { get; init; } = 110f;

        /// <summary>
        ///     Normal card scale for horizontal and vertical layouts. Defaults to <c>0.65</c> on both axes
        ///     and is ignored by the vanilla hand layout.
        ///     水平与垂直布局中的卡牌常态缩放。两轴默认为 <c>0.65</c>，原版手牌布局会忽略此值。
        /// </summary>
        public Vector2 CardScale { get; init; } = Vector2.One * 0.65f;

        /// <summary>
        ///     Focused/hovered card scale for horizontal and vertical layouts. Defaults to full size and is
        ///     ignored by the vanilla hand layout.
        ///     水平与垂直布局中获得焦点或悬停时的卡牌缩放。默认为完整尺寸，原版手牌布局会忽略此值。
        /// </summary>
        public Vector2 HoverScale { get; init; } = Vector2.One;

        /// <summary>
        ///     Whether cards use the vanilla hand-card playable glow rules. Defaults to true.
        ///     卡牌是否使用原版手牌的可打出发光规则。默认为 true。
        /// </summary>
        public bool ShowPlayableGlow { get; init; } = true;

        /// <summary>
        ///     Whether cards can be manually played through the vanilla targeting, queue, resource-spend,
        ///     hook, and result-pile pipeline. Defaults to true.
        ///     卡牌是否可通过原版目标选择、播放队列、资源支付、hook 与结果牌堆流程手动打出。默认为 true。
        /// </summary>
        public bool AllowCardPlay { get; init; } = true;

        /// <summary>
        ///     Optional per-card layout resolver. Return null to keep the built-in transform.
        ///     可选的逐卡布局 resolver。返回 null 时保留内置变换。
        /// </summary>
        public Func<ModExtraHandCardContext, ModExtraHandCardTransform?>? LayoutResolver { get; init; }

        /// <summary>
        ///     Optional callback invoked after an interactive card visual is mounted.
        ///     交互式卡牌视觉挂载后调用的可选回调。
        /// </summary>
        public Action<ModExtraHandCardContext>? OnCardVisualCreated { get; init; }

        /// <summary>
        ///     Optional callback invoked when the corresponding vanilla <c>NCardFlyVfx</c> finishes. Adds that
        ///     skip visuals or use aggregate shuffle visuals do not invoke this callback.
        ///     对应的原版 <c>NCardFlyVfx</c> 完成时调用的可选回调。跳过视觉或使用聚合 shuffle 视觉的
        ///     加入操作不会调用此回调。
        /// </summary>
        public Action<ModExtraHandCardContext>? OnCardArrived { get; init; }
    }
}
