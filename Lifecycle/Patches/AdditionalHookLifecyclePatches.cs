#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes additional strongly typed lifecycle events for stable game hooks adjacent to the existing combat,
    ///     card, turn, reward, and room lifecycle surface.
    ///     为与现有战斗、卡牌、回合、奖励和房间生命周期面相邻的稳定游戏 hook 发布补充强类型事件。
    /// </summary>
    public class AdditionalHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "additional_hook_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish additional strongly typed lifecycle events for stable game hooks";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeAttack), [typeof(CombatStateCompat), typeof(AttackCommand)]),
#if !STS2_AT_LEAST_0_104_0
                new(typeof(Hook), nameof(Hook.AfterAttack), [typeof(CombatStateCompat), typeof(AttackCommand)]),
#else
                new(typeof(Hook), nameof(Hook.AfterAttack),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(AttackCommand)]),
#endif
                new(typeof(Hook), nameof(Hook.AfterBlockBroken), [typeof(CombatStateCompat), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterBlockCleared), [typeof(CombatStateCompat), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.BeforeBlockGained),
                [
                    typeof(CombatStateCompat), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel),
                ]),
                new(typeof(Hook), nameof(Hook.AfterBlockGained),
                [
                    typeof(CombatStateCompat), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel),
                ]),
                new(typeof(Hook), nameof(Hook.BeforeCardAutoPlayed),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(Creature), typeof(AutoPlayType)]),
                new(typeof(Hook), nameof(Hook.AfterCardEnteredCombat), [typeof(CombatStateCompat), typeof(CardModel)]),
#if !STS2_AT_LEAST_0_104_0
                new(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(bool)]),
#else
                new(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(Player)]),
#endif
                new(typeof(Hook), nameof(Hook.BeforeCardRemoved), [typeof(IRunState), typeof(CardModel)]),
                new(typeof(Hook), nameof(Hook.AfterCreatureAddedToCombat),
                    [typeof(CombatStateCompat), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterCurrentHpChanged),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature), typeof(decimal)]),
                new(typeof(Hook), nameof(Hook.AfterEnergyReset), [typeof(CombatStateCompat), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterEnergySpent),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(int)]),
                new(typeof(Hook), nameof(Hook.BeforeHandDraw),
                    [typeof(CombatStateCompat), typeof(Player), typeof(PlayerChoiceContext)]),
                new(typeof(Hook), nameof(Hook.AfterHandEmptied),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterItemPurchased),
                    [typeof(IRunState), typeof(Player), typeof(MerchantEntry), typeof(int)]),
                new(typeof(Hook), nameof(Hook.AfterMapGenerated), [typeof(IRunState), typeof(ActMap), typeof(int)]),
                new(typeof(Hook), nameof(Hook.AfterPlayerTurnStart),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.BeforePotionUsed),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterPotionUsed),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel), typeof(Creature)]),
                new(typeof(Hook), nameof(Hook.AfterRestSiteHeal), [typeof(IRunState), typeof(Player), typeof(bool)]),
                new(typeof(Hook), nameof(Hook.AfterRestSiteSmith), [typeof(IRunState), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterShuffle),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterStarsGained),
                    [typeof(CombatStateCompat), typeof(int), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterStarsSpent),
                    [typeof(CombatStateCompat), typeof(int), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterSummon),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player), typeof(decimal)]),
                new(typeof(Hook), nameof(Hook.AfterTakingExtraTurn), [typeof(CombatStateCompat), typeof(Player)]),
#if !STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.BeforeTurnEnd), [typeof(CombatStateCompat), typeof(CombatSide)]),
                new(typeof(Hook), nameof(Hook.AfterTurnEnd), [typeof(CombatStateCompat), typeof(CombatSide)]),
