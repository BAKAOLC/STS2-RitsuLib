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
    ///         The card remains in its extra-hand pile while targeting and queued. The vanilla manual-play checks
    ///         treat that pile as hand-compatible only inside the patched play path. Canceling targeting or the
    ///         queued action returns the same card node to the extra hand without changing model ownership.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         目标选择及排队期间，卡牌会保留在额外手牌堆中。仅在修补后的出牌路径内，原版手动出牌检查会将该牌堆
    ///         视为与手牌兼容。取消目标选择或已排队动作时，同一个卡牌节点会退回额外手牌，且不改变模型归属。
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

        internal static void CancelActiveTargeting(NModExtraHand container)
        {
            if (ReferenceEquals(_active?.Container, container))
                CancelActiveTargeting();
        }

        internal static bool TryBegin(NModExtraHand container, NHandCardHolder holder)
        {
            if (_active != null || holder.CardModel is not { } card)
                return false;
            if (card.Pile is not { } sourcePile || sourcePile.Type != container.Definition.PileType)
                return false;

            var hand = NPlayerHand.Instance;
            if (hand == null)
                return false;

            var origin = new PlayOrigin(container, holder, card, sourcePile);
            try
            {
                PendingOrigins[card] = origin;
                origin.SourceCardRemoved = removed => OnSourceCardRemoved(origin, removed);
                sourcePile.CardRemoved += origin.SourceCardRemoved;
                _active = origin;

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
                    RollBackTargeting(origin);
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

                ClearOrigin(origin);
            }
        }

        internal static PileType GetVanillaManualPlayPileType(CardPile pile)
        {
            var pileType = pile.Type;
            return ModCardPileRegistry.TryGetByPileType(pileType, out var definition)
                   && definition is
                   {
                       Style: ModCardPileUiStyle.ExtraHand,
                       ExtraHand.AllowCardPlay: true,
                   }
                   && PendingOrigins.Values.Any(origin => ReferenceEquals(origin.SourcePile, pile))
                ? PileType.Hand
                : pileType;
        }

        internal static NHandCardHolder ReturnCancelledQueuedCard(
            NPlayerHand hand,
            NCard cardNode,
            int index)
        {
            var card = cardNode.Model;
            if (card == null || !PendingOrigins.TryGetValue(card, out var origin))
                return hand.Add(cardNode, index);

            ClearOrigin(origin);
            return origin.Container.RestoreCancelledQueuedCard(card, cardNode);
        }

        internal static void RestoreCancelledAction(PlayCardAction action)
        {
            var card = action.NetCombatCard.ToCardModelOrNull();
            if (card == null || !PendingOrigins.TryGetValue(card, out var origin))
                return;

            var hand = NPlayerHand.Instance;
            if (hand?.GetCardHolder(card) is not NHandCardHolder holder)
                return;

            ClearOrigin(origin);
            origin.Container.RestoreCancelledPlay(card, holder);
            hand.ForceRefreshCardIndices();
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

        private static void RollBackTargeting(PlayOrigin origin)
        {
            if (origin.Closed)
                return;
            if (ReferenceEquals(_active, origin))
                _active = null;

            ClearOrigin(origin);
            origin.Container.RestoreCancelledPlay(origin.Card, origin.Holder);
            NPlayerHand.Instance?.ForceRefreshCardIndices();
        }

        private static void OnSourceCardRemoved(PlayOrigin origin, CardModel removed)
        {
            if (!ReferenceEquals(origin.Card, removed))
                return;
            ClearOrigin(origin);
        }

        private static void ClearOrigin(PlayOrigin origin)
        {
            if (ReferenceEquals(_active, origin))
                _active = null;
            if (origin.Closed)
                return;
            origin.Closed = true;
            PendingOrigins.Remove(origin.Card);
            if (origin.SourceCardRemoved != null)
                origin.SourcePile.CardRemoved -= origin.SourceCardRemoved;
            origin.SourceCardRemoved = null;
        }

        private sealed class PlayOrigin(
            NModExtraHand container,
            NHandCardHolder holder,
            CardModel card,
            CardPile sourcePile)
        {
            public NModExtraHand Container { get; } = container;
            public NHandCardHolder Holder { get; } = holder;
            public CardModel Card { get; } = card;
            public CardPile SourcePile { get; } = sourcePile;
            public NCardPlay? CardPlay { get; set; }
            public Action<CardModel>? SourceCardRemoved { get; set; }
            public bool Closed { get; set; }
        }
    }
}
