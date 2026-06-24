#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace STS2RitsuLib
{
    /// <summary>
    ///     An attack hook is about to resolve.
    ///     攻击 hook 即将结算。
    /// </summary>
    public readonly record struct AttackStartingEvent(
        CombatStateCompat CombatState,
        AttackCommand Attack,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     An attack hook has resolved.
    ///     攻击 hook 已结算。
    /// </summary>
    public readonly record struct AttackEndedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext? ChoiceContext,
        AttackCommand Attack,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Block gain is about to be processed.
    ///     格挡获得即将处理。
    /// </summary>
    public readonly record struct BlockGainingEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        decimal Amount,
        ValueProp Props,
        CardModel? CardSource,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Block gain has been processed.
    ///     格挡获得已处理。
    /// </summary>
    public readonly record struct BlockGainedEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        decimal Amount,
        ValueProp Props,
        CardModel? CardSource,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature's block was broken.
    ///     生物格挡被打破。
    /// </summary>
    public readonly record struct BlockBrokenEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature's block was cleared.
    ///     生物格挡被清除。
    /// </summary>
    public readonly record struct BlockClearedEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card is about to be auto-played.
    ///     卡牌即将自动打出。
    /// </summary>
    public readonly record struct CardAutoPlayingEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        Creature? Target,
        AutoPlayType AutoPlayType,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card entered combat.
    ///     卡牌进入了战斗。
    /// </summary>
    public readonly record struct CardEnteredCombatEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was generated during combat.
    ///     战斗中生成了卡牌。
    /// </summary>
    public readonly record struct CardGeneratedForCombatEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        Player? Creator,
        bool? AddedByPlayer,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card is about to be removed from the run.
    ///     卡牌即将从跑局中移除。
    /// </summary>
    public readonly record struct CardRemovingEvent(
        IRunState RunState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature was added to combat.
    ///     生物已加入战斗。
    /// </summary>
    public readonly record struct CreatureAddedToCombatEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature's current HP changed.
    ///     生物当前生命发生变化。
    /// </summary>
    public readonly record struct CurrentHpChangedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        decimal Delta,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player energy was reset.
    ///     玩家能量已重置。
    /// </summary>
    public readonly record struct EnergyResetEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Energy was spent for a card.
    ///     为卡牌消耗了能量。
    /// </summary>
    public readonly record struct EnergySpentEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        int Amount,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A hand draw is about to run.
    ///     手牌抽牌即将执行。
    /// </summary>
    public readonly record struct HandDrawingEvent(
        CombatStateCompat CombatState,
        Player Player,
        PlayerChoiceContext ChoiceContext,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A hand became empty.
    ///     手牌已清空。
    /// </summary>
    public readonly record struct HandEmptiedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A player turn has started.
    ///     玩家回合已开始。
    /// </summary>
    public readonly record struct PlayerTurnStartedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A potion is about to be used.
    ///     药水即将使用。
    /// </summary>
    public readonly record struct PotionUsingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        Creature? Target,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A potion was used.
    ///     药水已使用。
    /// </summary>
    public readonly record struct PotionUsedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        Creature? Target,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A player's deck was shuffled.
    ///     玩家牌组已洗牌。
    /// </summary>
    public readonly record struct ShuffledEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Shuffler,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Stars were gained.
    ///     星星已获得。
    /// </summary>
    public readonly record struct StarsGainedEvent(
        CombatStateCompat CombatState,
        int Amount,
        Player Gainer,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Stars were spent.
    ///     星星已消耗。
    /// </summary>
    public readonly record struct StarsSpentEvent(
        CombatStateCompat CombatState,
        int Amount,
        Player Spender,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A summon hook resolved.
    ///     召唤 hook 已结算。
    /// </summary>
    public readonly record struct SummonedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Summoner,
        decimal Amount,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A player took an extra turn.
    ///     玩家获得了额外回合。
    /// </summary>
    public readonly record struct ExtraTurnTakenEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side's turn is about to end.
    ///     某一方回合即将结束。
    /// </summary>
    public readonly record struct SideTurnEndingEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        IReadOnlyCollection<Creature>? Participants,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side's turn ended.
    ///     某一方回合已结束。
    /// </summary>
    public readonly record struct SideTurnEndedEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        IReadOnlyCollection<Creature>? Participants,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A merchant item was purchased.
    ///     已购买商店物品。
    /// </summary>
    public readonly record struct ItemPurchasedEvent(
        IRunState RunState,
        Player Player,
        MerchantEntry ItemPurchased,
        int GoldSpent,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     An act map was generated.
    ///     章节地图已生成。
    /// </summary>
    public readonly record struct MapGeneratedEvent(
        IRunState RunState,
        ActMap Map,
        int ActIndex,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A rest-site heal resolved.
    ///     休息点治疗已结算。
    /// </summary>
    public readonly record struct RestSiteHealedEvent(
        IRunState RunState,
        Player Player,
        bool IsMimicked,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A rest-site smith resolved.
    ///     休息点锻造已结算。
    /// </summary>
    public readonly record struct RestSiteSmithedEvent(
        IRunState RunState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