#else
                new(typeof(Hook), nameof(Hook.BeforeTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
                new(typeof(Hook), nameof(Hook.AfterTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
#endif
            ];
        }

        /// <summary>
        ///     Publishes before-style supplemental lifecycle events before the original hook body.
        ///     在原始 hook 主体前发布 before 类补充生命周期事件。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static void Prefix(MethodBase __originalMethod, object[] __args)
        {
            switch (__originalMethod.Name)
            {
                case nameof(Hook.BeforeAttack):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new AttackStartingEvent((CombatStateCompat)__args[0], (AttackCommand)__args[1],
                            DateTimeOffset.UtcNow), nameof(AttackStartingEvent));
                    break;
                case nameof(Hook.BeforeBlockGained):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new BlockGainingEvent((CombatStateCompat)__args[0], (Creature)__args[1], (decimal)__args[2],
                            (ValueProp)__args[3], (CardModel?)__args[4], DateTimeOffset.UtcNow),
                        nameof(BlockGainingEvent));
                    break;
                case nameof(Hook.BeforeCardAutoPlayed):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CardAutoPlayingEvent((CombatStateCompat)__args[0], (CardModel)__args[1],
                            (Creature?)__args[2], (AutoPlayType)__args[3], DateTimeOffset.UtcNow),
                        nameof(CardAutoPlayingEvent));
                    break;
                case nameof(Hook.BeforeCardRemoved):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CardRemovingEvent((IRunState)__args[0], (CardModel)__args[1], DateTimeOffset.UtcNow),
                        nameof(CardRemovingEvent));
                    break;
                case nameof(Hook.BeforeHandDraw):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new HandDrawingEvent((CombatStateCompat)__args[0], (Player)__args[1],
                            (PlayerChoiceContext)__args[2], DateTimeOffset.UtcNow), nameof(HandDrawingEvent));
                    break;
                case nameof(Hook.BeforePotionUsed):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new PotionUsingEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                            (PotionModel)__args[2], (Creature?)__args[3], DateTimeOffset.UtcNow),
                        nameof(PotionUsingEvent));
                    break;
                case nameof(Hook.BeforeTurnEnd):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new SideTurnEndingEvent((CombatStateCompat)__args[0], (CombatSide)__args[1],
                            GetParticipants(__args), DateTimeOffset.UtcNow), nameof(SideTurnEndingEvent));
                    break;
            }
        }

        /// <summary>
        ///     Publishes after-style supplemental lifecycle events after the original hook task completes.
        ///     在原始 hook task 完成后发布 after 类补充生命周期事件。
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () => PublishAfter(__originalMethod.Name, __args));
        }

        private static void PublishAfter(string hookName, object[] args)
        {
            switch (hookName)
            {
                case nameof(Hook.AfterAttack):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new AttackEndedEvent((CombatStateCompat)args[0], GetChoiceContext(args, 1),
                            (AttackCommand)args[^1], DateTimeOffset.UtcNow), nameof(AttackEndedEvent));
                    break;
                case nameof(Hook.AfterBlockBroken):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new BlockBrokenEvent((CombatStateCompat)args[0], (Creature)args[1], DateTimeOffset.UtcNow),
                        nameof(BlockBrokenEvent));
                    break;
                case nameof(Hook.AfterBlockCleared):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new BlockClearedEvent((CombatStateCompat)args[0], (Creature)args[1], DateTimeOffset.UtcNow),
                        nameof(BlockClearedEvent));
                    break;
                case nameof(Hook.AfterBlockGained):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new BlockGainedEvent((CombatStateCompat)args[0], (Creature)args[1], (decimal)args[2],
                            (ValueProp)args[3], (CardModel?)args[4], DateTimeOffset.UtcNow),
                        nameof(BlockGainedEvent));
                    break;
                case nameof(Hook.AfterCardEnteredCombat):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CardEnteredCombatEvent((CombatStateCompat)args[0], (CardModel)args[1],
                            DateTimeOffset.UtcNow), nameof(CardEnteredCombatEvent));
                    break;
                case nameof(Hook.AfterCardGeneratedForCombat):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CardGeneratedForCombatEvent((CombatStateCompat)args[0], (CardModel)args[1],
                            args[2] as Player, args[2] is bool addedByPlayer ? addedByPlayer : null,
                            DateTimeOffset.UtcNow), nameof(CardGeneratedForCombatEvent));
                    break;
                case nameof(Hook.AfterCreatureAddedToCombat):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CreatureAddedToCombatEvent((CombatStateCompat)args[0], (Creature)args[1],
                            DateTimeOffset.UtcNow), nameof(CreatureAddedToCombatEvent));
                    break;
                case nameof(Hook.AfterCurrentHpChanged):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new CurrentHpChangedEvent((IRunState)args[0], (CombatStateCompat?)args[1],
                            (Creature)args[2], (decimal)args[3], DateTimeOffset.UtcNow),
                        nameof(CurrentHpChangedEvent));
                    break;
                case nameof(Hook.AfterEnergyReset):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EnergyResetEvent((CombatStateCompat)args[0], (Player)args[1], DateTimeOffset.UtcNow),
                        nameof(EnergyResetEvent));
                    break;
                case nameof(Hook.AfterEnergySpent):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EnergySpentEvent((CombatStateCompat)args[0], (CardModel)args[1], (int)args[2],
                            DateTimeOffset.UtcNow), nameof(EnergySpentEvent));
                    break;
                case nameof(Hook.AfterHandEmptied):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new HandEmptiedEvent((CombatStateCompat)args[0], (PlayerChoiceContext)args[1],
                            (Player)args[2], DateTimeOffset.UtcNow), nameof(HandEmptiedEvent));
                    break;
                case nameof(Hook.AfterItemPurchased):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ItemPurchasedEvent((IRunState)args[0], (Player)args[1], (MerchantEntry)args[2],
                            (int)args[3], DateTimeOffset.UtcNow), nameof(ItemPurchasedEvent));
                    break;
                case nameof(Hook.AfterMapGenerated):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new MapGeneratedEvent((IRunState)args[0], (ActMap)args[1], (int)args[2],
                            DateTimeOffset.UtcNow), nameof(MapGeneratedEvent));
                    break;
                case nameof(Hook.AfterPlayerTurnStart):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new PlayerTurnStartedEvent((CombatStateCompat)args[0], (PlayerChoiceContext)args[1],
                            (Player)args[2], DateTimeOffset.UtcNow), nameof(PlayerTurnStartedEvent));
                    break;
                case nameof(Hook.AfterPotionUsed):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new PotionUsedEvent((IRunState)args[0], (CombatStateCompat?)args[1], (PotionModel)args[2],
                            (Creature?)args[3], DateTimeOffset.UtcNow), nameof(PotionUsedEvent));
                    break;
                case nameof(Hook.AfterRestSiteHeal):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new RestSiteHealedEvent((IRunState)args[0], (Player)args[1], (bool)args[2],
                            DateTimeOffset.UtcNow), nameof(RestSiteHealedEvent));
                    break;
                case nameof(Hook.AfterRestSiteSmith):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new RestSiteSmithedEvent((IRunState)args[0], (Player)args[1], DateTimeOffset.UtcNow),
                        nameof(RestSiteSmithedEvent));
                    break;
                case nameof(Hook.AfterShuffle):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ShuffledEvent((CombatStateCompat)args[0], (PlayerChoiceContext)args[1],
                            (Player)args[2], DateTimeOffset.UtcNow), nameof(ShuffledEvent));
                    break;
                case nameof(Hook.AfterStarsGained):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new StarsGainedEvent((CombatStateCompat)args[0], (int)args[1], (Player)args[2],
                            DateTimeOffset.UtcNow), nameof(StarsGainedEvent));
                    break;
                case nameof(Hook.AfterStarsSpent):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new StarsSpentEvent((CombatStateCompat)args[0], (int)args[1], (Player)args[2],
                            DateTimeOffset.UtcNow), nameof(StarsSpentEvent));
                    break;
                case nameof(Hook.AfterSummon):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new SummonedEvent((CombatStateCompat)args[0], (PlayerChoiceContext)args[1], (Player)args[2],
                            (decimal)args[3], DateTimeOffset.UtcNow), nameof(SummonedEvent));
                    break;
                case nameof(Hook.AfterTakingExtraTurn):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ExtraTurnTakenEvent((CombatStateCompat)args[0], (Player)args[1], DateTimeOffset.UtcNow),
                        nameof(ExtraTurnTakenEvent));
                    break;
                case nameof(Hook.AfterTurnEnd):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new SideTurnEndedEvent((CombatStateCompat)args[0], (CombatSide)args[1], GetParticipants(args),
                            DateTimeOffset.UtcNow), nameof(SideTurnEndedEvent));
                    break;
            }
        }

        private static PlayerChoiceContext? GetChoiceContext(object[] args, int index)
        {
            return args.Length > index ? args[index] as PlayerChoiceContext : null;
        }

        private static IReadOnlyCollection<Creature>? GetParticipants(object[] args)
        {
            return args.Length > 2 && args[2] is IEnumerable<Creature> participants
                ? participants as IReadOnlyCollection<Creature> ?? participants.ToArray()
                : null;
        }
    }
}
