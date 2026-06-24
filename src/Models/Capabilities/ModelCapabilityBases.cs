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
    ///     Typed model capability base that opts into the owning model's vanilla hook listener stream when that owner
    ///     participates in vanilla hooks.
    ///     当 owner 参与原版 hook 时，会插入所属模型原版 hook listener 流的类型化模型能力基类。
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
    ///     Capability base for card-owned behavior and capability-owned card dynamic vars.
    ///     卡牌 owner 行为与能力自有卡牌动态变量基类。
    /// </summary>
    public abstract class CardCapability : OwnerHookCapability<CardModel>
    {
        /// <summary>
        ///     Called after the owning card's vanilla upgrade body has run.
        ///     所属卡牌的原版升级主体执行后调用。
        /// </summary>
        protected virtual void OnOwnerCardUpgraded(CardModel card)
        {
        }

        /// <summary>
        ///     Called after the owning card finalizes upgrade highlights.
        ///     所属卡牌完成升级高亮收尾后调用。
        /// </summary>
        protected virtual void OnOwnerCardUpgradeFinalized(CardModel card)
        {
        }

        /// <summary>
        ///     Called after the owning card's vanilla downgrade hook has run.
        ///     所属卡牌的原版降级钩子执行后调用。
        /// </summary>
        protected virtual void OnOwnerCardDowngraded(CardModel card)
        {
        }

        /// <summary>
        ///     Called after the owning card has been transformed from.
        ///     所属卡牌被转化离开后调用。
        /// </summary>
        protected virtual void OnOwnerCardTransformedFrom(CardModel card)
        {
        }

        /// <summary>
        ///     Called after the owning card has been transformed to.
        ///     所属卡牌作为转化结果进入后调用。
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
    ///     Card base with a protected default-capability hook.
    ///     带受保护默认能力钩子的卡牌基类。
    /// </summary>
    public abstract class CapabilityCardModel : CardModel, IModelCapabilitySource
    {
        /// <summary>
        ///     Creates a card model with default-capability support.
        ///     创建支持默认能力的卡牌模型。
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
        ///     Adds this card's own default capabilities.
        ///     添加此卡牌自身的默认能力。
        /// </summary>
        protected virtual void BuildDefaultCapabilities(ModelCapabilityList capabilities)
        {
        }
    }

    /// <summary>
    ///     Capability base for relic-owned behavior.
    ///     遗物 owner 行为能力基类。
    /// </summary>
    public abstract class RelicCapability : OwnerHookCapability<RelicModel>;

    /// <summary>
    ///     Capability base for potion-owned behavior.
    ///     药水 owner 行为能力基类。
    /// </summary>
    public abstract class PotionCapability : OwnerHookCapability<PotionModel>;

    /// <summary>
    ///     Capability base for power-owned behavior.
    ///     能力 owner 行为能力基类。
    /// </summary>
    public abstract class PowerCapability : OwnerHookCapability<PowerModel>;

    /// <summary>
    ///     Context passed after the owning orb's passive has triggered.
    ///     所属充能球被动触发后传入的上下文。
    /// </summary>
    public readonly record struct OrbPassiveTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext,
        Creature? Target);

    /// <summary>
    ///     Context passed after the owning orb's before-turn-end trigger method has run.
    ///     所属充能球的回合结束前触发方法运行后传入的上下文。
    /// </summary>
    public readonly record struct OrbBeforeTurnEndTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext);

    /// <summary>
    ///     Context passed after the owning orb's after-turn-start trigger method has run.
    ///     所属充能球的回合开始后触发方法运行后传入的上下文。
    /// </summary>
    public readonly record struct OrbAfterTurnStartTriggerContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext);

    /// <summary>
    ///     Context passed after the owning orb has been evoked.
    ///     所属充能球被激发后传入的上下文。
    /// </summary>
    public readonly record struct OrbEvokeContext(
        OrbModel Orb,
        PlayerChoiceContext ChoiceContext,
        IReadOnlyList<Creature> Targets);

    /// <summary>
    ///     Capability base for orb-owned behavior.
    ///     充能球 owner 行为能力基类。
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
        ///     Called after this capability's owning orb passive has triggered.
        ///     此能力所属充能球被动触发后调用。
        /// </summary>
        protected virtual Task OnOwnerOrbPassiveTriggered(OrbPassiveTriggerContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Called after this capability's owning orb has been evoked.
        ///     此能力所属充能球被激发后调用。
        /// </summary>
        protected virtual Task OnOwnerOrbEvoked(OrbEvokeContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Called after this capability's owning orb before-turn-end trigger method has run.
        ///     此能力所属充能球的回合结束前触发方法运行后调用。
        /// </summary>
        protected virtual Task OnOwnerOrbBeforeTurnEndTriggered(OrbBeforeTurnEndTriggerContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Called after this capability's owning orb after-turn-start trigger method has run.
        ///     此能力所属充能球的回合开始后触发方法运行后调用。
        /// </summary>
        protected virtual Task OnOwnerOrbAfterTurnStartTriggered(OrbAfterTurnStartTriggerContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Capability base for enchantment-owned behavior.
    ///     附魔 owner 行为能力基类。
    /// </summary>
    public abstract class EnchantmentCapability : OwnerHookCapability<EnchantmentModel>;

    /// <summary>
    ///     Capability base for affliction-owned behavior.
    ///     苦痛 owner 行为能力基类。
    /// </summary>
    public abstract class AfflictionCapability : OwnerHookCapability<AfflictionModel>;

    /// <summary>
    ///     Capability base for monster-owned behavior.
    ///     怪物 owner 行为能力基类。
    /// </summary>
    public abstract class MonsterCapability : OwnerHookCapability<MonsterModel>;

    /// <summary>
    ///     Capability base for character-owned state and display capabilities.
    ///     角色 owner 状态与展示能力基类。
    /// </summary>
    public abstract class CharacterCapability : ModelCapability<CharacterModel>;

    /// <summary>
    ///     Card capability base that handles plays of its owning card.
    ///     处理所属卡牌打出事件的卡牌能力基类。
    /// </summary>
    public abstract class CardPlayCapability : CardCapability, ICardOnPlayHookListener
    {
        /// <inheritdoc />
        public Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            return NotifyOwnerCardPlayed(context.ChoiceContext, context.CardPlay);
        }

        internal Task NotifyOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            return ShouldHandleCardPlay(cardPlay)
                ? OnOwnerCardPlayed(choiceContext, cardPlay)
                : Task.CompletedTask;
        }

        /// <summary>
        ///     Returns true when this capability should handle <paramref name="cardPlay" />.
        ///     返回此能力是否应处理 <paramref name="cardPlay" />。
        /// </summary>
        protected virtual bool ShouldHandleCardPlay(CardPlay cardPlay)
        {
            return Owner != null && ReferenceEquals(cardPlay.Card, Owner);
        }

        /// <summary>
        ///     Called after the owning card's OnPlay body completes.
        ///     所属卡牌的 OnPlay 主体完成后调用。
        /// </summary>
        protected abstract Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay);
    }

    /// <summary>
    ///     Card capability that removes itself after the owning card is played once.
    ///     所属卡牌打出一次后自动移除自身的卡牌能力。
    /// </summary>
    public abstract class OneShotCardPlayCapability : CardPlayCapability
    {
        /// <inheritdoc />
        protected sealed override async Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await OnOwnerCardPlayedOnce(choiceContext, cardPlay);
            RemoveFromOwner();
        }

        /// <summary>
        ///     Called once after the owning card's OnPlay body completes, before the capability removes itself.
        ///     所属卡牌的 OnPlay 主体完成后调用一次，随后能力会移除自身。
        /// </summary>
        protected abstract Task OnOwnerCardPlayedOnce(PlayerChoiceContext choiceContext, CardPlay cardPlay);
    }

    /// <summary>
    ///     Owner-hook capability that removes itself after combat ends.
    ///     战斗结束后自动移除自身的 owner-hook 能力。
    /// </summary>
    public abstract class UntilCombatEndCapability<TModel> : OwnerHookCapability<TModel>
        where TModel : AbstractModel
    {
        /// <inheritdoc />
        public override async Task AfterCombatEnd(CombatRoom room)
        {
            await OnCombatEnded(room);
            RemoveFromOwner();
        }

        /// <summary>
        ///     Called when combat ends, before the capability removes itself.
        ///     战斗结束时调用，随后能力会移除自身。
        /// </summary>
        protected virtual Task OnCombatEnded(CombatRoom room)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Owner-hook capability with a saved turn counter that removes itself when the counter reaches zero.
    ///     带保存回合计数、计数归零后自动移除自身的 owner-hook 能力。
    /// </summary>
    public abstract class TurnLimitedCapability<TModel> : OwnerHookCapability<TModel>
        where TModel : AbstractModel
    {
        private const string RemainingTurnsKey = "remainingTurns";

        /// <summary>
        ///     Creates a capability with one remaining turn.
        ///     创建剩余一回合的能力。
        /// </summary>
        protected TurnLimitedCapability()
        {
        }

        /// <summary>
        ///     Creates a capability with <paramref name="remainingTurns" /> remaining turns.
        ///     创建剩余 <paramref name="remainingTurns" /> 回合的能力。
        /// </summary>
        protected TurnLimitedCapability(int remainingTurns)
        {
            SetRemainingTurns(remainingTurns);
        }

        /// <summary>
        ///     Remaining turn ticks before this capability removes itself.
        ///     此能力移除自身前剩余的回合 tick 数。
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

            await OnTurnLimitTicked(choiceContext, side, RemainingTurns);
            if (RemainingTurns > 0)
                return;

            await OnTurnLimitExpired(choiceContext, side);
            RemoveFromOwner();
        }

        /// <summary>
        ///     Sets the remaining turn count and marks the capability dirty when attached.
        ///     设置剩余回合数，并在已附加时标记能力变更。
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
        ///     Returns true when this turn-end hook should decrement the remaining count.
        ///     返回此 turn-end hook 是否应减少剩余计数。
        /// </summary>
        protected virtual bool ShouldTickTurnLimit(PlayerChoiceContext choiceContext, CombatSide side)
        {
            return true;
        }

        /// <summary>
        ///     Called after a turn tick decrements the remaining count.
        ///     回合 tick 减少剩余计数后调用。
        /// </summary>
        protected virtual Task OnTurnLimitTicked(
            PlayerChoiceContext choiceContext,
            CombatSide side,
            int remainingTurns)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Called when the turn counter reaches zero, before the capability removes itself.
        ///     回合计数归零时调用，随后能力会移除自身。
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
