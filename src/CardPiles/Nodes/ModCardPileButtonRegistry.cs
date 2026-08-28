using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     <para xml:lang="en">Tracks the active UI node for each registered mod card pile.</para>
    ///     <para xml:lang="zh-CN">跟踪各已注册模组卡牌牌堆的当前界面节点。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         A newly mounted node replaces the previous entry. Unregistration removes an entry only when it
    ///         still refers to the node being removed.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         新挂载的节点会替换旧条目。注销时仅在条目仍指向正在移除的节点时才将其删除。
    ///     </para>
    /// </remarks>
    internal static class ModCardPileButtonRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, NModCardPileButton> Buttons = [];
        private static readonly Dictionary<string, NModExtraHand> ExtraHands = [];

        internal static void RegisterButton(ModCardPileDefinition definition, NModCardPileButton button)
        {
            lock (SyncRoot)
            {
                Buttons[definition.Id] = button;
            }
        }

        internal static void UnregisterButton(ModCardPileDefinition definition, NModCardPileButton button)
        {
            lock (SyncRoot)
            {
                if (Buttons.TryGetValue(definition.Id, out var existing) && ReferenceEquals(existing, button))
                    Buttons.Remove(definition.Id);
            }
        }

        internal static NModCardPileButton? TryGetButton(ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return Buttons.GetValueOrDefault(definition.Id);
            }
        }

        internal static void RegisterExtraHand(ModCardPileDefinition definition, NModExtraHand hand)
        {
            lock (SyncRoot)
            {
                ExtraHands[definition.Id] = hand;
            }
        }

        internal static void UnregisterExtraHand(ModCardPileDefinition definition, NModExtraHand hand)
        {
            lock (SyncRoot)
            {
                if (ExtraHands.TryGetValue(definition.Id, out var existing) && ReferenceEquals(existing, hand))
                    ExtraHands.Remove(definition.Id);
            }
        }

        internal static NModExtraHand? TryGetExtraHand(ModCardPileDefinition definition)
        {
            lock (SyncRoot)
            {
                return ExtraHands.GetValueOrDefault(definition.Id);
            }
        }

        internal static NModExtraHand[] GetExtraHands()
        {
            lock (SyncRoot)
            {
                return [.. ExtraHands.Values];
            }
        }

        internal static NModExtraHand? TryGetExtraHand(CardPile pile)
        {
            if (!ModCardPileRegistry.TryGetByPileType(pile.Type, out var definition))
                return null;

            var hand = TryGetExtraHand(definition);
            return hand?.RepresentsPile(pile) == true ? hand : null;
        }

        internal static NModExtraHand? TryGetExtraHandContaining(NCard cardNode)
        {
            if (cardNode.Model is not { } card)
                return null;

            NModExtraHand[] hands;
            lock (SyncRoot)
            {
                hands = [.. ExtraHands.Values];
            }

            return hands.FirstOrDefault(hand => ReferenceEquals(hand.GetCard(card), cardNode));
        }
    }
}
