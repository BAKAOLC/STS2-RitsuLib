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
    ///     Context for hooks that run after a card's own OnPlay body completes inside CardModel.OnPlayWrapper.
    ///     在 CardModel.OnPlayWrapper 内、卡牌自身 OnPlay 主体完成后运行的 hook 上下文。
    /// </summary>
    public readonly record struct CardOnPlayCompletedContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay);

    /// <summary>
    ///     Context for hooks that run before a card's own OnPlay body inside CardModel.OnPlayWrapper.
    ///     在 CardModel.OnPlayWrapper 内、卡牌自身 OnPlay 主体前运行的 hook 上下文。
    /// </summary>
    public readonly record struct BeforeCardOnPlayContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay);

    /// <summary>
    ///     Context for hooks that run after a card's own OnPlay body point inside CardModel.OnPlayWrapper.
    ///     在 CardModel.OnPlayWrapper 内、卡牌自身 OnPlay 主体位置后运行的 hook 上下文。
    /// </summary>
    public readonly record struct AfterCardOnPlayContext(
        CombatStateLike CombatState,
        PlayerChoiceContext ChoiceContext,
        CardPlay CardPlay,
        bool OriginalOnPlayRan);

    /// <summary>
    ///     Optional listener for card OnPlay before and after hooks.
    ///     卡牌 OnPlay 前置和后置 hook 的可选监听器。
    /// </summary>
    public interface ICardOnPlayHookListener
    {
        /// <summary>
        ///     Runs before the card's own OnPlay body. Return true to suppress the original OnPlay body while keeping
        ///     the rest of CardModel.OnPlayWrapper intact.
        ///     在卡牌自身 OnPlay 主体前运行。返回 true 可阻止原始 OnPlay 主体，同时保留
        ///     CardModel.OnPlayWrapper 的其他流程。
        /// </summary>
        Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        ///     Runs after the card's own OnPlay body point and before enchantment, affliction, and
        ///     Hook.AfterCardPlayed processing.
        ///     在卡牌自身 OnPlay 主体位置后、附魔/苦痛和 Hook.AfterCardPlayed 处理前运行。
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
        ///     Runs after the card's own OnPlay body or replacement point completes and before enchantment,
        ///     affliction, and Hook.AfterCardPlayed processing.
        ///     在卡牌自身 OnPlay 主体或替代位置完成后、附魔/苦痛和 Hook.AfterCardPlayed 处理前运行。
        /// </summary>
        [Obsolete("Use AfterCardOnPlay(AfterCardOnPlayContext) instead.")]
        Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Dispatches card OnPlay hooks to model, capability, and registered global listeners.
    ///     将卡牌 OnPlay hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class CardOnPlayHook
    {
        private static readonly ModelHookListenerRegistry<ICardOnPlayHookListener> GlobalListeners = new();
        private static readonly CardOnPlayDelegate CardOnPlay = CreateCardOnPlayDelegate();

        /// <summary>
        ///     Registers a process-wide listener. Model-owned effects should usually implement
        ///     <see cref="ICardOnPlayHookListener" /> directly.
        ///     注册一个进程级监听器。模型所属效果通常应直接实现 <see cref="ICardOnPlayHookListener" />。
        /// </summary>
        public static void RegisterGlobalListener(ICardOnPlayHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     Runs before hooks, the card's original OnPlay body when not suppressed, and after hooks.
        ///     运行前置 hook、未被阻止时的卡牌原始 OnPlay 主体，以及后置 hook。
        /// </summary>
        public static async Task RunCardOnPlayHooks(
            CardModel card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
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
        ///     Compatibility wrapper for the original RitsuLib card OnPlay hook injection method.
        ///     RitsuLib 原卡牌 OnPlay hook 注入方法的兼容包装。
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
        ///     Runs card OnPlay before hooks and returns true when the original OnPlay body should be suppressed.
        ///     运行卡牌 OnPlay 前置 hook，并在应阻止原始 OnPlay 主体时返回 true。
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
        ///     Runs OnPlay after hooks.
        ///     运行 OnPlay 后置 hook。
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
        ///     Runs the original OnPlay completion hooks.
        ///     运行原有 OnPlay 完成 hook。
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
