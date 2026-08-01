using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching;

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
        private const float MousePlayZoneScreenProportion = 0.75f;
        private const float MousePlayZoneStartOffset = 100f;

        private static readonly Action<NPlayerHand, NHandCardHolder, bool> StartVanillaCardPlay =
            PrivateAccess.DeclaredMethodDelegate<NPlayerHand, Action<NPlayerHand, NHandCardHolder, bool>>(
                "StartCardPlay",
                typeof(NHandCardHolder),
                typeof(bool));

        private static readonly AccessTools.FieldRef<NPlayerHand, NCardPlay?> CurrentCardPlayRef =
            PrivateAccess.FieldRef<NPlayerHand, NCardPlay?>("_currentCardPlay");

        private static readonly AccessTools.FieldRef<NPlayerHand, StringName[]> SelectCardShortcutsRef =
            PrivateAccess.FieldRef<NPlayerHand, StringName[]>("_selectCardShortcuts");

        private static readonly AccessTools.FieldRef<NMouseCardPlay, float> MouseDragStartYRef =
            PrivateAccess.FieldRef<NMouseCardPlay, float>("_dragStartYPosition");

        private static readonly Dictionary<CardModel, PlayOrigin> PendingOrigins = [];
        private static PlayOrigin? _active;

        internal static bool IsPlaying => _active != null;

        internal static bool IsActiveHolder(NHandCardHolder? holder)
        {
            return holder != null && ReferenceEquals(_active?.Holder, holder);
        }

        internal static void CancelActiveTargeting()
        {
            var cardPlay = _active?.CardPlay;
            if (cardPlay == null || !GodotObject.IsInstanceValid(cardPlay))
                return;

            if (NTargetManager.Instance.IsInSelection)
                NTargetManager.Instance.CancelTargeting();
            cardPlay.CancelPlayCard();
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

                holder.Reparent(hand.CardHolderContainer);
                StartVanillaCardPlayWithExtraHandShortcut(hand, holder);
                var cardPlay = CurrentCardPlayRef(hand);
                if (cardPlay == null
                    || !GodotObject.IsInstanceValid(cardPlay)
                    || !ReferenceEquals(cardPlay.Holder, holder))
                    throw new InvalidOperationException(
                        "Vanilla hand did not create a card-play node for the extra-hand holder.");

                origin.CardPlay = cardPlay;
                if (cardPlay is NMouseCardPlay mouseCardPlay)
                    NormalizeMouseDragStart(mouseCardPlay);
                holder.SetIndexLabel(0);
                cardPlay.Connect(NCardPlay.SignalName.Finished,
                    Callable.From<bool>(success => OnTargetingFinished(origin, success)));
                return true;
            }
            catch (Exception ex)
            {
                Exception? cancellationException = null;
                try
                {
                    CancelVanillaCardPlayIfOwned(hand, origin);
                }
                catch (Exception cleanupException)
                {
                    cancellationException = cleanupException;
                }

                try
                {
                    RollBackTargeting(origin, true);
                }
                catch (Exception rollbackException)
                {
                    Exception[] failures = cancellationException == null
                        ? [ex, rollbackException]
                        : [ex, cancellationException, rollbackException];
                    throw new AggregateException(
                        "Extra-hand targeting initialization and its rollback both failed.",
                        failures);
                }

                if (cancellationException != null)
                    throw new AggregateException(
                        "Extra-hand targeting initialization and vanilla cancellation both failed.",
                        ex,
                        cancellationException);

                throw;
            }
        }

        private static void CancelVanillaCardPlayIfOwned(NPlayerHand hand, PlayOrigin origin)
        {
            var cardPlay = origin.CardPlay ?? CurrentCardPlayRef(hand);
            if (cardPlay == null
                || !GodotObject.IsInstanceValid(cardPlay)
                || !ReferenceEquals(cardPlay.Holder, origin.Holder))
                return;

            origin.CardPlay = cardPlay;
            if (NTargetManager.Instance.IsInSelection)
                NTargetManager.Instance.CancelTargeting();
            cardPlay.CancelPlayCard();
        }

        internal static void DetachContainer(NModExtraHand container)
        {
            foreach (var origin in PendingOrigins.Values
                         .Where(candidate => ReferenceEquals(candidate.Container, container))
                         .ToArray())
            {
                if (ReferenceEquals(_active, origin))
                {
                    CancelActiveTargeting();
                    if (origin.Closed)
                        continue;
                }

                RestoreToSourcePile(origin);
                ClearOrigin(origin);
            }
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
            if (success)
                origin.Container.ReleaseHolderForQueuedPlay(origin.Card);
            if (origin.Closed)
                return;
            if (ReferenceEquals(_active, origin))
                _active = null;

            if (success)
                return;

            RollBackTargeting(origin);
        }

        private static void NormalizeMouseDragStart(NMouseCardPlay cardPlay)
        {
            var playZoneY = cardPlay.GetViewport().GetVisibleRect().Size.Y * MousePlayZoneScreenProportion;
            ref var dragStartY = ref MouseDragStartYRef(cardPlay);
            if (dragStartY <= playZoneY)
                dragStartY = playZoneY + MousePlayZoneStartOffset;
        }

        private static void StartVanillaCardPlayWithExtraHandShortcut(
            NPlayerHand hand,
            NHandCardHolder holder)
        {
            var holderIndex = holder.GetIndex();
            if (holderIndex < 0)
                throw new InvalidOperationException("Extra-hand holder is not mounted in the vanilla hand container.");

            ref var shortcuts = ref SelectCardShortcutsRef(hand);
            var originalShortcuts = shortcuts;
            var temporaryShortcuts = new StringName[Math.Max(originalShortcuts.Length, holderIndex + 1)];
            originalShortcuts.CopyTo(temporaryShortcuts, 0);
            temporaryShortcuts[holderIndex] = MegaInput.cancel;
            shortcuts = temporaryShortcuts;
            try
            {
                StartVanillaCardPlay(hand, holder, false);
            }
            finally
            {
                shortcuts = originalShortcuts;
            }
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
            NPlayerHand.Instance?.ForceRefreshCardIndices();
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
            if (ReferenceEquals(_active, origin))
                _active = null;
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
