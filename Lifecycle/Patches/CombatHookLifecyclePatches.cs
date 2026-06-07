#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes fine-grained combat, card, turn, pile, flush, and creature death lifecycle events by patching
    ///     <see cref="Hook" /> before/after callbacks.
    ///     通过 patch <see cref="Hook" /> 的 before/after callback，发布细粒度的战斗、卡牌、回合、牌堆、flush 和生物死亡生命周期事件。
    /// </summary>
    public class CombatHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "combat_hook_lifecycle";

        /// <inheritdoc />
        public static string Description =>
            "Publish fine-grained combat, card, turn, flush, and death lifecycle events";

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
#if STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.BeforeSideTurnStart),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IReadOnlyList<Creature>)]),
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IReadOnlyList<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.BeforeSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
#endif
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
                new(typeof(Hook), nameof(Hook.BeforeFlush), [typeof(CombatStateCompat), typeof(Player)]),
#if !STS2_AT_LEAST_0_105_0
                new(typeof(Hook), nameof(Hook.AfterCardRetained), [typeof(CombatStateCompat), typeof(CardModel)]),
#else
                new(typeof(Hook), nameof(Hook.AfterFlush),
                [
                    typeof(CombatStateCompat), typeof(Player), typeof(PlayerChoiceContext),
                    typeof(IReadOnlyCollection<CardModel>), typeof(IReadOnlyCollection<CardModel>),
                ]),
#endif
                new(typeof(Hook), nameof(Hook.BeforeDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature), typeof(bool), typeof(float)]),
            ];
        }

        /// <summary>
        ///     Harmony prefix: publishes synchronous lifecycle events for hook methods that run before combat, side turns,
        ///     card play, flush, and creature death.
        ///     Harmony prefix：为在战斗、side turn、
        ///     卡牌打出、flush 和生物死亡之前运行的 hook 方法发布同步生命周期事件。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static void Prefix(MethodBase __originalMethod, object[] __args)
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
                case nameof(Hook.BeforeFlush):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new BeforeFlushEvent(
                            (CombatStateCompat)__args[0],
                            (Player)__args[1],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(BeforeFlushEvent)
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
        ///     combat end, victory, turns, card pile changes, flush completion, and death resolution.
        ///     Harmony postfix：链接到原始异步 <see cref="Task" />，并在
        ///     战斗结束、胜利、回合、卡牌牌堆变化、flush 完成和死亡解析后发布生命周期事件。
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
            {
                switch (__originalMethod.Name)
                {
                    case nameof(Hook.AfterCombatEnd):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CombatEndedEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                                (CombatRoom)__args[2],
                                DateTimeOffset.UtcNow), nameof(CombatEndedEvent));
                        break;
                    case nameof(Hook.AfterCombatVictory):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CombatVictoryEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                                (CombatRoom)__args[2],
                                DateTimeOffset.UtcNow), nameof(CombatVictoryEvent));
                        break;
                    case nameof(Hook.AfterSideTurnStart):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new SideTurnStartedEvent((CombatStateCompat)__args[0], (CombatSide)__args[1],
                                DateTimeOffset.UtcNow),
                            nameof(SideTurnStartedEvent));
                        break;
                    case nameof(Hook.AfterCardPlayed):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CardPlayedEvent((CombatStateCompat)__args[0], (CardPlay)__args[2],
                                DateTimeOffset.UtcNow),
                            nameof(CardPlayedEvent));
                        break;
                    case nameof(Hook.AfterCardChangedPiles):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CardMovedBetweenPilesEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                                (CardModel)__args[2], (PileType)__args[3], (AbstractModel?)__args[4],
                                DateTimeOffset.UtcNow), nameof(CardMovedBetweenPilesEvent));
                        break;
                    case nameof(Hook.AfterCardDrawn):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CardDrawnEvent((CombatStateCompat)__args[0], (CardModel)__args[2], (bool)__args[3],
                                DateTimeOffset.UtcNow), nameof(CardDrawnEvent));
                        break;
                    case nameof(Hook.AfterCardDiscarded):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CardDiscardedEvent((CombatStateCompat)__args[0], (CardModel)__args[2],
                                DateTimeOffset.UtcNow),
                            nameof(CardDiscardedEvent));
                        break;
                    case nameof(Hook.AfterCardExhausted):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CardExhaustedEvent((CombatStateCompat)__args[0], (CardModel)__args[2], (bool)__args[3],
                                DateTimeOffset.UtcNow), nameof(CardExhaustedEvent));
                        break;
#if !STS2_AT_LEAST_0_105_0
                    case nameof(Hook.AfterCardRetained):
                        PublishLegacyCardRetained((CombatStateCompat)__args[0], (CardModel)__args[1]);
                        break;
#else
                    case nameof(Hook.AfterFlush):
                        PublishCardsFlushed(
                            (CombatStateCompat)__args[0],
                            (Player)__args[1],
                            (IReadOnlyCollection<CardModel>)__args[3],
                            (IReadOnlyCollection<CardModel>)__args[4]);
                        break;
#endif
                    case nameof(Hook.AfterDeath):
                        RitsuLibFramework.PublishLifecycleEvent(
                            new CreatureDiedEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                                (Creature)__args[2],
                                (bool)__args[3], (float)__args[4], DateTimeOffset.UtcNow),
                            nameof(CreatureDiedEvent));
                        break;
                }
            });
        }

#if !STS2_AT_LEAST_0_105_0
        private static void PublishLegacyCardRetained(CombatStateCompat combatState, CardModel card)
        {
#pragma warning disable CS0618
            RitsuLibFramework.PublishLifecycleEvent(
                new CardRetainedEvent(combatState, card, DateTimeOffset.UtcNow),
                nameof(CardRetainedEvent));
#pragma warning restore CS0618
        }
#else
        private static void PublishCardsFlushed(
            CombatStateCompat combatState,
            Player player,
            IReadOnlyCollection<CardModel> flushedCards,
            IReadOnlyCollection<CardModel> retainedCards)
        {
            var occurredAtUtc = DateTimeOffset.UtcNow;
            RitsuLibFramework.PublishLifecycleEvent(
                new CardsFlushedEvent(combatState, player, flushedCards, retainedCards, occurredAtUtc),
                nameof(CardsFlushedEvent));
#pragma warning disable CS0618
            foreach (var card in retainedCards)
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardRetainedEvent(combatState, card, occurredAtUtc),
                    nameof(CardRetainedEvent));
#pragma warning restore CS0618
        }
#endif
    }
}
