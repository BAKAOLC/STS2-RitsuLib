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
    ///     Optional listener for card OnPlay completion hooks.
    ///     卡牌 OnPlay 完成 hook 的可选监听器。
    /// </summary>
    public interface ICardOnPlayHookListener
    {
        /// <summary>
        ///     Runs after the card's own OnPlay body completes and before enchantment, affliction, and
        ///     Hook.AfterCardPlayed processing.
        ///     在卡牌自身 OnPlay 主体完成后、附魔/苦痛和 Hook.AfterCardPlayed 处理前运行。
        /// </summary>
        Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Dispatches card OnPlay completion hooks to model, capability, and registered global listeners.
    ///     将卡牌 OnPlay 完成 hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class CardOnPlayHook
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<ICardOnPlayHookListener> GlobalListeners = [];
        private static readonly CardOnPlayDelegate CardOnPlay = CreateCardOnPlayDelegate();

        /// <summary>
        ///     Registers a process-wide listener. Model-owned effects should usually implement
        ///     <see cref="ICardOnPlayHookListener" /> directly.
        ///     注册一个进程级监听器。模型所属效果通常应直接实现 <see cref="ICardOnPlayHookListener" />。
        /// </summary>
        public static void RegisterGlobalListener(ICardOnPlayHookListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            lock (SyncRoot)
            {
                if (!GlobalListeners.Contains(listener))
                    GlobalListeners.Add(listener);
            }
        }

        /// <summary>
        ///     Runs the card's original OnPlay method and then dispatches OnPlay completion hooks.
        ///     运行卡牌原始 OnPlay 方法，然后分发 OnPlay 完成 hook。
        /// </summary>
        public static async Task RunOnPlayAndAfterCardOnPlayCompleted(
            CardModel card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            await CardOnPlay(card, choiceContext, cardPlay);

            var combatState = card.CombatState;
            if (combatState == null)
                return;

            await AfterCardOnPlayCompleted(new(combatState, choiceContext, cardPlay));
        }

        /// <summary>
        ///     Runs OnPlay completion hooks.
        ///     运行 OnPlay 完成 hook。
        /// </summary>
        public static async Task AfterCardOnPlayCompleted(CardOnPlayCompletedContext context)
        {
            foreach (var entry in IterateListeners(context.CombatState))
            {
                if (entry.Model == null)
                {
                    await entry.Listener.AfterCardOnPlayCompleted(context);
                    continue;
                }

                context.ChoiceContext.PushModel(entry.Model);
                try
                {
                    await entry.Listener.AfterCardOnPlayCompleted(context);
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
            HashSet<object> seen = new(ReferenceEqualityComparer.Instance);

            foreach (var model in combatState.IterateHookListeners())
            {
                if (model is ICardOnPlayHookListener modelListener && seen.Add(modelListener))
                    yield return new(modelListener, model);

                foreach (var capability in IterateCapabilityListeners(model))
                    if (seen.Add(capability))
                        yield return new(capability, capability as AbstractModel);
            }

            ICardOnPlayHookListener[] globals;
            lock (SyncRoot)
            {
                globals = GlobalListeners.ToArray();
            }

            foreach (var listener in globals)
                if (seen.Add(listener))
                    yield return new(listener, null);
        }

        private static IEnumerable<ICardOnPlayHookListener> IterateCapabilityListeners(AbstractModel model)
        {
            if (!ModelCapabilities.TryGet(model, out var capabilities))
                yield break;

            foreach (var capability in capabilities.All)
                if (capability is ICardOnPlayHookListener listener)
                    yield return listener;
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
