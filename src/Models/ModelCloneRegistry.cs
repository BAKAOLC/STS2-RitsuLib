using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en">Provides per-mod registration for base-game model clone listeners.</para>
    ///     <para xml:lang="zh-CN">提供按模组划分的游戏原版模型复制监听器注册入口。</para>
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
        ///     <para xml:lang="en">
        ///         Gets the singleton clone registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 对应的单例复制注册表，并在首次使用时创建。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers or replaces a listener that receives every completed
        ///         <see cref="AbstractModel.MutableClone" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册或替换监听器，以接收每次完成的 <see cref="AbstractModel.MutableClone" />。
        ///     </para>
        /// </summary>
        /// <param name="listenerId">
        ///     <para xml:lang="en">The unique listener ID within this mod's registry.</para>
        ///     <para xml:lang="zh-CN">监听器在当前模组注册表内的唯一 ID。</para>
        /// </param>
        /// <param name="listener">
        ///     <para xml:lang="en">The listener invoked after the clone is created and initialized.</para>
        ///     <para xml:lang="zh-CN">复制体创建并初始化后调用的监听器。</para>
        /// </param>
        public void Register(string listenerId, Action<ModelCloneContext> listener)
        {
            Register(listenerId, _ => true, listener);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a listener with a custom predicate.</para>
        ///     <para xml:lang="zh-CN">注册或替换带有自定义谓词的监听器。</para>
        /// </summary>
        /// <param name="listenerId">
        ///     <para xml:lang="en">The unique listener ID within this mod's registry.</para>
        ///     <para xml:lang="zh-CN">监听器在当前模组注册表内的唯一 ID。</para>
        /// </param>
        /// <param name="predicate">
        ///     <para xml:lang="en">The predicate used to select clone operations for this listener.</para>
        ///     <para xml:lang="zh-CN">用于筛选此监听器所接收复制操作的谓词。</para>
        /// </param>
        /// <param name="listener">
        ///     <para xml:lang="en">The listener invoked after the clone is created and initialized.</para>
        ///     <para xml:lang="zh-CN">复制体创建并初始化后调用的监听器。</para>
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
        ///     <para xml:lang="en">
        ///         Registers or replaces a typed listener for a model family, including base-game model types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册或替换某个模型族的类型化监听器，包括游戏原版模型类型。
        ///     </para>
        /// </summary>
        /// <typeparam name="TModel">
        ///     <para xml:lang="en">The model base or concrete type to listen for.</para>
        ///     <para xml:lang="zh-CN">要监听的模型基类或具体类型。</para>
        /// </typeparam>
        /// <param name="listenerId">
        ///     <para xml:lang="en">The unique listener ID within this mod's registry.</para>
        ///     <para xml:lang="zh-CN">监听器在当前模组注册表内的唯一 ID。</para>
        /// </param>
        /// <param name="listener">
        ///     <para xml:lang="en">
        ///         The typed listener invoked when both the prototype and clone are
        ///         <typeparamref name="TModel" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         原型和复制体均为 <typeparamref name="TModel" /> 时调用的类型化监听器。
        ///     </para>
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
        ///     <para xml:lang="en">Removes a previously registered listener from this mod's registry.</para>
        ///     <para xml:lang="zh-CN">从当前模组的注册表中移除先前注册的监听器。</para>
        /// </summary>
        /// <param name="listenerId">
        ///     <para xml:lang="en">The listener ID used during registration.</para>
        ///     <para xml:lang="zh-CN">注册时使用的监听器 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if an entry was removed; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若已移除条目，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
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
