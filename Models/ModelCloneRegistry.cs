using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Global listeners for vanilla model clone operations.
    ///     原版模型复制操作的全局监听器。
    /// </summary>
    public static class ModelCloneRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<(string ModId, string ListenerId), ListenerEntry> Listeners = [];
        private static long _nextRegistrationOrder;

        /// <summary>
        ///     Registers or replaces a listener for <paramref name="modId" /> that receives every completed
        ///     <see cref="AbstractModel.MutableClone" />.
        ///     为 <paramref name="modId" /> 注册或替换一个监听器，以接收每次完成的
        ///     <see cref="AbstractModel.MutableClone" />。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 mod 标识符。
        /// </param>
        /// <param name="listenerId">
        ///     Unique listener id within the mod.
        ///     此监听器在 mod 内的唯一标识符。
        /// </param>
        /// <param name="listener">
        ///     Listener invoked after the clone has been created and initialized.
        ///     在复制体创建并初始化后调用的监听器。
        /// </param>
        public static void Register(string modId, string listenerId, Action<ModelCloneContext> listener)
        {
            Register(modId, listenerId, _ => true, listener);
        }

        /// <summary>
        ///     Registers or replaces a listener with a custom predicate for <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 注册或替换一个带自定义谓词的监听器。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 mod 标识符。
        /// </param>
        /// <param name="listenerId">
        ///     Unique listener id within the mod.
        ///     此监听器在 mod 内的唯一标识符。
        /// </param>
        /// <param name="predicate">
        ///     Predicate used to select clone operations for this listener.
        ///     用于筛选此监听器关心的复制操作。
        /// </param>
        /// <param name="listener">
        ///     Listener invoked after the clone has been created and initialized.
        ///     在复制体创建并初始化后调用的监听器。
        /// </param>
        public static void Register(
            string modId,
            string listenerId,
            Func<ModelCloneContext, bool> predicate,
            Action<ModelCloneContext> listener)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(listener);

            lock (SyncRoot)
            {
                var key = (modId, listenerId);
                var registrationOrder = Listeners.TryGetValue(key, out var existing)
                    ? existing.RegistrationOrder
                    : _nextRegistrationOrder++;

                Listeners[key] = new(modId, listenerId, predicate, listener, registrationOrder);
            }
        }

        /// <summary>
        ///     Registers or replaces a typed listener for a model family, including vanilla model types, for
        ///     <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 注册或替换某个模型族的类型化监听器，包括原版模型类型。
        /// </summary>
        /// <typeparam name="TModel">
        ///     Model base or concrete type to listen for.
        ///     要监听的模型基类或具体类型。
        /// </typeparam>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 mod 标识符。
        /// </param>
        /// <param name="listenerId">
        ///     Unique listener id within the mod.
        ///     此监听器在 mod 内的唯一标识符。
        /// </param>
        /// <param name="listener">
        ///     Typed listener invoked when both prototype and cloned model are <typeparamref name="TModel" />.
        ///     当原型和复制体均为 <typeparamref name="TModel" /> 时调用的类型化监听器。
        /// </param>
        public static void Register<TModel>(string modId, string listenerId, Action<TModel, TModel> listener)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                modId,
                listenerId,
                context => context is { Prototype: TModel, ClonedModel: TModel },
                context => listener((TModel)context.Prototype, (TModel)context.ClonedModel));
        }

        /// <summary>
        ///     Removes a previously registered listener.
        ///     移除先前注册的监听器。
        /// </summary>
        /// <param name="modId">
        ///     Mod identifier used at registration.
        ///     注册时使用的 mod 标识符。
        /// </param>
        /// <param name="listenerId">
        ///     Listener id used at registration.
        ///     注册时使用的监听器标识符。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an entry was removed.
        ///     如果移除了条目，则为 <see langword="true" />。
        /// </returns>
        public static bool Unregister(string modId, string listenerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);

            lock (SyncRoot)
            {
                return Listeners.Remove((modId, listenerId));
            }
        }

        internal static void NotifyCloned(AbstractModel prototype, AbstractModel clone)
        {
            ArgumentNullException.ThrowIfNull(prototype);
            ArgumentNullException.ThrowIfNull(clone);

            var context = new ModelCloneContext(prototype, clone);
            ListenerEntry[] listeners;
            lock (SyncRoot)
            {
                listeners = Listeners.Values.OrderBy(static entry => entry.RegistrationOrder).ToArray();
            }

            foreach (var entry in listeners)
                try
                {
                    if (entry.Predicate(context))
                        entry.Listener(context);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModelCloneRegistry] Listener '{entry.ModId}/{entry.ListenerId}' failed for {prototype.Id}: {ex.Message}");
                }
        }

        private sealed record ListenerEntry(
            string ModId,
            string ListenerId,
            Func<ModelCloneContext, bool> Predicate,
            Action<ModelCloneContext> Listener,
            long RegistrationOrder);
    }
}
