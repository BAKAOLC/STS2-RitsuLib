#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Cards
{
    /// <summary>
    ///     <para xml:lang="en">Context for the legacy hook that runs after a card's own <c>OnPlay</c> method.</para>
    ///     <para xml:lang="zh-CN">卡牌自身的 <c>OnPlay</c> 方法执行后，旧版钩子所使用的上下文。</para>
    /// </summary>
    public readonly record struct CardOnPlayCompletedContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay);

    /// <summary>
    ///     <para xml:lang="en">Context for hooks that run before a card's own <c>OnPlay</c> method.</para>
    ///     <para xml:lang="zh-CN">卡牌自身的 <c>OnPlay</c> 方法执行前，钩子所使用的上下文。</para>
    /// </summary>
    public readonly record struct BeforeCardOnPlayContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides after-play context, including whether the card's own <c>OnPlay</c> method ran.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供卡牌打出后的上下文，其中包括其自身的 <c>OnPlay</c> 方法是否已执行。</para>
    /// </summary>
    public readonly record struct AfterCardOnPlayContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay,
        bool OriginalOnPlayRan);

    /// <summary>
    ///     <para xml:lang="en">Receives hooks immediately before and after a card's own <c>OnPlay</c> method.</para>
    ///     <para xml:lang="zh-CN">接收紧邻卡牌自身 <c>OnPlay</c> 方法前后的钩子。</para>
    /// </summary>
    public interface ICardOnPlayHookListener
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs before the card's own <c>OnPlay</c> method. Return <see langword="true" /> to skip that method
        ///         without skipping the remaining <c>CardModel.OnPlayWrapper</c> flow.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在卡牌自身的 <c>OnPlay</c> 方法前运行。返回 <see langword="true" /> 可跳过该方法，但不会跳过
        ///         <c>CardModel.OnPlayWrapper</c> 的其余流程。
        ///     </para>
        /// </summary>
        Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs after the card's own <c>OnPlay</c> point and before enchantments, afflictions, and
        ///         <c>Hook.AfterCardPlayed</c> are processed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在卡牌自身的 <c>OnPlay</c> 执行点之后，以及附魔、侵蚀和 <c>Hook.AfterCardPlayed</c> 处理之前运行。
        ///     </para>
        /// </summary>
        Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
#pragma warning disable CS0618
            return context.OriginalOnPlayRan
                ? AfterCardOnPlayCompleted(new(context.CombatState, context.ChoiceContext, context.CardPlay))
                : Task.CompletedTask;
