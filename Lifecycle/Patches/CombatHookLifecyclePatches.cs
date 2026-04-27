#if STS2_V_0_103_2
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes fine-grained combat, card, turn, pile, and creature death lifecycle events by patching
    ///     <see cref="Hook" /> before/after callbacks.
    /// </summary>
    public class CombatHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "combat_hook_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish fine-grained combat, card, turn, and death lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeCombatStart), [typeof(IRunState), typeof(CombatStateCompat)]),
                new(typeof(Hook), nameof(Hook.AfterCombatEnd),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(CombatRoom)]),
                new(typeof(Hook), nameof(Hook.AfterCombatVictory),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(CombatRoom)]),
                new(typeof(Hook), nameof(Hook.BeforeSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
                new(typeof(Hook), nameof(Hook.BeforeCardPlayed), [typeof(CombatStateCompat), typeof(CardPlay)]),
                new(typeof(Hook), nameof(Hook.AfterCardPlayed),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardPlay)]),
                new(typeof(Hook), nameof(Hook.AfterCardChangedPiles),
                [
                    typeof(IRunState), typeof(CombatStateCompat), typeof(CardModel), typeof(PileType),
                    typeof(AbstractModel),
                ]),
                new(typeof(Hook), nameof(Hook.AfterCardDrawn),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)]),
                new(typeof(Hook), nameof(Hook.AfterCardDiscarded),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel)]),
                new(typeof(Hook), nameof(Hook.AfterCardExhausted),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)]),
                new(typeof(Hook), nameof(Hook.AfterCardRetained), [typeof(CombatStateCompat), typeof(CardModel)]),
                new(typeof(Hook), nameof(Hook.BeforeDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature), typeof(bool), typeof(float)]),
            ];
        }

        /// <summary>
        ///     Harmony prefix: publishes synchronous lifecycle events for hook methods that run before combat, side turns,
        ///     card play, and creature death.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Prefix(MethodBase __originalMethod, object[] __args)
            // ReSharper restore InconsistentNaming
        {
            switch (__originalMethod.Name)
            {
                case nameof(Hook.BeforeCombatStart):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CombatStartingEvent(
                            (IRunState)__args[0],
                            (CombatStateCompat?)__args[1],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(CombatStartingEvent)
                    );
                    break;
                case nameof(Hook.BeforeSideTurnStart):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new SideTurnStartingEvent(
                            (CombatStateCompat)__args[0],
                            (CombatSide)__args[1],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(SideTurnStartingEvent)
                    );
                    break;
                case nameof(Hook.BeforeCardPlayed):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CardPlayingEvent(
                            (CombatStateCompat)__args[0],
                            (CardPlay)__args[1],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(CardPlayingEvent)
                    );
                    break;
                case nameof(Hook.BeforeDeath):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CreatureDyingEvent(
                            (IRunState)__args[0],
                            (CombatStateCompat?)__args[1],
                            (Creature)__args[2],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(CreatureDyingEvent)
                    );
                    break;
            }
        }

        /// <summary>
        ///     Harmony postfix: chains onto the original async <see cref="Task" /> and publishes lifecycle events after
        ///     combat end, victory, turns, card pile changes, and death resolution.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            __result = __originalMethod.Name switch
            {
                nameof(Hook.AfterCombatEnd) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CombatEndedEvent((IRunState)__args[0], (CombatStateCompat?)__args[1], (CombatRoom)__args[2],
                            DateTimeOffset.UtcNow), nameof(CombatEndedEvent))),
                nameof(Hook.AfterCombatVictory) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CombatVictoryEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                            (CombatRoom)__args[2],
                            DateTimeOffset.UtcNow), nameof(CombatVictoryEvent))),
                nameof(Hook.AfterSideTurnStart) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new SideTurnStartedEvent((CombatStateCompat)__args[0], (CombatSide)__args[1],
                            DateTimeOffset.UtcNow),
                        nameof(SideTurnStartedEvent))),
                nameof(Hook.AfterCardPlayed) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardPlayedEvent((CombatStateCompat)__args[0], (CardPlay)__args[2], DateTimeOffset.UtcNow),
                        nameof(CardPlayedEvent))),
                nameof(Hook.AfterCardChangedPiles) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardMovedBetweenPilesEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                            (CardModel)__args[2], (PileType)__args[3], (AbstractModel?)__args[4],
                            DateTimeOffset.UtcNow), nameof(CardMovedBetweenPilesEvent))),
                nameof(Hook.AfterCardDrawn) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardDrawnEvent((CombatStateCompat)__args[0], (CardModel)__args[2], (bool)__args[3],
                            DateTimeOffset.UtcNow), nameof(CardDrawnEvent))),
                nameof(Hook.AfterCardDiscarded) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardDiscardedEvent((CombatStateCompat)__args[0], (CardModel)__args[2],
                            DateTimeOffset.UtcNow),
                        nameof(CardDiscardedEvent))),
                nameof(Hook.AfterCardExhausted) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardExhaustedEvent((CombatStateCompat)__args[0], (CardModel)__args[2], (bool)__args[3],
                            DateTimeOffset.UtcNow), nameof(CardExhaustedEvent))),
                nameof(Hook.AfterCardRetained) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CardRetainedEvent((CombatStateCompat)__args[0], (CardModel)__args[1],
                            DateTimeOffset.UtcNow),
                        nameof(CardRetainedEvent))),
                nameof(Hook.AfterDeath) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new CreatureDiedEvent((IRunState)__args[0], (CombatStateCompat?)__args[1], (Creature)__args[2],
                            (bool)__args[3], (float)__args[4], DateTimeOffset.UtcNow), nameof(CreatureDiedEvent))),
                _ => __result,
            };
        }
    }
}
