using Godot;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Hooks framework lifecycle and keeps sidecar session state aligned with the current net service. Handshake
    ///     negotiation is attempted only for peers marked <see cref="RitsuLibSidecarPeerReachability.Supported" />.
    ///     挂接框架生命周期，并保持 sidecar 会话状态与当前 net service 对齐。握手
    ///     协商仅会对标记为 <see cref="RitsuLibSidecarPeerReachability.Supported" /> 的 peer 尝试。
    /// </summary>
    public static class RitsuLibSidecarNetworkingLifecycle
    {
        private static readonly Lock Gate = new();

        private static IDisposable? _subscriptions;

        private static bool _processFrameHooked;

        /// <summary>
        ///     Subscribes once per process (idempotent). Called from <see cref="RitsuLibSidecarProtocol.EnsureDefaultHandlers" />.
        ///     每个进程订阅一次（幂等）。由 <see cref="RitsuLibSidecarProtocol.EnsureDefaultHandlers" /> 调用。
        /// </summary>
        public static void EnsureHooksInstalled()
        {
            if (_subscriptions != null)
                return;

            lock (Gate)
            {
                if (_subscriptions != null)
                    return;

                var a = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(_ => TryAttachProcessFrameWatch());
                var b = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => OnRunEnded());
                _subscriptions = new SubscriptionGroup(a, b);
                TryAttachProcessFrameWatch();
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
        }

        private static void OnRunEnded()
        {
            RitsuLibSidecarBus.CancelAllPendingWaits();
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
