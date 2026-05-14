#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Combat is about to start (or resume).
    ///     战斗即将开始（或恢复）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when available.
    ///     可用时为战斗状态。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CombatStartingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Combat has ended (any outcome).
    ///     战斗已结束（任意结果）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when available.
    ///     可用时为战斗状态。
    /// </param>
    /// <param name="Room">
    ///     Room that hosted the combat.
    ///     承载该战斗的房间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CombatEndedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player won the combat.
    ///     玩家赢得了战斗。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when available.
    ///     可用时为战斗状态。
    /// </param>
    /// <param name="Room">
    ///     Room that hosted the combat.
    ///     承载该战斗的房间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CombatVictoryEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side’s turn is about to begin.
    ///     某一方的回合即将开始。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Side">
    ///     Side whose turn is starting.
    ///     即将开始回合的一方。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct SideTurnStartingEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side’s turn has started.
    ///     某一方的回合已经开始。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Side">
    ///     Side that is now active.
    ///     当前处于活动状态的一方。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct SideTurnStartedEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card play is being resolved (before full resolution completes).
    ///     一次出牌正在结算中（完整结算完成前）。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="CardPlay">
    ///     Play context.
    ///     出牌上下文。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardPlayingEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card play has finished resolving.
    ///     一次出牌已完成结算。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="CardPlay">
    ///     Play context.
    ///     出牌上下文。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardPlayedEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card moved between piles (draw, discard, exhaust, etc.).
    ///     一张卡牌在牌堆之间移动（抽牌堆、弃牌堆、消耗堆等）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when in combat.
    ///     处于战斗中时的战斗状态。
    /// </param>
    /// <param name="Card">
    ///     Card that moved.
    ///     发生移动的卡牌。
    /// </param>
    /// <param name="PreviousPile">
    ///     Source pile classification.
    ///     来源牌堆分类。
    /// </param>
    /// <param name="Source">
    ///     Optional model that caused the move.
    ///     导致移动的可选模型。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardMovedBetweenPilesEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CardModel Card,
        PileType PreviousPile,
        AbstractModel? Source,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was drawn into a hand or similar pile.
    ///     一张卡牌被抽入手牌或类似牌堆。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Card">
    ///     Drawn card.
    ///     被抽取的卡牌。
    /// </param>
    /// <param name="FromHandDraw">
    ///     True when drawn via hand-draw rules.
    ///     如果通过手牌抽牌规则抽取则为 true。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardDrawnEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool FromHandDraw,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was discarded.
    ///     一张卡牌被弃置。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Card">
    ///     Discarded card.
    ///     被弃置的卡牌。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardDiscardedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was exhausted.
    ///     一张卡牌被消耗。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Card">
    ///     Exhausted card.
    ///     被消耗的卡牌。
    /// </param>
    /// <param name="CausedByEthereal">
    ///     True when ethereal timing caused the exhaust.
    ///     如果因虚无时机导致消耗则为 true。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardExhaustedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool CausedByEthereal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was retained for the next turn.
    ///     一张卡牌被保留到下一回合。
    /// </summary>
    /// <remarks>
    ///     On host API 0.105.0 and newer the underlying <c>Hook.AfterCardRetained</c> callback no longer exists; this event
    ///     is replayed per retained card from <c>Hook.AfterFlush</c> for backward compatibility. Subscribe to
    ///     <see cref="CardsFlushedEvent" /> instead to also observe the matching flushed cards and the player.
    ///     <see cref="CardsFlushedEvent" />。
    ///     在 host API 0.105.0 及更新版本中，底层 <c>Hook.AfterCardRetained</c> callback 已不存在；此事件
    ///     会为了向后兼容，从 <c>Hook.AfterFlush</c> 按每张保留卡牌 replay。请改为订阅
    ///     <see cref="CardsFlushedEvent" />，以同时观察匹配的 flushed 卡牌和玩家。
    ///     <see cref="CardsFlushedEvent" />。
    /// </remarks>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Card">
    ///     Retained card.
    ///     被保留的卡牌。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    [Obsolete(
        "Use CardsFlushedEvent. CardRetainedEvent is replayed from Hook.AfterFlush on host API 0.105.0 and newer.")]
    public readonly record struct CardRetainedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A flush sequence is about to run for the given player. Mirrors <c>Hook.BeforeFlush</c>.
    ///     指定玩家的清空序列即将运行。镜像 <c>Hook.BeforeFlush</c>。
    /// </summary>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Player">
    ///     Player whose hand is about to be flushed.
    ///     手牌即将被清空的玩家。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct BeforeFlushEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Hand flush completed for the given player.
    ///     指定玩家的手牌清空已完成。
    /// </summary>
    /// <remarks>
    ///     Fired from <c>Hook.AfterFlush</c> on host API 0.105.0 and newer. On older host APIs <c>Hook.AfterFlush</c> does
    ///     not exist and this event is not raised; use the legacy <see cref="CardRetainedEvent" /> there.
    ///     在宿主 API 0.105.0 及更新版本中由 <c>Hook.AfterFlush</c> 触发。在旧版宿主 API 中 <c>Hook.AfterFlush</c>
    ///     不存在，此事件不会触发；请在旧版中使用遗留的 <see cref="CardRetainedEvent" />。
    /// </remarks>
    /// <param name="CombatState">
    ///     Active combat state.
    ///     当前活动战斗状态。
    /// </param>
    /// <param name="Player">
    ///     Player whose hand was flushed.
    ///     手牌已被清空的玩家。
    /// </param>
    /// <param name="FlushedCards">
    ///     Cards that left the hand during flush (non-retained).
    ///     清空期间离开手牌的卡牌（非保留）。
    /// </param>
    /// <param name="RetainedCards">
    ///     Cards that stayed in the hand (retain semantics).
    ///     留在手牌中的卡牌（保留语义）。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CardsFlushedEvent(
        CombatStateCompat CombatState,
        Player Player,
        IReadOnlyCollection<CardModel> FlushedCards,
        IReadOnlyCollection<CardModel> RetainedCards,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature is dying (HP reached zero or equivalent).
    ///     一个生物正在死亡（生命值降至零或等效状态）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when in combat.
    ///     处于战斗中时的战斗状态。
    /// </param>
    /// <param name="Creature">
    ///     Creature that is dying.
    ///     正在死亡的生物。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CreatureDyingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Death resolution finished (may still be alive if removal was prevented).
    ///     死亡结算已完成（如果移除被阻止，目标可能仍然存活）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when in combat.
    ///     处于战斗中时的战斗状态。
    /// </param>
    /// <param name="Creature">
    ///     Creature that died or was spared.
    ///     死亡或被豁免的生物。
    /// </param>
    /// <param name="WasRemovalPrevented">
    ///     True if death was cancelled by effects.
    ///     如果死亡被效果取消则为 true。
    /// </param>
    /// <param name="DeathAnimationDurationSeconds">
    ///     Suggested VFX duration.
    ///     建议的视觉效果持续时间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct CreatureDiedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        bool WasRemovalPrevented,
        float DeathAnimationDurationSeconds,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
