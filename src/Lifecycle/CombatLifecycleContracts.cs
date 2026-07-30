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
    ///     <para xml:lang="en">A combat encounter is about to start or resume.</para>
    ///     <para xml:lang="zh-CN">一场战斗即将开始或恢复。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when available.</para>
    ///     <para xml:lang="zh-CN">可用时为战斗状态。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CombatStartingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A combat encounter has ended, regardless of its outcome.</para>
    ///     <para xml:lang="zh-CN">一场战斗已经结束，不限战斗结果。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when available.</para>
    ///     <para xml:lang="zh-CN">可用时为战斗状态。</para>
    /// </param>
    /// <param name="Room">
    ///     <para xml:lang="en">Room that hosted the combat.</para>
    ///     <para xml:lang="zh-CN">承载该战斗的房间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CombatEndedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">The players have won a combat encounter.</para>
    ///     <para xml:lang="zh-CN">玩家已赢得一场战斗。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when available.</para>
    ///     <para xml:lang="zh-CN">可用时为战斗状态。</para>
    /// </param>
    /// <param name="Room">
    ///     <para xml:lang="en">Room that hosted the combat.</para>
    ///     <para xml:lang="zh-CN">承载该战斗的房间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CombatVictoryEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A combat side's turn is about to begin.</para>
    ///     <para xml:lang="zh-CN">战斗中一方的回合即将开始。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Side">
    ///     <para xml:lang="en">Side whose turn is starting.</para>
    ///     <para xml:lang="zh-CN">即将开始回合的一方。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct SideTurnStartingEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A combat side's turn has started.</para>
    ///     <para xml:lang="zh-CN">战斗中一方的回合已经开始。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Side">
    ///     <para xml:lang="en">Side that is now active.</para>
    ///     <para xml:lang="zh-CN">当前处于活动状态的一方。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct SideTurnStartedEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card is being played, before its full resolution completes.</para>
    ///     <para xml:lang="zh-CN">一张牌正在被打出，此时其完整结算尚未完成。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="CardPlay">
    ///     <para xml:lang="en">Play context.</para>
    ///     <para xml:lang="zh-CN">出牌上下文。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardPlayingEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A played card has finished resolving.</para>
    ///     <para xml:lang="zh-CN">一张已打出的牌已完成结算。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="CardPlay">
    ///     <para xml:lang="en">Play context.</para>
    ///     <para xml:lang="zh-CN">出牌上下文。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardPlayedEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card has moved from one pile to another.</para>
    ///     <para xml:lang="zh-CN">一张牌已从一个牌堆移动到另一个牌堆。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when in combat.</para>
    ///     <para xml:lang="zh-CN">处于战斗中时的战斗状态。</para>
    /// </param>
    /// <param name="Card">
    ///     <para xml:lang="en">Card that moved.</para>
    ///     <para xml:lang="zh-CN">发生移动的卡牌。</para>
    /// </param>
    /// <param name="PreviousPile">
    ///     <para xml:lang="en">Source pile classification.</para>
    ///     <para xml:lang="zh-CN">来源牌堆分类。</para>
    /// </param>
    /// <param name="Source">
    ///     <para xml:lang="en">Optional model that caused the move.</para>
    ///     <para xml:lang="zh-CN">导致移动的可选模型。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
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
    ///     <para xml:lang="en">A card has been drawn.</para>
    ///     <para xml:lang="zh-CN">一张牌已被抽取。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Card">
    ///     <para xml:lang="en">Drawn card.</para>
    ///     <para xml:lang="zh-CN">被抽取的卡牌。</para>
    /// </param>
    /// <param name="FromHandDraw">
    ///     <para xml:lang="en">True when drawn via hand-draw rules.</para>
    ///     <para xml:lang="zh-CN">如果通过手牌抽牌规则抽取则为 true。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardDrawnEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool FromHandDraw,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card has been discarded.</para>
    ///     <para xml:lang="zh-CN">一张牌已被弃置。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Card">
    ///     <para xml:lang="en">Discarded card.</para>
    ///     <para xml:lang="zh-CN">被弃置的卡牌。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardDiscardedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A card has been Exhausted.</para>
    ///     <para xml:lang="zh-CN">一张牌已被消耗。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Card">
    ///     <para xml:lang="en">Exhausted card.</para>
    ///     <para xml:lang="zh-CN">被消耗的卡牌。</para>
    /// </param>
    /// <param name="CausedByEthereal">
    ///     <para xml:lang="en">True when ethereal timing caused the exhaust.</para>
    ///     <para xml:lang="zh-CN">如果因虚无时机导致消耗则为 true。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardExhaustedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool CausedByEthereal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's hand is about to be flushed.</para>
    ///     <para xml:lang="zh-CN">一名玩家的手牌即将被清理。</para>
    /// </summary>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Player">
    ///     <para xml:lang="en">Player whose hand is about to be flushed.</para>
    ///     <para xml:lang="zh-CN">手牌即将被清空的玩家。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct BeforeFlushEvent(
        CombatStateCompat CombatState,
        Player Player,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's hand has finished being flushed.</para>
    ///     <para xml:lang="zh-CN">一名玩家的手牌已完成清理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Raised from <c>Hook.AfterFlush</c> on host API 0.105.0 and later. Older host APIs do not
    ///         provide <c>Hook.AfterFlush</c>, so this event is not raised there.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在宿主 API 0.105.0 及更高版本中由 <c>Hook.AfterFlush</c> 触发。旧版宿主 API 不提供 <c>Hook.AfterFlush</c>
    ///         ，因此不会触发此事件。
    ///     </para>
    /// </remarks>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Active combat state.</para>
    ///     <para xml:lang="zh-CN">当前活动战斗状态。</para>
    /// </param>
    /// <param name="Player">
    ///     <para xml:lang="en">Player whose hand was flushed.</para>
    ///     <para xml:lang="zh-CN">手牌已被清空的玩家。</para>
    /// </param>
    /// <param name="FlushedCards">
    ///     <para xml:lang="en">Cards that left the hand during flush (non-retained).</para>
    ///     <para xml:lang="zh-CN">清空期间离开手牌的卡牌（非保留）。</para>
    /// </param>
    /// <param name="RetainedCards">
    ///     <para xml:lang="en">Cards that stayed in the hand (retain semantics).</para>
    ///     <para xml:lang="zh-CN">留在手牌中的卡牌（保留语义）。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CardsFlushedEvent(
        CombatStateCompat CombatState,
        Player Player,
        IReadOnlyCollection<CardModel> FlushedCards,
        IReadOnlyCollection<CardModel> RetainedCards,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature is beginning its death resolution.</para>
    ///     <para xml:lang="zh-CN">一名生物即将开始死亡结算。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when in combat.</para>
    ///     <para xml:lang="zh-CN">处于战斗中时的战斗状态。</para>
    /// </param>
    /// <param name="Creature">
    ///     <para xml:lang="en">Creature that is dying.</para>
    ///     <para xml:lang="zh-CN">正在死亡的生物。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct CreatureDyingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A creature has finished death resolution and may remain alive if removal was prevented.</para>
    ///     <para xml:lang="zh-CN">一名生物已完成死亡结算；若移除被阻止，其仍可能存活。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when in combat.</para>
    ///     <para xml:lang="zh-CN">处于战斗中时的战斗状态。</para>
    /// </param>
    /// <param name="Creature">
    ///     <para xml:lang="en">Creature that died or was spared.</para>
    ///     <para xml:lang="zh-CN">死亡或被豁免的生物。</para>
    /// </param>
    /// <param name="WasRemovalPrevented">
    ///     <para xml:lang="en">True if death was cancelled by effects.</para>
    ///     <para xml:lang="zh-CN">如果死亡被效果取消则为 true。</para>
    /// </param>
    /// <param name="DeathAnimationDurationSeconds">
    ///     <para xml:lang="en">Suggested VFX duration.</para>
    ///     <para xml:lang="zh-CN">建议的视觉效果持续时间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
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
