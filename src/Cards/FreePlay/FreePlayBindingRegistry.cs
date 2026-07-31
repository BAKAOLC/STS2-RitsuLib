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
    ///     <para xml:lang="en">Describes which detection sources marked a card play as free.</para>
    ///     <para xml:lang="zh-CN">描述哪些检测来源将一次出牌标记为免费。</para>
    /// </summary>
    public sealed record FreePlayResolution(
        bool IsAutoPlayNoSpend,
        bool IsCardBindingFree,
        bool IsRegisteredDetectorFree)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether any detection source marks this play as free.</para>
        ///     <para xml:lang="zh-CN">获取是否有任一检测来源将本次出牌标记为免费。</para>
        /// </summary>
        public bool IsFree => IsAutoPlayNoSpend || IsCardBindingFree || IsRegisteredDetectorFree;
    }

    internal readonly record struct FreePlayCardCostScope(
        bool FixedSecondaryCostsFree,
        bool XSecondaryCostsFree)
    {
        public bool IsFree => FixedSecondaryCostsFree || XSecondaryCostsFree;
    }

    /// <summary>
    ///     <para xml:lang="en">Provides an extensible registry for determining whether a card play is free.</para>
    ///     <para xml:lang="zh-CN">提供可扩展注册表，用于判断一次出牌是否免费。</para>
    /// </summary>
    public static class FreePlayBindingRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Func<CardPlay, bool>> RegisteredDetectors = [];
        private static readonly AttachedState<CardModel, CardFreeBindingState> CardStates = new(() => new());
        private static readonly AttachedState<CardPlay, PlayFreeBindingState> PlayStates = new(() => new());

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an additional free-play detector. The detector should return <see langword="true" /> when
        ///         mod-defined rules consider the specified <see cref="CardPlay" /> free.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册额外的免费出牌检测器。模组规则将指定 <see cref="CardPlay" /> 视为免费时，检测器应返回
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        /// <param name="bindingId">
        ///     <para xml:lang="en">Stable unique ID used for replacement and diagnostics.</para>
        ///     <para xml:lang="zh-CN">用于替换和诊断的稳定唯一 ID。</para>
        /// </param>
        /// <param name="detector">
        ///     <para xml:lang="en">Predicate that determines whether a play is free.</para>
        ///     <para xml:lang="zh-CN">判断一次出牌是否免费的谓词。</para>
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
        ///     <para xml:lang="en">Marks the card's base costs as free for its next play.</para>
        ///     <para xml:lang="zh-CN">将卡牌下一次打出时的基础费用标记为免费。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">Card receiving a single-use base-cost-free charge.</para>
        ///     <para xml:lang="zh-CN">获得一次性基础费用免费次数的卡牌。</para>
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
        ///     <para xml:lang="en">Marks the card's base costs as free until the end of the turn or its next play.</para>
        ///     <para xml:lang="zh-CN">将卡牌的基础费用标记为免费，直至回合结束或该牌下一次打出。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">Card receiving a current-turn base-cost-free charge.</para>
        ///     <para xml:lang="zh-CN">获得本回合基础费用免费次数的卡牌。</para>
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
        ///     <para xml:lang="en">Marks the card's base costs as free for the current combat.</para>
        ///     <para xml:lang="zh-CN">将卡牌在当前战斗中的基础费用标记为免费。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">Card receiving a combat-duration base-cost-free state.</para>
        ///     <para xml:lang="zh-CN">获得持续整场战斗基础费用免费状态的卡牌。</para>
        /// </param>
        public static void MarkCardFreeThisCombat(CardModel card)
        {
            MarkCardBaseCostsFreeThisCombat(card);
        }

        /// <summary>
        ///     <para xml:lang="en">Immediately marks the current <see cref="CardPlay" /> as free.</para>
        ///     <para xml:lang="zh-CN">立即将当前 <see cref="CardPlay" /> 标记为免费。</para>
        /// </summary>
        /// <param name="play">
        ///     <para xml:lang="en">Play instance to mark.</para>
        ///     <para xml:lang="zh-CN">要标记的出牌实例。</para>
        /// </param>
        public static void MarkCurrentPlayFree(CardPlay play)
        {
            ArgumentNullException.ThrowIfNull(play);
            PlayStates.Update(play, state =>
            {
                state.Resolution = state.IsResolved
                    ? state.Resolution with { IsCardBindingFree = true }
                    : new(play.IsAutoPlay, true, false);
                state.IsResolved = true;
                return state;
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the free-play sources for this <see cref="CardPlay" />.</para>
        ///     <para xml:lang="zh-CN">解析此 <see cref="CardPlay" /> 的免费出牌来源。</para>
        /// </summary>
        /// <param name="play">
        ///     <para xml:lang="en">Play instance to evaluate.</para>
        ///     <para xml:lang="zh-CN">要求值的出牌实例。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A result indicating which sources marked the play as free.</para>
        ///     <para xml:lang="zh-CN">指示哪些来源将本次出牌标记为免费的结果。</para>
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
        ///     <para xml:lang="en">Returns whether any source marks the play as free.</para>
        ///     <para xml:lang="zh-CN">返回是否有任一来源将本次出牌标记为免费。</para>
        /// </summary>
        /// <param name="play">
        ///     <para xml:lang="en">Play instance to evaluate.</para>
        ///     <para xml:lang="zh-CN">要求值的出牌实例。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when any free-play source applies.</para>
        ///     <para xml:lang="zh-CN">有任一免费出牌来源适用时为 <see langword="true" />。</para>
        /// </returns>
        public static bool IsFreeForPlay(CardPlay play)
        {
            return Resolve(play).IsFree;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the card is marked free before a <see cref="CardPlay" /> exists, without consuming a
        ///         next-play charge.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回卡牌在 <see cref="CardPlay" /> 创建前是否已被标记为免费，且不消耗下一次出牌的免费次数。
        ///     </para>
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
            var isBaseCostFree = state.BaseCostsFreeNextPlayCharges > 0 ||
                                 state.BaseCostsFreeThisTurnCharges > 0 ||
                                 state.BaseCostsFreeForRestOfTurnCharges > 0 ||
                                 (state.BaseCostsFreeThisCombatState != null &&
                                  ReferenceEquals(state.BaseCostsFreeThisCombatState, combatState));
            return new(isBaseCostFree, false);
        }

        /// <summary>
        ///     <para xml:lang="en">Clears current-turn free-play charges that were not consumed by playing the card.</para>
        ///     <para xml:lang="zh-CN">清除未因打出卡牌而消耗的本回合免费出牌次数。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">Card receiving end-of-turn cleanup.</para>
        ///     <para xml:lang="zh-CN">正在执行回合结束清理的卡牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when any current-turn free-play charge was cleared.</para>
        ///     <para xml:lang="zh-CN">清除了任一本回合免费出牌次数时为 <see langword="true" />。</para>
        /// </returns>
        public static bool ClearCardFreeThisTurn(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            var changed = false;
            CardStates.Update(card, state =>
            {
                changed = state.BaseCostsFreeThisTurnCharges > 0 ||
                          state.BaseCostsFreeForRestOfTurnCharges > 0;
                state.BaseCostsFreeThisTurnCharges = 0;
                state.BaseCostsFreeForRestOfTurnCharges = 0;
                return state;
            });
            return changed;
        }

        /// <summary>
        ///     <para xml:lang="en">Clears free-play bindings that expire after the card is played.</para>
        ///     <para xml:lang="zh-CN">清除在卡牌打出后失效的免费出牌绑定。</para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">Card receiving after-play cleanup.</para>
        ///     <para xml:lang="zh-CN">正在执行打出后清理的卡牌。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when any after-play binding was cleared or consumed.</para>
        ///     <para xml:lang="zh-CN">清除或消耗了任一打出后失效的绑定时为 <see langword="true" />。</para>
        /// </returns>
        public static bool ClearCardFreeAfterPlayed(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            var changed = false;
            CardStates.Update(card, state =>
            {
                changed = state.BaseCostsFreeNextPlayCharges > 0 ||
                          state.BaseCostsFreeThisTurnCharges > 0;
                state.BaseCostsFreeNextPlayCharges = Math.Max(0, state.BaseCostsFreeNextPlayCharges - 1);
                state.BaseCostsFreeThisTurnCharges = 0;
                return state;
            });
            return changed;
        }

        private static FreePlayResolution BuildResolution(CardPlay play)
        {
            if (play.IsAutoPlay)
                return new(true, false, false);

            var isCardBindingFree = EvaluateCardBindings(play);
            var isRegisteredDetectorFree = EvaluateRegisteredDetectors(play);
            return new(false, isCardBindingFree, isRegisteredDetectorFree);
        }

        private static bool EvaluateCardBindings(CardPlay play)
        {
            var card = play.Card;
            var state = CardStates.GetOrCreate(card);
            var combatState = ResolveCombatState(card);

            if (state.BaseCostsFreeThisCombatState != null &&
                ReferenceEquals(state.BaseCostsFreeThisCombatState, combatState))
                return true;

            if (state.BaseCostsFreeThisTurnCharges > 0)
                return true;

            if (state.BaseCostsFreeForRestOfTurnCharges > 0)
                return true;

            return state.BaseCostsFreeNextPlayCharges > 0;
        }

        private static bool EvaluateRegisteredDetectors(CardPlay play)
        {
            KeyValuePair<string, Func<CardPlay, bool>>[] detectors;
            lock (Gate)
            {
                detectors = [.. RegisteredDetectors];
            }

            foreach (var (bindingId, detector) in detectors)
                try
                {
                    if (detector(play))
                        return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[FreePlay] Detector '{bindingId}' failed for card '{play.Card.Id}': {ex}");
                    throw;
                }

            return false;
        }

        private static CombatStateLike? ResolveCombatState(CardModel card)
        {
            return card.CombatState ?? (card.IsMutable ? card.Owner.Creature.CombatState : null);
        }

        private sealed class CardFreeBindingState
        {
            public int BaseCostsFreeNextPlayCharges { get; set; }
            public int BaseCostsFreeThisTurnCharges { get; set; }
            public int BaseCostsFreeForRestOfTurnCharges { get; set; }
            public CombatStateLike? BaseCostsFreeThisCombatState { get; set; }
        }

        private sealed class PlayFreeBindingState
        {
            public bool IsResolved { get; set; }
            public FreePlayResolution Resolution { get; set; } = new(false, false, false);
        }
    }
}
