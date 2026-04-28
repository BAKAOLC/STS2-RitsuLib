using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Hooks framework lifecycle: while a non-singleplayer <see cref="RunManager.NetService" /> exists (including
    ///     lobby before <see cref="RunStartedEvent" />), sends one sidecar handshake per distinct
    ///     <c>NetService</c> instance; clears transient sidecar state when the run ends.
    /// </summary>
    public static class RitsuLibSidecarNetworkingLifecycle
    {
        private static readonly Lock Gate = new();

        private static IDisposable? _subscriptions;

        private static bool _processFrameHooked;

        private static object? _helloSentForNetService;

        /// <summary>
        ///     Subscribes once per process (idempotent). Called from <see cref="RitsuLibSidecarProtocol.EnsureDefaultHandlers" />.
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
            if (net == null || net.Type == NetGameType.Singleplayer)
            {
                _helloSentForNetService = null;
                return;
            }

            if (ReferenceEquals(_helloSentForNetService, net))
                return;

            _helloSentForNetService = net;
            RitsuLibSidecarConnectionExchange.TrySendLocalHello();
        }

        private static void OnRunEnded()
        {
            RitsuLibSidecarBus.CancelAllPendingWaits();
            RitsuLibSidecarConnectionSession.Clear();
            _helloSentForNetService = null;
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
