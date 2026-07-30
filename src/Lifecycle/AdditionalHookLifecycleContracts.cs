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
    ///     <para xml:lang="en">An attack is about to resolve.</para>
    ///     <para xml:lang="zh-CN">一次攻击即将结算。</para>
    /// </summary>
    public readonly record struct AttackStartingEvent(
        CombatStateCompat CombatState,
        AttackCommand Attack,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">An attack has finished resolving.</para>
    ///     <para xml:lang="zh-CN">一次攻击已完成结算。</para>
    /// </summary>
    public readonly record struct AttackEndedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext? ChoiceContext,
        AttackCommand Attack,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature is about to gain Block.</para>
    ///     <para xml:lang="zh-CN">一名生物即将获得格挡。</para>
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
    ///     <para xml:lang="en">A creature has gained Block.</para>
    ///     <para xml:lang="zh-CN">一名生物已获得格挡。</para>
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
    ///     <para xml:lang="en">A creature's Block has been broken.</para>
    ///     <para xml:lang="zh-CN">一名生物的格挡已被击破。</para>
    /// </summary>
    public readonly record struct BlockBrokenEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature's Block has been cleared.</para>
    ///     <para xml:lang="zh-CN">一名生物的格挡已被清除。</para>
    /// </summary>
    public readonly record struct BlockClearedEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card is about to be played automatically.</para>
    ///     <para xml:lang="zh-CN">一张牌即将被自动打出。</para>
    /// </summary>
    public readonly record struct CardAutoPlayingEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        Creature? Target,
        AutoPlayType AutoPlayType,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card has entered combat.</para>
    ///     <para xml:lang="zh-CN">一张牌已进入战斗。</para>
    /// </summary>
    public readonly record struct CardEnteredCombatEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card has been generated during combat.</para>
    ///     <para xml:lang="zh-CN">一张牌已在战斗中生成。</para>
    /// </summary>
    public readonly record struct CardGeneratedForCombatEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        Player? Creator,
        bool? AddedByPlayer,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card is about to be removed from the run.</para>
    ///     <para xml:lang="zh-CN">一张牌即将从一局游戏中移除。</para>
    /// </summary>
    public readonly record struct CardRemovingEvent(
        IRunState RunState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature has been added to combat.</para>
    ///     <para xml:lang="zh-CN">一名生物已加入战斗。</para>
    /// </summary>
    public readonly record struct CreatureAddedToCombatEvent(
        CombatStateCompat CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature's current HP has changed.</para>
    ///     <para xml:lang="zh-CN">一名生物的当前生命值已发生变化。</para>
    /// </summary>
    public readonly record struct CurrentHpChangedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        decimal Delta,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player has gained Energy.</para>
    ///     <para xml:lang="zh-CN">一名玩家已获得能量。</para>
    /// </summary>
    public readonly record struct EnergyGainedEvent(
        CombatStateCompat CombatState,
        int Amount,
        Player Gainer,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's Energy has been reset.</para>
    ///     <para xml:lang="zh-CN">一名玩家的能量已被重置。</para>
    /// </summary>
    public readonly record struct EnergyResetEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Energy has been spent to play a card.</para>
    ///     <para xml:lang="zh-CN">打出一张牌所需的能量已被消耗。</para>
    /// </summary>
    public readonly record struct EnergySpentEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        int Amount,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's hand draw is about to begin.</para>
    ///     <para xml:lang="zh-CN">一名玩家的手牌抽取即将开始。</para>
    /// </summary>
    public readonly record struct HandDrawingEvent(
        CombatStateCompat CombatState,
        Player Player,
        PlayerChoiceContext ChoiceContext,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's hand has become empty.</para>
    ///     <para xml:lang="zh-CN">一名玩家的手牌已变为空。</para>
    /// </summary>
    public readonly record struct HandEmptiedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's turn has started.</para>
    ///     <para xml:lang="zh-CN">一名玩家的回合已开始。</para>
    /// </summary>
    public readonly record struct PlayerTurnStartedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A potion is about to be used.</para>
    ///     <para xml:lang="zh-CN">一瓶药水即将被使用。</para>
    /// </summary>
    public readonly record struct PotionUsingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        Creature? Target,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A potion has been used.</para>
    ///     <para xml:lang="zh-CN">一瓶药水已被使用。</para>
    /// </summary>
    public readonly record struct PotionUsedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        Creature? Target,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's draw pile has been shuffled.</para>
    ///     <para xml:lang="zh-CN">一名玩家的抽牌堆已被洗牌。</para>
    /// </summary>
    public readonly record struct ShuffledEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Shuffler,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player has gained Stars.</para>
    ///     <para xml:lang="zh-CN">一名玩家已获得星星。</para>
    /// </summary>
    public readonly record struct StarsGainedEvent(
        CombatStateCompat CombatState,
        int Amount,
        Player Gainer,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player has spent Stars.</para>
    ///     <para xml:lang="zh-CN">一名玩家已消耗星星。</para>
    /// </summary>
    public readonly record struct StarsSpentEvent(
        CombatStateCompat CombatState,
        int Amount,
        Player Spender,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A summon has finished resolving.</para>
    ///     <para xml:lang="zh-CN">一次召唤已完成结算。</para>
    /// </summary>
    public readonly record struct SummonedEvent(
        CombatStateCompat CombatState,
        PlayerChoiceContext ChoiceContext,
        Player Summoner,
        decimal Amount,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player has taken an extra turn.</para>
    ///     <para xml:lang="zh-CN">一名玩家已进行一个额外回合。</para>
    /// </summary>
    public readonly record struct ExtraTurnTakenEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A combat side's turn is about to end.</para>
    ///     <para xml:lang="zh-CN">战斗中一方的回合即将结束。</para>
    /// </summary>
    public readonly record struct SideTurnEndingEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        IReadOnlyCollection<Creature>? Participants,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A combat side's turn has ended.</para>
    ///     <para xml:lang="zh-CN">战斗中一方的回合已结束。</para>
    /// </summary>
    public readonly record struct SideTurnEndedEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        IReadOnlyCollection<Creature>? Participants,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player has purchased a merchant item.</para>
    ///     <para xml:lang="zh-CN">一名玩家已购买一件商人物品。</para>
    /// </summary>
    public readonly record struct ItemPurchasedEvent(
        IRunState RunState,
        Player Player,
        MerchantEntry ItemPurchased,
        int GoldSpent,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">An Act map has been generated.</para>
    ///     <para xml:lang="zh-CN">一张章节地图已生成。</para>
    /// </summary>
    public readonly record struct MapGeneratedEvent(
        IRunState RunState,
        ActMap Map,
        int ActIndex,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A Rest Site healing action has finished resolving.</para>
    ///     <para xml:lang="zh-CN">休息处的治疗操作已完成结算。</para>
    /// </summary>
    public readonly record struct RestSiteHealedEvent(
        IRunState RunState,
        Player Player,
        bool IsMimicked,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A Rest Site Smith action has finished resolving.</para>
    ///     <para xml:lang="zh-CN">休息处的锻造操作已完成结算。</para>
    /// </summary>
    public readonly record struct RestSiteSmithedEvent(
        IRunState RunState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
