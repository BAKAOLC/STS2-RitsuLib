using Godot;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Keeps the Sidecar session synchronized with framework lifecycle and network-service changes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 Sidecar 会话与框架生命周期及网络服务变更保持同步。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarNetworkingLifecycle
    {
        private static readonly Lock Gate = new();

        private static IDisposable? _subscriptions;

        private static bool _processFrameHooked;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Installs the lifecycle subscriptions and process-frame watcher once per process. If installation fails,
        ///         acquired subscriptions are disposed before the failure is rethrown, so a later call may retry.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在每个进程中安装一次生命周期订阅和逐帧监视器。安装失败时会先释放已获取的订阅，再重新抛出失败，
        ///         因此后续调用可以重试。
        ///     </para>
        /// </summary>
        public static void EnsureHooksInstalled()
        {
            if (_subscriptions != null)
                return;

            lock (Gate)
            {
                if (_subscriptions != null)
                    return;

                IDisposable? gameReadySubscription = null;
                IDisposable? runEndedSubscription = null;
                try
                {
                    gameReadySubscription =
                        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(_ => TryAttachProcessFrameWatch());
                    runEndedSubscription = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => OnRunEnded());
                    TryAttachProcessFrameWatch();
                    _subscriptions = new SubscriptionGroup(gameReadySubscription, runEndedSubscription);
                }
                catch (Exception installException)
                {
                    List<Exception>? cleanupExceptions = null;
                    TryDispose(runEndedSubscription);
                    TryDispose(gameReadySubscription);
                    if (cleanupExceptions == null)
                        throw;

                    cleanupExceptions.Insert(0, installException);
                    throw new AggregateException(
                        "Sidecar lifecycle installation and rollback both failed.",
                        cleanupExceptions);

                    void TryDispose(IDisposable? subscription)
                    {
                        if (subscription == null)
                            return;

                        try
                        {
                            subscription.Dispose();
                        }
                        catch (Exception cleanupException)
                        {
                            cleanupExceptions ??= [];
                            cleanupExceptions.Add(cleanupException);
                        }
                    }
                }
            }
        }

        private static void TryAttachProcessFrameWatch()
        {
            if (_processFrameHooked)
                return;

            if (Engine.GetMainLoop() is not SceneTree tree)
                return;

            tree.ProcessFrame += OnSceneProcessFrame;
            _processFrameHooked = true;
        }

        private static void OnSceneProcessFrame()
        {
            var rm = RunManager.Instance;
            var net = rm?.NetService;
            if (net == null)
                return;

            RitsuLibSidecarSessionManager.ObserveNetService(net);
            RitsuLibSidecarConnectionExchange.TickHandshakeNegotiation();
            RitsuLibSidecarSessionManager.RefreshAllReachabilityFromProviders();
            RitsuLibSidecarConnectionExchange.TrySendClientHelloIfReachable(net);
            RitsuLibSidecarEndpointRegistry.TickBulkTransfers();
            RitsuLibSidecarOutboundScheduler.Tick(net);
        }

        private static void OnRunEnded()
        {
            RitsuLibSidecarBus.CancelAllPendingWaits();
            RitsuLibSidecarSync.Clear();
            RitsuLibSidecarConnectionSession.Clear();
            RitsuLibSidecarSessionManager.ObserveNetService(null);
            RitsuLibSidecarConnectionExchange.DiscardNegotiationStateAfterSessionEnds();
        }

        private sealed class SubscriptionGroup : IDisposable
        {
            private readonly IDisposable _a;
            private readonly IDisposable _b;

            internal SubscriptionGroup(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                _a.Dispose();
                _b.Dispose();
            }
        }
    }
}
