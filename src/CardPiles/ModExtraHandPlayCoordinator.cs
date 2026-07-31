using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates playable extra-hand cards with the vanilla hand-based manual-play flow.
    ///     </para>
    ///     <para xml:lang="zh-CN">协调可打出的额外手牌卡牌与原版基于手牌的手动打牌流程。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         A card is temporarily moved to the player's hand while targeting is active. Canceling targeting
    ///         or the queued action restores the card to its original pile and position.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         目标选择期间会将卡牌暂时移入玩家手牌。取消目标选择或已排队动作时，会将卡牌恢复到原牌堆及原位置。
    ///     </para>
    /// </remarks>
    internal static class ModExtraHandPlayCoordinator
    {
        private static readonly Dictionary<CardModel, PlayOrigin> PendingOrigins = [];
        private static PlayOrigin? _active;

        internal static bool IsPlaying => _active != null;

        internal static bool IsActiveHolder(NHandCardHolder? holder)
        {
            return holder != null && ReferenceEquals(_active?.Holder, holder);
        }

        internal static bool TryBegin(NModExtraHand container, NHandCardHolder holder)
        {
            if (_active != null || holder.CardModel is not { } card)
                return false;
            if (card.Pile is not { } sourcePile || sourcePile.Type != container.Definition.PileType)
                return false;

            var hand = NPlayerHand.Instance;
            var handPile = PileType.Hand.GetPile(card.Owner);
            if (hand == null || handPile == null)
                return false;

            var origin = new PlayOrigin(container, holder, card, sourcePile, handPile,
                Array.IndexOf([.. sourcePile.Cards], card));
            try
            {
                sourcePile.RemoveInternal(card, true);
                handPile.AddInternal(card, silent: true);
                PendingOrigins[card] = origin;
                _active = origin;
                origin.HandCardRemoved = removed => OnHandCardRemoved(origin, removed);
                handPile.CardRemoved += origin.HandCardRemoved;

                holder.Reparent(hand);
                holder.BeginDrag();
                NCardPlay cardPlay = Sts2InputCompat.IsUsingDirectionalNavigation
                    ? NControllerCardPlay.Create(holder)
                    : NMouseCardPlay.Create(holder, Sts2InputCompat.CancelCardPlayAction, false);
                origin.CardPlay = cardPlay;
                container.AddChild(cardPlay);
                cardPlay.Connect(NCardPlay.SignalName.Finished,
                    Callable.From<bool>(success => OnTargetingFinished(origin, success)));
                cardPlay.Start();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    RollBackTargeting(origin, true);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Extra-hand targeting initialization and its rollback both failed.",
                        ex,
                        rollbackException);
                }

                throw;
            }
        }

        internal static void DetachContainer(NModExtraHand container)
        {
            foreach (var origin in PendingOrigins.Values
                         .Where(candidate => ReferenceEquals(candidate.Container, container))
                         .ToArray())
            {
                RestoreToSourcePile(origin);

                if (ReferenceEquals(_active, origin))
                    _active = null;
                ClearOrigin(origin);
            }
        }

        internal static void PrepareForEnqueue(NCardPlay cardPlay)
        {
            var origin = _active;
            if (origin == null || !ReferenceEquals(origin.CardPlay, cardPlay))
                return;
            if (!GodotObject.IsInstanceValid(origin.Holder))
                return;

            var handContainer = NPlayerHand.Instance?.CardHolderContainer;
            if (handContainer != null && origin.Holder.GetParent() != handContainer)
                origin.Holder.Reparent(handContainer);
        }

        internal static void RestoreCancelledAction(PlayCardAction action)
        {
            var card = action.NetCombatCard.ToCardModelOrNull();
            if (card == null || !PendingOrigins.TryGetValue(card, out var origin))
                return;

            NCard? cardNode = null;
            var hand = NPlayerHand.Instance;
            var holder = hand?.GetCardHolder(card);
            if (holder != null)
            {
                cardNode = holder.CardNode;
                hand!.RemoveCardHolder(holder);
            }

            RestoreToSourcePile(origin);

            ClearOrigin(origin);
            origin.Container.RestoreCancelledQueuedCard(card, cardNode);
        }

        private static void OnTargetingFinished(PlayOrigin origin, bool success)
        {
            if (origin.Closed)
                return;
            if (ReferenceEquals(_active, origin))
                _active = null;

            if (success)
            {
                origin.Container.ReleaseHolderForQueuedPlay(origin.Card);
                return;
            }

            RollBackTargeting(origin);
        }

        private static void RollBackTargeting(PlayOrigin origin, bool restoreInterruptedTransfer = false)
        {
            if (origin.Closed)
                return;
            if (ReferenceEquals(_active, origin))
                _active = null;
            RestoreToSourcePile(origin, restoreInterruptedTransfer);

            ClearOrigin(origin);
            origin.Container.RestoreCancelledPlay(origin.Card, origin.Holder);
        }

        private static void OnHandCardRemoved(PlayOrigin origin, CardModel removed)
        {
            if (!ReferenceEquals(origin.Card, removed))
                return;
            ClearOrigin(origin);
        }

        private static void RestoreToSourcePile(PlayOrigin origin, bool restoreInterruptedTransfer = false)
        {
            if (origin.HandPile.Cards.Contains(origin.Card))
                origin.HandPile.RemoveInternal(origin.Card, true);
            else if (!restoreInterruptedTransfer)
                return;

            if (origin.SourcePile.Cards.Contains(origin.Card))
                return;

            var index = Math.Clamp(origin.SourceIndex, 0, origin.SourcePile.Cards.Count);
            origin.SourcePile.AddInternal(origin.Card, index, true);
        }

        private static void ClearOrigin(PlayOrigin origin)
        {
            if (origin.Closed)
                return;
            origin.Closed = true;
            PendingOrigins.Remove(origin.Card);
            if (origin.HandCardRemoved != null)
                origin.HandPile.CardRemoved -= origin.HandCardRemoved;
            origin.HandCardRemoved = null;
        }

        private sealed class PlayOrigin(
            NModExtraHand container,
            NHandCardHolder holder,
            CardModel card,
            CardPile sourcePile,
            CardPile handPile,
            int sourceIndex)
        {
            public NModExtraHand Container { get; } = container;
            public NHandCardHolder Holder { get; } = holder;
            public CardModel Card { get; } = card;
            public CardPile SourcePile { get; } = sourcePile;
            public CardPile HandPile { get; } = handPile;
            public int SourceIndex { get; } = sourceIndex;
            public NCardPlay? CardPlay { get; set; }
            public Action<CardModel>? HandCardRemoved { get; set; }
            public bool Closed { get; set; }
        }
    }
}
