namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes a typed callback that runs at most once for the returned subscription. Each invocation
        ///         disposes the subscription and removes the handler in a <c>finally</c> path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         订阅一个强类型回调；对于返回的每个订阅，该回调最多运行一次。每次调用都会在 <c>finally</c> 中释放订阅并移除回调。
        ///     </para>
        /// </summary>
        /// <typeparam name="TEvent">
        ///     <para xml:lang="en">Concrete lifecycle event type, which must be a struct or sealed class.</para>
        ///     <para xml:lang="zh-CN">具体生命周期事件类型，必须为结构体或密封类。</para>
        /// </typeparam>
        /// <param name="handler">
        ///     <para xml:lang="en">Invoked once when a matching event is delivered, including synchronous replay.</para>
        ///     <para xml:lang="zh-CN">在匹配事件送达时调用一次，包括同步重放。</para>
        /// </param>
        /// <param name="replayCurrentState">
        ///     <para xml:lang="en">Whether to invoke <paramref name="handler" /> once for a replayable last event, then dispose.</para>
        ///     <para xml:lang="zh-CN">是否在存在可重放的最后事件时调用 <paramref name="handler" /> 一次后释放订阅。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A subscription whose disposal unsubscribes without invoking the handler.</para>
        ///     <para xml:lang="zh-CN">释放后会取消订阅且不调用回调的订阅。</para>
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     <para xml:lang="en">
        ///         Thrown when <typeparamref name="TEvent" /> is ineligible for typed dispatch, using the same rule as
        ///         <see cref="SubscribeLifecycle{TEvent}(Action{TEvent}, bool)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <typeparamref name="TEvent" /> 不符合强类型派发条件时抛出；规则与
        ///         <see cref="SubscribeLifecycle{TEvent}(Action{TEvent}, bool)" /> 相同。
        ///     </para>
        /// </exception>
        public static IDisposable SubscribeLifecycleOnce<TEvent>(
            Action<TEvent> handler,
            bool replayCurrentState = true
        )
            where TEvent : IFrameworkLifecycleEvent
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!LifecycleEventTypeCache<TEvent>.SupportsTypedDispatch)
                throw new NotSupportedException(
                    "SubscribeLifecycleOnce requires a sealed or struct lifecycle event type (typed dispatch). " +
                    $"Unsupported type: {typeof(TEvent).FullName}."
                );

            var topic = GetLifecycleTopic<TEvent>();
            FrameworkLifecycleSubscription? subscription = null;

            object? replayEvent = null;

            lock (SyncRoot)
            {
                subscription = new(() =>
                {
                    lock (SyncRoot)
                    {
                        topic.Remove(Wrapped);
                    }
                });

                topic.Add(Wrapped);

                if (replayCurrentState)
                    ReplayableLifecycleEvents.TryGetValue(LifecycleEventTypeCache<TEvent>.EventType, out replayEvent);
            }

            if (replayCurrentState && replayEvent is TEvent typedReplayEvent)
                SafeNotify(Wrapped, typedReplayEvent, LifecycleEventTypeCache<TEvent>.EventName);

            return subscription;

            void Wrapped(TEvent evt)
            {
                try
                {
                    handler(evt);
                }
                finally
                {
                    // The subscription is assigned before Wrapped can be published or invoked.
                    // ReSharper disable once AccessToModifiedClosure
                    subscription?.Dispose();
                }
            }
        }
    }
}
