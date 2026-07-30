using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates secondary-resource card-cost layout with the game's <see cref="NCard" /> layout.
    ///     </para>
    ///     <para xml:lang="zh-CN">协调次级资源卡牌费用与游戏 <see cref="NCard" /> 的布局。</para>
    /// </summary>
    public static class SecondaryResourceCardUiLayout
    {
        private const float VanillaStarCostEnchantmentOffset = 45f;

        private static readonly AttachedState<NCard, bool> ReservedVanillaStarCostSlot = new();

        /// <summary>
        ///     <para xml:lang="en">Marks the current card UI refresh as occupying the game's Stars-cost slot.</para>
        ///     <para xml:lang="zh-CN">标记当前卡牌界面刷新占用了游戏的辉星费用槽。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">The card node currently being refreshed.</para>
        ///     <para xml:lang="zh-CN">当前正在刷新的卡牌节点。</para>
        /// </param>
        public static void ReserveVanillaStarCostSlot(NCard card)
        {
            ArgumentNullException.ThrowIfNull(card);
            ReservedVanillaStarCostSlot.Set(card, true);
        }

        internal static void BeginUpdate(NCard card)
        {
            ArgumentNullException.ThrowIfNull(card);
            ReservedVanillaStarCostSlot.Remove(card);
        }

        internal static void ApplyReservedLayout(NCard card)
        {
            ArgumentNullException.ThrowIfNull(card);
            if (!ReservedVanillaStarCostSlot.TryRemove(card, out var reserved) ||
                !reserved ||
                card.Model == null ||
                !card.EnchantmentTab.Visible ||
                card.Model.HasStarCostX ||
                card.Model.CurrentStarCost >= 0)
                return;

            card.EnchantmentTab.Position += Vector2.Down * VanillaStarCostEnchantmentOffset;
        }
    }
}
