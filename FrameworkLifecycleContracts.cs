namespace STS2RitsuLib
{
    /// <summary>
    ///     Base type for framework lifecycle notifications published through
    ///     <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    /// </summary>
    public interface IFrameworkLifecycleEvent
    {
        /// <summary>
        ///     UTC timestamp when the event was raised.
        /// </summary>
        DateTimeOffset OccurredAtUtc { get; }
    }

    /// <summary>
    ///     Marker for events that are replayed to new subscribers when <c>replayCurrentState</c> is true.
    /// </summary>
    public interface IReplayableFrameworkLifecycleEvent : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired while the RitsuLib framework is initializing (before mods complete setup).
    /// </summary>
    /// <param name="FrameworkModId">Manifest id of the framework mod.</param>
    /// <param name="FrameworkVersion">Framework assembly or package version string.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct FrameworkInitializingEvent(
        string FrameworkModId,
        string FrameworkVersion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired after framework initialization finished.
    /// </summary>
    /// <param name="FrameworkModId">Manifest id of the framework mod.</param>
    /// <param name="IsActive">Whether the framework considers itself active for this session.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct FrameworkInitializedEvent(
        string FrameworkModId,
        bool IsActive,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired before profile-scoped services are initialized.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileServicesInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired after profile-scoped services are ready.
    /// </summary>
    /// <param name="ProfileId">Active profile identifier.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileServicesInitializedEvent(
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Receives strongly typed lifecycle events from <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    /// </summary>
    public interface ILifecycleObserver
    {
        /// <summary>
        ///     Called for each lifecycle event; implementors typically switch on concrete event types.
        /// </summary>
        /// <param name="evt">The event instance.</param>
        void OnEvent(IFrameworkLifecycleEvent evt);
    }

    internal sealed class DelegateLifecycleObserver<TEvent>(Action<TEvent> handler) : ILifecycleObserver
        where TEvent : IFrameworkLifecycleEvent
    {
        public void OnEvent(IFrameworkLifecycleEvent evt)
        {
            if (evt is TEvent typedEvent)
                handler(typedEvent);
        }
    }

    internal sealed class FrameworkLifecycleSubscription(Action unsubscribe) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            unsubscribe();
        }
    }
}
