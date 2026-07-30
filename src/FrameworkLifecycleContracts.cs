namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the base type for framework lifecycle notifications published through
    ///         <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义通过 <see cref="RitsuLibFramework.SubscribeLifecycle" /> 发布的框架生命周期通知的基类型。</para>
    /// </summary>
    public interface IFrameworkLifecycleEvent
    {
        /// <summary>
        ///     <para xml:lang="en">UTC timestamp when the event was raised.</para>
        ///     <para xml:lang="zh-CN">引发事件时的 UTC 时间戳。</para>
        /// </summary>
        DateTimeOffset OccurredAtUtc { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Marks events replayed to new subscribers when <c>replayCurrentState</c> is
    ///         <see langword="true" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">标记在 <c>replayCurrentState</c> 为 <see langword="true" /> 时会向新订阅者重放的事件。</para>
    /// </summary>
    public interface IReplayableFrameworkLifecycleEvent : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised while RitsuLib initializes, before mods complete setup.</para>
    ///     <para xml:lang="zh-CN">在 RitsuLib 初始化期间、模组完成设置前引发。</para>
    /// </summary>
    /// <param name="FrameworkModId">
    ///     <para xml:lang="en">Manifest ID of the framework mod.</para>
    ///     <para xml:lang="zh-CN">框架模组的清单 ID。</para>
    /// </param>
    /// <param name="FrameworkVersion">
    ///     <para xml:lang="en">Framework assembly or package version string.</para>
    ///     <para xml:lang="zh-CN">框架程序集或包版本字符串。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct FrameworkInitializingEvent(
        string FrameworkModId,
        string FrameworkVersion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after framework initialization finishes.</para>
    ///     <para xml:lang="zh-CN">在框架初始化完成后引发。</para>
    /// </summary>
    /// <param name="FrameworkModId">
    ///     <para xml:lang="en">Manifest ID of the framework mod.</para>
    ///     <para xml:lang="zh-CN">框架模组的清单 ID。</para>
    /// </param>
    /// <param name="IsActive">
    ///     <para xml:lang="en">Whether the framework considers itself active for this session.</para>
    ///     <para xml:lang="zh-CN">框架是否认为自身在本会话中处于活动状态。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct FrameworkInitializedEvent(
        string FrameworkModId,
        bool IsActive,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised before profile-scoped services initialize.</para>
    ///     <para xml:lang="zh-CN">在档案作用域服务初始化前引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ProfileServicesInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after profile-scoped services are ready.</para>
    ///     <para xml:lang="zh-CN">在档案作用域服务就绪后引发。</para>
    /// </summary>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Active profile identifier.</para>
    ///     <para xml:lang="zh-CN">活动档案标识符。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ProfileServicesInitializedEvent(
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">
    ///         Receives strongly typed lifecycle events from
    ///         <see cref="RitsuLibFramework.SubscribeLifecycle" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">接收来自 <see cref="RitsuLibFramework.SubscribeLifecycle" /> 的强类型生命周期事件。</para>
    /// </summary>
    public interface ILifecycleObserver
    {
        /// <summary>
        ///     <para xml:lang="en">Receives each lifecycle event; implementations usually switch on its concrete type.</para>
        ///     <para xml:lang="zh-CN">接收每个生命周期事件；实现通常按其具体类型分支。</para>
        /// </summary>
        /// <param name="evt">
        ///     <para xml:lang="en">Lifecycle event instance.</para>
        ///     <para xml:lang="zh-CN">生命周期事件实例。</para>
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
