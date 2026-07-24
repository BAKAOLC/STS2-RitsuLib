#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.AttackHits
{
    /// <summary>
    ///     Dispatches per-hit attack hooks to game hook listeners and attached model capabilities.
    ///     将每段攻击 hook 分发给游戏 hook listener 和附加的模型 capability。
    /// </summary>
    public static class AttackHitHook
    {
        /// <summary>
        ///     Wrapper used by the <c>AttackCommand.Execute</c> transpiler.
        ///     由 <c>AttackCommand.Execute</c> 转译器调用的包装器。
        /// </summary>
        public static Task<IEnumerable<DamageResult>> DamageWithAttackHitHooks(
            PlayerChoiceContext choiceContext,
            IEnumerable<Creature> targets,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
#if STS2_AT_LEAST_0_108_0
            CardPlay? cardPlay,
#endif
            AttackCommand attack,
            int hitIndex,
            decimal totalHitCount)
        {
            var targetList = targets as IReadOnlyList<Creature> ?? [.. targets];
            var context = TryCreateContext(
                choiceContext,
                targetList,
                amount,
                props,
                dealer,
                cardSource,
#if STS2_AT_LEAST_0_108_0
                cardPlay,
#endif
                attack,
                hitIndex,
                totalHitCount);
            return context == null
#if STS2_AT_LEAST_0_108_0
                ? CreatureCmd.Damage(choiceContext, targetList, amount, props, dealer, cardSource, cardPlay)
                : DamageAndAfterAttackHit(context);
#else
                ? CreatureCmd.Damage(choiceContext, targetList, amount, props, dealer, cardSource)
                : DamageAndAfterAttackHit(context);
#endif
        }

        private static async Task<IEnumerable<DamageResult>> DamageAndAfterAttackHit(
            AttackHitContext context)
        {
            await BeforeAttackHit(context);

#if STS2_AT_LEAST_0_108_0
            var results = (await CreatureCmd.Damage(
                context.ChoiceContext,
                context.Targets,
                context.Damage,
                context.DamageProps,
                context.Dealer,
                context.CardSource,
                context.CardPlay)).ToArray();
#else
            var results = (await CreatureCmd.Damage(
                context.ChoiceContext,
                context.Targets,
                context.Damage,
                context.DamageProps,
                context.Dealer,
                context.CardSource)).ToArray();
#endif

            context.SetResults(results);
            await AfterAttackHit(context);
            return results;
        }

        /// <summary>
        ///     Runs before-hit hooks.
        ///     运行前置命中 hook。
        /// </summary>
        public static async Task BeforeAttackHit(AttackHitContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState, context.Attack.ModelSource, context.CardSource))
                await Invoke(context, entry, static (listener, ctx) => listener.BeforeAttackHit(ctx));
        }

        /// <summary>
        ///     Runs after-hit hooks.
        ///     运行后置命中 hook。
        /// </summary>
        public static async Task AfterAttackHit(AttackHitContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState, context.Attack.ModelSource, context.CardSource))
                await Invoke(context, entry, static (listener, ctx) => listener.AfterAttackHit(ctx));
        }

        private static async Task Invoke(
            AttackHitContext context,
            ListenerEntry entry,
            Func<IAttackHitHookListener, AttackHitContext, Task> callback)
        {
            if (entry.Model == null)
            {
                await callback(entry.Listener, context);
                return;
            }

            context.ChoiceContext.PushModel(entry.Model);
            try
            {
                await callback(entry.Listener, context);
                entry.Model.InvokeExecutionFinished();
            }
            finally
            {
                context.ChoiceContext.PopModel(entry.Model);
            }
        }

        private static IEnumerable<ListenerEntry> IterateListeners(
            CombatStateLike combatState,
            params AbstractModel?[] extraModels)
        {
            return ModelHookListenerDispatcher.FromCombat<IAttackHitHookListener>(
                combatState,
                extraModels).Select(entry => new ListenerEntry(entry.Listener, entry.Model));
        }

        private static AttackHitContext? TryCreateContext(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<Creature> targets,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
#if STS2_AT_LEAST_0_108_0
            CardPlay? cardPlay,
#endif
            AttackCommand attack,
            int hitIndex,
            decimal totalHitCount)
        {
            var combatState = attack?.Attacker?.CombatState;
            if (attack == null || combatState == null)
                return null;

            return new(
                combatState,
                choiceContext,
                attack,
                targets,
                hitIndex,
                totalHitCount,
                amount,
                props,
                dealer,
                cardSource
#if STS2_AT_LEAST_0_108_0
                ,
                cardPlay
#endif
            );
        }

        private readonly record struct ListenerEntry(IAttackHitHookListener Listener, AbstractModel? Model);
    }
}
