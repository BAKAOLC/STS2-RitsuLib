#if STS2_V_0_103_2
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Combat is about to start (or resume).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when available.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CombatStartingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Combat has ended (any outcome).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when available.</param>
    /// <param name="Room">Room that hosted the combat.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CombatEndedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player won the combat.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when available.</param>
    /// <param name="Room">Room that hosted the combat.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CombatVictoryEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        CombatRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side’s turn is about to begin.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Side">Side whose turn is starting.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct SideTurnStartingEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A side’s turn has started.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Side">Side that is now active.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct SideTurnStartedEvent(
        CombatStateCompat CombatState,
        CombatSide Side,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card play is being resolved (before full resolution completes).
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="CardPlay">Play context.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardPlayingEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card play has finished resolving.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="CardPlay">Play context.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardPlayedEvent(
        CombatStateCompat CombatState,
        CardPlay CardPlay,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card moved between piles (draw, discard, exhaust, etc.).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when in combat.</param>
    /// <param name="Card">Card that moved.</param>
    /// <param name="PreviousPile">Source pile classification.</param>
    /// <param name="Source">Optional model that caused the move.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
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
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Card">Drawn card.</param>
    /// <param name="FromHandDraw">True when drawn via hand-draw rules.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardDrawnEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool FromHandDraw,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was discarded.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Card">Discarded card.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardDiscardedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was exhausted.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Card">Exhausted card.</param>
    /// <param name="CausedByEthereal">True when ethereal timing caused the exhaust.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardExhaustedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        bool CausedByEthereal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A card was retained for the next turn.
    /// </summary>
    /// <param name="CombatState">Active combat state.</param>
    /// <param name="Card">Retained card.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CardRetainedEvent(
        CombatStateCompat CombatState,
        CardModel Card,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A creature is dying (HP reached zero or equivalent).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when in combat.</param>
    /// <param name="Creature">Creature that is dying.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CreatureDyingEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Death resolution finished (may still be alive if removal was prevented).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when in combat.</param>
    /// <param name="Creature">Creature that died or was spared.</param>
    /// <param name="WasRemovalPrevented">True if death was cancelled by effects.</param>
    /// <param name="DeathAnimationDurationSeconds">Suggested VFX duration.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct CreatureDiedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        Creature Creature,
        bool WasRemovalPrevented,
        float DeathAnimationDurationSeconds,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
