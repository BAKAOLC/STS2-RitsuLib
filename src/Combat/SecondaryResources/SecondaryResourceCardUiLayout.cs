using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Layout coordination helpers for secondary-resource card UI attached to <see cref="NCard" />.
    ///     附加到 <see cref="NCard" /> 的次级资源卡牌 UI 布局协调辅助工具。
    /// </summary>
    public static class SecondaryResourceCardUiLayout
    {
        private const float VanillaStarCostEnchantmentOffset = 45f;

        private static readonly AttachedState<NCard, bool> ReservedVanillaStarCostSlot = new();

        /// <summary>
        ///     Marks the current card UI refresh as occupying the vanilla star-cost slot.
        ///     将当前卡牌 UI 刷新标记为占用了原版辉星费用槽。
        /// </summary>
        /// <param name="card">
        ///     Card node being refreshed.
        ///     正在刷新的卡牌节点。
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
