namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Subscribes a typed callback that runs at most once per returned subscription: after each invocation the
        ///     subscription is disposed and the handler is removed.
        /// </summary>
        /// <typeparam name="TEvent">Concrete lifecycle event type (must be a struct or sealed class).</typeparam>
        /// <param name="handler">Invoked once when a matching event is delivered (including synchronous replay).</param>
        /// <param name="replayCurrentState">
        ///     When true, invokes <paramref name="handler" /> once if a replayable last event exists, then disposes.
        /// </param>
        /// <returns>Disposing unsubscribes without invoking the handler.</returns>
        /// <exception cref="NotSupportedException">
        ///     Thrown when <typeparamref name="TEvent" /> is not eligible for typed dispatch (same rule as
        ///     <see cref="SubscribeLifecycle{TEvent}(Action{TEvent}, bool)" />).
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
