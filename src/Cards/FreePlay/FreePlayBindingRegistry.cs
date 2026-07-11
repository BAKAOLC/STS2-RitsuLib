#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Cards.FreePlay
{
    /// <summary>
    ///     Detailed free-play resolution result split by detection source.
    ///     按检测来源拆分的详细 free-play 解析结果。
    /// </summary>
    public sealed record FreePlayResolution(
        bool IsAutoPlayNoSpend,
        bool IsCardBindingFree,
        bool IsDualResourceModelFree,
        bool IsRegisteredDetectorFree)
    {
        /// <summary>
        ///     True when any detection source marks this play as free.
        ///     任一检测来源将本次出牌标记为免费时为 true。
        /// </summary>
        public bool IsFree => IsAutoPlayNoSpend || IsCardBindingFree || IsDualResourceModelFree ||
                              IsRegisteredDetectorFree;
    }

    internal readonly record struct FreePlayCardCostScope(
        bool FixedSecondaryCostsFree,
        bool XSecondaryCostsFree)
    {
        public bool IsFree => FixedSecondaryCostsFree || XSecondaryCostsFree;
    }

    /// <summary>
    ///     Extensible binding registry for "this play is free" semantics.
    ///     用于“本次出牌免费”语义的可扩展绑定注册表。
    /// </summary>
    public static class FreePlayBindingRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<CardPlay, bool>> RegisteredDetectors = [];
        private static readonly AttachedState<CardModel, CardFreeBindingState> CardStates = new(() => new());
        private static readonly AttachedState<CardPlay, PlayFreeBindingState> PlayStates = new(() => new());

        /// <summary>
        ///     Registers an additional free-play detector. The detector should return true when the specified
        ///     <see cref="CardPlay" /> is considered free by mod-defined rules.
        ///     注册额外的 free-play 检测器。当指定 <see cref="CardPlay" /> 按 mod 定义规则视为免费时，检测器应返回 true。
        /// </summary>
        /// <param name="bindingId">
        ///     Stable unique identifier for replacement/debugging.
        ///     用于替换和调试的稳定唯一标识符。
        /// </param>
        /// <param name="detector">
        ///     Predicate that evaluates whether a play is free.
        ///     判断一次出牌是否免费的谓词。
        /// </param>
        public static void Register(string bindingId, Func<CardPlay, bool> detector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bindingId);
            ArgumentNullException.ThrowIfNull(detector);

            lock (Gate)
            {
                RegisteredDetectors[bindingId] = detector;
            }
        }

        /// <summary>
        ///     Marks that the given card's base costs should be treated as free for its next play.
        ///     标记给定卡牌下一次出牌的基础费用应视为免费。
        /// </summary>
        /// <param name="card">
        ///     Card receiving a single-use base-cost free charge.
        ///     获得一次性基础费用免费层数的卡牌。
        /// </param>
        public static void MarkCardFreeNextPlay(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            CardStates.Update(card, state =>
            {
                state.BaseCostsFreeNextPlayCharges++;
                return state;
            });
        }

        /// <summary>
        ///     Marks that the given card's base costs should be treated as free until end of turn or its next play.
        ///     标记给定卡牌在回合结束或下一次打出前，基础费用应视为免费。
        /// </summary>
        /// <param name="card">
        ///     Card receiving a current-turn base-cost free charge.
        ///     获得本回合基础费用免费层数的卡牌。
        /// </param>
        public static void MarkCardFreeThisTurn(CardModel card)
        {
            MarkCardBaseCostsFreeThisTurn(card);
        }

        internal static void MarkCardBaseCostsFreeThisTurn(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            CardStates.Update(card, state =>
            {
                state.BaseCostsFreeThisTurnCharges++;
                return state;
            });
        }

        internal static void MarkCardBaseCostsFreeForRestOfTurn(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            CardStates.Update(card, state =>
            {
                state.BaseCostsFreeForRestOfTurnCharges++;
                return state;
            });
        }

        internal static void MarkCardBaseCostsFreeThisCombat(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            CardStates.Update(card, state =>
            {
                state.BaseCostsFreeThisCombatState = ResolveCombatState(card);
                return state;
            });
        }

        /// <summary>
        ///     Marks that the given card's base costs should be treated as free for the current combat.
        ///     标记给定卡牌在当前战斗中，基础费用应视为免费。
        /// </summary>
        /// <param name="card">
        ///     Card receiving combat-duration base-cost free state.
        ///     获得持续整场战斗基础费用免费状态的卡牌。
        /// </param>
        public static void MarkCardFreeThisCombat(CardModel card)
        {
            MarkCardBaseCostsFreeThisCombat(card);
        }

        /// <summary>
        ///     Marks the current <see cref="CardPlay" /> as free immediately.
        /// </summary>
        /// <param name="play">
        ///     Play instance to mark.
        ///     要标记的出牌实例。
        /// </param>
        public static void MarkCurrentPlayFree(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);
            PlayStates.Set(play, new()
            {
                IsResolved = true,
                Resolution = new(false, true, false, false),
            });
        }

        /// <summary>
        ///     Resolves detailed free-play sources for this <see cref="CardPlay" />.
        ///     解析此 <see cref="CardPlay" /> 的详细 free-play 来源。
        /// </summary>
        /// <param name="play">
        ///     Play instance to evaluate.
        ///     要求值的出牌实例。
        /// </param>
        /// <returns>
        ///     A split resolution indicating which source marked the play as free.
        ///     指示由哪个来源将本次出牌标记为免费的拆分解析结果。
        /// </returns>
        public static FreePlayResolution Resolve(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);

            var cached = PlayStates.GetOrCreate(play);
            if (cached.IsResolved)
                return cached.Resolution;

            var resolution = BuildResolution(play);
            PlayStates.Set(play, new()
            {
                IsResolved = true,
                Resolution = resolution,
            });
            return resolution;
        }

        /// <summary>
        ///     Convenience helper returning whether the play is free by any source.
        ///     返回本次出牌是否因任一来源而免费的便捷辅助方法。
        /// </summary>
        /// <param name="play">
        ///     Play instance to evaluate.
        ///     要求值的出牌实例。
        /// </param>
        /// <returns>
        ///     True when any free-play source applies.
        ///     任一 free-play 来源适用时为 true。
        /// </returns>
        public static bool IsFreeForPlay(CardPlay play)
        {
            return Resolve(play).IsFree;
        }

        /// <summary>
        ///     Returns whether the card is already marked free before a <see cref="CardPlay" /> exists.
        ///     This does not consume next-play free charges.
        ///     在 <see cref="CardPlay" /> 尚未创建前返回此卡是否已被标记为免费。
        ///     此方法不会消费下一次出牌免费层数。
        /// </summary>
        public static bool IsCardFreeForUpcomingPlay(CardModel card)
        {
            return ResolveCardCostScopeForUpcomingPlay(card).IsFree;
        }

        internal static FreePlayCardCostScope ResolveCardCostScopeForUpcomingPlay(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (!CardStates.TryGetValue(card, out var state))
                return new(false, false);

            var combatState = ResolveCombatState(card);
            var isFullFree = state.ThisTurnCharges > 0 ||
                             state.NextPlayCharges > 0 ||
                             (state.FreeThisCombatState != null &&
                              ReferenceEquals(state.FreeThisCombatState, combatState));
            var isBaseCostFree = isFullFree ||
                                 state.BaseCostsFreeNextPlayCharges > 0 ||
                                 state.BaseCostsFreeThisTurnCharges > 0 ||
                                 state.BaseCostsFreeForRestOfTurnCharges > 0 ||
                                 (state.BaseCostsFreeThisCombatState != null &&
                                  ReferenceEquals(state.BaseCostsFreeThisCombatState, combatState));
            return new(isBaseCostFree, isFullFree);
        }

        /// <summary>
        ///     Clears current-turn free-play charges that were not consumed by playing the card.
        ///     清除未通过打出消耗的本回合 free-play 层数。
        /// </summary>
        /// <param name="card">
        ///     Card receiving end-of-turn cleanup.
        ///     正在执行回合结束清理的卡牌。
        /// </param>
        /// <returns>
        ///     True when any current-turn free-play charge was cleared.
        ///     清除了任意本回合 free-play 层数时返回 true。
        /// </returns>
        public static bool ClearCardFreeThisTurn(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            var changed = false;
            CardStates.Update(card, state =>
            {
                changed = state.ThisTurnCharges > 0 ||
                          state.BaseCostsFreeThisTurnCharges > 0 ||
                          state.BaseCostsFreeForRestOfTurnCharges > 0;
                state.ThisTurnCharges = 0;
                state.BaseCostsFreeThisTurnCharges = 0;
                state.BaseCostsFreeForRestOfTurnCharges = 0;
                return state;
            });
            return changed;
        }

        /// <summary>
        ///     Clears free-play bindings that expire after the card has been played.
        ///     清除卡牌打出后过期的 free-play 绑定。
        /// </summary>
        /// <param name="card">
        ///     Card receiving after-play cleanup.
        ///     正在执行打出后清理的卡牌。
        /// </param>
        /// <returns>
        ///     True when any after-play binding was cleared or consumed.
        ///     清除或消费了任意打出后过期绑定时返回 true。
        /// </returns>
        public static bool ClearCardFreeAfterPlayed(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            var changed = false;
            CardStates.Update(card, state =>
            {
                changed = state.ThisTurnCharges > 0 ||
                          state.NextPlayCharges > 0 ||
                          state.BaseCostsFreeNextPlayCharges > 0 ||
                          state.BaseCostsFreeThisTurnCharges > 0;
                state.ThisTurnCharges = 0;
                state.NextPlayCharges = Math.Max(0, state.NextPlayCharges - 1);
                state.BaseCostsFreeNextPlayCharges = Math.Max(0, state.BaseCostsFreeNextPlayCharges - 1);
                state.BaseCostsFreeThisTurnCharges = 0;
                return state;
            });
            return changed;
        }

        private static FreePlayResolution BuildResolution(CardPlay play)
        {
            if (play.IsAutoPlay)
                return new(true, false, false, false);

            var isCardBindingFree = EvaluateCardBindings(play);
            var isDualResourceModelFree = IsFreeByDualResourceModel(play);
            var isRegisteredDetectorFree = EvaluateRegisteredDetectors(play);
            return new(false, isCardBindingFree, isDualResourceModelFree, isRegisteredDetectorFree);
        }

        private static bool EvaluateCardBindings(CardPlay play)
        {
            var card = play.Card;
            var state = CardStates.GetOrCreate(card);
            var combatState = ResolveCombatState(card);

            if (state.FreeThisCombatState != null && ReferenceEquals(state.FreeThisCombatState, combatState))
                return true;

            if (state.BaseCostsFreeThisCombatState != null &&
                ReferenceEquals(state.BaseCostsFreeThisCombatState, combatState))
                return true;

            if (state.BaseCostsFreeThisTurnCharges > 0)
                return true;

            if (state.BaseCostsFreeForRestOfTurnCharges > 0)
                return true;

            if (state.BaseCostsFreeNextPlayCharges > 0)
                return true;

            if (state.ThisTurnCharges > 0)
                return true;

            return state.NextPlayCharges > 0;
        }

        private static bool EvaluateRegisteredDetectors(CardPlay play)
        {
            Func<CardPlay, bool>[] detectors;
            lock (Gate)
            {
                detectors = RegisteredDetectors.Values.ToArray();
            }

            return detectors.Any(detector => detector(play));
        }

        private static bool IsFreeByDualResourceModel(CardPlay play)
        {
            var card = play.Card;
            var owner = card.Owner;
            if (owner?.Creature == null)
                return false;

            if (play.IsAutoPlay)
                return false;

            var models = owner.Creature.Powers
                .Cast<AbstractModel>()
                .Concat(owner.Relics);

            return models.Any(model => IsDualResourceZeroedByModel(model, card));
        }

        private static bool IsDualResourceZeroedByModel(AbstractModel model, CardModel card)
        {
            var energyOriginal = (decimal)card.EnergyCost.GetWithModifiers(CostModifiers.Local);
            var starOriginal = card.CurrentStarCost;

            var changedEnergy = model.TryModifyEnergyCostInCombat(card, energyOriginal, out var energyModified);
            if (!changedEnergy || energyModified > 0m)
                return false;

            var changedStar = model.TryModifyStarCost(card, starOriginal, out var starModified);
            return changedStar && starModified <= 0m;
        }

        private static CombatStateLike? ResolveCombatState(CardModel card)
        {
            return card.CombatState ?? card.Owner?.Creature?.CombatState;
        }

        private sealed class CardFreeBindingState
        {
            public int ThisTurnCharges { get; set; }
            public int NextPlayCharges { get; set; }
            public CombatStateLike? FreeThisCombatState { get; set; }
            public int BaseCostsFreeNextPlayCharges { get; set; }
            public int BaseCostsFreeThisTurnCharges { get; set; }
            public int BaseCostsFreeForRestOfTurnCharges { get; set; }
            public CombatStateLike? BaseCostsFreeThisCombatState { get; set; }
        }

        private sealed class PlayFreeBindingState
        {
            public bool IsResolved { get; set; }
            public FreePlayResolution Resolution { get; set; } = new(false, false, false, false);
        }
    }
}
