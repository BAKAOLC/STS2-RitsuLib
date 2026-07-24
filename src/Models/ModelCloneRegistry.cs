using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Per-mod registration surface for vanilla model clone listeners.
    ///     原版模型复制监听器的按 mod 注册入口。
    /// </summary>
    public sealed class ModelCloneRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModelCloneRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<(string ModId, string ListenerId), ListenerEntry> Listeners = [];
        private static long _nextRegistrationOrder;

        private readonly string _modId;

        private ModelCloneRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     Returns the singleton clone registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例复制注册表，并在首次使用时创建。
        /// </summary>
        public static ModelCloneRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModelCloneRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers or replaces a listener that receives every completed <see cref="AbstractModel.MutableClone" />.
        ///     注册或替换一个监听器，以接收每次完成的 <see cref="AbstractModel.MutableClone" />。
        /// </summary>
        /// <param name="listenerId">
        ///     Unique listener id within this registry's mod.
        ///     此监听器在当前注册表 mod 内的唯一标识符。
        /// </param>
        /// <param name="listener">
        ///     Listener invoked after the clone has been created and initialized.
        ///     在复制体创建并初始化后调用的监听器。
        /// </param>
        public void Register(string listenerId, Action<ModelCloneContext> listener)
        {
            Register(listenerId, _ => true, listener);
        }

        /// <summary>
        ///     Registers or replaces a listener with a custom predicate.
        ///     注册或替换一个带自定义谓词的监听器。
        /// </summary>
        /// <param name="listenerId">
        ///     Unique listener id within this registry's mod.
        ///     此监听器在当前注册表 mod 内的唯一标识符。
        /// </param>
        /// <param name="predicate">
        ///     Predicate used to select clone operations for this listener.
        ///     用于筛选此监听器关心的复制操作。
        /// </param>
        /// <param name="listener">
        ///     Listener invoked after the clone has been created and initialized.
        ///     在复制体创建并初始化后调用的监听器。
        /// </param>
        public void Register(
            string listenerId,
            Func<ModelCloneContext, bool> predicate,
            Action<ModelCloneContext> listener)
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
        ///     Registers or replaces a typed listener for a model family, including vanilla model types.
        ///     注册或替换某个模型族的类型化监听器，包括原版模型类型。
        /// </summary>
        /// <typeparam name="TModel">
        ///     Model base or concrete type to listen for.
        ///     要监听的模型基类或具体类型。
        /// </typeparam>
        /// <param name="listenerId">
        ///     Unique listener id within this registry's mod.
        ///     此监听器在当前注册表 mod 内的唯一标识符。
        /// </param>
        /// <param name="listener">
        ///     Typed listener invoked when both prototype and cloned model are <typeparamref name="TModel" />.
        ///     当原型和复制体均为 <typeparamref name="TModel" /> 时调用的类型化监听器。
        /// </param>
        public void Register<TModel>(string listenerId, Action<TModel, TModel> listener)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context is { Prototype: TModel, ClonedModel: TModel },
                context => listener((TModel)context.Prototype, (TModel)context.ClonedModel));
        }

        /// <summary>
        ///     Removes a previously registered listener from this registry's mod.
        ///     从当前注册表 mod 移除先前注册的监听器。
        /// </summary>
        /// <param name="listenerId">
        ///     Listener id used at registration.
        ///     注册时使用的监听器标识符。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an entry was removed.
        ///     如果移除了条目，则为 <see langword="true" />。
        /// </returns>
        public bool Unregister(string listenerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);

            lock (SyncRoot)
            {
                return Listeners.Remove((_modId, listenerId));
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
                listeners = [.. Listeners.Values.OrderBy(static entry => entry.RegistrationOrder)];
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
