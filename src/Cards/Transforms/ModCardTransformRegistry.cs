using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Transforms
{
    /// <summary>
    ///     <para xml:lang="en">Provides per-mod registration for base-game card-transformation listeners.</para>
    ///     <para xml:lang="zh-CN">提供按模组注册游戏本体卡牌转化监听器的功能。</para>
    /// </summary>
    public sealed class ModCardTransformRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardTransformRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<(string ModId, string ListenerId), ListenerEntry> Listeners = [];
        private static long _nextRegistrationOrder;

        private readonly string _modId;

        private ModCardTransformRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the transformation registry for <paramref name="modId" />, creating it on first use.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的转化注册表，并在首次使用时创建。</para>
        /// </summary>
        public static ModCardTransformRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModCardTransformRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a listener for every completed base-game card transformation.</para>
        ///     <para xml:lang="zh-CN">注册或替换监听器，以接收每次已完成的游戏本体卡牌转化。</para>
        /// </summary>
        public void Register(string listenerId, Action<ModCardTransformContext> listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            Register(listenerId, _ => true, context =>
            {
                listener(context);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an asynchronous listener for every completed base-game card transformation.</para>
        ///     <para xml:lang="zh-CN">注册或替换异步监听器，以接收每次已完成的游戏本体卡牌转化。</para>
        /// </summary>
        public void Register(string listenerId, Func<ModCardTransformContext, Task> listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            Register(listenerId, _ => true, listener);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a listener with a custom predicate.</para>
        ///     <para xml:lang="zh-CN">注册或替换带自定义谓词的监听器。</para>
        /// </summary>
        public void Register(
            string listenerId,
            Func<ModCardTransformContext, bool> predicate,
            Action<ModCardTransformContext> listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            Register(listenerId, predicate, context =>
            {
                listener(context);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an asynchronous listener with a custom predicate.</para>
        ///     <para xml:lang="zh-CN">注册或替换带自定义谓词的异步监听器。</para>
        /// </summary>
        public void Register(
            string listenerId,
            Func<ModCardTransformContext, bool> predicate,
            Func<ModCardTransformContext, Task> listener)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(listener);

            lock (SyncRoot)
            {
                var key = (_modId, listenerId);
                var registrationOrder = Listeners.TryGetValue(key, out var existing)
                    ? existing.RegistrationOrder
                    : _nextRegistrationOrder++;

                Listeners[key] = new(_modId, listenerId, predicate, listener, registrationOrder);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a typed listener for an original and replacement card pair.</para>
        ///     <para xml:lang="zh-CN">注册或替换针对原卡牌与替换卡牌类型组合的监听器。</para>
        /// </summary>
        public void Register<TOriginal, TReplacement>(
            string listenerId,
            Action<TOriginal, TReplacement> listener)
            where TOriginal : CardModel
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context is { Original: TOriginal, Replacement: TReplacement },
                context =>
                {
                    listener((TOriginal)context.Original, (TReplacement)context.Replacement);
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a typed asynchronous listener for an original and replacement card pair.</para>
        ///     <para xml:lang="zh-CN">注册或替换针对原卡牌与替换卡牌类型组合的异步监听器。</para>
        /// </summary>
        public void Register<TOriginal, TReplacement>(
            string listenerId,
            Func<TOriginal, TReplacement, Task> listener)
            where TOriginal : CardModel
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context is { Original: TOriginal, Replacement: TReplacement },
                context => listener((TOriginal)context.Original, (TReplacement)context.Replacement));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a listener for cards transformed from <typeparamref name="TOriginal" />.</para>
        ///     <para xml:lang="zh-CN">注册或替换监听器，以处理从 <typeparamref name="TOriginal" /> 转化而来的卡牌。</para>
        /// </summary>
        public void RegisterFrom<TOriginal>(
            string listenerId,
            Action<TOriginal, CardModel> listener)
            where TOriginal : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Original is TOriginal,
                context =>
                {
                    listener((TOriginal)context.Original, context.Replacement);
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an asynchronous listener for cards transformed from <typeparamref name="TOriginal" />.</para>
        ///     <para xml:lang="zh-CN">注册或替换异步监听器，以处理从 <typeparamref name="TOriginal" /> 转化而来的卡牌。</para>
        /// </summary>
        public void RegisterFrom<TOriginal>(
            string listenerId,
            Func<TOriginal, CardModel, Task> listener)
            where TOriginal : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Original is TOriginal,
                context => listener((TOriginal)context.Original, context.Replacement));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a listener for cards transformed into <typeparamref name="TReplacement" />.</para>
        ///     <para xml:lang="zh-CN">注册或替换监听器，以处理转化为 <typeparamref name="TReplacement" /> 的卡牌。</para>
        /// </summary>
        public void RegisterTo<TReplacement>(
            string listenerId,
            Action<CardModel, TReplacement> listener)
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Replacement is TReplacement,
                context =>
                {
                    listener(context.Original, (TReplacement)context.Replacement);
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces an asynchronous listener for cards transformed into <typeparamref name="TReplacement" />.</para>
        ///     <para xml:lang="zh-CN">注册或替换异步监听器，以处理转化为 <typeparamref name="TReplacement" /> 的卡牌。</para>
        /// </summary>
        public void RegisterTo<TReplacement>(
            string listenerId,
            Func<CardModel, TReplacement, Task> listener)
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Replacement is TReplacement,
                context => listener(context.Original, (TReplacement)context.Replacement));
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a listener registered by this mod.</para>
        ///     <para xml:lang="zh-CN">移除此模组注册的监听器。</para>
        /// </summary>
        public bool Unregister(string listenerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);

            lock (SyncRoot)
            {
                return Listeners.Remove((_modId, listenerId));
            }
        }

        internal static async Task NotifyTransformedAsync(
            CardModel original,
            CardModel replacement,
            CardPile originalPile,
            int originalPileIndex)
        {
            ArgumentNullException.ThrowIfNull(original);
            ArgumentNullException.ThrowIfNull(replacement);
            ArgumentNullException.ThrowIfNull(originalPile);

            var context = new ModCardTransformContext(original, replacement, originalPile, originalPileIndex);
            ListenerEntry[] listeners;
            lock (SyncRoot)
            {
                listeners = [.. Listeners.Values.OrderBy(static entry => entry.RegistrationOrder)];
            }

            foreach (var entry in listeners)
                try
                {
                    if (entry.Predicate(context))
                        await entry.Listener(context);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModCardTransformRegistry] Listener '{entry.ModId}/{entry.ListenerId}' failed for " +
                        $"{original.Id}: {ex}");
                    throw;
                }
        }

        private sealed record ListenerEntry(
            string ModId,
            string ListenerId,
            Func<ModCardTransformContext, bool> Predicate,
            Func<ModCardTransformContext, Task> Listener,
            long RegistrationOrder);
    }
}
