using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Describes the resolved presentation transform of one extra-hand card.</para>
    ///     <para xml:lang="zh-CN">描述一张额外手牌卡牌最终采用的展示变换。</para>
    /// </summary>
    /// <param name="Position">
    ///     <para xml:lang="en">The card-holder position in the extra-hand control.</para>
    ///     <para xml:lang="zh-CN">卡牌容器在额外手牌控件中的位置。</para>
    /// </param>
    /// <param name="Scale">
    ///     <para xml:lang="en">The card-holder scale.</para>
    ///     <para xml:lang="zh-CN">卡牌容器的缩放比例。</para>
    /// </param>
    /// <param name="RotationDegrees">
    ///     <para xml:lang="en">The clockwise rotation in degrees.</para>
    ///     <para xml:lang="zh-CN">以度为单位的顺时针旋转角度。</para>
    /// </param>
    /// <param name="ZIndex">
    ///     <para xml:lang="en">The canvas draw order assigned to the card holder.</para>
    ///     <para xml:lang="zh-CN">分配给卡牌容器的画布绘制顺序。</para>
    /// </param>
    public readonly record struct ModExtraHandCardTransform(
        Vector2 Position,
        Vector2 Scale,
        float RotationDegrees = 0f,
        int ZIndex = 0);
}
