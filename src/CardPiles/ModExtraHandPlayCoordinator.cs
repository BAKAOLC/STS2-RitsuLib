using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Bridges playable extra-hand cards through the vanilla hand-only manual-play pipeline. The card is
    ///     moved silently into the backend hand while targeting is active, preserving the original pile so a
    ///     canceled target selection or canceled queued action can restore both model and visual ownership.
    ///     将可打出的额外手牌卡牌接入原版仅支持手牌的手动打牌流程。目标选择期间，卡牌会静默移入后端手牌，
    ///     同时保留原牌堆，以便目标选择取消或队列动作取消时恢复模型与视觉归属。
    /// </summary>
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
                Array.IndexOf(sourcePile.Cards.ToArray(), card));
            try
            {
                sourcePile.RemoveInternal(card, true);
                handPile.AddInternal(card, silent: true);
                PendingOrigins[card] = origin;
                _active = origin;
                origin.HandCardRemoved = removed => OnHandCardRemoved(origin, removed);
                handPile.CardRemoved += origin.HandCardRemoved;

                holder.BeginDrag();
                var cardPlay = NControllerManager.Instance?.IsUsingController == true
                    ? (NCardPlay)NControllerCardPlay.Create(holder)
                    : NMouseCardPlay.Create(holder, MegaInput.releaseCard, false);
                origin.CardPlay = cardPlay;
                container.AddChild(cardPlay);
                cardPlay.Connect(NCardPlay.SignalName.Finished,
                    Callable.From<bool>(success => OnTargetingFinished(origin, success)));
                cardPlay.Start();
                return true;
            }
            catch
            {
                RollBackTargeting(origin);
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

        private static void RollBackTargeting(PlayOrigin origin)
        {
            if (origin.Closed)
                return;
            if (ReferenceEquals(_active, origin))
                _active = null;
            RestoreToSourcePile(origin);

            ClearOrigin(origin);
            origin.Container.RestoreCancelledPlay(origin.Card, origin.Holder);
        }

        private static void OnHandCardRemoved(PlayOrigin origin, CardModel removed)
        {
            if (!ReferenceEquals(origin.Card, removed))
                return;
            ClearOrigin(origin);
        }

        private static void RestoreToSourcePile(PlayOrigin origin)
        {
            if (!ReferenceEquals(origin.Card.Pile, origin.HandPile))
                return;

            origin.HandPile.RemoveInternal(origin.Card, true);
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
