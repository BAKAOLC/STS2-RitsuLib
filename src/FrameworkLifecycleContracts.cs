namespace STS2RitsuLib
{
    /// <summary>
    ///     Base type for framework lifecycle notifications published through
    ///     <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    ///     通过 <see cref="RitsuLibFramework.SubscribeLifecycle" /> 发布的框架生命周期通知的
    ///     基类型。
    /// </summary>
    public interface IFrameworkLifecycleEvent
    {
        /// <summary>
        ///     UTC timestamp when the event was raised.
        ///     事件引发时的 UTC 时间戳。
        /// </summary>
        DateTimeOffset OccurredAtUtc { get; }
    }

    /// <summary>
    ///     Marker for events that are replayed to new subscribers when <c>replayCurrentState</c> is true.
    ///     当 <c>replayCurrentState</c> 为 true 时，会向新订阅者重放的事件标记。
    /// </summary>
    public interface IReplayableFrameworkLifecycleEvent : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired while the RitsuLib framework is initializing (before mods complete setup).
    ///     RitsuLib 框架初始化期间触发（在 mod 完成设置之前）。
    /// </summary>
    /// <param name="FrameworkModId">
    ///     Manifest id of the framework mod.
    ///     框架 mod 的清单 id。
    /// </param>
    /// <param name="FrameworkVersion">
    ///     Framework assembly or package version string.
    ///     框架程序集或包版本字符串。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件引发的时间。
    /// </param>
    public readonly record struct FrameworkInitializingEvent(
        string FrameworkModId,
        string FrameworkVersion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired after framework initialization finished.
    ///     框架初始化完成后触发。
    /// </summary>
    /// <param name="FrameworkModId">
    ///     Manifest id of the framework mod.
    ///     框架 mod 的清单 id。
    /// </param>
    /// <param name="IsActive">
    ///     Whether the framework considers itself active for this session.
    ///     框架是否认为自己在本会话中处于活动状态。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件引发的时间。
    /// </param>
    public readonly record struct FrameworkInitializedEvent(
        string FrameworkModId,
        bool IsActive,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired before profile-scoped services are initialized.
    ///     在档案作用域服务初始化之前触发。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件引发的时间。
    /// </param>
    public readonly record struct ProfileServicesInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Fired after profile-scoped services are ready.
    ///     在档案作用域服务就绪后触发。
    /// </summary>
    /// <param name="ProfileId">
    ///     Active profile identifier.
    ///     活动档案标识符。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件引发的时间。
    /// </param>
    public readonly record struct ProfileServicesInitializedEvent(
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Receives strongly typed lifecycle events from <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    ///     接收来自 <see cref="RitsuLibFramework.SubscribeLifecycle" /> 的强类型生命周期事件。
    /// </summary>
    public interface ILifecycleObserver
    {
        /// <summary>
        ///     Called for each lifecycle event; implementors typically switch on concrete event types.
        ///     针对每个生命周期事件调用；实现通常会按具体事件类型分支。
        /// </summary>
        /// <param name="evt">
        ///     The event instance.
        ///     事件实例。
        /// </param>
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

    internal sealed class LifecycleSubscriptionHolder
    {
        public IDisposable Subscription { get; set; } = null!;
    }

    internal sealed class DelegateLifecycleObserverWithSubscription<TEvent>(
        Action<TEvent, IDisposable> handler,
        LifecycleSubscriptionHolder holder
    ) : ILifecycleObserver
        where TEvent : IFrameworkLifecycleEvent
    {
        public void OnEvent(IFrameworkLifecycleEvent evt)
        {
            if (evt is TEvent typedEvent)
                handler(typedEvent, holder.Subscription);
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
