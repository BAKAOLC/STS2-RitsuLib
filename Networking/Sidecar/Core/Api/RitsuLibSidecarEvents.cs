namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Event facade for sub-mod consumers that prefer stable subscribe/unsubscribe helpers.
    /// </summary>
    public static class RitsuLibSidecarEvents
    {
        /// <summary>
        ///     Subscribes session-bound events.
        /// </summary>
        public static IDisposable OnSessionBound(Action<SidecarSessionBoundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionBound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionBound -= handler);
        }

        /// <summary>
        ///     Subscribes session-unbound events.
        /// </summary>
        public static IDisposable OnSessionUnbound(Action<SidecarSessionUnboundEvent> handler)
        {
            RitsuLibSidecarSessionManager.SessionUnbound += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.SessionUnbound -= handler);
        }

        /// <summary>
        ///     Subscribes peer reachability transition events.
        /// </summary>
        public static IDisposable OnPeerReachabilityChanged(Action<SidecarPeerReachabilityChangedEvent> handler)
        {
            RitsuLibSidecarSessionManager.PeerReachabilityChanged += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.PeerReachabilityChanged -= handler);
        }

        /// <summary>
        ///     Subscribes handshake-completed events.
        /// </summary>
        public static IDisposable OnHandshakeCompleted(Action<SidecarHandshakeCompletedEvent> handler)
        {
            RitsuLibSidecarSessionManager.HandshakeCompleted += handler;
            return new Subscription(() => RitsuLibSidecarSessionManager.HandshakeCompleted -= handler);
        }

        /// <summary>
        ///     Subscribes typed-message receive events.
        /// </summary>
        public static IDisposable OnTypedMessageReceived(Action<SidecarTypedMessageReceivedEvent> handler)
        {
            RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived += handler;
            return new Subscription(() => RitsuLibSidecarTypedMessageRegistry.TypedMessageReceived -= handler);
        }

        /// <summary>
        ///     Subscribes config topic-change events.
        /// </summary>
        public static IDisposable OnConfigTopicChanged(Action<SidecarConfigTopicChangedEvent> handler)
        {
            RitsuLibSidecarConfigSyncService.TopicChanged += handler;
            return new Subscription(() => RitsuLibSidecarConfigSyncService.TopicChanged -= handler);
        }

        /// <summary>
        ///     Subscribes required-capability validation completion events.
        /// </summary>
        public static IDisposable OnRequiredCapabilityCheck(
            Action<SidecarRequiredCapabilityCheckCompletedEvent> handler)
        {
            RitsuLibSidecarRequiredCapabilities.CheckCompleted += handler;
            return new Subscription(() => RitsuLibSidecarRequiredCapabilities.CheckCompleted -= handler);
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                dispose();
            }
        }
    }
}
