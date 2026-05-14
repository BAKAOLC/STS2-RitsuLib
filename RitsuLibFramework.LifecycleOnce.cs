namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Subscribes a typed callback that runs at most once per returned subscription: after each invocation the
        ///     subscription is disposed and the handler is removed.
        ///     订阅一个每个返回订阅最多只运行一次的强类型回调：每次调用后都会释放订阅并移除处理器。
        /// </summary>
        /// <typeparam name="TEvent">
        ///     Concrete lifecycle event type (must be a struct or sealed class).
        ///     具体生命周期事件类型（必须是结构体或密封类）。
        /// </typeparam>
        /// <param name="handler">
        ///     Invoked once when a matching event is delivered (including synchronous replay).
        ///     匹配事件送达时调用一次（包括同步回放）。
        /// </param>
        /// <param name="replayCurrentState">
        ///     When true, invokes <paramref name="handler" /> once if a replayable last event exists, then disposes.
        ///     为 true 时，如果存在可重放的最后事件，则调用 <paramref name="handler" /> 一次，然后释放。
        /// </param>
        /// <returns>
        ///     Disposing unsubscribes without invoking the handler.
        ///     释放返回值会取消订阅，且不会调用处理器。
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     Thrown when <typeparamref name="TEvent" /> is not eligible for typed dispatch (same rule as
        ///     <see cref="SubscribeLifecycle{TEvent}(Action{TEvent}, bool)" />).
        ///     当 <c>TEvent</c> 不符合强类型派发条件时抛出（规则与
        ///     <c>SubscribeLifecycle{TEvent}(Action{TEvent}, bool)</c> 相同）。
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
                    subscription?.Dispose();
                }
            }
        }
    }
}
