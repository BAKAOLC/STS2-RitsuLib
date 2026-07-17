using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Resolved presentation transform for one card in an extra-hand pile.
    ///     额外手牌牌堆中单张卡牌的最终展示变换。
    /// </summary>
    public readonly record struct ModExtraHandCardTransform(
        Vector2 Position,
        Vector2 Scale,
        float RotationDegrees = 0f,
        int ZIndex = 0);
}