#pragma warning restore CS0618
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs after the card's own <c>OnPlay</c> method completes and before enchantments, afflictions, and
        ///         <c>Hook.AfterCardPlayed</c> are processed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在卡牌自身的 <c>OnPlay</c> 方法完成后，以及附魔、侵蚀和 <c>Hook.AfterCardPlayed</c> 处理之前运行。
        ///     </para>
        /// </summary>
        [Obsolete("Use AfterCardOnPlay(AfterCardOnPlayContext) instead.")]
        Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Dispatches card-play hooks to listeners implemented by models or capabilities and to registered global
    ///         listeners.
    ///     </para>
    ///     <para xml:lang="zh-CN">向模型、模型能力及已注册的全局监听器分发卡牌打出钩子。</para>
    /// </summary>
    public static class CardOnPlayHook
    {
        private static readonly ModelHookListenerRegistry<ICardOnPlayHookListener> GlobalListeners = new();
        private static readonly CardOnPlayDelegate CardOnPlay = CreateCardOnPlayDelegate();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide listener. Effects owned by a model should normally implement
        ///         <see cref="ICardOnPlayHookListener" /> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个进程级监听器。由模型持有的效果通常应直接实现 <see cref="ICardOnPlayHookListener" />。
        ///     </para>
        /// </summary>
        public static void RegisterGlobalListener(ICardOnPlayHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs the before hooks, the card's own <c>OnPlay</c> method unless skipped, and then the after hooks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         依次运行前置钩子、未被跳过时卡牌自身的 <c>OnPlay</c> 方法，以及后置钩子。
        ///     </para>
        /// </summary>
        public static async Task RunCardOnPlayHooks(
            CardModel card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(choiceContext);
            ArgumentNullException.ThrowIfNull(cardPlay);

            var combatState = card.CombatState;
            var suppressOriginal = combatState != null &&
                                   await BeforeCardOnPlay(new(combatState, choiceContext, cardPlay));
            var originalOnPlayRan = false;
            if (!suppressOriginal)
            {
                await CardOnPlay(card, choiceContext, cardPlay);
                originalOnPlayRan = true;
            }

            combatState = card.CombatState;
            if (combatState == null)
                return;

            await AfterCardOnPlay(new(combatState, choiceContext, cardPlay, originalOnPlayRan));
        }

        /// <summary>
        ///     <para xml:lang="en">Compatibility wrapper for RitsuLib's original card-play injection method.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 原卡牌打出注入方法的兼容包装。</para>
        /// </summary>
        [Obsolete("Use RunCardOnPlayHooks(CardModel, PlayerChoiceContext, CardPlay) instead.")]
        public static Task RunOnPlayAndAfterCardOnPlayCompleted(
            CardModel card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            return RunCardOnPlayHooks(card, choiceContext, cardPlay);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs the before-play hooks and returns whether the card's own <c>OnPlay</c> method should be skipped.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         运行打出前置钩子，并返回是否应跳过卡牌自身的 <c>OnPlay</c> 方法。
        ///     </para>
        /// </summary>
        public static async Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context)
        {
            var suppressOriginal = false;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var entry in IterateListeners(context.CombatState))
                suppressOriginal |= await BeforeCardOnPlay(context, entry);

            return suppressOriginal;
        }

        private static async Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context, ListenerEntry entry)
        {
            if (entry.Model == null)
                return await entry.Listener.BeforeCardOnPlay(context);

            context.ChoiceContext.PushModel(entry.Model);
            try
            {
                var suppressOriginal = await entry.Listener.BeforeCardOnPlay(context);
                entry.Model.InvokeExecutionFinished();
                return suppressOriginal;
            }
            finally
            {
                context.ChoiceContext.PopModel(entry.Model);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Runs the after-play hooks.</para>
        ///     <para xml:lang="zh-CN">运行打出后置钩子。</para>
        /// </summary>
        public static async Task AfterCardOnPlay(AfterCardOnPlayContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
            {
                if (entry.Model == null)
                {
                    await entry.Listener.AfterCardOnPlay(context);
                    continue;
                }

                context.ChoiceContext.PushModel(entry.Model);
                try
                {
                    await entry.Listener.AfterCardOnPlay(context);
                    entry.Model.InvokeExecutionFinished();
                }
                finally
                {
                    context.ChoiceContext.PopModel(entry.Model);
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Runs the legacy after-play hooks.</para>
        ///     <para xml:lang="zh-CN">运行旧版打出后置钩子。</para>
        /// </summary>
        [Obsolete("Use AfterCardOnPlay(AfterCardOnPlayContext) instead.")]
        public static async Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
            {
                if (entry.Model == null)
                {
#pragma warning disable CS0618
                    await entry.Listener.AfterCardOnPlayCompleted(context);
#pragma warning restore CS0618
                    continue;
                }

                context.ChoiceContext.PushModel(entry.Model);
                try
                {
#pragma warning disable CS0618
                    await entry.Listener.AfterCardOnPlayCompleted(context);
#pragma warning restore CS0618
                    entry.Model.InvokeExecutionFinished();
                }
                finally
                {
                    context.ChoiceContext.PopModel(entry.Model);
                }
            }
        }

        private static IEnumerable<ListenerEntry> IterateListeners(CombatStateLike combatState)
        {
            return ModelHookListenerDispatcher.FromCombat(
                combatState,
                GlobalListeners).Select(entry => new ListenerEntry(entry.Listener, entry.Model));
        }

        private static CardOnPlayDelegate CreateCardOnPlayDelegate()
        {
            var method = typeof(CardModel).GetMethod(
                "OnPlay",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(PlayerChoiceContext), typeof(CardPlay)],
                null);
            return method?.CreateDelegate<CardOnPlayDelegate>() ??
                   throw new MissingMethodException(typeof(CardModel).FullName, "OnPlay");
        }

        private delegate Task CardOnPlayDelegate(
            CardModel card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay);

        private readonly record struct ListenerEntry(ICardOnPlayHookListener Listener, AbstractModel? Model);
    }
}
