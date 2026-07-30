using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Cards;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Typed model capability base that opts into the owning model's vanilla hook listener stream when that owner
    ///         participates in vanilla hooks.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当所属模型参与游戏原版钩子时，此类型化模型能力基类会加入所属模型的原版钩子监听器流。
    ///     </para>
    /// </summary>
    public abstract class OwnerHookCapability<TModel> : ModelCapability<TModel>, IModelCapabilityHookListener
        where TModel : AbstractModel
    {
        /// <inheritdoc />
        public virtual bool ShouldReceiveOwnerHooks => true;

        /// <inheritdoc />
        public virtual int OwnerHookOrder => 0;
    }

    /// <summary>
    ///     <para xml:lang="en">Capability base for card-owned behavior and capability-owned card dynamic vars.</para>
    ///     <para xml:lang="zh-CN">用于所属卡牌行为及能力自有卡牌动态变量的能力基类。</para>
    /// </summary>
    public abstract class CardCapability : OwnerHookCapability<CardModel>
    {
        /// <summary>
        ///     <para xml:lang="en">Called after the owning card's vanilla upgrade body has run.</para>
        ///     <para xml:lang="zh-CN">所属卡牌的原版升级主体执行后调用。</para>
        /// </summary>
        protected virtual void OnOwnerCardUpgraded(CardModel card)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called after the owning card finalizes upgrade highlights.</para>
        ///     <para xml:lang="zh-CN">所属卡牌完成升级高亮收尾后调用。</para>
        /// </summary>
        protected virtual void OnOwnerCardUpgradeFinalized(CardModel card)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called after the owning card's vanilla downgrade hook has run.</para>
        ///     <para xml:lang="zh-CN">所属卡牌的原版降级钩子执行后调用。</para>
        /// </summary>
        protected virtual void OnOwnerCardDowngraded(CardModel card)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called after the owning card has been transformed from.</para>
        ///     <para xml:lang="zh-CN">所属卡牌被转化离开后调用。</para>
        /// </summary>
        protected virtual void OnOwnerCardTransformedFrom(CardModel card)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called after the owning card has been transformed to.</para>
        ///     <para xml:lang="zh-CN">所属卡牌作为转化结果进入后调用。</para>
        /// </summary>
        protected virtual void OnOwnerCardTransformedTo(CardModel card)
        {
        }

        internal void NotifyOwnerCardUpgraded(CardModel card)
        {
            OnOwnerCardUpgraded(card);
        }

        internal void NotifyOwnerCardUpgradeFinalized(CardModel card)
        {
            OnOwnerCardUpgradeFinalized(card);
        }

        internal void NotifyOwnerCardDowngraded(CardModel card)
        {
            OnOwnerCardDowngraded(card);
        }

        internal void NotifyOwnerCardTransformedFrom(CardModel card)
        {
            OnOwnerCardTransformedFrom(card);
        }

        internal void NotifyOwnerCardTransformedTo(CardModel card)
        {
            OnOwnerCardTransformedTo(card);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Card base with a protected default-capability hook.</para>
    ///     <para xml:lang="zh-CN">带受保护默认能力钩子的卡牌基类。</para>
    /// </summary>
    public abstract class CapabilityCardModel : CardModel, IModelCapabilitySource
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a card model with default-capability support.</para>
        ///     <para xml:lang="zh-CN">创建支持默认能力的卡牌模型。</para>
        /// </summary>
        protected CapabilityCardModel(
            int canonicalEnergyCost,
            CardType type,
            CardRarity rarity,
            TargetType targetType,
            bool shouldShowInCardLibrary = true)
            : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        void IModelCapabilitySource.BuildDefaultCapabilities(ModelCapabilityList capabilities)
        {
            BuildDefaultCapabilities(capabilities);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds this card's own default capabilities.</para>
        ///     <para xml:lang="zh-CN">添加此卡牌自身的默认能力。</para>
        /// </summary>
        protected virtual void BuildDefaultCapabilities(ModelCapabilityList capabilities)
        {
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Capability base for relic-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属遗物行为的能力基类。</para>
    /// </summary>
    public abstract class RelicCapability : OwnerHookCapability<RelicModel>;

    /// <summary>
    ///     <para xml:lang="en">Capability base for potion-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属药水行为的能力基类。</para>
    /// </summary>
    public abstract class PotionCapability : OwnerHookCapability<PotionModel>;

    /// <summary>
    ///     <para xml:lang="en">Capability base for power-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属能力行为的能力基类。</para>
    /// </summary>
    public abstract class PowerCapability : OwnerHookCapability<PowerModel>;

    /// <summary>
    ///     <para xml:lang="en">Context passed after the owning orb's passive has triggered.</para>
    ///     <para xml:lang="zh-CN">所属充能球被动触发后传入的上下文。</para>
    /// </summary>
    public readonly record struct OrbPassiveTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext,
        Creature? Target);

    /// <summary>
    ///     <para xml:lang="en">Context passed after the owning orb's before-turn-end trigger method has run.</para>
    ///     <para xml:lang="zh-CN">所属充能球的回合结束前触发方法运行后传入的上下文。</para>
    /// </summary>
    public readonly record struct OrbBeforeTurnEndTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext);

    /// <summary>
    ///     <para xml:lang="en">Context passed after the owning orb's after-turn-start trigger method has run.</para>
    ///     <para xml:lang="zh-CN">所属充能球的回合开始后触发方法运行后传入的上下文。</para>
    /// </summary>
    public readonly record struct OrbAfterTurnStartTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext);

    /// <summary>
    ///     <para xml:lang="en">Context passed after the owning orb has been evoked.</para>
    ///     <para xml:lang="zh-CN">所属充能球被激发后传入的上下文。</para>
    /// </summary>
    public readonly record struct OrbEvokeContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext,
        IReadOnlyList<Creature> Targets);

    /// <summary>
    ///     <para xml:lang="en">Capability base for orb-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属充能球行为的能力基类。</para>
    /// </summary>
    public abstract class OrbCapability : OwnerHookCapability<OrbModel>
    {
        internal Task NotifyOwnerOrbPassiveTriggered(OrbPassiveTriggerContext context)
        {
            return OnOwnerOrbPassiveTriggered(context);
        }

        internal Task NotifyOwnerOrbEvoked(OrbEvokeContext context)
        {
            return OnOwnerOrbEvoked(context);
        }

        internal Task NotifyOwnerOrbBeforeTurnEndTriggered(OrbBeforeTurnEndTriggerContext context)
        {
            return OnOwnerOrbBeforeTurnEndTriggered(context);
        }

        internal Task NotifyOwnerOrbAfterTurnStartTriggered(OrbAfterTurnStartTriggerContext context)
        {
            return OnOwnerOrbAfterTurnStartTriggered(context);
        }

        /// <summary>
        ///     <para xml:lang="en">Called after this capability's owning orb passive has triggered.</para>
        ///     <para xml:lang="zh-CN">此能力所属充能球被动触发后调用。</para>
        /// </summary>
        protected virtual Task OnOwnerOrbPassiveTriggered(OrbPassiveTriggerContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Called after this capability's owning orb has been evoked.</para>
        ///     <para xml:lang="zh-CN">此能力所属充能球被激发后调用。</para>
        /// </summary>
        protected virtual Task OnOwnerOrbEvoked(OrbEvokeContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Called after this capability's owning orb before-turn-end trigger method has run.</para>
        ///     <para xml:lang="zh-CN">此能力所属充能球的回合结束前触发方法运行后调用。</para>
        /// </summary>
        protected virtual Task OnOwnerOrbBeforeTurnEndTriggered(OrbBeforeTurnEndTriggerContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Called after this capability's owning orb after-turn-start trigger method has run.</para>
        ///     <para xml:lang="zh-CN">此能力所属充能球的回合开始后触发方法运行后调用。</para>
        /// </summary>
        protected virtual Task OnOwnerOrbAfterTurnStartTriggered(OrbAfterTurnStartTriggerContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Capability base for enchantment-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属附魔行为的能力基类。</para>
    /// </summary>
    public abstract class EnchantmentCapability : OwnerHookCapability<EnchantmentModel>;

    /// <summary>
    ///     <para xml:lang="en">Capability base for affliction-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属苦痛行为的能力基类。</para>
    /// </summary>
    public abstract class AfflictionCapability : OwnerHookCapability<AfflictionModel>;

    /// <summary>
    ///     <para xml:lang="en">Capability base for monster-owned behavior.</para>
    ///     <para xml:lang="zh-CN">用于所属怪物行为的能力基类。</para>
    /// </summary>
    public abstract class MonsterCapability : OwnerHookCapability<MonsterModel>;

    /// <summary>
    ///     <para xml:lang="en">Capability base for character-owned state and display capabilities.</para>
    ///     <para xml:lang="zh-CN">用于所属角色状态及显示行为的能力基类。</para>
    /// </summary>
    public abstract class CharacterCapability : ModelCapability<CharacterModel>;

    /// <summary>
    ///     <para xml:lang="en">Card capability base that handles plays of its owning card.</para>
    ///     <para xml:lang="zh-CN">处理所属卡牌打出事件的卡牌能力基类。</para>
    /// </summary>
    public abstract class CardPlayCapability : CardCapability, ICardOnPlayHookListener
    {
        /// <inheritdoc />
        public Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context)
        {
            return ShouldHandleCardPlay(context.CardPlay)
                ? BeforeOwnerCardOnPlay(context.ChoiceContext, context.CardPlay)
                : Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
            return NotifyOwnerCardPlayed(context.ChoiceContext, context.CardPlay, context.OriginalOnPlayRan);
        }

        /// <inheritdoc />
        [Obsolete("Use AfterCardOnPlay(AfterCardOnPlayContext) instead.")]
        public Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            return NotifyOwnerCardPlayed(context.ChoiceContext, context.CardPlay);
        }

        internal Task NotifyOwnerCardPlayed(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            bool originalOnPlayRan = true)
        {
            return ShouldHandleCardPlay(cardPlay)
                ? OnOwnerCardPlayed(choiceContext, cardPlay, originalOnPlayRan)
                : Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when this capability should handle <paramref name="cardPlay" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回此能力是否应处理 <paramref name="cardPlay" />。</para>
        /// </summary>
        protected virtual bool ShouldHandleCardPlay(CardPlay cardPlay)
        {
            return Owner != null && ReferenceEquals(cardPlay.Card, Owner);
        }

        /// <summary>
        ///     <para xml:lang="en">Called after the owning card's <c>OnPlay</c> body completes.</para>
        ///     <para xml:lang="zh-CN">所属卡牌的 <c>OnPlay</c> 主体完成后调用。</para>
        /// </summary>
        protected abstract Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Called after the point where the owning card's <c>OnPlay</c> body would run.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在所属卡牌的 <c>OnPlay</c> 主体原本应运行的位置之后调用。
        ///     </para>
        /// </summary>
        protected virtual Task OnOwnerCardPlayed(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            bool originalOnPlayRan)
        {
            return originalOnPlayRan
                ? OnOwnerCardPlayed(choiceContext, cardPlay)
                : Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs before the owning card's <c>OnPlay</c> body. Return <see langword="true" /> to suppress the
        ///         original body.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在所属卡牌的 <c>OnPlay</c> 主体前运行。返回 <see langword="true" /> 可阻止原始主体执行。
        ///     </para>
        /// </summary>
        protected virtual Task<bool> BeforeOwnerCardOnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Card capability that removes itself after the owning card is played once.</para>
    ///     <para xml:lang="zh-CN">所属卡牌打出一次后自动移除自身的卡牌能力。</para>
    /// </summary>
    public abstract class OneShotCardPlayCapability : CardPlayCapability
    {
        /// <inheritdoc />
        protected sealed override async Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            try
            {
                await OnOwnerCardPlayedOnce(choiceContext, cardPlay);
            }
            finally
            {
                RemoveFromOwner();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Called once after the owning card's <c>OnPlay</c> body completes, before the capability removes itself.
        ///     </para>
        ///     <para xml:lang="zh-CN">所属卡牌的 <c>OnPlay</c> 主体完成后调用一次，随后能力会移除自身。</para>
        /// </summary>
        protected abstract Task OnOwnerCardPlayedOnce(PlayerChoiceContext choiceContext, CardPlay cardPlay);
    }

    /// <summary>
    ///     <para xml:lang="en">Owner-hook capability that removes itself after combat ends.</para>
    ///     <para xml:lang="zh-CN">战斗结束后自动移除自身的所属模型钩子能力。</para>
    /// </summary>
    public abstract class UntilCombatEndCapability<TModel> : OwnerHookCapability<TModel>
        where TModel : AbstractModel
    {
        /// <inheritdoc />
        public override async Task AfterCombatEnd(CombatRoom room)
        {
            try
            {
                await OnCombatEnded(room);
            }
            finally
            {
                RemoveFromOwner();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Called when combat ends, before the capability removes itself.</para>
        ///     <para xml:lang="zh-CN">战斗结束时调用，随后能力会移除自身。</para>
        /// </summary>
        protected virtual Task OnCombatEnded(CombatRoom room)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Owner-hook capability with a saved turn counter that removes itself when the counter reaches zero.</para>
    ///     <para xml:lang="zh-CN">保存回合计数并在计数归零后自动移除自身的所属模型钩子能力。</para>
    /// </summary>
    public abstract class TurnLimitedCapability<TModel> : OwnerHookCapability<TModel>
        where TModel : AbstractModel
    {
        private const string RemainingTurnsKey = "remainingTurns";

        /// <summary>
        ///     <para xml:lang="en">Creates a capability with one remaining turn.</para>
        ///     <para xml:lang="zh-CN">创建剩余一回合的能力。</para>
        /// </summary>
        protected TurnLimitedCapability()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a capability with <paramref name="remainingTurns" /> remaining turns.</para>
        ///     <para xml:lang="zh-CN">创建剩余 <paramref name="remainingTurns" /> 回合的能力。</para>
        /// </summary>
        protected TurnLimitedCapability(int remainingTurns)
        {
            SetRemainingTurns(remainingTurns);
        }

        /// <summary>
        ///     <para xml:lang="en">Remaining turn ticks before this capability removes itself.</para>
        ///     <para xml:lang="zh-CN">此能力移除自身前剩余的回合计数。</para>
        /// </summary>
        public int RemainingTurns { get; private set; } = 1;

        /// <inheritdoc />
        protected override JsonNode SaveAdditionalState()
        {
            return new JsonObject
            {
                [RemainingTurnsKey] = RemainingTurns,
            };
        }

        /// <inheritdoc />
        protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
        {
            RemainingTurns = ReadRemainingTurns(state);
        }

#if STS2_AT_LEAST_0_106_0
        /// <inheritdoc />
        public override Task AfterSideTurnEnd(
            PlayerChoiceContext choiceContext,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            return AfterTurnLimitTurnEnded(choiceContext, side);
        }
#else
        /// <inheritdoc />
        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            await AfterTurnLimitTurnEnded(choiceContext, side);
        }
#endif

        private async Task AfterTurnLimitTurnEnded(PlayerChoiceContext choiceContext, CombatSide side)
        {
            if (RemainingTurns <= 0 || !ShouldTickTurnLimit(choiceContext, side))
                return;

            RemainingTurns--;
            MarkDirty();

            try
            {
                await OnTurnLimitTicked(choiceContext, side, RemainingTurns);
            }
            finally
            {
                if (RemainingTurns <= 0)
                    try
                    {
                        await OnTurnLimitExpired(choiceContext, side);
                    }
                    finally
                    {
                        RemoveFromOwner();
                    }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the remaining turn count and marks the capability dirty when attached.</para>
        ///     <para xml:lang="zh-CN">设置剩余回合数，并在已附加时标记能力变更。</para>
        /// </summary>
        protected void SetRemainingTurns(int remainingTurns)
        {
            if (remainingTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingTurns), remainingTurns,
                    "Remaining turns cannot be negative.");

            RemainingTurns = remainingTurns;
            MarkDirty();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when this turn-end hook should decrement the remaining count.
        ///     </para>
        ///     <para xml:lang="zh-CN">确定此回合结束钩子是否应减少剩余计数。</para>
        /// </summary>
        protected virtual bool ShouldTickTurnLimit(PlayerChoiceContext choiceContext, CombatSide side)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Called after a turn tick decrements the remaining count.</para>
        ///     <para xml:lang="zh-CN">每次回合计数减少后调用。</para>
        /// </summary>
        protected virtual Task OnTurnLimitTicked(
            PlayerChoiceContext choiceContext,
            CombatSide side,
            int remainingTurns)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Called when the turn counter reaches zero, before the capability removes itself.</para>
        ///     <para xml:lang="zh-CN">回合计数归零时调用，随后能力会移除自身。</para>
        /// </summary>
        protected virtual Task OnTurnLimitExpired(PlayerChoiceContext choiceContext, CombatSide side)
        {
            return Task.CompletedTask;
        }

        private static int ReadRemainingTurns(JsonNode? state)
        {
            if (state is not JsonObject obj ||
                !obj.TryGetPropertyValue(RemainingTurnsKey, out var remainingTurnsNode) ||
                remainingTurnsNode == null)
                return 1;

            var remainingTurns = remainingTurnsNode.GetValue<int>();
            return Math.Max(remainingTurns, 0);
        }
    }
}
